using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using VLTK.Core;

namespace VLTK.UI
{
    [Serializable]
    public class BuffData
    {
        public int id;
        public string name;
        public string image;
        public string desc;
    }

    [Serializable]
    public class BuffListWrapper
    {
        public List<BuffData> buffs;
    }

    [Serializable]
    public class EmoteData
    {
        public int id;
        public string tip;
        public string text;
        public string spr;
    }

    [Serializable]
    public class EmoteListWrapper
    {
        public List<EmoteData> emotes;
    }

    [Serializable]
    public class RankingTitleData
    {
        public int index;
        public string name;
        public int id;
        public int @class;
        public int flag;
        public string unit;
    }

    [Serializable]
    public class RankingTitleListWrapper
    {
        public List<RankingTitleData> titles;
    }

    [Serializable]
    public class FactionIconData
    {
        public string id;
        public string abbrev;
        public string nameVi;
        public string element;
        public string color;
        public int placeholderSkillId;
    }

    [Serializable]
    public class FactionIconListWrapper
    {
        public List<FactionIconData> factions;
    }

    [Serializable]
    public class KeyValuePairData
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class HudSettingsData
    {
        public List<KeyValuePairData> map_colors;
        public List<KeyValuePairData> info_strings;
    }

    public class HudDataService
    {
        private static HudDataService _instance;
        public static HudDataService Instance => _instance ??= new HudDataService();

        private Dictionary<int, BuffData> _buffs = new();
        private List<EmoteData> _emotes = new();
        private Dictionary<int, RankingTitleData> _titles = new();
        private Dictionary<string, FactionIconData> _factions = new();
        private Dictionary<string, Color> _mapColors = new();
        private Dictionary<int, string> _infoStrings = new();

        public bool IsLoaded { get; private set; }

        public HudDataService()
        {
            // Async loading is triggered by the caller via LoadDataAsync().
            // Synchronous System.IO.File reads do not work on Android (jar:// path).
            IsLoaded = false;
        }

        /// <summary>
        /// Async data loader using UnityWebRequest so it works on Android/iOS where
        /// System.IO.File cannot read files inside the StreamingAssets APK archive.
        /// Callers should <c>StartCoroutine(service.LoadDataAsync())</c>.
        /// </summary>
        public IEnumerator LoadDataAsync()
        {
            string streamingPath = Application.streamingAssetsPath;

            // 1. Load buffs
            yield return LoadAndParse<BuffListWrapper>(
                System.IO.Path.Combine(streamingPath, "buff_list.json"),
                wrapper =>
                {
                    _buffs.Clear();
                    if (wrapper != null && wrapper.buffs != null)
                    {
                        foreach (var b in wrapper.buffs)
                        {
                            _buffs[b.id] = b;
                        }
                    }
                });

            // 2. Load emotes
            yield return LoadAndParse<EmoteListWrapper>(
                System.IO.Path.Combine(streamingPath, "emote_list.json"),
                wrapper =>
                {
                    _emotes.Clear();
                    if (wrapper != null && wrapper.emotes != null)
                    {
                        _emotes.AddRange(wrapper.emotes);
                    }
                });

            // 3. Load ranking titles
            yield return LoadAndParse<RankingTitleListWrapper>(
                System.IO.Path.Combine(streamingPath, "ranking_titles.json"),
                wrapper =>
                {
                    _titles.Clear();
                    if (wrapper != null && wrapper.titles != null)
                    {
                        foreach (var t in wrapper.titles)
                        {
                            _titles[t.id] = t;
                        }
                    }
                });

            // 4. Load faction icons
            yield return LoadAndParse<FactionIconListWrapper>(
                System.IO.Path.Combine(streamingPath, "faction_icons.json"),
                wrapper =>
                {
                    _factions.Clear();
                    if (wrapper != null && wrapper.factions != null)
                    {
                        foreach (var f in wrapper.factions)
                        {
                            _factions[f.id] = f;
                            _factions[f.abbrev] = f;
                        }
                    }
                });

            // 5. Load settings (map colors & info strings)
            yield return LoadAndParse<HudSettingsData>(
                System.IO.Path.Combine(streamingPath, "hud_settings.json"),
                settings =>
                {
                    _mapColors.Clear();
                    if (settings != null && settings.map_colors != null)
                    {
                        foreach (var kv in settings.map_colors)
                        {
                            if (ParseColor(kv.value, out Color c))
                            {
                                _mapColors[kv.key.ToLower()] = c;
                            }
                        }
                    }

                    _infoStrings.Clear();
                    if (settings != null && settings.info_strings != null)
                    {
                        foreach (var kv in settings.info_strings)
                        {
                            if (int.TryParse(kv.key, out int id))
                            {
                                _infoStrings[id] = kv.value;
                            }
                        }
                    }
                });

            IsLoaded = true;
        }

        private IEnumerator LoadAndParse<T>(string path, Action<T> onParsed)
        {
            using var req = UnityWebRequest.Get(path);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var data = JsonUtility.FromJson<T>(req.downloadHandler.text);
                    onParsed?.Invoke(data);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"HudDataService: failed to parse {path}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"HudDataService: could not load {path}: {req.error}");
            }
        }

        public BuffData GetBuff(int id)
        {
            return _buffs.TryGetValue(id, out var b) ? b : null;
        }

        public List<EmoteData> GetEmoteList()
        {
            return _emotes;
        }

        public RankingTitleData GetRankingTitle(int id)
        {
            return _titles.TryGetValue(id, out var t) ? t : null;
        }

        public FactionIconData GetFaction(string idOrAbbrev)
        {
            if (string.IsNullOrEmpty(idOrAbbrev)) return null;
            return _factions.TryGetValue(idOrAbbrev.ToLower(), out var f) ? f : null;
        }

        public Color GetMapColor(string key, Color defaultColor)
        {
            if (string.IsNullOrEmpty(key)) return defaultColor;
            return _mapColors.TryGetValue(key.ToLower(), out var c) ? c : defaultColor;
        }

        public string GetInfoString(int id)
        {
            return _infoStrings.TryGetValue(id, out var s) ? s : string.Empty;
        }

        private bool ParseColor(string rgbStr, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrEmpty(rgbStr)) return false;

            string[] parts = rgbStr.Split(',');
            if (parts.Length >= 3)
            {
                if (float.TryParse(parts[0], out float r) &&
                    float.TryParse(parts[1], out float g) &&
                    float.TryParse(parts[2], out float b))
                {
                    float a = 255f;
                    if (parts.Length >= 4)
                    {
                        float.TryParse(parts[3], out a);
                    }
                    color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                    return true;
                }
            }
            return false;
        }
    }
}
