using System;
using System.Collections.Generic;

namespace VLTK.Model
{
    /// <summary>
    /// M4.1 — Skill definition mapped from PC source (KSkill, KSkills.h /
    /// KSkills.cpp). Field names mirror the PC config so parity is traceable:
    /// m_nId, m_szName, m_usReqLevel, m_szSkillIcon, m_szPreCastEffectFile, m_nCost,
    /// m_nAttackRadius (range), m_bIsPhysical, m_eMisslesForm.
    /// </summary>
    [Serializable]
    public enum SkillMissileForm
    {
        None = 0,        // instant / melee (no missile)
        Single = 1,      // single projectile toward target
        Fan = 2,         // fan / multi-shot
        Surround = 3,    // surrounding burst
        Chain = 4,       // chained between targets
    }

    [Serializable]
    public class SkillDamageLevel
    {
        public int level;
        public int baseDamage;       // KMagicAttrib damage value for this level
        public float attackRatio;    // scales with caster attack (m_bUseAttackRate)
        public bool isPhysical;      // m_bIsPhysical
    }

    [Serializable]
    public class SkillDefinition
    {
        public int skillId;              // m_nId
        public string nameRaw;           // m_szName (GB2312)
        public string nameNormalized;
        public int reqLevel;             // m_usReqLevel
        public int cost;                 // m_nCost (mana/cost)
        public int attackRadius;         // m_nAttackRadius (range, source units)
        public bool isPhysical;          // m_bIsPhysical
        public SkillMissileForm missileForm; // m_eMisslesForm

        // Resource references (resolved through the asset registry).
        public SourceAssetId iconSourceId;       // m_szSkillIcon
        public SourceAssetId effectSourceId;     // m_szPreCastEffectFile
        public SourceAssetId missileSpriteId;    // missile/projectile sprite

        public bool iconResolved;
        public bool effectResolved;

        // Per-level damage data (m_DamageAttribs loaded per level).
        public List<SkillDamageLevel> damageLevels = new();

        public List<string> warnings = new();

        public string DisplayName =>
            !string.IsNullOrEmpty(nameNormalized) ? nameNormalized :
            !string.IsNullOrEmpty(nameRaw) ? nameRaw : $"Skill_{skillId}";

        public bool HasMissile => missileForm != SkillMissileForm.None;

        public SkillDamageLevel GetLevel(int level)
        {
            SkillDamageLevel best = null;
            foreach (var d in damageLevels)
                if (d.level <= level && (best == null || d.level > best.level))
                    best = d;
            return best ?? (damageLevels.Count > 0 ? damageLevels[0] : null);
        }
    }
}
