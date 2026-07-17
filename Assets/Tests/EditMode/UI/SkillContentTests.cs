// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Skill Panel popup tests (EditMode, Skill)
// Verifies the new SkillContent popup body: 30-cell PC-parity grid, Vietnamese
// labels, skill-point summary, tap-to-select detail toggle, "+" upgrade that
// mutates the LIVE PlayerProgressionState, progression-grant idempotency on
// reopen, and null-safety. Written RED-first (SkillContent did not exist).
//
// PC source: gaibang.lua + Reference/PcSkills.txt read by PcSkillPanelService
// (reused UNCHANGED). Mirror: FactionContentTests.cs.
//
// TDD: these tests MUST fail before SkillContent.cs exists (compile RED) and
// pass after implementation (GREEN).
// -----------------------------------------------------------------------------
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;
using VLTK.UI.Popup;
using VLTK.UI.Skill;
using VLTK.Tests.Sandbox;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("Skill")]
    public class SkillContentTests
    {
        private const string ArtFolder = "UI/HUD/Art";

        // Faction name + null grantProgression => the EditMode fallback grant path
        // (PlayerProgressionState.GrantFactionSkillPanelProgression) is exercised, NOT
        // the SandboxManager path.
        private static SkillContent MakeContent(PlayerProgressionState progression, SkillCatalog catalog = null)
            => new SkillContent(catalog ?? TestCatalogCache.NoviceAndCaiBang, progression,
                                CombatFaction.CaiBang, "Cái Bang", ArtFolder, grantProgression: null);

        private static VisualElement MakeBody(SkillContent content)
        {
            var body = new VisualElement();
            content.Build(body);
            return body;
        }

        // Populated cells carry the skill id in userData and lack the --empty class.
        private static bool IsPopulated(VisualElement cell) => !cell.ClassListContains("skill-grid-cell--empty");

        private static VisualElement FindCell(VisualElement grid, int skillId)
            => grid.Children().Single(c => IsPopulated(c) && (int)c.userData == skillId);

        // --- T2: Vietnamese title ---
        [Test]
        public void TitleVi_IsVietnamese()
        {
            var content = MakeContent(new PlayerProgressionState());
            Assert.AreEqual("Kỹ năng võ công", content.TitleVi);
        }

        // --- T3: implements both popup contracts with the PC inline footprint ---
        [Test]
        public void Implements_IPopupContent_And_IPopupLayoutHint_WithPcFootprint()
        {
            var content = MakeContent(new PlayerProgressionState());
            Assert.IsInstanceOf<IPopupContent>(content);
            Assert.IsInstanceOf<IPopupLayoutHint>(content);
            var hint = (IPopupLayoutHint)content;
            Assert.AreEqual(205f, hint.Width, "parity with prior inline Rect width");
            Assert.AreEqual(376f, hint.Height, "parity with prior inline Rect height");
            Assert.AreEqual(338f, hint.Left, "parity with prior inline Rect left");
            Assert.AreEqual(110f, hint.Top, "parity with prior inline Rect top");
        }

        [Test]
        public void PopupWindow_UsesDedicatedPcSkillChrome()
        {
            var content = MakeContent(new PlayerProgressionState());
            var window = new VLTK.UI.Popup.PopupWindow(content);

            Assert.IsTrue(window.ClassListContains("popup-window--pc-skill"),
                "the 205px PC skill sheet must opt out of the generic 16px chrome gutters");
            Assert.IsNotNull(window.Q("PopupSkillCombatTab"),
                "the PC sheet must layer the checked combat tab over UiSkillsSheet's source-selected background");
            Assert.IsNull(window.Q("PopupTitle"),
                "the PC sheet title is part of the UiSkillsSheet sprite, not generic popup text");
        }

        [Test]
        public void PopupWindow_CentersCompactPcSkillSheetInMobileDesignSpace()
        {
            var content = MakeContent(new PlayerProgressionState());
            var window = new VLTK.UI.Popup.PopupWindow(content);

            Assert.AreEqual((1280f - 205f) * 0.5f, window.style.left.value.value);
            Assert.AreEqual((720f - 376f) * 0.5f, window.style.top.value.value);
            Assert.AreEqual(205f, window.style.width.value.value);
            Assert.AreEqual(376f, window.style.height.value.value);
        }

        [Test]
        public void TangMen_UsesPcFiveByFiveSlotFootprint()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantTangMenSkillPanelProgression(catalog);
            var content = new SkillContent(catalog, progression, CombatFaction.TangMen, "Đường Môn", ArtFolder);
            var body = MakeBody(content);
            content.OnShow();

            var grid = body.Q("SkillGrid");
            Assert.AreEqual(PcSkillPanelService.PcFightSkillSlotsPerPcPanel, grid.childCount,
                "23 TangMen roots plus two empty cells fill the PC's five-by-five sheet without a sixth mobile row");
            var fifth = grid[4];
            Assert.AreEqual(156f, fifth.style.left.value.value);
            Assert.AreEqual(3f, fifth.style.top.value.value);
            var firstSecondRow = grid[5];
            Assert.AreEqual(0f, firstSecondRow.style.left.value.value);
            Assert.AreEqual(54f, firstSecondRow.style.top.value.value);
        }

        [Test]
        public void TangMen_GridContainsEveryCanonicalRoot_AndOnlyTwoEmptySlots()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var progression = new PlayerProgressionState();
            progression.GrantTangMenSkillPanelProgression(catalog);
            var content = new SkillContent(catalog, progression, CombatFaction.TangMen, "Đường Môn", ArtFolder);
            var body = MakeBody(content);
            content.OnShow();

            var grid = body.Q("SkillGrid");
            var ids = grid.Children().Where(IsPopulated).Select(c => (int)c.userData).ToArray();
            CollectionAssert.AreEqual(PcSkillPanelService.PcTangMenSkillOrder, ids,
                "the player-facing grid must expose every canonical TangMen root in PC source order");
            Assert.AreEqual(2, grid.childCount - ids.Length,
                "23 roots leave exactly two empty cells in UiSkillsFightSub.ini's 25-slot footprint");
        }

        // --- T4: 30 cells, 26 populated, 4 empty for Cái Bang ---
        [Test]
        public void Build_Produces30Cells_26Populated_4Empty_ForCaiBang()
        {
            var content = MakeContent(new PlayerProgressionState());
            var body = MakeBody(content);
            content.OnShow();

            var grid = body.Q("SkillGrid");
            Assert.IsNotNull(grid, "grid container must exist");
            Assert.AreEqual(PcSkillPanelService.PcFightSkillSlotsPerPage, grid.childCount,
                "slot count sourced from PcSkillPanelService.PcFightSkillSlotsPerPage (not a literal)");

            int populated = grid.Children().Count(IsPopulated);
            int empty = grid.childCount - populated;
            Assert.AreEqual(26, populated, "Cái Bang resolves 26 fight skills (PC parity)");
            Assert.AreEqual(4, empty, "remaining cells are empty placeholders");
        }

        // --- T5: populated skill ids in PC order ---
        [Test]
        public void Build_PopulatedSkillIds_InPcOrder()
        {
            var content = MakeContent(new PlayerProgressionState());
            var body = MakeBody(content);
            content.OnShow();

            var grid = body.Q("SkillGrid");
            var ids = grid.Children().Where(IsPopulated).Select(c => (int)c.userData).ToArray();
            CollectionAssert.AreEqual(PcSkillPanelService.PcCaiBangSkillOrder, ids,
                "grid skill ids must equal the authoritative PC Cái Bang order");
            Assert.AreEqual(115, ids[0], "first skill id is 115");
            Assert.AreEqual(1074, ids[ids.Length - 1], "last skill id is 1074");
            Assert.Contains(125, ids, "skill 125 (Bổng Đả Ác Cẩu) is present");
        }

        // --- T6: skill 125 cell name label is the exact Vietnamese PC name ---
        [Test]
        public void Build_CellNameLabel_ContainsBongDaAcCau()
        {
            var content = MakeContent(new PlayerProgressionState());
            var body = MakeBody(content);
            content.OnShow();

            var grid = body.Q("SkillGrid");
            var cell125 = FindCell(grid, 125);
            var name = cell125.Q<Label>("SkillGridName");
            Assert.IsNotNull(name, "populated cell carries a skill-name label");
            Assert.AreEqual("Bổng Đả Ác Cẩu", name.text, "exact PC VI parity for skill 125");
        }

        // --- T7: OnShow grants progression so the summary reads 200 (PC parity) ---
        [Test]
        public void OnShow_GrantsProgression_SummaryReads200()
        {
            var progression = new PlayerProgressionState();
            var content = MakeContent(progression);
            var body = MakeBody(content);
            content.OnShow();

            var summary = body.Q<Label>("SkillSummary");
            Assert.IsNotNull(summary);
            Assert.AreEqual("200", summary.text, "PC parity: Cái Bang grant yields 200 skill points");
            Assert.AreEqual(200, progression.fightSkillPoints, "live progression granted 200 points");
            Assert.AreEqual(CombatFaction.CaiBang, progression.faction);
        }

        // --- T8: tap toggles selection and shows/clears detail ---
        [Test]
        public void SelectSkill_TogglesSelectionAndDetail()
        {
            var content = MakeContent(new PlayerProgressionState());
            var body = MakeBody(content);
            content.OnShow();
            var grid = body.Q("SkillGrid");
            var detail = body.Q("SkillDetail");
            Assert.IsNotNull(detail);

            // First tap selects skill 125.
              content.SelectSkill(125);
              var cell125 = FindCell(grid, 125);
              Assert.IsTrue(cell125.ClassListContains("skill-grid-cell--selected"), "selected skill cell is highlighted");
              Assert.IsTrue(body.ClassListContains("skill-body--has-selection"), "selected state reserves visible detail space");
            var title = detail.Q<Label>("SkillDetailTitle");
            Assert.IsNotNull(title, "detail region renders after selecting");
            Assert.AreEqual("Bổng Đả Ác Cẩu", title.text);
            var status = detail.Q<Label>("SkillDetailStatus");
            Assert.IsNotNull(status);
            Assert.IsNotEmpty(status.text, "detail shows the PC upgradeStatus");

            // Second tap on the same skill deselects it.
            content.SelectSkill(125);
              var cell125After = FindCell(grid, 125);
              Assert.IsFalse(cell125After.ClassListContains("skill-grid-cell--selected"), "re-tap deselects");
              Assert.IsFalse(body.ClassListContains("skill-body--has-selection"), "deselect restores the PC catalog view");
            Assert.AreEqual(0, detail.childCount, "detail region is cleared on deselect");
        }

        // --- T9: TryUpgrade spends exactly one point and mutates the live progression ---
        [Test]
        public void TryUpgrade_SpendsOnePoint_AndMutatesLiveProgression()
        {
            var progression = new PlayerProgressionState();
            var content = MakeContent(progression);
            var body = MakeBody(content);
            content.OnShow();
            Assert.AreEqual(200, progression.fightSkillPoints);

            bool upgraded = content.TryUpgrade(117);
            Assert.IsTrue(upgraded, "skill 117 is upgradable after the grant");
            Assert.AreEqual(199, progression.fightSkillPoints, "exactly one point spent");
            Assert.AreEqual(1, progression.skillLevels[117], "live level raised to 1");

            // Re-render shows the new level (learnedLevel +1) in the cell.
            var grid = body.Q("SkillGrid");
            var level = FindCell(grid, 117).Q<Label>("SkillGridLevel");
            Assert.AreEqual("1", level.text, "grid re-renders the upgraded level");
        }

        // --- T10: TryUpgrade honors the PC max-level cap ---
        [Test]
        public void TryUpgrade_HonorsPcMaxLevelCap()
        {
            var progression = new PlayerProgressionState();
            var content = MakeContent(progression);
            var body = MakeBody(content);
            content.OnShow();

            var skill = TestCatalogCache.NoviceAndCaiBang.Resolve(128);
            Assert.NotNull(skill);
            for (int i = 0; i < skill.maxLevel; i++)
                Assert.IsTrue(content.TryUpgrade(128), $"upgrade {i + 1} to maxLevel should succeed at granted level 200");
            Assert.AreEqual(skill.maxLevel, progression.skillLevels[128], "reached PC max level");
            Assert.IsFalse(content.TryUpgrade(128), "PC rejects upgrades past skill max level");
        }

        [Test]
        public void SelectedLearnedActiveSkill_CanAssignToRequestedActiveDeckSlot()
        {
            var progression = new PlayerProgressionState();
            int assignedSkillId = 0;
            int assignedSlot = -1;
            var content = new SkillContent(TestCatalogCache.NoviceAndCaiBang, progression,
                CombatFaction.CaiBang, "Cái Bang", ArtFolder, grantProgression: null,
                assignToActiveDeckSlot: (skillId, slot) =>
                {
                    assignedSkillId = skillId;
                    assignedSlot = slot;
                    return true;
                });
            MakeBody(content);
            content.OnShow();
            Assert.IsTrue(content.TryUpgrade(117));
            content.SelectSkill(117);

            Assert.IsTrue(content.TryAssignSelectedSkillToSlot(3));
            Assert.AreEqual(117, assignedSkillId);
            Assert.AreEqual(3, assignedSlot);
        }

        [Test]
          public void SelectedUnlearnedSkill_CannotAssignToDeckSlot()
        {
            var progression = new PlayerProgressionState();
            var content = new SkillContent(TestCatalogCache.NoviceAndCaiBang, progression,
                CombatFaction.CaiBang, "Cái Bang", ArtFolder, grantProgression: null,
                assignToActiveDeckSlot: (_, _) => true);
            var body = MakeBody(content);
            content.OnShow();
            content.SelectSkill(117);

            Assert.IsFalse(content.TryAssignSelectedSkillToSlot(0));
            var slots = body.Q("SkillEquipSlots");
            Assert.IsNotNull(slots, "unlearned active skills still explain their deck-assignment action");
            Assert.AreEqual(CombatSkillSlotController.MobileSkillSlotCount, slots.childCount);
              for (int i = 0; i < slots.childCount; i++)
                  Assert.IsFalse(slots[i].enabledSelf, "unlearned skills must show disabled deck slots");
          }

          [Test]
          public void SelectedLearnedSkill_IdentifiesTheActiveDeckAndCurrentSlot()
          {
              var progression = new PlayerProgressionState();
              var content = new SkillContent(TestCatalogCache.NoviceAndCaiBang, progression,
                  CombatFaction.CaiBang, "Cái Bang", ArtFolder, grantProgression: null,
                  assignToActiveDeckSlot: (_, _) => true,
                  activeDeckName: () => "B",
                  activeDeckSlotSkill: slot => slot == 2 ? 117 : 0);
              var body = MakeBody(content);
              content.OnShow();
              Assert.IsTrue(content.TryUpgrade(117));
              content.SelectSkill(117);

                Assert.AreEqual("Deck B — chạm ô để thay · chạm lại icon để đóng", body.Q<Label>("SkillEquipLabel").text);
              var current = body.Q<Button>("SkillEquipSlot_3");
              Assert.IsTrue(current.ClassListContains("skill-equip-slot--occupied"));
              Assert.IsTrue(current.ClassListContains("skill-equip-slot--selected"));
          }

        // --- T11: TryUpgrade honors the PC low-level gate ---
        [Test]
        public void TryUpgrade_HonorsLowLevelGate()
        {
            var progression = new PlayerProgressionState();
            var content = MakeContent(progression);
            var body = MakeBody(content);
            content.OnShow();
            progression.level = 10; // clamp the PC gate (desiredLevel <= playerLevel - reqLevel + 1)

            Assert.IsTrue(content.TryUpgrade(117), "first upgrade to level 1 succeeds at player level 10");
            Assert.AreEqual(1, progression.skillLevels[117]);
            Assert.IsFalse(content.TryUpgrade(117), "PC gate rejects the next upgrade at low player level");
        }

        // --- T12: OnShow grant is idempotent across two opens ---
        [Test]
        public void OnShow_GrantIsIdempotent_OnReopen()
        {
            var progression = new PlayerProgressionState();
            var content = MakeContent(progression);
            var body = MakeBody(content);

            content.OnShow();
            Assert.AreEqual(200, progression.fightSkillPoints);
            content.OnShow(); // reopen — grant re-runs with no extra effect
            Assert.AreEqual(200, progression.fightSkillPoints, "reopen grant is idempotent");

            var summary = body.Q<Label>("SkillSummary");
            Assert.AreEqual("200", summary.text);
        }

        // --- T13: null catalog / null progression does not throw ---
        [Test]
        public void Build_NullCatalog_AndNullProgression_DoesNotThrow()
        {
            var content = new SkillContent(null, null, CombatFaction.CaiBang, null, null, grantProgression: null);
            var body = new VisualElement();
            Assert.DoesNotThrow(() => content.Build(body));
            Assert.DoesNotThrow(() => content.OnShow());

            var grid = body.Q("SkillGrid");
            Assert.IsNotNull(grid, "body scaffolds the grid even with null data");
            Assert.AreEqual(PcSkillPanelService.PcFightSkillSlotsPerPage, grid.childCount,
                "all 30 cells render as empty placeholders with null catalog");
            Assert.AreEqual(PcSkillPanelService.PcFightSkillSlotsPerPage, grid.Children().Count(c => !IsPopulated(c)),
                "every cell is an empty placeholder with null catalog");
        }
    }
}
