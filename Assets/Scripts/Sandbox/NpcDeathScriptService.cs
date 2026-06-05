// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.9 NPC Death Script Service
// PC source: npcscript/death.txt — kịch bản khi NPC chết (drop đồ + chạy script).
// Vietnamese: "Kịch Bản Chết NPC", "Rơi Đồ", "Phần Thưởng".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý kịch bản khi NPC chết. Khi nhân vật giết NPC, service trả
    /// về drop item id/count + script cần chạy. PC có thể có nhiều NPC dùng cùng script.
    /// </summary>
    public class NpcDeathScriptService
    {
        public const string DefaultStreamingDir = "Reference/PcNpcScript";
        public const string LogTag = "NpcDeathScript";

        private readonly PcNpcDeathScriptRegistry _registry;

        public event Action<PcNpcDeathScriptEntry> OnNpcDeathTriggered;

        public int Count => _registry?.Count ?? 0;

        public NpcDeathScriptService(PcNpcDeathScriptRegistry registry)
        {
            _registry = registry ?? new PcNpcDeathScriptRegistry();
        }

        /// <summary>Tra cứu kịch bản khi NPC chết theo template id.</summary>
        public PcNpcDeathScriptEntry GetDeathScript(int npcTemplateId)
            => _registry?.Get(npcTemplateId);

        /// <summary>Toàn bộ kịch bản NPC death đã đăng ký.</summary>
        public IReadOnlyList<PcNpcDeathScriptEntry> GetAllDeathScripts()
            => _registry?.GetAll()
                ?? (IReadOnlyList<PcNpcDeathScriptEntry>)Array.Empty<PcNpcDeathScriptEntry>();

        /// <summary>Kích hoạt khi NPC chết (drop đồ + chạy script). Trả về entry.</summary>
        public PcNpcDeathScriptEntry TriggerDeath(int npcTemplateId)
        {
            var entry = GetDeathScript(npcTemplateId);
            if (entry == null)
            {
                SubsystemLog.Info(LogTag, $"NPC {npcTemplateId} chết nhưng không có kịch bản");
                return null;
            }
            string dropInfo = entry.dropItemId > 0
                ? $"rơi item {entry.dropItemId} x{entry.dropCount}"
                : "không rơi đồ";
            SubsystemLog.Info(LogTag,
                $"NPC {npcTemplateId} chết: {dropInfo}, chạy {entry.scriptFile}");
            OnNpcDeathTriggered?.Invoke(entry);
            return entry;
        }

        /// <summary>Load từ StreamingAssets.</summary>
        public static NpcDeathScriptService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcNpcDeathScriptParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"NpcDeathScriptService loaded {reg.Count} kịch bản NPC death từ {dir}");
            return new NpcDeathScriptService(reg);
        }
    }
}
