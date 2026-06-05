// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings clientweaponskill.txt parser
// Source: clientweaponskill.txt (vũ khí skill client-side).
// Columns: WeaponType  SkillId  SkillName  RequiredLevel  IconPath
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcClientWeaponSkillParser
    {
        public const int WeaponTypeCol = 0;
        public const int SkillIdCol = 1;
        public const int SkillNameCol = 2;
        public const int RequiredLevelCol = 3;
        public const int IconPathCol = 4;

        public static List<PcClientWeaponSkillEntry> ParseFile(string path)
        {
            var rows = new List<PcClientWeaponSkillEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int w = PcItemCommon.Int(cols, WeaponTypeCol);
                if (w <= 0) continue;
                rows.Add(new PcClientWeaponSkillEntry
                {
                    weaponType = w,
                    skillId = PcItemCommon.Int(cols, SkillIdCol),
                    skillName = PcItemCommon.Str(cols, SkillNameCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    iconPath = PcItemCommon.Str(cols, IconPathCol),
                });
            }
            return rows;
        }

        public static PcClientWeaponSkillRegistry BuildRegistry(string dir)
        {
            var reg = new PcClientWeaponSkillRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcClientWeaponSkillEntry
    {
        public int weaponType;
        public int skillId;
        public string skillName;
        public int requiredLevel;
        public string iconPath;
    }

    public sealed class PcClientWeaponSkillRegistry
    {
        private readonly Dictionary<int, PcClientWeaponSkillEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcClientWeaponSkillEntry e) { if (e == null || e.weaponType <= 0) return; _byId[e.weaponType] = e; }
        public PcClientWeaponSkillEntry Get(int weaponType) => _byId.TryGetValue(weaponType, out var v) ? v : null;
        public IReadOnlyList<PcClientWeaponSkillEntry> GetByLevel(int level)
        {
            var list = new List<PcClientWeaponSkillEntry>();
            foreach (var e in _byId.Values)
                if (e.requiredLevel <= level) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcClientWeaponSkillEntry> All => new List<PcClientWeaponSkillEntry>(_byId.Values);
    }
}
