// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved. Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Sandbox
{
    public enum PlayerVisualAction
    {
        Idle,       // ST01 (fight-stand / 空手站立1)
        Move,       // RN01/RN03 (run)
        Magic,      // MG01/MG04 (magic cast)
        Attack,     // AT04/AT05 (melee attack)
        Ride,       // RD01 (mounted IDLE: RideStand — full 8-dir layered horse+rider)
        RideMove,   // HR01 (mounted MOVE: RideRun gallop — full 8-dir layered horse+rider)
    }

    /// <summary>
    /// PC weapon equip categories from 男主角未骑马关联表.txt.
    /// Each category maps to different SPR action suffixes for the same cdo_* action.
    /// </summary>
    public enum PcWeaponType
    {
        EmptyHand = 0,     // 空手 → ST01, RN01, MG01
        ShortWeapon = 1,   // 短武器 → ST04, RN02, MG02
        LongWeapon = 2,    // 长武器(棍/枪) → ST05, RN03, MG04
        DualWeapon = 3,    // 双武器 → ST06, RN04, MG05
    }

    public enum PlayerSpritePartKind
    {
        Shadow = -1,
        Head = 0,
        Hair = 1,
        Shoulder = 4,
        Body = 5,
        LeftHand = 6,
        RightHand = 7,
        LeftWeapon = 8,
        RightWeapon = 9,
        Saddle = 11,    // Mounted-only: MA_HB_*_HM01 (horse body / saddle region).
        HorseFront = 12,
        HorseMiddle = 13,
        HorseRear = 14,
    }

    [Serializable]
    public struct PlayerSpritePartSpec
    {
        public PlayerSpritePartKind kind;
        public string name;
        public string sourcePath;
        public bool required;
        // Override cho SPR header directions sai. 0 = dùng header. Horse HH/HT báo
        // dirs=1 dù thực có 8 hướng × 14 frame → ép 8 để không bị "tự xoay".
        public int expectedDirections;

        public PlayerSpritePartSpec(PlayerSpritePartKind kind, string name, string sourcePath, bool required = true, int expectedDirections = 0)
        {
            this.kind = kind;
            this.name = name;
            this.sourcePath = sourcePath;
            this.required = required;
            this.expectedDirections = expectedDirections;
        }
    }

    /// <summary>
    /// Male player sprite catalog ported from PC Settings/NpcRes tables.
    /// Supports weapon-type-aware action selection matching PC KNpcRes::SetAction logic:
    /// 男主角未骑马关联表.txt maps (weapon category × CLIENTACTION) → action name,
    /// and each action has its own SPR file set per body part.
    ///
    /// Current defaults: armor variant 19, shadow variant 999.
    /// Empty hand: right weapon variant 000, Long staff (长棍类1): variant 010.
    /// </summary>
    public static class MalePlayerSpriteCatalog
    {
        public const string SourceRoot = @"spr\npcres\man";
        public const int DirectionCount = 8;
        public const int ArmorVariant = 19;
        public const int MountArmorVariant = 050;  // MA_(BD/HD/HR/LH/RH)_050 — mounted rider outfit.
        public const int MountAltArmorVariant = 072; // alt mounted rider outfit.
        public const int MountHorseVariant = 016;   // MA_(HH/HB/HT)_016 — horse body (front/mid/back).
        public const int MountAltHorseVariant = 018; // alt horse body.
        public const int ShadowVariant = 999;
        // PC 男主角马* tables: RideStand=RD01 (mounted idle), RideRun=HR01 (mounted gallop).
        public const string MountIdleSuffix = "RD01";
        public const string MountMoveSuffix = "HR01";
        public const int EmptyWeaponVariant = 0;
        public const int ShortWeaponVariant = 001; // 单手剑1 from 男主角右手武器.txt
        public const int StaffWeaponVariant = 010; // 长棍类1 from 男主角右手武器.txt
        public const int DualWeaponVariant = 013; // 双武器/双剑类1 from 男主角左右手武器.txt

        // PC action suffixes per weapon type. From 男主角未骑马关联表.txt columns:
        // cdo_fightstand maps to: 空手站立1=ST01, 短武器站立=ST04, 长武器站立=ST05, 双武器站立=ST06
        // cdo_run maps to:        空手跑步=RN01, 短武器跑步=RN02, 长武器跑步=RN03, 双武器跑步=RN04
        // cdo_magic maps to:      空手魔法=MG01, 短武器魔法=MG02, 长武器魔法=MG04, 双武器魔法=MG05
        // cdo_attack (long staff) maps to: 长武器劈=AT05 (primary)
        private static readonly string[,] ActionSuffix = new string[4, 4]
        {
            // Idle,            Move,            Magic,            Attack
            { "ST01",          "RN01",          "MG01",           "AT01" }, // EmptyHand
            { "ST04",          "RN02",          "MG02",           "AT03" }, // ShortWeapon
            { "ST05",          "RN03",          "MG04",           "AT05" }, // LongWeapon
            { "ST06",          "RN04",          "MG05",           "AT07" }, // DualWeapon
        };

        // Weapon right-hand SPR variant per weapon type (from 男主角右手武器.txt)
        private static readonly int[] WeaponSprVariant = new int[4]
        {
            EmptyWeaponVariant,  // EmptyHand
            ShortWeaponVariant,  // ShortWeapon (单手剑1 = RW_001)
            StaffWeaponVariant,  // LongWeapon (长棍类1 = RW_010)
            DualWeaponVariant,   // DualWeapon (双剑类1 = LW/RW_013)
        };

        // PC draw-order table: Settings/NpcRes/男主角贴图顺序表.txt, Dir1..Dir8.
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
        /// Build the SPR part spec list for a given action + weapon type, matching PC KNpcRes::SetAction.
        /// When action is <see cref="PlayerVisualAction.Ride"/>, returns the mounted rider
        /// (BD/HD/HB) layered set with HM01 suffix; weapons and shadow are skipped because
        /// PC npcres/man has no mounted Shadow/LW/RW/Hair/LHand/RHand SPRs.
        /// </summary>
        public static PlayerSpritePartSpec[] BuildParts(PlayerVisualAction action, PcWeaponType weapon)
        {
            if (action == PlayerVisualAction.Ride)
                return BuildMountedParts(MountArmorVariant, MountHorseVariant, MountIdleSuffix);
            if (action == PlayerVisualAction.RideMove)
                return BuildMountedParts(MountArmorVariant, MountHorseVariant, MountMoveSuffix);

            int wIdx = (int)weapon;
            string suffix = ActionSuffix[wIdx, (int)action];
            string rightWeaponSuffix = (weapon == PcWeaponType.ShortWeapon && action == PlayerVisualAction.Magic)
                ? "MG03" // PC 男主角右手武器.txt: MeleeWMagic uses MA_RW_001_MG03.spr.
                : suffix;
            int rwVariant = WeaponSprVariant[wIdx];
            // Long staff has no left weapon SPR — use empty hand for left
            int lwVariant = (weapon == PcWeaponType.DualWeapon) ? WeaponSprVariant[(int)PcWeaponType.DualWeapon] : EmptyWeaponVariant;

            // Long staff has no left weapon SPR — only right hand holds the staff.
            // Dual weapons would have both. For other types, left weapon is empty (still has SPR).
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
                new(PlayerSpritePartKind.RightWeapon,  "RightWeapon",  BuildPath("RW", rwVariant, rightWeaponSuffix)),
            };
        }

        /// <summary>
        /// Build the full mounted layered set: horse body (HH/HB/HT = parts 12/13/14)
        /// + rider (BD/HD/HR/LH/RH). Suffix is RD01 (idle/RideStand) or HR01 (move/RideRun),
        /// both 8-direction. PC tables 男主角马前/马中/马后 map HH/HB/HT to the horse body;
        /// 男主角 BD/HD/HR/LH/RH ride columns supply the rider. No Shadow/Weapon when mounted.
        /// </summary>
        public static PlayerSpritePartSpec[] BuildMountedParts(int riderVariant, int horseVariant, string suffix)
        {
            return new PlayerSpritePartSpec[]
            {
                // Horse body — drawn behind/around rider per draw-order (ids 12/13/14).
                // HH/HT SPR header báo dirs=1 sai → ép expectedDirections=8 (khớp HB) để
                // animation khóa theo hướng rider, không "tự xoay" qua mọi hướng.
                new(PlayerSpritePartKind.HorseFront,  "HorseFront",  BuildPath("HH", horseVariant, suffix), true, 8),
                new(PlayerSpritePartKind.HorseMiddle, "HorseMiddle", BuildPath("HB", horseVariant, suffix), true, 8),
                new(PlayerSpritePartKind.HorseRear,   "HorseRear",   BuildPath("HT", horseVariant, suffix), true, 8),
                // Rider.
                new(PlayerSpritePartKind.Body,        "MountBody",   BuildPath("BD", riderVariant, suffix)),
                new(PlayerSpritePartKind.Head,        "MountHead",   BuildPath("HD", riderVariant, suffix)),
                new(PlayerSpritePartKind.Hair,        "MountHair",   BuildPath("HR", riderVariant, suffix)),
                new(PlayerSpritePartKind.LeftHand,    "MountLeftHand",  BuildPath("LH", riderVariant, suffix)),
                new(PlayerSpritePartKind.RightHand,   "MountRightHand", BuildPath("RH", riderVariant, suffix)),
            };
        }

        public static string BuildPath(string part, int variant, string action)
        {
            return SourceRoot + @"\MA_" + part + "_" + variant.ToString("D3") + "_" + action + ".spr";
        }

        public static string BuildSourcePath(string fileName)
        {
            return SourceRoot + @"\" + fileName;
        }

        /// <summary>
        /// Map PC Skills.txt CharAnimId + current weapon type to a visual action.
        /// PC KNpc.cpp đổi CharAnimId thành CLIENTACTION rồi KNpcRes::SetAction chọn suffix theo vũ khí.
        /// </summary>
        public static PlayerVisualAction? ResolveAction(int charAnimId, PcWeaponType weapon)
        {
            return charAnimId switch
            {
                7 or 8 => PlayerVisualAction.Attack,
                9 or 10 or 11 => PlayerVisualAction.Magic,
                14 => null,   // Passive/aura — không chạy animation nhân vật
                _ => null,
            };
        }

        public static int DirectionFromMove(Vector2 move)
        {
            if (move.sqrMagnitude <= 0.0001f)
                return -1;

            float angle = Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            // JX direction order: 0=S, 1=SW, 2=W, 3=NW, 4=N, 5=NE, 6=E, 7=SE.
            if (angle >= 337.5f || angle < 22.5f) return 6;
            if (angle < 67.5f) return 5;
            if (angle < 112.5f) return 4;
            if (angle < 157.5f) return 3;
            if (angle < 202.5f) return 2;
            if (angle < 247.5f) return 1;
            if (angle < 292.5f) return 0;
            return 7;
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
