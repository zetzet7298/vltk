// -----------------------------------------------------------------------------
// VLTK Mobile — ST-08.12 Công Thành Chiến Service (Bang Chiến runtime)
// Wraps PcBangChienRegistry. PC source: settings/battle/bangchien.txt.
// Vietnamese: "Công Thành", "Trấn Thủ", "Tấn Công", "Thu Nhập", "Phần Thưởng".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class BangChienService
    {
        public const string LogTag = "BangChien";
        public const string DefaultStreamingDir = "Reference/PcCity";

        private PcBangChienRegistry _registry;
        private int _challengerBangId;
        private int _defenderBangId;
        private int _challengerScore;
        private int _defenderScore;
        private bool _isActive;

        public event Action OnCityLoaded;
        public event Action<int, int, int> OnBangChienEnded;

        public int Count => _registry != null ? _registry.Count : 0;
        public bool IsActive => _isActive;

        public BangChienService() { }
        public BangChienService(PcBangChienRegistry registry) { AttachRegistry(registry); }

        public void AttachRegistry(PcBangChienRegistry registry)
        {
            _registry = registry ?? new PcBangChienRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} thành Công Thành Chiến");
            OnCityLoaded?.Invoke();
        }

        public PcBangChienEntry GetCity(int id)
            => _registry != null ? _registry.Get(id) : null;

        public IReadOnlyList<PcBangChienEntry> GetByMap(int mapId)
            => _registry != null ? _registry.GetByMap(mapId) : Array.Empty<PcBangChienEntry>();

        public IReadOnlyList<PcBangChienEntry> GetByTong(int tongId)
            => _registry != null ? _registry.GetByTong(tongId) : Array.Empty<PcBangChienEntry>();

        /// <summary>Bắt đầu Bang Chiến runtime.</summary>
        public void StartBangChien(int challengerBangId, int defenderBangId)
        {
            _challengerBangId = challengerBangId;
            _defenderBangId = defenderBangId;
            _challengerScore = 0;
            _defenderScore = 0;
            _isActive = true;
            SubsystemLog.Info(LogTag, $"Bang Chiến bắt đầu: Bang {challengerBangId} vs Bang {defenderBangId}");
        }

        public void RecordKill(bool isChallengerKill)
        {
            if (!_isActive) return;
            if (isChallengerKill) _challengerScore++;
            else _defenderScore++;
        }

        public int EndBangChien()
        {
            _isActive = false;
            int winner = _challengerScore > _defenderScore ? _challengerBangId :
                         _defenderScore > _challengerScore ? _defenderBangId : 0;
            OnBangChienEnded?.Invoke(winner, _challengerScore, _defenderScore);
            SubsystemLog.Info(LogTag, $"Bang Chiến kết thúc: {_challengerScore}-{_defenderScore}. Winner: Bang {winner}");
            return winner;
        }

        /// <summary>Lọc thành mở chiến theo ngày trong tuần (0=CN..6=T7).</summary>
        public IReadOnlyList<PcBangChienEntry> GetOpenDay(int day)
        {
            if (_registry == null) return Array.Empty<PcBangChienEntry>();
            var list = new List<PcBangChienEntry>();
            foreach (var e in _registry.All)
                if (((e.openDay >> day) & 1) == 1) list.Add(e);
            return list;
        }

        /// <summary>Tính thu nhập bạc cho 1 thành theo số giờ sở hữu.</summary>
        public long ComputeIncome(int cityId, int hours)
        {
            var c = GetCity(cityId);
            if (c == null) return 0L;
            return (long)c.income * Math.Max(0, hours);
        }

        public static BangChienService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new BangChienService();
            if (Directory.Exists(dir))
            {
                var reg = PcBangChienParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Bang Chien: directory không tồn tại {dir}");
                svc.OnCityLoaded?.Invoke();
            }
            return svc;
        }
    }
}
