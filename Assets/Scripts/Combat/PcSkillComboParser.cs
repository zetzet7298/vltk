// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/skillcombo.txt Skill Combo parser
// Source: skillcombo.txt (Reference/PcSkill, tab-separated).
//   ComboId  Name  SkillSequence (semicolon)  RequiredPlayerLevel
//   RequiredClass  BonusEffect  Description
// Chuỗi kỹ năng (combo) — thực hiện theo trình tự sẽ kích hoạt bonus effect.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSkillComboParser
    {
        public const int ComboIdCol = 0;
        public const int NameCol = 1;
        public const int SkillSequenceCol = 2;
        public const int RequiredPlayerLevelCol = 3;
        public const int RequiredClassCol = 4;
        public const int BonusEffectCol = 5;
        public const int DescriptionCol = 6;

        public static List<PcSkillComboEntry> ParseFile(string path)
        {
            var rows = new List<PcSkillComboEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int id = PcItemCommon.Int(cols, ComboIdCol);
                if (id <= 0) continue;
                rows.Add(new PcSkillComboEntry
                {
                    comboId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    skillSequence = cols.Length > SkillSequenceCol ? PcItemCommon.Str(cols, SkillSequenceCol) : string.Empty,
                    requiredPlayerLevel = cols.Length > RequiredPlayerLevelCol ? PcItemCommon.Int(cols, RequiredPlayerLevelCol) : 0,
                    requiredClass = cols.Length > RequiredClassCol ? PcItemCommon.Int(cols, RequiredClassCol) : 0,
                    bonusEffect = cols.Length > BonusEffectCol ? PcItemCommon.Int(cols, BonusEffectCol) : 0,
                    description = cols.Length > DescriptionCol ? PcItemCommon.Str(cols, DescriptionCol) : string.Empty,
                });
            }
            return rows;
        }

        public static PcSkillComboRegistry BuildRegistry(string dir)
        {
            var reg = new PcSkillComboRegistry();
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
    public class PcSkillComboEntry
    {
        public int comboId;
        public string nameRaw;
        public string skillSequence; // semicolon separated
        public int requiredPlayerLevel;
        public int requiredClass;
        public int bonusEffect;
        public string description;

        public int[] GetSkillSequence()
        {
            if (string.IsNullOrEmpty(skillSequence)) return System.Array.Empty<int>();
            var parts = skillSequence.Split(';');
            var result = new List<int>(parts.Length);
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out var v) && v > 0) result.Add(v);
            }
            return result.ToArray();
        }
    }

    public sealed class PcSkillComboRegistry
    {
        private readonly Dictionary<int, PcSkillComboEntry> _byId = new();
        private readonly Dictionary<int, List<PcSkillComboEntry>> _byClass = new();
        private readonly Dictionary<int, List<PcSkillComboEntry>> _byLevel = new();
        public int Count => _byId.Count;

        public void Register(PcSkillComboEntry e)
        {
            if (e == null || e.comboId <= 0) return;
            _byId[e.comboId] = e;
            if (e.requiredClass > 0)
            {
                if (!_byClass.TryGetValue(e.requiredClass, out var byClass))
                {
                    byClass = new List<PcSkillComboEntry>();
                    _byClass[e.requiredClass] = byClass;
                }
                byClass.Add(e);
            }
            if (e.requiredPlayerLevel > 0)
            {
                if (!_byLevel.TryGetValue(e.requiredPlayerLevel, out var byLevel))
                {
                    byLevel = new List<PcSkillComboEntry>();
                    _byLevel[e.requiredPlayerLevel] = byLevel;
                }
                byLevel.Add(e);
            }
        }

        public PcSkillComboEntry Get(int comboId)
            => _byId.TryGetValue(comboId, out var v) ? v : null;

        public IReadOnlyList<PcSkillComboEntry> GetByClass(int cls)
            => _byClass.TryGetValue(cls, out var v) ? v : (IReadOnlyList<PcSkillComboEntry>)System.Array.Empty<PcSkillComboEntry>();

        public IReadOnlyList<PcSkillComboEntry> GetByLevel(int level)
            => _byLevel.TryGetValue(level, out var v) ? v : (IReadOnlyList<PcSkillComboEntry>)System.Array.Empty<PcSkillComboEntry>();

        public IReadOnlyList<PcSkillComboEntry> All
            => new List<PcSkillComboEntry>(_byId.Values);
    }
}
