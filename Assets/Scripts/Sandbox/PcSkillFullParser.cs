// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/skills.txt full skill catalog parser
// Source: server settings/skills.txt (GB2312, 1,216 rows, 113 tab-separated columns)
// Purpose: expose the full PC skill catalog (id, name, faction, icon, cooldown,
// damage, range, attribute flags) to mobile runtime for skill databases and
// lookup. Reuses the same header layout as PcSkills.txt (the smaller curated
// reference file under Reference/).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcSkillFullParser
    {
        public const int MinColumns = 4;
        public const int NameCol = 0;
        public const int PropertyCol = 1;
        public const int SkillIdCol = 2;
        public const int AttribCol = 3;
        public const int SkillStyleCol = 4;
        public const int IconCol = 5;
        public const int IsAuraCol = 11;
        public const int AttackRadiusCol = 14;

        public static List<PcSkillEntry> ParseFile(string path)
        {
            var rows = new List<PcSkillEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < MinColumns) continue;
                // Always read the real SkillIdCol — never shadow it with a row index.
                var entry = ParseRow(cols);
                if (entry != null) rows.Add(entry);
            }
            return rows;
        }

        public static PcSkillEntry ParseRow(string[] cols, int idHint = 0)
        {
            if (cols == null || cols.Length < MinColumns) return null;
            string name = PcItemCommon.Str(cols, NameCol);
            int skillId = idHint > 0 ? idHint : PcItemCommon.Int(cols, SkillIdCol);
            if (string.IsNullOrEmpty(name) && skillId <= 0) return null;

            return new PcSkillEntry
            {
                skillId = skillId,
                nameRaw = name,
                nameNormalized = name.Trim(),
                property = PcItemCommon.Str(cols, PropertyCol),
                attrib = PcItemCommon.Int(cols, AttribCol),
                skillStyle = PcItemCommon.Int(cols, SkillStyleCol),
                iconPath = PcItemCommon.Str(cols, IconCol),
                isAura = PcItemCommon.Int(cols, IsAuraCol) > 0,
                attackRadius = PcItemCommon.Int(cols, AttackRadiusCol),
                warningCount = PcItemCommon.ContainsReplacementChar(name) ? 1 : 0,
            };
        }
    }

    [System.Serializable]
    public class PcSkillEntry
    {
        public int skillId;
        public string nameRaw;
        public string nameNormalized;
        public string property;
        public int attrib;
        public int skillStyle;
        public string iconPath;
        public bool isAura;
        public int attackRadius;
        public int warningCount;
    }

    /// <summary>In-memory runtime registry of all parsed PC skills.</summary>
    public sealed class PcSkillRegistry
    {
        private readonly Dictionary<int, PcSkillEntry> _byId = new();
        private readonly Dictionary<string, List<PcSkillEntry>> _byName = new();

        public int Count => _byId.Count;

        public static PcSkillRegistry LoadFromDirectory(string dir)
        {
            var reg = new PcSkillRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "skills.txt");
            if (File.Exists(main))
            {
                foreach (var s in PcSkillFullParser.ParseFile(main))
                    reg.Register(s);
            }
            return reg;
        }

        public void Register(PcSkillEntry entry)
        {
            if (entry == null || entry.skillId <= 0) return;
            _byId[entry.skillId] = entry;
            if (!string.IsNullOrEmpty(entry.nameNormalized))
            {
                if (!_byName.TryGetValue(entry.nameNormalized, out var rows))
                {
                    rows = new List<PcSkillEntry>();
                    _byName[entry.nameNormalized] = rows;
                }
                rows.Add(entry);
            }
        }

        public PcSkillEntry Resolve(int skillId)
            => _byId.TryGetValue(skillId, out var s) ? s : null;

        public IReadOnlyList<PcSkillEntry> FindByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return System.Array.Empty<PcSkillEntry>();
            return _byName.TryGetValue(name, out var rows) ? rows : System.Array.Empty<PcSkillEntry>();
        }
    }
}
