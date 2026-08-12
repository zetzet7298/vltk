using System;
using System.Collections.Generic;
using UnityEngine;

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
        Zone = 5,        // PC SKILL_MF_Zone — missiles distribute in a fixed-radius area
        Stance = 6,      // PC form 6 — stance/self buff (no missile spawn; metadata parity). Cái Bang 127/720.
        Stationary = 7,  // PC form 7 — stationary/area child missile such as Cái Bang 358
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
        public int timePerCastOnHorse;   // TimePerCastOnHorse
        public int waitTime;             // WaitTime / m_nWaitTime
        public int attackRadius;         // m_nAttackRadius (range, source units)
        public bool isPhysical;          // m_bIsPhysical
        public bool isMelee;             // IsMelee
        public bool isAura;              // IsAura
        public int stateSpecialId;       // StateSpecialId
        public PcSkillStyle skillStyle;  // SkillStyle
        public CombatFaction faction;    // CharClass
        public Series series;
        public SkillMissileForm missileForm; // m_eMisslesForm

        public int childSkillId;         // ChildSkillId
        public int childSkillLevel;      // ChildSkillLevel (0 means current level in PC missile skills)
        public int childSkillNum;        // ChildSkillNum
        public int missileDirStep;       // PC m_nValue1 (PcSkills Param1) — fan spread step in 1/64-circle units; 0 = default 1
        public int missileFirstStep;     // PC m_nValue2 (PcSkills Param2) — fan/circle missile spawn offset from caster (px)
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
        // [SECT-ALL] MeleeType cho các melee skill thật (Cái Bang Bổng Pháp, etc.).
        // PC KNpc::CastMeleeSkill switch (line 1834-1891) có 5 nhánh: AttackWithBlur, Jump, JumpAndAttack,
        //   RunAndAttack, ManyAttack. Mobile trước fix không phân biệt được — mọi melee thành instant swing.
        // [SECT-ALL fix 2026-06-15]: Phi Long (357) KHÔNG phải melee — IsMelee=0, ByMissle=1 trong PC source.
        //   Trước fix nhầm gán JumpAndAttack cho 357 (commit e194a242a đọc sai gaibang.lua). Đã revert.
        public PcMeleeType meleeType;    // PC MeleeType (default AttackWithBlur cho Melee, None cho khác)
        public int maxShadowNum;         // MaxShadowNum — multi-shadow cho melee nhiều hit
        public bool dashVisualsEnabled;  // Cờ runtime: có show visual follow caster khi dash không
        public Vector2 dashOrigin;       // Lưu vị trí bắt đầu dash (set khi dash start)
        // [SECT-ALL] TODO(PC-runtime): dashDurationSeconds KHÔNG có trong PC source.
        // PC server (KNpc::DoRunAttack/NewJump ở jx_linux_y) chỉ set state + distance;
        // animation duration thuộc client engine (cần PC runtime video để verify).
        // Mobile port: caller (CombatSkillSlotController) đọc field này để chạy BeginDash.
        //   - > 0: dash mượt theo PC state machine
        //   - <= 0: skip dash (PC source không provide duration → không implement)
        public float dashDurationSeconds; // 0 = no dash (PC source doesn't provide duration)
        // [SECT-ALL] PC 轻功 (Khinh Công / JumpFly, skill 210): a self-cast movement skill that
        // leaps the caster forward (PC KNpc::NewJump). Flagged so the cast pipeline triggers
        // SandboxPlayerController.BeginLeap (Jump JP01 animation + forward dash) instead of a no-op.
        public bool isLeapSkill;
        // [SECT-QUICKWIN] Gap report baocao-all-sect-skills.md §2.4.2 G6 + §2.8.2 G6: event chain anchors.
        // PC tianren.lua/tangmen.lua/emei.lua khai báo:
        //   skill_startevent[1] (1/0 → 1/1) trigger sub-skill khi cast start
        //   skill_flyevent[1] trigger sub-skill giữa đường bay
        //   skill_collideevent[1] trigger khi missile va chạm NPC
        //   skill_vanishedevent[1] trigger khi missile biến mất (hết lifetime)
        // Mobile runtime Phase 4 sẽ wire các field này. Hiện tại anchor cho catalog khớp PC.
        public int collideSkillId;      // PC CollidSkillId (event chain khi missile va chạm)
        public int collideSkillLevel;    // PC EventSkillLevel (level của sub-skill được fire, default 1)
        public int vanishSkillId;        // PC VanishSkillId (event chain khi missile vanish)
        public int vanishSkillLevel;     // PC VanishEvent level
        public int flySkillId;           // PC FlySkillId (event chain giữa đường bay)
        public int flySkillLevel;        // PC FlyEvent level
        public int flyEventTime;         // PC FlyEventTime (tick khi fire mid-flight event)
        public int startSkillId;         // PC StartSkillId (event chain khi cast start)
        public int startSkillLevel;      // PC StartEvent level
        public string lvlSetScript;      // LvlSetScript (PC Lua tuning script path)
        public string levelUpScript;     // LevelUpScript (PC Lua upgrade script path)

        // Resource references (resolved through the asset registry).
        public SourceAssetId iconSourceId;       // m_szSkillIcon
        public SourceAssetId effectSourceId;     // m_szPreCastEffectFile
        public SourceAssetId missileSpriteId;    // missile/projectile sprite

        // PC skills.txt cast audio (cols 7 + 8).
        // Source: KSkill::Cast → KClient::PlaySkillSound(m_szManCastSnd | m_szFMCastSnd).
        // Played at the cast-frame of the CharAnimId action, BEFORE the missile spawns.
        // Mobile wire: SkillEffectVisualService.PlaySkillCast → AudioService.PlaySkillCast.
        // Empty string = no skill-level cast sound (some passive/buff skills have none in PC).
        public string manCastSndPath;            // m_szManCastSnd  (\sound\skill\sound_kXXX.wav)
        public string fmCastSndPath;             // m_szFMCastSnd   (female variant; same wav family)

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
