// -----------------------------------------------------------------------------
// VLTK Mobile — Skill Tree Panel Service (Cây kỹ năng)
// UI service: dựng cây kỹ năng theo môn phái, học/kích hoạt, điều kiện tiên quyết.
// PC reference: skill/<faction>/... + PlayerProgressionState.knownSkills.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>Một nút trong cây kỹ năng.</summary>
    public readonly struct SkillTreeRow
    {
        public readonly int skillId;
        public readonly string skillName;
        public readonly int parentId;   // 0 = root
        public readonly int tier;       // 0..n tier độ sâu
        public readonly int column;     // vị trí cột trong cây
        public readonly bool isUnlocked;
        public readonly bool isLearned;
        public readonly bool isActive;
        public readonly bool prereqMet;
        public readonly int reqLevel;
        public readonly int reqSkillId;
        public readonly string iconPath;

        public SkillTreeRow(int skillId, string skillName, int parentId, int tier, int column, bool isUnlocked, bool isLearned, bool isActive, bool prereqMet, int reqLevel, int reqSkillId, string iconPath)
        {
            this.skillId = skillId;
            this.skillName = skillName ?? string.Empty;
            this.parentId = parentId;
            this.tier = tier;
            this.column = column;
            this.isUnlocked = isUnlocked;
            this.isLearned = isLearned;
            this.isActive = isActive;
            this.prereqMet = prereqMet;
            this.reqLevel = reqLevel;
            this.reqSkillId = reqSkillId;
            this.iconPath = iconPath ?? string.Empty;
        }
    }

    /// <summary>Snapshot toàn bộ cây kỹ năng.</summary>
    public sealed class SkillTreeSnapshot
    {
        public int playerId;
        public int playerLevel;
        public CombatFaction faction;
        public int totalSkills;
        public int learnedSkills;
        public int activeSkillId;
        public IReadOnlyList<SkillTreeRow> rows;
    }

    /// <summary>Dịch vụ UI: panel cây kỹ năng.</summary>
    public static class SkillTreePanelService
    {
        public const string Title = "Cây Kỹ Năng";
        public const string LabelLearn = "Học";
        public const string LabelActivate = "Kích hoạt";
        public const string LabelRequiredLevel = "Cấp yêu cầu";
        public const string LabelPrereq = "Kỹ năng tiên quyết";

        /// <summary>Thứ tự skill mặc định theo môn phái (mỗi phái 18-20 skill).</summary>
        public static IReadOnlyList<int> GetPcSkillTreeOrder(CombatFaction faction)
        {
            if (faction == CombatFaction.WuDang) return PcWuDangSkillOrder;
            if (faction == CombatFaction.Shaolin) return PcShaolinSkillOrder;
            if (faction == CombatFaction.TangMen) return PcTangMenSkillOrder;
            if (faction == CombatFaction.EMei) return PcEMeiSkillOrder;
            if (faction == CombatFaction.TianWang) return PcTianWangSkillOrder;
            if (faction == CombatFaction.WuDu) return PcWuDuSkillOrder;
            if (faction == CombatFaction.CuiYan) return PcCuiYanSkillOrder;
            if (faction == CombatFaction.TianRen) return PcTianRenSkillOrder;
            if (faction == CombatFaction.KunLun) return PcKunLunSkillOrder;
            return PcCaiBangSkillOrder;
        }

        // Tái sử dụng danh sách skill từ PcSkillPanelService
        public static readonly int[] PcCaiBangSkillOrder = { 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 274, 277 };
        public static readonly int[] PcWuDangSkillOrder = { 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166 };
        public static readonly int[] PcShaolinSkillOrder = { 3, 4, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 };
        public static readonly int[] PcTangMenSkillOrder = { 43, 45, 47, 48, 50, 51, 54, 55, 57, 58 };
        public static readonly int[] PcEMeiSkillOrder = { 77, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93 };
        public static readonly int[] PcTianWangSkillOrder = { 23, 24, 26, 29, 30, 31, 32, 33, 34, 35, 36, 37, 40, 41, 42 };
        public static readonly int[] PcWuDuSkillOrder = { 60, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76 };
        public static readonly int[] PcCuiYanSkillOrder = { 95, 97, 99, 100, 101, 102, 103, 105, 108, 109, 111, 113, 114 };
        public static readonly int[] PcTianRenSkillOrder = { 131, 132, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150 };
        public static readonly int[] PcKunLunSkillOrder = { 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184 };

        /// <summary>Dựng snapshot cây kỹ năng.</summary>
        public static SkillTreeSnapshot BuildSnapshot(SkillCatalog catalog, PlayerProgressionState prog, int selectedSkillId = 0)
        {
            var order = GetPcSkillTreeOrder(prog != null ? prog.faction : CombatFaction.None);
            var rows = new List<SkillTreeRow>();
            int learned = 0;
            for (int i = 0; i < order.Count; i++)
            {
                int skillId = order[i];
                int tier = i / 5;
                int col = i % 5;
                int parentId = i == 0 ? 0 : order[Math.Max(0, i - 1)];
                int reqLevel = 10 + i * 5;
                int reqSkill = i == 0 ? 0 : order[Math.Max(0, i - 1)];
                bool prereqMet = prog == null || prog.level >= reqLevel;
                bool isLearned = prog != null && prog.knownSkills != null && prog.knownSkills.Contains(skillId);
                if (isLearned) learned++;
                rows.Add(new SkillTreeRow(
                    skillId: skillId,
                    skillName: $"Skill {skillId}",
                    parentId: parentId,
                    tier: tier,
                    column: col,
                    isUnlocked: prereqMet,
                    isLearned: isLearned,
                    isActive: selectedSkillId == skillId,
                    prereqMet: prereqMet,
                    reqLevel: reqLevel,
                    reqSkillId: reqSkill,
                    iconPath: $"UI/Skills/icon_{skillId}.png"));
            }
            return new SkillTreeSnapshot
            {
                playerId = 0,
                playerLevel = prog != null ? prog.level : 1,
                faction = prog != null ? prog.faction : CombatFaction.None,
                totalSkills = order.Count,
                learnedSkills = learned,
                activeSkillId = selectedSkillId,
                rows = rows,
            };
        }

        /// <summary>Kiểm tra điều kiện học skill.</summary>
        public static bool CanLearn(SkillTreeRow row, PlayerProgressionState prog)
        {
            if (prog == null) return false;
            if (prog.level < row.reqLevel) return false;
            if (prog.fightSkillPoints <= 0) return false;
            if (row.reqSkillId > 0)
            {
                if (prog.skillLevels == null) return false;
                if (!prog.skillLevels.TryGetValue(row.reqSkillId, out var lvl) || lvl <= 0) return false;
            }
            return true;
        }

        /// <summary>Thử học skill (luôn false ở stub — cần ghi vào progression state).</summary>
        public static bool TryLearnSkill(int playerId, int skillId)
        {
            if (playerId <= 0 || skillId <= 0) return false;
            return false;
        }

        /// <summary>Thử kích hoạt skill (luôn false ở stub — cần ghi vào combat state).</summary>
        public static bool TryActivateSkill(int playerId, int skillId)
        {
            if (playerId <= 0 || skillId <= 0) return false;
            return false;
        }
    }
}
