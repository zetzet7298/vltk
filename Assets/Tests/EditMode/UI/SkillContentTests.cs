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
