using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Source-truth audit only for PC settings/missles*.txt files.
    /// Does not feed gameplay/runtime missile behavior.
    /// </summary>
    public sealed class PcMissileSourceFileAudit
    {
        public string sourceLabel;
        public string fileName;
        public long byteCount;
        public string sha256;
        public string headerSha256;
        public int physicalLineCount;
        public int dataRowCount;
        public int headerColumnCount;
        public string[] schemaColumns = Array.Empty<string>();
        public int parsedIdCount;
        public int uniqueIdCount;
        public int duplicateIdCount;
        public int minMissileId;
        public int maxMissileId;
        public int[] duplicateMissileIds = Array.Empty<int>();
        public int[] dataColumnCounts = Array.Empty<int>();

        public bool HasExactPcMissileSchema =>
            headerColumnCount == PcMissileSourceAudit.ExpectedSchemaColumnCount &&
            dataColumnCounts.Length == 1 &&
            dataColumnCounts[0] == PcMissileSourceAudit.ExpectedSchemaColumnCount;
    }

    public sealed class PcMissileSourceComparison
    {
        public PcMissileSourceFileAudit left;
        public PcMissileSourceFileAudit right;
        public bool exactBytes;
        public bool sameHeaderSchema;
        public bool sameDataRowCount;
        public bool sameIdSequence;
        public bool sameUniqueIdSet;
        public int differingRowByteCount;
        public int[] idsOnlyInLeft = Array.Empty<int>();
        public int[] idsOnlyInRight = Array.Empty<int>();
    }

    public static class PcMissileSourceAudit
    {
        public const int ExpectedSchemaColumnCount = 57;
        public const string ExpectedHeaderSha256 = "e7a8ce5c9855fe3fcd4e7c655e746d39e8385d530a01c4d46d512803d8efff7f";

        public static PcMissileSourceFileAudit ParseFile(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Path is required", nameof(path));
            return ParseBytes(path, File.ReadAllBytes(path));
        }

        public static PcMissileSourceFileAudit ParseBytes(string sourceLabel, byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            var lines = SplitLines(bytes);
            var audit = new PcMissileSourceFileAudit
            {
                sourceLabel = sourceLabel ?? string.Empty,
                fileName = string.IsNullOrEmpty(sourceLabel) ? string.Empty : Path.GetFileName(sourceLabel),
                byteCount = bytes.LongLength,
                sha256 = Sha256Hex(bytes),
                physicalLineCount = lines.Count,
            };

            if (lines.Count == 0) return audit;

            byte[] header = TrimTrailingCarriageReturn(lines[0]);
            audit.headerSha256 = Sha256Hex(header);
            audit.schemaColumns = DecodeAscii(header).Split('\t');
            audit.headerColumnCount = audit.schemaColumns.Length;

            var ids = new List<int>();
            var idCounts = new Dictionary<int, int>();
            var columnCounts = new SortedSet<int>();
            int minId = int.MaxValue;
            int maxId = int.MinValue;

            for (int i = 1; i < lines.Count; i++)
            {
                byte[] row = TrimTrailingCarriageReturn(lines[i]);
                if (IsBlank(row)) continue;

                audit.dataRowCount++;
                byte[][] cols = SplitTabs(row);
                columnCounts.Add(cols.Length);

                if (TryParseAsciiInt(cols.Length > 0 ? cols[0] : Array.Empty<byte>(), out int id))
                {
                    ids.Add(id);
                    if (!idCounts.ContainsKey(id)) idCounts[id] = 0;
                    idCounts[id]++;
                    if (id < minId) minId = id;
                    if (id > maxId) maxId = id;
                }
            }

            var duplicates = new List<int>();
            foreach (var kv in idCounts)
            {
                if (kv.Value > 1) duplicates.Add(kv.Key);
            }
            duplicates.Sort();

            audit.parsedIdCount = ids.Count;
            audit.uniqueIdCount = idCounts.Count;
            audit.duplicateIdCount = ids.Count - idCounts.Count;
            audit.minMissileId = ids.Count == 0 ? 0 : minId;
            audit.maxMissileId = ids.Count == 0 ? 0 : maxId;
            audit.duplicateMissileIds = duplicates.ToArray();
            audit.dataColumnCounts = ToArray(columnCounts);
            return audit;
        }

        public static PcMissileSourceComparison CompareFiles(string leftPath, string rightPath)
        {
            byte[] leftBytes = File.ReadAllBytes(leftPath);
            byte[] rightBytes = File.ReadAllBytes(rightPath);
            return CompareBytes(leftPath, leftBytes, rightPath, rightBytes);
        }

        public static PcMissileSourceComparison CompareBytes(
            string leftLabel,
            byte[] leftBytes,
            string rightLabel,
            byte[] rightBytes)
        {
            var left = ParseBytes(leftLabel, leftBytes);
            var right = ParseBytes(rightLabel, rightBytes);
            var leftIds = ReadIdSequence(leftBytes);
            var rightIds = ReadIdSequence(rightBytes);

            return new PcMissileSourceComparison
            {
                left = left,
                right = right,
                exactBytes = BytesEqual(leftBytes, rightBytes),
                sameHeaderSchema = left.headerSha256 == right.headerSha256 &&
                                   left.headerColumnCount == right.headerColumnCount,
                sameDataRowCount = left.dataRowCount == right.dataRowCount,
                sameIdSequence = SequenceEqual(leftIds, rightIds),
                sameUniqueIdSet = SameSet(leftIds, rightIds),
                differingRowByteCount = CountDifferingRows(leftBytes, rightBytes),
                idsOnlyInLeft = Difference(leftIds, rightIds),
                idsOnlyInRight = Difference(rightIds, leftIds),
            };
        }

        private static int CountDifferingRows(byte[] leftBytes, byte[] rightBytes)
        {
            var leftRows = SplitLines(leftBytes);
            var rightRows = SplitLines(rightBytes);
            int max = Math.Max(leftRows.Count, rightRows.Count);
            int diff = 0;
            for (int i = 0; i < max; i++)
            {
                if (i >= leftRows.Count || i >= rightRows.Count ||
                    !BytesEqual(TrimTrailingCarriageReturn(leftRows[i]), TrimTrailingCarriageReturn(rightRows[i]))) diff++;
            }
            return diff;
        }

        private static int[] ReadIdSequence(byte[] bytes)
        {
            var ids = new List<int>();
            var lines = SplitLines(bytes);
            for (int i = 1; i < lines.Count; i++)
            {
                byte[] row = TrimTrailingCarriageReturn(lines[i]);
                if (IsBlank(row)) continue;
                byte[][] cols = SplitTabs(row);
                if (cols.Length > 0 && TryParseAsciiInt(cols[0], out int id)) ids.Add(id);
            }
            return ids.ToArray();
        }

        private static bool SequenceEqual(int[] left, int[] right)
        {
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }

        private static bool SameSet(int[] left, int[] right) =>
            Difference(left, right).Length == 0 && Difference(right, left).Length == 0;

        private static int[] Difference(int[] left, int[] right)
        {
            var rightSet = new HashSet<int>(right);
            var resultSet = new SortedSet<int>();
            foreach (int id in left)
            {
                if (!rightSet.Contains(id)) resultSet.Add(id);
            }
            return ToArray(resultSet);
        }

        private static bool TryParseAsciiInt(byte[] bytes, out int value)
        {
            value = 0;
            if (bytes == null || bytes.Length == 0) return false;
            string text = DecodeAscii(bytes).Trim();
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static List<byte[]> SplitLines(byte[] bytes)
        {
            var lines = new List<byte[]>();
            int start = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != (byte)'\n') continue;
                lines.Add(Slice(bytes, start, i - start));
                start = i + 1;
            }
            if (start < bytes.Length) lines.Add(Slice(bytes, start, bytes.Length - start));
            return lines;
        }

        private static byte[][] SplitTabs(byte[] bytes)
        {
            var parts = new List<byte[]>();
            int start = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != (byte)'\t') continue;
                parts.Add(Slice(bytes, start, i - start));
                start = i + 1;
            }
            parts.Add(Slice(bytes, start, bytes.Length - start));
            return parts.ToArray();
        }

        private static byte[] TrimTrailingCarriageReturn(byte[] bytes)
        {
            if (bytes.Length > 0 && bytes[bytes.Length - 1] == (byte)'\r')
                return Slice(bytes, 0, bytes.Length - 1);
            return bytes;
        }

        private static bool IsBlank(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                if (b != (byte)' ' && b != (byte)'\t' && b != (byte)'\r') return false;
            }
            return true;
        }

        private static byte[] Slice(byte[] bytes, int start, int length)
        {
            var slice = new byte[length];
            Buffer.BlockCopy(bytes, start, slice, 0, length);
            return slice;
        }

        private static string DecodeAscii(byte[] bytes) => Encoding.ASCII.GetString(bytes);

        private static string Sha256Hex(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
            return true;
        }

        private static int[] ToArray(IEnumerable<int> values)
        {
            var list = new List<int>(values);
            return list.ToArray();
        }
    }
}
