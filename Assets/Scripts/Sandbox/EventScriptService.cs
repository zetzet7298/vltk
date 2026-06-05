// -----------------------------------------------------------------------------
// VLTK Mobile — ST-15.7 Event Scripts runtime service
// Quản lý 455 metadata scripts cho sự kiện.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class EventScriptService
    {
        public const string LogTag = "EventScript";
        public const string DefaultStreamingDir = "Reference/PcEvent";

        private PcEventScriptRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public EventScriptService() { }
        public EventScriptService(PcEventScriptRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcEventScriptRegistry reg)
        {
            _registry = reg;
            if (_registry == null) SubsystemLog.Warn(LogTag, "Event script registry rỗng");
        }

        public PcEventScriptEntry GetScript(int id) => _registry != null ? _registry.Get(id) : null;
        public IReadOnlyList<PcEventScriptEntry> GetByEvent(int eventId)
            => _registry != null ? _registry.GetByEvent(eventId) : System.Array.Empty<PcEventScriptEntry>();
        public IReadOnlyList<PcEventScriptEntry> GetByTrigger(int trigger)
            => _registry != null ? _registry.GetByTrigger(trigger) : System.Array.Empty<PcEventScriptEntry>();

        public string GetFunctionName(int scriptId)
        {
            return GetScript(scriptId)?.functionName ?? string.Empty;
        }

        public IReadOnlyList<string> GetEventScriptNames(int eventId)
        {
            var list = new List<string>();
            foreach (var s in GetByEvent(eventId)) list.Add(s.functionName ?? string.Empty);
            return list;
        }

        public static EventScriptService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new EventScriptService();
            if (Directory.Exists(dir))
            {
                svc.RegisterRegistry(PcEventScriptParser.BuildRegistry(dir));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Không tìm thấy {dir}");
            }
            return svc;
        }
    }
}
