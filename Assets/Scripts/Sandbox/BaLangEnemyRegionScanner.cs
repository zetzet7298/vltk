// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Single NPC spawn entry parsed from PC Region_S.dat section 2 (NPC section).
    /// Contains MPS (map-pixel-space) coordinates, template ID, level, direction, and series.
    /// </summary>
    [Serializable]
    public struct RegionSSpawnEntry
    {
        public int templateId;
        public int mpsX;
        public int mpsY;
        public string nameRaw;
        public int level;
        public int curFrame; // facing direction
        public int kind;     // 0=animal/enemy, 3=town NPC
        public int camp;
        public int series;   // ngũ hành element
        public string script;
    }

    /// <summary>
    /// Parses PC Region_S.dat binary files to extract NPC spawn data.
    /// Format: KCombinFileSection header + KNpcFileHead + KSPNpc variable-length records.
    /// Reference: jxwin-kinnox SceneDataDef.h (KCombinFileSection, KNpcFileHead, KSPNpc).
    /// </summary>
    public static class BaLangEnemyRegionScanner
    {
        /// <summary>
        /// Scan all Region_S.dat files for a map and extract NPC spawns.
        /// Uses the real PC binary format — not Region_C critters.
        /// </summary>
        public static List<RegionSSpawnEntry> ScanRegionS(string regionFolder)
        {
            var result = new List<RegionSSpawnEntry>();
            if (!Directory.Exists(regionFolder)) return result;

            foreach (var fp in Directory.GetFiles(regionFolder, "*_Region_S.dat"))
            {
                try
                {
                    var entries = ParseRegionSFile(fp);
                    result.AddRange(entries);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RegionScanner] Failed to parse {Path.GetFileName(fp)}: {ex.Message}");
                }
            }
            return result;
        }

        /// <summary>
        /// Parse a single Region_S.dat file. Returns all NPC entries from section 2.
        /// Format: DWORD sectionCount, then sectionCount * KCombinFileSection(off,len),
        /// then section data. NPC section index = 2 (REGION_NPC_FILE_INDEX).
        /// </summary>
        public static List<RegionSSpawnEntry> ParseRegionSFile(string filePath)
        {
            var entries = new List<RegionSSpawnEntry>();
            var data = File.ReadAllBytes(filePath);
            if (data.Length < 4) return entries;

            int sectionCount = BitConverter.ToInt32(data, 0);
            if (sectionCount < 4) return entries;

            int headerSize = 4 + sectionCount * 8;
            if (data.Length < headerSize) return entries;

            // Read NPC section (index 2) offset and length
            int npcOff = BitConverter.ToInt32(data, 4 + 2 * 8);
            int npcLen = BitConverter.ToInt32(data, 4 + 2 * 8 + 4);
            if (npcLen <= 0) return entries;

            int npcStart = headerSize + npcOff;
            if (npcStart + npcLen > data.Length) return entries;

            // KNpcFileHead: { uNumNpc(4), uReserved1(4), uReserved2(4) }
            if (npcLen < 12) return entries;
            int numNpc = BitConverter.ToInt32(data, npcStart);

            // KSPNpc fixed part: templateId(4)+posX(4)+posY(4)+specialNpc(1)+reserved(3)+name(32)
            //   +level(2)+curFrame(2)+headImg(2)+kind(2)+camp(1)+series(1)+scriptNameLen(2) = 60 bytes
            const int FixedSize = 60;
            int pos = npcStart + 12; // skip KNpcFileHead

            for (int i = 0; i < numNpc; i++)
            {
                if (pos + FixedSize > npcStart + npcLen) break;

                int tid = BitConverter.ToInt32(data, pos);
                int px = BitConverter.ToInt32(data, pos + 4);
                int py = BitConverter.ToInt32(data, pos + 8);
                // bool specialNpc at pos+12
                // reserved 3 bytes at pos+13
                string nameRaw = System.Text.Encoding.GetEncoding("gb2312")
                    .GetString(data, pos + 16, 32).Split('\0')[0];
                short level = BitConverter.ToInt16(data, pos + 48);
                short curFrame = BitConverter.ToInt16(data, pos + 50);
                // short headImg at pos+52
                short kind = BitConverter.ToInt16(data, pos + 54);
                byte camp = data[pos + 56];
                byte series = data[pos + 57];
                ushort scriptNameLen = BitConverter.ToUInt16(data, pos + 58);

                pos += FixedSize;

                string script = "";
                if (scriptNameLen > 0 && pos + scriptNameLen <= npcStart + npcLen)
                {
                    try
                    {
                        script = System.Text.Encoding.GetEncoding("gb2312")
                            .GetString(data, pos, scriptNameLen).Split('\0')[0];
                    }
                    catch { }
                    pos += scriptNameLen;
                }

                entries.Add(new RegionSSpawnEntry
                {
                    templateId = tid,
                    mpsX = px,
                    mpsY = py,
                    nameRaw = nameRaw,
                    level = level,
                    curFrame = curFrame,
                    kind = kind,
                    camp = camp,
                    series = series,
                    script = script,
                });
            }
            return entries;
        }
    }
}
