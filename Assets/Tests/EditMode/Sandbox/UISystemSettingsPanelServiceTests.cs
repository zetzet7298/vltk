// -----------------------------------------------------------------------------
// VLTK Mobile — UI System/Settings/Loading panel tests
// Verify null-safe defaults + basic state mutation for all 7 panel services.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class UISystemSettingsPanelServiceTests
    {
        // ─── BattleMapPanelService ──────────────────────────────────────────
        [Test]
        public void BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = BattleMapPanelService.BuildSnapshot(null, 1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.availableBattles);
        }

        [Test]
        public void GetByType_Empty_ForNull()
        {
            var rows = BattleMapPanelService.GetByType(null, BattleMapPanelService.BattleTypeSongJin);
            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void GetForLevel_Empty_ForNull()
        {
            var rows = BattleMapPanelService.GetForLevel(null, 50);
            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void GetBattleTypeName_NonEmpty()
        {
            Assert.AreEqual("Tống Kim", BattleMapPanelService.GetBattleTypeName(BattleMapPanelService.BattleTypeSongJin));
            Assert.AreEqual("Quốc Chiến", BattleMapPanelService.GetBattleTypeName(BattleMapPanelService.BattleTypeQuocChien));
            Assert.AreEqual("Công Thành", BattleMapPanelService.GetBattleTypeName(BattleMapPanelService.BattleTypeCongThanh));
            Assert.AreEqual("PvP", BattleMapPanelService.GetBattleTypeName(BattleMapPanelService.BattleTypePvP));
        }

        // ─── HuaShanPanelService ────────────────────────────────────────────
        [Test]
        public void HuaShan_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = HuaShanPanelService.BuildSnapshot(null, 1, 0);
            Assert.IsNotNull(snap);
        }

        [Test]
        public void HuaShan_GetCurrentRound_Null_ForNull()
        {
            var r = HuaShanPanelService.GetCurrentRound(null);
            Assert.IsNull(r);
        }

        [Test]
        public void HuaShan_TryRegister_False_ForNull()
        {
            Assert.IsFalse(HuaShanPanelService.TryRegister(null, 1));
        }

        // ─── VipPanelService ────────────────────────────────────────────────
        [Test]
        public void Vip_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = VipPanelService.BuildSnapshot(null, 1, 0);
            Assert.IsNotNull(snap);
        }

        [Test]
        public void Vip_GetCurrentVip_Zero_ForNull()
        {
            Assert.AreEqual(0, VipPanelService.GetCurrentVip(null, 1000));
        }

        [Test]
        public void Vip_GetNextVip_Zero_ForMax()
        {
            Assert.AreEqual(0, VipPanelService.GetNextVip(null, VipPanelService.MaxVipLevel));
        }

        [Test]
        public void Vip_ComputeRechargeToNext_Zero_ForNull()
        {
            Assert.AreEqual(0, VipPanelService.ComputeRechargeToNext(null, 0, 1000));
        }

        // ─── ReputationPanelService ─────────────────────────────────────────
        [Test]
        public void Reputation_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = ReputationPanelService.BuildSnapshot(null, 1);
            Assert.IsNotNull(snap);
        }

        [Test]
        public void Reputation_GetByFaction_Empty_ForNull()
        {
            var rows = ReputationPanelService.GetByFaction(null, 1);
            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void Reputation_GetCurrentTier_Empty_ForNull()
        {
            var tier = ReputationPanelService.GetCurrentTier(null, 1, 1000);
            Assert.AreEqual(string.Empty, tier);
        }

        // ─── SettingsPanelService ───────────────────────────────────────────
        [Test]
        public void Settings_BuildSnapshot_DoesNotThrow()
        {
            var snap = SettingsPanelService.BuildSnapshot();
            Assert.IsNotNull(snap);
        }

        [Test]
        public void Settings_GetBool_DefaultWhenUnset()
        {
            SettingsPanelService.Reset();
            Assert.IsFalse(SettingsPanelService.GetBool("nonexistent_key"));
        }

        [Test]
        public void Settings_SetBool_StoresValue()
        {
            SettingsPanelService.Reset();
            Assert.IsTrue(SettingsPanelService.SetBool("test_bool", true));
            Assert.IsTrue(SettingsPanelService.GetBool("test_bool"));
        }

        [Test]
        public void Settings_GetInt_DefaultWhenUnset()
        {
            SettingsPanelService.Reset();
            Assert.AreEqual(0, SettingsPanelService.GetInt("nonexistent_int"));
        }

        [Test]
        public void Settings_SetInt_StoresValue()
        {
            SettingsPanelService.Reset();
            Assert.IsTrue(SettingsPanelService.SetInt("test_int", 42));
            Assert.AreEqual(42, SettingsPanelService.GetInt("test_int"));
        }

        [Test]
        public void Settings_GetFloat_DefaultWhenUnset()
        {
            SettingsPanelService.Reset();
            Assert.AreEqual(0f, SettingsPanelService.GetFloat("nonexistent_float"));
        }

        [Test]
        public void Settings_SetFloat_StoresValue()
        {
            SettingsPanelService.Reset();
            Assert.IsTrue(SettingsPanelService.SetFloat("test_float", 3.14f));
            Assert.AreEqual(3.14f, SettingsPanelService.GetFloat("test_float"), 0.01f);
        }

        [Test]
        public void Settings_GetString_DefaultWhenUnset()
        {
            SettingsPanelService.Reset();
            Assert.AreEqual("default", SettingsPanelService.GetString("nonexistent_string", "default"));
        }

        [Test]
        public void Settings_SetString_StoresValue()
        {
            SettingsPanelService.Reset();
            Assert.IsTrue(SettingsPanelService.SetString("test_string", "hello"));
            Assert.AreEqual("hello", SettingsPanelService.GetString("test_string"));
        }

        [Test]
        public void Settings_Reset_ClearsAll()
        {
            SettingsPanelService.SetBool("x", true);
            SettingsPanelService.Reset();
            Assert.IsFalse(SettingsPanelService.GetBool("x"));
        }

        // ─── SystemMenuPanelService ─────────────────────────────────────────
        [Test]
        public void SystemMenu_BuildSnapshot_DoesNotThrow()
        {
            var snap = SystemMenuPanelService.BuildSnapshot();
            Assert.IsNotNull(snap);
            Assert.Greater(snap.rows.Count, 0);
        }

        [Test]
        public void SystemMenu_GetByName_NonNull()
        {
            var menu = SystemMenuPanelService.GetByName("Tùy chọn");
            Assert.IsTrue(menu.HasValue);
            Assert.AreEqual(SystemMenuPanelService.MenuOptions, menu.Value.menuId);
        }

        [Test]
        public void SystemMenu_BuildSnapshot_MatchesPcE6641da3Buttons()
        {
            var snap = SystemMenuPanelService.BuildSnapshot();
            Assert.AreEqual(5, snap.rows.Count, "PC e6641da3.ini exposes exactly ExitGame/GameHelp/Options/OffLine/ContiumeGame.");
            Assert.AreEqual("Thoát game", snap.rows[0].name);
            Assert.AreEqual("Trợ giúp", snap.rows[1].name);
            Assert.AreEqual("Tùy chọn", snap.rows[2].name);
            Assert.AreEqual("Treo máy offline", snap.rows[3].name);
            Assert.AreEqual("Tiếp tục game", snap.rows[4].name);
            Assert.IsTrue(snap.rows[0].requiresConfirm);
            Assert.IsTrue(snap.rows[3].requiresConfirm);
            Assert.IsTrue(SystemMenuPanelService.DisabledPcSystemMenuButtons.ContainsKey("CloseGame"));
            Assert.IsTrue(SystemMenuPanelService.DisabledPcSystemMenuButtons.ContainsKey("GameTask"));
        }

        [Test]
        public void SystemMenu_GetEnabled_AtLeastOne()
        {
            var list = SystemMenuPanelService.GetEnabled();
            Assert.IsNotNull(list);
            Assert.Greater(list.Count, 0);
        }

        // ─── LoadingScreenPanelService ──────────────────────────────────────
        [Test]
        public void Loading_BuildSnapshot_DoesNotThrow()
        {
            var snap = LoadingScreenPanelService.BuildSnapshot();
            Assert.IsNotNull(snap);
            Assert.AreEqual(LoadingScreenPanelService.TotalSteps, snap.totalSteps);
        }

        [Test]
        public void Loading_GetStep_NullForInvalid()
        {
            Assert.IsNull(LoadingScreenPanelService.GetStep(-1));
            Assert.IsNull(LoadingScreenPanelService.GetStep(9999));
        }

        [Test]
        public void Loading_SetStepStatus_Updates()
        {
            LoadingScreenPanelService.Reset();
            Assert.IsTrue(LoadingScreenPanelService.SetStepStatus(0, LoadingScreenPanelService.StatusLoading));
            var step = LoadingScreenPanelService.GetStep(0);
            Assert.IsTrue(step.HasValue);
            Assert.AreEqual(LoadingScreenPanelService.StatusLoading, step.Value.status);
        }

        [Test]
        public void Loading_GetTotalPercent_Zero_Initially()
        {
            LoadingScreenPanelService.Reset();
            Assert.AreEqual(0f, LoadingScreenPanelService.GetTotalPercent());
        }

        [Test]
        public void Loading_Reset_Clears()
        {
            LoadingScreenPanelService.SetStepStatus(0, LoadingScreenPanelService.StatusDone);
            LoadingScreenPanelService.Reset();
            var step = LoadingScreenPanelService.GetStep(0);
            Assert.IsTrue(step.HasValue);
            Assert.AreEqual(LoadingScreenPanelService.StatusPending, step.Value.status);
        }
    }
}
