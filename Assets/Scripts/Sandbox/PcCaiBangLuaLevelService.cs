// -----------------------------------------------------------------------------
// VLTK Mobile — PC Cái Bang per-level skill values parsed from Lua SKILLS dict.
//
// Source of truth: /var/www/vltk-mobile/Assets/StreamingAssets/Reference/gaibang.lua
//   (562 dòng, full SKILLS dict with 3-slot interpolation tables
//   {{level,value,func}, ...} per magic attrib).
//
// Format spec (xem gaibang.lua lines 11-385 + 397-472):
//   - SKILLS = { skillname = { magicattrib = { [1] = points1, [2] = points2, [3] = points3 } } }
//   - Mỗi points[i] = list of (level, value, funcName?). funcName optional: "Line" (default),
//     "Conic" (quadratic), "Extrac" (sqrt). PC `GetSkillLevelData` returns Param2String(p1,p2,p3).
//   - skillname khớp với cột LvlData trong skills.txt (vd "yanmen_tuobo", "kanglong_youhui").
//
// Migration note (2026-06-17): thay thế cho PcCaiBangSkillTuning + PcCaiBangModTuning
// đã cứng interpolation tables. Service này đọc từ Lua runtime data, không hardcode
// giá trị nào — mọi cập nhật từ PC team tự động được áp dụng khi skillId lookup
// vào SKILLS dict.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// PC Cái Bang per-skill level interpolation parsed từ
    /// <c>Assets/StreamingAssets/Reference/gaibang.lua</c>. Source of truth cho mọi
    /// runtime gameplay tuning (attack radius, missile speed/count, mana cost, series
    /// damage %, magic attrib ranges). Thay thế cho 2 file hardcoded
    /// <c>PcCaiBangSkillTuning</c> + <c>PcCaiBangModTuning</c>.
    /// </summary>
    public static class PcCaiBangLuaLevelService
    {
        /// <summary>Default path tới PC-truth Lua. Dùng cho cả editor test và runtime.</summary>
        public const string DefaultLuaPath = "Assets/StreamingAssets/Reference/gaibang.lua";

        // skillId → skillname (khớp cột LvlData trong skills.txt / tên key trong SKILLS dict).
        // Mở rộng dần khi port thêm skill mới; mỗi entry KHÔNG tự ý chuyển hướng tới
        // skill khác (vd 274 "Giáng Long Chưởng" → xianglong_zhang PC gốc).
        private static readonly Dictionary<int, string> SkillIdToName = new()
        {
            // Passive/buff (LvlData inline trong PC gaibang.lua, không có skill file riêng).
            [115] = "gaibang_bangfa",
            [116] = "gaibang_zhangfa",
            [127] = "huabu_liushou",       // Hoạt Bất Lưu Thủ
            [130] = "zuidie_kuangwu",      // Túy Điệp Cuồng Vũ

            // Active damage skills.
            // [CaiBang-FailClosed117 2026-07-17] PC skills.txt row 117 (Đầu Thạch Vấn Lộ) IsPhysical=0
            //   and LvlData1="skill_cost_v" ONLY — no 沿门托钵/yanmen_tuobo reference and no damage/level
            //   interpolation table (verified jx-pc pak_unpacked/update03/settings/skills.txt col 73).
            //   The former `[117] = "yanmen_tuobo"` borrow was a fabricated guess that surfaced 119's full
            //   table (radius scaling, missile speed, fire curve) for 117. Fail closed: 117 is NOT mapped,
            //   so PcCaiBangLuaLevelService returns 0 and callers fall back to catalog/engine defaults.
            [119] = "yanmen_tuobo",        // Duyên Môn Thoa Bát (PC IsPhysical=1, LvlData1=yanmen_tuobo).
            [122] = "jianren_shenshou",    // Kiến Nhân Thân Thủ.
            [125] = "bangda_egou",         // Bổng Đả Ác Cẩu (newest PC Skills.txt row 125; chains to 359 + 1074).
            [127] = "huabu_liushou",       // Hoạt Bất Lưu Thủ.
            [128] = "kanglong_youhui",     // Kháng Long Hữu Hối.
            [129] = "huaxian_weiyi",       // Hóa Hiểm Vi Di (slistcache LvlData1; meleedamagereturn_p + anti_block_rate).
            [130] = "zuidie_kuangwu",      // Túy Điệp Cuồng Vũ.
            [357] = "feilong_zaitian",     // Phi Long Tại Thiên.
            [358] = "qianlong_zaiyuan",    // Tiềm Long Tại Uyên (newest PC row 358; commented Lua table means row/default data wins).
            [359] = "tianxia_wugou",       // Thiên Hạ Vô Cẩu (MOD id, reuse 125 table).
            [389] = "longzhan_yuye",        // Long Chiến Ư Dã (Phi Long collide-event sub-skill; slistcache LvlData1).
            [1072] = "zhanggaibang150_2",  // Ngũ Diệu Càn Khôn (1073 collide-event sub-skill; slistcache LvlData1).
            [1073] = "zhanggaibang150",    // Thời Thặng Lục Long.
            [1074] = "gungaibang150",      // Bổng Hoành Lược Mã.
            [1101] = "zhanggaibang150",    // Multi-target variant, reuse 1073 table.
            [1103] = "zhanggaibang150",    // No-script variant, dùng cùng bảng 1073.
            [1161] = "zhanggaibang150",    // NPC variant.
            [1162] = "gungaibang150",      // NPC variant.
            [1539] = "tianxia_wugou",      // Thiên Hạ Vô Cẩu NPC/player-table variant.

            // NPC/MOD reuse.
            [209] = "dagou_zhen",          // Bang Đả Cẩu (NPC variant của 124).
            [274] = "xianglong_zhang",     // Bang Hàn Băng / Giáng Long Chưởng (reuse NPC lifemax/manamax/fire magic).
            [277] = "huabu_liushou",       // Bang Hộ Bộ (NPC variant của 127).
            [360] = "xiaoyao_gong",        // Tiêu Dao Công (castspeed/attackspeed).

            // 120-series.
            [714] = "gaibang120",          // Bang Huyết Chiến (autoattackskill).
            [720] = "gaibang120zuzhou",    // Bang HC Nguyền.
        };

        // MagicAttrib name → key trong SKILLS dict. Một số tên C++ dài hơn nhưng key PC ngắn:
        //   addphysicsdamage_p       -> addphysicsdamage_p
        //   skill_attackradius       -> skill_attackradius
        //   missle_speed_v           -> missle_speed_v (chú ý PC viết "missle", không phải "missile")
        //   skill_misslenum_v        -> skill_misslenum_v
        //   skill_cost_v             -> skill_cost_v
        //   seriesdamage_p           -> seriesdamage_p
        //   physicsdamage_v          -> physicsdamage_v
        //   firedamage_v             -> firedamage_v
        //   physicsenhance_p         -> physicsenhance_p
        //   addskilldamageN          -> addskilldamageN (N=1..N)
        //   addfiremagic_v           -> addfiremagic_v
        //   addfiredamage_v          -> addfiredamage_v
        //   deadlystrikeenhance_p    -> deadlystrikeenhance_p
        //   meleedamagereturn_p      -> meleedamagereturn_p
        //   rangedamagereturn_p      -> rangedamagereturn_p
        //   allres_p                 -> allres_p
        //   fastwalkrun_p            -> fastwalkrun_p
        //   staminamax_p             -> staminamax_p
        //   lifemax_p                -> lifemax_p
        //   manamax_p                -> manamax_p
        //   fireres_p                -> fireres_p
        //   coldres_p                -> coldres_p
        //   poisonres_p              -> poisonres_p
        //   lightingres_p            -> lightingres_p
        //   attackspeed_v            -> attackspeed_v
        //   castspeed_v              -> castspeed_v
        //   skill_misslesform_v      -> skill_misslesform_v
        //   skill_param1_v           -> skill_param1_v
        //   skill_param2_v           -> skill_param2_v
        //   skill_eventskilllevel    -> skill_eventskilllevel
        //   skill_skillexp_v         -> skill_skillexp_v
        //   skill_showevent          -> skill_showevent
        //   skill_collideevent       -> skill_collideevent
        //   autoattackskill          -> autoattackskill

        /// <summary>Default attack radius fallback khi skillId không có trong SKILLS dict.</summary>
        public const int DefaultAttackRadius = 320;
        public const int DefaultMissileSpeed = 12;
        public const int DefaultMissileCount = 1;
        public const int DefaultManaCost = 0;
        public const int DefaultSeriesDamageP = 0;

        /// <summary>Skill ID nào được phục vụ bởi service này (Cái Bang range).</summary>
        public static bool Applies(int skillId) => SkillIdToName.ContainsKey(skillId);

        // Singleton state. Lazy load trên truy cập đầu tiên. Editor & test đều share.
        private static readonly object s_lock = new();
        private static Dictionary<string, Dictionary<string, List<List<LuaPoint>>>> s_skills;
        private static string s_loadedFromPath;

        /// <summary>Parse gaibang.lua, expose data, return root SKILLS dict.</summary>
        /// <remarks>
        /// Chỉ parse được sub-set mà mobile cần (SKILLS dict + helper Line/Conic/Extrac
        /// evaluation). Không cần full Lua VM. Sử dụng lightweight tokenizer
        /// (recursive-descent thủ công) thay vì kéo dependency ngoài.
        /// </remarks>
        public static void EnsureLoaded(string absolutePath = null)
        {
            lock (s_lock)
            {
                if (s_skills != null && string.Equals(s_loadedFromPath, absolutePath ?? DefaultLuaPath, StringComparison.Ordinal))
                    return;
                s_skills = ParseGaibangLua(absolutePath ?? DefaultLuaPath);
                s_loadedFromPath = absolutePath ?? DefaultLuaPath;
            }
        }

        /// <summary>Reset cache (dùng cho test).</summary>
        public static void Reset()
        {
            lock (s_lock)
            {
                s_skills = null;
                s_loadedFromPath = null;
            }
        }

        /// <summary>Path Lua thực tế đã load (test diagnostic).</summary>
        public static string LoadedPath => s_loadedFromPath;

        // ==================== Public lookup API ====================

        /// <summary>
        /// PC gaibang.lua <c>skill_attackradius</c> at level. Returns 0 nếu skillId
        /// không có trong SKILLS dict (caller fallback về engine value).
        /// </summary>
        public static int GetAttackRadius(int skillId, int level)
        {
            return GetSingleValue(skillId, level, "skill_attackradius", 1);
        }

        /// <summary>
        /// PC gaibang.lua <c>missle_speed_v</c> at level. Returns 0 nếu skillId không
        /// có override (caller fallback về engine missles.txt Speed). Sentinel 0 cho
        /// phép caller phân biệt "Lua có override" vs "không có — dùng engine value".
        /// </summary>
        public static int GetMissileSpeed(int skillId, int level)
        {
            return GetSingleValue(skillId, level, "missle_speed_v", 1);
        }

        /// <summary>
        /// PC gaibang.lua <c>skill_misslenum_v</c> at level. Returns 0 nếu skillId
        /// không có override (caller fallback về catalog childSkillNum).
        /// Sử dụng ROUND-based interpolation (PC Lua Link() mặc định floor, nhưng
        /// skill_misslenum_v cần round để match legacy InterpolateIntRound và
        /// PC runtime GetSkillLevelData cho integer missile counts).
        /// </summary>
        public static int GetMissileCount(int skillId, int level)
        {
            return GetSingleValueRound(skillId, level, "skill_misslenum_v", 1);
        }

        /// <summary>
        /// PC gaibang.lua <c>skill_cost_v</c> at level. Returns 0 nếu skillId không
        /// có cost attrib (caller fallback về engine/catalog cost).
        /// </summary>
        public static int GetManaCost(int skillId, int level)
        {
            return GetSingleValue(skillId, level, "skill_cost_v", 1);
        }

        /// <summary>
        /// PC gaibang.lua <c>seriesdamage_p</c> at level. Returns 0 nếu skillId
        /// không có (caller fallback về 0).
        /// </summary>
        public static int GetSeriesDamageP(int skillId, int level)
        {
            return GetSingleValue(skillId, level, "seriesdamage_p", 1);
        }

        /// <summary>
        /// Lấy range damage (P1, P2, P3) cho magic attrib thuộc loại _v tại level.
        /// P1 = min, P2 = mid (=0 cho _v), P3 = max. Nếu min == max (P3 = P1 in some
        /// attribs như firedamage_v của thiên hạ vô cẩu) trả về cùng giá trị.
        /// </summary>
        public static (int min, int max) GetDamageRange(int skillId, int level, string magicAttrib)
        {
            EnsureLoaded();
            int p1 = GetSingleValue(skillId, level, magicAttrib, 1);
            int p3 = GetSingleValue(skillId, level, magicAttrib, 3);
            if (p1 == 0 && p3 == 0) return (0, 0);
            int max = p3 > 0 ? p3 : p1;
            return (p1, max);
        }

        /// <summary>
        /// Skill missile form ở level (1 = straight line, 2 = fan, 0 = stationary).
        /// Tương ứng <c>skill_misslesform_v</c>. Trả về -1 nếu skillId không có.
        /// </summary>
        public static int GetMissileForm(int skillId, int level)
        {
            return GetSingleValue(skillId, level, "skill_misslesform_v", 1);
        }

        /// <summary>
        /// Engine-readable SKILLS dict, dùng cho test debug/inspection. Trả về null nếu
        /// chưa load. Mỗi skill là dict { magicAttrib → { slotIndex (1/2/3) → list LuaPoint } }.
        /// </summary>
        public static IReadOnlyDictionary<string, Dictionary<string, List<List<LuaPoint>>>> SkillsOrNull
        {
            get { EnsureLoaded(); return s_skills; }
        }

        // ==================== Internal parsing ====================

        /// <summary>One control point trong interpolation table: (level, value, func).</summary>
        public readonly struct LuaPoint
        {
            public readonly int Level;
            public readonly float Value;
            public readonly string Func;
            public LuaPoint(int level, float value, string func) { Level = level; Value = value; Func = func; }
        }

        /// <summary>
        /// Raw single-slot value tại (skillId, level, magicAttrib, slotIndex). Public để test
        /// parity probing — production callers dùng typed wrappers (GetAttackRadius, v.v.).
        /// Trả về 0 nếu skillId/magicAttrib/slot không có trong SKILLS dict.
        /// </summary>
        public static int GetSingleValue(int skillId, int level, string magicAttrib, int slotIndex)
        {
            EnsureLoaded();
            if (!SkillIdToName.TryGetValue(skillId, out var skillName)) return 0;
            if (s_skills == null) return 0;
            if (!s_skills.TryGetValue(skillName, out var attribs)) return 0;
            if (!attribs.TryGetValue(magicAttrib, out var slots)) return 0;
            if (slots.Count < slotIndex) return 0;
            var points = slots[slotIndex - 1];
            if (points == null || points.Count == 0) return 0;
            return FloorToInt(Link(level, points));
        }

        /// <summary>
        /// Same as <see cref="GetSingleValue"/> nhưng dùng ROUND thay vì FLOOR. Dùng cho
        /// integer counts như skill_misslenum_v (PC legacy InterpolateIntRound parity).
        /// </summary>
        public static int GetSingleValueRound(int skillId, int level, string magicAttrib, int slotIndex)
        {
            EnsureLoaded();
            if (!SkillIdToName.TryGetValue(skillId, out var skillName)) return 0;
            if (s_skills == null) return 0;
            if (!s_skills.TryGetValue(skillName, out var attribs)) return 0;
            if (!attribs.TryGetValue(magicAttrib, out var slots)) return 0;
            if (slots.Count < slotIndex) return 0;
            var points = slots[slotIndex - 1];
            if (points == null || points.Count == 0) return 0;
            return Mathf.RoundToInt(Link(level, points));
        }

        /// <summary>
        /// PC Lua <c>Link(x, points)</c> — piecewise interpolation across the points list.
        /// Mirrors the semantics ở gaibang.lua lines 447-472 + Line/Conic/Extrac helpers
        /// lines 397-438. Default func = Line (linear). Returns -1 if &lt;2 points.
        /// </summary>
        public static float Link(int x, List<LuaPoint> points)
        {
            if (points == null || points.Count < 2) return points != null && points.Count == 1 ? points[0].Value : -1f;
            int num = points.Count;
            // PC fills default func = Line if missing.
            for (int i = 0; i < num; i++)
            {
                if (string.IsNullOrEmpty(points[i].Func))
                    points[i] = new LuaPoint(points[i].Level, points[i].Value, "Line");
            }
            if (x < points[0].Level)
                return CallFunc(points[0].Func, x, points[0].Level, points[0].Value, points[1].Level, points[1].Value);
            if (x > points[num - 1].Level)
                return CallFunc(points[num - 1].Func, x, points[num - 2].Level, points[num - 2].Value, points[num - 1].Level, points[num - 1].Value);
            for (int i = 1; i < num; i++)
            {
                if (x >= points[i - 1].Level && x <= points[i].Level)
                    return CallFunc(points[i].Func, x, points[i - 1].Level, points[i - 1].Value, points[i].Level, points[i].Value);
            }
            return points[num - 1].Value;
        }

        // Line: linear, f(x) = (y2-y1)*(x-x1)/(x2-x1) + y1
        // Conic: quadratic, f(x) = (y2-y1)*x²/(x2²-x1²) - (y2-y1)*x1²/(x2²-x1²) + y1
        // Extrac: sqrt-shaped, f(x) = (y2-y1)*x/(sqrt(x2)-sqrt(x1)) + y1 - (y2-y1)/(sqrt(x2)-sqrt(x1))
        // Gaibang uses Line (default) + Conic (a few 2nd-degree curves) + Extrac (sqrt).
        // Conic/Extrac guard: if either endpoint < 0, return 0.
        private static float CallFunc(string func, int x, int x1, float y1, int x2, float y2)
        {
            if (string.Equals(func, "Conic", StringComparison.OrdinalIgnoreCase))
            {
                if (x1 < 0 || x2 < 0) return 0f;
                if (x2 == x1) return y2;
                float denom = x2 * x2 - x1 * x1;
                if (Math.Abs(denom) < 1e-9f) return y2;
                return (y2 - y1) * x * x / denom - (y2 - y1) * x1 * x1 / denom + y1;
            }
            if (string.Equals(func, "Extrac", StringComparison.OrdinalIgnoreCase))
            {
                if (x1 < 0 || x2 < 0) return 0f;
                float s1 = Mathf.Sqrt(x1);
                float s2 = Mathf.Sqrt(x2);
                if (Math.Abs(s2 - s1) < 1e-9f) return y2;
                return (y2 - y1) * (Mathf.Sqrt(x) - s1) / (s2 - s1) + y1;
            }
            // Default + "Line": linear.
            if (x2 == x1) return y2;
            return (y2 - y1) * (x - x1) / (x2 - x1) + y1;
        }

        private static int FloorToInt(float v) => Mathf.FloorToInt(v);

        // ==================== Lua parser (sub-set) ====================

        /// <summary>
        /// Parse gaibang.lua and return SKILLS dict:
        ///   skillname → magicAttrib → [slotIndex 1..3] → list of LuaPoint
        /// Slot [1] is the primary value (P1 in Param2String), [2] is duration/secondary,
        /// [3] is max/tertiary. Một số magic attribs chỉ có [1] và [3] (vd firedamage_v
        /// dùng [1]=min, [3]=max).
        /// </summary>
        public static Dictionary<string, Dictionary<string, List<List<LuaPoint>>>> ParseGaibangLua(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Debug.LogWarning($"[PcCaiBangLuaLevelService] gaibang.lua not found at {path}; service will return defaults.");
                return new Dictionary<string, Dictionary<string, List<List<LuaPoint>>>>();
            }
            string text;
            try { text = File.ReadAllText(path); }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PcCaiBangLuaLevelService] read fail {path}: {ex.Message}");
                return new Dictionary<string, Dictionary<string, List<List<LuaPoint>>>>();
            }
            return ParseGaibangText(text);
        }

        public static Dictionary<string, Dictionary<string, List<List<LuaPoint>>>> ParseGaibangText(string text)
        {
            var result = new Dictionary<string, Dictionary<string, List<List<LuaPoint>>>>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(text)) return result;

            // Find `SKILLS={ ... }` body. Use bracket matching to handle nested tables.
            int skillsStart = text.IndexOf("SKILLS=", StringComparison.Ordinal);
            if (skillsStart < 0) return result;
            int braceOpen = text.IndexOf('{', skillsStart);
            if (braceOpen < 0) return result;
            int braceEnd = MatchClosingBrace(text, braceOpen);
            if (braceEnd < 0) return result;
            string body = text.Substring(braceOpen + 1, braceEnd - braceOpen - 1);
            ParseSkillsBody(body, result);
            return result;
        }

        private static void ParseSkillsBody(string body, Dictionary<string, Dictionary<string, List<List<LuaPoint>>>> result)
        {
            int pos = 0;
            while (pos < body.Length)
            {
                SkipTrivia(body, ref pos);
                if (pos >= body.Length) break;
                if (body[pos] == '}') break;
                if (body[pos] == ',') { pos++; continue; }
                // Parse `skillname = { ... }`
                var nameTok = ReadIdent(body, ref pos);
                SkipTrivia(body, ref pos);
                if (pos >= body.Length || body[pos] != '=') { SkipToNextEntry(body, ref pos); continue; }
                pos++;
                SkipTrivia(body, ref pos);
                if (pos >= body.Length || body[pos] != '{') { SkipToNextEntry(body, ref pos); continue; }
                int open = pos;
                int close = MatchClosingBrace(body, open);
                if (close < 0) break;
                string inner = body.Substring(open + 1, close - open - 1);
                var attribs = ParseAttribsBody(inner);
                if (attribs.Count > 0)
                    result[nameTok] = attribs;
                pos = close + 1;
            }
        }

        private static Dictionary<string, List<List<LuaPoint>>> ParseAttribsBody(string body)
        {
            var result = new Dictionary<string, List<List<LuaPoint>>>(StringComparer.Ordinal);
            int pos = 0;
            while (pos < body.Length)
            {
                SkipTrivia(body, ref pos);
                if (pos >= body.Length) break;
                if (body[pos] == '}') break;
                if (body[pos] == ',') { pos++; continue; }
                // Parse `attribname = { ... }` or `attribname = function...` or `attribname = "..."` (skill_desc string).
                var nameTok = ReadIdent(body, ref pos);
                SkipTrivia(body, ref pos);
                if (pos >= body.Length || body[pos] != '=') { SkipToNextEntry(body, ref pos); continue; }
                pos++;
                SkipTrivia(body, ref pos);
                if (pos >= body.Length) break;
                // `attribname = function(level) ... end`  — skip (no level table, e.g. skill_desc).
                if (StartsWith(body, pos, "function") || StartsWith(body, pos, "function("))
                {
                    SkipToNextEntry(body, ref pos);
                    continue;
                }
                // String literal (skill_desc could be a string instead of function; but gaibang.lua uses function).
                if (body[pos] == '"' || body[pos] == '\'')
                {
                    SkipStringLiteral(body, ref pos, body[pos]);
                    continue;
                }
                if (body[pos] != '{') { SkipToNextEntry(body, ref pos); continue; }
                int open = pos;
                int close = MatchClosingBrace(body, open);
                if (close < 0) break;
                string inner = body.Substring(open + 1, close - open - 1);
                var slots = ParseTableBody(inner);
                if (slots != null && slots.Count > 0)
                    result[nameTok] = slots;
                pos = close + 1;
            }
            return result;
        }

        /// <summary>
        /// Parse a Lua table that may be:
        ///   { {1,10},{20,50}, ... }                       → slot 1 (no [N] keys)
        ///   { [1]={...}, [2]={...}, [3]={...} }           → slots 1..3 explicit
        ///   { [1]={...}, [3]={...} }                      → slot 1 + 3
        /// Trả về list index 0..3 (slot 1 ở index 0, etc). List có thể chứa null nếu slot
        /// thiếu — caller sẽ fallback về 0.
        /// </summary>
        private static List<List<LuaPoint>> ParseTableBody(string body)
        {
            var slots = new List<List<LuaPoint>> { null, null, null };
            int pos = 0;
            int implicitSlot = 1;
            // [CaiBang-LuaParser 2026-06-19] PC gaibang.lua table body formats:
            //   Multi-slot (3 slots): `{{{1,10},{20,150}},{{1,-1},{2,-1}},{{1,2},{2,2}}}`
            //     → 3 outer `{...}` entries, mỗi entry chứa 1+ (level,value) tuples cho 1 slot.
            //   Single-slot (1 slot with N points): `{{1,0},{11,0},{11,32},{20,32}}`
            //     → 1 outer `{...}` entry chứa N (level,value) tuples cho slot 1.
            // Trước fix: parser treat mỗi `{level,value}` tuple như slot riêng → sai single-slot format.
            //   Detect format bằng cách peek inner của `{...}` — nếu chứa inner `{` thì multi-point slot.
            while (pos < body.Length)
            {
                SkipTrivia(body, ref pos);
                if (pos >= body.Length) break;
                if (body[pos] == '}') break;
                if (body[pos] == ',') { pos++; continue; }
                int slot;
                // Optional [N]= prefix.
                if (body[pos] == '[')
                {
                    pos++;
                    int numStart = pos;
                    while (pos < body.Length && body[pos] != ']') pos++;
                    if (!int.TryParse(body.Substring(numStart, pos - numStart).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out slot))
                    {
                        SkipToNextEntry(body, ref pos);
                        continue;
                    }
                    pos++;
                    SkipTrivia(body, ref pos);
                    if (pos >= body.Length || body[pos] != '=') { SkipToNextEntry(body, ref pos); continue; }
                    pos++;
                    SkipTrivia(body, ref pos);
                }
                else
                {
                    slot = implicitSlot;
                }
                if (pos >= body.Length || body[pos] != '{') { SkipToNextEntry(body, ref pos); continue; }
                int open = pos;
                int close = MatchClosingBrace(body, open);
                if (close < 0) break;
                string inner = body.Substring(open + 1, close - open - 1);
                // Multi-point slot: inner chứa `{` (e.g. `"{1,0},{11,0},{11,32},{20,32}"` hoặc `"{1,10},{20,150}"`)
                //   → parse tất cả tuples trong inner làm points của slot hiện tại.
                // Single-point slot: inner chỉ là `level,value` (e.g. `"1,0"`) → 1 point.
                bool isMultiPoint = ContainsTuple(inner);
                if (isMultiPoint)
                {
                    var points = ParsePointsList(inner);
                    if (points.Count > 0)
                    {
                        while (slots.Count < slot) slots.Add(null);
                        slots[slot - 1] = points;
                    }
                }
                else
                {
                    var tup = ReadTuple(inner);
                    if (tup.HasValue)
                    {
                        var points = new List<LuaPoint> { new LuaPoint(tup.Value.level, tup.Value.value, tup.Value.func) };
                        while (slots.Count < slot) slots.Add(null);
                        slots[slot - 1] = points;
                    }
                }
                pos = close + 1;
                if (slot == implicitSlot) implicitSlot++;
            }
            return slots;
        }

        /// <summary>
        /// Detect nếu inner của `{...}` chứa inner `{...}` tuples (multi-point slot format).
        ///   e.g. `"{1,0},{11,0},{11,32},{20,32}"` → true (multi-point).
        ///   e.g. `"1,0"` → false (single point).
        /// </summary>
        private static bool ContainsTuple(string body)
        {
            int pos = 0;
            while (pos < body.Length)
            {
                SkipTrivia(body, ref pos);
                if (pos >= body.Length) return false;
                if (body[pos] == '}') return false;
                if (body[pos] == ',') { pos++; continue; }
                if (body[pos] == '{') return true;
                pos++;
            }
            return false;
        }

        /// <summary>
        /// Parse list of { level, value[, func] } tuples.
        /// </summary>
        private static List<LuaPoint> ParsePointsList(string body)
        {
            var list = new List<LuaPoint>();
            int pos = 0;
            while (pos < body.Length)
            {
                SkipTrivia(body, ref pos);
                if (pos >= body.Length) break;
                if (body[pos] == '}') break;
                if (body[pos] == ',') { pos++; continue; }
                if (body[pos] != '{') { SkipToNextEntry(body, ref pos); continue; }
                int open = pos;
                int close = MatchClosingBrace(body, open);
                if (close < 0) break;
                string inner = body.Substring(open + 1, close - open - 1);
                var tup = ReadTuple(inner);
                if (tup.HasValue)
                    list.Add(new LuaPoint(tup.Value.level, tup.Value.value, tup.Value.func));
                pos = close + 1;
            }
            return list;
        }

        private static (int level, float value, string func)? ReadTuple(string inner)
        {
            int pos = 0;
            SkipTrivia(inner, ref pos);
            if (pos >= inner.Length) return null;
            int negLevel = 0;
            if (inner[pos] == '-') { negLevel = 1; pos++; }
            int lvStart = pos;
            while (pos < inner.Length && (char.IsDigit(inner[pos]) || inner[pos] == '.')) pos++;
            if (!int.TryParse(inner.Substring(lvStart, pos - lvStart).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var lv))
                return null;
            if (negLevel == 1) lv = -lv;
            SkipTrivia(inner, ref pos);
            if (pos >= inner.Length || inner[pos] != ',') return null;
            pos++;
            SkipTrivia(inner, ref pos);
            // Value: may be a number or a function call SkillExpFunc(...).
            float val;
            if (StartsWith(inner, pos, "SkillExpFunc"))
                val = ParseSkillExpFunc(inner, ref pos);
            else
            {
                int negVal = 0;
                if (pos < inner.Length && inner[pos] == '-') { negVal = 1; pos++; }
                int valStart = pos;
                while (pos < inner.Length && (char.IsDigit(inner[pos]) || inner[pos] == '.')) pos++;
                if (!float.TryParse(inner.Substring(valStart, pos - valStart).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                    return null;
                if (negVal == 1) val = -val;

                // Canonical skill Lua commonly keeps simple arithmetic in level
                // tables (for example missle_lifetime_v={...,{20,18*2}}).
                // Evaluate only literal multiplication/division here; identifiers
                // and general Lua expressions still fail closed.
                while (true)
                {
                    SkipTrivia(inner, ref pos);
                    if (pos >= inner.Length || (inner[pos] != '*' && inner[pos] != '/')) break;
                    char op = inner[pos++];
                    SkipTrivia(inner, ref pos);
                    bool negativeFactor = pos < inner.Length && inner[pos] == '-';
                    if (negativeFactor) pos++;
                    int factorStart = pos;
                    while (pos < inner.Length && (char.IsDigit(inner[pos]) || inner[pos] == '.')) pos++;
                    if (factorStart == pos ||
                        !float.TryParse(inner.Substring(factorStart, pos - factorStart), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out float factor))
                        return null;
                    if (negativeFactor) factor = -factor;
                    if (op == '/' && Mathf.Approximately(factor, 0f)) return null;
                    val = op == '*' ? val * factor : val / factor;
                }
                // [CaiBang-714 2026-07-17] PC gaibang.lua::gaibang120.autoattackskill
                //   slot[1] = {1,720*256+1},{20,720*256+20} ; slot[3] = {1,12*18*256+1},
                //   {20,12*18*256+10} (client_offline + server_offline + slistcache đồng ý).
                //   Lua precedence: + / - bind looser than * / ; left-to-right.
                //   Trước fix: parser chỉ evaluate * và / → đọc 12*18*256 (thiếu +10)
                //   → low byte 0 → proc% 0 thay vì PC 10 tại L20.
                while (true)
                {
                    SkipTrivia(inner, ref pos);
                    if (pos >= inner.Length || (inner[pos] != '+' && inner[pos] != '-')) break;
                    char addOp = inner[pos++];
                    SkipTrivia(inner, ref pos);
                    int addStart = pos;
                    while (pos < inner.Length && (char.IsDigit(inner[pos]) || inner[pos] == '.')) pos++;
                    if (addStart == pos ||
                        !float.TryParse(inner.Substring(addStart, pos - addStart), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out float addTerm))
                        return null;
                    val = addOp == '+' ? val + addTerm : val - addTerm;
                }
            }
            SkipTrivia(inner, ref pos);
            string func = null;
            if (pos < inner.Length && inner[pos] == ',')
            {
                pos++;
                SkipTrivia(inner, ref pos);
                int fnStart = pos;
                while (pos < inner.Length && (char.IsLetter(inner[pos]) || inner[pos] == '_')) pos++;
                if (pos > fnStart)
                    func = inner.Substring(fnStart, pos - fnStart);
            }
            return (lv, val, func);
        }

        // PC gaibang.lua's SkillExpFunc: floor(Exp0 * a^(Level-1) * Time * Range / 2).
        // We pre-evaluate to a single float to plug into our interpolation list.
        private static float ParseSkillExpFunc(string inner, ref int pos)
        {
            // Skip "SkillExpFunc("
            int paren = inner.IndexOf('(', pos);
            if (paren < 0) return 0f;
            pos = paren + 1;
            var args = new List<float>();
            while (pos < inner.Length)
            {
                SkipTrivia(inner, ref pos);
                if (pos < inner.Length && inner[pos] == ')') { pos++; break; }
                int start = pos;
                bool neg = false;
                if (pos < inner.Length && inner[pos] == '-') { neg = true; pos++; }
                while (pos < inner.Length && (char.IsDigit(inner[pos]) || inner[pos] == '.')) pos++;
                if (pos == start) { pos++; continue; }
                if (float.TryParse(inner.Substring(start + (neg ? 1 : 0), pos - start - (neg ? 1 : 0)).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    args.Add(neg ? -v : v);
                SkipTrivia(inner, ref pos);
                if (pos < inner.Length && inner[pos] == ',') pos++;
            }
            if (args.Count < 5) return 0f;
            float exp0 = args[0], a = args[1], level = args[2], time = args[3], range = args[4];
            return Mathf.Floor(exp0 * Mathf.Pow(a, level - 1) * time * range / 2f);
        }

        // ==================== Low-level tokenizer helpers ====================

        private static bool StartsWith(string s, int pos, string token)
        {
            if (pos + token.Length > s.Length) return false;
            for (int i = 0; i < token.Length; i++)
                if (s[pos + i] != token[i]) return false;
            return true;
        }

        private static void SkipTrivia(string s, ref int pos)
        {
            while (pos < s.Length)
            {
                char c = s[pos];
                if (char.IsWhiteSpace(c)) { pos++; continue; }
                if (c == '-' && pos + 1 < s.Length && s[pos + 1] == '-')
                {
                    // Line comment to end of line.
                    while (pos < s.Length && s[pos] != '\n') pos++;
                    continue;
                }
                break;
            }
        }

        private static string ReadIdent(string s, ref int pos)
        {
            int start = pos;
            while (pos < s.Length && (char.IsLetterOrDigit(s[pos]) || s[pos] == '_')) pos++;
            return s.Substring(start, pos - start);
        }

        private static void SkipStringLiteral(string s, ref int pos, char quote)
        {
            pos++;
            while (pos < s.Length && s[pos] != quote)
            {
                if (s[pos] == '\\' && pos + 1 < s.Length) pos += 2;
                else pos++;
            }
            if (pos < s.Length) pos++;
        }

        private static void SkipToNextEntry(string s, ref int pos)
        {
            int depth = 0;
            while (pos < s.Length)
            {
                char c = s[pos];
                if (c == '{') { depth++; pos++; continue; }
                if (c == '}') { if (depth == 0) return; depth--; pos++; if (depth == 0) return; continue; }
                if (c == ',' && depth == 0) { pos++; return; }
                pos++;
            }
        }

        private static int MatchClosingBrace(string s, int openPos)
        {
            int depth = 0;
            int pos = openPos;
            while (pos < s.Length)
            {
                char c = s[pos];
                if (c == '"' || c == '\'') { SkipStringLiteral(s, ref pos, c); continue; }
                if (c == '-' && pos + 1 < s.Length && s[pos + 1] == '-')
                {
                    while (pos < s.Length && s[pos] != '\n') pos++;
                    continue;
                }
                if (c == '{') { depth++; pos++; continue; }
                if (c == '}') { depth--; pos++; if (depth == 0) return pos - 1; continue; }
                pos++;
            }
            return -1;
        }
    }
}
