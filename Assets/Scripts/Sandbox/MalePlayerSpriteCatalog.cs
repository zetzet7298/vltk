using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Sandbox
{
    public enum PlayerVisualAction
    {
        Idle,
        Move,
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

        public PlayerSpritePartSpec(PlayerSpritePartKind kind, string name, string sourcePath, bool required = true)
        {
            this.kind = kind;
            this.name = name;
            this.sourcePath = sourcePath;
            this.required = required;
        }
    }

    /// <summary>
    /// Male player sprite catalog ported from PC Settings/NpcRes tables.
    /// Uses the default male avatar set present in the JX source fixture:
    /// armor/head variant 19, empty hand weapon variant 000, and YY_999 shadow.
    /// Each referenced .spr has 8 directions; move uses RN01 (88 frames = 11 per direction).
    /// </summary>
    public static class MalePlayerSpriteCatalog
    {
        public const string SourceRoot = @"spr\npcres\man";
        public const int DirectionCount = 8;
        public const int ArmorVariant = 19;
        public const int WeaponVariant = 0;
        public const int ShadowVariant = 999;

        private static readonly PlayerSpritePartSpec[] IdleParts =
        {
            new PlayerSpritePartSpec(PlayerSpritePartKind.Shadow, "Shadow", SourceRoot + @"\MA_YY_999_ST01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.Body, "Body", SourceRoot + @"\MA_BD_019_ST01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.Head, "Head", SourceRoot + @"\MA_HD_019_ST01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.Hair, "Hair", SourceRoot + @"\MA_HR_019_ST01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.LeftHand, "LeftHand", SourceRoot + @"\MA_LH_019_ST01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.RightHand, "RightHand", SourceRoot + @"\MA_RH_019_ST01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.LeftWeapon, "LeftWeaponEmpty", SourceRoot + @"\MA_LW_000_ST01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.RightWeapon, "RightWeaponEmpty", SourceRoot + @"\MA_RW_000_ST01.spr"),
        };

        private static readonly PlayerSpritePartSpec[] MoveParts =
        {
            new PlayerSpritePartSpec(PlayerSpritePartKind.Shadow, "Shadow", SourceRoot + @"\MA_YY_999_RN01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.Body, "Body", SourceRoot + @"\MA_BD_019_RN01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.Head, "Head", SourceRoot + @"\MA_HD_019_RN01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.Hair, "Hair", SourceRoot + @"\MA_HR_019_RN01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.LeftHand, "LeftHand", SourceRoot + @"\MA_LH_019_RN01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.RightHand, "RightHand", SourceRoot + @"\MA_RH_019_RN01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.LeftWeapon, "LeftWeaponEmpty", SourceRoot + @"\MA_LW_000_RN01.spr"),
            new PlayerSpritePartSpec(PlayerSpritePartKind.RightWeapon, "RightWeaponEmpty", SourceRoot + @"\MA_RW_000_RN01.spr"),
        };

        // PC draw-order table: Settings/NpcRes/男主角贴图顺序表.txt, Dir1..Dir8.
        // Values are part ids in back-to-front draw order. -1 is the body shadow.
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

        public static IReadOnlyList<PlayerSpritePartSpec> GetParts(PlayerVisualAction action)
        {
            return action == PlayerVisualAction.Move ? MoveParts : IdleParts;
        }

        public static string BuildSourcePath(string fileName)
        {
            return SourceRoot + @"\" + fileName;
        }

        public static int DirectionFromMove(Vector2 move)
        {
            if (move.sqrMagnitude <= 0.0001f)
                return -1;

            float angle = Mathf.Atan2(move.y, move.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            // JX direction order used by the PC male SPRs:
            // 0=S, 1=SW, 2=W, 3=NW, 4=N, 5=NE, 6=E, 7=SE.
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

            // Stable fallback for optional/missing table entries.
            return 100 + part;
        }

    }
}
