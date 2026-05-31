using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Asset-link validation issue for a skill (M4.1 AC#2).</summary>
    public class SkillAssetIssue
    {
        public int skillId;
        public string kind;      // "icon" | "effect" | "missile"
        public string sourceKey;
        public string message;
    }

    /// <summary>
    /// M4.1 — Catalog of skills mapped from PC config (KSkill). Pure C# (no
    /// MonoBehaviour) so it is fully EditMode-testable. Generates SkillDefinition
    /// entries from converter output (AC#1), validates icon/effect/missile asset
    /// links through the asset registry (AC#2), and supports a selected-skill
    /// readout for the GM UI (AC#3).
    /// </summary>
    public class SkillCatalog
    {
        private readonly Dictionary<int, SkillDefinition> _byId = new();
        private readonly IAssetRegistry _assets;
        private int _selectedSkillId = -1;

        public SkillCatalog(IAssetRegistry assets = null)
        {
            _assets = assets;
        }

        public int Count => _byId.Count;
        public IReadOnlyCollection<SkillDefinition> All => _byId.Values;
        public int SelectedSkillId => _selectedSkillId;

        /// <summary>AC#1 — register a generated skill definition.</summary>
        public void Register(SkillDefinition skill)
        {
            if (skill == null) { SubsystemLog.Warn("Skill", "Register null skill"); return; }
            if (_byId.ContainsKey(skill.skillId))
                SubsystemLog.Warn("Skill", $"Duplicate skill id {skill.skillId} overwritten");
            _byId[skill.skillId] = skill;
        }

        public SkillDefinition Resolve(int skillId)
        {
            _byId.TryGetValue(skillId, out var s);
            return s;
        }

        public bool Contains(int skillId) => _byId.ContainsKey(skillId);

        /// <summary>
        /// AC#2 — validate every skill's icon/effect/missile references against the
        /// asset registry, stamping resolution flags and returning the issue list.
        /// </summary>
        public List<SkillAssetIssue> ValidateAssets()
        {
            var issues = new List<SkillAssetIssue>();
            foreach (var s in _byId.Values)
            {
                s.iconResolved = CheckAsset(s.skillId, "icon", s.iconSourceId, issues, required: true);
                s.effectResolved = CheckAsset(s.skillId, "effect", s.effectSourceId, issues, required: false);
                // Missile sprite only required when the skill actually fires a missile.
                CheckAsset(s.skillId, "missile", s.missileSpriteId, issues, required: s.HasMissile);
            }
            if (issues.Count > 0)
                SubsystemLog.Warn("Skill", $"{issues.Count} skill asset issue(s) across {_byId.Count} skills");
            return issues;
        }

        private bool CheckAsset(int skillId, string kind, SourceAssetId id, List<SkillAssetIssue> issues, bool required)
        {
            if (id == null)
            {
                if (required)
                    issues.Add(new SkillAssetIssue
                    {
                        skillId = skillId, kind = kind, sourceKey = "<none>",
                        message = $"Skill {skillId} missing required {kind} reference",
                    });
                return false;
            }
            var entry = _assets?.Resolve(id);
            bool ok = entry != null && entry.status == AssetStatus.Available;
            if (!ok && required)
                issues.Add(new SkillAssetIssue
                {
                    skillId = skillId, kind = kind, sourceKey = id.ToKey(),
                    message = $"Skill {skillId} {kind} asset unresolved ({id.ToKey()})",
                });
            return ok;
        }

        /// <summary>AC#3 — select a skill for the GM UI; returns the selected definition.</summary>
        public SkillDefinition Select(int skillId)
        {
            if (!_byId.ContainsKey(skillId))
            {
                SubsystemLog.Warn("Skill", $"Select unknown skill {skillId}");
                _selectedSkillId = -1;
                return null;
            }
            _selectedSkillId = skillId;
            return _byId[skillId];
        }

        /// <summary>AC#3 — details string for the selected skill (GM panel readout).</summary>
        public string SelectedDetails()
        {
            var s = Resolve(_selectedSkillId);
            if (s == null) return "No skill selected";
            return $"{s.DisplayName} (id={s.skillId})\n" +
                   $"reqLevel={s.reqLevel} cost={s.cost} range={s.attackRadius}\n" +
                   $"physical={s.isPhysical} missile={s.missileForm} levels={s.damageLevels.Count}";
        }
    }
}
