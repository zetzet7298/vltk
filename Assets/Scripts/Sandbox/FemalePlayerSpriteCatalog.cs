// -----------------------------------------------------------------------------
// VLTK Mobile — ST-02.1 Female Player Sprite Catalog
// Mirror of MalePlayerSpriteCatalog for FM_* SPR parts.
// Source: PC Settings/NpcRes/woman, 女主角贴图顺序表.txt
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Female player sprite catalog ported from PC Settings/NpcRes tables.
    /// Uses FM_* prefix for body parts (FM_BD, FM_HD, FM_HR, FM_LH, FM_RH).
    /// Same weapon-type-aware action selection as male, same 8-direction system.
    /// Source: spr\npcres\woman
    ///
    /// PC differences vs male:
    ///   - Prefix is FM_ (not MA_/WO_); variant 050 is base female outfit.
    ///   - No separate shadow SPR (FM_YY_* not present in source). Shadow slot
    ///     is built but marked not required; runtime simply leaves it unloaded.
    ///   - No separate weapon SPRs (FM_LW_*/FM_RW_* not present). LH/RH hands
    ///     already encode the weapon pose for each action, so LW/RW slots are
    ///     built but marked not required.
    /// </summary>
    public static class FemalePlayerSpriteCatalog
    {
        public const string SourceRoot = @"spr\npcres\woman";
        public const int DirectionCount = 8;
        public const int ArmorVariant = 50;
        public const int MountArmorVariant = 050;
        public const int MountAltArmorVariant = 072;
        public const int MountHorseVariant = 016;
        public const int MountAltHorseVariant = 018;
        public const int ShadowVariant = 999;
        public const int EmptyWeaponVariant = 0;
        public const int StaffWeaponVariant = 010;
        public const string MountActionSuffix = "HM01";

        // PC action suffixes per weapon type. Same logic as male.
        private static readonly string[,] ActionSuffix = new string[4, 4]
        {
            // Idle,            Move,            Magic,            Attack
            { "ST01",          "RN01",          "MG01",           "AT01" }, // EmptyHand
            { "ST04",          "RN02",          "MG02",           "AT03" }, // ShortWeapon
            { "ST05",          "RN03",          "MG04",           "AT05" }, // LongWeapon
            { "ST06",          "RN04",          "MG05",           "AT07" }, // DualWeapon
        };

        // Weapon right-hand SPR variant per weapon type.
        // Female has no separate weapon SPRs, so these are placeholders only —
        // LW/RW slots are built but marked not required (see BuildParts).
        private static readonly int[] WeaponSprVariant = new int[4]
        {
            EmptyWeaponVariant,  // EmptyHand
            001,                 // ShortWeapon
            StaffWeaponVariant,  // LongWeapon
            002,                 // DualWeapon
        };

        // PC 女主角躯体.txt walk column (走路): WK01..WK04 per weapon; Sit (打坐)=ZZ01; Jump (跳跃)=JP01.
        // NOTE: female body SPRs at variant 019 are not present in the unpacked PAK set, so
        // Walk/Sit/Jump may not render for female until those assets are imported. Code stays
        // correct and mirrors the male catalog; the visual falls back gracefully on missing SPR.
        private static readonly string[] WalkSuffix = new string[4] { "WK01", "WK02", "WK03", "WK04" };
        public const string SitSuffix = "ZZ01";
        public const string JumpSuffix = "JP01";

        // PC draw-order table for female: 女主角贴图顺序表.txt
        // Dir1..Dir8 identical to male per PC source. Reuse works as-is.
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
        /// Build the SPR part spec list for the female player.
        /// </summary>
        public static PlayerSpritePartSpec[] BuildParts(PlayerVisualAction action, PcWeaponType weapon, int bodyVariant = ArmorVariant, int headVariant = ArmorVariant, int weaponVariant = EmptyWeaponVariant, int hairVariant = ArmorVariant, int horseVariant = MountHorseVariant)
        {
            if (action == PlayerVisualAction.Ride)
                return BuildMountedParts(50, 50, 50, horseVariant, MalePlayerSpriteCatalog.MountIdleSuffix);
            if (action == PlayerVisualAction.RideWalk)
                return BuildMountedParts(50, 50, 50, horseVariant, MalePlayerSpriteCatalog.MountWalkSuffix);
            if (action == PlayerVisualAction.RideMove)
                return BuildMountedParts(50, 50, 50, horseVariant, MalePlayerSpriteCatalog.MountMoveSuffix);

            int wIdx = (int)weapon;
            string suffix = action switch
            {
                PlayerVisualAction.Walk => WalkSuffix[wIdx],
                PlayerVisualAction.Sit  => SitSuffix,
                PlayerVisualAction.Jump => JumpSuffix,
                _ => ActionSuffix[wIdx, (int)action],
            };
            int lwVariant = (weapon == PcWeaponType.DualWeapon) ? weaponVariant : EmptyWeaponVariant;

            // Female has no separate weapon SPRs — LW/RW are not required.
            const bool leftWeaponRequired = false;
            const bool rightWeaponRequired = false;
            // npcres/woman has NO shadow SPR on disk (verified: 440 files, tags
            // BD/HD/HR/LH/RH only, no FM_YY_*). The path falls back to the male
            // shadow but the slot must NOT be required for the female set.
            const bool shadowRequired = false;

            return new PlayerSpritePartSpec[]
            {
                new(PlayerSpritePartKind.Shadow,      "Shadow",       FemaleShadowPath(suffix),                        shadowRequired),
                new(PlayerSpritePartKind.Body,         "Body",         BuildPath("BD", bodyVariant, suffix)),
                new(PlayerSpritePartKind.Head,         "Head",         BuildPath("HD", headVariant, suffix)),
                new(PlayerSpritePartKind.Hair,         "Hair",         BuildPath("HR", hairVariant, suffix)),
                new(PlayerSpritePartKind.LeftHand,     "LeftHand",     BuildPath("LH", bodyVariant, suffix)),
                new(PlayerSpritePartKind.RightHand,    "RightHand",    BuildPath("RH", bodyVariant, suffix)),
                new(PlayerSpritePartKind.LeftWeapon,   "LeftWeapon",   BuildPath("LW", lwVariant, suffix),             leftWeaponRequired),
                new(PlayerSpritePartKind.RightWeapon,  "RightWeapon",  BuildPath("RW", weaponVariant, suffix),             rightWeaponRequired),
            };
        }

        /// <summary>
        /// Mounted female rider + horse parts. Maps horse parts and shadow to male equivalents,
        /// and rider parts to female paths.
        /// </summary>
        public static PlayerSpritePartSpec[] BuildMountedParts(int bodyVariant, int headVariant, int hairVariant, int horseVariant, string suffix)
        {
            return new PlayerSpritePartSpec[]
            {
                // Shadow (maps to male shadow)
                new(PlayerSpritePartKind.Shadow,      "Shadow",       BuildPath("YY", ShadowVariant, suffix)),
                // Horse body (maps to male horse body)
                new(PlayerSpritePartKind.HorseFront,  "HorseFront",   MalePlayerSpriteCatalog.BuildPath("HH", horseVariant, suffix), true, 8),
                new(PlayerSpritePartKind.HorseMiddle, "HorseMiddle",  MalePlayerSpriteCatalog.BuildPath("HB", horseVariant, suffix), true, 8),
                new(PlayerSpritePartKind.HorseRear,   "HorseRear",    MalePlayerSpriteCatalog.BuildPath("HT", horseVariant, suffix), true, 8),
                // Rider
                new(PlayerSpritePartKind.Body,         "MountBody",    BuildPath("BD", bodyVariant, suffix)),
                new(PlayerSpritePartKind.Head,         "MountHead",    BuildPath("HD", headVariant, suffix)),
                new(PlayerSpritePartKind.Hair,         "MountHair",    BuildPath("HR", hairVariant, suffix)),
                new(PlayerSpritePartKind.LeftHand,     "MountLHand",   BuildPath("LH", bodyVariant, suffix)),
                new(PlayerSpritePartKind.RightHand,    "MountRHand",   BuildPath("RH", bodyVariant, suffix)),
            };
        }

        public static string BuildPath(string part, int variant, string action)
        {
            if (part == "YY")
            {
                // Female characters reuse male shadow SPRs since female-specific shadow files do not exist.
                return MalePlayerSpriteCatalog.BuildPath("YY", variant, action);
            }
            return SourceRoot + @"\FM_" + part + "_" + variant.ToString("D3") + "_" + action + ".spr";
        }

        /// <summary>
        /// Female shadow path. npcres/woman has no FM_YY_* shadow SPR on disk, so
        /// this is a placeholder kept under the FM_ prefix for catalog consistency;
        /// the slot is marked not required and never loaded at runtime.
        /// </summary>
        public static string FemaleShadowPath(string action)
            => SourceRoot + @"\FM_YY_" + ShadowVariant.ToString("D3") + "_" + action + ".spr";

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
