// -----------------------------------------------------------------------------
// VLTK Mobile — Chuyển Sinh translife.txt bonus lookup/diff service.
// Source of truth: /var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/task/metempsychosis/translife.txt
// This service wraps the level-bonus table only; it is not the missing
// translifeskill.txt skill-unlock/effect table.
// -----------------------------------------------------------------------------

using System;
using System.IO;

namespace VLTK.Sandbox
{
    public sealed class TranslifeLevelBonusService
    {
        public const string SourceFileName = PcTranslifeLevelParser.SourceFileName;
        public const string PcSourceRelativePath = "Client 6.0/settings/task/metempsychosis/translife.txt";
        public const int MinSupportedLevel = 160;
        public const int MaxSupportedLevel = 200;
        public const int ExpectedLevelRowCount = 41;
        public const int HeaderColumnCount = PcTranslifeLevelParser.ExpectedColumnCount;
        public const int BonusGroupCount = PcTranslifeLevelParser.BonusGroupCount;

        private readonly PcTranslifeLevelRegistry _registry;

        public TranslifeLevelBonusService(PcTranslifeLevelRegistry registry)
        {
            _registry = registry ?? new PcTranslifeLevelRegistry();
        }

        public int SourceRowCount => _registry.Count;
        public int SourceHeaderColumnCount => HeaderColumnCount;
        public int SourceBonusGroupCount => BonusGroupCount;

        public static TranslifeLevelBonusService FromDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return new TranslifeLevelBonusService(null);

            return new TranslifeLevelBonusService(PcTranslifeLevelParser.BuildRegistry(directory));
        }

        public static bool IsSupportedLevel(int level)
            => level >= MinSupportedLevel && level <= MaxSupportedLevel;

        public static void ValidateLevel(int level)
        {
            if (!IsSupportedLevel(level))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    level,
                    $"translife.txt only defines Chuyển Sinh level bonuses for {MinSupportedLevel}..{MaxSupportedLevel}.");
            }
        }

        public PcTranslifeLevelBonus[] GetBonusGroups(int level)
        {
            var entry = RequireEntry(level);
            var result = new PcTranslifeLevelBonus[BonusGroupCount];
            if (entry.bonuses == null) return result;

            int count = Math.Min(BonusGroupCount, entry.bonuses.Length);
            Array.Copy(entry.bonuses, result, count);
            return result;
        }

        public PcTranslifeLevelBonus GetBonusGroup(int level, int oneBasedGroup)
        {
            ValidateGroup(oneBasedGroup);
            return GetBonusGroups(level)[oneBasedGroup - 1];
        }

        public TranslifeLevelBonusDelta[] GetDeltaByGroup(int fromLevel, int toLevel)
        {
            var from = GetBonusGroups(fromLevel);
            var to = GetBonusGroups(toLevel);
            var result = new TranslifeLevelBonusDelta[BonusGroupCount];
            for (int i = 0; i < BonusGroupCount; i++)
                result[i] = TranslifeLevelBonusDelta.Between(from[i], to[i]);
            return result;
        }

        public TranslifeLevelBonusDelta GetDeltaForGroup(int fromLevel, int toLevel, int oneBasedGroup)
        {
            ValidateGroup(oneBasedGroup);
            return GetDeltaByGroup(fromLevel, toLevel)[oneBasedGroup - 1];
        }

        private PcTranslifeLevelEntry RequireEntry(int level)
        {
            ValidateLevel(level);
            var entry = _registry.Get(level);
            if (entry == null)
                throw new InvalidOperationException($"translife.txt row for level {level} was not loaded.");
            return entry;
        }

        private static void ValidateGroup(int oneBasedGroup)
        {
            if (oneBasedGroup < 1 || oneBasedGroup > BonusGroupCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(oneBasedGroup),
                    oneBasedGroup,
                    $"translife.txt bonus groups are 1..{BonusGroupCount}.");
            }
        }
    }

    [Serializable]
    public struct TranslifeLevelBonusDelta
    {
        public int magicPoint;
        public int prop;
        public int resist;
        public int skillLimit;

        public bool HasAnyDelta => magicPoint != 0 || prop != 0 || resist != 0 || skillLimit != 0;

        public static TranslifeLevelBonusDelta Between(PcTranslifeLevelBonus from, PcTranslifeLevelBonus to)
        {
            return new TranslifeLevelBonusDelta
            {
                magicPoint = to.magicPoint - from.magicPoint,
                prop = to.prop - from.prop,
                resist = to.resist - from.resist,
                skillLimit = to.skillLimit - from.skillLimit,
            };
        }
    }
}
