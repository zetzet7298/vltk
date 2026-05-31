using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M1.5 AC#1, AC#5 — Loads ObstacleGrid entries from a single packed binary
    /// (StreamingAssets/Obstacles.bin) instead of tens of thousands of loose JSON
    /// files. Loose files stall Unity's AssetDatabase Initial Refresh; one pack
    /// file keeps the editor fast and is friendlier for mobile builds.
    ///
    /// Pack format (little-endian), see vltktool/obstacle_pack.py:
    ///   HEADER (16 bytes): magic "VOBP", int32 version, int32 count, int32 dataSectionOffset
    ///   INDEX (count * 24 bytes): key[8] ascii, int16 width, int16 height,
    ///                             int32 blocked, int32 dataOff, int32 dataLen
    ///   DATA: concatenated raw cell bytes (1 byte/cell flags)
    ///
    /// Never parses raw PAK archives at runtime.
    /// </summary>
    public static class ObstacleGridLoader
    {
        private const string PACK_FILE = "Obstacles.bin";
        private const int HEADER_SIZE = 16;
        private const int INDEX_ENTRY_SIZE = 24;
        private static readonly byte[] Magic = { (byte)'V', (byte)'O', (byte)'B', (byte)'P' };

        private struct IndexEntry
        {
            public short width;
            public short height;
            public int blocked;
            public int dataOff;
            public int dataLen;
        }

        // Cached pack bytes + parsed index. Cleared via ResetCache for tests.
        private static byte[] _packBytes;
        private static Dictionary<string, IndexEntry> _index;
        private static bool _loadAttempted;
        private static string _packPathOverride;

        /// <summary>Test/diagnostic hook: point the loader at a specific pack file.</summary>
        public static void SetPackPathForTesting(string fullPath)
        {
            _packPathOverride = fullPath;
            ResetCache();
        }

        /// <summary>Clears cached pack + index so the next load re-reads from disk.</summary>
        public static void ResetCache()
        {
            _packBytes = null;
            _index = null;
            _loadAttempted = false;
        }

        private static string PackPath =>
            _packPathOverride ?? Path.Combine(Application.streamingAssetsPath, PACK_FILE);

        private static void EnsureLoaded()
        {
            if (_loadAttempted) return;
            _loadAttempted = true;

            var path = PackPath;
            if (!File.Exists(path))
            {
                SubsystemLog.Warn("ObstacleLoader", $"Obstacle pack not found: {path}");
                return;
            }

            try
            {
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length < HEADER_SIZE ||
                    bytes[0] != Magic[0] || bytes[1] != Magic[1] ||
                    bytes[2] != Magic[2] || bytes[3] != Magic[3])
                {
                    SubsystemLog.Error("ObstacleLoader", "Obstacle pack has invalid magic header");
                    return;
                }

                int version = BitConverter.ToInt32(bytes, 4);
                int count = BitConverter.ToInt32(bytes, 8);
                int dataSectionOffset = BitConverter.ToInt32(bytes, 12);

                if (count < 0 || dataSectionOffset > bytes.Length ||
                    HEADER_SIZE + (long)count * INDEX_ENTRY_SIZE > bytes.Length)
                {
                    SubsystemLog.Error("ObstacleLoader", $"Obstacle pack header out of range (v{version}, count={count})");
                    return;
                }

                var index = new Dictionary<string, IndexEntry>(count);
                int pos = HEADER_SIZE;
                for (int i = 0; i < count; i++)
                {
                    string key = Encoding.ASCII.GetString(bytes, pos, 8).TrimEnd();
                    short w = BitConverter.ToInt16(bytes, pos + 8);
                    short h = BitConverter.ToInt16(bytes, pos + 10);
                    int blocked = BitConverter.ToInt32(bytes, pos + 12);
                    int dOff = BitConverter.ToInt32(bytes, pos + 16);
                    int dLen = BitConverter.ToInt32(bytes, pos + 20);
                    index[key] = new IndexEntry
                    {
                        width = w, height = h, blocked = blocked, dataOff = dOff, dataLen = dLen,
                    };
                    pos += INDEX_ENTRY_SIZE;
                }

                _packBytes = bytes;
                _index = index;
                SubsystemLog.Info("ObstacleLoader",
                    $"Loaded obstacle pack v{version}: {count} regions ({bytes.Length:N0} bytes)");
            }
            catch (Exception ex)
            {
                SubsystemLog.Error("ObstacleLoader", $"Failed to read obstacle pack: {ex.Message}");
                _packBytes = null;
                _index = null;
            }
        }

        /// <summary>
        /// Load ObstacleGrid for a region file (e.g. "00015d99.dat").
        /// Returns null if the region is not present in the pack (AC#5 — caller
        /// must handle missing). Does not throw on bad input.
        /// </summary>
        public static ObstacleGrid LoadFromStreamingAssets(string regionFile)
        {
            if (string.IsNullOrEmpty(regionFile))
            {
                SubsystemLog.Warn("ObstacleLoader", "LoadFromStreamingAssets: null/empty regionFile");
                return null;
            }

            EnsureLoaded();
            if (_index == null || _packBytes == null)
                return null;

            var stem = Path.GetFileNameWithoutExtension(regionFile);
            if (!_index.TryGetValue(stem, out var entry))
            {
                SubsystemLog.Warn("ObstacleLoader", $"Region not in obstacle pack: {stem} — marked missing");
                return null;
            }

            byte[] cells;
            int expected = Mathf.Max(0, entry.width * entry.height);
            if (entry.dataLen > 0 &&
                entry.dataOff >= 0 &&
                (long)entry.dataOff + entry.dataLen <= _packBytes.Length)
            {
                cells = new byte[entry.dataLen];
                Buffer.BlockCopy(_packBytes, entry.dataOff, cells, 0, entry.dataLen);
            }
            else
            {
                cells = new byte[expected];
            }

            var grid = new ObstacleGrid
            {
                mapId = 0,   // will be set by caller
                regionX = 0,
                regionY = 0,
                width = entry.width,
                height = entry.height,
                cells = cells,
            };

            SubsystemLog.Info("ObstacleLoader",
                $"Loaded obstacle grid for {regionFile}: {entry.blocked}/{expected} blocked");
            return grid;
        }

        /// <summary>
        /// M1.5 AC#5 — Returns a passable grid with explicit defaults.
        /// Use when obstacle data is missing; the report should mark this as a risk.
        /// </summary>
        public static ObstacleGrid LoadDefault(int width = 16, int height = 32)
        {
            SubsystemLog.Warn("ObstacleLoader",
                $"Using default passable grid {width}x{height} — no obstacle data available. Mark as risk in report.");
            return new ObstacleGrid
            {
                mapId = 0,
                regionX = 0,
                regionY = 0,
                width = width,
                height = height,
                cells = new byte[width * height],  // all zeros = fully passable
            };
        }
    }
}
