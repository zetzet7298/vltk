// -----------------------------------------------------------------------------
// VLTK Mobile — PC achievement.txt parser
// Source: settings/achievement/achievement.txt (250+ thành tựu).
// Columns: AchievementId Name Description Category ConditionType ConditionValue
//          RewardItemId RewardCount RewardExp Points
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcAchievementParser
    {
        public const int AchievementIdCol = 0;
        public const int NameCol = 1;
        public const int DescriptionCol = 2;
        public const int CategoryCol = 3;
        public const int ConditionTypeCol = 4;
        public const int ConditionValueCol = 5;
        public const int RewardItemIdCol = 6;
        public const int RewardCountCol = 7;
        public const int RewardExpCol = 8;
        public const int PointsCol = 9;

        public static List<PcAchievementEntry> ParseFile(string path)
        {
            var rows = new List<PcAchievementEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, AchievementIdCol);
                if (id <= 0) continue;
                rows.Add(new PcAchievementEntry
                {
                    achievementId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                    category = PcItemCommon.Int(cols, CategoryCol),
                    conditionType = PcItemCommon.Int(cols, ConditionTypeCol),
                    conditionValue = PcItemCommon.Int(cols, ConditionValueCol),
                    rewardItemId = PcItemCommon.Int(cols, RewardItemIdCol),
                    rewardCount = PcItemCommon.Int(cols, RewardCountCol),
                    rewardExp = PcItemCommon.Int(cols, RewardExpCol),
                    points = PcItemCommon.Int(cols, PointsCol),
                });
            }
            return rows;
        }

        public static PcAchievementRegistry BuildRegistry(string dir)
        {
            var reg = new PcAchievementRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f).ToLowerInvariant();
                if (name.StartsWith("achievement"))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcAchievementEntry
    {
        public int achievementId;
        public string nameRaw;
        public string description;
        public int category;
        public int conditionType;
        public int conditionValue;
        public int rewardItemId;
        public int rewardCount;
        public int rewardExp;
        public int points;
    }

    public sealed class PcAchievementRegistry
    {
        private readonly Dictionary<int, PcAchievementEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcAchievementEntry e) { if (e == null || e.achievementId <= 0) return; _byId[e.achievementId] = e; }
        public PcAchievementEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcAchievementEntry> GetByCategory(int category)
        {
            var list = new List<PcAchievementEntry>();
            foreach (var e in _byId.Values)
                if (e.category == category) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcAchievementEntry> All => new List<PcAchievementEntry>(_byId.Values);
    }
}
