// -----------------------------------------------------------------------------
// VLTK Mobile — JX Võ Công / skill panel EditMode tests
// Port proof for KuiSkillVN.cpp + KuiSkilldescVN.cpp. Category: HudJxCocos.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxSkillPanelTests
    {
        private static JxSkillPanelSkill Skill(
            int id = 122,
            int level = 20,
            JxSkillLRInfo lr = JxSkillLRInfo.BothSkill,
            JxSkillStyle style = JxSkillStyle.Missiles)
        {
            return new JxSkillPanelSkill
            {
                SkillId = id,
                Genre = (int)JxSkillGenre.Fight,
                Level = level,
                AddPoint = 0,
                EnChance = 0,
                MaxLevel = 30,
                Name = "Phi Long Tại Thiên",
                IconPath = "\\spr\\skill\\philong.spr",
                CurrentDescription = "Mô tả hiện tại",
                NextDescription = "Mô tả cấp sau",
                SkillDescription = "Mô tả khác",
                Style = style,
                LRInfo = lr,
                Series = 4,
                WeaponLimit = -1,
            };
        }

        [Test]
        public void Constants_MatchKuiSkillSource()
        {
            Assert.AreEqual(50, JxSkillPanelState.FightSkillCount);
            Assert.AreEqual(25, JxSkillPanelState.FightSkillCountPerPage);
            Assert.AreEqual(10, JxSkillPanelState.GridColumns);
            Assert.AreEqual(5, JxSkillPanelState.GridRows);
            Assert.AreEqual(37f, JxSkillPanelState.SlotWidth);
            Assert.AreEqual(48f, JxSkillPanelState.SlotHeight);
            Assert.AreEqual(9f, JxSkillPanelState.FirstSlotX);
            Assert.AreEqual(75f, JxSkillPanelState.FirstSlotOffsetY);
            Assert.AreEqual(33f, JxSkillPanelState.IconScaledWidth);
            Assert.AreEqual(210f, JxSkillPanelState.SkillInfoLabelX);
            Assert.AreEqual(40f, JxSkillPanelState.SkillInfoLabelY);
        }

        [Test]
        public void SpritePaths_MatchSource()
        {
            Assert.AreEqual("ui/skill/skillbox.spr", JxSkillPanelState.BackgroundSprite);
            Assert.AreEqual("ui/item/btn_close_big.spr", JxSkillPanelState.CloseButtonSprite);
            Assert.AreEqual("sel_mask.png", JxSkillPanelState.SelectionMaskSprite);
        }

        [Test]
        public void CreateEmptySlot_MapsTenColumnsByFiveRows()
        {
            var s0 = JxSkillPanelState.CreateEmptySlot(0, 320);
            Assert.AreEqual(new Vector2(9, 245), s0.IconPosition);
            Assert.AreEqual(new Vector2(8, 229), s0.PointLabelPosition);
            Assert.AreEqual(new Rect(9, 245, 37, 48), s0.Rect);

            var s9 = JxSkillPanelState.CreateEmptySlot(9, 320);
            Assert.AreEqual(new Vector2(342, 245), s9.IconPosition);

            var s10 = JxSkillPanelState.CreateEmptySlot(10, 320);
            Assert.AreEqual(new Vector2(9, 197), s10.IconPosition);

            var s49 = JxSkillPanelState.CreateEmptySlot(49, 320);
            Assert.AreEqual(new Vector2(342, 53), s49.IconPosition);
        }

        [Test]
        public void SetFightSkills_PopulatesVisibleSlots_AndCapsAt50()
        {
            var state = new JxSkillPanelState();
            var many = new JxSkillPanelSkill[55];
            for (int i = 0; i < many.Length; i++) many[i] = Skill(id: 100 + i, level: i + 1);
            state.SetFightSkills(many);
            Assert.AreEqual(50, state.CountVisibleSkills());
            Assert.AreEqual(100, state.Slots[0].SkillId);
            Assert.AreEqual(149, state.Slots[49].SkillId);
        }

        [Test]
        public void SelectByIndex_ShowsMaskAndInfoLabelSemantics()
        {
            var state = new JxSkillPanelState();
            state.SetFightSkills(new[] { Skill(id: 122, level: 7) });
            Assert.IsTrue(state.TrySelectByIndex(0, out var detail));
            Assert.AreEqual(0, state.SelectedIndex);
            Assert.AreEqual(122, state.SelectedSkillId);
            Assert.IsTrue(state.InfoLabelCreated);
            Assert.AreEqual("Phi Long Tại Thiên (lv 7)", state.SkillInfoLabelText);
            Assert.AreEqual("Phi Long Tại Thiên(Hỏa)", detail.Title);
            Assert.AreEqual("Phi Long Tại Thiên (lv 7)", detail.InfoLabel);
        }

        [Test]
        public void TrySelectAt_SelectsVisibleSlotByRect()
        {
            var state = new JxSkillPanelState();
            state.SetFightSkills(new[] { Skill(id: 122, level: 1), Skill(id: 125, level: 2) });
            Assert.IsTrue(state.TrySelectAt(new Vector2(20, 250), out var detail0));
            Assert.AreEqual(122, detail0.SkillId);
            Assert.IsTrue(state.TrySelectAt(new Vector2(60, 250), out var detail1));
            Assert.AreEqual(125, detail1.SkillId);
        }

        [Test]
        public void TrySelectAt_OutsideClearsSelection()
        {
            var state = new JxSkillPanelState();
            state.SetFightSkills(new[] { Skill() });
            state.TrySelectByIndex(0, out _);
            Assert.IsFalse(state.TrySelectAt(new Vector2(-100, -100), out var detail));
            Assert.IsNull(detail);
            Assert.AreEqual(0, state.SelectedSkillId);
            Assert.AreEqual(-1, state.SelectedIndex);
        }

        [Test]
        public void UpdateSkill_ReturnsFalse_UntilInfoLabelExists_SourceQuirk()
        {
            var state = new JxSkillPanelState();
            Assert.IsFalse(state.UpdateSkill(Skill(id: 200, level: 3), 0));
        }

        [Test]
        public void UpdateSkill_AfterSelection_UpdatesSlotAndInfoLabel()
        {
            var state = new JxSkillPanelState();
            state.SetFightSkills(new[] { Skill(id: 122, level: 1) });
            state.TrySelectByIndex(0, out _);
            Assert.IsTrue(state.UpdateSkill(Skill(id: 125, level: 9), 0));
            Assert.AreEqual(125, state.Slots[0].SkillId);
            Assert.AreEqual(9, state.Slots[0].Level);
            Assert.AreEqual("Phi Long Tại Thiên (lv 9)", state.SkillInfoLabelText);
        }

        [Test]
        public void BuildDetail_BothSkill_CanMainAndExtra()
        {
            var state = new JxSkillPanelState();
            var detail = state.BuildDetail(Skill(lr: JxSkillLRInfo.BothSkill));
            Assert.IsTrue(detail.CanMain);
            Assert.IsTrue(detail.CanExtra);
            Assert.AreEqual(JxSkillPanelAction.AssignMain, detail.MainButtonAction);
            Assert.AreEqual(JxSkillPanelAction.AssignExtra, detail.ExtraButtonAction);
            Assert.AreEqual("ui/btn_skill/main.spr", detail.MainButtonSprite);
            Assert.AreEqual("ui/btn_skill/extra.spr", detail.ExtraButtonSprite);
        }

        [Test]
        public void BuildDetail_LeftOnly_DisablesExtra()
        {
            var state = new JxSkillPanelState();
            var detail = state.BuildDetail(Skill(lr: JxSkillLRInfo.LeftOnlySkill));
            Assert.IsTrue(detail.CanMain);
            Assert.IsFalse(detail.CanExtra);
            Assert.AreEqual(JxSkillPanelAction.AssignMain, detail.MainButtonAction);
            Assert.AreEqual(JxSkillPanelAction.None, detail.ExtraButtonAction);
        }

        [Test]
        public void BuildDetail_RightOnly_DisablesMain()
        {
            var state = new JxSkillPanelState();
            var detail = state.BuildDetail(Skill(lr: JxSkillLRInfo.RightOnlySkill));
            Assert.IsFalse(detail.CanMain);
            Assert.IsTrue(detail.CanExtra);
            Assert.AreEqual(JxSkillPanelAction.None, detail.MainButtonAction);
            Assert.AreEqual(JxSkillPanelAction.AssignExtra, detail.ExtraButtonAction);
        }

        [Test]
        public void BuildDetail_ForbiddenSkillIds_DisableMainAndExtra()
        {
            var state = new JxSkillPanelState();
            foreach (int id in new[] { 1, 2, 53 })
            {
                var detail = state.BuildDetail(Skill(id: id));
                Assert.IsFalse(detail.CanMain);
                Assert.IsFalse(detail.CanExtra);
            }
        }

        [Test]
        public void BuildDetail_Thief_DisablesMainOnly()
        {
            var state = new JxSkillPanelState();
            var detail = state.BuildDetail(Skill(style: JxSkillStyle.Thief));
            Assert.IsFalse(detail.CanMain);
            Assert.IsTrue(detail.CanExtra);
        }

        [Test]
        public void BuildDetail_LevelZero_DisablesMainAndExtra()
        {
            var state = new JxSkillPanelState();
            var detail = state.BuildDetail(Skill(level: 0));
            Assert.IsFalse(detail.CanMain);
            Assert.IsFalse(detail.CanExtra);
        }

        [Test]
        public void MainButton_WhenAlreadyEquipped_BecomesRemoveMain()
        {
            var state = new JxSkillPanelState();
            state.SetMainSkill(122);
            var detail = state.BuildDetail(Skill(id: 122));
            Assert.IsTrue(detail.IsMainEquipped);
            Assert.AreEqual(JxSkillPanelAction.RemoveMain, detail.MainButtonAction);
            Assert.AreEqual("ui/btn_skill/remove_main.spr", detail.MainButtonSprite);
        }

        [Test]
        public void ExtraButton_WhenAlreadyEquipped_BecomesRemoveExtra()
        {
            var state = new JxSkillPanelState();
            state.SetExtraEquipped(122, JxSkillUseModel.Facing);
            var detail = state.BuildDetail(Skill(id: 122));
            Assert.IsTrue(detail.IsExtraEquipped);
            Assert.AreEqual(JxSkillUseModel.Facing, detail.Model);
            Assert.AreEqual(JxSkillPanelAction.RemoveExtra, detail.ExtraButtonAction);
            Assert.AreEqual("ui/btn_skill/remove_extra.spr", detail.ExtraButtonSprite);
        }

        [Test]
        public void ClickMain_AssignsThenRemoves_WhenClickedAgain()
        {
            var state = new JxSkillPanelState();
            var detail = state.BuildDetail(Skill(id: 122));
            var assign = state.ClickMain(detail);
            Assert.AreEqual(JxSkillPanelAction.AssignMain, assign.Action);
            Assert.AreEqual(122, assign.SkillId);
            Assert.IsTrue(assign.CloseAfterAction);
            Assert.AreEqual(122, state.MainSkillId);

            var remove = state.ClickMain(detail);
            Assert.AreEqual(JxSkillPanelAction.RemoveMain, remove.Action);
            Assert.IsTrue(remove.CloseAfterAction);
            Assert.AreEqual(0, state.MainSkillId);
        }

        [Test]
        public void ClickExtra_AssignsWithPendingModel()
        {
            var state = new JxSkillPanelState();
            var detail = state.BuildDetail(Skill(id: 122));
            state.ToggleModel(detail, JxSkillUseModel.NoTarget);
            var refreshed = state.BuildDetail(Skill(id: 122));
            Assert.AreEqual(JxSkillUseModel.NoTarget, refreshed.Model);
            Assert.IsTrue(refreshed.IsExtraEquipped);
        }

        [Test]
        public void ToggleModel_IsMutuallyExclusive_BySingleStoredModel()
        {
            var state = new JxSkillPanelState();
            var detail = state.BuildDetail(Skill(id: 122));
            state.ToggleModel(detail, JxSkillUseModel.AutoTarget);
            state.ToggleModel(detail, JxSkillUseModel.TouchRelease);
            state.ToggleModel(detail, JxSkillUseModel.Facing);
            var refreshed = state.BuildDetail(Skill(id: 122));
            Assert.AreEqual(JxSkillUseModel.Facing, refreshed.Model);
        }

        [Test]
        public void AddPoint_PracticeOnlySkill_ReturnsVietnameseMessage()
        {
            var state = new JxSkillPanelState();
            var detail = state.BuildDetail(Skill(id: 714, level: 10));
            var cmd = state.ClickAddPoint(detail);
            Assert.AreEqual(JxSkillPanelAction.AddPoint, cmd.Action);
            Assert.AreEqual("Skill không thể nâng cấp, chỉ có luyện tập mới lên được!!!", cmd.Message);
        }

        [Test]
        public void AddPoint_NormalSkill_RequestsToneUpFightSkill()
        {
            var state = new JxSkillPanelState();
            var detail = state.BuildDetail(Skill(id: 122, level: 10));
            var cmd = state.ClickAddPoint(detail);
            Assert.AreEqual(JxSkillPanelAction.AddPoint, cmd.Action);
            Assert.AreEqual(122, cmd.SkillId);
            Assert.AreEqual((int)JxSkillGenre.Fight, cmd.Genre);
            Assert.AreEqual(string.Empty, cmd.Message);
        }

        [Test]
        public void AddPoint_AtMaxLevel_ReturnsNone()
        {
            var state = new JxSkillPanelState();
            var skill = Skill(id: 122, level: 30);
            skill.MaxLevel = 30;
            var detail = state.BuildDetail(skill);
            Assert.IsFalse(detail.CanAddPoint);
            Assert.AreEqual(JxSkillPanelAction.None, state.ClickAddPoint(detail).Action);
        }

        [Test]
        public void FormatTitle_UsesVietnameseSeriesNames()
        {
            Assert.AreEqual("X(Kim)", JxSkillPanelState.FormatTitle(new JxSkillPanelSkill { Name = "X", Series = 1 }));
            Assert.AreEqual("X(Mộc)", JxSkillPanelState.FormatTitle(new JxSkillPanelSkill { Name = "X", Series = 2 }));
            Assert.AreEqual("X(Thủy)", JxSkillPanelState.FormatTitle(new JxSkillPanelSkill { Name = "X", Series = 3 }));
            Assert.AreEqual("X(Hỏa)", JxSkillPanelState.FormatTitle(new JxSkillPanelSkill { Name = "X", Series = 4 }));
            Assert.AreEqual("X(Thổ)", JxSkillPanelState.FormatTitle(new JxSkillPanelSkill { Name = "X", Series = 5 }));
        }

        [Test]
        public void BuildSeriesText_IncludesLevelAddPointEnChanceAndExp()
        {
            string text = JxSkillPanelState.BuildSeriesText(20, 3, 15, true, 40);
            StringAssert.Contains("Cấp 20(17+3)", text);
            StringAssert.Contains("Gia tăng 15%", text);
            StringAssert.Contains("Kinh nghiệm 40%", text);
        }

        [Test]
        public void BuildLimitText_IncludesHorseRestriction()
        {
            StringAssert.Contains("Không thể dùng trên ngựa", JxSkillPanelState.BuildLimitText(-1, 1));
            StringAssert.Contains("Chỉ dùng trên ngựa", JxSkillPanelState.BuildLimitText(-1, 2));
        }

        [Test]
        public void PracticeOnlyList_MatchesRepresentativeSourceIds()
        {
            Assert.IsTrue(JxSkillPanelState.IsPracticeOnlySkill(714)); // Cái Bang 120
            Assert.IsTrue(JxSkillPanelState.IsPracticeOnlySkill(1073));
            Assert.IsFalse(JxSkillPanelState.IsPracticeOnlySkill(122));
        }
    }
}
