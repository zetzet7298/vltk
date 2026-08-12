// -----------------------------------------------------------------------------
// VLTK.Survivor — JxPathHash
// PC resource-path hash parity. Algorithm identical to:
//   - vltktool/jx_hash.py  `file_id_from_bytes`  (canonical RE tool)
//   - Sandbox SprRuntimeService.ComputePathUid / ComputePathUidHex (signed variant)
// Verified live against /SpritesRuntime staging: hashing GBK bytes of
// missles.txt AnimFile2 paths reproduces 286/353 staged uids; display-file
// PreCastSpr GBK paths 112/121.
// Signed-bytes = PC pack hash (matches staged SPRs). Unsigned = fallback
// candidate, same as SprRuntimeService probing both.
// -----------------------------------------------------------------------------

using System;
using System.Text;

namespace VLTK.Survivor
{
    public static class JxPathHash
    {
        private static readonly Encoding Gb2312;

        static JxPathHash()
        {
            Encoding gb = null;
            try
            {
                var t = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                var prov = t?.GetProperty("Instance")?.GetValue(null, null) as EncodingProvider;
                if (prov != null)
                {
                    Encoding.RegisterProvider(prov);
                    gb = Encoding.GetEncoding("GB2312");
                }
            }
            catch { }
            Gb2312 = gb;
        }

        /// <summary>Mirror jx_hash.normalize_resource_path: trim, strip NUL, / → \, ensure leading \.</summary>
        public static string NormalizeResourcePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            var s = path.Trim().TrimEnd('\0').Replace('/', '\\');
            if (s.Length == 0) return "";
            if (!s.StartsWith("\\", StringComparison.Ordinal)) s = "\\" + s;
            return s;
        }

        /// <summary>
        /// Compute the JX1 pack-hash UID from exact path bytes (GB2312/GBK bytes of
        /// the normalized logical path). Plain long math is exact here:
        /// (value + i*c) mod 0x8000000B stays &lt; 2^31 and * 0xFFFFFFEF
        /// ≈ 9.223e18 &lt; long.MaxValue, so no overflow, no BigInteger needed.
        /// </summary>
        public static uint ComputePathUid(byte[] bytes, bool signedBytes = true)
        {
            if (bytes == null || bytes.Length == 0) return 0;
            long value = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];
                if (b >= 65 && b <= 90) b += 32; // A-Z → a-z, PC pack-hash behavior
                int c = signedBytes && b >= 128 ? b - 256 : b;
                value = unchecked(((value + (i + 1L) * c) % 0x8000000BL) * 0xFFFFFFEFL);
                value &= 0xFFFFFFFFL;
            }
            return unchecked((uint)((value ^ 0x12345678L) & 0xFFFFFFFFL));
        }

        public static string ComputePathUidHex(byte[] bytes, bool signedBytes = true)
        {
            uint uid = ComputePathUid(bytes, signedBytes);
            return uid == 0 ? null : uid.ToString("x8");
        }

        /// <summary>GB2312 encode a normalized path; UTF-8 fallback if the provider is unavailable.</summary>
        public static byte[] EncodePath(string path)
        {
            var norm = NormalizeResourcePath(path);
            if (norm.Length == 0) return Array.Empty<byte>();
            if (Gb2312 != null)
            {
                try { return Gb2312.GetBytes(norm); }
                catch { }
            }
            return Encoding.UTF8.GetBytes(norm);
        }
    }
}
