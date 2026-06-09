// -----------------------------------------------------------------------------
// VLTK Mobile — Mission Battle Config Service
// Quản lý bảng combo + scores Tống Kim (Killer\Dead matrix).
// Data-only phase: no Lua mission semantics.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý cấu hình battle (combo + scores matrix).</summary>
    public class MissionBattleConfigService
    {
        public const string LogTag = "MissionBattleConfig";
        public const string DefaultStreamingDir = "Reference/PcBattlefield/MissionBattle";

        private readonly PcMissionBattleRegistry _reg;
        public int Count => _reg?.Count ?? 0;
        public int ComboCellCount => _reg?.ComboCellCount ?? 0;
        public int ScoreCellCount => _reg?.ScoreCellCount ?? 0;
        public IReadOnlyList<string> ComboHeaders => _reg?.ComboHeaders ?? System.Array.Empty<string>();
        public IReadOnlyList<string> ScoreHeaders => _reg?.ScoreHeaders ?? System.Array.Empty<string>();
        public string ComboRowHeader => _reg?.ComboRowHeader;
        public string ScoreRowHeader => _reg?.ScoreRowHeader;

        public MissionBattleConfigService() { _reg = new PcMissionBattleRegistry(); }
        public MissionBattleConfigService(PcMissionBattleRegistry reg) { _reg = reg ?? new PcMissionBattleRegistry(); }

        public static MissionBattleConfigService LoadFromStreamingAssets(string subdir = null)
        {
            string path = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            return new MissionBattleConfigService(PcMissionBattleParser.BuildRegistry(path));
        }

        public PcMissionBattleEntry Get(string rank) => _reg.Get(rank);
        public IEnumerable<PcMissionBattleEntry> All => _reg.All;

        /// <summary>Lấy combo value: comboMatrix[killer][dead] = int.</summary>
        public int GetCombo(string killerRank, string deadRank)
        {
            var entry = _reg.Get(killerRank);
            if (entry == null) return 0;
            return entry.ComboValues.TryGetValue(deadRank, out int v) ? v : 0;
        }

        /// <summary>Lấy score value: scoreMatrix[killer][dead] = int.</summary>
        public int GetScore(string killerRank, string deadRank)
        {
            var entry = _reg.Get(killerRank);
            if (entry == null) return 0;
            return entry.ScoreValues.TryGetValue(deadRank, out int v) ? v : 0;
        }
    }
}
