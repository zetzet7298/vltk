// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Skill Panel popup content (PR-1, additive)
// PC source: gaibang.lua + Reference/PcSkills.txt, read by PcSkillPanelService
// (reused UNCHANGED — BuildPage / TryUpgrade / PcFightSkillSlotsPerPage). This
// content renders the 30-cell fight-skill grid, the skill-point summary, the
// tap-to-select detail toggle, and the "+" upgrade that spends a real
// fight-skill point on the LIVE PlayerProgressionState.
//
// Port rule: 100% ported from PC — no invention. All skill text / ordering /
// level-cap / canUpgrade logic comes from PcSkillPanelSnapshot rows; the content
// contains NO locally-computed skill description.
//
// Mirror pattern: FactionContent.cs (IPopupContent + IPopupLayoutHint, code-built
// body, no .uxml). Selection/upgrade are content-owned internal seams exercised by
// SkillContentTests via InternalsVisibleTo (no UI pointer-event flakiness).
//
// PR-1 SCOPE: SkillContent exists + is unit-tested + Skill.uss ships, but
// BtnSkills still opens the old inline panel. Nothing is wired yet.
// -----------------------------------------------------------------------------
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI.Popup;

namespace VLTK.UI.Skill
{
    /// <summary>
    /// Popup body for BtnSkills: the Cái Bang / active-faction fight-skill grid.
    /// Renders exactly PcSkillPanelService.PcFightSkillSlotsPerPage cells in a single
    /// scrollable page, binds/mutates skill data ONLY through PcSkillPanelService,
    /// and grants faction skill-panel progression on open (gameplay-critical, idempotent).
    /// </summary>
    public sealed class SkillContent : IPopupContent, IPopupLayoutHint
    {
        // Strict parity with the prior inline Rect(338, 110, 205, 376). The skill sheet
        // art (UiSkillsSheet.ini) is authored at 205x376; these four values are the full
        // window geometry (PopupWindow.ApplyLayoutHint sets style.width/height/left/top).
        public string TitleVi => "Kỹ năng võ công";
        public float Width => 205f;
        public float Height => 376f;
        public float Left => 338f;
        public float Top => 110f;

        private readonly SkillCatalog _catalog;
        private readonly PlayerProgressionState _progression; // LIVE ref — mutated by TryUpgrade
        private readonly CombatFaction _faction;              // resolved by caller (CaiBang when None)
        private readonly string _factionNameVi;               // body header context; optional
          private readonly string _artFolder;                   // HUD art root for skill icons
          private readonly System.Action<CombatFaction> _grantProgression; // null => EditMode fallback
          private readonly System.Func<int, int, bool> _assignToActiveDeckSlot;
          private readonly System.Func<string> _activeDeckName;
          private readonly System.Func<int, int> _activeDeckSlotSkill;

        // Selection + page state owned INTERNALLY (not received per-call). PageIndex is
        // fixed at 0: PcFightSkillPageCount == 1 (single scrollable page; no 2-tab UI).
        private int _selectedSkillId;
        private const int PageIndex = 0;

          private Label _summary;
          private VisualElement _body;
          private VisualElement _grid;
          private VisualElement _detail;

        public SkillContent(
            SkillCatalog catalog,
            PlayerProgressionState progression,
              CombatFaction faction,
                string factionNameVi,
                string artFolder,
                System.Action<CombatFaction> grantProgression = null,
                System.Func<int, int, bool> assignToActiveDeckSlot = null,
                System.Func<string> activeDeckName = null,
                System.Func<int, int> activeDeckSlotSkill = null)
        {
            _catalog = catalog;
            _progression = progression;
            _faction = faction;
            _factionNameVi = factionNameVi;
            _artFolder = artFolder;
              _grantProgression = grantProgression;
              _assignToActiveDeckSlot = assignToActiveDeckSlot;
              _activeDeckName = activeDeckName;
              _activeDeckSlotSkill = activeDeckSlotSkill;
        }

        // IPopupContent — heavy: scaffold summary + scrollable grid + detail region.
        // Safe to call before the OnShow grant (Refresh is deferred to OnShow).
          public void Build(VisualElement body)
          {
                body.Clear();
                body.AddToClassList("skill-body");
                if (_faction == CombatFaction.TangMen)
                    body.AddToClassList("skill-body--pc-five-by-five");
                _body = body;

              _summary = new Label { name = "SkillSummary" };
              UseReadableFont(_summary);
            _summary.AddToClassList("skill-summary");
            body.Add(_summary);

            var scroll = new ScrollView { name = "SkillGridScroll" };
            scroll.AddToClassList("skill-grid-scroll");
            _grid = new VisualElement { name = "SkillGrid" };
            _grid.AddToClassList("skill-grid");
            scroll.Add(_grid);
            body.Add(scroll);

            _detail = new VisualElement { name = "SkillDetail" };
            _detail.AddToClassList("skill-detail");
            body.Add(_detail);
        }

        // Gameplay-critical (design D2): grant faction skill-panel progression BEFORE
        // BuildPage, then Refresh. Re-run safe — GrantFactionSkillPanelProgression is
        // idempotent (re-granting the same faction leaves spent points/levels unchanged),
        // so reopening the popup is safe.
        public void OnShow()
        {
            if (_grantProgression != null)
                _grantProgression(_faction);                 // runtime: SandboxManager.Grant...
            else
                _progression?.GrantFactionSkillPanelProgression(_catalog, _faction); // EditMode fallback
            Refresh();
            SubsystemLog.Info("HUD", string.Format(CultureInfo.InvariantCulture,
                "Open {0} Skills popup (points={1})",
                string.IsNullOrEmpty(_factionNameVi) ? _faction.ToString() : _factionNameVi,
                _summary != null ? _summary.text : "?"));
        }

        public void OnClose()
        {
            _summary = null;
            _grid = null;
            _detail = null;
        }

        // Rebuild the snapshot and re-render grid + summary + detail. NULL-safe:
        // PcSkillPanelService.BuildPage dereferences progression.faction, so a null
        // progression is rendered as an all-empty 30-cell grid (no progression = no
        // skills) rather than calling BuildPage. A null catalog with a non-null
        // progression is handled inside BuildPage (zero rows => all-empty grid),
        // so only the null-progression case needs the explicit guard. No skill
        // ordering/description/cap logic is computed locally here.
        private void Refresh()
        {
            if (_grid == null)
                return;

              if (_progression == null)
              {
                  _body?.RemoveFromClassList("skill-body--has-selection");
                  if (_summary != null)
                      _summary.text = 0.ToString(CultureInfo.InvariantCulture);
                  _grid.Clear();
                  int emptyDisplaySlots = PcSkillPanelService.GetDisplaySlotCount(_faction);
                  for (int slotIndex = 0; slotIndex < emptyDisplaySlots; slotIndex++)
                      _grid.Add(BuildEmptyCell(slotIndex));
                if (_detail != null)
                    _detail.Clear();
                return;
            }

              var snap = PcSkillPanelService.BuildPage(_catalog, _progression, _selectedSkillId, PageIndex);
              if (snap.selectedRow.HasValue)
                  _body?.AddToClassList("skill-body--has-selection");
              else
                  _body?.RemoveFromClassList("skill-body--has-selection");

            if (_summary != null)
                _summary.text = snap.skillPoints.ToString(CultureInfo.InvariantCulture);

              _grid.Clear();
              int displaySlots = PcSkillPanelService.GetDisplaySlotCount(_faction);
              for (int slotIndex = 0; slotIndex < displaySlots; slotIndex++)
              {
                  if (slotIndex < snap.rows.Count)
                      _grid.Add(BuildPopulatedCell(snap.rows[slotIndex], slotIndex));
                  else
                      _grid.Add(BuildEmptyCell(slotIndex));
            }

            RenderDetail(snap);
        }

        // Port of inline GameHudController.PopulateSkillPanel cell construction:
        // slot icon (via HudArtPathResolver/LoadIconStatic), level overlay, optional
        // "+" add-point (only when canUpgrade), Vietnamese name label, select callback.
        private VisualElement BuildPopulatedCell(PcSkillPanelRow row, int slotIndex)
        {
            var cell = new VisualElement();
            cell.name = "SkillCell_" + row.skillId.ToString(CultureInfo.InvariantCulture);
            cell.userData = row.skillId; // test seam: populated cells carry the skill id
            cell.AddToClassList("skill-grid-cell");
            ApplyPcSlotPosition(cell, slotIndex);
            cell.pickingMode = PickingMode.Position;
            if (row.canUpgrade)
                cell.AddToClassList("skill-grid-cell--upgradable");
            if (row.skillId == _selectedSkillId)
                cell.AddToClassList("skill-grid-cell--selected");

            var slot = new VisualElement();
            slot.AddToClassList("skill-grid-slot");
            cell.Add(slot);
            // Load the PC skill icon exactly as the inline path did (cai_bang_skill_<id>).
            var artPath = HudArtPathResolver.ResolveGeneratedArtRoot(_artFolder);
            var iconName = string.Format(CultureInfo.InvariantCulture, "cai_bang_skill_{0}", row.skillId);
            GameHudController.LoadIconStatic(slot, artPath, iconName);

            var levelText = row.learnedLevel > 0
                ? row.learnedLevel.ToString(CultureInfo.InvariantCulture)
                : string.Empty;
              var level = new Label(levelText) { name = "SkillGridLevel" };
              UseReadableFont(level);
            level.AddToClassList("skill-grid-level");
            cell.Add(level);

            // Only an upgradable row renders an actionable "+" spend affordance; a
            // non-upgradable row renders none, so activating it only toggles selection.
            if (row.canUpgrade)
            {
                var add = new VisualElement();
                add.AddToClassList("skill-add-point");
                add.pickingMode = PickingMode.Position;
                int skillId = row.skillId;
                add.RegisterCallback<PointerDownEvent>(evt =>
                {
                    TryUpgrade(skillId);
                    evt.StopPropagation();
                });
                cell.Add(add);
            }

              var name = new Label(row.displayName) { name = "SkillGridName" };
              UseReadableFont(name);
            name.AddToClassList("skill-grid-name");
            cell.Add(name);

            int selectId = row.skillId;
            cell.RegisterCallback<PointerDownEvent>(evt =>
            {
                SelectSkill(selectId);
                evt.StopPropagation();
            });

            return cell;
        }

        private static VisualElement BuildEmptyCell(int slotIndex)
        {
            var cell = new VisualElement();
            cell.name = "SkillCellEmpty_" + slotIndex.ToString(CultureInfo.InvariantCulture);
            cell.AddToClassList("skill-grid-cell");
            cell.AddToClassList("skill-grid-cell--empty");
            ApplyPcSlotPosition(cell, slotIndex);

            var slot = new VisualElement();
            slot.AddToClassList("skill-grid-slot");
            slot.AddToClassList("skill-grid-slot--empty");
            cell.Add(slot);
            return cell;
        }

        // UiSkillsFightSub.ini [SkillIcon]: x=0,39,78,117,156; y=3,54,105,156,207.
        // Absolute PC coordinates make five columns deterministic even when a
        // ScrollView's generated content container reports a narrower flex width.
        private static void ApplyPcSlotPosition(VisualElement cell, int slotIndex)
        {
            int column = slotIndex % PcSkillPanelService.PcFightSkillColumns;
            int row = slotIndex / PcSkillPanelService.PcFightSkillColumns;
            cell.style.position = Position.Absolute;
            cell.style.left = column * 39;
            cell.style.top = 3 + row * 51;
        }

        // Detail region derived ENTIRELY from the selected PcSkillPanelRow (no local
        // description logic). Cleared when nothing is selected.
        private void RenderDetail(PcSkillPanelSnapshot snap)
        {
            if (_detail == null)
                return;
            _detail.Clear();
            if (!snap.selectedRow.HasValue)
                return;
            var row = snap.selectedRow.Value;

              var title = new Label(row.displayName) { name = "SkillDetailTitle" };
              UseReadableFont(title);
            title.AddToClassList("skill-detail-title");
            _detail.Add(title);

              var level = new Label(string.Format(CultureInfo.InvariantCulture, "Cấp {0}/{1}", row.learnedLevel, row.maxLevel))
              { name = "SkillDetailLevel" };
              UseReadableFont(level);
            level.AddToClassList("skill-detail-level");
            _detail.Add(level);

              var summary = new Label(row.summary) { name = "SkillDetailSummary" };
              UseReadableFont(summary);
            summary.AddToClassList("skill-detail-summary");
            _detail.Add(summary);

            if (!string.IsNullOrEmpty(row.nextLevelSummary))
            {
                  var next = new Label(row.nextLevelSummary) { name = "SkillDetailNext" };
                  UseReadableFont(next);
                next.AddToClassList("skill-detail-next");
                _detail.Add(next);
            }

              var status = new Label(row.upgradeStatus) { name = "SkillDetailStatus" };
              UseReadableFont(status);
            status.AddToClassList("skill-detail-status");
            _detail.Add(status);

            if (CanRenderCombatSlotAssignment(row, out bool canAssign))
            {
                    string deckName = _activeDeckName?.Invoke();
                    var equipLabel = new Label(canAssign
                        ? $"Deck {(string.IsNullOrEmpty(deckName) ? "hiện tại" : deckName)} — chạm ô để thay · chạm lại icon để đóng"
                        : "Học skill cấp 1 để gán vào deck") { name = "SkillEquipLabel" };
                  UseReadableFont(equipLabel);
                equipLabel.AddToClassList("skill-equip-label");
                _detail.Add(equipLabel);

                var equipSlots = new VisualElement { name = "SkillEquipSlots" };
                equipSlots.AddToClassList("skill-equip-slots");
                for (int slot = 0; slot < CombatSkillSlotController.MobileSkillSlotCount; slot++)
                {
                    int capturedSlot = slot;
                      var equip = new Button(() => TryAssignSelectedSkillToSlot(capturedSlot))
                    {
                        name = "SkillEquipSlot_" + (slot + 1).ToString(CultureInfo.InvariantCulture),
                        text = "Ô " + (slot + 1).ToString(CultureInfo.InvariantCulture)
                    };
                      equip.AddToClassList("skill-equip-slot");
                      UseReadableFont(equip);
                      int assignedSkill = _activeDeckSlotSkill?.Invoke(slot) ?? 0;
                      if (assignedSkill > 0)
                      {
                          equip.AddToClassList("skill-equip-slot--occupied");
                          // The slot keeps the exact hotbar skill icon, so a touch user
                          // can see which action will be replaced without relying on a
                          // tiny text label inside the PC-sized sheet.
                          var artPath = HudArtPathResolver.ResolveGeneratedArtRoot(_artFolder);
                          var iconName = string.Format(CultureInfo.InvariantCulture, "cai_bang_skill_{0}", assignedSkill);
                          GameHudController.LoadIconStatic(equip, artPath, iconName);
                      }
                      if (assignedSkill == row.skillId)
                      {
                          equip.AddToClassList("skill-equip-slot--selected");
                          equip.tooltip = "Skill này đang ở ô này";
                      }
                      else if (assignedSkill > 0)
                      {
                          equip.tooltip = "Chạm để thay skill đang gán ở ô này";
                      }
                      equip.SetEnabled(canAssign);
                    equipSlots.Add(equip);
                }
                _detail.Add(equipSlots);
            }
        }

          private bool CanRenderCombatSlotAssignment(PcSkillPanelRow row, out bool canAssign)
        {
            var skill = _catalog?.Resolve(row.skillId);
            bool activeSkill = skill != null && skill.skillStyle != PcSkillStyle.PassivityNpcState;
            canAssign = _assignToActiveDeckSlot != null && row.learnedLevel > 0 && activeSkill;
            return _assignToActiveDeckSlot != null && activeSkill;
          }

          private static void UseReadableFont(TextElement element)
          {
              if (element == null) return;
              var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
              if (font != null)
                  element.style.unityFont = font;
          }

        // Content-owned selection toggle (design D6): tapping an already-selected skill
        // deselects it (selectedSkillId -> 0). Internal seam for tests; runtime fires it
        // via the cell PointerDownEvent.
        internal void SelectSkill(int skillId)
        {
            _selectedSkillId = _selectedSkillId == skillId ? 0 : skillId;
            Refresh();
        }

        // Content-owned upgrade (design D6): spends one fight-skill point on the LIVE
        // progression via PcSkillPanelService.TryUpgrade, then re-renders. Internal seam
        // for tests; runtime fires it via the "+" PointerDownEvent.
        internal bool TryUpgrade(int skillId)
        {
            if (PcSkillPanelService.TryUpgrade(_progression, _catalog, skillId))
            {
                Refresh();
                return true;
            }
            return false;
        }

        // Test seam and Button callback: panel only requests assignment; the hotbar
        // remains authoritative for active-deck ownership and final validation.
        internal bool TryAssignSelectedSkillToSlot(int slot)
        {
            if (_selectedSkillId <= 0 || _assignToActiveDeckSlot == null) return false;
            var skill = _catalog?.Resolve(_selectedSkillId);
            if (skill == null || skill.skillStyle == PcSkillStyle.PassivityNpcState) return false;
            if ((_progression?.GetSkillLevel(_selectedSkillId) ?? 0) <= 0) return false;
            return _assignToActiveDeckSlot(_selectedSkillId, slot);
        }
    }
}
