// -----------------------------------------------------------------------------
// VLTK Mobile — BattleAwardService (Phần Thưởng Chiến Đấu runtime)
// Wraps PcBattleAwardRegistry. PC source: settings/battleaward.txt.
// Cấu hình phần thưởng theo battleType (0=Tống Kim, 1=Quốc Chiến, 2=Boss, 3=Võ Đài).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public enum BattleType
    {
        TongKim = 0,        // Tống Kim
        QuocChien = 1,      // Quốc Chiến
        Boss = 2,           // Boss
        VoDai = 3,          // Võ Đài (Arena)
    }

    /// <summary>
    /// Service quản lý phần thưởng chiến đấu theo xếp hạng.
    /// Lookup theo battleType + rank, hoặc awardId trực tiếp.
    /// </summary>
    public class BattleAwardService
    {
        public const string LogTag = "BattleAward";

        private PcBattleAwardRegistry _registry;
        private IBattleAwardHost _host;

        public int Count => _registry != null ? _registry.Count : 0;

        public event Action<int, int> OnAwardGranted; // (playerId, awardId)

        public BattleAwardService() : this(null, null) { }
        public BattleAwardService(PcBattleAwardRegistry registry) : this(registry, null) { }
        public BattleAwardService(PcBattleAwardRegistry registry, IBattleAwardHost host)
        {
            _registry = registry;
            _host = host;
        }

        public void AttachHost(IBattleAwardHost host) { _host = host; }

        public void RegisterRegistry(PcBattleAwardRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"Phần Thưởng Chiến Đấu loaded: {Count} hạng");
        }

        public PcBattleAwardEntry GetAward(int awardId)
            => _registry != null ? _registry.Get(awardId) : null;

        public IReadOnlyList<PcBattleAwardEntry> GetByBattleType(int type)
            => _registry != null
                ? _registry.GetByBattleType(type)
                : (IReadOnlyList<PcBattleAwardEntry>)System.Array.Empty<PcBattleAwardEntry>();

        public IReadOnlyList<PcBattleAwardEntry> GetByRank(int rank)
            => _registry != null
                ? _registry.GetByRank(rank)
                : (IReadOnlyList<PcBattleAwardEntry>)System.Array.Empty<PcBattleAwardEntry>();

        public IEnumerable<PcBattleAwardEntry> GetAllAwards()
            => _registry != null ? _registry.All : (IEnumerable<PcBattleAwardEntry>)System.Array.Empty<PcBattleAwardEntry>();

        /// <summary>Phát thưởng chiến đấu cho player theo awardId. Trả về false nếu không tìm thấy award.</summary>
        public bool GrantAward(int playerId, int awardId)
        {
            var entry = GetAward(awardId);
            if (entry == null) return false;
            OnAwardGranted?.Invoke(playerId, awardId);
            if (_host != null)
            {
                _host.OnAwardReceived(playerId, awardId, entry.battleType, entry.rank,
                    entry.rewardSilver, entry.rewardExp, entry.rewardItem);
                _host.PlayAwardSFX(playerId, entry.battleType, entry.rank);
                _host.ShowAwardNotice(playerId, entry.battleType, entry.rank, entry.rewardSilver, entry.rewardExp);
                if (entry.rewardSilver > 0) _host.GrantSilver(playerId, entry.rewardSilver);
                if (entry.rewardExp > 0) _host.GrantExp(playerId, entry.rewardExp);
                if (entry.rewardItem > 0) _host.GrantItem(playerId, entry.rewardItem, 1);
                // Top rank (rank 1) -> broadcast
                if (entry.rank == 1) _host.BroadcastTopRank(playerId, entry.battleType, entry.rank);
                _host.SaveAwardHistory(playerId, awardId, entry.battleType, entry.rank, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }
            SubsystemLog.Info(LogTag, $"Grant award {awardId} (battleType={entry.battleType}, rank={entry.rank}) cho player {playerId}");
            return true;
        }

        /// <summary>Tìm award theo battleType + rank và phát thưởng.</summary>
        public bool GrantAwardByRank(int playerId, int battleType, int rank)
        {
            var list = GetByBattleType(battleType);
            foreach (var e in list)
            {
                if (e.rank == rank) return GrantAward(playerId, e.awardId);
            }
            return false;
        }

        /// <summary>Load từ StreamingAssets/Reference (folder gốc nếu data nằm rải rác).</summary>
        public static BattleAwardService LoadFromStreamingAssets()
        {
            string root = Application.streamingAssetsPath;
            PcBattleAwardRegistry reg = null;
            // Tìm file battleaward.txt ở PcEvent (thư mục sự kiện)
            string[] candidates =
            {
                Path.Combine(root, "Reference/PcEvent"),
                Path.Combine(root, "Reference"),
            };
            foreach (var dir in candidates)
            {
                if (Directory.Exists(dir))
                {
                    reg = PcBattleAwardParser.BuildRegistry(dir);
                    if (reg != null && reg.Count > 0) break;
                }
            }
            return new BattleAwardService(reg);
        }
    }
}
