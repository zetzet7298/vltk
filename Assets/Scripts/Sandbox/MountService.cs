// -----------------------------------------------------------------------------
// VLTK Mobile — Mount Service (Ngựa cưỡi runtime)
// Wraps PcMountRegistry + HorseService để quản lý ngựa cưỡi + thể lực.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý ngựa cưỡi của player (state runtime: mounted/dismounted/stamina).
    /// </summary>
    public class MountService
    {
        public const string LogTag = "Mount";
        public const int MaxStamina = 100;
        public const int DefaultStamina = 100;

        private PcMountRegistry _registry = new();
        private readonly Dictionary<int, int> _activeMount = new();
        private readonly Dictionary<int, int> _stamina = new();

        public int Count => _registry?.Count ?? 0;

        public MountService() { }
        public MountService(PcMountRegistry registry) { _registry = registry ?? new PcMountRegistry(); }

        public void AttachRegistry(PcMountRegistry reg) { _registry = reg ?? new PcMountRegistry(); }

        /// <summary>Lấy thông tin ngựa đang cưỡi.</summary>
        public PcMountEntry GetActiveMount(int playerId)
        {
            if (_activeMount.TryGetValue(playerId, out int mid))
                return _registry.Get(mid);
            return null;
        }

        /// <summary>Thử cưỡi ngựa (validate id tồn tại).</summary>
        public bool TryMount(int playerId, int horseId)
        {
            if (playerId <= 0) return false;
            var m = _registry.Get(horseId);
            if (m == null) return false;
            _activeMount[playerId] = horseId;
            if (!_stamina.ContainsKey(playerId)) _stamina[playerId] = MaxStamina;
            return true;
        }

        /// <summary>Xuống ngựa.</summary>
        public bool TryDismount(int playerId)
        {
            bool had = _activeMount.Remove(playerId);
            return had;
        }

        /// <summary>Tốc độ di chuyển khi cưỡi (0 nếu không cưỡi).</summary>
        public int GetMountSpeed(int playerId)
        {
            if (!_activeMount.TryGetValue(playerId, out int mid)) return 0;
            var m = _registry.Get(mid);
            return m != null ? m.speed : 0;
        }

        /// <summary>Thể lực hiện tại.</summary>
        public int GetStamina(int playerId)
        {
            return _stamina.TryGetValue(playerId, out var v) ? v : 0;
        }

        /// <summary>Thể lực tối đa.</summary>
        public int GetMaxStamina(int playerId) => MaxStamina;

        /// <summary>Cho ăn để hồi thể lực (foodId ảnh hưởng lượng hồi).</summary>
        public bool TryFeed(int playerId, int foodId)
        {
            if (foodId <= 0) return false;
            if (!_stamina.ContainsKey(playerId)) _stamina[playerId] = MaxStamina;
            int current = _stamina[playerId];
            int gain = 20; // mỗi loại thức ăn +20 thể lực
            _stamina[playerId] = System.Math.Min(MaxStamina, current + gain);
            return true;
        }

        public PcMountEntry GetMount(int mountId) => _registry.Get(mountId);
        public IReadOnlyList<PcMountEntry> GetByLevel(int level) => _registry.GetByLevel(level);
        public IReadOnlyList<PcMountEntry> AllMounts => _registry.All;

        public static MountService LoadFromStreamingAssets()
        {
            var svc = new MountService();
            try
            {
                string dir = System.IO.Path.Combine(Application.streamingAssetsPath, "Reference/PcMount");
                var reg = PcMountParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            catch (System.Exception)
            {
                // Fallback: empty registry
            }
            return svc;
        }
    }
}
