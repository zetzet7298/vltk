// -----------------------------------------------------------------------------
// VLTK Mobile — PC permitdialognpc_info.txt parser
// Source: settings/permitdialognpc_info.txt (GB2312). Mỗi dòng tab-separated:
// NPCName, MapId, TireLimit. Danh sách NPC cho phép đối thoại khi mệt mỏi.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcPermitDialogNpcEntry
    {
        public int NpcId { get; set; }
        public string NpcName { get; set; }
        public int MapId { get; set; }
        public int TireLimit { get; set; }
    }

    public sealed class PcPermitDialogNpcRegistry
    {
        private readonly Dictionary<int, PcPermitDialogNpcEntry> _byId = new Dictionary<int, PcPermitDialogNpcEntry>();
        public int Count => _byId.Count;
        public PcPermitDialogNpcEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcPermitDialogNpcEntry> All => _byId.Values;
        public void Add(PcPermitDialogNpcEntry e)
        {
            if (e == null) return;
            if (e.NpcId == 0 && !string.IsNullOrEmpty(e.NpcName)) e.NpcId = e.NpcName.GetHashCode();
            _byId[e.NpcId] = e;
        }
    }

    public static class PcPermitDialogNpcParser
    {
        public static PcPermitDialogNpcRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcPermitDialogNpcRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "permitdialognpc_info.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 1) continue;
                var e = new PcPermitDialogNpcEntry
                {
                    NpcName = cols[0].Trim(),
                    MapId = cols.Length > 1 && int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int m) ? m : 0,
                    TireLimit = cols.Length > 2 && int.TryParse(cols[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int t) ? t : 0
                };
                if (string.IsNullOrEmpty(e.NpcName)) continue;
                reg.Add(e);
            }
            return reg;
        }
    }
}
