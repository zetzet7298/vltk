// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorRuntimeWiringTests
// Ticket 43 boot smoke: chống tái phạm dead-wiring (council FAIL) — sau
// SurvivorGameDirector.OnInit, mọi feature P2 phải được nối vào game loop thật:
//   - Player.Cast (SkillCastRuntime) khác null
//   - Overlay.SkillService (SkillChoiceService) khác null — levelup không còn
//     chạy P1 flat-card path
//   - Supply (SurvivorSupplyMgr) + SupplyBar khác null — heal/bomb/magnet/full-clear
//   - BossSkillPool không rỗng (catalog boss/npc pool thật)
//   - HUD.WaveIndexSource != null — banner wave số thật
//   - SettingsPanel != null — settings boot được (persist + language)
//   - Pause != null (SurvivorPause chung)
// + Catalog smoke: đọc StreamingAssets thật → ≥ 1000 skill, player pool ≥ 400
//   (spec D2: ~452), supply defs ≥ 1 heal + ≥ 1 bomb (nếu data PC có tag).
//
// Scene thật (EditMode — AddComponent chạy Awake ngay): tạo director → boot →
// assert → destroy + reset Time.timeScale (fail-safe cho suite khác).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorRuntimeWiringTests
    {
        // EditMode: AddComponent KHÔNG chạy Awake (Unity 6) → boot qua OnInit
        // protected — test subclass gọi trực tiếp (đúng contract boot thật).
        private sealed class TestDirector : SurvivorGameDirector
        {
            public void Boot() => OnInit();
        }

        private GameObject _directorGo;
        private TestDirector _director;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            _directorGo = new GameObject("director_test");
            _director = _directorGo.AddComponent<TestDirector>();
            _director.Boot(); // OnInit — boot wiring thật (không skip bước nào)
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (_directorGo != null) Object.DestroyImmediate(_directorGo);
            // dọn singleton/UI khác boot tạo — tránh rò rỉ sang test kế
            foreach (var o in Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (o is OverlayPanel || o is SurvivorHud || o is SupplyBar ||
                    o is SurvivorAudioSettingsPanel || o is SurvivorAudioMgr ||
                    o is UnityEngine.EventSystems.EventSystem)
                    Object.DestroyImmediate(o.gameObject);
            }
        }

        // ------------------------------------------------------------------
        // boot smoke (chặn tái phạm dead-wiring)
        // ------------------------------------------------------------------

        [Test]
        public void OnInit_Wires_AllP2Systems()
        {
            Assert.IsNotNull(_director.Pause, "SurvivorPause chung phải boot");
            Assert.IsNotNull(_director.Player, "player spawn");
            Assert.IsNotNull(_director.Player.Cast, "SkillCastRuntime gán Player.Cast (roster cast thật)");
            Assert.IsNotNull(_director.Overlay, "overlay boot");
            Assert.IsNotNull(_director.Overlay.SkillService, "SkillChoiceService gán overlay — levelup skill thật");
            Assert.IsNotNull(_director.Supply, "SurvivorSupplyMgr boot");
            Assert.IsNotNull(_director.SupplyBar, "SupplyBar boot — slot UI hiện");
            Assert.IsNotNull(_director.SettingsPanel, "settings panel boot (persist + language)");
            Assert.Greater(_director.BossSkillPool.Count, 0, "BossSkillPool fill từ catalog boss/npc");
            // EditMode: Awake không chạy → Instance static null; HUD object tồn tại thật
            var hud = SurvivorHud.Instance;
            if (hud == null) hud = Object.FindAnyObjectByType<SurvivorHud>();
            Assert.IsNotNull(hud, "HUD boot (OverlayPanel.Build hook)");
            Assert.IsNotNull(hud.WaveIndexSource, "WaveIndexSource wire — banner wave số thật");
        }

        [Test]
        public void OnInit_LevelUp_Flow_UsesSkillService_NotLegacy()
        {
            // service path hoạt động: Request → event có card (pool thật không rỗng)
            var svc = _director.Overlay.SkillService;
            Assert.IsNotNull(svc);
            svc.Tick(0f);
            Assert.IsTrue(svc.Request(1, SkillChoiceMode.LevelUp), "levelup request trigger ngay (rảnh)");
            var ev = svc.Current(1);
            Assert.IsNotNull(ev, "event mở modal");
            Assert.AreEqual(3, ev.Cards.Length, "levelup 3 card");
            Assert.IsNotNull(ev.Cards[0].Def, "card là SkillDef thật (không phải P1 flat-card)");
            Assert.AreEqual(1, _director.Pause.Count, "modal mở → pause acquire (scope LevelUp/CardChoice)");
        }

        // ------------------------------------------------------------------
        // catalog smoke — StreamingAssets thật (repo committed)
        // ------------------------------------------------------------------

        [Test]
        public void Catalog_FromStreamingAssets_RealSizes()
        {
            var catalog = SurvivorSkillCatalogService.LoadFromStreamingAssets();
            Assert.GreaterOrEqual(catalog.Skills.Count, 1000, "PcSkills.txt ~1216 row");
            Assert.GreaterOrEqual(catalog.PlayerPoolCount, 400, "player pool ~452 (spec D2)");
            Assert.Greater(catalog.MissileRows, 0, "missles.txt ~441 row");
            var boss = SurvivorSkillCatalogService.Defs(catalog, SurvivorSkillPool.BossNpc);
            Assert.Greater(boss.Count, 0, "boss/npc pool không rỗng");
            var supply = SurvivorSkillCatalogService.SupplyDefs(catalog);
            Assert.GreaterOrEqual(supply.Count, 1, "có ≥1 supply skill (heal/bomb tag)");
        }
    }
}
