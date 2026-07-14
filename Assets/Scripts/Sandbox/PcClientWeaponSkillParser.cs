// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/clientweaponskill.txt focused parser.
// Source: /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/clientweaponskill.txt
// Columns: Id  WeaponType  SkillId
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcClientWeaponSkillParser
    {
        public const int IdCol = 0;
        public const int WeaponTypeCol = 1;
        public const int SkillIdCol = 2;

        public static List<PcClientWeaponSkillEntry> ParseFile(string path)
        {
            var rows = new List<PcClientWeaponSkillEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcText.ReadLinesTcvn3(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length <= SkillIdCol) continue;
                int id = PcItemCommon.Int(cols, IdCol);
                int skillId = PcItemCommon.Int(cols, SkillIdCol);
                if (id <= 0 || skillId <= 0) continue;
                rows.Add(new PcClientWeaponSkillEntry
                {
                    id = id,
                    weaponType = id,
                    weaponTypeName = PcItemCommon.Str(cols, WeaponTypeCol),
                    skillId = skillId,
                    skillName = PcItemCommon.Str(cols, WeaponTypeCol),
                });
            }
            return rows;
        }

        public static PcClientWeaponSkillRegistry BuildRegistry(string dir)
        {
            var reg = new PcClientWeaponSkillRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string path = Path.Combine(dir, "clientweaponskill.txt");
            foreach (var s in ParseFile(path)) reg.Register(s);
            reg.LinkSkillScripts(Path.Combine(dir, "skills.txt"));
            return reg;
        }
    }

    [System.Serializable]
    public class PcClientWeaponSkillEntry
    {
        public int id;
        public int weaponType;
        public string weaponTypeName;
        public int skillId;
        public string skillName;
        public int requiredLevel;
        public string iconPath;
        public string lvlSetScript;
    }

    public sealed class PcClientWeaponSkillRegistry
    {
        private readonly Dictionary<int, PcClientWeaponSkillEntry> _byId = new();
        private readonly Dictionary<int, List<PcClientWeaponSkillEntry>> _bySkillId = new();
        public int Count => _byId.Count;
        public IReadOnlyList<PcClientWeaponSkillEntry> All => new List<PcClientWeaponSkillEntry>(_byId.Values);

        public void Register(PcClientWeaponSkillEntry e)
        {
            if (e == null || e.id <= 0) return;
            _byId[e.id] = e;
            if (!_bySkillId.TryGetValue(e.skillId, out var list))
            {
                list = new List<PcClientWeaponSkillEntry>();
                _bySkillId[e.skillId] = list;
            }
            list.Add(e);
        }

        public PcClientWeaponSkillEntry Get(int weaponType) => _byId.TryGetValue(weaponType, out var v) ? v : null;
        public IReadOnlyList<PcClientWeaponSkillEntry> GetBySkillId(int skillId)
            => _bySkillId.TryGetValue(skillId, out var list) ? list : System.Array.Empty<PcClientWeaponSkillEntry>();

        public IReadOnlyList<PcClientWeaponSkillEntry> GetByLevel(int level)
        {
            var list = new List<PcClientWeaponSkillEntry>();
            foreach (var e in _byId.Values)
                if (e.requiredLevel <= level) list.Add(e);
            return list;
        }

        public void LinkSkillScripts(string skillsTxtPath)
        {
            var scripts = PcSkillSourceLinkParser.ParseSkillScripts(skillsTxtPath);
            foreach (var entry in _byId.Values)
                if (scripts.TryGetValue(entry.skillId, out var script)) entry.lvlSetScript = script;
        }
    }
}
