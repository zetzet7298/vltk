// -----------------------------------------------------------------------------
// VLTK Mobile — ST-06.x Adventure Service (Mạo Hiểm)
// Quản lý mục mạo hiểm: 1,037 điểm dã tẩu/phiêu lưu trên bản đồ.
// PC source: settings/adventure.txt (MapId, PosX, PosY).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý mục mạo hiểm & phiêu lưu.</summary>
    public class AdventureService
    {
        public const string LogTag = "Adventure";

        private readonly PcAdventureRegistry _registry;
        private readonly HashSet<int> _completedAdventures = new();
        private IAdventureHost _host;
        private int _playerId = 0;

        /// <summary>Event kích hoạt khi hoàn thành một mục mạo hiểm.</summary>
        public event Action<int> OnAdventureCompleted;
        /// <summary>Event kích hoạt khi player hiện tại hoàn thành tất cả mục.</summary>
        public event Action OnAllCompleted;

        public int Count => _registry.Count;
        public int CompletedCount => _completedAdventures.Count;
        public float CompletionRatio => Count == 0 ? 0f : (float)CompletedCount / Count;

        public int PlayerId { get => _playerId; set => _playerId = value; }

        public AdventureService() : this(null, null) { }
        public AdventureService(PcAdventureRegistry registry) : this(registry, null) { }
        public AdventureService(PcAdventureRegistry registry, IAdventureHost host)
        {
            _registry = registry ?? new PcAdventureRegistry();
            _host = host;
        }

        public void AttachHost(IAdventureHost host) { _host = host; }

        /// <summary>Tra cứu mục mạo hiểm theo advId.</summary>
        public PcAdventureEntry GetAdventure(int advId) => _registry.Resolve(advId);

        /// <summary>Toàn bộ mục mạo hiểm.</summary>
        public IEnumerable<PcAdventureEntry> GetAllAdventures() => _registry.All;

        /// <summary>Lọc mục mạo hiểm thuộc một bản đồ cụ thể.</summary>
        public IEnumerable<PcAdventureEntry> GetAdventuresForMap(int mapId)
        {
            foreach (var adv in _registry.All)
                if (adv != null && adv.mapId == mapId) yield return adv;
        }

        /// <summary>Đánh dấu mục mạo hiểm hoàn thành. Trả về true nếu mới hoàn thành.</summary>
        public bool MarkCompleted(int advId)
        {
            if (_completedAdventures.Add(advId))
            {
                SubsystemLog.Info(LogTag, $"Hoàn thành mạo hiểm #{advId}");
                OnAdventureCompleted?.Invoke(advId);
                if (_host != null)
                {
                    var entry = _registry.Resolve(advId);
                    string advName = entry?.nameRaw ?? $"#{advId}";
                    int mapId = entry?.mapId ?? 0;
                    _host.ShowMapPin(advId, mapId, true);
                    _host.OnAdventureCompleted(_playerId, advId, advName, mapId);
                    _host.LogAdventureEvent(_playerId, advId, $"Hoàn thành mục mạo hiểm {advName} trên bản đồ {mapId}");
                    _host.UpdateProgress(_playerId, CompletedCount, Count, CompletionRatio);
                    _host.SaveAdventureProgress(_playerId, advId, true);
                    // Thưởng vật phẩm nếu có
                    if (entry?.extra0 != null && int.TryParse(entry.extra0, out int rewardId) && rewardId > 0)
                    {
                        int count = entry?.extra1 != null && int.TryParse(entry.extra1, out int c) ? c : 1;
                        _host.GrantAdventureReward(_playerId, advId, rewardId, count);
                    }
                }
                // 100% hoàn thành (fire bên ngoài host block để không phụ thuộc host)
                if (CompletedCount >= Count)
                {
                    OnAllCompleted?.Invoke();
                    if (_host != null) _host.OnAllAdventuresCompleted(_playerId, Count);
                }
                return true;
            }
            return false;
        }

        /// <summary>Hoàn thành mục mạo hiểm với playerId cụ thể (PC AdventureCompleteEvent).</summary>
        public bool MarkCompletedFor(int playerId, int advId)
        {
            int prev = _playerId;
            _playerId = playerId;
            bool result = MarkCompleted(advId);
            _playerId = prev;
            return result;
        }

        /// <summary>Đếm số mục mạo hiểm trên 1 bản đồ.</summary>
        public int GetMapAdventureCount(int mapId)
        {
            int n = 0;
            foreach (var adv in _registry.All)
                if (adv != null && adv.mapId == mapId) n++;
            return n;
        }

        /// <summary>Mục mạo hiểm đã hoàn thành chưa.</summary>
        public bool IsCompleted(int advId) => _completedAdventures.Contains(advId);

        /// <summary>Reset tiến độ (đổi nhân vật hoặc bắt đầu mới).</summary>
        public void Clear() => _completedAdventures.Clear();

        /// <summary>Load từ StreamingAssets/Reference/PcAdventure.</summary>
        public static AdventureService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcAdventure");
            var reg = PcAdventureParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} mục mạo hiểm từ {dir}");
            return new AdventureService(reg);
        }
    }
}
