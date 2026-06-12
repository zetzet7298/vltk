// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.7 NPC Level Script Service
// PC source: npcscript/npc_level.txt — kịch bản NPC theo cấp (58 entries).
// Vietnamese: "Kịch Bản NPC", "Cấp Độ", "Đối Thoại", "Kích Hoạt".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public enum NpcScriptTrigger
    {
        Talk = 0,
        Kill = 1,
        Use = 2,
    }

    /// <summary>
    /// Service quản lý kịch bản NPC theo cấp. Tra cứu theo NPC + level nhân vật,
    /// theo NPC (toàn bộ), hoặc theo tên file script.
    /// </summary>
    public class NpcLevelScriptService
    {
        public const string DefaultStreamingDir = "Reference/PcNpcScript";
        public const string LogTag = "NpcLevelScript";

        private readonly PcNpcLevelScriptRegistry _registry;

        public event Action<PcNpcLevelScriptEntry> OnScriptTriggered;

        public int Count => _registry?.Count ?? 0;

        public NpcLevelScriptService() : this(null) { }

        public NpcLevelScriptService(PcNpcLevelScriptRegistry registry)
        {
            _registry = registry ?? new PcNpcLevelScriptRegistry();
        }

        /// <summary>Tra cứu script theo NPC + cấp nhân vật.</summary>
        public PcNpcLevelScriptEntry GetScriptForNpc(int npcTemplateId, int level)
            => _registry?.Get(npcTemplateId, level);

        /// <summary>Toàn bộ script của một NPC (mọi cấp).</summary>
        public IReadOnlyList<PcNpcLevelScriptEntry> GetScriptsForNpc(int npcTemplateId)
            => _registry?.GetByNpc(npcTemplateId)
                ?? (IReadOnlyList<PcNpcLevelScriptEntry>)Array.Empty<PcNpcLevelScriptEntry>();

        /// <summary>Tra cứu script theo tên file .lua.</summary>
        public IReadOnlyList<PcNpcLevelScriptEntry> GetScriptsByFile(string fileName)
            => _registry?.GetByScriptFile(fileName)
                ?? (IReadOnlyList<PcNpcLevelScriptEntry>)Array.Empty<PcNpcLevelScriptEntry>();

        /// <summary>Kích hoạt thủ công (test/GM). Trả về entry nếu tìm thấy.</summary>
        public PcNpcLevelScriptEntry TriggerScript(int npcTemplateId, int level)
        {
            var entry = GetScriptForNpc(npcTemplateId, level);
            if (entry == null)
            {
                SubsystemLog.Info(LogTag, $"Không tìm thấy kịch bản NPC {npcTemplateId} cấp {level}");
                return null;
            }
            SubsystemLog.Info(LogTag,
                $"Kích hoạt kịch bản NPC {npcTemplateId} cấp {level}: {entry.scriptFile} (trigger {entry.triggerType})");
            OnScriptTriggered?.Invoke(entry);
            return entry;
        }

        /// <summary>Load từ StreamingAssets.</summary>
        public static NpcLevelScriptService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcNpcLevelScriptParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"NpcLevelScriptService loaded {reg.Count} kịch bản NPC từ {dir}");
            return new NpcLevelScriptService(reg);
        }
    }
}
