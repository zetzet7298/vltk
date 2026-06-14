// -----------------------------------------------------------------------------
// VLTK Mobile — MinimapService EditMode tests.
// Kiểm tra minimap/world-map: resolve artifact, coord conversions, missing state.
// PC source: M1.8 minimap/world-map + asset registry.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class MinimapServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IMinimapHost
        {
            public int ResolvedCalls;
            public int MissingCalls;
            public int NoRefCalls;
            public int WorldToMinimapCalls;
            public int MinimapToWorldCalls;
            public int ShowCalls;
            public int LogCalls;
            public int SfxCalls;
            public int SaveCalls;
            public int LastMapId;
            public string LastSourceId;
            public string LastArtifactPath;
            public string LastReason;
            public string LastSettingSourceId;
            public float LastWorldX;
            public float LastWorldY;
            public float LastU;
            public float LastV;
            public float LastPixelX;
            public float LastPixelY;
            public float LastOutWorldX;
            public float LastOutWorldY;
            public bool LastMissing;

            public void OnMinimapResolved(int mapId, string sourceId, string artifactPath)
            {
                ResolvedCalls++;
                LastMapId = mapId;
                LastSourceId = sourceId;
                LastArtifactPath = artifactPath;
            }
            public void OnMinimapMissing(int mapId, string sourceId, string reason)
            {
                MissingCalls++;
                LastReason = reason;
            }
            public void OnMapNoMinimapRef(int mapId, string settingSourceId)
            {
                NoRefCalls++;
                LastSettingSourceId = settingSourceId;
            }
            public void OnWorldToMinimap(int mapId, float worldX, float worldY, float u, float v)
            {
                WorldToMinimapCalls++;
                LastWorldX = worldX;
                LastWorldY = worldY;
                LastU = u;
                LastV = v;
            }
            public void OnMinimapToWorld(int mapId, float pixelX, float pixelY, float worldX, float worldY)
            {
                MinimapToWorldCalls++;
                LastPixelX = pixelX;
                LastPixelY = pixelY;
                LastOutWorldX = worldX;
                LastOutWorldY = worldY;
            }
            public void ShowMinimapUI(int mapId, string artifactPath, bool missing)
            {
                ShowCalls++;
                LastMissing = missing;
            }
            public void LogMinimapEvent(int mapId, string message) { LogCalls++; }
            public void PlayMinimapSFX(int mapId, string action) { SfxCalls++; }
            public void SaveMinimapState(int mapId, string sourceId, string artifactPath) { SaveCalls++; }
        }

        // ── Registry fake ────────────────────────────────────────────────────

        private sealed class FakeRegistry : IAssetRegistry
        {
            private readonly Dictionary<string, AssetRegistryEntry> _byPath = new();
            public void Register(AssetRegistryEntry e)
            {
                if (e?.sourceId == null) return;
                _byPath[e.sourceId.ToKey()] = e;
            }
            public AssetRegistryEntry Resolve(string sourcePath)
            {
                if (sourcePath == null) return null;
                _byPath.TryGetValue(sourcePath, out var e);
                return e;
            }
            public AssetRegistryEntry Resolve(int uid) => null;
            public AssetRegistryEntry Resolve(SourceAssetId sourceId) => Resolve(sourceId?.ToKey());
            public IReadOnlyList<AssetRegistryEntry> GetAll() => new List<AssetRegistryEntry>();
            public IReadOnlyList<AssetRegistryEntry> GetByStatus(AssetStatus s) => new List<AssetRegistryEntry>();
            public IReadOnlyList<AssetRegistryEntry> GetByMapId(int m) => new List<AssetRegistryEntry>();
            public ValidationResult Validate() => new ValidationResult();
        }

        private static MapDefinition MakeMap(int id, Rect rect, MinimapRef ref_ = null, string sourceId = null)
        {
            return new MapDefinition
            {
                catalogEntry = new MapCatalogEntry { mapId = id, settingSourceId = sourceId ?? $"map{id}" },
                sourceBoundsRect = rect,
                minimapRef = ref_,
            };
        }

        private static MinimapRef MakeRef(string sourceId, string artifactPath = null, MinimapArtifactStatus status = MinimapArtifactStatus.Registered)
        {
            return new MinimapRef
            {
                sourceId = new SourceAssetId { category = "minimap", path = sourceId },
                artifactPath = artifactPath,
                status = status,
            };
        }

        // ── Ctor / AttachHost ────────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new MinimapService(new FakeRegistry());
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var svc = new MinimapService(new FakeRegistry(), host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new MinimapService(new FakeRegistry());
            svc.AttachHost(host);
            svc.ResolveArtifact(MakeMap(1, new Rect(0, 0, 100, 100)));
            // No minimap ref → OnMapNoMinimapRef is dispatched
            Assert.AreEqual(1, host.NoRefCalls);
        }

        // ── ResolveArtifact ──────────────────────────────────────────────────

        [Test]
        public void ResolveArtifact_NullMap_ReturnsNull()
        {
            var svc = new MinimapService(new FakeRegistry());
            Assert.IsNull(svc.ResolveArtifact(null));
        }

        [Test]
        public void ResolveArtifact_NoMinimapRef_CreatesMissing()
        {
            var host = new FakeHost();
            var svc = new MinimapService(new FakeRegistry(), host);
            var map = MakeMap(1, new Rect(0, 0, 100, 100));
            var mmRef = svc.ResolveArtifact(map);
            Assert.IsNotNull(mmRef);
            Assert.AreEqual(MinimapArtifactStatus.Missing, mmRef.status);
        }

        [Test]
        public void ResolveArtifact_NoMinimapRef_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new MinimapService(new FakeRegistry(), host);
            var map = MakeMap(1, new Rect(0, 0, 100, 100));
            svc.ResolveArtifact(map);
            Assert.AreEqual(1, host.NoRefCalls);
            Assert.AreEqual(1, host.MissingCalls);
            Assert.AreEqual(1, host.ShowCalls);
            Assert.IsTrue(host.LastMissing);
        }

        [Test]
        public void ResolveArtifact_AvailableArtifact_Registers()
        {
            var reg = new FakeRegistry();
            reg.Register(new AssetRegistryEntry
            {
                sourceId = new SourceAssetId { category = "minimap", path = "minimap/r001" },
                unityAssetPath = "Assets/Minimaps/r001.png",
                status = AssetStatus.Available,
            });
            var host = new FakeHost();
            var svc = new MinimapService(reg, host);
            var map = MakeMap(1, new Rect(0, 0, 100, 100), MakeRef("minimap/r001"));
            var mmRef = svc.ResolveArtifact(map);
            Assert.AreEqual(MinimapArtifactStatus.Registered, mmRef.status);
            Assert.AreEqual("Assets/Minimaps/r001.png", mmRef.artifactPath);
        }

        [Test]
        public void ResolveArtifact_AvailableArtifact_DispatchesResolved()
        {
            var reg = new FakeRegistry();
            reg.Register(new AssetRegistryEntry
            {
                sourceId = new SourceAssetId { category = "minimap", path = "minimap/r001" },
                unityAssetPath = "Assets/Minimaps/r001.png",
                status = AssetStatus.Available,
            });
            var host = new FakeHost();
            var svc = new MinimapService(reg, host);
            var map = MakeMap(1, new Rect(0, 0, 100, 100), MakeRef("minimap/r001"));
            svc.ResolveArtifact(map);
            Assert.AreEqual(1, host.ResolvedCalls);
            Assert.AreEqual(1, host.MissingCalls == 0 ? 1 : 0);  // 0 means resolved, not missing
        }

        [Test]
        public void ResolveArtifact_MissingArtifact_DispatchesMissing()
        {
            var host = new FakeHost();
            var svc = new MinimapService(new FakeRegistry(), host);
            var map = MakeMap(1, new Rect(0, 0, 100, 100), MakeRef("minimap/unknown"));
            svc.ResolveArtifact(map);
            Assert.AreEqual(1, host.MissingCalls);
            Assert.AreEqual(0, host.ResolvedCalls);
        }

        // ── WorldToMinimapNormalized ─────────────────────────────────────────

        [Test]
        public void WorldToMinimapNormalized_NullMap_Center()
        {
            var svc = new MinimapService(new FakeRegistry());
            var n = svc.WorldToMinimapNormalized(null, Vector2.zero);
            Assert.AreEqual(0.5f, n.x);
            Assert.AreEqual(0.5f, n.y);
        }

        [Test]
        public void WorldToMinimapNormalized_ZeroRect_Center()
        {
            var svc = new MinimapService(new FakeRegistry());
            var n = svc.WorldToMinimapNormalized(MakeMap(1, new Rect(0, 0, 0, 0)), Vector2.zero);
            Assert.AreEqual(0.5f, n.x);
        }

        [Test]
        public void WorldToMinimapNormalized_Center()
        {
            var svc = new MinimapService(new FakeRegistry());
            var n = svc.WorldToMinimapNormalized(MakeMap(1, new Rect(0, 0, 100, 100)), new Vector2(50, 50));
            Assert.AreEqual(0.5f, n.x);
            Assert.AreEqual(0.5f, n.y);
        }

        [Test]
        public void WorldToMinimapNormalized_TopLeft()
        {
            var svc = new MinimapService(new FakeRegistry());
            var n = svc.WorldToMinimapNormalized(MakeMap(1, new Rect(0, 0, 100, 100)), new Vector2(0, 0));
            Assert.AreEqual(0f, n.x);
            Assert.AreEqual(0f, n.y);
        }

        [Test]
        public void WorldToMinimapNormalized_BottomRight()
        {
            var svc = new MinimapService(new FakeRegistry());
            var n = svc.WorldToMinimapNormalized(MakeMap(1, new Rect(0, 0, 100, 100)), new Vector2(100, 100));
            Assert.AreEqual(1f, n.x);
            Assert.AreEqual(1f, n.y);
        }

        [Test]
        public void WorldToMinimapNormalized_ClampsOutside()
        {
            var svc = new MinimapService(new FakeRegistry());
            var n = svc.WorldToMinimapNormalized(MakeMap(1, new Rect(0, 0, 100, 100)), new Vector2(200, 200));
            Assert.AreEqual(1f, n.x);
            Assert.AreEqual(1f, n.y);
        }

        [Test]
        public void WorldToMinimapNormalized_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new MinimapService(new FakeRegistry(), host);
            svc.WorldToMinimapNormalized(MakeMap(1, new Rect(0, 0, 100, 100)), new Vector2(50, 50));
            Assert.AreEqual(1, host.WorldToMinimapCalls);
            Assert.AreEqual(0.5f, host.LastU);
            Assert.AreEqual(0.5f, host.LastV);
        }

        // ── WorldToMinimapPixel ─────────────────────────────────────────────

        [Test]
        public void WorldToMinimapPixel_Center()
        {
            var svc = new MinimapService(new FakeRegistry());
            var p = svc.WorldToMinimapPixel(MakeMap(1, new Rect(0, 0, 100, 100)), new Vector2(50, 50), new Vector2(200, 200));
            Assert.AreEqual(100f, p.x);
            Assert.AreEqual(100f, p.y);
        }

        [Test]
        public void WorldToMinimapPixel_YInverted()
        {
            var svc = new MinimapService(new FakeRegistry());
            // Top-left world coord → bottom-left of minimap (y inverted)
            var p = svc.WorldToMinimapPixel(MakeMap(1, new Rect(0, 0, 100, 100)), new Vector2(0, 100), new Vector2(200, 200));
            Assert.AreEqual(0f, p.x);
            Assert.AreEqual(0f, p.y); // top of world = top of minimap (y=0)
        }

        // ── MinimapPixelToWorld ─────────────────────────────────────────────

        [Test]
        public void MinimapPixelToWorld_NullMap_Zero()
        {
            var svc = new MinimapService(new FakeRegistry());
            var w = svc.MinimapPixelToWorld(null, Vector2.zero, new Vector2(200, 200));
            Assert.AreEqual(Vector2.zero, w);
        }

        [Test]
        public void MinimapPixelToWorld_ZeroSize_Zero()
        {
            var svc = new MinimapService(new FakeRegistry());
            var w = svc.MinimapPixelToWorld(MakeMap(1, new Rect(0, 0, 100, 100)), Vector2.zero, Vector2.zero);
            Assert.AreEqual(Vector2.zero, w);
        }

        [Test]
        public void MinimapPixelToWorld_Center()
        {
            var svc = new MinimapService(new FakeRegistry());
            var w = svc.MinimapPixelToWorld(MakeMap(1, new Rect(0, 0, 100, 100)), new Vector2(100, 100), new Vector2(200, 200));
            Assert.AreEqual(50f, w.x);
            Assert.AreEqual(50f, w.y);
        }

        [Test]
        public void MinimapPixelToWorld_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new MinimapService(new FakeRegistry(), host);
            svc.MinimapPixelToWorld(MakeMap(1, new Rect(0, 0, 100, 100)), new Vector2(100, 100), new Vector2(200, 200));
            Assert.AreEqual(1, host.MinimapToWorldCalls);
        }

        // ── MinimapNormalizedToWorld ────────────────────────────────────────

        [Test]
        public void MinimapNormalizedToWorld_Zero()
        {
            var svc = new MinimapService(new FakeRegistry());
            var w = svc.MinimapNormalizedToWorld(MakeMap(1, new Rect(10, 20, 100, 100)), new Vector2(0, 0));
            Assert.AreEqual(10f, w.x);
            Assert.AreEqual(120f, w.y); // y inverted → 20+100=120
        }

        [Test]
        public void MinimapNormalizedToWorld_NullMap_Zero()
        {
            var svc = new MinimapService(new FakeRegistry());
            var w = svc.MinimapNormalizedToWorld(null, new Vector2(0.5f, 0.5f));
            Assert.AreEqual(Vector2.zero, w);
        }

        // ── IsMissing / GetMissingSourceId ───────────────────────────────────

        [Test]
        public void IsMissing_NullMap_True()
        {
            var svc = new MinimapService(new FakeRegistry());
            Assert.IsTrue(svc.IsMissing(null));
        }

        [Test]
        public void IsMissing_NoRef_True()
        {
            var svc = new MinimapService(new FakeRegistry());
            var map = MakeMap(1, new Rect(0, 0, 100, 100));
            Assert.IsTrue(svc.IsMissing(map));
        }

        [Test]
        public void IsMissing_Registered_False()
        {
            var svc = new MinimapService(new FakeRegistry());
            var map = MakeMap(1, new Rect(0, 0, 100, 100));
            map.minimapRef = new MinimapRef { status = MinimapArtifactStatus.Registered };
            Assert.IsFalse(svc.IsMissing(map));
        }

        [Test]
        public void IsMissing_Missing_True()
        {
            var svc = new MinimapService(new FakeRegistry());
            var map = MakeMap(1, new Rect(0, 0, 100, 100));
            map.minimapRef = new MinimapRef { status = MinimapArtifactStatus.Missing };
            Assert.IsTrue(svc.IsMissing(map));
        }

        [Test]
        public void GetMissingSourceId_FromRef()
        {
            var svc = new MinimapService(new FakeRegistry());
            var map = MakeMap(1, new Rect(0, 0, 100, 100));
            map.minimapRef = new MinimapRef { sourceId = new SourceAssetId { category = "minimap", path = "abc" } };
            Assert.AreEqual("abc", svc.GetMissingSourceId(map)?.ToKey());
        }

        [Test]
        public void GetMissingSourceId_FromCatalog()
        {
            var svc = new MinimapService(new FakeRegistry());
            var map = MakeMap(1, new Rect(0, 0, 100, 100), sourceId: "catalogSrc");
            Assert.AreEqual("catalogSrc", svc.GetMissingSourceId(map)?.ToKey());
        }

        [Test]
        public void GetMissingSourceId_NullMap_Null()
        {
            var svc = new MinimapService(new FakeRegistry());
            Assert.IsNull(svc.GetMissingSourceId(null));
        }

        // ── No-host ─────────────────────────────────────────────────────────

        [Test]
        public void MinimapService_WithoutHost_DoesNotThrow()
        {
            var svc = new MinimapService(new FakeRegistry());
            var map = MakeMap(1, new Rect(0, 0, 100, 100));
            Assert.DoesNotThrow(() => svc.ResolveArtifact(map));
            Assert.DoesNotThrow(() => svc.WorldToMinimapNormalized(map, Vector2.zero));
            Assert.DoesNotThrow(() => svc.WorldToMinimapPixel(map, Vector2.zero, new Vector2(100, 100)));
            Assert.DoesNotThrow(() => svc.MinimapPixelToWorld(map, Vector2.zero, new Vector2(100, 100)));
            Assert.DoesNotThrow(() => svc.MinimapNormalizedToWorld(map, Vector2.zero));
        }
    }
}
