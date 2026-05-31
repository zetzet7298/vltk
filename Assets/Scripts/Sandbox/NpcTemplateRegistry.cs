using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M3.1 — Registry of NPC templates derived from PC source config. Pure C# (no
    /// MonoBehaviour) so it is fully EditMode-testable. Resolves templates by id for
    /// spawn markers (AC#2) and validates resource references through the asset
    /// registry, reporting missing sprite/script resources (AC#3).
    /// </summary>
    public class NpcTemplateRegistry
    {
        private readonly Dictionary<int, NpcTemplate> _byId = new();
        private readonly IAssetRegistry _assets;

        public NpcTemplateRegistry(IAssetRegistry assets = null)
        {
            _assets = assets;
        }

        public int Count => _byId.Count;
        public IReadOnlyCollection<NpcTemplate> All => _byId.Values;

        /// <summary>AC#1 — register a template from converter output.</summary>
        public void Register(NpcTemplate template)
        {
            if (template == null) { SubsystemLog.Warn("NpcTemplate", "Register null template"); return; }
            if (_byId.ContainsKey(template.templateId))
                SubsystemLog.Warn("NpcTemplate", $"Duplicate template id {template.templateId} overwritten");
            _byId[template.templateId] = template;
        }

        /// <summary>AC#2 — resolve a template for a spawn reference; null if unknown.</summary>
        public NpcTemplate Resolve(int templateId)
        {
            _byId.TryGetValue(templateId, out var t);
            return t;
        }

        public bool Contains(int templateId) => _byId.ContainsKey(templateId);

        /// <summary>
        /// AC#3 — validate every template's resource references against the asset
        /// registry, stamping resolution flags and returning a missing-resource report.
        /// A null asset registry means resources cannot be confirmed → all reported.
        /// </summary>
        public List<NpcResourceIssue> ValidateResources()
        {
            var issues = new List<NpcResourceIssue>();
            foreach (var t in _byId.Values)
            {
                // Sprite resource.
                if (t.spriteSourceId != null)
                {
                    var entry = _assets?.Resolve(t.spriteSourceId);
                    t.spriteResolved = entry != null && entry.status == AssetStatus.Available;
                    if (!t.spriteResolved)
                        issues.Add(new NpcResourceIssue
                        {
                            templateId = t.templateId,
                            kind = "sprite",
                            sourceKey = t.spriteSourceId.ToKey(),
                            message = $"Sprite resource missing for template {t.templateId} ({t.DisplayName})",
                        });
                }
                else
                {
                    t.spriteResolved = false;
                    issues.Add(new NpcResourceIssue
                    {
                        templateId = t.templateId,
                        kind = "sprite",
                        sourceKey = "<none>",
                        message = $"Template {t.templateId} ({t.DisplayName}) has no sprite reference",
                    });
                }

                // Script resource (resolved by path key only; presence in registry optional).
                if (!string.IsNullOrEmpty(t.scriptRef))
                {
                    var entry = _assets?.Resolve(t.scriptRef);
                    t.scriptResolved = entry != null && entry.status == AssetStatus.Available;
                    if (!t.scriptResolved)
                        issues.Add(new NpcResourceIssue
                        {
                            templateId = t.templateId,
                            kind = "script",
                            sourceKey = t.scriptRef,
                            message = $"Script resource missing for template {t.templateId} ({t.scriptRef})",
                        });
                }
                else
                {
                    // No script is allowed (not all NPCs are scripted); not an issue.
                    t.scriptResolved = false;
                }
            }

            if (issues.Count > 0)
                SubsystemLog.Warn("NpcTemplate", $"{issues.Count} NPC resource issue(s) across {_byId.Count} templates");
            return issues;
        }
    }
}
