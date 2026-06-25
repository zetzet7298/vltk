// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/task/metempsychosis/translife.txt level table parser
// Source of truth: /var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/task/metempsychosis/translife.txt
// Schema: LEVEL + 7 repeating groups of MAGICPOINT/PROP/RESIST/SKILLLIMIT.
// This is the Chuyển Sinh level bonus table (levels 160..200), not translifeskill.txt.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTranslifeLevelParser
    {
        public const string SourceFileName = "translife.txt";
        public const int ExpectedColumnCount = 29;
        public const int BonusGroupCount = 7;
        public const int LevelCol = 0;

        public static List<PcTranslifeLevelEntry> ParseFile(string path)
        {
            var rows = new List<PcTranslifeLevelEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            return ParseLines(PcItemCommon.ReadServerLines(path));
        }

        public static List<PcTranslifeLevelEntry> ParseLines(IEnumerable<string> lines)
        {
            var rows = new List<PcTranslifeLevelEntry>();
            if (lines == null) return rows;

            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped)
                {
                    headerSkipped = true;
                    continue;
                }

                var cols = line.Split('\t');
                int level = PcItemCommon.Int(cols, LevelCol);
                if (level <= 0) continue;

                var bonuses = new PcTranslifeLevelBonus[BonusGroupCount];
                for (int i = 0; i < BonusGroupCount; i++)
                {
                    int start = 1 + i * 4;
                    bonuses[i] = new PcTranslifeLevelBonus
                    {
                        magicPoint = PcItemCommon.Int(cols, start),
                        prop = PcItemCommon.Int(cols, start + 1),
                        resist = PcItemCommon.Int(cols, start + 2),
                        skillLimit = PcItemCommon.Int(cols, start + 3),
                    };
                }

                rows.Add(new PcTranslifeLevelEntry
                {
                    level = level,
                    bonuses = bonuses,
                });
            }
            return rows;
        }

        public static PcTranslifeLevelRegistry BuildRegistry(string dir)
        {
            var reg = new PcTranslifeLevelRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string path = Path.Combine(dir, SourceFileName);
            foreach (var row in ParseFile(path)) reg.Register(row);
            return reg;
        }
    }

    [Serializable]
    public struct PcTranslifeLevelBonus
    {
        public int magicPoint;
        public int prop;
        public int resist;
        public int skillLimit;

        public bool HasAnyValue => magicPoint != 0 || prop != 0 || resist != 0 || skillLimit != 0;
    }

    [Serializable]
    public class PcTranslifeLevelEntry
    {
        public int level;
        public PcTranslifeLevelBonus[] bonuses = Array.Empty<PcTranslifeLevelBonus>();

        public PcTranslifeLevelBonus GetBonusGroup(int oneBasedGroup)
        {
            int index = oneBasedGroup - 1;
            if (index < 0 || bonuses == null || index >= bonuses.Length)
                return default;
            return bonuses[index];
        }
    }

    public sealed class PcTranslifeLevelRegistry
    {
        private readonly Dictionary<int, PcTranslifeLevelEntry> _byLevel = new();

        public int Count => _byLevel.Count;
        public IEnumerable<PcTranslifeLevelEntry> All => _byLevel.Values;

        public void Register(PcTranslifeLevelEntry e)
        {
            if (e == null || e.level <= 0) return;
            _byLevel[e.level] = e;
        }

        public PcTranslifeLevelEntry Get(int level)
            => _byLevel.TryGetValue(level, out var v) ? v : null;
    }
}
