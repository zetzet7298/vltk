using System;
using System.Collections.Generic;
using System.Text;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class BuildinObjRecord
    {
        public uint props;
        public int imgX1, imgY1, imgZ1;
        public int imgX2, imgY2, imgZ2;
        public int imgX3, imgY3, imgZ3;
        public int imgX4, imgY4, imgZ4;
        public short imgWidth;
        public short imgHeight;
        public string imageName;
        public uint flipTime;
        public ushort frame;
        public ushort imgNumFrames;
        public ushort aniSpeed;
        public ushort order;
        public int oPosX1, oPosY1, oPosZ1;
        public int oPosX2, oPosY2, oPosZ2;
        public float angleXY;
        public float nodicalY;

        public bool IsPlaneTypeH => (props & 0x03) == 0x00;
        public bool IsPlaneTypeV => (props & 0x03) == 0x02;
    }

    public class BuildinObjData
    {
        public uint totalObjects;
        public ushort numTree;
        public ushort numLine;
        public ushort numPoint;
        public ushort numAbove;
        public ushort maxAboveHeadOrder;
        public ushort numLights;
        public List<BuildinObjRecord> objects = new();
    }

    public static class BuildinObjParser
    {
        public static BuildinObjData Parse(byte[] sectionData)
        {
            if (sectionData == null || sectionData.Length < 20)
            {
                SubsystemLog.Warn("Builtin", "Section data too small for header");
                return null;
            }

            var result = new BuildinObjData();
            int pos = 0;

            result.totalObjects = ReadUInt32(sectionData, ref pos);
            result.numTree = ReadUInt16(sectionData, ref pos);
            result.numLine = ReadUInt16(sectionData, ref pos);
            result.numPoint = ReadUInt16(sectionData, ref pos);
            result.numAbove = ReadUInt16(sectionData, ref pos);
            result.maxAboveHeadOrder = ReadUInt16(sectionData, ref pos);
            result.numLights = ReadUInt16(sectionData, ref pos);

            for (int i = 0; i < result.totalObjects; i++)
            {
                // KBuildinObj: variable but roughly 228 bytes
                if (pos + 228 > sectionData.Length) break;

                var obj = new BuildinObjRecord
                {
                    props = ReadUInt32(sectionData, ref pos),
                    imgX1 = ReadInt32(sectionData, ref pos),
                    imgY1 = ReadInt32(sectionData, ref pos),
                    imgZ1 = ReadInt32(sectionData, ref pos),
                    imgX2 = ReadInt32(sectionData, ref pos),
                    imgY2 = ReadInt32(sectionData, ref pos),
                    imgZ2 = ReadInt32(sectionData, ref pos),
                    imgX3 = ReadInt32(sectionData, ref pos),
                    imgY3 = ReadInt32(sectionData, ref pos),
                    imgZ3 = ReadInt32(sectionData, ref pos),
                    imgX4 = ReadInt32(sectionData, ref pos),
                    imgY4 = ReadInt32(sectionData, ref pos),
                    imgZ4 = ReadInt32(sectionData, ref pos),
                    imgWidth = ReadInt16(sectionData, ref pos),
                    imgHeight = ReadInt16(sectionData, ref pos),
                };

                // szImage[128]
                int nameEnd = pos;
                int nameMax = pos + 128;
                while (nameEnd < nameMax && sectionData[nameEnd] != 0) nameEnd++;
                var gbk = Encoding.GetEncoding("GB2312");
                obj.imageName = gbk.GetString(sectionData, pos, nameEnd - pos);
                pos += 128;

                obj.flipTime = ReadUInt32(sectionData, ref pos);
                obj.frame = ReadUInt16(sectionData, ref pos);
                obj.imgNumFrames = ReadUInt16(sectionData, ref pos);
                obj.aniSpeed = ReadUInt16(sectionData, ref pos);
                obj.order = ReadUInt16(sectionData, ref pos);
                obj.oPosX1 = ReadInt32(sectionData, ref pos);
                obj.oPosY1 = ReadInt32(sectionData, ref pos);
                obj.oPosZ1 = ReadInt32(sectionData, ref pos);
                obj.oPosX2 = ReadInt32(sectionData, ref pos);
                obj.oPosY2 = ReadInt32(sectionData, ref pos);
                obj.oPosZ2 = ReadInt32(sectionData, ref pos);
                obj.angleXY = ReadFloat(sectionData, ref pos);
                obj.nodicalY = ReadFloat(sectionData, ref pos);

                result.objects.Add(obj);
            }

            return result;
        }

        public static BuildinObjData ExtractFromRegion(RegionParseResult region)
        {
            if (!region.success || !region.HasBuiltin) return null;

            var sec = region.sections;
            int headerSize = 4 + region.sectionCount * 8;
            int start = headerSize + (int)sec.builtinOffset;
            int length = (int)sec.builtinLength;

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

        private static float ReadFloat(byte[] d, ref int p)
        { float v = BitConverter.ToSingle(d, p); p += 4; return v; }
    }
}
