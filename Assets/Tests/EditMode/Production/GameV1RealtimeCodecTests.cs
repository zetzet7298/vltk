using System;
using Game.V1;
using NUnit.Framework;
using VLTK.Production.Networking;

namespace VLTK.Tests.Production.EditMode
{
    public sealed class GameV1RealtimeCodecTests
    {
        private const string GoldenMoveDelimitedHex = "270a057265712d311007180120035a180a16080110091a05636d642d31520908d00f10f30318a30c";

        [Test]
        public void GeneratedMoveInput_Exists()
        {
            var move = new MoveInput { AxisXMilli = 1000, AxisYMilli = -250, FacingMillirad = 1571 };
            Assert.That(move.AxisXMilli, Is.EqualTo(1000));
            Assert.That(move.AxisYMilli, Is.EqualTo(-250));
        }

        [Test]
        public void ClientHello_PopulatesCompleteContentDigest()
        {
            var codec = new GameV1RealtimeCodec();
            ClientEnvelope envelope = GameV1RealtimeCodec.DecodeDelimited<ClientEnvelope>(codec.EncodeClientHello(new RealtimeAdmission { endpoint = "wss://realm.example/game", admissionTicket = "ticket", clientVersion = "editor-p1", content = ValidContent() }));

            Assert.That(envelope.Hello.AcceptedContent.ContentReleaseId, Is.EqualTo("30000000-0000-0000-0000-000000000053"));
            Assert.That(envelope.Hello.AcceptedContent.ManifestSha256, Is.EqualTo(new string('a', 64)));
            Assert.That(envelope.Hello.AcceptedContent.SourceSnapshotId, Is.EqualTo("map-runtime"));
            Assert.That(envelope.Hello.AcceptedContent.CatalogUnionSize, Is.EqualTo(242));
            Assert.That(envelope.Hello.AcceptedContent.CatalogUnionSha256, Is.EqualTo(new string('b', 64)));
            Assert.That(envelope.Hello.AcceptedContent.RuntimeSkillPolicyId, Is.EqualTo("p1"));
            Assert.That(envelope.Hello.AcceptedContent.ClientProjectionSha256, Is.EqualTo(new string('c', 64)));
        }

        [Test]
        public void ServerHelloAndInitialSnapshot_KeepCursorOnMap53()
        {
            var codec = new GameV1RealtimeCodec();
            codec.EncodeClientHello(new RealtimeAdmission { endpoint = "wss://realm.example/game", admissionTicket = "ticket", clientVersion = "editor-p1", content = ValidContent() });
            var ack = new ServerEnvelope { SessionEpoch = 7, ServerSeq = 1, ServerTick = 10, Hello = new ServerHello { Protocol = "game.v1", TickRateHz = 18, SessionEpoch = 7, ReconnectGraceSeconds = 15, ActiveContent = ValidDigestProto() } };
            var snapshot = new ServerEnvelope { SessionEpoch = 7, ServerSeq = 2, ServerTick = 11, Snapshot = new WorldSnapshot { Full = true, Upserts = { new EntityState { EntityId = "local-avatar", MapId = 53 } } } };

            Assert.That(codec.DecodeAdmissionAck(GameV1RealtimeCodec.EncodeDelimited(ack)).admitted, Is.True);
            codec.DecodeInitialSnapshot(GameV1RealtimeCodec.EncodeDelimited(snapshot));

            Assert.That(codec.LastSnapshot, Is.Not.Null);
            Assert.That(codec.LastServerSeq, Is.EqualTo(2));
        }

        [Test]
        public void MoveInputDelimitedFrame_MatchesGoGolden()
        {
            var codec = new GameV1RealtimeCodec();
            codec.BeginSession(7, 3, 0);
            byte[] frame = codec.EncodeMoveInput(new MoveIntent(1f, -0.25f, 1000, -250, 9, 9, "cmd-1", "req-1", 1571));
            string hex = BitConverter.ToString(frame).Replace("-", string.Empty).ToLowerInvariant();
            Assert.That(hex, Is.EqualTo(GoldenMoveDelimitedHex));
        }

        private static VerifiedContentResponse ValidContent()
        {
            return new VerifiedContentResponse
            {
                verified = true,
                mapId = 53,
                contentDigest = new ContentDigestDto
                {
                    contentReleaseId = "30000000-0000-0000-0000-000000000053",
                    manifestSha256 = new string('a', 64),
                    sourceSnapshotId = "map-runtime",
                    catalogUnionSize = 242,
                    catalogUnionSha256 = new string('b', 64),
                    runtimeSkillPolicyId = "p1",
                    clientProjectionSha256 = new string('c', 64)
                },
                trust = new ContentTrustResult(ContentTrustMode.EditorPinnedDigest, true, false, null),
                provenanceSha256 = new string('d', 64)
            };
        }

        private static ContentDigest ValidDigestProto()
        {
            return new ContentDigest
            {
                ContentReleaseId = "30000000-0000-0000-0000-000000000053",
                ManifestSha256 = new string('a', 64),
                SourceSnapshotId = "map-runtime",
                CatalogUnionSize = 242,
                CatalogUnionSha256 = new string('b', 64),
                RuntimeSkillPolicyId = "p1",
                ClientProjectionSha256 = new string('c', 64)
            };
        }
    }
}
