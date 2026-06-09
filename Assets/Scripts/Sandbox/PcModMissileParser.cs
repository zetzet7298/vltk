// -----------------------------------------------------------------------------
// VLTK Mobile — PC ModMissles.txt data-driven port
// Source: Assets/StreamingAssets/Reference/ModMissles.txt
// Purpose: keep expansion/event/title/boss missiles visible to mobile runtime
// instead of silently dropping ids when skills 1216+ cast.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public sealed class PcModMissileRow
    {
        public int missileId;
        public string nameRaw;
        public string nameNormalized;
        public int speed;
        public int lifetime;
        public int count;
        public int minRadius;
        public int maxRadius;
        public string sprFile;
        public int flyEventId;
        public int collideEventId;
        public int vanishEventId;
    }

    public static class PcModMissileParser
    {
        public const int ExpansionMinMissileId = 300;

        public static List<PcModMissileRow> ParseFile(string absolutePath, int minMissileId = 0)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<PcModMissileRow>();
            return ParseLines(PcItemCommon.ReadServerLines(absolutePath), minMissileId);
        }

        public static List<PcModMissileRow> ParseLines(IEnumerable<string> lines, int minMissileId = 0)
        {
            var rows = new List<PcModMissileRow>();
            if (lines == null) return rows;
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var c = line.Split('\t');
                if (c.Length < 33) continue;
                int id = Int(c, 0);
                if (id < minMissileId) continue;

                var row = new PcModMissileRow
                {
                    missileId = id,
                    nameRaw = Str(c, 1),
                    nameNormalized = Str(c, 1).Trim(),
                    lifetime = Int(c, 10),
                    speed = Int(c, 11),
                    count = Int(c, 14),
                    minRadius = Int(c, 6),
                    maxRadius = Int(c, 8),
                    sprFile = !string.IsNullOrEmpty(Str(c, 29)) ? Str(c, 29) :
                              !string.IsNullOrEmpty(Str(c, 32)) ? Str(c, 32) :
                              !string.IsNullOrEmpty(Str(c, 35)) ? Str(c, 35) : Str(c, 38),
                    flyEventId = Int(c, 18),
                    collideEventId = Int(c, 20),
                    vanishEventId = Int(c, 21),
                };
                rows.Add(row);
            }
            return rows;
        }

        public static List<PcMissileEntry> ToMissileEntries(List<PcModMissileRow> rows)
        {
            var result = new List<PcMissileEntry>();
            if (rows == null) return result;
            foreach (var r in rows)
            {
                result.Add(new PcMissileEntry
                {
                    missileId = r.missileId,
                    nameRaw = r.nameRaw,
                    nameNormalized = r.nameNormalized,
                    speed = r.speed,
                    lifetime = r.lifetime,
                    count = r.count,
                    minRadius = r.minRadius,
                    maxRadius = r.maxRadius,
                    sprFile = r.sprFile,
                    flyEventId = r.flyEventId,
                    collideEventId = r.collideEventId,
                    vanishEventId = r.vanishEventId,
                });
            }
            return result;
        }

        private static string Str(string[] c, int i) => i >= 0 && i < c.Length ? (c[i] ?? string.Empty).Trim() : string.Empty;
        private static int Int(string[] c, int i) => int.TryParse(Str(c, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
    }

    public static class PcMissileRegistry
    {
        private static readonly Dictionary<int, PcMissileEntry> _missiles = new();
        private static bool _initialized;
        public static int Count => _missiles.Count;

        /// <summary>
        /// Load runtime missile rows from the full PC missles1.txt source when it is
        /// staged under Reference/PcAttrib. The PC file contains duplicate id 408;
        /// sequential dictionary insertion keeps the later row, which is the policy
        /// used by this runtime registry. Legacy PcMissles/ModMissles remain fallback
        /// sources for environments without the audited PcAttrib copy.
        /// </summary>

        public static void Initialize(string streamingAssetsPath)
        {
            if (_initialized) return;

            string refPath = Path.Combine(streamingAssetsPath, "Reference");
            string missles1File = Path.Combine(refPath, "PcAttrib", "missles1.txt");
            string pcFile = Path.Combine(refPath, "PcMissles.txt");
            string modFile = Path.Combine(refPath, "ModMissles.txt");

            if (File.Exists(missles1File))
            {
                var list = PcModMissileParser.ToMissileEntries(PcModMissileParser.ParseFile(missles1File));
                foreach (var m in list) _missiles[m.missileId] = m;
            }
            else if (File.Exists(pcFile))
            {
                var list = PcConfigParser.ParseMissiles(pcFile);
                foreach (var m in list) _missiles[m.missileId] = m;
            }

            if (File.Exists(modFile))
            {
                var list = PcModMissileParser.ToMissileEntries(PcModMissileParser.ParseFile(modFile));
                foreach (var m in list)
                {
                    if (_missiles.TryGetValue(m.missileId, out var existing))
                    {
                        if (string.IsNullOrEmpty(existing.sprFile) && !string.IsNullOrEmpty(m.sprFile))
                            existing.sprFile = m.sprFile;
                        continue;
                    }

                    _missiles[m.missileId] = m;
                }
            }

            _initialized = true;
        }

        public static bool TryGet(int missileId, out PcMissileEntry entry)
        {
            if (!_initialized)
            {
                try
                {
                    Initialize(UnityEngine.Application.streamingAssetsPath);
                }
                catch
                {
                    // Fallback for tests or non-main-thread calls where Application properties might fail
                    Initialize(Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets"));
                }
            }
            return _missiles.TryGetValue(missileId, out entry);
        }

        public static void ClearAndInitialize(string streamingAssetsPath)
        {
            _missiles.Clear();
            _initialized = false;
            Initialize(streamingAssetsPath);
        }
    }
}
