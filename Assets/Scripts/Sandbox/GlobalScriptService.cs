// -----------------------------------------------------------------------------
// VLTK Mobile — ST-15.4 Global Scripts runtime service
// Quản lý 579 metadata scripts toàn cục (login, logout, heartbeat, GM...).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class GlobalScriptService
    {
        public const string LogTag = "GlobalScript";
        public const string DefaultStreamingDir = "Reference/PcGlobal";

        private PcGlobalScriptRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public GlobalScriptService() { }
        public GlobalScriptService(PcGlobalScriptRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcGlobalScriptRegistry reg)
        {
            _registry = reg;
            if (_registry == null) SubsystemLog.Warn(LogTag, "Global script registry rỗng");
        }

        public static string GetTriggerName(int trigger)
        {
            return trigger switch
            {
                0 => "Đăng nhập",
                1 => "Đăng xuất",
                2 => "Heartbeat",
                3 => "Lệnh GM",
                4 => "Khởi động server",
                5 => "Dừng server",
                _ => $"Khác ({trigger})",
            };
        }

        public PcGlobalScriptEntry GetScript(int id) => _registry != null ? _registry.Get(id) : null;
        public IReadOnlyList<PcGlobalScriptEntry> GetByTrigger(int trigger)
            => _registry != null ? _registry.GetByTrigger(trigger) : System.Array.Empty<PcGlobalScriptEntry>();
        public IReadOnlyList<PcGlobalScriptEntry> GetByFile(string fileName)
            => _registry != null ? _registry.GetByFile(fileName) : System.Array.Empty<PcGlobalScriptEntry>();

        public string GetFunctionName(int scriptId)
        {
            return GetScript(scriptId)?.functionName ?? string.Empty;
        }

        public static GlobalScriptService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new GlobalScriptService();
            if (Directory.Exists(dir))
            {
                svc.RegisterRegistry(PcGlobalScriptParser.BuildRegistry(dir));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Không tìm thấy {dir}");
            }
            return svc;
        }
    }
}
