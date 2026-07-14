// -----------------------------------------------------------------------------
// VLTK Mobile — PC itemexchange_setting source inventory parser (phase 1)
// Source of truth: /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/itemexchange_setting
// Purpose: expose file/header/row/key facts only. This does not execute exchange
// runtime rules and intentionally does not read rolevalue_log runtime logs.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcItemExchangeSourceParser
    {
        public static readonly string[] ExpectedTopLevelFiles =
        {
            "normal.txt", "rare.txt", "level_exp.txt", "level_lead_exp.txt", "rolevalue.ini"
        };

        public static PcItemExchangeSourceCatalog ParseDirectory(string dir)
        {
            var catalog = new PcItemExchangeSourceCatalog();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return catalog;

            catalog.normal = ParseTable(Path.Combine(dir, "normal.txt"), "normal.txt");
            catalog.rare = ParseTable(Path.Combine(dir, "rare.txt"), "rare.txt");
            catalog.levelExp = ParseTable(Path.Combine(dir, "level_exp.txt"), "level_exp.txt");
            catalog.levelLeadExp = ParseTable(Path.Combine(dir, "level_lead_exp.txt"), "level_lead_exp.txt");
            catalog.roleValue = ParseIni(Path.Combine(dir, "rolevalue.ini"));
            catalog.hasRoleValueLog = Directory.Exists(Path.Combine(dir, "rolevalue_log"));
            return catalog;
        }

        public static PcItemExchangeSourceTable ParseTable(string path, string sourceName)
        {
            var lines = ReadLines(path);
            var table = new PcItemExchangeSourceTable { sourceName = sourceName ?? string.Empty, exists = File.Exists(path) };
            table.totalLineCount = lines.Count;

            bool foundHeader = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!foundHeader)
                {
                    foundHeader = true;
                    table.headerLine = line;
                    table.headerColumns.AddRange(SplitColumns(line));
                    continue;
                }
                table.dataRowCount++;
            }
            return table;
        }

        public static PcItemExchangeRoleValueIni ParseIni(string path)
        {
            var ini = new PcItemExchangeRoleValueIni { exists = File.Exists(path) };
            string section = string.Empty;
            foreach (var raw in ReadLines(path))
            {
                var line = (raw ?? string.Empty).Trim();
                if (line.Length == 0 || line.StartsWith(";", StringComparison.Ordinal)) continue;
                if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    if (!ini.sections.Contains(section)) ini.sections.Add(section);
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                ini.keys.Add(new PcItemExchangeIniKey { section = section, key = key, value = value });
            }
            return ini;
        }

        private static List<string> ReadLines(string path)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return result;
            var bytes = File.ReadAllBytes(path);
            var text = DecodeServerText(bytes);
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            result.AddRange(text.Split('\n'));
            return result;
        }

        private static string DecodeServerText(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;
            foreach (var name in new[] { "GB18030", "GB2312", "utf-8", "windows-1258", "ISO-8859-1" })
            {
                try { return Encoding.GetEncoding(name).GetString(bytes).TrimStart('\ufeff'); }
                catch { }
            }
            return Encoding.Default.GetString(bytes).TrimStart('\ufeff');
        }

        private static string[] SplitColumns(string line)
            => (line ?? string.Empty).Split('\t');
    }

    [Serializable]
    public sealed class PcItemExchangeSourceCatalog
    {
        public PcItemExchangeSourceTable normal = new PcItemExchangeSourceTable();
        public PcItemExchangeSourceTable rare = new PcItemExchangeSourceTable();
        public PcItemExchangeSourceTable levelExp = new PcItemExchangeSourceTable();
        public PcItemExchangeSourceTable levelLeadExp = new PcItemExchangeSourceTable();
        public PcItemExchangeRoleValueIni roleValue = new PcItemExchangeRoleValueIni();
        public bool hasRoleValueLog;
    }

    [Serializable]
    public sealed class PcItemExchangeSourceTable
    {
        public string sourceName;
        public bool exists;
        public int totalLineCount;
        public int dataRowCount;
        public string headerLine;
        public readonly List<string> headerColumns = new List<string>();
    }

    [Serializable]
    public sealed class PcItemExchangeRoleValueIni
    {
        public bool exists;
        public readonly List<string> sections = new List<string>();
        public readonly List<PcItemExchangeIniKey> keys = new List<PcItemExchangeIniKey>();
    }

    [Serializable]
    public sealed class PcItemExchangeIniKey
    {
        public string section;
        public string key;
        public string value;
        public string FullKey => string.IsNullOrEmpty(section) ? key : section + "." + key;
    }
}
