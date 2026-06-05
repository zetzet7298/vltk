// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/item/itemvalue/*.txt parser
// Source: settings/item/itemvalue/equip_*.txt, ore.txt, magicattrib_*.txt (GBK).
// Quản lý giá trị tính toán cho trang bị theo cấp, loại, magic.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace VLTK.Sandbox
{
    [Serializable]
    public class PcItemValueEntry
    {
        public string category;
        public string name;
        public int genre;
        public int index;
        public int level;
        public long value;
        public string rawLine;
    }

    public sealed class PcItemValueRegistry
    {
        private readonly List<PcItemValueEntry> _all = new();
        public int Count => _all.Count;
        public void Register(PcItemValueEntry e) { if (e != null) _all.Add(e); }
        public IReadOnlyList<PcItemValueEntry> All => _all;
        public IReadOnlyList<PcItemValueEntry> GetByCategory(string category)
        {
            if (string.IsNullOrEmpty(category)) return System.Array.Empty<PcItemValueEntry>();
            var result = new List<PcItemValueEntry>();
            foreach (var e in _all) if (string.Equals(e.category, category, StringComparison.OrdinalIgnoreCase)) result.Add(e);
            return result;
        }
        public IReadOnlyList<PcItemValueEntry> GetByLevel(int level)
        {
            var result = new List<PcItemValueEntry>();
            foreach (var e in _all) if (e.level == level) result.Add(e);
            return result;
        }
    }

    public static class PcItemValueParser
    {
        public static PcItemValueRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcItemValueRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var files = Directory.GetFiles(absoluteDir, "*.txt", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var category = Path.GetFileNameWithoutExtension(file);
                var lines = PcMapListParser.ReadLines(file);
                bool headerSkipped = false;
                foreach (var raw in lines)
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    var line = raw.Trim();
                    if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                    var cols = line.Split('\t');
                    if (cols.Length < 2) continue;
                    // Try to detect header (non-numeric first col) — skip 1 header
                    if (!headerSkipped)
                    {
                        if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                            && !long.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                            && !System.Text.RegularExpressions.Regex.IsMatch(cols[0].Trim(), "^\\d"))
                        {
                            headerSkipped = true;
                            continue;
                        }
                        headerSkipped = true;
                    }
                    var entry = new PcItemValueEntry
                    {
                        category = category,
                        rawLine = line
                    };
                    if (cols.Length == 2)
                    {
                        // LEVEL VALUE format (e.g. ore.txt)
                        if (int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lvl)
                            && long.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long val))
                        {
                            entry.level = lvl;
                            entry.value = val;
                            reg.Register(entry);
                        }
                    }
                    else
                    {
                        // NAME GENRE INDEX VALUE format
                        entry.name = cols[0].Trim();
                        entry.genre = cols.Length > 1 && int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int g) ? g : 0;
                        entry.index = cols.Length > 2 && int.TryParse(cols[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? i : 0;
                        entry.value = cols.Length > 3 && long.TryParse(cols[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : 0;
                        if (!string.IsNullOrEmpty(entry.name)) reg.Register(entry);
                    }
                }
            }
            return reg;
        }
    }
}
