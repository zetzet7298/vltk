using System.Collections.Generic;
using System.Linq;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct PcSkillPanelRow
    {
        public readonly int skillId;
        public readonly string displayName;
        public readonly int requiredLevel;
        public readonly int maxLevel;
        public readonly int learnedLevel;
        public readonly int levelCap;
        public readonly bool canUpgrade;
        public readonly string summary;
        public readonly string nextLevelSummary;
        public readonly string upgradeStatus;

        public PcSkillPanelRow(int skillId, string displayName, int requiredLevel, int maxLevel, int learnedLevel, int levelCap, bool canUpgrade, string summary, string nextLevelSummary, string upgradeStatus)
        {
            this.skillId = skillId;
            this.displayName = displayName;
            this.requiredLevel = requiredLevel;
            this.maxLevel = maxLevel;
            this.learnedLevel = learnedLevel;
            this.levelCap = levelCap;
            this.canUpgrade = canUpgrade;
            this.summary = summary;
            this.nextLevelSummary = nextLevelSummary;
            this.upgradeStatus = upgradeStatus;
        }
    }

    public sealed class PcSkillPanelSnapshot
    {
        public int playerLevel;
        public int skillPoints;
        public CombatFaction faction;
        public int selectedSkillId;
        public PcSkillPanelRow? selectedRow;
        public IReadOnlyList<PcSkillPanelRow> rows;
    }

    public static class PcSkillPanelService
    {
        public const int PcFightSkillSlotsPerPage = 30;
        public const int PcFightSkillPageCount = 1;

        // PC source: bin/client/script/skill/gaibang.lua
        // 115-116 passive mastery, 117-130 active combat skills, 274/357/358/359/360 high-tier
        // 358 (Kháng Long Hữu Hối) exists in PC gaibang.lua but commented out in client script;
        // included here so 5-slot default deck can pick it.
        public static readonly int[] PcCaiBangSkillOrder =
        {
            115, 116, 117, 118, 119,
            120, 121, 122, 123, 124,
            125, 126, 127, 128, 129,
            130, 274, 277, 357, 358,
            359, 360, 714, 720, 1073, 1074,
        };

        public static readonly int[] PcWuDangSkillOrder =
        {
            151, 152, 153, 154, 155,
            156, 157, 158, 159, 160,
            161, 162, 163, 164, 165,
            166,
        };

        public static readonly int[] PcShaolinSkillOrder =
        {
            3, 4, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21
        };

        public static readonly int[] PcTangMenSkillOrder =
        {
            43, 45, 47, 48, 50, 51, 54, 55, 57, 58
        };

        public static readonly int[] PcEMeiSkillOrder =
        {
            // PC EMei has 15 core skills (skill 90 reassigned to KunLun per PC emei.lua/kunlun.lua)
            77, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 91, 92, 93
        };

        public static readonly int[] PcTianWangSkillOrder =
        {
            23, 24, 26, 29, 30, 31, 32, 33, 34, 35, 36, 37, 40, 41, 42
        };

        public static readonly int[] PcWuDuSkillOrder =
        {
            60, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76
        };

        public static readonly int[] PcCuiYanSkillOrder =
        {
            95, 97, 99, 100, 101, 102, 103, 105, 108, 109, 111, 113, 114
        };

        public static readonly int[] PcTianRenSkillOrder =
        {
            131, 132, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150
        };

        public static readonly int[] PcKunLunSkillOrder =
        {
            167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184
        };

        // NPC variant filter: delegate to PlayerProgressionState.IsNpcVariant (single source of truth).
        public const int NpcVariantSkillId = 1539; // kept for external callers in CombatSkillSlotController
        public static bool IsNpcVariant(int skillId) => PlayerProgressionState.IsNpcVariant(skillId);

        public static IReadOnlyList<int> GetPcSkillOrder(CombatFaction faction)
        {
            if (faction == CombatFaction.WuDang)
                return PcWuDangSkillOrder;
            if (faction == CombatFaction.Shaolin)
                return PcShaolinSkillOrder;
            if (faction == CombatFaction.TangMen)
                return PcTangMenSkillOrder;
            if (faction == CombatFaction.EMei)
                return PcEMeiSkillOrder;
            if (faction == CombatFaction.TianWang)
                return PcTianWangSkillOrder;
            if (faction == CombatFaction.WuDu)
                return PcWuDuSkillOrder;
            if (faction == CombatFaction.CuiYan)
                return PcCuiYanSkillOrder;
            if (faction == CombatFaction.TianRen)
                return PcTianRenSkillOrder;
            if (faction == CombatFaction.KunLun)
                return PcKunLunSkillOrder;
            return PcCaiBangSkillOrder;
        }

        public static PcSkillPanelSnapshot Build(SkillCatalog catalog, PlayerProgressionState progression, int selectedSkillId = 0)
        {
            var skillOrder = GetPcSkillOrder(progression.faction);
            return BuildForOrder(catalog, progression, selectedSkillId, skillOrder);
        }

        public static PcSkillPanelSnapshot BuildPage(SkillCatalog catalog, PlayerProgressionState progression, int selectedSkillId, int pageIndex)
        {
            var skillOrder = GetPcSkillOrder(progression.faction);
            return BuildForOrder(catalog, progression, selectedSkillId, skillOrder);
        }

        private static PcSkillPanelSnapshot BuildForOrder(SkillCatalog catalog, PlayerProgressionState progression, int selectedSkillId, IReadOnlyList<int> skillOrder)
        {
            progression ??= new PlayerProgressionState();
            PcSkillPanelRow? selected = null;
            var rows = new List<PcSkillPanelRow>();
            if (catalog != null)
            {
                foreach (var skillId in skillOrder)
                {
                    if (IsNpcVariant(skillId))
                        continue;
                    var skill = catalog.Resolve(skillId);
                    if (skill == null)
                        continue;
                    int learned = progression.GetSkillLevel(skill.skillId);
                    int cap = progression.GetLevelCap(skill);
                    bool canUpgrade = progression.CanUpgradeSkill(skill);
                    var row = new PcSkillPanelRow(
                        skill.skillId,
                        skill.DisplayName,
                        skill.reqLevel,
                        skill.maxLevel,
                        learned,
                        cap,
                        canUpgrade,
                        Describe(skill, learned),
                        DescribeNext(skill, learned),
                        UpgradeStatus(skill, progression, learned, cap, canUpgrade));
                    rows.Add(row);
                    if (skill.skillId == selectedSkillId)
                        selected = row;
                }
            }
            return new PcSkillPanelSnapshot
            {
                playerLevel = progression.level,
                skillPoints = progression.fightSkillPoints,
                faction = progression.faction,
                selectedSkillId = selected.HasValue ? selectedSkillId : 0,
                selectedRow = selected,
                rows = rows,
            };
        }

        public static bool TryUpgrade(PlayerProgressionState progression, SkillCatalog catalog, int skillId)
        {
            if (progression == null || catalog == null)
                return false;
            var skill = catalog.Resolve(skillId);
            return progression.TryUpgradeSkill(skill);
        }

        private static string Describe(SkillDefinition skill, int learnedLevel)
        {
            int level = learnedLevel > 0 ? learnedLevel : 1;
            return BuildPcLikeDescription(skill, level, learnedLevel, includeCurrentLevel: true);
        }

        private static string DescribeNext(SkillDefinition skill, int learnedLevel)
        {
            int nextLevel = learnedLevel + 1;
            if (skill.maxLevel > 0 && nextLevel > skill.maxLevel)
                return string.Empty;
            return BuildPcLikeDescription(skill, nextLevel, learnedLevel, includeCurrentLevel: false);
        }

        private static string UpgradeStatus(SkillDefinition skill, PlayerProgressionState progression, int learnedLevel, int levelCap, bool canUpgrade)
        {
            if (canUpgrade)
                return "Có thể bấm dấu + để tăng 1 cấp";
            if (progression.fightSkillPoints <= 0)
                return "Không còn điểm kỹ năng";
            if (learnedLevel >= skill.maxLevel)
                return "Kỹ năng đã đạt giới hạn";
            int want = learnedLevel + 1;
            int needPlayerLevel = skill.reqLevel - 1 + want;
            if (want > levelCap)
                return $"Cần cấp nhân vật {needPlayerLevel}";
            return "Chưa đủ điều kiện tăng";
        }

        private static string BuildPcLikeDescription(SkillDefinition skill, int level, int learnedLevel, bool includeCurrentLevel)
        {
            var data = skill.GetPcLevelData(level);
            string kind = skill.skillStyle switch
            {
                PcSkillStyle.PassivityNpcState => "Bị động",
                PcSkillStyle.InitiativeNpcState => skill.isAura ? "Trận pháp" : "Hỗ trợ",
                _ => skill.targetEnemy ? "Tấn công" : "Hỗ trợ",
            };
            var parts = new List<string>
            {
                kind,
                $"Yêu cầu cấp {skill.reqLevel}",
            };
            if (includeCurrentLevel)
                parts.Add($"Cấp hiện tại {learnedLevel}");
            if (skill.cost > 0)
                parts.Add($"Tiêu hao {skill.cost}");
            if (skill.attackRadius > 0)
                parts.Add($"Phạm vi {skill.attackRadius}");
            var attrs = data?.AllAttributes()
                .Where(a => a.kind != MagicAttributeKind.SkillCostV)
                .Take(4)
                .Select(a => $"{a.kind} {a.value1},{a.value2},{a.value3}");
            if (attrs != null)
                parts.AddRange(attrs);
            return string.Join("\n", parts);
        }
    }
}
