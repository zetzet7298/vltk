// -----------------------------------------------------------------------------
// VLTK Mobile — PC item common parsing helpers
// Source: server/settings/item/004/*.txt (GB2312, tab-separated)
// Purpose: shared encoder/decoder + column helpers used by the 12 PC item
// parsers (armor, helm, boot, cuff, belt, ring, amulet, pendant, meleeweapon,
// rangeweapon, horse share 46 columns; potion uses 28).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcItemCommon
    {
        public const int EquipColumnCount = 46;
        public const int PotionColumnCount = 28;

        public const string GbkFallbackEncoding = "GB2312";

        public static Encoding GetServerEncoding()
        {
            try
            {
                return Encoding.GetEncoding(GbkFallbackEncoding);
            }
            catch
            {
                return Encoding.Default;
            }
        }

        public static List<string> ReadServerLines(string absolutePath)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return result;

            Encoding encoding;
            try
            {
                encoding = GetServerEncoding();
            }
            catch
            {
                encoding = Encoding.Default;
            }

            string text;
            try
            {
                text = File.ReadAllText(absolutePath, encoding);
            }
            catch (DecoderFallbackException)
            {
                encoding = Encoding.GetEncoding("GB2312", new EncoderReplacementFallback(string.Empty), new DecoderReplacementFallback("?"));
                try { text = File.ReadAllText(absolutePath, encoding); }
                catch { return result; }
            }

            if (string.IsNullOrEmpty(text)) return result;
            if (text.Length > 0 && text[0] == '\ufeff')
                text = text.Substring(1);
            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            foreach (var line in lines)
            {
                if (line == null) continue;
                result.Add(line);
            }
            return result;
        }

        public static string Str(string[] cols, int i)
        {
            if (cols == null) return string.Empty;
            if (i < 0 || i >= cols.Length) return string.Empty;
            return (cols[i] ?? string.Empty).Trim();
        }

        public static int Int(string[] cols, int i)
        {
            var s = Str(cols, i);
            if (string.IsNullOrEmpty(s)) return 0;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
        }

        public static bool IsMissing(string s) => string.IsNullOrEmpty(s) || s == "-1";

        public static List<ItemStatDelta> BuildStatDeltas(string[] cols, int statBlockStart, int reqBlockStart, int statCount, int reqCount, int refineLevel)
        {
            var deltas = new List<ItemStatDelta>();
            if (cols == null) return deltas;
            for (int i = 0; i < statCount; i++)
            {
                int typeCol = statBlockStart + i * 3;
                int minCol = typeCol + 1;
                int maxCol = typeCol + 2;
                var type = Str(cols, typeCol);
                if (IsMissing(type)) continue;
                int min = Int(cols, minCol);
                int max = Int(cols, maxCol);
                int value = (max > min) ? max : min;
                if (value == 0) continue;
                deltas.Add(new ItemStatDelta
                {
                    ruleId = $"STAT_BASE_{type}",
                    stage = ItemStatStage.Base,
                    attrCode = Int(cols, typeCol),
                    value = value,
                });
            }
            for (int i = 0; i < reqCount; i++)
            {
                int typeCol = reqBlockStart + i * 2;
                int valCol = typeCol + 1;
                var type = Str(cols, typeCol);
                if (IsMissing(type)) continue;
                int value = Int(cols, valCol);
                if (value == 0) continue;
                deltas.Add(new ItemStatDelta
                {
                    ruleId = $"STAT_REQ_{type}",
                    stage = ItemStatStage.Base,
                    attrCode = Int(cols, typeCol),
                    value = value,
                });
            }
            return deltas;
        }

        public static SourceAssetId BuildIconSourceId(string sprPath, string evidenceNote)
        {
            if (string.IsNullOrWhiteSpace(sprPath)) return null;
            return new SourceAssetId
            {
                sourcePath = sprPath,
                resourceKind = ResourceKind.Sprite,
                uid = sprPath.GetHashCode(),
                discoveryTool = DiscoveryTool.Vltktool,
                evidenceNote = evidenceNote ?? "pc_item_004",
            };
        }

        public static bool ContainsReplacementChar(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return text.IndexOf('\ufffd') >= 0;
        }

        public static bool ContainsCjk(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var ch in text)
            {
                if ((ch >= '\u3400' && ch <= '\u4dbf')
                    || (ch >= '\u4e00' && ch <= '\u9fff')
                    || (ch >= '\uf900' && ch <= '\ufaff'))
                    return true;
            }
            return false;
        }

        public static string SafeDisplayName(string nameRaw, int itemId)
        {
            if (!string.IsNullOrEmpty(nameRaw)) return nameRaw;
            return $"Item_{itemId}";
        }

        public static int EstimateRefineLevel(string[] cols, int firstMinCol, int statCount)
        {
            if (cols == null) return 0;
            for (int i = 0; i < statCount; i++)
            {
                int minCol = firstMinCol + i * 3;
                int maxCol = minCol + 1;
                int min = Int(cols, minCol);
                int max = Int(cols, maxCol);
                if (max > min && max > 0) return Math.Max(1, (max - min) / Math.Max(1, min));
            }
            return 0;
        }
    }
}
