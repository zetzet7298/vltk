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
        public int reqLevel;             // ReqLevel
        public int maxLevel;             // MaxLevel
        public int cost;                 // CostValue / magic_skill_cost_v
        public int skillCostType;        // SkillCostType (PC NPCATTRIB, 0=mana)
        public int timePerCast;          // TimePerCast / m_nMinTimePerCast
        public int waitTime;             // WaitTime / m_nWaitTime
        public int attackRadius;         // m_nAttackRadius (range, source units)
        public bool isPhysical;          // m_bIsPhysical
        public bool isMelee;             // IsMelee
        public bool isAura;              // IsAura
        public int stateSpecialId;       // StateSpecialId
        public PcSkillStyle skillStyle;  // SkillStyle
        public CombatFaction faction;    // CharClass
        public SkillMissileForm missileForm; // m_eMisslesForm

        public int childSkillId;         // ChildSkillId
        public int childSkillLevel;      // ChildSkillLevel (0 means current level in PC missile skills)
        public int childSkillNum;        // ChildSkillNum
        public bool baseSkill;           // BaseSkill
        public int charAnimId;           // CharAnimId (PC client action id)
        public bool targetOnly;          // TargetOnly
        public bool targetEnemy;         // TargetEnemy
        public bool targetAlly;          // TargetAlly
        public bool targetSelf;          // TargetSelf
        public bool targetObj;           // TargetObj
        public bool byMissile;           // ByMissle
        public bool isUseAttackRating;   // IsUseAR
        public bool doHurt;              // DoHurt
        public bool weaponSkill;         // WeaponSkill
        public int equipLimit = -2;      // EqtLimit
        public int horseLimit;           // HorseLimit
        public int missilesGenerate;     // MslsGenerate
        public int missilesGenerateData; // MslsGenerateData

        // Resource references (resolved through the asset registry).
        public SourceAssetId iconSourceId;       // m_szSkillIcon
        public SourceAssetId effectSourceId;     // m_szPreCastEffectFile
        public SourceAssetId missileSpriteId;    // missile/projectile sprite

        public bool iconResolved;
        public bool effectResolved;

        // Per-level legacy damage summary (kept for M4.1 tests) and full PC magic data.
        public List<SkillDamageLevel> damageLevels = new();
        public List<SkillLevelData> pcLevelData = new();

        public List<string> warnings = new();

        public string DisplayName =>
            !string.IsNullOrEmpty(nameNormalized) ? nameNormalized :
            !string.IsNullOrEmpty(nameRaw) ? nameRaw : $"Skill_{skillId}";

        public bool HasMissile => missileForm != SkillMissileForm.None;

        public bool IsCaiBang => faction == CombatFaction.CaiBang;

        public SkillLevelData GetPcLevelData(int level)
        {
            SkillLevelData best = null;
            foreach (var d in pcLevelData)
                if (d.level <= level && (best == null || d.level > best.level))
                    best = d;
            return best ?? (pcLevelData.Count > 0 ? pcLevelData[0] : null);
        }

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
