using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class GroundTileRecord
    {
        public ushort h;
        public ushort v;
        public ushort frame;
        public string spriteName;
    }

    public class GroundObjectRecord
    {
        public int positionX;
        public int positionY;
        public string imageName;
        public ushort width;
        public ushort height;
        public ushort frame;
        public byte relateRegion;
        public byte order;
        public short layer;
    }

    public class GroundLayerData
    {
        public uint numTiles;
        public uint numObjects;
        public uint objectDataOffset;
        public List<GroundTileRecord> tiles = new();
        public List<GroundObjectRecord> objects = new();
    }

    public static class GroundLayerParser
    {
        public static GroundLayerData Parse(byte[] sectionData)
        {
            if (sectionData == null || sectionData.Length < 12)
            {
                SubsystemLog.Warn("Ground", "Section data too small for header");
                return null;
            }

            var result = new GroundLayerData();
            int pos = 0;

            // KGroundFileHead (12 bytes)
            result.numTiles = ReadUInt32(sectionData, ref pos);
            result.numObjects = ReadUInt32(sectionData, ref pos);
            result.objectDataOffset = ReadUInt32(sectionData, ref pos);

            // Parse tiles (KSPRCrunode — variable length)
            for (int i = 0; i < result.numTiles; i++)
            {
                if (pos + 8 > sectionData.Length) break;

                var tile = new GroundTileRecord
                {
                    h = ReadUInt16(sectionData, ref pos),
                    v = ReadUInt16(sectionData, ref pos),
                    frame = ReadUInt16(sectionData, ref pos),
                };
                ushort nameLen = ReadUInt16(sectionData, ref pos);

                if (pos + nameLen > sectionData.Length) break;

                var gbk = Encoding.GetEncoding("GB2312");
                tile.spriteName = nameLen > 0
                    ? gbk.GetString(sectionData, pos, nameLen)
                    : "";
                pos += nameLen;

                result.tiles.Add(tile);
            }

            // Seek to object data offset if specified
            if (result.objectDataOffset > 0 && result.objectDataOffset < sectionData.Length)
                pos = (int)result.objectDataOffset;

            // Parse objects (KSPRCoverGroundObj — packed, fixed size)
            // Struct is packed(2): 4+4+128+2+2+2+1+1+2 = 146 bytes
            for (int i = 0; i < result.numObjects; i++)
            {
                if (pos + 146 > sectionData.Length) break;

                var obj = new GroundObjectRecord
                {
                    positionX = ReadInt32(sectionData, ref pos),
                    positionY = ReadInt32(sectionData, ref pos),
                };

                // szImage[128]
                var nameBytes = new byte[128];
                Array.Copy(sectionData, pos, nameBytes, 0, 128);
                var gbk = Encoding.GetEncoding("GB2312");
                obj.imageName = gbk.GetString(nameBytes).TrimEnd('\0');
                pos += 128;

                obj.width = ReadUInt16(sectionData, ref pos);
                obj.height = ReadUInt16(sectionData, ref pos);
                obj.frame = ReadUInt16(sectionData, ref pos);
                obj.relateRegion = sectionData[pos++];
                obj.order = sectionData[pos++];
                obj.layer = ReadInt16(sectionData, ref pos);

                result.objects.Add(obj);
            }

            return result;
        }

        public static GroundLayerData ExtractFromRegion(RegionParseResult region)
        {
            if (!region.success || !region.HasGround) return null;

            var sec = region.sections;
            int headerSize = 4 + region.sectionCount * 8;
            int start = headerSize + (int)sec.groundOffset;
            int length = (int)sec.groundLength;

            if (start + length > region.rawData.Length) return null;

            var sectionData = new byte[length];
            Array.Copy(region.rawData, start, sectionData, 0, length);

            return Parse(sectionData);
        }

        private static uint ReadUInt32(byte[] d, ref int p)
        { uint v = BitConverter.ToUInt32(d, p); p += 4; return v; }

        private static int ReadInt32(byte[] d, ref int p)
        { int v = BitConverter.ToInt32(d, p); p += 4; return v; }

        private static ushort ReadUInt16(byte[] d, ref int p)
        { ushort v = BitConverter.ToUInt16(d, p); p += 2; return v; }

        private static short ReadInt16(byte[] d, ref int p)
        { short v = BitConverter.ToInt16(d, p); p += 2; return v; }
    }
}
