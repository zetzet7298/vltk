// -----------------------------------------------------------------------------
// VLTK Mobile — ST-02.1 Female Player Sprite Catalog
// Mirror of MalePlayerSpriteCatalog for WO_* SPR parts.
// Source: PC Settings/NpcRes/woman, 女主角贴图顺序表.txt
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Female player sprite catalog ported from PC Settings/NpcRes tables.
    /// Uses WO_* prefix for body parts (WO_BD, WO_HD, WO_HR, WO_LH, WO_RH, WO_LW, WO_RW).
    /// Same weapon-type-aware action selection as male, same 8-direction system.
    /// Source: spr\npcres\woman
    /// </summary>
    public static class FemalePlayerSpriteCatalog
    {
        public const string SourceRoot = @"spr\npcres\woman";
        public const int DirectionCount = 8;
        public const int ArmorVariant = 19;
        public const int ShadowVariant = 999;
        public const int EmptyWeaponVariant = 0;
        public const int StaffWeaponVariant = 010;

        // PC action suffixes per weapon type. Same logic as male.
        private static readonly string[,] ActionSuffix = new string[4, 4]
        {
            // Idle,            Move,            Magic,            Attack
            { "ST01",          "RN01",          "MG01",           "AT01" }, // EmptyHand
            { "ST04",          "RN02",          "MG02",           "AT03" }, // ShortWeapon
            { "ST05",          "RN03",          "MG04",           "AT05" }, // LongWeapon
            { "ST06",          "RN04",          "MG05",           "AT07" }, // DualWeapon
        };

        private static readonly int[] WeaponSprVariant = new int[4]
        {
            EmptyWeaponVariant,  // EmptyHand
            001,                 // ShortWeapon
            StaffWeaponVariant,  // LongWeapon
            002,                 // DualWeapon
        };

        // PC draw-order table for female: 女主角贴图顺序表.txt
        // Same structure as male but female-specific order.
        private static readonly int[][] DrawOrderByDirection =
        {
            new[] { -1, 14, 13, 1, 4, 9, 7, 5, 6, 12, 8, 0 },
            new[] { -1, 14, 13, 9, 7, 4, 1, 5, 12, 6, 8, 0 },
            new[] { -1, 9, 7, 12, 13, 14, 5, 4, 1, 0, 6, 8 },
            new[] { -1, 9, 7, 12, 13, 5, 14, 4, 1, 0, 8, 6 },
            new[] { -1, 12, 13, 8, 6, 5, 14, 4, 1, 0, 7, 9 },
            new[] { -1, 8, 6, 12, 13, 5, 14, 4, 1, 0, 9, 7 },
            new[] { -1, 8, 6, 12, 13, 14, 5, 4, 1, 0, 9, 7 },
            new[] { -1, 14, 13, 4, 1, 8, 6, 5, 12, 0, 9, 7 },
        };

        /// <summary>
        /// Build the SPR part spec list cho female player.
        /// Same structure as MalePlayerSpriteCatalog.BuildParts nhưng dùng WO_ prefix.
        /// </summary>
        public static PlayerSpritePartSpec[] BuildParts(PlayerVisualAction action, PcWeaponType weapon)
        {
            int wIdx = (int)weapon;
            string suffix = ActionSuffix[wIdx, (int)action];
            int rwVariant = WeaponSprVariant[wIdx];
            int lwVariant = (weapon == PcWeaponType.DualWeapon) ? WeaponSprVariant[(int)PcWeaponType.DualWeapon] : EmptyWeaponVariant;
            bool leftWeaponRequired = weapon != PcWeaponType.LongWeapon;

            return new PlayerSpritePartSpec[]
            {
                new(PlayerSpritePartKind.Shadow,      "Shadow",       BuildPath("YY", ShadowVariant, suffix)),
                new(PlayerSpritePartKind.Body,         "Body",         BuildPath("BD", ArmorVariant, suffix)),
                new(PlayerSpritePartKind.Head,         "Head",         BuildPath("HD", ArmorVariant, suffix)),
                new(PlayerSpritePartKind.Hair,         "Hair",         BuildPath("HR", ArmorVariant, suffix)),
                new(PlayerSpritePartKind.LeftHand,     "LeftHand",     BuildPath("LH", ArmorVariant, suffix)),
                new(PlayerSpritePartKind.RightHand,    "RightHand",    BuildPath("RH", ArmorVariant, suffix)),
                new(PlayerSpritePartKind.LeftWeapon,   "LeftWeapon",   BuildPath("LW", lwVariant, suffix), leftWeaponRequired),
                new(PlayerSpritePartKind.RightWeapon,  "RightWeapon",  BuildPath("RW", rwVariant, suffix)),
            };
        }

        public static string BuildPath(string part, int variant, string action)
        {
            return SourceRoot + @"\WO_" + part + "_" + variant.ToString("D3") + "_" + action + ".spr";
        }

        public static int DirectionFromMove(Vector2 move)
        {
            return MalePlayerSpriteCatalog.DirectionFromMove(move);
        }

        public static int SortingOffset(PlayerSpritePartKind kind, int direction)
        {
            int dir = Mathf.Clamp(direction, 0, DirectionCount - 1);
            var order = DrawOrderByDirection[dir];
            int part = (int)kind;
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == part)
                    return i * 2;
            }
            return 100 + part;
        }
    }
}
