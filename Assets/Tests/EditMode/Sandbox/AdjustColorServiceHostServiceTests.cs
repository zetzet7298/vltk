// -----------------------------------------------------------------------------
// VLTK Mobile — AdjustColorService host dispatch tests
// PC source: settings/adjustcolor.txt — Cấu hình điều chỉnh màu sắc (R/G/B/A).
// Verifies IAdjustColorServiceHost receives expected events for load / query /
// apply / preview operations.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class AdjustColorServiceHostServiceTests
    {
        private sealed class FakeHost : IAdjustColorServiceHost
        {
            public int RegistryAttachedCalls;
            public int LastRegistryColorCount;
            public int RegistryEmptyCalls;

            public int ResolvedCalls;
            public int LastResolvedSettingId;
            public int LastResolvedR, LastResolvedG, LastResolvedB, LastResolvedA;
            public string LastResolvedDescription;

            public int AllQueriedCalls;
            public int LastAllResultCount;

            public int AppliedCalls;
            public int PreviewedCalls;
            public int LastApplySettingId;
            public int LastApplyR, LastApplyG, LastApplyB, LastApplyA;

            public int UIShowCalls;
            public int LastUISettingId;
            public int LastUIR, LastUIG, LastUIB, LastUIA;

            public int LogCalls;
            public int LastLogSettingId;
            public string LastLogEventType;
            public string LastLogDetail;

            public int SFXCalls;
            public int LastSFXSettingId;
            public string LastSFXAction;

            public int SaveCalls;
            public int LastSaveSettingId;
            public int LastSaveR, LastSaveG, LastSaveB, LastSaveA;

            public void OnColorRegistryAttached(int colorCount)
            {
                RegistryAttachedCalls++;
                LastRegistryColorCount = colorCount;
            }
            public void OnColorRegistryEmpty() => RegistryEmptyCalls++;
            public void OnColorResolved(int settingId, int r, int g, int b, int a, string descriptionVi)
            {
                ResolvedCalls++;
                LastResolvedSettingId = settingId;
                LastResolvedR = r; LastResolvedG = g; LastResolvedB = b; LastResolvedA = a;
                LastResolvedDescription = descriptionVi;
            }
            public void OnAllColorsQueried(int resultCount)
            {
                AllQueriedCalls++;
                LastAllResultCount = resultCount;
            }
            public void OnColorApplied(int settingId, int r, int g, int b, int a)
            {
                AppliedCalls++;
                LastApplySettingId = settingId;
                LastApplyR = r; LastApplyG = g; LastApplyB = b; LastApplyA = a;
            }
            public void OnColorPreviewed(int settingId, int r, int g, int b, int a)
            {
                PreviewedCalls++;
                LastApplySettingId = settingId;
                LastApplyR = r; LastApplyG = g; LastApplyB = b; LastApplyA = a;
            }
            public void ShowColorUI(int settingId, int r, int g, int b, int a)
            {
                UIShowCalls++;
                LastUISettingId = settingId;
                LastUIR = r; LastUIG = g; LastUIB = b; LastUIA = a;
            }
            public void LogColorEvent(string eventType, int settingId, string detailVi)
            {
                LogCalls++;
                LastLogEventType = eventType;
                LastLogSettingId = settingId;
                LastLogDetail = detailVi;
            }
            public void PlayColorSFX(string action, int settingId)
            {
                SFXCalls++;
                LastSFXAction = action;
                LastSFXSettingId = settingId;
            }
            public void SaveColorState(int settingId, int r, int g, int b, int a)
            {
                SaveCalls++;
                LastSaveSettingId = settingId;
                LastSaveR = r; LastSaveG = g; LastSaveB = b; LastSaveA = a;
            }
        }

        private static (PcAdjustColorRegistry reg, PcAdjustColorEntry e1, PcAdjustColorEntry e2) MakeRegistry()
        {
            var reg = new PcAdjustColorRegistry();
            var e1 = new PcAdjustColorEntry
            {
                settingId = 1, r = 255, g = 128, b = 64, a = 255,
                description = "Màu chữ thường",
            };
            var e2 = new PcAdjustColorEntry
            {
                settingId = 2, r = 64, g = 200, b = 64, a = 255,
                description = "Màu xanh hệ thống",
            };
            reg.Register(e1);
            reg.Register(e2);
            return (reg, e1, e2);
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────
        [Test]
        public void Ctor_Default_Empty()
        {
            var svc = new AdjustColorService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachHost_NullSafe()
        {
            var svc = new AdjustColorService();
            Assert.DoesNotThrow(() => svc.AttachHost(null));
        }

        // ── RegisterRegistry dispatch ──────────────────────────────────────
        [Test]
        public void RegisterRegistry_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new AdjustColorService();
            svc.AttachHost(host);
            var (reg, _, _) = MakeRegistry();
            int baseline = host.RegistryAttachedCalls;
            svc.RegisterRegistry(reg);
            Assert.AreEqual(baseline + 1, host.RegistryAttachedCalls);
            Assert.AreEqual(2, host.LastRegistryColorCount);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual("load", host.LastSFXAction);
        }

        [Test]
        public void RegisterRegistry_Empty_DispatchesEmpty()
        {
            var host = new FakeHost();
            var svc = new AdjustColorService();
            svc.AttachHost(host);
            svc.RegisterRegistry(null);
            Assert.AreEqual(1, host.RegistryEmptyCalls);
            Assert.AreEqual(0, host.RegistryAttachedCalls);
        }

        // ── GetColor dispatch ───────────────────────────────────────────────
        [Test]
        public void GetColor_Found_DispatchesResolved()
        {
            var host = new FakeHost();
            var (reg, e1, _) = MakeRegistry();
            var svc = new AdjustColorService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            var c = svc.GetColor(1);
            Assert.IsNotNull(c);
            Assert.AreEqual(baseline + 1, host.ResolvedCalls);
            Assert.AreEqual(1, host.LastResolvedSettingId);
            Assert.AreEqual(255, host.LastResolvedR);
            Assert.AreEqual(128, host.LastResolvedG);
            Assert.AreEqual(64, host.LastResolvedB);
            Assert.AreEqual(255, host.LastResolvedA);
            Assert.AreEqual("Màu chữ thường", host.LastResolvedDescription);
        }

        [Test]
        public void GetColor_Missing_LogsButNoResolve()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AdjustColorService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            int baselineLog = host.LogCalls;
            var c = svc.GetColor(9999);
            Assert.IsNull(c);
            Assert.AreEqual(baseline, host.ResolvedCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("query_missing", host.LastLogEventType);
        }

        // ── All dispatch ───────────────────────────────────────────────────
        [Test]
        public void All_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AdjustColorService(reg);
            svc.AttachHost(host);
            var list = svc.All;
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, host.AllQueriedCalls);
            Assert.AreEqual(2, host.LastAllResultCount);
        }

        [Test]
        public void All_NoRegistry_DispatchesZero()
        {
            var host = new FakeHost();
            var svc = new AdjustColorService();
            svc.AttachHost(host);
            var list = svc.All;
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(1, host.AllQueriedCalls);
            Assert.AreEqual(0, host.LastAllResultCount);
        }

        // ── ApplyColor dispatch ────────────────────────────────────────────
        [Test]
        public void ApplyColor_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AdjustColorService(reg);
            svc.AttachHost(host);
            int baselineUI = host.UIShowCalls;
            int baselineLog = host.LogCalls;
            svc.ApplyColor(1);
            Assert.AreEqual(1, host.AppliedCalls);
            Assert.AreEqual(1, host.LastApplySettingId);
            Assert.AreEqual(255, host.LastApplyR);
            Assert.AreEqual(baselineUI + 1, host.UIShowCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("apply", host.LastLogEventType);
            Assert.AreEqual("apply", host.LastSFXAction);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void ApplyColor_Unknown_NoDispatch()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AdjustColorService(reg);
            svc.AttachHost(host);
            int baseline = host.AppliedCalls;
            svc.ApplyColor(9999);
            Assert.AreEqual(baseline, host.AppliedCalls);
        }

        // ── PreviewColor dispatch ──────────────────────────────────────────
        [Test]
        public void PreviewColor_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AdjustColorService(reg);
            svc.AttachHost(host);
            int baselineUI = host.UIShowCalls;
            int baselineLog = host.LogCalls;
            int baselineSFX = host.SFXCalls;
            svc.PreviewColor(2);
            Assert.AreEqual(1, host.PreviewedCalls);
            Assert.AreEqual(2, host.LastApplySettingId);
            Assert.AreEqual(64, host.LastApplyR);
            Assert.AreEqual(200, host.LastApplyG);
            Assert.AreEqual(baselineUI + 1, host.UIShowCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("preview", host.LastLogEventType);
            Assert.AreEqual(baselineSFX + 1, host.SFXCalls);
            Assert.AreEqual("preview", host.LastSFXAction);
        }

        [Test]
        public void PreviewColor_Unknown_NoDispatch()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AdjustColorService(reg);
            svc.AttachHost(host);
            int baseline = host.PreviewedCalls;
            svc.PreviewColor(9999);
            Assert.AreEqual(baseline, host.PreviewedCalls);
        }

        // ── No-host path is silent ─────────────────────────────────────────
        [Test]
        public void NoHost_OperationsDoNotThrow()
        {
            var svc = new AdjustColorService();
            Assert.DoesNotThrow(() => svc.RegisterRegistry(null));
            Assert.DoesNotThrow(() => svc.GetColor(1));
            var _ = svc.All;
            Assert.DoesNotThrow(() => svc.ApplyColor(1));
            Assert.DoesNotThrow(() => svc.PreviewColor(1));
        }
    }
}
