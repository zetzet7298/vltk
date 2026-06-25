// -----------------------------------------------------------------------------
// VLTK Mobile — PC skills1.txt NPC/Boss skill catalog parser.
// Source of truth: /var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/settings/skills1.txt
// The committed npcskills.txt preserves the PC skills1.txt header plus rows where
// LvlSetScript starts with "\\script\\skill\\npc" or SkillName contains "boss".
// PC source proves 158 rows (145 NPC-script rows + 13 boss-name-only rows), not 43.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcNpcSkillParser
    {
        public const int SkillNameCol = 0;
        public const int PropertyCol = 1;
        public const int SkillIdCol = 2;
        public const int SkillStyleCol = 4;
        public const int SkillIconCol = 5;
        public const int AttackRadiusCol = 14;
        public const int MissilesFormCol = 19;
        public const int ChildSkillIdCol = 20;
        public const int ChildSkillLevelCol = 21;
        public const int ChildSkillNumCol = 22;
        public const int IsMeleeCol = 26;
        public const int SkillCostTypeCol = 30;
        public const int CostValueCol = 31;
        public const int TimePerCastCol = 32;
        public const int IsPhysicalCol = 34;
        public const int TargetOnlyCol = 35;
        public const int TargetEnemyCol = 36;
        public const int TargetAllyCol = 37;
        public const int TargetSelfCol = 38;
        public const int TargetOtherCol = 39;
        public const int TargetObjCol = 40;
        public const int TargetNoNpcCol = 41;
        public const int MaxLevelCol = 54;
        public const int HorseLimitCol = 56;
        public const int DoHurtCol = 57;
        public const int WeaponSkillCol = 58;
        public const int LvlSetScriptCol = 71;
        public const int PcSkills1ColumnCount = 115;

        public static List<PcNpcSkillEntry> ParseFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new List<PcNpcSkillEntry>();
            return ParseLines(PcText.ReadLinesTcvn3(path));
        }

        public static List<PcNpcSkillEntry> ParseLines(IReadOnlyList<string> lines)
        {
            var rows = new List<PcNpcSkillEntry>();
            if (lines == null || lines.Count == 0) return rows;

            bool headerSkipped = false;
            int sourceRowNumber = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped)
                {
                    headerSkipped = true;
                    continue;
                }

                sourceRowNumber++;
                var cols = line.Split('\t');
                var entry = ParseRow(cols, sourceRowNumber);
                if (entry != null) rows.Add(entry);
            }
            return rows;
        }

        public static PcNpcSkillRegistry BuildRegistry(string dir)
        {
            var reg = new PcNpcSkillRegistry();
            if (string.IsNullOrEmpty(dir)) return reg;
            string main = Directory.Exists(dir) ? Path.Combine(dir, "npcskills.txt") : dir;
            if (!File.Exists(main)) return reg;
            foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }

        private static PcNpcSkillEntry ParseRow(string[] cols, int sourceRowNumber)
        {
            if (cols == null || cols.Length <= LvlSetScriptCol) return null;
            int id = PcItemCommon.Int(cols, SkillIdCol);
            if (id <= 0) return null;

            string name = PcItemCommon.Str(cols, SkillNameCol);
            string script = PcItemCommon.Str(cols, LvlSetScriptCol);
            bool isNpcScript = script.StartsWith("\\script\\skill\\npc", StringComparison.OrdinalIgnoreCase);
            bool isBossName = name.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isNpcScript && !isBossName) return null;

            return new PcNpcSkillEntry
            {
                skillId = id,
                nameRaw = name,
                propertyRaw = PcItemCommon.Str(cols, PropertyCol),
                skillStyle = PcItemCommon.Int(cols, SkillStyleCol),
                skillIcon = PcItemCommon.Str(cols, SkillIconCol),
                attackRadius = PcItemCommon.Int(cols, AttackRadiusCol),
                missilesForm = PcItemCommon.Int(cols, MissilesFormCol),
                childSkillId = PcItemCommon.Int(cols, ChildSkillIdCol),
                childSkillLevel = PcItemCommon.Int(cols, ChildSkillLevelCol),
                childSkillNum = PcItemCommon.Int(cols, ChildSkillNumCol),
                timePerCast = PcItemCommon.Int(cols, TimePerCastCol),
                skillCostType = PcItemCommon.Int(cols, SkillCostTypeCol),
                costValue = PcItemCommon.Int(cols, CostValueCol),
                isPhysical = PcItemCommon.Int(cols, IsPhysicalCol) != 0,
                isMelee = PcItemCommon.Int(cols, IsMeleeCol) != 0,
                targetOnly = PcItemCommon.Int(cols, TargetOnlyCol) != 0,
                targetEnemy = PcItemCommon.Int(cols, TargetEnemyCol) != 0,
                targetAlly = PcItemCommon.Int(cols, TargetAllyCol) != 0,
                targetSelf = PcItemCommon.Int(cols, TargetSelfCol) != 0,
                targetOther = PcItemCommon.Int(cols, TargetOtherCol) != 0,
                targetObj = PcItemCommon.Int(cols, TargetObjCol) != 0,
                targetNoNpc = PcItemCommon.Int(cols, TargetNoNpcCol) != 0,
                horseLimit = PcItemCommon.Int(cols, HorseLimitCol),
                doHurt = PcItemCommon.Int(cols, DoHurtCol) != 0,
                weaponSkill = PcItemCommon.Int(cols, WeaponSkillCol) != 0,
                maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                levelSetScript = script,
                isNpcScript = isNpcScript,
                isBossName = isBossName,
                sourceColumnCount = cols.Length,
                sourceRowNumber = sourceRowNumber,
            };
        }
    }

    [Serializable]
    public class PcNpcSkillEntry
    {
        public int skillId;
        public string nameRaw;
        public string propertyRaw;
        public int skillStyle;
        public string skillIcon;
        public int attackRadius;
        public int missilesForm;
        public int childSkillId;
        public int childSkillLevel;
        public int childSkillNum;
        public int timePerCast;
        public int skillCostType;
        public int costValue;
        public bool isPhysical;
        public bool isMelee;
        public bool targetOnly;
        public bool targetEnemy;
        public bool targetAlly;
        public bool targetSelf;
        public bool targetOther;
        public bool targetObj;
        public bool targetNoNpc;
        public int horseLimit;
        public bool doHurt;
        public bool weaponSkill;
        public int maxLevel;
        public string levelSetScript;
        public bool isNpcScript;
        public bool isBossName;
        public int sourceColumnCount;
        public int sourceRowNumber;

        // Legacy fields kept so old NpcSkillService callers continue to compile.
        public int npcTemplateId;
        public int minNpcLevel;
        public int maxNpcLevel;
        public int damage;
        public int radius;
        public int coolDownMs;
    }

    public sealed class PcNpcSkillRegistry
    {
        private readonly Dictionary<int, PcNpcSkillEntry> _byId = new Dictionary<int, PcNpcSkillEntry>();
        private readonly Dictionary<int, List<PcNpcSkillEntry>> _byNpc = new Dictionary<int, List<PcNpcSkillEntry>>();
        private readonly List<PcNpcSkillEntry> _all = new List<PcNpcSkillEntry>();

        public int Count => _all.Count;
        public int NpcScriptCount { get; private set; }
        public int BossNameCount { get; private set; }
        public int BossNameOnlyCount => BossNameCount - BothNpcScriptAndBossNameCount;
        public int BothNpcScriptAndBossNameCount { get; private set; }
        public IReadOnlyList<PcNpcSkillEntry> All => _all;

        public void Register(PcNpcSkillEntry e)
        {
            if (e == null || e.skillId <= 0) return;
            if (_byId.ContainsKey(e.skillId)) return;
            _byId[e.skillId] = e;
            _all.Add(e);
            if (e.isNpcScript) NpcScriptCount++;
            if (e.isBossName) BossNameCount++;
            if (e.isNpcScript && e.isBossName) BothNpcScriptAndBossNameCount++;
            if (e.npcTemplateId > 0)
            {
                if (!_byNpc.TryGetValue(e.npcTemplateId, out var list))
                {
                    list = new List<PcNpcSkillEntry>();
                    _byNpc[e.npcTemplateId] = list;
                }
                list.Add(e);
            }
        }

        public PcNpcSkillEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcNpcSkillEntry> GetByNpcTemplate(int templateId)
            => _byNpc.TryGetValue(templateId, out var v)
                ? (IReadOnlyList<PcNpcSkillEntry>)v
                : (IReadOnlyList<PcNpcSkillEntry>)Array.Empty<PcNpcSkillEntry>();
    }
}
