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

        /// <summary>Event kích hoạt khi hoàn thành một mục mạo hiểm.</summary>
        public event Action<int> OnAdventureCompleted;

        public int Count => _registry.Count;
        public int CompletedCount => _completedAdventures.Count;
        public float CompletionRatio => Count == 0 ? 0f : (float)CompletedCount / Count;

        public AdventureService(PcAdventureRegistry registry)
        {
            _registry = registry ?? new PcAdventureRegistry();
        }

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
                return true;
            }
            return false;
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
