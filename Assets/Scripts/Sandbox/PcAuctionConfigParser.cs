// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/auction.ini auction (đấu giá) parser
// Source: auction.ini (GB2312). INI-format config for auction rules + prices.
// Mobile runtime exposes the key/value pairs for runtime price scaling.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcAuctionConfigParser
    {
        public static List<PcAuctionConfigEntry> ParseFile(string path)
        {
            var rows = new List<PcAuctionConfigEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            string section = string.Empty;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2);
                    continue;
                }
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                rows.Add(new PcAuctionConfigEntry
                {
                    section = section,
                    key = line.Substring(0, eq).Trim(),
                    value = line.Substring(eq + 1).Trim(),
                });
            }
            return rows;
        }

        public static PcAuctionConfigRegistry BuildRegistry(string dir)
        {
            var reg = new PcAuctionConfigRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "auction.ini");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcAuctionConfigEntry
    {
        public string section;
        public string key;
        public string value;
    }

    public sealed class PcAuctionConfigRegistry
    {
        private readonly List<PcAuctionConfigEntry> _all = new();
        public int Count => _all.Count;
        public void Register(PcAuctionConfigEntry e) { if (e == null) return; _all.Add(e); }
        public string Get(string section, string key)
        {
            foreach (var e in _all)
                if (e.section == section && e.key == key) return e.value;
            return null;
        }
    }
}
