// -----------------------------------------------------------------------------
// VLTK Mobile — ClientSkillScriptService (Kịch Bản Client-Side Kỹ Năng runtime)
// Wraps PcClientSkillScriptRegistry. PC source: settings/clientskillscripts.txt (722 script).
// ClientEvent: 0=pre_cast, 1=on_hit, 2=on_crit, 3=on_kill.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public enum SkillClientEvent
    {
        PreCast = 0,        // Trước khi thi triển
        OnHit = 1,          // Khi trúng mục tiêu
        OnCrit = 2,         // Khi chí mạng
        OnKill = 3,         // Khi giết mục tiêu
    }

    /// <summary>
    /// Service quản lý kịch bản client-side cho kỹ năng: pre_cast SFX, on_hit
    /// effect, on_crit flash, on_kill reward animation. Mobile runtime tra
    /// cứu nhanh theo skillId + event.
    /// </summary>
    public class ClientSkillScriptService
    {
        public const string LogTag = "ClientSkill";

        private PcClientSkillScriptRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public ClientSkillScriptService() : this(null) { }

        public ClientSkillScriptService(PcClientSkillScriptRegistry registry)
        {
            _registry = registry;
        }

        public void RegisterRegistry(PcClientSkillScriptRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"Client Skill Script loaded: {Count} kịch bản");
        }

        public PcClientSkillScriptEntry GetScript(int scriptId)
            => _registry != null ? _registry.Get(scriptId) : null;

        public IReadOnlyList<PcClientSkillScriptEntry> GetBySkill(int skillId)
            => _registry != null
                ? _registry.GetBySkill(skillId)
                : (IReadOnlyList<PcClientSkillScriptEntry>)System.Array.Empty<PcClientSkillScriptEntry>();

        public IReadOnlyList<PcClientSkillScriptEntry> GetByEvent(int evt)
            => _registry != null
                ? _registry.GetByEvent(evt)
                : (IReadOnlyList<PcClientSkillScriptEntry>)System.Array.Empty<PcClientSkillScriptEntry>();

        public IEnumerable<PcClientSkillScriptEntry> GetAllScripts()
            => _registry != null ? _registry.All : (IEnumerable<PcClientSkillScriptEntry>)System.Array.Empty<PcClientSkillScriptEntry>();

        /// <summary>
        /// Tìm script khớp với cả skillId lẫn event (nếu có nhiều, trả về cái đầu).
        /// Trả về null nếu không tìm thấy.
        /// </summary>
        public PcClientSkillScriptEntry FindMatch(int skillId, int evt)
        {
            if (_registry == null) return null;
            foreach (var s in _registry.GetBySkill(skillId))
            {
                if (s != null && s.clientEvent == evt) return s;
            }
            return null;
        }

        /// <summary>Load từ StreamingAssets/Reference/PcSkill.</summary>
        public static ClientSkillScriptService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcSkill");
            var reg = PcClientSkillScriptParser.BuildRegistry(dir);
            return new ClientSkillScriptService(reg);
        }
    }
}
