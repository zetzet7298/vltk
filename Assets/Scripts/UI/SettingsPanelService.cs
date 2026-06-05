// -----------------------------------------------------------------------------
// VLTK Mobile — UI Settings Panel Service (Bảng Cài Đặt)
// Quản lý key-value settings cho client: đồ họa, âm thanh, gameplay, UI, mạng.
// Vietnamese: "Cài Đặt", "Đồ Họa", "Âm Thanh", "Lối Chơi", "Giao Diện", "Mạng".
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace VLTK.UI
{
    public readonly struct SettingsPanelRow
    {
        public readonly int settingId;
        public readonly string key;
        public readonly string displayName;
        public readonly string value;
        public readonly int type;          // 0=bool, 1=int, 2=float, 3=string, 4=key
        public readonly int category;      // 0=Graphics, 1=Audio, 2=Gameplay, 3=UI, 4=Network, 5=Mobile
        public readonly string description;

        public SettingsPanelRow(int settingId, string key, string displayName, string value, int type, int category, string description)
        {
            this.settingId = settingId;
            this.key = key ?? string.Empty;
            this.displayName = displayName ?? string.Empty;
            this.value = value ?? string.Empty;
            this.type = type;
            this.category = category;
            this.description = description ?? string.Empty;
        }
    }

    public sealed class SettingsPanelSnapshot
    {
        public int playerId;
        public IReadOnlyList<SettingsPanelRow> rows;
    }

    /// <summary>
    /// Panel service Cài Đặt — lưu trữ key-value settings, phân loại theo category.
    /// </summary>
    public static class SettingsPanelService
    {
        public const int CategoryGraphics = 0;
        public const int CategoryAudio = 1;
        public const int CategoryGameplay = 2;
        public const int CategoryUI = 3;
        public const int CategoryNetwork = 4;
        public const int CategoryMobile = 5;

        public const int TypeBool = 0;
        public const int TypeInt = 1;
        public const int TypeFloat = 2;
        public const int TypeString = 3;
        public const int TypeKey = 4;

        private static readonly Dictionary<string, string> _store = new Dictionary<string, string>();

        public static SettingsPanelSnapshot BuildSnapshot()
        {
            var snap = new SettingsPanelSnapshot
            {
                playerId = 0,
                rows = new List<SettingsPanelRow>(),
            };
            try
            {
                var list = new List<SettingsPanelRow>();
                int id = 0;
                foreach (var kv in _store)
                {
                    int cat = CategoryGameplay;
                    if (kv.Key.StartsWith("graphics_")) cat = CategoryGraphics;
                    else if (kv.Key.StartsWith("audio_")) cat = CategoryAudio;
                    else if (kv.Key.StartsWith("ui_")) cat = CategoryUI;
                    else if (kv.Key.StartsWith("network_")) cat = CategoryNetwork;
                    else if (kv.Key.StartsWith("mobile_")) cat = CategoryMobile;

                    int type = TypeString;
                    bool parsed = bool.TryParse(kv.Value, out _);
                    if (parsed) type = TypeBool;
                    else if (int.TryParse(kv.Value, out _)) type = TypeInt;
                    else if (float.TryParse(kv.Value, out _)) type = TypeFloat;

                    list.Add(new SettingsPanelRow(
                        settingId: id++,
                        key: kv.Key,
                        displayName: kv.Key,
                        value: kv.Value,
                        type: type,
                        category: cat,
                        description: "Cài đặt: " + kv.Key
                    ));
                }
                snap.rows = list;
            }
            catch { }
            return snap;
        }

        public static bool GetBool(string key, bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            if (_store.TryGetValue(key, out var v))
            {
                if (bool.TryParse(v, out var b)) return b;
                if (int.TryParse(v, out var i)) return i != 0;
            }
            return defaultValue;
        }

        public static bool SetBool(string key, bool value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            _store[key] = value ? "true" : "false";
            return true;
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            if (_store.TryGetValue(key, out var v) && int.TryParse(v, out var i)) return i;
            return defaultValue;
        }

        public static bool SetInt(string key, int value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            _store[key] = value.ToString();
            return true;
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            if (_store.TryGetValue(key, out var v) && float.TryParse(v, out var f)) return f;
            return defaultValue;
        }

        public static bool SetFloat(string key, float value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            _store[key] = value.ToString();
            return true;
        }

        public static string GetString(string key, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(key)) return defaultValue;
            return _store.TryGetValue(key, out var v) ? v : defaultValue;
        }

        public static bool SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            _store[key] = value ?? string.Empty;
            return true;
        }

        public static IReadOnlyList<SettingsPanelRow> GetByCategory(int category)
        {
            var snap = BuildSnapshot();
            var filtered = new List<SettingsPanelRow>();
            foreach (var r in snap.rows)
                if (r.category == category) filtered.Add(r);
            return filtered;
        }

        public static void Reset()
        {
            _store.Clear();
        }

        public static void ResetCategory(int category)
        {
            var snap = BuildSnapshot();
            foreach (var r in snap.rows)
                if (r.category == category) _store.Remove(r.key);
        }
    }
}
