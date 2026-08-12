// -----------------------------------------------------------------------------
// VLTK Mobile — AreaScriptService host dispatch tests
// PC source: settings/areas.txt — 9 vùng bản đồ GBK (Đông Bắc, Đại Lý, ...).
// Verifies IAreaScriptServiceHost receives expected events for load / query /
// category name / area name / total script count.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class AreaScriptServiceHostServiceTests
    {
        private sealed class FakeHost : IAreaScriptServiceHost
        {
            public int RegistryAttachedCalls;
            public int LastRegistryAreaCount;
            public int RegistryEmptyCalls;

            public int ResolvedCalls;
            public int LastResolvedAreaId;
            public string LastResolvedNameRaw;
            public int LastResolvedMapId;
            public int LastResolvedCategory;

            public int ByMapQueriedCalls;
            public int LastByMapId;
            public int LastByMapResultCount;

            public int ByCategoryQueriedCalls;
            public int LastByCategoryCategory;
            public int LastByCategoryResultCount;
            public string LastByCategoryNameVi;

            public int TotalScriptCountQueriedCalls;
            public int LastTotalScriptCount;

            public int CategoryNameResolvedCalls;
            public int LastCategoryNameCategory;
            public string LastCategoryName;

            public int AreaNameResolvedCalls;
            public int LastAreaNameAreaId;
            public string LastAreaNameRaw;
            public bool LastAreaNameFound;

            public int UIShowCalls;
            public int LastUIShowAreaId;
            public string LastUIShowNameRaw;
            public int LastUIShowMapId;

            public int LogCalls;
            public int LastLogAreaId;
            public string LastLogEventType;
            public string LastLogDetail;

            public int SFXCalls;
            public int LastSFXAreaId;
            public string LastSFXAction;

            public int SaveCalls;
            public int LastSaveAreaId;
            public int LastSaveCategory;
            public int LastSaveMapId;

            public void OnAreaRegistryAttached(int areaCount)
            {
                RegistryAttachedCalls++;
                LastRegistryAreaCount = areaCount;
            }
            public void OnAreaRegistryEmpty() => RegistryEmptyCalls++;
            public void OnAreaResolved(int areaId, string areaNameRaw, int mapId, int category)
            {
                ResolvedCalls++;
                LastResolvedAreaId = areaId;
                LastResolvedNameRaw = areaNameRaw;
                LastResolvedMapId = mapId;
                LastResolvedCategory = category;
            }
            public void OnAreasByMapQueried(int mapId, int resultCount)
            {
                ByMapQueriedCalls++;
                LastByMapId = mapId;
                LastByMapResultCount = resultCount;
            }
            public void OnAreasByCategoryQueried(int category, int resultCount, string categoryNameVi)
            {
                ByCategoryQueriedCalls++;
                LastByCategoryCategory = category;
                LastByCategoryResultCount = resultCount;
                LastByCategoryNameVi = categoryNameVi;
            }
            public void OnTotalScriptCountQueried(int totalScriptCount)
            {
                TotalScriptCountQueriedCalls++;
                LastTotalScriptCount = totalScriptCount;
            }
            public void OnCategoryNameResolved(int category, string categoryNameVi)
            {
                CategoryNameResolvedCalls++;
                LastCategoryNameCategory = category;
                LastCategoryName = categoryNameVi;
            }
            public void OnAreaNameResolved(int areaId, string areaNameRaw, bool found)
            {
                AreaNameResolvedCalls++;
                LastAreaNameAreaId = areaId;
                LastAreaNameRaw = areaNameRaw;
                LastAreaNameFound = found;
            }
            public void ShowAreaUI(int areaId, string areaNameRaw, int mapId)
            {
                UIShowCalls++;
                LastUIShowAreaId = areaId;
                LastUIShowNameRaw = areaNameRaw;
                LastUIShowMapId = mapId;
            }
            public void LogAreaEvent(string eventType, int areaId, string detailVi)
            {
                LogCalls++;
                LastLogEventType = eventType;
                LastLogAreaId = areaId;
                LastLogDetail = detailVi;
            }
            public void PlayAreaSFX(string action, int areaId)
            {
                SFXCalls++;
                LastSFXAction = action;
                LastSFXAreaId = areaId;
            }
            public void SaveAreaState(int areaId, int category, int mapId)
            {
                SaveCalls++;
                LastSaveAreaId = areaId;
                LastSaveCategory = category;
                LastSaveMapId = mapId;
            }
        }

        private static (PcAreaScriptRegistry reg, PcAreaScriptEntry e1, PcAreaScriptEntry e2) MakeRegistry()
        {
            var reg = new PcAreaScriptRegistry();
            var e1 = new PcAreaScriptEntry
            {
                areaId = 1, areaNameRaw = "Đại Lý", mapId = 200,
                scriptFileName = "dali.lua", scriptCount = 5, category = 0,
                descriptionRaw = "Thành phố Đại Lý",
            };
            var e2 = new PcAreaScriptEntry
            {
                areaId = 2, areaNameRaw = "Phượng Tường", mapId = 201,
                scriptFileName = "phuongtuong.lua", scriptCount = 3, category = 4,
                descriptionRaw = "Thành phố Phượng Tường",
            };
            reg.Register(e1);
            reg.Register(e2);
            return (reg, e1, e2);
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────
        [Test]
        public void Ctor_Default_Empty()
        {
            var svc = new AreaScriptService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachHost_NullSafe()
        {
            var svc = new AreaScriptService();
            Assert.DoesNotThrow(() => svc.AttachHost(null));
        }

        // ── RegisterRegistry dispatch ──────────────────────────────────────
        [Test]
        public void RegisterRegistry_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new AreaScriptService();
            svc.AttachHost(host);
            var (reg, _, _) = MakeRegistry();
            int baseline = host.RegistryAttachedCalls;
            svc.RegisterRegistry(reg);
            Assert.AreEqual(baseline + 1, host.RegistryAttachedCalls);
            Assert.AreEqual(2, host.LastRegistryAreaCount);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual("load", host.LastSFXAction);
        }

        [Test]
        public void RegisterRegistry_Empty_DispatchesEmpty()
        {
            var host = new FakeHost();
            var svc = new AreaScriptService();
            svc.AttachHost(host);
            svc.RegisterRegistry(null);
            Assert.AreEqual(1, host.RegistryEmptyCalls);
            Assert.AreEqual(0, host.RegistryAttachedCalls);
        }

        // ── GetArea dispatch ───────────────────────────────────────────────
        [Test]
        public void GetArea_Found_DispatchesResolved()
        {
            var host = new FakeHost();
            var (reg, e1, _) = MakeRegistry();
            var svc = new AreaScriptService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            var e = svc.GetArea(1);
            Assert.IsNotNull(e);
            Assert.AreEqual(baseline + 1, host.ResolvedCalls);
            Assert.AreEqual(1, host.LastResolvedAreaId);
            Assert.AreEqual("Đại Lý", host.LastResolvedNameRaw);
            Assert.AreEqual(200, host.LastResolvedMapId);
            Assert.AreEqual(0, host.LastResolvedCategory);
        }

        [Test]
        public void GetArea_Missing_LogsButNoResolve()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AreaScriptService(reg);
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            int baselineLog = host.LogCalls;
            var e = svc.GetArea(9999);
            Assert.IsNull(e);
            Assert.AreEqual(baseline, host.ResolvedCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("query_missing", host.LastLogEventType);
        }

        // ── GetByMap dispatch ──────────────────────────────────────────────
        [Test]
        public void GetByMap_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AreaScriptService(reg);
            svc.AttachHost(host);
            var list = svc.GetByMap(200);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, host.ByMapQueriedCalls);
            Assert.AreEqual(200, host.LastByMapId);
            Assert.AreEqual(1, host.LastByMapResultCount);
        }

        [Test]
        public void GetByMap_NoRegistry_DispatchesZero()
        {
            var host = new FakeHost();
            var svc = new AreaScriptService();
            svc.AttachHost(host);
            var list = svc.GetByMap(200);
            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(1, host.ByMapQueriedCalls);
            Assert.AreEqual(0, host.LastByMapResultCount);
        }

        // ── GetByCategory dispatch ─────────────────────────────────────────
        [Test]
        public void GetByCategory_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AreaScriptService(reg);
            svc.AttachHost(host);
            var list = svc.GetByCategory(4);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, host.ByCategoryQueriedCalls);
            Assert.AreEqual(4, host.LastByCategoryCategory);
            Assert.AreEqual(1, host.LastByCategoryResultCount);
            Assert.AreEqual("Thành Phố Lớn", host.LastByCategoryNameVi);
        }

        [Test]
        public void GetAreasInCategory_Alias_DispatchesHostCount()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AreaScriptService(reg);
            svc.AttachHost(host);
            var list = svc.GetAreasInCategory(0);
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, host.ByCategoryQueriedCalls);
        }

        // ── GetTotalScriptCount dispatch ───────────────────────────────────
        [Test]
        public void GetTotalScriptCount_DispatchesHost()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AreaScriptService(reg);
            svc.AttachHost(host);
            int n = svc.GetTotalScriptCount();
            Assert.GreaterOrEqual(n, 0);
            Assert.AreEqual(1, host.TotalScriptCountQueriedCalls);
            Assert.AreEqual(n, host.LastTotalScriptCount);
        }

        [Test]
        public void GetTotalScriptCount_NoRegistry_DispatchesZero()
        {
            var host = new FakeHost();
            var svc = new AreaScriptService();
            svc.AttachHost(host);
            int n = svc.GetTotalScriptCount();
            Assert.AreEqual(0, n);
            Assert.AreEqual(1, host.TotalScriptCountQueriedCalls);
            Assert.AreEqual(0, host.LastTotalScriptCount);
        }

        // ── GetCategoryName dispatch ───────────────────────────────────────
        [Test]
        public void GetCategoryName_AllKnown()
        {
            var host = new FakeHost();
            var svc = new AreaScriptService();
            svc.AttachHost(host);
            int baseline = host.CategoryNameResolvedCalls;
            Assert.AreEqual("Khu Vực Bản Đồ", svc.GetCategoryName(0));
            Assert.AreEqual("Nhiệm Vụ Môn Phái", svc.GetCategoryName(1));
            Assert.AreEqual("Thị Trấn", svc.GetCategoryName(2));
            Assert.AreEqual("PvP", svc.GetCategoryName(3));
            Assert.AreEqual("Thành Phố Lớn", svc.GetCategoryName(4));
            Assert.AreEqual("Khác (99)", svc.GetCategoryName(99));
            Assert.AreEqual(baseline + 6, host.CategoryNameResolvedCalls);
        }

        // ── GetAreaName dispatch ───────────────────────────────────────────
        [Test]
        public void GetAreaName_Found_DispatchesAll()
        {
            var host = new FakeHost();
            var (reg, e1, _) = MakeRegistry();
            var svc = new AreaScriptService(reg);
            svc.AttachHost(host);
            int baselineUI = host.UIShowCalls;
            int baselineLog = host.LogCalls;
            int baselineSFX = host.SFXCalls;
            int baselineSave = host.SaveCalls;
            var name = svc.GetAreaName(1);
            Assert.AreEqual("Đại Lý", name);
            Assert.IsTrue(host.LastAreaNameFound);
            Assert.AreEqual("Đại Lý", host.LastAreaNameRaw);
            Assert.AreEqual(1, host.LastAreaNameAreaId);
            Assert.AreEqual(baselineUI + 1, host.UIShowCalls);
            Assert.AreEqual(200, host.LastUIShowMapId);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("area_named", host.LastLogEventType);
            Assert.AreEqual(baselineSFX + 1, host.SFXCalls);
            Assert.AreEqual("open", host.LastSFXAction);
            Assert.AreEqual(baselineSave + 1, host.SaveCalls);
            Assert.AreEqual(0, host.LastSaveCategory);
        }

        [Test]
        public void GetAreaName_Missing_DispatchesNotFound()
        {
            var host = new FakeHost();
            var (reg, _, _) = MakeRegistry();
            var svc = new AreaScriptService(reg);
            svc.AttachHost(host);
            var name = svc.GetAreaName(9999);
            Assert.IsNull(name);
            Assert.IsFalse(host.LastAreaNameFound);
            Assert.AreEqual(0, host.UIShowCalls);
        }

        // ── No-host path is silent ─────────────────────────────────────────
        [Test]
        public void NoHost_OperationsDoNotThrow()
        {
            var svc = new AreaScriptService();
            Assert.DoesNotThrow(() => svc.RegisterRegistry(null));
            Assert.DoesNotThrow(() => svc.GetArea(1));
            Assert.DoesNotThrow(() => svc.GetByCategory(0));
            Assert.DoesNotThrow(() => svc.GetByMap(200));
            Assert.DoesNotThrow(() => svc.GetAreasInCategory(0));
            Assert.DoesNotThrow(() => svc.GetTotalScriptCount());
            Assert.DoesNotThrow(() => svc.GetCategoryName(0));
            Assert.DoesNotThrow(() => svc.GetAreaName(1));
        }
    }
}
