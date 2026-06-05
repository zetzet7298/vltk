// -----------------------------------------------------------------------------
// VLTK Mobile — SimCityPluginService (Auto-play Plugin runtime)
// Wraps PcSimCityPluginRegistry. PC source: settings/simcity_plugins.txt (14 plugin).
// Trigger: 0=on_idle, 1=on_level, 2=on_event. Cooldown giây giữa các lần kích hoạt.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public enum SimCityTrigger
    {
        OnIdle = 0,         // Khi người chơi rảnh
        OnLevel = 1,        // Khi đạt cấp mới
        OnEvent = 2,        // Khi trigger sự kiện (nhiệm vụ / phần thưởng)
    }

    /// <summary>
    /// Service quản lý plugin auto-play: danh sách plugin, kích hoạt theo trigger,
    /// cooldown giữa các lần. Cho phép mobile hook vào SimCity/AFK flow.
    /// </summary>
    public class SimCityPluginService
    {
        public const string LogTag = "SimCity";

        private PcSimCityPluginRegistry _registry;
        private readonly HashSet<int> _enabledPlugins = new();
        private readonly Dictionary<int, float> _lastFireTime = new();

        public int Count => _registry != null ? _registry.Count : 0;

        public SimCityPluginService() : this(null) { }

        public SimCityPluginService(PcSimCityPluginRegistry registry)
        {
            _registry = registry;
        }

        public void RegisterRegistry(PcSimCityPluginRegistry registry)
        {
            _registry = registry;
            _enabledPlugins.Clear();
            _lastFireTime.Clear();
            if (_registry != null)
            {
                foreach (var p in _registry.All)
                {
                    if (p != null && p.enabledByDefault) _enabledPlugins.Add(p.pluginId);
                }
            }
            SubsystemLog.Info(LogTag, $"SimCity Plugin loaded: {Count} plugin");
        }

        public PcSimCityPluginEntry GetPlugin(int pluginId)
            => _registry != null ? _registry.Get(pluginId) : null;

        public IReadOnlyList<PcSimCityPluginEntry> GetByTrigger(int triggerType)
            => _registry != null
                ? _registry.GetByTrigger(triggerType)
                : (IReadOnlyList<PcSimCityPluginEntry>)System.Array.Empty<PcSimCityPluginEntry>();

        public IEnumerable<PcSimCityPluginEntry> GetAllPlugins()
            => _registry != null ? _registry.All : (IEnumerable<PcSimCityPluginEntry>)System.Array.Empty<PcSimCityPluginEntry>();

        public void SetEnabled(int pluginId, bool enabled)
        {
            if (enabled) _enabledPlugins.Add(pluginId);
            else _enabledPlugins.Remove(pluginId);
        }

        public bool IsEnabled(int pluginId) => _enabledPlugins.Contains(pluginId);

        /// <summary>
        /// Thử kích hoạt plugin theo trigger. Trả về true nếu plugin fire được (enabled + cooldown ok).
        /// </summary>
        public bool TryFire(int pluginId, int triggerType, float nowSec)
        {
            var p = GetPlugin(pluginId);
            if (p == null || p.triggerType != triggerType) return false;
            if (!_enabledPlugins.Contains(pluginId)) return false;
            if (_lastFireTime.TryGetValue(pluginId, out var last) && (nowSec - last) < p.cooldownSec) return false;
            _lastFireTime[pluginId] = nowSec;
            SubsystemLog.Info(LogTag, $"Auto-play plugin #{pluginId} ({p.pluginNameRaw}) đã kích hoạt");
            return true;
        }

        /// <summary>Load từ StreamingAssets/Reference.</summary>
        public static SimCityPluginService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference");
            var reg = PcSimCityPluginParser.BuildRegistry(root);
            return new SimCityPluginService(reg);
        }
    }
}
