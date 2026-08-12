// -----------------------------------------------------------------------------
// VLTK Mobile — PC FlipCard protocol source catalog parser.
// Source: 00.src-tinh-kiem/server1/script/flipcard/{flipcard_head,flipcard_c}.lua
// Catalog only: UI protocol constants/functions, not reward/runtime execution.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcFlipCardProtocolParser
    {
        public const string SourceFileName = "flipcard_protocol.txt";
        public const int KeyCol = 0;
        public const int ValueCol = 1;
        public const int ValueTypeCol = 2;
        public const int SourceFileCol = 3;
        public const int EvidenceCol = 4;

        public static List<PcFlipCardProtocolEntry> ParseFile(string path)
        {
            var rows = new List<PcFlipCardProtocolEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            bool header = false;
            foreach (var line in File.ReadAllText(path).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split('\t');
                if (!header) { header = true; continue; }
                if (cols.Length <= EvidenceCol) continue;
                rows.Add(new PcFlipCardProtocolEntry
                {
                    key = Str(cols, KeyCol),
                    valueRaw = Str(cols, ValueCol),
                    valueType = Str(cols, ValueTypeCol),
                    sourceFile = Str(cols, SourceFileCol),
                    evidence = Str(cols, EvidenceCol),
                });
            }
            return rows;
        }

        public static PcFlipCardProtocolRegistry BuildRegistry(string dir)
        {
            var reg = new PcFlipCardProtocolRegistry();
            if (string.IsNullOrEmpty(dir)) return reg;
            var path = Directory.Exists(dir) ? Path.Combine(dir, SourceFileName) : dir;
            foreach (var row in ParseFile(path)) reg.Register(row);
            return reg;
        }

        private static string Str(string[] cols, int i)
            => cols != null && i >= 0 && i < cols.Length ? (cols[i] ?? string.Empty).Trim() : string.Empty;
    }

    [Serializable]
    public sealed class PcFlipCardProtocolEntry
    {
        public string key;
        public string valueRaw;
        public string valueType;
        public string sourceFile;
        public string evidence;
    }
}
