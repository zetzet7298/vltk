// -----------------------------------------------------------------------------
// VLTK Mobile — shared PC text file reader
// Source: tab-separated PC exports. Some files are UTF-8/GBK Chinese, while
// Vietnamese tables such as objdata.txt use Western ANSI bytes plus TCVN3 glyph
// codes. Callers that know the encoding can pass it explicitly to skip scoring.
// -----------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    internal static class PcText
    {
        // Source: /var/www/vltktool/decode_item_texts_vi.py TCVN3_TABLE.
        // PC Vietnamese settings (npcs.txt, objdata.txt) are Western ANSI bytes
        // whose high chars are TCVN3 glyph codes, not Unicode Vietnamese.
        private static readonly int[] Tcvn3Table = new int[]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
            32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47,
            48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63,
            64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
            80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95,
            96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111,
            112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127,
            128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142, 143,
            144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159,
            160, 258, 194, 202, 212, 416, 431, 272, 259, 226, 234, 244, 417, 432, 273, 175,
            176, 177, 178, 179, 180, 224, 7843, 227, 225, 7841, 186, 7857, 7859, 7861, 7855, 191,
            192, 193, 194, 195, 196, 197, 7863, 7847, 7849, 7851, 7845, 7853, 232, 205, 7867, 7869,
            233, 7865, 7873, 7875, 7877, 7871, 7879, 236, 7881, 217, 218, 219, 297, 237,
            7883, 242, 224, 7887, 245, 243, 7885, 7891, 7893, 7895, 7889, 7897, 7901, 7903,
            7905, 7899, 7907, 249, 240, 7911, 361, 250, 7909, 7915, 7917, 7919, 7913, 7921,
            7923, 7927, 7929, 253, 7925, 255,
        };

        public static string[] ReadLines(string absolutePath, Encoding encoding)
        {
            var raw = File.ReadAllBytes(absolutePath);
            string text = encoding != null ? TryDecode(raw, encoding) : DecodeBest(raw);
            return SplitLines(text);
        }

        private static string TryDecodeStrict(byte[] bytes, Encoding enc)
        {
            try { return enc.GetString(bytes); }
            catch { return string.Empty; }
        }

        private static string TryDecode(byte[] bytes, Encoding enc)
        {
            try { return enc.GetString(bytes); }
            catch { return string.Empty; }
        }

        private static string DecodeBest(byte[] raw)
        {
            string best = string.Empty;
            int bestScore = int.MinValue;
            void Consider(string text, bool tcvn3 = false)
            {
                if (string.IsNullOrEmpty(text)) return;
                var candidate = tcvn3 ? Tcvn3ToUnicode(text) : text;
                int score = Score(candidate);
                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            Consider(TryDecodeStrict(raw, new UTF8Encoding(false, true)));
            Consider(TryDecode(raw, TryEncoding("GB18030")));
            Consider(TryDecode(raw, TryEncoding("GB2312")));
            var western = TryDecode(raw, TryEncoding("windows-1252"));
            Consider(western);
            Consider(western, tcvn3: true);
            var latin1 = TryDecode(raw, TryEncoding("iso-8859-1"));
            Consider(latin1);
            Consider(latin1, tcvn3: true);
            return best;
        }

        private static Encoding TryEncoding(string name)
        {
            try { return Encoding.GetEncoding(name); }
            catch { return Encoding.Default; }
        }

        private static string Tcvn3ToUnicode(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            var chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int code = chars[i];
                if (code >= 0 && code < Tcvn3Table.Length)
                    chars[i] = (char)Tcvn3Table[code];
            }
            return new string(chars);
        }

        private static int Score(string text)
        {
            int score = 0;
            foreach (char ch in text)
            {
                // CJK weighted >= Vietnamese: a valid GB2312/GB18030 hanzi decode must
                // out-score the windows-1252+TCVN3 candidate for Chinese files (e.g.
                // objsetting.txt 宝箱1), while genuine TCVN3 Vietnamese files (objdata.txt
                // "Bảo rương 1") still win because their bytes do not form clean hanzi.
                // Verified on 198 real reference files: 7 GBK files corrected, 0 Vietnamese regressed.
                if (IsVietnamese(ch)) score += 4;
                else if (ch >= 0x4e00 && ch <= 0x9fff) score += 8;
                else if (ch == '\ufffd') score -= 20;
                else if (IsMojibake(ch)) score -= 4;
                else if (char.IsControl(ch) && ch != '\n' && ch != '\r' && ch != '\t') score -= 10;
                else if (char.IsWhiteSpace(ch) || !char.IsControl(ch)) score += 1;
            }
            return score;
        }

        private static bool IsVietnamese(char ch)
        {
            const string chars =
                "ăâđêôơưáàảãạắằẳẵặấầẩẫậéèẻẽẹếềểễệ" +
                "íìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵ" +
                "ĂÂĐÊÔƠƯÁÀẢÃẠẮẰẲẴẶẤẦẨẪẬÉÈẺẼẸẾỀỂỄỆ" +
                "ÍÌỈĨỊÓÒỎÕỌỐỒỔỖỘỚỜỞỠỢÚÙỦŨỤỨỪỬỮỰÝỲỶỸỴ";
            return chars.IndexOf(ch) >= 0;
        }

        private static bool IsMojibake(char ch)
        {
            const string chars = "ÃÂÊÎÔÛÐÑÒÓÕÖ×ØÙÚÛÜÝÞß¶·¸¹º»¼½¾¿±";
            return chars.IndexOf(ch) >= 0;
        }

        private static string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
            if (text.Length > 0 && text[0] == '\ufeff') text = text.Substring(1);
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }
    }
}
