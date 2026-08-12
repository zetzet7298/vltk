// -----------------------------------------------------------------------------
// VLTK Mobile — PC faction skill tree parser (Faction Skill Tree - 10 phái)
// Source: faction_skilltree.txt (Reference/PcFaction).
// Columns: FactionId  SkillId  Tier  RequiredLevel  RequiredPrevSkill
//          BonusType  BonusValue  IconPath
// Vietnamese: "Cây Kỹ Năng", "Tầng", "Yêu Cầu", "Học Trước".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcFactionSkillTreeParser
    {
        public const int FactionIdCol = 0;
        public const int SkillIdCol = 1;
        public const int TierCol = 2;
        public const int RequiredLevelCol = 3;
        public const int RequiredPrevSkillCol = 4;
        public const int BonusTypeCol = 5;
        public const int BonusValueCol = 6;
        public const int IconPathCol = 7;

        public static List<PcFactionSkillTreeEntry> ParseFile(string path)
        {
            var rows = new List<PcFactionSkillTreeEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int factionId = PcItemCommon.Int(cols, FactionIdCol);
                int skillId = PcItemCommon.Int(cols, SkillIdCol);
                if (factionId < 0 || skillId <= 0) continue;
                rows.Add(new PcFactionSkillTreeEntry
                {
                    factionId = factionId,
                    skillId = skillId,
                    tier = PcItemCommon.Int(cols, TierCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    requiredPrevSkill = PcItemCommon.Int(cols, RequiredPrevSkillCol),
                    bonusType = PcItemCommon.Int(cols, BonusTypeCol),
                    bonusValue = PcItemCommon.Int(cols, BonusValueCol),
                    iconPath = PcItemCommon.Str(cols, IconPathCol),
                });
            }
            return rows;
        }

        public static PcFactionSkillTreeRegistry BuildRegistry(string dir)
        {
            var reg = new PcFactionSkillTreeRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (string.Equals(ext, ".ini", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcFactionSkillTreeEntry
    {
        public int factionId;
        public int skillId;
        public int tier;
        public int requiredLevel;
        public int requiredPrevSkill;
        public int bonusType;
        public int bonusValue;
        public string iconPath;
    }

    public sealed class PcFactionSkillTreeRegistry
    {
        private readonly Dictionary<long, PcFactionSkillTreeEntry> _byKey = new();
        public int Count => _byKey.Count;

        private static long Key(int factionId, int skillId)
            => ((long)factionId << 32) | (uint)skillId;

        public void Register(PcFactionSkillTreeEntry e)
        {
            if (e == null || e.skillId <= 0) return;
            _byKey[Key(e.factionId, e.skillId)] = e;
        }

        public PcFactionSkillTreeEntry Get(int factionId, int skillId)
            => _byKey.TryGetValue(Key(factionId, skillId), out var v) ? v : null;

        public IReadOnlyList<PcFactionSkillTreeEntry> GetByFaction(int factionId)
        {
            var list = new List<PcFactionSkillTreeEntry>();
            foreach (var e in _byKey.Values)
                if (e.factionId == factionId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcFactionSkillTreeEntry> GetByTier(int factionId, int tier)
        {
            var list = new List<PcFactionSkillTreeEntry>();
            foreach (var e in _byKey.Values)
                if (e.factionId == factionId && e.tier == tier) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcFactionSkillTreeEntry> All => new List<PcFactionSkillTreeEntry>(_byKey.Values);
    }
}
