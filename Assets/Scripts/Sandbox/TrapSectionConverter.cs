using System;
using System.Collections.Generic;
using System.Text;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M1.6 — Parses the trap section from a region .dat file.
    /// Trap section format (from PC source KSceneTrap):
    ///   uint32 count
    ///   count × KSceneTrap entries:
    ///     int32  x, y, w, h  (bounding rect in region cells)
    ///     uint32 scriptId
    ///     byte   triggerType
    ///     char[64] scriptName (optional, GB2312)
    /// </summary>
    public class TrapSectionData
    {
        public uint count;
        public List<TrapRawEntry> entries = new();
    }

    public class TrapRawEntry
    {
        public int x, y, width, height;
        public uint scriptId;
        public byte triggerType;
        public string scriptName;
    }

    public static class TrapSectionParser
    {
        private const int TRAP_STRUCT_MIN = 4 * 4 + 4 + 1;  // rect + scriptId + type = 21

        public static TrapSectionData Parse(byte[] sectionData)
        {
            if (sectionData == null || sectionData.Length < 4)
            {
                SubsystemLog.Warn("Trap", "Trap section data too small");
                return null;
            }

            var result = new TrapSectionData();
            int pos = 0;
            result.count = ReadUInt32(sectionData, ref pos);

            var gbk = Encoding.GetEncoding("GB2312");
            for (int i = 0; i < result.count; i++)
            {
                if (pos + TRAP_STRUCT_MIN > sectionData.Length) break;

                var entry = new TrapRawEntry
                {
                    x = ReadInt32(sectionData, ref pos),
                    y = ReadInt32(sectionData, ref pos),
                    width = ReadInt32(sectionData, ref pos),
                    height = ReadInt32(sectionData, ref pos),
                    scriptId = ReadUInt32(sectionData, ref pos),
                    triggerType = sectionData[pos++],
                };

                // Optional script name: up to 64 bytes, null-terminated
                if (pos + 64 <= sectionData.Length)
                {
                    entry.scriptName = gbk.GetString(sectionData, pos, 64).TrimEnd('\0');
                    pos += 64;
                }

                result.entries.Add(entry);
            }

            return result;
        }

        public static TrapSectionData ExtractFromRegion(RegionParseResult region)
        {
            if (!region.success || !region.HasTrap) return null;
            var sec = region.sections;
            int headerSize = 4 + region.sectionCount * 8;
            int start = headerSize + (int)sec.trapOffset;
            int length = (int)sec.trapLength;
            if (start + length > region.rawData.Length) return null;
            var data = new byte[length];
            Array.Copy(region.rawData, start, data, 0, length);
            return Parse(data);
        }

        private static uint ReadUInt32(byte[] d, ref int p) { uint v = BitConverter.ToUInt32(d, p); p += 4; return v; }
        private static int ReadInt32(byte[] d, ref int p) { int v = BitConverter.ToInt32(d, p); p += 4; return v; }
    }

    /// <summary>
    /// M1.6 — Converts TrapSectionData into RegionTrapManifest with
    /// TrapDefinition entries. AC#1-4.
    /// </summary>
    public static class TrapSectionConverter
    {
        public static RegionTrapManifest Convert(
            TrapSectionData trapData,
            int mapId, int regionX, int regionY,
            string sourceRegionPath = null)
        {
            var manifest = new RegionTrapManifest
            {
                mapId = mapId,
                regionX = regionX,
                regionY = regionY,
                sourceRegionFile = sourceRegionPath,
            };

            if (trapData == null)
            {
                manifest.status = ConversionStatus.NotStarted;
                return manifest;
            }

            manifest.totalTraps = trapData.entries.Count;

            foreach (var raw in trapData.entries)
            {
                // AC#1: TrapDefinition with bounds, scriptId/name, triggerType
                var def = new TrapDefinition
                {
                    trapIndex = manifest.traps.Count,
                    boundsRect = new RectDef
                    {
                        x = raw.x,
                        y = raw.y,
                        width = raw.width,
                        height = raw.height,
                    },
                    scriptRef = !string.IsNullOrEmpty(raw.scriptName)
                        ? raw.scriptName
                        : raw.scriptId.ToString(),
                    triggerType = (TrapTriggerType)Math.Min((int)raw.triggerType, 5),
                    scriptFound = false,  // script lookup deferred until Lua phase
                };

                // AC#4: Validate — if script reference is empty/zero, report it
                if (raw.scriptId == 0 && string.IsNullOrEmpty(raw.scriptName))
                {
                    def.warnings.Add("No script reference found — missing script reference");
                    manifest.missingScripts++;
                }

                manifest.traps.Add(def);
            }

            manifest.status = manifest.missingScripts == 0
                ? ConversionStatus.Complete
                : (manifest.traps.Count > manifest.missingScripts
                    ? ConversionStatus.Partial
                    : ConversionStatus.Failed);

            SubsystemLog.Info("Trap",
                $"Region [{regionX},{regionY}]: {manifest.totalTraps} traps, " +
                $"{manifest.missingScripts} missing scripts");

            return manifest;
        }
    }
}
