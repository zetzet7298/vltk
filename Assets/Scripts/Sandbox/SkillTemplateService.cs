// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX Skill Template Service (Template Skill runtime)
// Wraps PcSkillTemplateRegistry. PC source: settings/skill_template.txt (219).
// Cấu hình hiệu ứng đạn (buff, debuff, dot, hot). Mỗi template gắn với 1 missle.
// Vietnamese: "Template Skill", "Hiệu Ứng Đạn", "Buff", "Debuff".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý template hiệu ứng skill.
    /// PC source: settings/skill_template.txt + missletemplate.txt.
    /// </summary>
    public class SkillTemplateService
    {
        public const string LogTag = "SkillTemplate";

        private PcSkillTemplateRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public SkillTemplateService() : this(null) { }

        public SkillTemplateService(PcSkillTemplateRegistry registry)
        {
            RegisterRegistry(registry);
        }

        public void RegisterRegistry(PcSkillTemplateRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"Template Skill loaded: {Count} template");
        }

        public PcSkillTemplateEntry GetTemplate(int id)
            => _registry != null ? _registry.Get(id) : null;

        public IEnumerable<PcSkillTemplateEntry> GetAllTemplates()
            => _registry != null ? _registry.GetAll() : (IEnumerable<PcSkillTemplateEntry>)Array.Empty<PcSkillTemplateEntry>();

        /// <summary>Tìm template theo missleId (1 missle có thể có nhiều template).</summary>
        public IEnumerable<PcSkillTemplateEntry> GetTemplatesForMissle(int missleId)
        {
            if (_registry == null) yield break;
            foreach (var t in _registry.GetAll())
                if (t != null && t.missleId == missleId) yield return t;
        }

        public static SkillTemplateService LoadFromStreamingAssets(string subdir = "Reference/PcAttrib")
        {
            var svc = new SkillTemplateService();
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            if (Directory.Exists(dir))
            {
                var reg = PcSkillTemplateParser.BuildRegistry(dir);
                svc.RegisterRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"SkillTemplateService: directory không tồn tại {dir}");
            }
            return svc;
        }
    }
}
