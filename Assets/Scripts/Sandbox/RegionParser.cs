using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public class RegionSectionOffsets
    {
        public uint obstacleOffset, obstacleLength;
        public uint trapOffset, trapLength;
        public uint npcOffset, npcLength;
        public uint objOffset, objLength;
        public uint groundOffset, groundLength;
        public uint builtinOffset, builtinLength;
    }

    public class RegionParseResult
    {
        public bool success;
        public string error;
        public int sectionCount;
        public RegionSectionOffsets sections;
        public byte[] rawData;

        public bool HasObstacle => sections?.obstacleLength > 0;
        public bool HasGround => sections?.groundLength > 0;
        public bool HasBuiltin => sections?.builtinLength > 0;
        public bool HasTrap => sections?.trapLength > 0;
        public bool HasNpc => sections?.npcLength > 0;
        public bool HasObj => sections?.objLength > 0;
    }

    public static class RegionParser
    {
        public static RegionParseResult Parse(byte[] data)
        {
            var result = new RegionParseResult { rawData = data };

            if (data == null || data.Length < 4)
            {
                result.error = "Data too small";
                return result;
            }

            try
            {
                int pos = 0;
                uint sectionCount = ReadUInt32(data, ref pos);

                if (sectionCount < 1 || sectionCount > 20)
                {
                    result.error = $"Invalid section count: {sectionCount}";
                    return result;
                }

                int headerSize = 4 + (int)sectionCount * 8;
                if (data.Length < headerSize)
                {
                    result.error = "Header exceeds data";
                    return result;
                }

                var offsets = new uint[sectionCount * 2];
                for (int i = 0; i < sectionCount; i++)
                {
                    offsets[i * 2] = ReadUInt32(data, ref pos);     // offset
                    offsets[i * 2 + 1] = ReadUInt32(data, ref pos); // length
                }

                result.sectionCount = (int)sectionCount;
                result.sections = new RegionSectionOffsets();

                if (sectionCount > 0) { result.sections.obstacleOffset = offsets[0]; result.sections.obstacleLength = offsets[1]; }
                if (sectionCount > 1) { result.sections.trapOffset = offsets[2]; result.sections.trapLength = offsets[3]; }
                if (sectionCount > 2) { result.sections.npcOffset = offsets[4]; result.sections.npcLength = offsets[5]; }
                if (sectionCount > 3) { result.sections.objOffset = offsets[6]; result.sections.objLength = offsets[7]; }
                if (sectionCount > 4) { result.sections.groundOffset = offsets[8]; result.sections.groundLength = offsets[9]; }
                if (sectionCount > 5) { result.sections.builtinOffset = offsets[10]; result.sections.builtinLength = offsets[11]; }

                result.success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.error = ex.Message;
                return result;
            }
        }

        public static ObstacleGrid ExtractObstacle(RegionParseResult region, int mapId, int regionX, int regionY)
        {
            if (!region.success || !region.HasObstacle) return null;

            var sec = region.sections;
            int headerSize = 4 + region.sectionCount * 8;
            int start = headerSize + (int)sec.obstacleOffset;

            if (start + sec.obstacleLength > region.rawData.Length) return null;

            var grid = new ObstacleGrid
            {
                mapId = mapId,
                regionX = regionX,
                regionY = regionY,
                width = 16,
                height = 32,
                cellToWorldScale = 32f,
                cells = new byte[16 * 32],
            };

            // PC format: long[16][32] = int32[512], each value != 0 means blocked
            if (sec.obstacleLength == 2048)
            {
                for (int i = 0; i < 512; i++)
                {
                    int val = BitConverter.ToInt32(region.rawData, start + i * 4);
                    if (val != 0)
                        grid.cells[i] = ObstacleGrid.WalkBlocked;
                }
            }

            return grid;
        }

        private static uint ReadUInt32(byte[] data, ref int pos)
        {
            uint val = BitConverter.ToUInt32(data, pos);
            pos += 4;
            return val;
        }
    }
}
