// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/specialskills.txt Kỹ Năng Đặc Biệt parser
// Source: specialskills.txt (58 entries, GB2312, tab-separated).
//   SkillId  SkillName  FactionId  SkillType  ManaCost
//   CoolDownMs  ReqLevel  Icon  ScriptFile
// Skill đặc biệt = skill cuối cấp / skill nội công môn phái, gắn với 1 phái.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSpecialSkillParser
    {
        public const int SkillIdCol = 0;
        public const int SkillNameCol = 1;
        public const int FactionIdCol = 2;
        public const int SkillTypeCol = 3;
        public const int ManaCostCol = 4;
        public const int CoolDownCol = 5;
        public const int ReqLevelCol = 6;
        public const int IconCol = 7;
        public const int ScriptCol = 8;

        public static List<PcSpecialSkillEntry> ParseFile(string path)
        {
            var rows = new List<PcSpecialSkillEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                int id = PcItemCommon.Int(cols, SkillIdCol);
                if (id <= 0) continue;
                rows.Add(new PcSpecialSkillEntry
                {
                    skillId = id,
                    nameRaw = PcItemCommon.Str(cols, SkillNameCol),
                    factionId = PcItemCommon.Int(cols, FactionIdCol),
                    skillType = PcItemCommon.Int(cols, SkillTypeCol),
                    manaCost = PcItemCommon.Int(cols, ManaCostCol),
                    coolDownMs = cols.Length > CoolDownCol ? PcItemCommon.Int(cols, CoolDownCol) : 0,
                    reqLevel = cols.Length > ReqLevelCol ? PcItemCommon.Int(cols, ReqLevelCol) : 0,
                    icon = cols.Length > IconCol ? PcItemCommon.Str(cols, IconCol) : string.Empty,
                    scriptFile = cols.Length > ScriptCol ? PcItemCommon.Str(cols, ScriptCol) : string.Empty,
                });
            }
            return rows;
        }

        public static PcSpecialSkillRegistry BuildRegistry(string dir)
        {
            var reg = new PcSpecialSkillRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "specialskills.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcSpecialSkillEntry
    {
        public int skillId;
        public string nameRaw;
        public int factionId;
        public int skillType;
        public int manaCost;
        public int coolDownMs;
        public int reqLevel;
        public string icon;
        public string scriptFile;
    }

    public sealed class PcSpecialSkillRegistry
    {
        private readonly Dictionary<int, PcSpecialSkillEntry> _byId = new();
        private readonly Dictionary<int, List<PcSpecialSkillEntry>> _byFaction = new();
        public int Count => _byId.Count;
        public void Register(PcSpecialSkillEntry e)
        {
            if (e == null || e.skillId <= 0) return;
            _byId[e.skillId] = e;
            if (!_byFaction.TryGetValue(e.factionId, out var list))
            {
                list = new List<PcSpecialSkillEntry>();
                _byFaction[e.factionId] = list;
            }
            list.Add(e);
        }
        public PcSpecialSkillEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcSpecialSkillEntry> GetByFaction(int factionId)
            => _byFaction.TryGetValue(factionId, out var v)
                ? (IReadOnlyList<PcSpecialSkillEntry>)v
                : (IReadOnlyList<PcSpecialSkillEntry>)System.Array.Empty<PcSpecialSkillEntry>();
    }
}
