using System;
using System.IO;
using Google.Protobuf;
using NUnit.Framework;
using VLTK.SkillPort;

namespace VLTK.Tests.SkillPort
{
    [Category("SkillPort")]
    public class GameV1SkillPortAdapterTests
    {
        private const string ReleaseId = "0f44f6d2-f1ca-4f2d-a228-4ea1875e59aa";
        private const string CurrentProjectionHash = "498e2f3d14d352b7924aaebdff17765aee719f968804fd3c96a54f127286a773";
        private const string CurrentManifestHash = "016f56f198eef6438f801f87a5ca3fdfa88610356c6ae27b4d71bd427ab6b61a";

        [Test]
        public void Loader_VerifiesCurrentProjectionHashAndPreservesBlockersExposure()
        {
            SkillPortProjectionLoadResult result = LoadCurrentDevelopmentProjection();

            Assert.IsTrue(result.success, result.detail);
            Assert.AreEqual(242, result.projection.rows.Count);
            Assert.AreEqual(CurrentProjectionHash, result.projection.projectionSha256);
            Assert.AreEqual(CurrentManifestHash, result.projection.manifestSha256);
            Assert.IsTrue(result.projection.TryGetRow(4, out SkillPortClientSkillRow row));
            Assert.AreEqual("exposed", row.exposureState);
            CollectionAssert.Contains(row.blockers, "presentation_lifecycle_source_only");
        }

        [Test]
        public void Loader_ProductionRejectsTestOnlySigningKey()
        {
            SkillPortProjectionLoadResult result = SkillPortClientProjectionLoader.LoadFromDirectory(CurrentSkillPortDirectory());

            Assert.IsFalse(result.success);
            Assert.AreEqual(SkillPortProjectionLoadFailure.SignatureRejected, result.failure);
        }

        [Test]
        public void Loader_BlocksClientProtobufHashMismatch()
        {
            string dir = CopyCurrentProjectionToTemp();
            string client = Path.Combine(dir, "skill_port.client.pb");
            byte[] bytes = File.ReadAllBytes(client);
            bytes[bytes.Length - 1] ^= 0x01;
            File.WriteAllBytes(client, bytes);

            SkillPortProjectionLoadResult result = SkillPortClientProjectionLoader.LoadDevelopmentFixtureFromDirectory(dir);

            Assert.IsFalse(result.success);
            Assert.AreEqual(SkillPortProjectionLoadFailure.HashMismatch, result.failure);
        }

        [Test]
        public void Loader_BlocksInvalidFixtureSignature()
        {
            string dir = CopyCurrentProjectionToTemp();
            string manifest = Path.Combine(dir, "manifest.json");
            File.WriteAllText(manifest, File.ReadAllText(manifest).Replace(
                SkillPortManifestVerifiers.FixtureSignatureBase64,
                Convert.ToBase64String(new byte[64])));

            SkillPortProjectionLoadResult result = SkillPortClientProjectionLoader.LoadDevelopmentFixtureFromDirectory(dir);

            Assert.IsFalse(result.success);
            Assert.AreEqual(SkillPortProjectionLoadFailure.SignatureRejected, result.failure);
        }

        [Test]
        public void Loader_BlocksUnknownAdditiveManifestField()
        {
            string dir = CopyCurrentProjectionToTemp();
            string manifest = Path.Combine(dir, "manifest.json");
            File.WriteAllText(manifest, File.ReadAllText(manifest).Replace(
                "\"artifacts\": [",
                "\"unknown\": 0,\n  \"artifacts\": ["));

            SkillPortProjectionLoadResult result = SkillPortClientProjectionLoader.LoadDevelopmentFixtureFromDirectory(dir);

            Assert.IsFalse(result.success);
            Assert.AreEqual(SkillPortProjectionLoadFailure.UnknownField, result.failure);
        }

        [Test]
        public void Loader_BlocksUnblockedProtobufRow()
        {
            string dir = CopyCurrentProjectionToTemp();
            global::Content.V1.ClientSkillCatalog catalog = ReadCurrentCatalog();
            catalog.Rows[0].Blockers.Clear();
            catalog.Rows[0].ExposureState = global::Content.V1.ExposureState.Exposed;
            RewriteClientProtobufAndManifest(dir, catalog.ToByteArray());

            SkillPortProjectionLoadResult result = LoadWithAcceptingVerifier(dir);

            Assert.IsFalse(result.success);
            Assert.AreEqual(SkillPortProjectionLoadFailure.BlockedSkillExposure, result.failure);
        }

        [Test]
        public void Loader_BlocksDependencyWithoutBlockerData()
        {
            string dir = CopyCurrentProjectionToTemp();
            global::Content.V1.ClientSkillCatalog catalog = ReadCurrentCatalog();
            global::Content.V1.ClientSkillRow row = null;
            foreach (global::Content.V1.ClientSkillRow candidate in catalog.Rows)
            {
                if (candidate.AssetDependencies.Count > 0)
                {
                    row = candidate;
                    break;
                }
            }
            Assert.NotNull(row, "fixture must contain at least one asset dependency");
            row.AssetDependencies[0].Blockers.Clear();
            RewriteClientProtobufAndManifest(dir, catalog.ToByteArray());

            SkillPortProjectionLoadResult result = LoadWithAcceptingVerifier(dir);

            Assert.IsFalse(result.success);
            Assert.AreEqual(SkillPortProjectionLoadFailure.MissingDependencyData, result.failure);
        }

        [Test]
        public void AuthoritativeLoader_IgnoresShadowJsonMutation()
        {
            string dir = CopyCurrentProjectionToTemp();
            File.WriteAllText(Path.Combine(dir, "skill_port.client.json"), "{not-json");

            SkillPortProjectionLoadResult authoritative = SkillPortClientProjectionLoader.LoadDevelopmentFixtureFromDirectory(dir);
            SkillPortProjectionLoadResult shadow = SkillPortClientProjectionLoader.LoadJsonShadowFromDirectory(dir);

            Assert.IsTrue(authoritative.success, authoritative.detail);
            Assert.IsFalse(shadow.success);
        }

        [Test]
        public void RuntimePolicy_DisablesBlockedCurrentSkill()
        {
            SkillPortClientProjection projection = LoadCurrentDevelopmentProjection().projection;

            RuntimePolicySnapshot policy = SkillPortClientProjectionLoader.BuildRuntimePolicy(projection, 9);

            Assert.AreEqual(SkillAuthorityMode.Disabled, policy.Resolve(4, "Shaolin").authorityMode);
        }

        [Test]
        public void ContentDigest_MapsClientAndServerHello()
        {
            SkillPortClientProjection projection = LoadCurrentDevelopmentProjection().projection;
            var digest = new ContentReleaseDigest(ReleaseId, new string('a', 64), new string('b', 64));

            global::Game.V1.ClientHello hello = GameV1SkillPortAdapters.ToProtoClientHello(
                "ticket", "1.0", projection, 0, 7, 100);
            var serverHello = new global::Game.V1.ServerHello
            {
                Protocol = "game.v1",
                ContentReleaseId = ReleaseId,
                ActiveContent = GameV1SkillPortAdapters.ToProtoContentDigest(digest, "slice", 242, "gate0"),
            };

            Assert.AreEqual("game.v1", hello.Protocol);
            Assert.AreEqual(15, hello.SupportedReconnectGraceSeconds);
            Assert.AreEqual(projection.catalogUnionSha256, hello.AcceptedContent.CatalogUnionSha256);
            Assert.AreEqual(CurrentProjectionHash, hello.AcceptedContent.ClientProjectionSha256);
            Assert.IsTrue(digest.ExactMatch(GameV1SkillPortAdapters.ActiveContentDigest(serverHello)));
        }

        [Test]
        public void CombatEvent_MapsIntoReducerLifecycle()
        {
            var reducer = new CombatPresentationReducer();
            Assert.IsTrue(reducer.ApplySnapshot(new CombatPresentationSnapshot { serverSequence = 10, baselineTick = 100 }));
            var proto = new global::Game.V1.CombatEvent
            {
                EventId = "evt-1",
                ServerTick = 101,
                Kind = global::Game.V1.CombatEventKind.CastStarted,
                CastId = "cast-1",
                SourceEntityId = "player-1",
                SkillId = 4,
            };

            CombatLifecycleEvent evt = GameV1SkillPortAdapters.ToLifecycleEvent(proto, 11);

            Assert.AreEqual(PresentationApplyResult.Applied, reducer.Apply(evt));
            Assert.AreEqual(1, reducer.casts.Count);
            Assert.AreEqual(SkillTriggerPhase.CastStart, evt.triggerPhase);
        }

        [Test]
        public void ActiveCombatResyncState_LoadsReducerSnapshot()
        {
            var state = new global::Game.V1.ActiveCombatResyncState
            {
                BaselineTick = 100,
                Full = true,
            };
            state.ActiveCasts.Add(new global::Game.V1.ActiveCastState
            {
                CastId = "cast-1",
                SourceEntityId = "player-1",
                SkillId = 4,
                StartedTick = 95,
            });
            state.ActiveMissiles.Add(new global::Game.V1.ActiveMissileState
            {
                MissileInstanceId = "missile-1",
                MissileId = 33,
                SpawnedTick = 96,
                X = 10,
                Y = 20,
            });

            CombatPresentationSnapshot snapshot = GameV1SkillPortAdapters.ToPresentationSnapshot(state, 20);
            var reducer = new CombatPresentationReducer();

            Assert.IsTrue(reducer.ApplySnapshot(snapshot));
            Assert.AreEqual(1, reducer.casts.Count);
            Assert.AreEqual(1, reducer.missiles.Count);
        }

        [Test]
        public void SequenceCursor_AcceptsGeneratedServerEnvelope()
        {
            var cursor = new RealtimeSessionCursor();
            cursor.Begin(7, 10, 100);
            cursor.AllocateClientSequence();
            var envelope = new global::Game.V1.ServerEnvelope
            {
                SessionEpoch = 7,
                ServerSeq = 11,
                LastProcessedClientSeq = 1,
                ServerTick = 101,
            };

            Assert.AreEqual(ServerEnvelopeAcceptance.Accepted, GameV1SkillPortAdapters.AcceptServerEnvelope(cursor, envelope));
        }

        [Test]
        public void Serialization_RoundTripsAndPreservesUnknownFields()
        {
            var envelope = new global::Game.V1.ClientEnvelope
            {
                RequestId = "req-1",
                SessionEpoch = 7,
                ClientSeq = 1,
                Hello = GameV1SkillPortAdapters.ToProtoClientHello(
                    "ticket",
                    "1.0",
                    LoadCurrentDevelopmentProjection().projection,
                    0,
                    0,
                    0),
            };
            byte[] bytesWithUnknown = AppendUnknownVarint(envelope, fieldNumber: 999, value: 7);

            global::Game.V1.ClientEnvelope parsed = GameV1SkillPortAdapters.ParseClientEnvelope(bytesWithUnknown);
            byte[] roundTrip = GameV1SkillPortAdapters.Serialize(parsed);

            Assert.AreEqual(envelope.RequestId, parsed.RequestId);
            CollectionAssert.AreEqual(bytesWithUnknown, roundTrip);
        }

        private static string CurrentSkillPortDirectory()
        {
            return Path.GetFullPath(Path.Combine("Assets", "StreamingAssets", "Generated", "SkillPort"));
        }

        private static string CopyCurrentProjectionToTemp()
        {
            string dir = Path.Combine(Path.GetTempPath(), "vltk-skillport-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            File.Copy(Path.Combine(CurrentSkillPortDirectory(), "skill_port.client.pb"), Path.Combine(dir, "skill_port.client.pb"));
            File.Copy(Path.Combine(CurrentSkillPortDirectory(), "skill_port.client.json"), Path.Combine(dir, "skill_port.client.json"));
            File.Copy(Path.Combine(CurrentSkillPortDirectory(), "manifest.json"), Path.Combine(dir, "manifest.json"));
            return dir;
        }

        private static SkillPortProjectionLoadResult LoadCurrentDevelopmentProjection()
        {
            SkillPortProjectionLoadResult result =
                SkillPortClientProjectionLoader.LoadDevelopmentFixtureFromDirectory(CurrentSkillPortDirectory());
            Assert.IsTrue(result.success, result.detail);
            return result;
        }

        private static global::Content.V1.ClientSkillCatalog ReadCurrentCatalog()
        {
            return global::Content.V1.ClientSkillCatalog.Parser.ParseFrom(
                File.ReadAllBytes(Path.Combine(CurrentSkillPortDirectory(), "skill_port.client.pb")));
        }

        private static SkillPortProjectionLoadResult LoadWithAcceptingVerifier(string dir)
        {
            return SkillPortClientProjectionLoader.Load(
                Path.Combine(dir, "skill_port.client.pb"),
                Path.Combine(dir, "manifest.json"),
                new AcceptingManifestVerifier(),
                SkillPortManifestTrustPolicy.DevelopmentFixture);
        }

        private static void RewriteClientProtobufAndManifest(string dir, byte[] bytes)
        {
            string client = Path.Combine(dir, "skill_port.client.pb");
            string manifest = Path.Combine(dir, "manifest.json");
            File.WriteAllBytes(client, bytes);
            string json = File.ReadAllText(manifest)
                .Replace(CurrentProjectionHash, Sha256(bytes))
                .Replace("\"sizeBytes\": 235046", "\"sizeBytes\": " + bytes.Length);
            File.WriteAllText(manifest, json);
        }

        private sealed class AcceptingManifestVerifier : ISkillPortManifestVerifier
        {
            public bool VerifyManifestSignature(
                string signingKeyId,
                byte[] canonicalSigningPayload,
                string signatureBase64,
                SkillPortManifestTrustPolicy policy,
                out string detail)
            {
                detail = null;
                return true;
            }
        }

        private static byte[] AppendUnknownVarint(IMessage message, int fieldNumber, ulong value)
        {
            using (var stream = new MemoryStream())
            {
                byte[] bytes = message.ToByteArray();
                stream.Write(bytes, 0, bytes.Length);
                WriteVarint(stream, ((ulong)fieldNumber << 3) | 0UL);
                WriteVarint(stream, value);
                return stream.ToArray();
            }
        }

        private static void WriteVarint(Stream stream, ulong value)
        {
            while (value > 127)
            {
                stream.WriteByte((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }
            stream.WriteByte((byte)value);
        }

        private static string Sha256(byte[] bytes)
        {
            using (System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                char[] chars = new char[hash.Length * 2];
                const string hex = "0123456789abcdef";
                for (int i = 0; i < hash.Length; i++)
                {
                    chars[i * 2] = hex[hash[i] >> 4];
                    chars[i * 2 + 1] = hex[hash[i] & 0xF];
                }
                return new string(chars);
            }
        }
    }
}
