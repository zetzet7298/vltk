// -----------------------------------------------------------------------------
// VLTK.Survivor — SkillTextCodec
// Encoding helpers for the three JX skill tables:
//   PcSkills.txt                         — transcoded UTF-8 (names VN, sprite
//                                          paths GBK-mojibake via TCVN3 decode)
//   PcAllFactionLearnedDisplaySkills.txt — byte-preserving: TCVN3 names + raw
//                                          GBK paths
//   PcAttrib/missles.txt                 — byte-preserving: TCVN3 names + raw
//                                          GBK paths (AnimFile2 col 32)
// Mojibake recovery (verified on real data): UTF-8 mojibake chars are the
// TCVN3 decodes of the original GBK bytes → reverse-TCVN3 table reproduces
// the exact GBK bytes, which then hash to the same UIDs as PC.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;

namespace VLTK.Survivor
{
    public static class SkillTextCodec
    {
        // TCVN3 (TCVN 5712) byte → Unicode. Source: Sandbox PcText.Tcvn3Table
        // (originated from vltktool decode_item_texts_vi.py). Index = windows-1252
        // byte value; value = Unicode code point.
        private static readonly int[] Tcvn3Table =
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

        // Reverse: Unicode code point → first byte that decodes to it.
        private static readonly Dictionary<int, int> Tcvn3Reverse = BuildReverse();

        private static Dictionary<int, int> BuildReverse()
        {
            var map = new Dictionary<int, int>();
            for (int i = 0; i < Tcvn3Table.Length; i++)
                if (!map.ContainsKey(Tcvn3Table[i]))
                    map[Tcvn3Table[i]] = i;
            return map;
        }

        /// <summary>Decode a byte-preserved (latin1) TCVN3 string to Unicode.</summary>
        public static string DecodeTcvn3(string bytePreserved)
        {
            if (string.IsNullOrEmpty(bytePreserved)) return bytePreserved;
            var chars = bytePreserved.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                int code = chars[i];
                if (code >= 0 && code < Tcvn3Table.Length)
                    chars[i] = (char)Tcvn3Table[code];
            }
            return new string(chars);
        }

        /// <summary>Decode raw TCVN3 bytes to Unicode.</summary>
        public static string DecodeTcvn3(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return "";
            var chars = new char[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];
                chars[i] = b < Tcvn3Table.Length ? (char)Tcvn3Table[b] : (char)b;
            }
            return new string(chars);
        }

        /// <summary>
        /// Reverse TCVN3: if EVERY char resolves, returns the original byte
        /// sequence (the GBK bytes that were mis-decoded as TCVN3). False when
        /// any non-ASCII char is not a TCVN3 glyph (e.g. proper Chinese).
        /// </summary>
        public static bool TryReverseTcvn3(string s, out byte[] bytes)
        {
            bytes = null;
            if (string.IsNullOrEmpty(s)) { bytes = System.Array.Empty<byte>(); return true; }
            var outBytes = new byte[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                int cp = s[i];
                if (cp < 128) { outBytes[i] = (byte)cp; continue; }
                if (!Tcvn3Reverse.TryGetValue(cp, out int b)) return false;
                outBytes[i] = (byte)b;
            }
            bytes = outBytes;
            return true;
        }

        /// <summary>Latin1 view: chars ≤ 0xFF → their byte values (byte-preserving round trip).</summary>
        public static byte[] Latin1Bytes(string s)
        {
            if (string.IsNullOrEmpty(s)) return System.Array.Empty<byte>();
            var bytes = new byte[s.Length];
            for (int i = 0; i < s.Length; i++)
                bytes[i] = (byte)(s[i] & 0xFF);
            return bytes;
        }

        /// <summary>Decode a UTF-8 name column from a byte-preserved row string.</summary>
        public static string DecodeUtf8Name(string bytePreserved)
        {
            return Encoding.UTF8.GetString(Latin1Bytes(bytePreserved)).Trim();
        }

        /// <summary>
        /// Path → hashable bytes, per source transcode reality:
        ///   1. empty → empty
        ///   2. all-ASCII → ASCII bytes
        ///   3. every non-ASCII char is a TCVN3 glyph (mojibake) → reverse-TCVN3
        ///      bytes (== original GBK bytes; verified on real data)
        ///   4. else proper CJK → GB2312 encode (== original GBK bytes for the
        ///      standard Chinese in these files)
        /// </summary>
        public static byte[] PathToHashBytes(string path)
        {
            if (string.IsNullOrEmpty(path)) return System.Array.Empty<byte>();
            if (TryReverseTcvn3(path, out var rev)) return rev;
            return JxPathHash.EncodePath(path);
        }
    }
}
