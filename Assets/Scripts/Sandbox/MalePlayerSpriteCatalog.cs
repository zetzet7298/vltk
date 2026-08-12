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
        Magic,      // MG02..MG05 (magic cast)
        Attack,     // physical attack 1
        Attack1,    // physical attack 2
        Ride,       // RD01 (mounted IDLE: RideStand — full 8-dir layered horse+rider)
        RideWalk,   // HW01 (mounted WALK: RideWalk — full 8-dir layered horse+rider)
        RideMove,   // HR01 (mounted RUN: RideRun gallop — full 8-dir layered horse+rider)
        Walk,       // WK01/WK04 (PC 走路 — walk mode, slower than run; 男主角躯体.txt)
        Sit,        // ZZ01 (PC 打坐 — meditate / cross-legged sit; one suffix for all weapons)
        Jump,       // JP01 (PC 跳跃 — Khinh Công leap; one suffix for all weapons)
        RideAttack, // HA01 (mounted slash)
        RideAttack1,// HA02 (mounted thrust)
        RideMagic,  // HM01 (mounted magic)
    }

    /// <summary>
    /// PC weapon equip categories from 男主角未骑马关联表.txt.
    /// Each category maps to different SPR action suffixes for the same cdo_* action.
    /// </summary>
    public enum PcWeaponType
    {
        EmptyHand = 0,     // 空手 → ST01, RN01, MG02
        ShortWeapon = 1,   // 短武器 → ST04, RN02, MG03
        LongWeapon = 2,    // 长武器(棍/枪) → ST05, RN03, MG04
        DualWeapon = 3,    // 双武器 → ST06, RN04, MG05
        HiddenWeapon = 4,  // 暗器 → ST01, RN01, MG02; physical attacks use MG01
    }

    public enum PcWeaponMotionProfile
    {
        PrimaryPhysicalOrder = 0,
        AlternatePhysicalOrder = 1,
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
    /// Current package.ini defaults: rider variant 019, horse variant 001, shadow 999.
    /// Empty hand: right weapon variant 000, Long staff (长棍类1): variant 010.
    /// </summary>
    public static class MalePlayerSpriteCatalog
    {
        public const string SourceRoot = @"spr\npcres\man";
        public const int DirectionCount = 8;
        // package.ini winners: MA body/head/hands use 019; MA horse uses 001.
        public const int ArmorVariant = 019;
        public const int MountArmorVariant = 019;
        public const int MountAltArmorVariant = 072; // alternate package winner.
        public const int MountHorseVariant = 001;
        public const int MountAltHorseVariant = 018; // alt horse body.
        public const int ShadowVariant = 999;
        // PC mount tables (pak_unpacked/_slistcache/unknown/d14d05cc.dat):
        // RideStand=RD01, RideWalk=HW01, RideRun=HR01.
        public const string MountIdleSuffix = "RD01";
        public const string MountWalkSuffix = "HW01";
        public const string MountMoveSuffix = "HR01";
        public const int EmptyWeaponVariant = 0;
        public const int ShortWeaponVariant = 001; // 单手剑1 from 男主角右手武器.txt
        public const int StaffWeaponVariant = 010; // 长棍类1 from 男主角右手武器.txt
        public const int DualWeaponVariant = 013; // 双武器/双剑类1 from 男主角左右手武器.txt

        // PC action suffixes per weapon type. From 男主角未骑马关联表.txt columns:
        // cdo_fightstand maps to: 空手站立1=ST01, 短武器站立=ST04, 长武器站立=ST05, 双武器站立=ST06
        // cdo_run maps to:        空手跑步=RN01, 短武器跑步=RN02, 长武器跑步=RN03, 双武器跑步=RN04
        // cdo_magic maps to:      空手魔法=MG02, 短武器魔法=MG03, 长武器魔法=MG04, 双武器魔法=MG05, 暗器=MG01.
        // 暗器: cdo_attack/cdo_attack1=MG01; cdo_magic=MG02.
        private static readonly string[,] ActionSuffix = new string[5, 6]
        {
            // Idle,    Move,    Magic,   Attack, Attack1, unused
            { "ST01", "RN01", "MG02", "AT01", "AT01", "" }, // EmptyHand
            { "ST04", "RN02", "MG03", "AT02", "AT03", "" }, // ShortWeapon
            { "ST05", "RN03", "MG04", "AT04", "AT05", "" }, // LongWeapon
            { "ST06", "RN04", "MG05", "AT06", "AT07", "" }, // DualWeapon
            { "ST01", "RN01", "MG02", "MG01", "MG01", "" }, // HiddenWeapon
        };

        // Weapon right-hand SPR variant per weapon type (from 男主角右手武器.txt)
        private static readonly int[] WeaponSprVariant = new int[5]
        {
            EmptyWeaponVariant,  // EmptyHand
            ShortWeaponVariant,  // ShortWeapon (单手剑1 = RW_001)
            StaffWeaponVariant,  // LongWeapon (长棍类1 = RW_010)
            DualWeaponVariant,   // DualWeapon (双剑类1 = LW/RW_013)
            EmptyWeaponVariant,  // HiddenWeapon uses empty weapon layers.
        };

        // PC 男主角躯体.txt walk column (走路): WK01=FreeWalk/NormalWalk, WK02=MeleeW, WK03=RangeW, WK04=DoubleW.
        private static readonly string[] WalkSuffix = new string[5]
        {
            "WK01", // EmptyHand
            "WK02", // ShortWeapon
            "WK03", // LongWeapon
            "WK04", // DualWeapon
            "WK01", // HiddenWeapon
        };
        // PC 打坐 (SitDown) and 跳跃 (JumpFly) columns: ONE shared suffix for every weapon type.
        public const string SitSuffix = "ZZ01";
        public const string JumpSuffix = "JP01";

        /// <summary>
        /// Resolve the PC SPR variant index for a given weapon equip category.
        /// Used by MalePlayerVisual.SetWeapon to keep weaponVariant in sync.
        /// </summary>
        public static int GetWeaponSprVariant(PcWeaponType weapon) => WeaponSprVariant[(int)weapon];

        /// <summary>
        /// PC relation-table rows swap cdo_attack/cdo_attack1 for knife, staff and
        /// dual-hammer variants while keeping the same broad equip family.
        /// </summary>
        public static PcWeaponMotionProfile ResolveMotionProfile(PcWeaponType weapon, int weaponVariant)
        {
            return weapon switch
            {
                PcWeaponType.ShortWeapon when weaponVariant is >= 4 and <= 6 or >= 20 and <= 22
                    => PcWeaponMotionProfile.AlternatePhysicalOrder,
                PcWeaponType.LongWeapon when weaponVariant is >= 10 and <= 12 or >= 25 and <= 26
                    => PcWeaponMotionProfile.AlternatePhysicalOrder,
                PcWeaponType.DualWeapon when weaponVariant is >= 16 and <= 18 or >= 29 and <= 30
                    => PcWeaponMotionProfile.AlternatePhysicalOrder,
                _ => PcWeaponMotionProfile.PrimaryPhysicalOrder,
            };
        }

        public static string ResolveFootActionSuffix(PlayerVisualAction action, PcWeaponType weapon,
            int weaponVariant = AutoWeaponVariant)
        {
            int effectiveWeaponVariant = weaponVariant == AutoWeaponVariant
                ? WeaponSprVariant[(int)weapon]
                : weaponVariant;
            PlayerVisualAction resolvedAction = action;
            if (ResolveMotionProfile(weapon, effectiveWeaponVariant) == PcWeaponMotionProfile.AlternatePhysicalOrder)
            {
                if (action == PlayerVisualAction.Attack)
                    resolvedAction = PlayerVisualAction.Attack1;
                else if (action == PlayerVisualAction.Attack1)
                    resolvedAction = PlayerVisualAction.Attack;
            }
            return ActionSuffix[(int)weapon, (int)resolvedAction];
        }

        // Sentinel value: when weaponVariant equals this, BuildParts auto-resolves
        // from the weapon type via WeaponSprVariant.
        private const int AutoWeaponVariant = int.MinValue;

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
        /// </summary>
        public static PlayerSpritePartSpec[] BuildParts(PlayerVisualAction action, PcWeaponType weapon, int bodyVariant = ArmorVariant, int headVariant = ArmorVariant, int weaponVariant = AutoWeaponVariant, int hairVariant = ArmorVariant, int horseVariant = MountHorseVariant)
        {
            // Mounted defaults stay on canonical 019 via default args; explicit equipped variants pass through.
            int mountBody = bodyVariant;
            int mountHead = headVariant;
            int mountHair = hairVariant;

            // Auto-resolve weapon variant from weapon type when caller passes the sentinel default.
            int effectiveWeaponVariant = (weaponVariant == AutoWeaponVariant)
                ? WeaponSprVariant[(int)weapon]
                : weaponVariant;

            if (action == PlayerVisualAction.Ride || action == PlayerVisualAction.RideWalk || action == PlayerVisualAction.RideMove ||
                action == PlayerVisualAction.RideAttack || action == PlayerVisualAction.RideAttack1 || action == PlayerVisualAction.RideMagic)
                return BuildMountedParts(mountBody, mountHead, mountHair, horseVariant,
                    PlayerMountService.GetMountedActionSuffix(action, weapon), weapon, effectiveWeaponVariant);

            int wIdx = (int)weapon;
            // Walk/Sit/Jump resolve to dedicated PC suffixes (男主角躯体.txt):
            //   Walk (走路) -> WK01..WK04 per weapon; Sit (打坐) -> ZZ01; Jump (跳跃) -> JP01.
            // Sit/Jump share ONE suffix across all weapon categories per PC source.
            string suffix = action switch
            {
                PlayerVisualAction.Walk => WalkSuffix[wIdx],
                PlayerVisualAction.Sit  => SitSuffix,
                PlayerVisualAction.Jump => JumpSuffix,
                _ => ResolveFootActionSuffix(action, weapon, effectiveWeaponVariant),
            };
            int lwVariant = (weapon == PcWeaponType.DualWeapon) ? effectiveWeaponVariant : EmptyWeaponVariant;

            // Long staff has no left weapon SPR — only right hand holds the staff.
            // Dual weapons would have both. For other types, left weapon is empty (still has SPR).
            bool leftWeaponRequired = weapon != PcWeaponType.LongWeapon;

            string shoulderPath = BuildShoulderPath(bodyVariant, suffix);
            return new PlayerSpritePartSpec[]
            {
                new(PlayerSpritePartKind.Shadow,      "Shadow",       BuildPath("YY", ShadowVariant, suffix)),
                new(PlayerSpritePartKind.Body,         "Body",         BuildPath("BD", bodyVariant, suffix)),
                new(PlayerSpritePartKind.Head,         "Head",         BuildPath("HD", headVariant, suffix)),
                new(PlayerSpritePartKind.Hair,         "Hair",         BuildPath("HR", hairVariant, suffix)),
                new(PlayerSpritePartKind.Shoulder,     "Shoulder",     shoulderPath, IsShoulderRequired(bodyVariant)),
                new(PlayerSpritePartKind.LeftHand,     "LeftHand",     BuildPath("LH", bodyVariant, suffix)),
                new(PlayerSpritePartKind.RightHand,    "RightHand",    BuildPath("RH", bodyVariant, suffix)),
                new(PlayerSpritePartKind.LeftWeapon,   "LeftWeapon",   BuildPath("LW", lwVariant, suffix), leftWeaponRequired),
                new(PlayerSpritePartKind.RightWeapon,  "RightWeapon",  BuildPath("RW", effectiveWeaponVariant, suffix)),
            };
        }

        /// <summary>
        /// Build the full mounted layered set with dynamic rider and horse parts.
        /// </summary>
        public static PlayerSpritePartSpec[] BuildMountedParts(int bodyVariant, int headVariant, int hairVariant, int horseVariant, string suffix)
            => BuildMountedParts(bodyVariant, headVariant, hairVariant, horseVariant, suffix, PcWeaponType.EmptyHand, EmptyWeaponVariant);

        public static PlayerSpritePartSpec[] BuildMountedParts(int bodyVariant, int headVariant, int hairVariant, int horseVariant, string suffix, PcWeaponType weapon, int weaponVariant)
        {
            int leftWeaponVariant = weapon == PcWeaponType.DualWeapon ? weaponVariant : EmptyWeaponVariant;
            string shoulderPath = BuildShoulderPath(bodyVariant, suffix);
            return new PlayerSpritePartSpec[]
            {
                new(PlayerSpritePartKind.Shadow,      "Shadow",       BuildPath("YY", ShadowVariant, suffix)),
                // Horse body — drawn behind/around rider per draw-order (ids 12/13/14).
                new(PlayerSpritePartKind.HorseFront,  "HorseFront",  BuildPath("HH", horseVariant, suffix), true, 8),
                new(PlayerSpritePartKind.HorseMiddle, "HorseMiddle", BuildPath("HB", horseVariant, suffix), true, 8),
                new(PlayerSpritePartKind.HorseRear,   "HorseRear",   BuildPath("HT", horseVariant, suffix), true, 8),
                // Rider.
                new(PlayerSpritePartKind.Body,        "MountBody",   BuildPath("BD", bodyVariant, suffix)),
                new(PlayerSpritePartKind.Head,        "MountHead",   BuildPath("HD", headVariant, suffix)),
                new(PlayerSpritePartKind.Hair,        "MountHair",   BuildPath("HR", hairVariant, suffix)),
                new(PlayerSpritePartKind.Shoulder,    "MountShoulder", shoulderPath, IsShoulderRequired(bodyVariant)),
                new(PlayerSpritePartKind.LeftHand,    "MountLeftHand",  BuildPath("LH", bodyVariant, suffix)),
                new(PlayerSpritePartKind.RightHand,   "MountRightHand", BuildPath("RH", bodyVariant, suffix)),
                new(PlayerSpritePartKind.LeftWeapon,  "MountLeftWeapon", BuildPath("LW", leftWeaponVariant, suffix)),
                new(PlayerSpritePartKind.RightWeapon, "MountRightWeapon", BuildPath("RW", weaponVariant, suffix)),
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

        private static string BuildShoulderPath(int variant, string action)
        {
            // PC Settings/NpcRes/男主角肩膀.txt covers variants 001..021 only.
            return variant is >= 1 and <= 21 ? BuildPath("SH", variant, action) : string.Empty;
        }

        private static bool IsShoulderRequired(int variant)
        {
            // package.ini audit: SH_019 cast winner bytes are absent. Keep its path
            // for provenance, but do not fail or render a guessed shoulder layer.
            return variant is >= 1 and <= 21 && variant != ArmorVariant;
        }

        /// <summary>
        /// Map PC Skills.txt CharAnimId + current weapon type to a visual action.
        /// PC KNpc.cpp đổi CharAnimId thành CLIENTACTION rồi KNpcRes::SetAction chọn suffix theo vũ khí.
        /// </summary>
        public static PlayerVisualAction? ResolveAction(int charAnimId, PcWeaponType weapon)
        {
            return charAnimId switch
            {
                9 => PlayerVisualAction.Attack,
                10 => PlayerVisualAction.Attack1,
                11 => PlayerVisualAction.Magic,
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
