using System.Collections.Generic;
using System.Linq;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct CaiBangSkillPanelRow
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

        public CaiBangSkillPanelRow(int skillId, string displayName, int requiredLevel, int maxLevel, int learnedLevel, int levelCap, bool canUpgrade, string summary, string nextLevelSummary, string upgradeStatus)
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

    public sealed class CaiBangSkillPanelSnapshot
    {
        public int playerLevel;
        public int skillPoints;
        public CombatFaction faction;
        public int selectedSkillId;
        public CaiBangSkillPanelRow? selectedRow;
        public IReadOnlyList<CaiBangSkillPanelRow> rows;
    }

    /// <summary>Builds the Cái Bang skill panel from the same PC-derived combat catalog used by runtime combat.</summary>
    public static class CaiBangSkillPanelService
    {
        // PC UI: ui3/战斗技能细分页.ini defines 25 skill slots per combat subpage (5x5).
        // Mobile: use one scrollable list of all 23 Cái Bang player skills (16 PC gốc + 7 MOD).
        // The 25-slot grid wraps rows so the ScrollView shows everything via vertical scroll.
        public const int PcFightSkillSlotsPerPage = 30;
        public const int PcFightSkillPageCount = 1;

        // All 23 Cái Bang player skills in PC display order. MOD-only additions appended.
        public static readonly int[] PcCaiBangSkillOrder =
        {
            115, 116, 117, 118, 119,
            120, 121, 122, 123, 124,
            125, 126, 127, 128, 129,
            130, 274, 277, 357, 359,
            360, 1073, 1074,
        };

        // 1539 = Thiên Hạ Vô Cẩu NPC variant (ReqLevel 1, MaxLevel 60). MOD-only boss skill
        // registered in the catalog for boss AI, but NOT shown in the player skill panel.
        public const int NpcVariantSkillId = 1539;

        public static IReadOnlyList<int> GetPcCaiBangSkillOrderForPage(int pageIndex)
        {
            return PcCaiBangSkillOrder;
        }

        public static CaiBangSkillPanelSnapshot Build(SkillCatalog catalog, PlayerProgressionState progression, int selectedSkillId = 0)
        {
            return BuildForOrder(catalog, progression, selectedSkillId, PcCaiBangSkillOrder);
        }

        public static CaiBangSkillPanelSnapshot BuildPage(SkillCatalog catalog, PlayerProgressionState progression, int selectedSkillId, int pageIndex)
        {
            return BuildForOrder(catalog, progression, selectedSkillId, PcCaiBangSkillOrder);
        }

        private static CaiBangSkillPanelSnapshot BuildForOrder(SkillCatalog catalog, PlayerProgressionState progression, int selectedSkillId, IReadOnlyList<int> skillOrder)
        {
            progression ??= new PlayerProgressionState();
            CaiBangSkillPanelRow? selected = null;
            var rows = new List<CaiBangSkillPanelRow>();
            if (catalog != null)
            {
                foreach (var skillId in skillOrder)
                {
                    // Skip NPC/boss variant - registered in catalog for boss AI but not for player panel.
                    if (skillId == NpcVariantSkillId)
                        continue;
                    var skill = catalog.Resolve(skillId);
                    if (skill == null)
                        continue;
                    int learned = progression.GetSkillLevel(skill.skillId);
                    int cap = progression.GetLevelCap(skill);
                    bool canUpgrade = progression.CanUpgradeSkill(skill);
                    var row = new CaiBangSkillPanelRow(
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
            return new CaiBangSkillPanelSnapshot
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
