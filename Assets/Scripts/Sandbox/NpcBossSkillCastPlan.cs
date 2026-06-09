using VLTK.Model;

namespace VLTK.Sandbox
{
    public struct NpcBossSkillCastPlan
    {
        public bool canCast;
        public int skillId;
        public string skillNameRaw;
        public int skillStyle;
        public int attackRadius;
        public int childSkillId;
        public int childSkillLevel;
        public int childSkillNum;
        public int cooldownTicks;
        public int skillCostType;
        public int costValue;
        public bool isPhysical;
        public bool isMelee;
        public bool targetOnly;
        public bool targetEnemy;
        public bool targetAlly;
        public bool targetSelf;
        public bool targetOther;
        public bool targetObj;
        public bool targetNoNpc;
        public int horseLimit;
        public bool doHurt;
        public bool weaponSkill;
        public int maxLevel;
        public string levelSetScript;
        public bool missingScriptGuard;
        public string guardReason;

        public SkillDefinition ToSkillDefinition()
        {
            return new SkillDefinition
            {
                skillId = skillId,
                nameRaw = skillNameRaw,
                nameNormalized = skillNameRaw,
                maxLevel = maxLevel > 0 ? maxLevel : 20,
                cost = costValue,
                skillCostType = skillCostType,
                timePerCast = cooldownTicks,
                attackRadius = attackRadius,
                isPhysical = isPhysical,
                isMelee = isMelee,
                skillStyle = (PcSkillStyle)skillStyle,
                missileForm = skillStyle == (int)PcSkillStyle.Missiles ? SkillMissileForm.Single : SkillMissileForm.None,
                childSkillId = childSkillId,
                childSkillLevel = childSkillLevel,
                childSkillNum = childSkillNum,
                targetOnly = targetOnly,
                targetEnemy = targetEnemy,
                targetAlly = targetAlly,
                targetSelf = targetSelf,
                targetObj = targetObj,
                horseLimit = horseLimit,
                doHurt = doHurt,
                weaponSkill = weaponSkill,
            };
        }
    }
}
