// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/skillupgrade.txt Skill Upgrade Path parser
// Source: skillupgrade.txt (Reference/PcSkill, tab-separated).
//   SkillId  RequiredPrevSkill  RequiredPlayerLevel  RequiredSkillPoints
//   RequiredReputation  ResultSkillId  UpgradeType
// UpgradeType: 0=tăng cường, 1=tiến hóa, 2=siêu việt.
// Quy tắc nâng cấp kỹ năng: tiền điều kiện (kỹ năng trước, cấp nhân vật,
// điểm kỹ năng, danh vọng) → kỹ năng kết quả.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSkillUpgradeParser
    {
        public const int SkillIdCol = 0;
        public const int RequiredPrevSkillCol = 1;
        public const int RequiredPlayerLevelCol = 2;
        public const int RequiredSkillPointsCol = 3;
        public const int RequiredReputationCol = 4;
        public const int ResultSkillIdCol = 5;
        public const int UpgradeTypeCol = 6;

        public static List<PcSkillUpgradeEntry> ParseFile(string path)
        {
            var rows = new List<PcSkillUpgradeEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int id = PcItemCommon.Int(cols, SkillIdCol);
                if (id <= 0) continue;
                rows.Add(new PcSkillUpgradeEntry
                {
                    skillId = id,
                    requiredPrevSkill = cols.Length > RequiredPrevSkillCol ? PcItemCommon.Int(cols, RequiredPrevSkillCol) : 0,
                    requiredPlayerLevel = cols.Length > RequiredPlayerLevelCol ? PcItemCommon.Int(cols, RequiredPlayerLevelCol) : 0,
                    requiredSkillPoints = cols.Length > RequiredSkillPointsCol ? PcItemCommon.Int(cols, RequiredSkillPointsCol) : 0,
                    requiredReputation = cols.Length > RequiredReputationCol ? PcItemCommon.Int(cols, RequiredReputationCol) : 0,
                    resultSkillId = cols.Length > ResultSkillIdCol ? PcItemCommon.Int(cols, ResultSkillIdCol) : 0,
                    upgradeType = cols.Length > UpgradeTypeCol ? PcItemCommon.Int(cols, UpgradeTypeCol) : 0,
                });
            }
            return rows;
        }

        public static PcSkillUpgradeRegistry BuildRegistry(string dir)
        {
            var reg = new PcSkillUpgradeRegistry();
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
    public class PcSkillUpgradeEntry
    {
        public int skillId;
        public int requiredPrevSkill;
        public int requiredPlayerLevel;
        public int requiredSkillPoints;
        public int requiredReputation;
        public int resultSkillId;
        public int upgradeType;
    }

    public sealed class PcSkillUpgradeRegistry
    {
        private readonly Dictionary<int, PcSkillUpgradeEntry> _byId = new();
        private readonly Dictionary<int, List<PcSkillUpgradeEntry>> _byRequired = new();
        public int Count => _byId.Count;

        public void Register(PcSkillUpgradeEntry e)
        {
            if (e == null || e.skillId <= 0) return;
            _byId[e.skillId] = e;
            if (e.requiredPrevSkill > 0)
            {
                if (!_byRequired.TryGetValue(e.requiredPrevSkill, out var list))
                {
                    list = new List<PcSkillUpgradeEntry>();
                    _byRequired[e.requiredPrevSkill] = list;
                }
                list.Add(e);
            }
        }

        public PcSkillUpgradeEntry Get(int skillId)
            => _byId.TryGetValue(skillId, out var v) ? v : null;

        public IReadOnlyList<PcSkillUpgradeEntry> GetByRequiredSkill(int skillId)
        {
            return _byRequired.TryGetValue(skillId, out var v) ? v : (IReadOnlyList<PcSkillUpgradeEntry>)System.Array.Empty<PcSkillUpgradeEntry>();
        }

        public IReadOnlyList<PcSkillUpgradeEntry> All
            => new List<PcSkillUpgradeEntry>(_byId.Values);
    }
}
