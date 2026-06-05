// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX AttribConst Service (Thuộc tính hằng số runtime)
// Wraps PcAttribConstRegistry. Exposes section list, individual key/value
// entries, and a magic-code resolver for magicdesc-style sections.
// Vietnamese: "Thuộc Tính", "Hằng Số", "Công Thức", "Ma Pháp".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Một entry key=value trong section thuộc tính hằng số.</summary>
    [Serializable]
    public class PcAttribConstEntry
    {
        public string section;
        public string key;
        public string value;
        public int index;       // thứ tự trong section (0..count-1)
    }

    /// <summary>
    /// Service quản lý thuộc tính hằng số trong game (state skill ignore, magic
    /// desc, role value, gamesetting…). PC source: settings/attribconstdata.ini,
    /// magicdesc.ini, rolevalue.ini, gamesetting.ini.
    /// </summary>
    public class AttribConstService
    {
        public const string LogTag = "Attrib";

        private PcAttribConstRegistry _registry;
        private readonly Dictionary<string, List<PcAttribConstEntry>> _sectionIndex = new();

        /// <summary>Sự kiện khi toàn bộ thuộc tính load xong.</summary>
        public event Action OnAttribLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public AttribConstService() { }

        public AttribConstService(PcAttribConstRegistry registry)
        {
            AttachRegistry(registry);
        }

        public void AttachRegistry(PcAttribConstRegistry registry)
        {
            _registry = registry ?? new PcAttribConstRegistry();
            RebuildIndex();
        }

        private void RebuildIndex()
        {
            _sectionIndex.Clear();
            if (_registry == null) return;
            foreach (var s in _registry.GetAll())
            {
                BuildSection(s.name);
            }
        }

        private void BuildSection(string section)
        {
            if (_registry == null) return;
            var sec = _registry.Get(section);
            if (sec == null) return;
            var list = new List<PcAttribConstEntry>();
            int idx = 0;
            // Sắp xếp key Data0..DataN theo thứ tự số
            var sorted = new List<KeyValuePair<string, string>>(sec.data);
            sorted.Sort((a, b) => CompareDataKey(a.Key, b.Key));
            foreach (var kv in sorted)
            {
                list.Add(new PcAttribConstEntry
                {
                    section = section,
                    key = kv.Key,
                    value = kv.Value,
                    index = idx++,
                });
            }
            // Append extras
            foreach (var kv in sec.extras)
            {
                list.Add(new PcAttribConstEntry
                {
                    section = section,
                    key = kv.Key,
                    value = kv.Value,
                    index = -1,
                });
            }
            _sectionIndex[section] = list;
        }

        private static int CompareDataKey(string a, string b)
        {
            int na = ExtractDataIndex(a);
            int nb = ExtractDataIndex(b);
            return na.CompareTo(nb);
        }

        private static int ExtractDataIndex(string key)
        {
            if (string.IsNullOrEmpty(key)) return int.MaxValue;
            if (key.Length <= 4) return int.MaxValue;
            string tail = key.Substring(4);
            return int.TryParse(tail, out int v) ? v : int.MaxValue;
        }

        // ── Query APIs ────────────────────────────────────────────────

        public IReadOnlyList<PcAttribConstEntry> GetSection(string section)
        {
            if (string.IsNullOrEmpty(section)) return Array.Empty<PcAttribConstEntry>();
            if (!_sectionIndex.TryGetValue(section, out var list))
            {
                // Lazy build
                BuildSection(section);
                _sectionIndex.TryGetValue(section, out list);
            }
            return list ?? (IReadOnlyList<PcAttribConstEntry>)Array.Empty<PcAttribConstEntry>();
        }

        public IEnumerable<string> GetAllSections()
        {
            if (_registry == null) yield break;
            foreach (var s in _registry.GetAll())
                yield return s.name;
        }

        public string GetValue(string section, string key)
        {
            if (_registry == null) return null;
            var sec = _registry.Get(section);
            if (sec == null) return null;
            if (sec.data.TryGetValue(key ?? string.Empty, out var v)) return v;
            if (sec.extras.TryGetValue(key ?? string.Empty, out v)) return v;
            return null;
        }

        public int GetInt(string section, string key, int fallback = 0)
        {
            var v = GetValue(section, key);
            if (string.IsNullOrEmpty(v)) return fallback;
            return int.TryParse(v, out int n) ? n : fallback;
        }

        /// <summary>
        /// Tìm mã số (int) cho một key trong section kiểu magicdesc. Trả về -1 nếu không tìm thấy.
        /// </summary>
        public int ResolveMagicCode(string section, string key)
        {
            if (_registry == null) return -1;
            var sec = _registry.Get(section);
            if (sec == null) return -1;
            if (sec.data.TryGetValue(key ?? string.Empty, out var v))
                return int.TryParse(v, out int n) ? n : -1;
            return -1;
        }

        // ── Loading ───────────────────────────────────────────────────

        public static AttribConstService LoadFromStreamingAssets()
        {
            var svc = new AttribConstService();
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcAttrib");
            if (Directory.Exists(dir))
            {
                var reg = PcAttribConstParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
                SubsystemLog.Info(LogTag, $"AttribConstService loaded {reg.Count} section(s) từ {dir}");
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"AttribConstService: directory không tồn tại {dir}");
            }
            svc.OnAttribLoaded?.Invoke();
            return svc;
        }
    }
}
