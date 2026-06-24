// -----------------------------------------------------------------------------
// VLTK Mobile — JX Võ Công / skill panel state
// Port of jx-cocos VN UI:
//   /home/zet/Projects/jx-cocos/client/Classes/vn/gameui/KuiSkillVN.cpp
//   /home/zet/Projects/jx-cocos/client/Classes/vn/gameui/KuiSkilldescVN.cpp
//
// Verified source behavior:
//  - FIGHT_SKILL_COUNT = 50, FIGHT_SKILL_COUNT_PER_PAGE = 25.
//  - Skill grid uses 10 columns x 5 rows. First icon at (9, panelHeight-75),
//    slot rect 37x48, icon scaled to 33px width, point label at x-1,y-16.
//  - Selecting a visible skill shows sel_mask at icon position and label
//    "<skill name> (lv <level>)" at (210,40), then opens KuiSkilldescVN.
//  - UpdateSkill only updates when the info label already exists in source.
//  - Detail buttons: main/remove_main, extra/remove_extra, addpoint, and four
//    mutually exclusive model toggles: Tự tìm, Không mục tiêu, Chạm và nhả,
//    Hướng nhìn.
//  - Main action calls setattackSprInfo(skillId, genre, icon) or clears if the
//    skill is already the left skill. Extra action calls setaauxiliaryskillInfo
//    with model skill mode and icon.
//
// This file is pure state (no MonoBehaviour) for EditMode validation. Rendering
// adapters should convert these states to UI Toolkit/USS later.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VLTK.UI.JxCocos
{
    public enum JxSkillGenre
    {
        Unknown = 0,
        Fight = 1,
        Shortcut = 2,
    }

    public enum JxSkillStyle
    {
        Unknown = 0,
        Missiles = 1,
        Melee = 2,
        InitiativeNpcState = 3,
        PassivityNpcState = 4,
        Thief = 5,
    }

    public enum JxSkillLRInfo
    {
        None = 0,
        BothSkill = 1,
        LeftOnlySkill = 2,
        RightOnlySkill = 3,
    }

    public enum JxSkillUseModel
    {
        AutoTarget = 0,     // Tự tìm mục tiêu
        TouchRelease = 1,   // Chạm và nhả
        NoTarget = 2,       // Không mục tiêu
        Facing = 3,         // Hướng nhìn
    }

    public enum JxSkillPanelAction
    {
        None = 0,
        AssignMain = 1,
        RemoveMain = 2,
        AssignExtra = 3,
        RemoveExtra = 4,
        AddPoint = 5,
    }

    public sealed class JxSkillPanelSkill
    {
        public int SkillId;
        public int Genre;
        public int Level;
        public int AddPoint;
        public int EnChance;
        public int MaxLevel = 20;
        public string Name = string.Empty;
        public string IconPath = string.Empty;
        public string CurrentDescription = string.Empty;
        public string NextDescription = string.Empty;
        public string SkillDescription = string.Empty;
        public JxSkillStyle Style = JxSkillStyle.Unknown;
        public JxSkillLRInfo LRInfo = JxSkillLRInfo.BothSkill;
        public int Series;
        public bool ExpSkill;
        public int ExpPercent;
        public int WeaponLimit;
        public int HorseLimit;
    }

    public sealed class JxSkillGridSlot
    {
        public int Index;
        public Rect Rect;
        public Vector2 IconPosition;
        public Vector2 PointLabelPosition;
        public int SkillId;
        public int Genre;
        public int Level;
        public int AddPoint;
        public int EnChance;
        public string IconPath = string.Empty;
        public bool Visible;

        public string LevelText => AddPoint > 0 ? Level.ToString() : Level.ToString();
        public bool HasAddedPoints => AddPoint > 0;
    }

    public sealed class JxSkillDetailState
    {
        public int SkillId;
        public int Genre;
        public int Level;
        public int AddPoint;
        public int EnChance;
        public int MaxLevel;
        public string Name = string.Empty;
        public string IconPath = string.Empty;
        public string Title = string.Empty;
        public string InfoLabel = string.Empty;
        public string CurrentDescription = string.Empty;
        public string NextDescription = string.Empty;
        public string LimitText = string.Empty;
        public string SeriesText = string.Empty;
        public bool CanMain;
        public bool CanExtra;
        public bool CanAddPoint;
        public bool IsMainEquipped;
        public bool IsExtraEquipped;
        public JxSkillUseModel Model;

        public JxSkillPanelAction MainButtonAction => !CanMain ? JxSkillPanelAction.None : IsMainEquipped ? JxSkillPanelAction.RemoveMain : JxSkillPanelAction.AssignMain;
        public JxSkillPanelAction ExtraButtonAction => !CanExtra ? JxSkillPanelAction.None : IsExtraEquipped ? JxSkillPanelAction.RemoveExtra : JxSkillPanelAction.AssignExtra;
        public string MainButtonSprite => MainButtonAction == JxSkillPanelAction.RemoveMain ? "ui/btn_skill/remove_main.spr" : "ui/btn_skill/main.spr";
        public string ExtraButtonSprite => ExtraButtonAction == JxSkillPanelAction.RemoveExtra ? "ui/btn_skill/remove_extra.spr" : "ui/btn_skill/extra.spr";
    }

    public sealed class JxSkillPanelCommand
    {
        public JxSkillPanelAction Action;
        public int SkillId;
        public int Genre;
        public string IconPath = string.Empty;
        public JxSkillUseModel Model;
        public bool CloseAfterAction;
        public string Message = string.Empty;
    }

    public sealed class JxSkillPanelState
    {
        public const int FightSkillCount = 50;
        public const int FightSkillCountPerPage = 25;
        public const int GridColumns = 10;
        public const int GridRows = 5;
        public const float SlotWidth = 37f;
        public const float SlotHeight = 48f;
        public const float FirstSlotX = 9f;
        public const float FirstSlotOffsetY = 75f;
        public const float IconScaledWidth = 33f;
        public const float SkillInfoLabelX = 210f;
        public const float SkillInfoLabelY = 40f;
        public const string BackgroundSprite = "ui/skill/skillbox.spr";
        public const string CloseButtonSprite = "ui/item/btn_close_big.spr";
        public const string SelectionMaskSprite = "sel_mask.png";

        private readonly JxSkillGridSlot[] _slots = new JxSkillGridSlot[FightSkillCount];
        private readonly Dictionary<int, JxSkillPanelSkill> _catalog = new();
        private readonly HashSet<int> _extraEquipped = new();
        private readonly Dictionary<int, JxSkillUseModel> _extraModels = new();

        public float PanelHeight { get; }
        public int SelectedSkillId { get; private set; }
        public int SelectedIndex { get; private set; } = -1;
        public bool InfoLabelCreated { get; private set; }
        public string SkillInfoLabelText { get; private set; } = string.Empty;
        public int MainSkillId { get; private set; }
        public JxSkillUseModel PendingModel { get; private set; } = JxSkillUseModel.AutoTarget;

        public IReadOnlyList<JxSkillGridSlot> Slots => _slots;

        public JxSkillPanelState(float panelHeight = 320f)
        {
            PanelHeight = panelHeight;
            for (int i = 0; i < FightSkillCount; i++)
                _slots[i] = CreateEmptySlot(i, panelHeight);
        }

        public static JxSkillGridSlot CreateEmptySlot(int index, float panelHeight)
        {
            if (index < 0 || index >= FightSkillCount) throw new ArgumentOutOfRangeException(nameof(index));
            int row = index / GridColumns;
            int col = index % GridColumns;
            var pos = new Vector2(FirstSlotX + col * SlotWidth, panelHeight - FirstSlotOffsetY - row * SlotHeight);
            return new JxSkillGridSlot
            {
                Index = index,
                IconPosition = pos,
                PointLabelPosition = new Vector2(pos.x - 1f, pos.y - 16f),
                Rect = new Rect(pos.x, pos.y, SlotWidth, SlotHeight),
            };
        }

        public void SetFightSkills(IEnumerable<JxSkillPanelSkill> skills)
        {
            foreach (var slot in _slots)
            {
                slot.Visible = false;
                slot.SkillId = 0;
                slot.Genre = 0;
                slot.Level = 0;
                slot.AddPoint = 0;
                slot.EnChance = 0;
                slot.IconPath = string.Empty;
            }

            _catalog.Clear();
            if (skills == null) return;

            int i = 0;
            foreach (var skill in skills)
            {
                if (i >= FightSkillCount) break;
                if (skill == null || skill.SkillId <= 0)
                {
                    i++;
                    continue;
                }

                _catalog[skill.SkillId] = skill;
                var slot = _slots[i];
                slot.Visible = true;
                slot.SkillId = skill.SkillId;
                slot.Genre = skill.Genre;
                slot.Level = skill.Level;
                slot.AddPoint = skill.AddPoint;
                slot.EnChance = skill.EnChance;
                slot.IconPath = skill.IconPath ?? string.Empty;
                i++;
            }
        }

        public void SetMainSkill(int skillId) => MainSkillId = skillId;

        public void SetExtraEquipped(int skillId, JxSkillUseModel model = JxSkillUseModel.AutoTarget)
        {
            if (skillId <= 0) return;
            _extraEquipped.Add(skillId);
            _extraModels[skillId] = model;
        }

        public void ClearExtraEquipped(int skillId)
        {
            _extraEquipped.Remove(skillId);
            _extraModels.Remove(skillId);
        }

        public int CountVisibleSkills() => _slots.Count(s => s.Visible);

        public bool TrySelectByIndex(int index, out JxSkillDetailState detail)
        {
            detail = null;
            if (index < 0 || index >= FightSkillCount) return false;
            var slot = _slots[index];
            if (!slot.Visible || slot.SkillId <= 0) return false;
            if (!_catalog.TryGetValue(slot.SkillId, out var skill)) return false;

            SelectedIndex = index;
            SelectedSkillId = slot.SkillId;
            InfoLabelCreated = true;
            SkillInfoLabelText = FormatInfoLabel(skill.Name, skill.Level);
            detail = BuildDetail(skill);
            return true;
        }

        public bool TrySelectAt(Vector2 point, out JxSkillDetailState detail)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                var slot = _slots[i];
                if (slot.Visible && slot.Rect.Contains(point))
                    return TrySelectByIndex(i, out detail);
            }
            SelectedSkillId = 0;
            SelectedIndex = -1;
            detail = null;
            return false;
        }

        /// <summary>Source quirk: KuiSkillVN::UpdateSkill returns if pSkillInfoLabel == NULL.</summary>
        public bool UpdateSkill(JxSkillPanelSkill skill, int index)
        {
            if (!InfoLabelCreated) return false;
            if (skill == null || index < 0 || index >= FightSkillCount) return false;
            if (skill.Genre != (int)JxSkillGenre.Fight) return false;

            _catalog[skill.SkillId] = skill;
            var slot = _slots[index];
            slot.Visible = true;
            slot.SkillId = skill.SkillId;
            slot.Genre = skill.Genre;
            slot.Level = skill.Level;
            slot.AddPoint = skill.AddPoint;
            slot.EnChance = skill.EnChance;
            slot.IconPath = skill.IconPath ?? string.Empty;
            SkillInfoLabelText = FormatInfoLabel(skill.Name, skill.Level);
            return true;
        }

        public JxSkillDetailState BuildDetail(JxSkillPanelSkill skill)
        {
            if (skill == null || skill.SkillId <= 0) return null;

            bool canMain = true;
            bool canExtra = true;
            if (skill.Level > 0)
            {
                switch (skill.Style)
                {
                    case JxSkillStyle.Missiles:
                    case JxSkillStyle.Melee:
                    case JxSkillStyle.InitiativeNpcState:
                    case JxSkillStyle.PassivityNpcState:
                        if (skill.SkillId == 1 || skill.SkillId == 2 || skill.SkillId == 53)
                        {
                            canMain = false;
                            canExtra = false;
                        }
                        else if (skill.LRInfo == JxSkillLRInfo.LeftOnlySkill)
                            canExtra = false;
                        else if (skill.LRInfo == JxSkillLRInfo.RightOnlySkill)
                            canMain = false;
                        else if (skill.LRInfo != JxSkillLRInfo.BothSkill)
                        {
                            canMain = false;
                            canExtra = false;
                        }
                        break;
                    case JxSkillStyle.Thief:
                        canMain = false;
                        break;
                }
            }
            else
            {
                canMain = false;
                canExtra = false;
            }

            _extraModels.TryGetValue(skill.SkillId, out var model);
            return new JxSkillDetailState
            {
                SkillId = skill.SkillId,
                Genre = skill.Genre,
                Level = skill.Level,
                AddPoint = skill.AddPoint,
                EnChance = skill.EnChance,
                MaxLevel = skill.MaxLevel,
                Name = skill.Name ?? string.Empty,
                IconPath = skill.IconPath ?? string.Empty,
                Title = FormatTitle(skill),
                InfoLabel = FormatInfoLabel(skill.Name, skill.Level),
                CurrentDescription = skill.CurrentDescription ?? string.Empty,
                NextDescription = skill.NextDescription ?? string.Empty,
                LimitText = BuildLimitText(skill.WeaponLimit, skill.HorseLimit),
                SeriesText = BuildSeriesText(skill.Level, skill.AddPoint, skill.EnChance, skill.ExpSkill, skill.ExpPercent),
                CanMain = canMain,
                CanExtra = canExtra,
                CanAddPoint = skill.Level - Math.Abs(skill.AddPoint) < skill.MaxLevel,
                IsMainEquipped = MainSkillId == skill.SkillId,
                IsExtraEquipped = _extraEquipped.Contains(skill.SkillId),
                Model = model,
            };
        }

        public JxSkillPanelCommand ClickMain(JxSkillDetailState detail)
        {
            if (detail == null || !detail.CanMain) return new JxSkillPanelCommand();
            if (MainSkillId == detail.SkillId)
            {
                MainSkillId = 0;
                return new JxSkillPanelCommand { Action = JxSkillPanelAction.RemoveMain, CloseAfterAction = true };
            }
            MainSkillId = detail.SkillId;
            return new JxSkillPanelCommand { Action = JxSkillPanelAction.AssignMain, SkillId = detail.SkillId, Genre = detail.Genre, IconPath = detail.IconPath, CloseAfterAction = true };
        }

        public JxSkillPanelCommand ClickExtra(JxSkillDetailState detail)
        {
            if (detail == null || !detail.CanExtra) return new JxSkillPanelCommand();
            _extraEquipped.Add(detail.SkillId);
            _extraModels[detail.SkillId] = PendingModel;
            return new JxSkillPanelCommand { Action = JxSkillPanelAction.AssignExtra, SkillId = detail.SkillId, Genre = detail.Genre, IconPath = detail.IconPath, Model = PendingModel, CloseAfterAction = true };
        }

        public JxSkillPanelCommand ToggleModel(JxSkillDetailState detail, JxSkillUseModel model)
        {
            PendingModel = model;
            if (detail != null)
            {
                _extraModels[detail.SkillId] = model;
                if (detail.CanExtra)
                    _extraEquipped.Add(detail.SkillId);
            }
            var command = ClickExtra(detail);
            command.Model = model;
            return command;
        }

        public JxSkillPanelCommand ClickAddPoint(JxSkillDetailState detail)
        {
            if (detail == null || !detail.CanAddPoint) return new JxSkillPanelCommand();
            if (IsPracticeOnlySkill(detail.SkillId))
                return new JxSkillPanelCommand { Action = JxSkillPanelAction.AddPoint, SkillId = detail.SkillId, Message = "Skill không thể nâng cấp, chỉ có luyện tập mới lên được!!!" };
            return new JxSkillPanelCommand { Action = JxSkillPanelAction.AddPoint, SkillId = detail.SkillId, Genre = (int)JxSkillGenre.Fight };
        }

        public static string FormatInfoLabel(string name, int level) => $"{name ?? string.Empty} (lv {level})";

        public static string FormatTitle(JxSkillPanelSkill skill)
        {
            string series = skill.Series switch
            {
                1 => "(Kim)",
                2 => "(Mộc)",
                3 => "(Thủy)",
                4 => "(Hỏa)",
                5 => "(Thổ)",
                _ => string.Empty,
            };
            return (skill.Name ?? string.Empty) + series;
        }

        public static string BuildSeriesText(int level, int addPoint, int enChance, bool expSkill, int expPercent)
        {
            var lines = new List<string>();
            if (addPoint != 0) lines.Add($"Cấp {level}({level - Math.Abs(addPoint)}+{Math.Abs(addPoint)})");
            else lines.Add($"Cấp {level}");
            if (enChance != 0) lines.Add($"Gia tăng {enChance}%");
            if (expSkill) lines.Add($"Kinh nghiệm {expPercent}%");
            return string.Join("\n", lines) + "\n";
        }

        public static string BuildLimitText(int weaponLimit, int horseLimit)
        {
            var lines = new List<string> { "Giới hạn vũ khí: " + weaponLimit };
            if (horseLimit == 1) lines.Add("Không thể dùng trên ngựa");
            else if (horseLimit == 2) lines.Add("Chỉ dùng trên ngựa");
            return string.Join("\n", lines) + "\n";
        }

        public static bool IsPracticeOnlySkill(int skillId) => PracticeOnlySkillIds.Contains(skillId);

        private static readonly HashSet<int> PracticeOnlySkillIds = new(new[]
        {
            380,328,357,359,372,375,318,319,321,339,302,342,361,362,322,323,325,365,368,353,355,336,337,
            717,716,715,714,713,712,711,710,709,708,
            1055,1056,1057,1058,1059,1060,1066,1067,1069,1070,1071,1110,1061,1062,1114,1063,1065,
            1073,1074,1075,1076,1078,1079,1080,1081,
        });
    }
}
