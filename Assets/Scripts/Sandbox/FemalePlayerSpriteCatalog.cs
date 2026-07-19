// -----------------------------------------------------------------------------
// VLTK Mobile — PC female player sprite catalog
// Source: PC Settings/NpcRes/女主角*.txt
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>Female FM_* player layers. Horse HH/HB/HT use canonical MA_* paths.</summary>
    public static class FemalePlayerSpriteCatalog
    {
        public const string SourceRoot = @"spr\npcres\woman";
        public const int DirectionCount = 8;
        // package.ini winners: FM body/head/hands use 019; MA horse uses 001.
        // HR_019 bytes are absent, so keep 019 required and report the exact hole.
        public const int ArmorVariant = 019;
        public const int MountArmorVariant = 019;
        public const int MountAltArmorVariant = 072;
        public const int MountHorseVariant = 001;
        public const int MountAltHorseVariant = 018;
        public const int ShadowVariant = 999;
        public const int EmptyWeaponVariant = 0;
        public const int ShortWeaponVariant = 001;
        public const int StaffWeaponVariant = 010;
        public const int DualWeaponVariant = 013;

        private static readonly int[] WeaponSprVariant =
        {
            EmptyWeaponVariant, ShortWeaponVariant, StaffWeaponVariant, DualWeaponVariant, EmptyWeaponVariant,
        };
        private static readonly string[] WalkSuffix = { "WK01", "WK02", "WK03", "WK04", "WK01" };
        public const string SitSuffix = "ZZ01";
        public const string JumpSuffix = "JP01";

        // PC 女主角贴图顺序表.txt. Same direction order as male table.
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

        public static int GetWeaponSprVariant(PcWeaponType weapon) => WeaponSprVariant[(int)weapon];

        public static PlayerSpritePartSpec[] BuildParts(PlayerVisualAction action, PcWeaponType weapon,
            int bodyVariant = ArmorVariant, int headVariant = ArmorVariant, int weaponVariant = int.MinValue,
            int hairVariant = ArmorVariant, int horseVariant = MountHorseVariant)
        {
            int effectiveWeaponVariant = weaponVariant == int.MinValue ? GetWeaponSprVariant(weapon) : weaponVariant;
            if (action == PlayerVisualAction.Ride || action == PlayerVisualAction.RideWalk || action == PlayerVisualAction.RideMove ||
                action == PlayerVisualAction.RideAttack || action == PlayerVisualAction.RideAttack1 || action == PlayerVisualAction.RideMagic)
                return BuildMountedParts(bodyVariant, headVariant, hairVariant, horseVariant,
                    PlayerMountService.GetMountedActionSuffix(action, weapon), weapon, effectiveWeaponVariant);

            int wIdx = (int)weapon;
            string suffix = action switch
            {
                PlayerVisualAction.Walk => WalkSuffix[wIdx],
                PlayerVisualAction.Sit => SitSuffix,
                PlayerVisualAction.Jump => JumpSuffix,
                _ => MalePlayerSpriteCatalog.ResolveFootActionSuffix(action, weapon, effectiveWeaponVariant),
            };
            int leftWeaponVariant = weapon == PcWeaponType.DualWeapon ? effectiveWeaponVariant : EmptyWeaponVariant;
            bool leftWeaponRequired = weapon != PcWeaponType.LongWeapon;
            return new[]
            {
                // No FM_YY source winner in current package. Keep non-required, never borrow a guessed shadow.
                new PlayerSpritePartSpec(PlayerSpritePartKind.Shadow, "Shadow", FemaleShadowPath(suffix), false),
                new PlayerSpritePartSpec(PlayerSpritePartKind.Body, "Body", BuildPath("BD", bodyVariant, suffix)),
                new PlayerSpritePartSpec(PlayerSpritePartKind.Head, "Head", BuildPath("HD", headVariant, suffix)),
                new PlayerSpritePartSpec(PlayerSpritePartKind.Hair, "Hair", BuildPath("HR", hairVariant, suffix)),
                // Female shoulder is optional; no canonical source winner exists.
                new PlayerSpritePartSpec(PlayerSpritePartKind.Shoulder, "Shoulder", string.Empty, false),
                new PlayerSpritePartSpec(PlayerSpritePartKind.LeftHand, "LeftHand", BuildPath("LH", bodyVariant, suffix)),
                new PlayerSpritePartSpec(PlayerSpritePartKind.RightHand, "RightHand", BuildPath("RH", bodyVariant, suffix)),
                new PlayerSpritePartSpec(PlayerSpritePartKind.LeftWeapon, "LeftWeapon", BuildPath("LW", leftWeaponVariant, suffix), leftWeaponRequired),
                new PlayerSpritePartSpec(PlayerSpritePartKind.RightWeapon, "RightWeapon", BuildPath("RW", effectiveWeaponVariant, suffix)),
            };
        }

        public static PlayerSpritePartSpec[] BuildMountedParts(int bodyVariant, int headVariant, int hairVariant, int horseVariant, string suffix)
            => BuildMountedParts(bodyVariant, headVariant, hairVariant, horseVariant, suffix, PcWeaponType.EmptyHand, EmptyWeaponVariant);

        public static PlayerSpritePartSpec[] BuildMountedParts(int bodyVariant, int headVariant, int hairVariant, int horseVariant,
            string suffix, PcWeaponType weapon, int weaponVariant)
        {
            int leftWeaponVariant = weapon == PcWeaponType.DualWeapon ? weaponVariant : EmptyWeaponVariant;
            bool leftWeaponRequired = weapon != PcWeaponType.LongWeapon;
            return new[]
            {
                new PlayerSpritePartSpec(PlayerSpritePartKind.Shadow, "Shadow", FemaleShadowPath(suffix), false),
                // PC 女主角 horse columns resolve MA_H* files, not FM_H* substitutes.
                new PlayerSpritePartSpec(PlayerSpritePartKind.HorseFront, "HorseFront", MalePlayerSpriteCatalog.BuildPath("HH", horseVariant, suffix), true, 8),
                new PlayerSpritePartSpec(PlayerSpritePartKind.HorseMiddle, "HorseMiddle", MalePlayerSpriteCatalog.BuildPath("HB", horseVariant, suffix), true, 8),
                new PlayerSpritePartSpec(PlayerSpritePartKind.HorseRear, "HorseRear", MalePlayerSpriteCatalog.BuildPath("HT", horseVariant, suffix), true, 8),
                new PlayerSpritePartSpec(PlayerSpritePartKind.Body, "MountBody", BuildPath("BD", bodyVariant, suffix)),
                new PlayerSpritePartSpec(PlayerSpritePartKind.Head, "MountHead", BuildPath("HD", headVariant, suffix)),
                new PlayerSpritePartSpec(PlayerSpritePartKind.Hair, "MountHair", BuildPath("HR", hairVariant, suffix)),
                // Fail closed until exact female mounted shoulder source is recovered.
                new PlayerSpritePartSpec(PlayerSpritePartKind.Shoulder, "MountShoulder", string.Empty, false),
                new PlayerSpritePartSpec(PlayerSpritePartKind.LeftHand, "MountLeftHand", BuildPath("LH", bodyVariant, suffix)),
                new PlayerSpritePartSpec(PlayerSpritePartKind.RightHand, "MountRightHand", BuildPath("RH", bodyVariant, suffix)),
                new PlayerSpritePartSpec(PlayerSpritePartKind.LeftWeapon, "MountLeftWeapon", BuildPath("LW", leftWeaponVariant, suffix), leftWeaponRequired),
                new PlayerSpritePartSpec(PlayerSpritePartKind.RightWeapon, "MountRightWeapon", BuildPath("RW", weaponVariant, suffix)),
            };
        }

        public static string BuildPath(string part, int variant, string action)
            => SourceRoot + @"\FM_" + part + "_" + variant.ToString("D3") + "_" + action + ".spr";

        public static string FemaleShadowPath(string action)
            => SourceRoot + @"\FM_YY_" + ShadowVariant.ToString("D3") + "_" + action + ".spr";

        public static int DirectionFromMove(Vector2 move) => MalePlayerSpriteCatalog.DirectionFromMove(move);

        public static int SortingOffset(PlayerSpritePartKind kind, int direction)
        {
            int part = (int)kind;
            var order = DrawOrderByDirection[Mathf.Clamp(direction, 0, DirectionCount - 1)];
            for (int i = 0; i < order.Length; i++)
                if (order[i] == part) return i * 2;
            return 100 + part;
        }
    }
}
