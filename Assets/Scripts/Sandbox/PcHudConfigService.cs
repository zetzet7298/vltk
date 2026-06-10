// -----------------------------------------------------------------------------
// VLTK Mobile — PC HUD Config Service
// Reads all PC UI config from StreamingAssets/Reference/PcUiConfig/ and provides
// 100% PC-authentic values for HUD rendering.
//
// PC source files:
//   pc_setting.ini    → [Map] colors (SelfColor, TeammateColor, etc.), [InfoString] connection messages
//   pc_miniskill.ini  → [BuffList] 204 buff/debuff definitions (24x24, font 12, colors)
//   pc_chatpics.ini   → [Main] 58 表情 SPR paths
//   faces.ini         → [List] 153 emotes with Tip, Text, Spr
//   pc_npcbobo.ini    → [Series], [Emotes], [Actions], [TransLife], [NationalEmblem], [FortuneRank]
//   pc_tradeinfo.ini  → [Main] Trade panel layout, [Labels], [Colors]
//   team_info.ini     → [Main] Team preview layout
//   wuxing.ini        → [Gold/Wood/Water/Fire/Earth] faction descriptions
//   ranking.ini       → [List] ranking categories
//   pc_adjustcolor.txt → 15 adjust colors (ID, ALPHA, R, G, B)
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    // ──────────────────────────── Data structures ────────────────────────────

    public struct PcMapColor
    {
        public byte r, g, b;
        public Color ToColor() => new Color32(r, g, b, 255);
        public override string ToString() => $"rgb({r},{g},{b})";
    }

    public struct PcBuffDef
    {
        public int id;
        public string name;
        public string imagePath;
        public string desc;
        public bool isDebuff;
    }

    public struct PcEmoteDef
    {
        public int index;
        public string tip;     // Vietnamese description
        public string text;    // shortcut text like ":)", ":D"
        public string sprPath; // SPR path
    }

    public struct PcAdjustColor
    {
        public int id;
        public byte alpha, r, g, b;
        public Color ToColor() => new Color32(r, g, b, alpha);
    }

    // ──────────────────────────── Service ────────────────────────────

    public class PcHudConfigService
    {
        public const string LogTag = "PcHudConfig";
        public const string ConfigRoot = "Reference/PcUiConfig";

        // Parsed data
        public PcMapColor SelfColor;
        public PcMapColor TeammateColor;
        public PcMapColor PlayerColor;
        public PcMapColor FightNpcColor;
        public PcMapColor NormalNpcColor;
        public PcMapColor SelfPartnerColor;
        public PcMapColor OtherPartnerColor;
        public PcMapColor OtherPlayerColor;
        public PcMapColor OtherNpcColor;
        public PcMapColor SelfNpcColor;
        public PcMapColor SelfPlayerColor;

        public readonly Dictionary<int, string> InfoStrings = new();
        public readonly List<PcBuffDef> Buffs = new();
        public readonly List<string> ChatPicPaths = new();
        public readonly List<PcEmoteDef> Emotes = new();
        public readonly List<PcAdjustColor> AdjustColors = new();
        public readonly Dictionary<string, string> TradeLabels = new();
        public readonly Dictionary<string, string> TeamInfo = new();
        public readonly Dictionary<string, string> WuxingTexts = new();

        // MiniSkill layout
        public int MiniSkillLeft = 170;
        public int MiniSkillTop = 48;
        public int BuffIconWidth = 24;
        public int BuffIconHeight = 24;
        public int BuffTimeFontSize = 12;
        public Color BuffTimeColor = new Color32(0, 255, 0, 255);
        public Color DebuffTimeColor = new Color32(255, 140, 0, 255);
        public Color WarningTimeColor = new Color32(255, 0, 0, 255);

        public string GameName = "Vo Lam Truyen Ky";
        public int SendMsgInterval = 800;
        public int SysMsgMoveInterval = 4;

        public bool Loaded { get; private set; }

        // ──────────────────────────── Load all ────────────────────────────

        public void LoadAll()
        {
            string root = Path.Combine(Application.streamingAssetsPath, ConfigRoot);
            if (!Directory.Exists(root))
            {
                SubsystemLog.Warn(LogTag, $"Config root not found: {root}");
                return;
            }

            LoadSettingIni(root);
            LoadMiniSkillIni(root);
            LoadChatPicsIni(root);
            LoadFacesIni(root);
            LoadAdjustColors(root);
            LoadTradeInfo(root);
            LoadTeamInfo(root);
            LoadWuxing(root);
            LoadNpcBobo(root);

            Loaded = true;
            SubsystemLog.Info(LogTag, $"Loaded PC HUD config: {InfoStrings.Count} infostrings, {Buffs.Count} buffs, {Emotes.Count} emotes, {ChatPicPaths.Count} chatpics, {AdjustColors.Count} colors");
        }

        // ──────────────────────────── pc_setting.ini ────────────────────────────

        private void LoadSettingIni(string root)
        {
            string path = Path.Combine(root, "pc_setting.ini");
            if (!File.Exists(path)) return;

            var ini = ParseIni(path);
            GameName = GetStr(ini, "Main", "GameName", "Vo Lam Truyen Ky");

            if (int.TryParse(GetStr(ini, "Main", "SendMsgInterval", "800"), out var smi))
                SendMsgInterval = smi;
            if (int.TryParse(GetStr(ini, "Main", "SysMsgMoveInterval", "4"), out var smmi))
                SysMsgMoveInterval = smmi;

            // [Map] colors
            SelfColor = ParseMapColor(GetStr(ini, "Map", "SelfColor", "255,255,0"));
            TeammateColor = ParseMapColor(GetStr(ini, "Map", "TeammateColor", "0,255,0"));
            PlayerColor = ParseMapColor(GetStr(ini, "Map", "PlayerColor", "255,72,0"));
            FightNpcColor = ParseMapColor(GetStr(ini, "Map", "FightNpcColor", "165,48,255"));
            NormalNpcColor = ParseMapColor(GetStr(ini, "Map", "NormalNpcColor", "165,48,255"));
            SelfPartnerColor = ParseMapColor(GetStr(ini, "Map", "SelfPartnerColor", "180,230,0"));
            OtherPartnerColor = ParseMapColor(GetStr(ini, "Map", "OtherPartnerColor", "255,128,0"));
            OtherPlayerColor = ParseMapColor(GetStr(ini, "Map", "OtherPlayerColor", "255,0,0"));
            OtherNpcColor = ParseMapColor(GetStr(ini, "Map", "OtherNpcColor", "255,155,155"));
            SelfNpcColor = ParseMapColor(GetStr(ini, "Map", "SelfNpcColor", "155,255,155"));
            SelfPlayerColor = ParseMapColor(GetStr(ini, "Map", "SelfPlayerColor", "0,255,0"));

            // [InfoString]
            if (ini.TryGetValue("InfoString", out var infoSec))
            {
                foreach (var kv in infoSec)
                {
                    if (int.TryParse(kv.Key, out var id))
                        InfoStrings[id] = kv.Value;
                }
            }
        }

        // ──────────────────────────── pc_miniskill.ini ────────────────────────────

        private void LoadMiniSkillIni(string root)
        {
            string path = Path.Combine(root, "pc_miniskill.ini");
            if (!File.Exists(path)) return;

            var ini = ParseIni(path);

            MiniSkillLeft = GetInt(ini, "Main", "Left", 170);
            MiniSkillTop = GetInt(ini, "Main", "Top", 48);
            BuffIconWidth = GetInt(ini, "BuffImage", "Width", 24);
            BuffIconHeight = GetInt(ini, "BuffImage", "Height", 24);
            BuffTimeFontSize = GetInt(ini, "txtBuffTime", "Font", 12);
            BuffTimeColor = ParseIniColor(GetStr(ini, "txtBuffTime", "Color", "0,255,0"));
            DebuffTimeColor = ParseIniColor(GetStr(ini, "txtDebuffTime", "Color", "255,140,0"));
            WarningTimeColor = ParseIniColor(GetStr(ini, "txtWarningTime", "Color", "255,0,0"));

            // [BuffList] Buff_N_ID=, Buff_N_Name=, Buff_N_Image=, Buff_N_Desc=, Buff_N_IsDebuff=
            int i = 0;
            while (true)
            {
                string prefix = $"Buff_{i}_";
                string idStr = null;
                if (ini.TryGetValue("BuffList", out var sec))
                {
                    string idKey = prefix + "ID";
                    if (!sec.TryGetValue(idKey, out idStr)) break;
                }
                else break;

                if (idStr == null) break;
                if (!int.TryParse(idStr, out var buffId)) { i++; continue; }

                var buff = new PcBuffDef
                {
                    id = buffId,
                    name = GetSectionStr(ini, "BuffList", prefix + "Name", ""),
                    imagePath = GetSectionStr(ini, "BuffList", prefix + "Image", ""),
                    desc = GetSectionStr(ini, "BuffList", prefix + "Desc", ""),
                    isDebuff = GetSectionStr(ini, "BuffList", prefix + "IsDebuff", "0") == "1"
                };
                Buffs.Add(buff);
                i++;
            }
        }

        // ──────────────────────────── pc_chatpics.ini ────────────────────────────

        private void LoadChatPicsIni(string root)
        {
            string path = Path.Combine(root, "pc_chatpics.ini");
            if (!File.Exists(path)) return;

            var ini = ParseIni(path);
            int count = GetInt(ini, "Main", "Count", 0);
            string basePath = GetStr(ini, "Main", "Path", "");

            for (int i = 0; i < count; i++)
            {
                if (ini.TryGetValue("Main", out var sec) && sec.TryGetValue(i.ToString(), out var sprFile))
                    ChatPicPaths.Add(basePath + "\\" + sprFile);
            }
        }

        // ──────────────────────────── faces.ini (153 表情) ────────────────────────────

        private void LoadFacesIni(string root)
        {
            string path = Path.Combine(root, "faces.ini");
            if (!File.Exists(path)) return;

            var ini = ParseIni(path);
            int count = GetInt(ini, "List", "Count", 0);

            for (int i = 1; i <= count; i++)
            {
                string section = $"Face{i}";
                if (!ini.ContainsKey(section)) continue;
                Emotes.Add(new PcEmoteDef
                {
                    index = i,
                    tip = GetStr(ini, section, "Tip", ""),
                    text = GetStr(ini, section, "Text", ""),
                    sprPath = GetStr(ini, section, "Spr", "")
                });
            }
        }

        // ──────────────────────────── adjustcolor.txt ────────────────────────────

        private void LoadAdjustColors(string root)
        {
            string path = Path.Combine(root, "pc_adjustcolor.txt");
            if (!File.Exists(path)) return;

            var lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("ID")) continue;
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                if (!int.TryParse(cols[0], out var id)) continue;
                AdjustColors.Add(new PcAdjustColor
                {
                    id = id,
                    alpha = byte.TryParse(cols[1], out var a) ? a : (byte)255,
                    r = byte.TryParse(cols[2], out var r) ? r : (byte)255,
                    g = byte.TryParse(cols[3], out var g) ? g : (byte)255,
                    b = byte.TryParse(cols[4], out var b) ? b : (byte)255
                });
            }
        }

        // ──────────────────────────── TradeInfo ────────────────────────────

        private void LoadTradeInfo(string root)
        {
            string path = Path.Combine(root, "pc_tradeinfo.ini");
            if (!File.Exists(path)) return;
            var ini = ParseIni(path);
            if (ini.TryGetValue("Labels", out var labels))
                foreach (var kv in labels) TradeLabels[kv.Key] = kv.Value;
        }

        // ──────────────────────────── Team Info ────────────────────────────

        private void LoadTeamInfo(string root)
        {
            string path = Path.Combine(root, "team_info.ini");
            if (!File.Exists(path)) return;
            var ini = ParseIni(path);
            if (ini.TryGetValue("TxtName", out var sec))
                foreach (var kv in sec) TeamInfo[kv.Key] = kv.Value;
        }

        // ──────────────────────────── Wuxing ────────────────────────────

        private void LoadWuxing(string root)
        {
            string path = Path.Combine(root, "wuxing.ini");
            if (!File.Exists(path)) return;
            var ini = ParseIni(path);
            foreach (var sec in ini)
            {
                if (sec.Key == "Gold" || sec.Key == "Wood" || sec.Key == "Water" || sec.Key == "Fire" || sec.Key == "Earth")
                {
                    if (sec.Value.TryGetValue("PropText", out var txt))
                        WuxingTexts[sec.Key] = txt;
                }
            }
        }

        // ──────────────────────────── NpcBobo ────────────────────────────

        private readonly Dictionary<string, Dictionary<string, string>> _npcbobo = new();
        private void LoadNpcBobo(string root)
        {
            string path = Path.Combine(root, "pc_npcbobo.ini");
            if (!File.Exists(path)) return;
            _npcbobo.Clear();
            var ini = ParseIni(path);
            foreach (var sec in ini)
            {
                _npcbobo[sec.Key] = sec.Value;
            }
        }

        // ──────────────────────────── INI parser ────────────────────────────

        private static Dictionary<string, Dictionary<string, string>> ParseIni(string path)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            string currentSection = "";
            Dictionary<string, string> current = new(StringComparer.OrdinalIgnoreCase);
            result[currentSection] = current;

            string[] lines;
            try { lines = PcItemCommon.ReadServerLines(path).ToArray(); }
            catch { try { lines = File.ReadAllLines(path); } catch { return result; } }

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    if (!result.ContainsKey(currentSection))
                    {
                        current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        result[currentSection] = current;
                    }
                    else current = result[currentSection];
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                current[key] = val;
            }
            return result;
        }

        // ──────────────────────────── Helpers ────────────────────────────

        private static string GetStr(Dictionary<string, Dictionary<string, string>> ini, string section, string key, string def)
        {
            if (ini.TryGetValue(section, out var sec) && sec.TryGetValue(key, out var val)) return val;
            return def;
        }

        private static string GetSectionStr(Dictionary<string, Dictionary<string, string>> ini, string section, string key, string def)
        {
            if (ini.TryGetValue(section, out var sec) && sec.TryGetValue(key, out var val)) return val;
            return def;
        }

        private static int GetInt(Dictionary<string, Dictionary<string, string>> ini, string section, string key, int def)
        {
            var s = GetStr(ini, section, key, "");
            return int.TryParse(s, out var v) ? v : def;
        }

        private static PcMapColor ParseMapColor(string csv)
        {
            var parts = csv.Split(',');
            var c = new PcMapColor();
            if (parts.Length >= 3)
            {
                byte.TryParse(parts[0].Trim(), out c.r);
                byte.TryParse(parts[1].Trim(), out c.g);
                byte.TryParse(parts[2].Trim(), out c.b);
            }
            return c;
        }

        private static Color ParseIniColor(string csv)
        {
            var parts = csv.Split(',');
            if (parts.Length >= 3 &&
                byte.TryParse(parts[0].Trim(), out var r) &&
                byte.TryParse(parts[1].Trim(), out var g) &&
                byte.TryParse(parts[2].Trim(), out var b))
                return new Color32(r, g, b, 255);
            return Color.white;
        }

        // ──────────────────────────── Static singleton ────────────────────────────

        private static PcHudConfigService _instance;
        public static PcHudConfigService Instance
        {
            get
            {
                if (_instance == null || !_instance.Loaded)
                {
                    _instance = new PcHudConfigService();
                    _instance.LoadAll();
                }
                return _instance;
            }
        }
    }
}
