// -----------------------------------------------------------------------------
// VLTK Mobile — ST-14.x GBK Map Script Service
// Quản lý script theo map GBK. Trigger: 0=enter, 1=leave, 2=tick, 3=event, 4=npc.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý GBK map script (script theo bản đồ).</summary>
    public class GbkMapScriptService
    {
        public const string LogTag = "GbkMapScript";
        public const string DefaultStreamingDir = "Reference/PcGbk";

        private PcGbkMapScriptRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public GbkMapScriptService() { }
        public GbkMapScriptService(PcGbkMapScriptRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcGbkMapScriptRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "GBK map script registry rỗng");
        }

        public static GbkMapScriptService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new GbkMapScriptService();
            if (Directory.Exists(dir))
            {
                var reg = PcGbkMapScriptParser.BuildRegistry(dir);
                svc.RegisterRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"GBK map script directory không tồn tại {dir}");
            }
            return svc;
        }

        public PcGbkMapScriptEntry GetScript(int id) => _reg != null ? _reg.Get(id) : null;
        public IReadOnlyList<PcGbkMapScriptEntry> GetByArea(int areaId)
            => _reg != null ? _reg.GetByArea(areaId) : System.Array.Empty<PcGbkMapScriptEntry>();
        public IReadOnlyList<PcGbkMapScriptEntry> GetByMap(int mapId)
            => _reg != null ? _reg.GetByMap(mapId) : System.Array.Empty<PcGbkMapScriptEntry>();
        public IReadOnlyList<PcGbkMapScriptEntry> GetByTrigger(int triggerType)
            => _reg != null ? _reg.GetByTrigger(triggerType) : System.Array.Empty<PcGbkMapScriptEntry>();
        public IReadOnlyList<PcGbkMapScriptEntry> GetScriptsForMap(int mapId) => GetByMap(mapId);

        public string GetFunctionName(int scriptId)
        {
            var e = GetScript(scriptId);
            return e != null ? e.functionName : null;
        }
    }
}
