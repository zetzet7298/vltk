// PC Đường Môn event-level data parsed from the pinned tangmen.lua slice.
// The parser and Link implementation are shared with the already audited Lua
// reader; this class owns only TangMen's ID-to-key and event semantics.
using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTangMenLuaLevelService
    {
        public const string DefaultLuaPath = "Assets/StreamingAssets/Reference/PcTangMenSkillLevelData.lua";

        private static readonly Dictionary<int, string> SkillIdToName = new()
        {
              [301] = "zhuixing_zhudian",
              [302] = "baoyu_lihua",
              [339] = "shehun_yueying",
              [1069] = "feidaotang150",
              [1070] = "nutang150",
              [1097] = "feidaotang150_2",
              [1098] = "nutang150_2",
              [1110] = "pili_dan",
              [1113] = "luanhuan_ji",
        };

        // Bounded temporal overrides. Keep separate from SkillIdToName: adding a
        // skill there also changes combat event gates.
        private static readonly Dictionary<int, string> MissileLifetimeSkillIdToName = new()
        {
              [302] = "baoyu_lihua",
              [1070] = "nutang150",
        };

        private static readonly Dictionary<int, string> MissileSpeedSkillIdToName = new()
        {
              [58] = "tianluo_diwang",
              [1069] = "feidaotang150",
              [1071] = "jiugong_feixing",
        };

        private static readonly object Sync = new();
        private static Dictionary<string, Dictionary<string, List<List<PcCaiBangLuaLevelService.LuaPoint>>>> _skills;
        private static string _loadedPath;

        public static bool Applies(int skillId) => SkillIdToName.ContainsKey(skillId);

        public static void Reset()
        {
            lock (Sync)
            {
                _skills = null;
                _loadedPath = null;
            }
        }

        public static void EnsureLoaded(string path = null)
        {
            string resolved = path ?? DefaultLuaPath;
            lock (Sync)
            {
                if (_skills != null && string.Equals(_loadedPath, resolved, StringComparison.Ordinal)) return;
                _skills = File.Exists(resolved)
                    ? PcCaiBangLuaLevelService.ParseGaibangLua(resolved)
                    : new Dictionary<string, Dictionary<string, List<List<PcCaiBangLuaLevelService.LuaPoint>>>>(StringComparer.Ordinal);
                _loadedPath = resolved;
            }
        }

        public static int GetSingleValue(int skillId, int level, string attribute, int slot = 1)
        {
            EnsureLoaded();
            if (!SkillIdToName.TryGetValue(skillId, out var key) || !_skills.TryGetValue(key, out var attrs)) return 0;
            if (!attrs.TryGetValue(attribute, out var slots) || slot < 1 || slot > slots.Count) return 0;
            var points = slots[slot - 1];
            return points == null || points.Count == 0 ? 0 : UnityEngine.Mathf.FloorToInt(PcCaiBangLuaLevelService.Link(level, points));
        }

        private static int GetSingleValue(string key, int level, string attribute, int slot = 1)
        {
            EnsureLoaded();
            if (!_skills.TryGetValue(key, out var attrs)) return 0;
            if (!attrs.TryGetValue(attribute, out var slots) || slot < 1 || slot > slots.Count) return 0;
            var points = slots[slot - 1];
            return points == null || points.Count == 0 ? 0 : UnityEngine.Mathf.FloorToInt(PcCaiBangLuaLevelService.Link(level, points));
        }

        public static int MissileLifetime(int skillId, int level) =>
            MissileLifetimeSkillIdToName.TryGetValue(skillId, out var key)
                ? GetSingleValue(key, level, "missle_lifetime_v")
                : 0;

        public static int MissileSpeed(int skillId, int level) =>
            MissileSpeedSkillIdToName.TryGetValue(skillId, out var key)
                ? GetSingleValue(key, level, "missle_speed_v")
                : 0;

        public static bool FlyEnabled(int skillId, int level) => GetSingleValue(skillId, level, "skill_flyevent", 1) > 0;
        public static int FlyInterval(int skillId, int level) => GetSingleValue(skillId, level, "skill_flyevent", 2);
        public static int FlySkillId(int skillId, int level) => GetSingleValue(skillId, level, "skill_flyevent", 3);
        public static int CollideEnabled(int skillId, int level) => GetSingleValue(skillId, level, "skill_collideevent", 1);
        public static int CollideSkillId(int skillId, int level) => GetSingleValue(skillId, level, "skill_collideevent", 3);
        public static int VanishEnabled(int skillId, int level) => GetSingleValue(skillId, level, "skill_vanishedevent", 1);
        public static int VanishSkillId(int skillId, int level) => GetSingleValue(skillId, level, "skill_vanishedevent", 3);
        public static int EventSkillLevel(int skillId, int level) => GetSingleValue(skillId, level, "skill_eventskilllevel", 1);
    }
}
