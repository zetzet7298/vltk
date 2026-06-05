// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/simcity_plugins.txt SimCity Auto-play plugin parser
// Source: simcity_plugins.txt (14 plugins).
//   PluginId  PluginName  TriggerType  EnabledByDefault  CooldownSec
// TriggerType: 0=on_idle, 1=on_level, 2=on_event
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSimCityPluginParser
    {
        public const int PluginIdCol = 0;
        public const int PluginNameCol = 1;
        public const int TriggerTypeCol = 2;
        public const int EnabledByDefaultCol = 3;
        public const int CooldownSecCol = 4;

        public static List<PcSimCityPluginEntry> ParseFile(string path)
        {
            var rows = new List<PcSimCityPluginEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                rows.Add(new PcSimCityPluginEntry
                {
                    pluginId = PcItemCommon.Int(cols, PluginIdCol),
                    pluginNameRaw = PcItemCommon.Str(cols, PluginNameCol),
                    triggerType = PcItemCommon.Int(cols, TriggerTypeCol),
                    enabledByDefault = PcItemCommon.Int(cols, EnabledByDefaultCol) > 0,
                    cooldownSec = PcItemCommon.Int(cols, CooldownSecCol),
                });
            }
            return rows;
        }

        public static PcSimCityPluginRegistry BuildRegistry(string dir)
        {
            var reg = new PcSimCityPluginRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "simcity_plugins.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcSimCityPluginEntry
    {
        public int pluginId;
        public string pluginNameRaw;
        public int triggerType;       // 0=on_idle, 1=on_level, 2=on_event
        public bool enabledByDefault;
        public int cooldownSec;
    }

    public sealed class PcSimCityPluginRegistry
    {
        private readonly Dictionary<int, PcSimCityPluginEntry> _byId = new();
        private readonly Dictionary<int, List<PcSimCityPluginEntry>> _byTrigger = new();
        public int Count => _byId.Count;
        public IEnumerable<PcSimCityPluginEntry> All => _byId.Values;
        public void Register(PcSimCityPluginEntry e)
        {
            if (e == null || e.pluginId <= 0) return;
            _byId[e.pluginId] = e;
            if (!_byTrigger.TryGetValue(e.triggerType, out var list))
            {
                list = new List<PcSimCityPluginEntry>();
                _byTrigger[e.triggerType] = list;
            }
            list.Add(e);
        }
        public PcSimCityPluginEntry Get(int pluginId)
            => _byId.TryGetValue(pluginId, out var v) ? v : null;
        public IReadOnlyList<PcSimCityPluginEntry> GetByTrigger(int triggerType)
            => _byTrigger.TryGetValue(triggerType, out var v)
                ? (IReadOnlyList<PcSimCityPluginEntry>)v
                : (IReadOnlyList<PcSimCityPluginEntry>)System.Array.Empty<PcSimCityPluginEntry>();
    }
}
