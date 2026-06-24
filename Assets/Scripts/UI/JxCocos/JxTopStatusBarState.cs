// -----------------------------------------------------------------------------
// VLTK Mobile — JX Top Status Bar state (port from jx-cocos KuiTopControlVN.cpp)
//
// Nguồn truth: /home/zet/Projects/jx-cocos/client/Classes/vn/gameui/KuiTopControlVN.cpp
//
// KuiTopControlVN::upRoleInfo(float nMinVer, float nMaxVer, int nKind, char* str)
// là API cập nhật duy nhất cho thanh trạng thái trên (HP/MP/Stamina/EXP/Level/
// Rank/Name). Logic fill: scaleX = clamp01(cur/max). EXP label dạng "%NN".
// Level 0 -> RANK_WORLD_ZERO ("Bàn tay vàng" / ký tự 0 cấp). Name -> translate(str).
//
// Lớp này là PURE STATE (không dependency UI), EditMode-testable. Renderer
// (JxTopStatusBarAdapter) tách riêng.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace VLTK.UI.JxCocos
{
    /// <summary>
    /// Pure state mirror of jx-cocos KuiTopControlVN. Encodes the upRoleInfo()
    /// update contract and the fill-fraction / label-text derivations 1:1 from
    /// the C++ source, so the rendering adapter only reads exported values.
    /// </summary>
    public sealed class JxTopStatusBarState
    {
        /// <summary>nKind values matching KuiTopControlVN::upRoleInfo switch.</summary>
        public static class Kind
        {
            public const int Hp = 0;
            public const int Mana = 1;
            public const int Stamina = 2;
            public const int Exp = 3;
            public const int Level = 4;
            public const int Rank = 5;
            public const int Name = 6;
            // kind 7 (PaiLabel) is commented-out stub in source — no-op.
        }

        /// <summary>Level 0 render target, matching RANK_WORLD_ZERO in source.</summary>
        public const string LevelZeroText = "0";

        // Current/max per resource.
        public float HpCurrent { get; private set; }
        public float HpMax { get; private set; }
        public float ManaCurrent { get; private set; }
        public float ManaMax { get; private set; }
        public float StaminaCurrent { get; private set; }
        public float StaminaMax { get; private set; }
        public float ExpCurrent { get; private set; }
        public float ExpMax { get; private set; }

        public int Level { get; private set; }
        public int Rank { get; private set; }
        public string PlayerName { get; private set; } = string.Empty;
        public bool IsFemale { get; private set; }

        /// <summary>
        /// Port of KuiTopControlVN::upRoleInfo(). kind selects which field to
        /// update; str is the name text (kind == Name). Mirrors the switch()
        /// semantics exactly, including clamp01 on every fill bar.
        /// </summary>
        public void UpRoleInfo(float minVer, float maxVer, int kind, string str = null)
        {
            switch (kind)
            {
                case Kind.Hp:
                    HpCurrent = minVer; HpMax = maxVer;
                    break;
                case Kind.Mana:
                    ManaCurrent = minVer; ManaMax = maxVer;
                    break;
                case Kind.Stamina:
                    StaminaCurrent = minVer; StaminaMax = maxVer;
                    break;
                case Kind.Exp:
                    ExpCurrent = minVer; ExpMax = maxVer;
                    break;
                case Kind.Level:
                    Level = (int)minVer;
                    break;
                case Kind.Rank:
                    // Source only sets rank label when nMinVer != 0; keep 0 default.
                    Rank = (int)minVer;
                    break;
                case Kind.Name:
                    PlayerName = str ?? string.Empty;
                    break;
                default:
                    break;
            }
        }

        /// <summary>Set avatar gender (drives AvatarNam vs AvatarNu sprite).</summary>
        public void SetGender(bool isFemale) => IsFemale = isFemale;

        // ---- Derived fill fractions (scaleX in source, clamp01) ----

        /// <summary>HP fill = clamp01(cur/max). Source: nTempScalX=nMinVer/nMaxVer.</summary>
        public float HpFraction => ClampFraction(HpCurrent, HpMax);

        public float ManaFraction => ClampFraction(ManaCurrent, ManaMax);

        public float StaminaFraction => ClampFraction(StaminaCurrent, StaminaMax);

        public float ExpFraction => ClampFraction(ExpCurrent, ExpMax);

        /// <summary>
        /// Clamp a cur/max ratio to [0,1], matching the source's
        /// "if &gt;1 → 1; if &lt;0 → 0" guards on every bar (HP/MP/Stamina/EXP).
        /// Guarded against max &lt;= 0 (returns 0, never div-by-zero / full-bar lie).
        /// </summary>
        public static float ClampFraction(float current, float max)
        {
            if (max <= 0f) return 0f;
            float x = current / max;
            if (x > 1f) return 1f;
            if (x < 0f) return 0f;
            return x;
        }

        // ---- Derived label texts ----

        /// <summary>HP/MP/Stamina label: "cur/max" (source t_sprintf "%d/%d").</summary>
        public string HpText => $"{(int)HpCurrent}/{(int)HpMax}";
        public string ManaText => $"{(int)ManaCurrent}/{(int)ManaMax}";
        public string StaminaText => $"{(int)StaminaCurrent}/{(int)StaminaMax}";

        /// <summary>EXP label: "%NN" (source t_sprintf "%%%0.0f", 100*scale).</summary>
        public string ExpText => $"{Mathf.RoundToInt(ExpFraction * 100f)}%";

        /// <summary>
        /// Level label: the level number, or LevelZeroText when level &lt;= 0
        /// (source: nMinVer &gt; 0 ? number : RANK_WORLD_ZERO).
        /// </summary>
        public string LevelText => Level > 0 ? Level.ToString() : LevelZeroText;

        /// <summary>Rank label: the rank number. Source only renders when nonzero.</summary>
        public string RankText => Rank.ToString();

        /// <summary>Player name (already translated upstream; passed via str).</summary>
        public string NameText => PlayerName;
    }
}
