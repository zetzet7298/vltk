// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/goldboss.txt (Boss Hoàng Kim) parser
// Source: goldboss.txt (boss spawn data, GB2312).
//   Col 0:  Name
//   Col 1:  PhysicalDamageBase (format "a|b")
//   Col 2:  PhysicalMagic
//   Col 3:  PoisonDamageBase
//   ...
//   Col 13: AuraSkillName
//   Col 14: AuraSkillLevel
// We keep name + skill name/level for runtime boss info.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcGoldBossEntry
    {
        public int bossId;
        public string name;
        public string auraSkillName;     // Tên kỹ năng nội
        public int auraSkillLevel;       // Cấp kỹ năng nội
        public string passiveSkillName;   // Tên kỹ năng bị động
        public int passiveSkillLevel;    // Cấp kỹ năng bị động
    }

    public sealed class PcGoldBossRegistry
    {
        private readonly Dictionary<int, PcGoldBossEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcGoldBossEntry e)
        {
            if (e == null) return;
            _byId[e.bossId] = e;
        }

        public PcGoldBossEntry Get(int id)
            => _byId.TryGetValue(id, out var v) ? v : null;

        public IEnumerable<PcGoldBossEntry> All => _byId.Values;
    }

    public static class PcGoldBossParser
    {
        public const int NameCol = 0;
        public const int AuraSkillNameCol = 11;
        public const int AuraSkillLevelCol = 12;
        public const int PassiveSkillNameCol = 13;
        public const int PassiveSkillLevelCol = 14;

        public static List<PcGoldBossEntry> ParseFile(string path)
        {
            var rows = new List<PcGoldBossEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            int autoId = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                autoId++;
                rows.Add(new PcGoldBossEntry
                {
                    bossId = autoId,
                    name = PcItemCommon.Str(cols, NameCol),
                    auraSkillName = cols.Length > AuraSkillNameCol ? PcItemCommon.Str(cols, AuraSkillNameCol) : "",
                    auraSkillLevel = cols.Length > AuraSkillLevelCol ? PcItemCommon.Int(cols, AuraSkillLevelCol) : 0,
                    passiveSkillName = cols.Length > PassiveSkillNameCol ? PcItemCommon.Str(cols, PassiveSkillNameCol) : "",
                    passiveSkillLevel = cols.Length > PassiveSkillLevelCol ? PcItemCommon.Int(cols, PassiveSkillLevelCol) : 0,
                });
            }
            return rows;
        }

        public static PcGoldBossRegistry BuildRegistry(string dir)
        {
            var reg = new PcGoldBossRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "goldboss*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
