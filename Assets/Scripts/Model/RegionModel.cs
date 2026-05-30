using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Model
{
    /// <summary>
    /// M1.2 — Section manifest for a region file. Records which sections were found
    /// and which are missing, with warnings for AC#3.
    /// </summary>
    [Serializable]
    public class RegionSectionManifest
    {
        public bool hasObstacle;
        public bool hasTrap;
        public bool hasNpc;
        public bool hasObj;
        public bool hasGround;
        public bool hasBuiltin;
        public List<string> missingSections = new();
        public List<string> warnings = new();
    }

    [Serializable]
    public class RegionDefinition
    {
        public int mapId;
        public int regionX;
        public int regionY;
        public string sourceRegionPath;
        public RectDef boundsPixels;
        public int loadPriority;

        // M1.2 AC#1-3: section manifest
        public RegionSectionManifest sectionManifest;
        public ConversionStatus sectionStatus;

        // M1.2 AC#4: neighbor references (-1 = not resolved)
        public int neighborRight  = -1;
        public int neighborBottom = -1;
    }

    [Serializable]
    public class ObstacleGrid
    {
        public int mapId;
        public int regionX;
        public int regionY;
        public int width;
        public int height;
        public float cellToWorldScale = 1f;
        public byte[] cells;

        public const byte WalkBlocked = 0x01;
        public const byte FlyBlocked = 0x02;
        public const byte JumpBlocked = 0x04;

        public bool CanWalk(int cx, int cy)
        {
            if (!InBounds(cx, cy)) return false;
            return (cells[cy * width + cx] & WalkBlocked) == 0;
        }

        public bool CanFly(int cx, int cy)
        {
            if (!InBounds(cx, cy)) return false;
            return (cells[cy * width + cx] & FlyBlocked) == 0;
        }

        public bool CanJump(int cx, int cy)
        {
            if (!InBounds(cx, cy)) return false;
            return (cells[cy * width + cx] & JumpBlocked) == 0;
        }

        public byte GetRawFlags(int cx, int cy)
        {
            if (!InBounds(cx, cy)) return 0xFF;
            return cells[cy * width + cx];
        }

        private bool InBounds(int cx, int cy)
        {
            return cells != null && cx >= 0 && cx < width && cy >= 0 && cy < height;
        }
    }

    [Serializable]
    public enum ObstacleCellState
    {
        Normal = 0,
        WalkBlocked = 1,
        FlyBlocked = 2,
        JumpBlocked = 4,
    }
}
