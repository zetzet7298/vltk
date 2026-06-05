// -----------------------------------------------------------------------------
// VLTK Mobile — ST-9.10 Guild City War (Bang Hội Công Thành Chiến)
// Quản lý các trận công thành giữa các bang hội.
// PC source: settings/tong/guildcitywar.txt + 9.10 port spec.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Trạng thái trận công thành bang hội.</summary>
    public enum GuildCityWarStatus
    {
        Scheduled = 0,  // Đã lên lịch
        Active = 1,     // Đang diễn ra
        Finished = 2,   // Đã kết thúc
        Cancelled = 3,  // Đã hủy
    }

    /// <summary>Mục trận công thành bang hội.</summary>
    [System.Serializable]
    public struct GuildCityWarEntry
    {
        public int warId;                  // ID trận
        public int cityId;                 // ID thành
        public int attackerTongId;         // ID bang tấn công
        public int defenderTongId;         // ID bang phòng thủ
        public long startTimeUnix;         // Thời gian bắt đầu (Unix)
        public long endTimeUnix;           // Thời gian kết thúc (Unix)
        public int scoreAttacker;          // Điểm tấn công
        public int scoreDefender;          // Điểm phòng thủ
        public int status;                 // Trạng thái (0=scheduled, 1=active, 2=finished, 3=cancelled)
    }

    /// <summary>Service quản lý Bang Hội Công Thành Chiến.</summary>
    public class GuildCityWarService
    {
        public const string LogTag = "GuildCityWar";
        public const string DefaultStreamingDir = "Reference/PcTong";

        private readonly Dictionary<int, GuildCityWarEntry> _wars = new();
        private int _nextWarId = 1;
        private CityWarService _cityWarService;

        public int Count => _wars.Count;

        public GuildCityWarService() { }
        public GuildCityWarService(CityWarService cityWarService) { _cityWarService = cityWarService; }

        public void AttachCityWarService(CityWarService cityWarService)
        {
            _cityWarService = cityWarService;
        }

        public static string GetStatusName(int status)
        {
            return status switch
            {
                0 => "Đã lên lịch",
                1 => "Đang diễn ra",
                2 => "Đã kết thúc",
                3 => "Đã hủy",
                _ => $"Không rõ ({status})",
            };
        }

        public int ScheduleWar(int cityId, int attackerTongId, int defenderTongId, long startTimeUnix)
        {
            int warId = _nextWarId++;
            _wars[warId] = new GuildCityWarEntry
            {
                warId = warId,
                cityId = cityId,
                attackerTongId = attackerTongId,
                defenderTongId = defenderTongId,
                startTimeUnix = startTimeUnix,
                endTimeUnix = 0,
                scoreAttacker = 0,
                scoreDefender = 0,
                status = (int)GuildCityWarStatus.Scheduled,
            };
            SubsystemLog.Info(LogTag, $"Lên lịch công thành #{warId} cho thành {cityId} (tấn công bang {attackerTongId} vs phòng thủ bang {defenderTongId})");
            return warId;
        }

        public bool StartWar(int warId)
        {
            if (!_wars.TryGetValue(warId, out var e)) return false;
            if (e.status != (int)GuildCityWarStatus.Scheduled) return false;
            e.status = (int)GuildCityWarStatus.Active;
            _wars[warId] = e;
            SubsystemLog.Info(LogTag, $"Bắt đầu công thành #{warId}");
            return true;
        }

        public int FinishWar(int warId, int finalScoreAttacker, int finalScoreDefender)
        {
            if (!_wars.TryGetValue(warId, out var e)) return -1;
            if (e.status != (int)GuildCityWarStatus.Active) return -1;
            e.scoreAttacker = finalScoreAttacker;
            e.scoreDefender = finalScoreDefender;
            e.status = (int)GuildCityWarStatus.Finished;
            e.endTimeUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _wars[warId] = e;
            int winningTong = 0;
            if (finalScoreAttacker > finalScoreDefender) winningTong = e.attackerTongId;
            else if (finalScoreDefender > finalScoreAttacker) winningTong = e.defenderTongId;
            SubsystemLog.Info(LogTag, $"Kết thúc công thành #{warId}, thắng bang {winningTong}");
            return winningTong;
        }

        public bool CancelWar(int warId)
        {
            if (!_wars.TryGetValue(warId, out var e)) return false;
            if (e.status == (int)GuildCityWarStatus.Finished) return false;
            e.status = (int)GuildCityWarStatus.Cancelled;
            e.endTimeUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _wars[warId] = e;
            SubsystemLog.Info(LogTag, $"Hủy công thành #{warId}");
            return true;
        }

        public GuildCityWarEntry GetWar(int warId)
        {
            return _wars.TryGetValue(warId, out var e) ? e : default;
        }

        public IReadOnlyList<GuildCityWarEntry> GetActiveWars()
        {
            var list = new List<GuildCityWarEntry>();
            foreach (var e in _wars.Values)
                if (e.status == (int)GuildCityWarStatus.Active) list.Add(e);
            return list;
        }

        public IReadOnlyList<GuildCityWarEntry> GetWarsForTong(int tongId)
        {
            var list = new List<GuildCityWarEntry>();
            foreach (var e in _wars.Values)
                if (e.attackerTongId == tongId || e.defenderTongId == tongId) list.Add(e);
            return list;
        }

        public IReadOnlyList<GuildCityWarEntry> GetWarsForCity(int cityId)
        {
            var list = new List<GuildCityWarEntry>();
            foreach (var e in _wars.Values)
                if (e.cityId == cityId) list.Add(e);
            return list;
        }

        public static GuildCityWarService LoadFromStreamingAssets()
        {
            var svc = new GuildCityWarService();
            string root = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            if (Directory.Exists(root))
            {
                SubsystemLog.Info(LogTag, $"Đã tải danh sách công thành từ {root} (rỗng)");
            }
            return svc;
        }
    }
}
