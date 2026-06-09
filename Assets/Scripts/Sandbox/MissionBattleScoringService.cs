// -----------------------------------------------------------------------------
// VLTK Mobile — Mission Battle Scoring Service
// Pure lookup model for PC Tống Kim battle scoring tables.
// PC evidence:
// - Server 6.0/server/home_jxser/server1/script/missions/battle/scoring.lua
// - Client 6.0/settings/missions/battle/combo.txt
// - Client 6.0/settings/missions/battle/scores.txt
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    public sealed class MissionBattleScoringFact
    {
        public string KillerRank { get; set; }
        public string DeadRank { get; set; }
        public bool KillerRankExists { get; set; }
        public bool DeadRankExists { get; set; }
        public int ComboValue { get; set; }
        public int ScoreValue { get; set; }
        public string ComboRowHeader { get; set; }
        public string ScoreRowHeader { get; set; }
        public string KillerSourceRowName { get; set; }
        public string DeadSourceColumnName { get; set; }
        public int PcKillerTitleIndex { get; set; }
        public int PcDeadTitleIndex { get; set; }

        public bool RanksExist => KillerRankExists && DeadRankExists;
        public bool IsValidCombo => RanksExist && ComboValue == 1;
    }

    /// <summary>
    /// Side-effect-free MissionBattle scoring lookup.
    /// PC scoring.lua loads combo/scores with 1-based title indices and returns
    /// score 0 for missing cells; combo is valid only when combo cell equals 1.
    /// </summary>
    public sealed class MissionBattleScoringService
    {
        public const string PcScoringLuaSource =
            "Server 6.0/server/home_jxser/server1/script/missions/battle/scoring.lua";
        public const string PcComboDataSource =
            "Client 6.0/settings/missions/battle/combo.txt";
        public const string PcScoresDataSource =
            "Client 6.0/settings/missions/battle/scores.txt";

        private readonly MissionBattleConfigService _config;

        public MissionBattleScoringService() : this(new MissionBattleConfigService()) { }

        public MissionBattleScoringService(MissionBattleConfigService config)
        {
            _config = config ?? new MissionBattleConfigService();
        }

        public int RankCount => _config.Count;
        public int ComboCellCount => _config.ComboCellCount;
        public int ScoreCellCount => _config.ScoreCellCount;
        public string ComboRowHeader => _config.ComboRowHeader;
        public string ScoreRowHeader => _config.ScoreRowHeader;
        public IReadOnlyList<string> Ranks => _config.ComboHeaders;

        public static MissionBattleScoringService LoadFromStreamingAssets(string subdir = null)
        {
            return new MissionBattleScoringService(
                MissionBattleConfigService.LoadFromStreamingAssets(subdir));
        }

        public MissionBattleScoringFact Lookup(string killerRank, string deadRank)
        {
            string killerSourceRow = ResolveKillerRow(killerRank);
            string deadSourceColumn = ResolveDeadColumn(deadRank);
            int killerIndex = GetPcTitleIndex(killerSourceRow);
            int deadIndex = GetPcTitleIndex(deadSourceColumn);
            return BuildFact(
                killerRank,
                deadRank,
                killerSourceRow,
                deadSourceColumn,
                killerIndex,
                deadIndex);
        }

        public MissionBattleScoringFact LookupByPcTitleIndex(int killerTitleIndex, int deadTitleIndex)
        {
            string killerRank = GetRankNameByPcTitleIndex(killerTitleIndex);
            string deadRank = GetRankNameByPcTitleIndex(deadTitleIndex);
            return BuildFact(
                killerRank,
                deadRank,
                killerRank,
                deadRank,
                killerTitleIndex,
                deadTitleIndex);
        }

        public string GetRankNameByPcTitleIndex(int titleIndex)
        {
            if (titleIndex <= 0 || titleIndex > Ranks.Count) return null;
            return Ranks[titleIndex - 1];
        }

        public int GetPcTitleIndex(string rank)
        {
            if (string.IsNullOrEmpty(rank)) return 0;
            for (int i = 0; i < Ranks.Count; i++)
            {
                if (string.Equals(Ranks[i], rank, StringComparison.Ordinal)) return i + 1;
            }
            return 0;
        }

        private MissionBattleScoringFact BuildFact(
            string killerRank,
            string deadRank,
            string killerSourceRow,
            string deadSourceColumn,
            int killerTitleIndex,
            int deadTitleIndex)
        {
            bool killerExists = !string.IsNullOrEmpty(killerSourceRow) && _config.Get(killerSourceRow) != null;
            bool deadExists = !string.IsNullOrEmpty(deadSourceColumn) && GetPcTitleIndex(deadSourceColumn) > 0;
            int combo = 0;
            int score = 0;
            if (killerExists && deadExists)
            {
                combo = _config.GetCombo(killerSourceRow, deadSourceColumn);
                score = _config.GetScore(killerSourceRow, deadSourceColumn);
            }

            return new MissionBattleScoringFact
            {
                KillerRank = killerRank,
                DeadRank = deadRank,
                KillerRankExists = killerExists,
                DeadRankExists = deadExists,
                ComboValue = combo,
                ScoreValue = score,
                ComboRowHeader = ComboRowHeader,
                ScoreRowHeader = ScoreRowHeader,
                KillerSourceRowName = killerExists ? killerSourceRow : null,
                DeadSourceColumnName = deadExists ? deadSourceColumn : null,
                PcKillerTitleIndex = killerTitleIndex,
                PcDeadTitleIndex = deadTitleIndex
            };
        }

        private string ResolveKillerRow(string rank)
        {
            if (string.IsNullOrEmpty(rank)) return null;
            return _config.Get(rank) != null ? rank : null;
        }

        private string ResolveDeadColumn(string rank)
        {
            if (string.IsNullOrEmpty(rank)) return null;
            return GetPcTitleIndex(rank) > 0 ? rank : null;
        }
    }
}
