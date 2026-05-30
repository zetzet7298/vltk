using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M1.7 — Parses the NPC spawn section from a region .dat file.
    /// NPC section format (from PC source KSceneNpc):
    ///   uint32 count
    ///   count × entry:
    ///     int32  posX, posY
    ///     uint32 templateId
    ///     byte   direction (0-7)
    ///     char[128] scriptRef (Lua script name, GB2312)
    /// </summary>
    public class NpcSectionData
    {
        public uint count;
        public List<NpcRawEntry> entries = new();
    }

    public class NpcRawEntry
    {
        public int posX, posY;
        public uint templateId;
        public byte direction;
        public string scriptRef;
    }

    /// <summary>
    /// M1.7 — Parses the object placement section from a region .dat file.
    /// Object section format (from PC source KSceneObj):
    ///   uint32 count
    ///   count × entry:
    ///     int32  posX, posY
    ///     uint32 spriteId
    ///     uint16 frame
    ///     byte   layer
    ///     byte   flags (0x01 = foreground)
    ///     char[64] spritePath (GB2312)
    /// </summary>
    public class ObjSectionData
    {
        public uint count;
        public List<ObjRawEntry> entries = new();
    }

    public class ObjRawEntry
    {
        public int posX, posY;
        public uint spriteId;
        public ushort frame;
        public byte layer;
        public byte flags;
        public string spritePath;
        public bool isForeground => (flags & 0x01) != 0;
    }

    public static class NpcSectionParser
    {
        private const int NPC_STRUCT_MIN = 4 + 4 + 4 + 1; // posX+posY+templateId+dir = 13

        public static NpcSectionData Parse(byte[] sectionData)
        {
            if (sectionData == null || sectionData.Length < 4) return null;
            var result = new NpcSectionData();
            int pos = 0;
            result.count = ReadUInt32(sectionData, ref pos);

            var gbk = Encoding.GetEncoding("GB2312");
            for (int i = 0; i < result.count; i++)
            {
                if (pos + NPC_STRUCT_MIN > sectionData.Length) break;

                var entry = new NpcRawEntry
                {
                    posX = ReadInt32(sectionData, ref pos),
                    posY = ReadInt32(sectionData, ref pos),
                    templateId = ReadUInt32(sectionData, ref pos),
                    direction = sectionData[pos++],
                };

                if (pos + 128 <= sectionData.Length)
                {
                    entry.scriptRef = gbk.GetString(sectionData, pos, 128).TrimEnd('\0');
                    pos += 128;
                }

                result.entries.Add(entry);
            }
            return result;
        }

        public static NpcSectionData ExtractFromRegion(RegionParseResult region)
        {
            if (!region.success || !region.HasNpc) return null;
            var sec = region.sections;
            int headerSize = 4 + region.sectionCount * 8;
            int start = headerSize + (int)sec.npcOffset;
            int length = (int)sec.npcLength;
            if (start + length > region.rawData.Length) return null;
            var data = new byte[length];
            Array.Copy(region.rawData, start, data, 0, length);
            return Parse(data);
        }

        private static uint ReadUInt32(byte[] d, ref int p) { uint v = BitConverter.ToUInt32(d, p); p += 4; return v; }
        private static int ReadInt32(byte[] d, ref int p) { int v = BitConverter.ToInt32(d, p); p += 4; return v; }
    }

    public static class ObjSectionParser
    {
        private const int OBJ_STRUCT_MIN = 4 + 4 + 4 + 2 + 1 + 1; // posX+posY+sprId+frame+layer+flags = 16

        public static ObjSectionData Parse(byte[] sectionData)
        {
            if (sectionData == null || sectionData.Length < 4) return null;
            var result = new ObjSectionData();
            int pos = 0;
            result.count = ReadUInt32(sectionData, ref pos);

            var gbk = Encoding.GetEncoding("GB2312");
            for (int i = 0; i < result.count; i++)
            {
                if (pos + OBJ_STRUCT_MIN > sectionData.Length) break;

                var entry = new ObjRawEntry
                {
                    posX = ReadInt32(sectionData, ref pos),
                    posY = ReadInt32(sectionData, ref pos),
                    spriteId = ReadUInt32(sectionData, ref pos),
                    frame = ReadUInt16(sectionData, ref pos),
                    layer = sectionData[pos++],
                    flags = sectionData[pos++],
                };

                if (pos + 64 <= sectionData.Length)
                {
                    entry.spritePath = gbk.GetString(sectionData, pos, 64).TrimEnd('\0');
                    pos += 64;
                }

                result.entries.Add(entry);
            }
            return result;
        }

        public static ObjSectionData ExtractFromRegion(RegionParseResult region)
        {
            if (!region.success || !region.HasObj) return null;
            var sec = region.sections;
            int headerSize = 4 + region.sectionCount * 8;
            int start = headerSize + (int)sec.objOffset;
            int length = (int)sec.objLength;
            if (start + length > region.rawData.Length) return null;
            var data = new byte[length];
            Array.Copy(region.rawData, start, data, 0, length);
            return Parse(data);
        }

        private static uint ReadUInt32(byte[] d, ref int p) { uint v = BitConverter.ToUInt32(d, p); p += 4; return v; }
        private static int ReadInt32(byte[] d, ref int p) { int v = BitConverter.ToInt32(d, p); p += 4; return v; }
        private static ushort ReadUInt16(byte[] d, ref int p) { ushort v = BitConverter.ToUInt16(d, p); p += 2; return v; }
    }

    /// <summary>
    /// M1.7 — Converts NpcSectionData + ObjSectionData into RegionSpawnManifest.
    /// AC#1: templateId, position, region, direction, scriptRef.
    /// AC#3: missing templates reported separately from missing spawns.
    /// AC#4: draw count + performance warning.
    /// </summary>
    public static class SpawnSectionConverter
    {
        private const int SPAWN_WARN_THRESHOLD = 200;

        public static RegionSpawnManifest Convert(
            NpcSectionData npcData,
            ObjSectionData objData,
            int mapId, int regionX, int regionY,
            string sourceRegionPath = null)
        {
            var manifest = new RegionSpawnManifest
            {
                mapId = mapId,
                regionX = regionX,
                regionY = regionY,
                sourceRegionFile = sourceRegionPath,
            };

            // AC#1: NPC spawns
            if (npcData?.entries != null)
            {
                foreach (var raw in npcData.entries)
                {
                    var spawn = new NpcSpawn
                    {
                        spawnIndex = manifest.npcSpawns.Count,
                        templateId = (int)raw.templateId,
                        posX = raw.posX,
                        posY = raw.posY,
                        direction = (NpcDirection)Mathf.Clamp(raw.direction, 0, 7),
                        regionX = regionX,
                        regionY = regionY,
                        scriptRef = raw.scriptRef ?? "",
                        // AC#3: templateId=0 treated as missing template
                        templateFound = raw.templateId != 0,
                    };

                    if (!spawn.templateFound)
                    {
                        spawn.warnings.Add($"Template ID 0 — no NPC template reference (AC#3)");
                        manifest.missingTemplates++;
                    }

                    manifest.npcSpawns.Add(spawn);
                }
                manifest.totalNpcs = manifest.npcSpawns.Count;
            }

            // AC#1: Object placements
            if (objData?.entries != null)
            {
                foreach (var raw in objData.entries)
                {
                    var obj = new ObjectPlacement
                    {
                        placementIndex = manifest.objects.Count,
                        spriteId = (int)raw.spriteId,
                        spritePath = raw.spritePath ?? "",
                        posX = raw.posX,
                        posY = raw.posY,
                        layer = raw.layer,
                        zOrder = raw.layer,
                        flags = raw.flags,
                        isForeground = raw.isForeground,
                        spriteMissing = string.IsNullOrEmpty(raw.spritePath),
                    };

                    if (obj.spriteMissing)
                        manifest.missingSprites++;

                    manifest.objects.Add(obj);
                }
                manifest.totalObjects = manifest.objects.Count;
            }

            // AC#4: Performance warning
            int totalSpawns = manifest.totalNpcs + manifest.totalObjects;
            if (totalSpawns > SPAWN_WARN_THRESHOLD)
                SubsystemLog.Warn("Spawn",
                    $"Region [{regionX},{regionY}]: {totalSpawns} spawns — GM Panel shows performance warning (AC#4)");

            manifest.status = (manifest.missingTemplates + manifest.missingSprites == 0)
                ? ConversionStatus.Complete
                : (totalSpawns > 0 ? ConversionStatus.Partial : ConversionStatus.NotStarted);

            SubsystemLog.Info("Spawn",
                $"Region [{regionX},{regionY}]: {manifest.totalNpcs} NPCs ({manifest.missingTemplates} missing template), " +
                $"{manifest.totalObjects} objects ({manifest.missingSprites} missing sprite)");

            return manifest;
        }
    }
}
