// -----------------------------------------------------------------------------
// VLTK Mobile — ST-12.x Text Resource runtime service
// Wraps PcTextResourceRegistry. PC source: settings/text/textresource.txt.
// Quản lý tài nguyên văn bản: tra cứu chuỗi tiếng Việt, fallback.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Text Resource (chuỗi dịch tiếng Việt): lookup, fallback.
    /// </summary>
    public class TextResourceService
    {
        public const string LogTag = "TextResource";
        public const string DefaultStreamingDir = "Reference/PcText";

        private PcTextResourceRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public TextResourceService() { }
        public TextResourceService(PcTextResourceRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcTextResourceRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "TextResource registry rỗng");
        }

        public string GetVietnamese(string key)
        {
            if (_reg == null || string.IsNullOrEmpty(key)) return null;
            var entry = _reg.Get(key);
            return entry?.vietnamese;
        }

        public string GetChinese(string key)
        {
            if (_reg == null || string.IsNullOrEmpty(key)) return null;
            var entry = _reg.Get(key);
            return entry?.chinese;
        }

        public string GetOrVietnamese(string key, string fallback)
        {
            var v = GetVietnamese(key);
            return !string.IsNullOrEmpty(v) ? v : (fallback ?? key);
        }

        public IEnumerable<string> GetAllKeys()
        {
            if (_reg == null) return Array.Empty<string>();
            return EnumerateKeys(_reg.All);
        }

        private static IEnumerable<string> EnumerateKeys(IEnumerable<PcTextResourceEntry> entries)
        {
            foreach (var entry in entries)
                yield return entry.key;
        }

        public IReadOnlyList<PcTextResourceEntry> All
            => _reg != null ? _reg.All : Array.Empty<PcTextResourceEntry>();

        public static TextResourceService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcTextResourceParser.BuildRegistry(dir);
            return new TextResourceService(reg);
        }
    }
}
