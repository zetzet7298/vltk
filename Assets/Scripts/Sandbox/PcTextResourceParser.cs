// -----------------------------------------------------------------------------
// VLTK Mobile — PC text resource parser
// Source: settings/text/textresource.txt (Text Resource - 1000+ chuỗi tiếng Việt).
// Columns: Key Vietnamese Chinese Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTextResourceParser
    {
        public const int KeyCol = 0;
        public const int VietnameseCol = 1;
        public const int ChineseCol = 2;
        public const int DescriptionCol = 3;

        public static List<PcTextResourceEntry> ParseFile(string path)
        {
            var rows = new List<PcTextResourceEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                var key = PcItemCommon.Str(cols, KeyCol);
                if (string.IsNullOrEmpty(key)) continue;
                rows.Add(new PcTextResourceEntry
                {
                    key = key,
                    vietnamese = PcItemCommon.Str(cols, VietnameseCol),
                    chinese = PcItemCommon.Str(cols, ChineseCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcTextResourceRegistry BuildRegistry(string dir)
        {
            var reg = new PcTextResourceRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f).ToLowerInvariant();
                if (name.StartsWith("text") || name.StartsWith("locale"))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcTextResourceEntry
    {
        public string key;
        public string vietnamese;
        public string chinese;
        public string description;
    }

    public sealed class PcTextResourceRegistry
    {
        private readonly Dictionary<string, PcTextResourceEntry> _byKey = new(System.StringComparer.Ordinal);
        public int Count => _byKey.Count;
        public void Register(PcTextResourceEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.key)) return;
            _byKey[e.key] = e;
        }
        public PcTextResourceEntry Get(string key)
            => _byKey.TryGetValue(key ?? string.Empty, out var v) ? v : null;
        public IReadOnlyList<PcTextResourceEntry> All => new List<PcTextResourceEntry>(_byKey.Values);
    }
}
