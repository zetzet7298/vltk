// -----------------------------------------------------------------------------
// VLTK Mobile — shared PC text file reader
// Source: tab-separated PC exports (server = GB2312, client = UTF-8). The
// auto-detect path picks GB2312 when it produces few replacement characters
// and falls back to UTF-8 otherwise; callers that know the encoding can pass
// it explicitly to skip the heuristic.
// -----------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    internal static class PcText
    {
        public static string[] ReadLines(string absolutePath, Encoding encoding)
        {
            var raw = File.ReadAllBytes(absolutePath);
            string text;
            if (encoding != null)
            {
                text = TryDecode(raw, encoding);
            }
            else
            {
                var strict = new UTF8Encoding(false, true);
                var gbk = TryDecode(raw, Encoding.GetEncoding("GB2312"));
                var utf = TryDecodeStrict(raw, strict);
                text = !string.IsNullOrEmpty(utf) ? utf : gbk;
            }
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

        private static int CountReplacement(string text)
        {
            if (string.IsNullOrEmpty(text)) return int.MaxValue;
            int n = 0;
            for (int i = 0; i < text.Length; i++)
                if (text[i] == '\ufffd') n++;
            return n;
        }

        private static string[] SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
            if (text.Length > 0 && text[0] == '\ufeff') text = text.Substring(1);
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }
    }
}
