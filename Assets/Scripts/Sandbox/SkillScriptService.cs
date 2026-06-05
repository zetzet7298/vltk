// -----------------------------------------------------------------------------
// VLTK Mobile — ST-15.6 Skill Scripts runtime service
// Quản lý 2,486 metadata scripts cho kỹ năng.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class SkillScriptService
    {
        public const string LogTag = "SkillScript";
        public const string DefaultStreamingDir = "Reference/PcSkill";

        private PcSkillScriptRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public SkillScriptService() { }
        public SkillScriptService(PcSkillScriptRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcSkillScriptRegistry reg)
        {
            _registry = reg;
            if (_registry == null) SubsystemLog.Warn(LogTag, "Skill script registry rỗng");
        }

        public PcSkillScriptEntry GetScript(int id) => _registry != null ? _registry.Get(id) : null;
        public IReadOnlyList<PcSkillScriptEntry> GetBySkill(int skillId)
            => _registry != null ? _registry.GetBySkill(skillId) : System.Array.Empty<PcSkillScriptEntry>();
        public IReadOnlyList<PcSkillScriptEntry> GetByVersion(int version)
            => _registry != null ? _registry.GetByVersion(version) : System.Array.Empty<PcSkillScriptEntry>();

        public IReadOnlyList<string> GetScriptNamesForSkill(int skillId)
        {
            var list = new List<string>();
            foreach (var s in GetBySkill(skillId)) list.Add(s.functionName ?? string.Empty);
            return list;
        }

        public string GetFunctionName(int scriptId)
        {
            return GetScript(scriptId)?.functionName ?? string.Empty;
        }

        public static SkillScriptService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new SkillScriptService();
            if (Directory.Exists(dir))
            {
                svc.RegisterRegistry(PcSkillScriptParser.BuildRegistry(dir));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Không tìm thấy {dir}");
            }
            return svc;
        }
    }
}
