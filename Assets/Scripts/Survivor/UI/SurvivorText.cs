// -----------------------------------------------------------------------------
// VLTK Mobile — Survivor i18n VN/EN bundle service (ticket 38)
// Pattern copy TextResourceService (Sandbox): registry + lookup + fallback.
// Key namespace: survivor.<screen>.<key> (skill: survivor.skill.<id>.name/.desc).
// Fallback chain: current lang -> vi -> raw key. Empty text treated as missing.
// Runtime switch: SetLanguage fires Changed event -> UI refresh, no restart.
// Unity Localization (1.5.12 installed) = upgrade path only (spec D14), NOT v1.
// Class thuần — không MonoBehaviour, không scene. Test seam: EditMode pure-logic.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Survivor
{
    [Serializable]
    public sealed class SurvivorTextEntry
    {
        public string key;
        public string text;
    }

    [Serializable]
    public sealed class SurvivorTextBundleData
    {
        public string lang;
        public SurvivorTextEntry[] entries;
    }

    /// <summary>Loader: đọc bundle JSON từ StreamingAssets/SurvivorText/{lang}.json.</summary>
    public static class SurvivorTextLoader
    {
        public const string DefaultStreamingDir = "SurvivorText";

        public static string BundlePath(string lang)
            => Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir, lang + ".json");

        /// <summary>null khi file thiếu/parse lỗi — caller fallback sẵn.</summary>
        public static List<SurvivorTextEntry> Load(string lang)
        {
            string path = BundlePath(lang);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            var data = JsonUtility.FromJson<SurvivorTextBundleData>(File.ReadAllText(path));
            if (data == null || data.entries == null) return null;
            return new List<SurvivorTextEntry>(data.entries);
        }
    }

    /// <summary>
    /// Survivor i18n service. Lookup: lang hiện tại → vi → raw key.
    /// Bundle đăng ký qua RegisterBundle (test inject) hoặc LoadFromStreamingAssets.
    /// </summary>
    public sealed class SurvivorText
    {
        public const string FallbackLang = "vi";

        private readonly Dictionary<string, Dictionary<string, string>> _bundles = new(StringComparer.Ordinal);
        private string _lang = FallbackLang;

        /// <summary>Fire khi SetLanguage đổi lang (param = lang mới) — UI subscribe để refresh text.</summary>
        public event Action<string> Changed;

        public string Language => _lang;

        public int Count
            => _bundles.TryGetValue(_lang, out var b) ? b.Count : 0;

        public IReadOnlyCollection<string> Languages => _bundles.Keys;

        public void SetLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang)) lang = FallbackLang;
            if (lang == _lang) return;
            _lang = lang;
            Changed?.Invoke(_lang);
        }

        public void RegisterBundle(string lang, IReadOnlyList<SurvivorTextEntry> entries)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (e == null || string.IsNullOrEmpty(e.key)) continue;
                    map[e.key] = e.text ?? string.Empty;
                }
            }
            _bundles[lang ?? FallbackLang] = map;
        }

        public string Get(string key) => Get(key, _lang);

        /// <summary>Chain 3 tầng: lang → vi → raw key. Empty text = missing → fallback tiếp.</summary>
        public string Get(string key, string lang)
        {
            if (string.IsNullOrEmpty(key)) return key ?? string.Empty;
            if (TryGet(key, lang, out var t)) return t;
            if (lang != FallbackLang && TryGet(key, FallbackLang, out t)) return t;
            return key;
        }

        public bool TryGet(string key, string lang, out string text)
        {
            text = null;
            if (string.IsNullOrEmpty(lang) || string.IsNullOrEmpty(key)) return false;
            if (_bundles.TryGetValue(lang, out var b) && b.TryGetValue(key, out text))
                return !string.IsNullOrEmpty(text);
            return false;
        }

        public static SurvivorText LoadFromStreamingAssets()
        {
            var t = new SurvivorText();
            foreach (var lang in new[] { FallbackLang, "en" })
            {
                var entries = SurvivorTextLoader.Load(lang);
                if (entries != null) t.RegisterBundle(lang, entries);
            }
            return t;
        }
    }
}
