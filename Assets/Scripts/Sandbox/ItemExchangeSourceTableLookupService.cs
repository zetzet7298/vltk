// -----------------------------------------------------------------------------
// VLTK Mobile — typed lookup over imported PC itemexchange_setting tables.
// Source of truth: /var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/itemexchange_setting
// Catalog/read-only only: this does not execute or mutate item exchange runtime.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class ItemExchangeSourceTableLookupService
    {
        public const string DefaultSubdir = "Reference/PcItemExchange";

        public string DirectoryPath { get; private set; }
        public bool HasRoleValueLog { get; private set; }
        public ItemExchangeSourceLookupTable Normal { get; private set; }
        public ItemExchangeSourceLookupTable Rare { get; private set; }

        public int NormalHeaderCount => Normal != null ? Normal.HeaderCount : 0;
        public int NormalRowCount => Normal != null ? Normal.RowCount : 0;
        public int RareHeaderCount => Rare != null ? Rare.HeaderCount : 0;
        public int RareRowCount => Rare != null ? Rare.RowCount : 0;

        public ItemExchangeSourceTableLookupSummary Summary => new ItemExchangeSourceTableLookupSummary
        {
            normalHeaderCount = NormalHeaderCount,
            normalRowCount = NormalRowCount,
            rareHeaderCount = RareHeaderCount,
            rareRowCount = RareRowCount,
            hasRoleValueLog = HasRoleValueLog
        };

        private ItemExchangeSourceTableLookupService(
            string dir,
            ItemExchangeSourceLookupTable normal,
            ItemExchangeSourceLookupTable rare,
            bool hasRoleValueLog)
        {
            DirectoryPath = dir ?? string.Empty;
            Normal = normal ?? ItemExchangeSourceLookupTable.Empty("normal.txt");
            Rare = rare ?? ItemExchangeSourceLookupTable.Empty("rare.txt");
            HasRoleValueLog = hasRoleValueLog;
        }

        public static ItemExchangeSourceTableLookupService LoadFromStreamingAssets(string subdir = DefaultSubdir)
            => LoadFromDirectory(Path.Combine(Application.streamingAssetsPath, subdir));

        public static ItemExchangeSourceTableLookupService LoadFromDirectory(string dir)
        {
            var normal = LoadTable(Path.Combine(dir ?? string.Empty, "normal.txt"), "normal.txt");
            var rare = LoadTable(Path.Combine(dir ?? string.Empty, "rare.txt"), "rare.txt");
            return new ItemExchangeSourceTableLookupService(
                dir,
                normal,
                rare,
                Directory.Exists(Path.Combine(dir ?? string.Empty, "rolevalue_log")));
        }

        public bool TryFindByName(string tableName, string name, out ItemExchangeSourceLookupRow row)
            => TryFindByColumn(tableName, "NAME", name, out row);

        public bool TryFindByMagicId(string tableName, int magicId, out ItemExchangeSourceLookupRow row)
            => TryFindByColumn(tableName, "MAGIC_ID", magicId.ToString(CultureInfo.InvariantCulture), out row);

        public bool TryFindByColumn(string tableName, string column, string rawValue, out ItemExchangeSourceLookupRow row)
        {
            row = null;
            return TryGetTable(tableName, out var table) && table.TryFindFirstByColumn(column, rawValue, out row);
        }

        public bool TryGetTable(string tableName, out ItemExchangeSourceLookupTable table)
        {
            table = null;
            if (string.Equals(tableName, "normal", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(tableName, "normal.txt", StringComparison.OrdinalIgnoreCase)) table = Normal;
            else if (string.Equals(tableName, "rare", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(tableName, "rare.txt", StringComparison.OrdinalIgnoreCase)) table = Rare;
            return table != null && table.Exists;
        }

        private static ItemExchangeSourceLookupTable LoadTable(string path, string sourceName)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return ItemExchangeSourceLookupTable.Empty(sourceName);
            var lines = ReadText(path).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var headers = new List<string>();
            var rows = new List<string[]>();
            bool foundHeader = false;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                if (!foundHeader)
                {
                    headers.AddRange(SplitColumns(raw));
                    foundHeader = true;
                    continue;
                }
                rows.Add(SplitColumns(raw));
            }
            return new ItemExchangeSourceLookupTable(sourceName, true, headers, rows);
        }

        private static string ReadText(string path) => DecodeServerText(File.ReadAllBytes(path)).TrimStart('\ufeff');

        private static string DecodeServerText(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;
            foreach (var name in new[] { "GB18030", "GB2312", "utf-8", "windows-1258", "ISO-8859-1" })
            {
                try { return Encoding.GetEncoding(name).GetString(bytes); }
                catch { }
            }
            return Encoding.Default.GetString(bytes);
        }

        private static string[] SplitColumns(string line) => (line ?? string.Empty).Split('\t');
    }

    public sealed class ItemExchangeSourceLookupTable
    {
        private readonly Dictionary<string, int> columnIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> headers;
        private readonly List<ItemExchangeSourceLookupRow> rows = new List<ItemExchangeSourceLookupRow>();

        public string SourceName { get; private set; }
        public bool Exists { get; private set; }
        public IReadOnlyList<string> Headers => headers;
        public IReadOnlyList<ItemExchangeSourceLookupRow> Rows => rows;
        public int HeaderCount => headers.Count;
        public int RowCount => rows.Count;

        public static ItemExchangeSourceLookupTable Empty(string sourceName)
            => new ItemExchangeSourceLookupTable(sourceName, false, new List<string>(), new List<string[]>());

        public ItemExchangeSourceLookupTable(string sourceName, bool exists, List<string> headerValues, List<string[]> rowValues)
        {
            SourceName = sourceName ?? string.Empty;
            Exists = exists;
            headers = headerValues ?? new List<string>();
            for (int i = 0; i < headers.Count; i++) AddColumnIndex(headers[i], i);
            if (rowValues == null) return;
            for (int i = 0; i < rowValues.Count; i++) rows.Add(new ItemExchangeSourceLookupRow(this, i + 2, rowValues[i]));
        }

        public bool HasColumn(string column) => TryGetColumnIndex(column, out _);

        public bool TryGetColumnIndex(string column, out int index)
        {
            index = -1;
            if (string.IsNullOrEmpty(column)) return false;
            if (columnIndexes.TryGetValue(column, out index)) return true;
            return columnIndexes.TryGetValue(column.Trim(), out index);
        }

        public bool TryFindFirstByColumn(string column, string rawValue, out ItemExchangeSourceLookupRow row)
        {
            row = null;
            if (rawValue == null || !TryGetColumnIndex(column, out var index)) return false;
            foreach (var candidate in rows)
            {
                if (candidate.TryGetRaw(index, out var value) && string.Equals(value, rawValue, StringComparison.Ordinal))
                {
                    row = candidate;
                    return true;
                }
            }
            return false;
        }

        private void AddColumnIndex(string column, int index)
        {
            if (string.IsNullOrEmpty(column)) return;
            if (!columnIndexes.ContainsKey(column)) columnIndexes.Add(column, index);
            var trimmed = column.Trim();
            if (trimmed.Length > 0 && !columnIndexes.ContainsKey(trimmed)) columnIndexes.Add(trimmed, index);
        }
    }

    public sealed class ItemExchangeSourceLookupRow
    {
        private readonly ItemExchangeSourceLookupTable table;
        private readonly string[] values;

        public int SourceLineNumber { get; private set; }
        public IReadOnlyList<string> Values => values;

        internal ItemExchangeSourceLookupRow(ItemExchangeSourceLookupTable owner, int sourceLineNumber, string[] rowValues)
        {
            table = owner;
            SourceLineNumber = sourceLineNumber;
            values = rowValues ?? Array.Empty<string>();
        }

        public bool TryGetRaw(string column, out string value)
        {
            value = null;
            return table != null && table.TryGetColumnIndex(column, out var index) && TryGetRaw(index, out value);
        }

        public string GetRawOrDefault(string column, string fallback = null)
            => TryGetRaw(column, out var value) ? value : fallback;

        public bool TryGetInt(string column, out int value)
        {
            value = 0;
            return TryGetRaw(column, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        internal bool TryGetRaw(int index, out string value)
        {
            value = null;
            if (index < 0 || index >= values.Length) return false;
            value = values[index];
            return true;
        }
    }

    [Serializable]
    public struct ItemExchangeSourceTableLookupSummary
    {
        public int normalHeaderCount;
        public int normalRowCount;
        public int rareHeaderCount;
        public int rareRowCount;
        public bool hasRoleValueLog;
    }
}
