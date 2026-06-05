// -----------------------------------------------------------------------------
// VLTK Mobile — Ranking Service (Xếp hạng runtime)
// Quản lý bảng xếp hạng theo level/gold/kill/faction.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public enum RankingType
    {
        Level = 0,
        Gold = 1,
        Kill = 2,
        Faction = 3,
    }

    /// <summary>
    /// Một dòng trong bảng xếp hạng.
    /// </summary>
    public class RankingEntry
    {
        public int rank;
        public int playerId;
        public string playerName;
        public int level;
        public int score;
        public int faction;
    }

    /// <summary>
    /// Service quản lý bảng xếp hạng.
    /// </summary>
    public class RankingService
    {
        public const string LogTag = "Ranking";

        private PcRankingRegistry _registry = new();
        private readonly Dictionary<int, RankingEntry> _liveScores = new();
        private int _weekEpoch = 0;

        public int Count => _registry?.Count ?? 0;

        public RankingService() { }
        public RankingService(PcRankingRegistry reg) { _registry = reg ?? new PcRankingRegistry(); }
        public void AttachRegistry(PcRankingRegistry reg) { _registry = reg ?? new PcRankingRegistry(); }

        /// <summary>Top N người chơi theo loại xếp hạng.</summary>
        public IReadOnlyList<RankingEntry> GetTopPlayers(int count, RankingType type)
        {
            if (count <= 0) return System.Array.Empty<RankingEntry>();
            var list = new List<RankingEntry>();
            // Ưu tiên live scores
            foreach (var ls in _liveScores.Values)
            {
                if (type == RankingType.Faction)
                {
                    // Faction rank tính theo điểm tổng hợp của faction
                }
                list.Add(ls);
            }
            // Kết hợp registry
            foreach (var r in _registry.GetByType((int)type))
            {
                if (!_liveScores.ContainsKey(r.playerId))
                {
                    list.Add(new RankingEntry
                    {
                        rank = 0,
                        playerId = r.playerId,
                        playerName = r.playerName,
                        level = r.level,
                        score = r.score,
                        faction = r.factionId,
                    });
                }
            }
            list.Sort((a, b) => b.score.CompareTo(a.score));
            for (int i = 0; i < list.Count; i++) list[i].rank = i + 1;
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return list;
        }

        /// <summary>Thứ hạng của 1 player theo loại. Trả 0 nếu chưa có.</summary>
        public int GetPlayerRank(int playerId, RankingType type)
        {
            var top = GetTopPlayers(int.MaxValue, type);
            foreach (var e in top)
                if (e.playerId == playerId) return e.rank;
            return 0;
        }

        /// <summary>Cập nhật điểm cho player.</summary>
        public bool UpdateScore(int playerId, RankingType type, int newScore)
        {
            if (playerId <= 0) return false;
            string name = _liveScores.TryGetValue(playerId, out var existing) ? existing.playerName : $"Player_{playerId}";
            int level = existing != null ? existing.level : 1;
            int faction = existing != null ? existing.faction : 0;
            _liveScores[playerId] = new RankingEntry
            {
                rank = 0,
                playerId = playerId,
                playerName = name,
                level = level,
                score = newScore,
                faction = faction,
            };
            return true;
        }

        /// <summary>Thứ hạng của 1 faction (cao = tốt). Trả 0 nếu không có data.</summary>
        public int GetFactionRank(int factionId)
        {
            if (factionId < 0) return 0;
            var top = GetTopPlayers(int.MaxValue, RankingType.Faction);
            // Sắp xếp theo tổng điểm faction
            var factionTotals = new Dictionary<int, int>();
            foreach (var e in top)
            {
                if (!factionTotals.ContainsKey(e.faction)) factionTotals[e.faction] = 0;
                factionTotals[e.faction] += e.score;
            }
            var sorted = new List<KeyValuePair<int, int>>(factionTotals);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
            for (int i = 0; i < sorted.Count; i++)
                if (sorted[i].Key == factionId) return i + 1;
            return 0;
        }

        /// <summary>Reset xếp hạng tuần.</summary>
        public void ResetWeekly()
        {
            _liveScores.Clear();
            _weekEpoch++;
        }

        public int CurrentWeek => _weekEpoch;

        public static RankingService LoadFromStreamingAssets()
        {
            var svc = new RankingService();
            try
            {
                string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcRanking");
                var reg = PcRankingParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            catch (System.Exception)
            {
                // empty registry fallback
            }
            return svc;
        }
    }
}
