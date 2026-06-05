// -----------------------------------------------------------------------------
// VLTK Mobile — Battle Script Service (Kịch Bản Chiến Đấu runtime)
// Wraps PcBattleScriptRegistry. Lọc theo bản đồ, theo trigger type.
// Vietnamese: "Kịch Bản", "Chiến Đấu", "Bắt Đầu", "Kết Thúc", "Giết Boss", "Chết".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý kịch bản chiến đấu (183 scripts). PC source:
    /// settings/battlescripts.txt — kịch bản cho Tống Kim, Công Thành Chiến,
    /// Võ Lâm Liên Đấu, Phong Hỏa Liên Thành, Bách Bảo Lâu, ...
    /// </summary>
    public class BattleScriptService
    {
        public const string LogTag = "BattleScript";

        private PcBattleScriptRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public BattleScriptService() { }
        public BattleScriptService(PcBattleScriptRegistry registry)
        {
            _registry = registry ?? new PcBattleScriptRegistry();
        }

        public void AttachRegistry(PcBattleScriptRegistry registry)
        {
            _registry = registry ?? new PcBattleScriptRegistry();
        }

        public PcBattleScriptEntry GetScript(int scriptId)
            => _registry != null ? _registry.Get(scriptId) : null;

        public IEnumerable<PcBattleScriptEntry> GetAllScripts()
            => _registry != null ? _registry.All : (IEnumerable<PcBattleScriptEntry>)System.Array.Empty<PcBattleScriptEntry>();

        public IEnumerable<PcBattleScriptEntry> GetScriptsForMap(int mapId)
        {
            if (_registry == null) yield break;
            foreach (var e in _registry.GetByMap(mapId)) yield return e;
        }

        public IEnumerable<PcBattleScriptEntry> GetScriptsByTrigger(int triggerType)
        {
            if (_registry == null) yield break;
            foreach (var e in _registry.GetByTriggerType(triggerType)) yield return e;
        }

        public static BattleScriptService LoadFromStreamingAssets()
        {
            var svc = new BattleScriptService();
            string[] candidates = { "Reference/PcBattleScript", "Reference/PcEvent/Battle" };
            foreach (var sub in candidates)
            {
                string dir = Path.Combine(Application.streamingAssetsPath, sub);
                if (Directory.Exists(dir))
                {
                    var reg = PcBattleScriptParser.BuildRegistry(dir);
                    svc.AttachRegistry(reg);
                    SubsystemLog.Info(LogTag, $"BattleScriptService loaded {reg.Count} kịch bản từ {dir}");
                    return svc;
                }
            }
            SubsystemLog.Warn(LogTag, "BattleScriptService: không tìm thấy thư mục, khởi tạo registry rỗng");
            return svc;
        }
    }
}
