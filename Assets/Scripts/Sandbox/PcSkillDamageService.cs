// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.2 PC Skill Damage Service
// Sát thương chiêu thức ngũ hành tương khắc của 10 môn phái.
// Sách tham khảo: PcSkills.txt magic attributes, gaibang.lua
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service tính toán sát thương chiêu thức ngũ hành tương khắc của 10 môn phái.
    /// PC source: KNpc::CalcDamage / KSkill::GetSkillDamage.
    /// </summary>
    public class PcSkillDamageService
    {
        private readonly DamageFormulaService _formulaService;

        public PcSkillDamageService(DamageFormulaService formulaService = null)
        {
            _formulaService = formulaService ?? new DamageFormulaService();
        }

        /// <summary>
        /// Tính toán sát thương chiêu thức dựa trên thuộc tính ngũ hành của skill và stats của caster/target.
        /// </summary>
        public DamageResult CalculateSkillDamage(SkillDefinition skill, int skillLevel, AttackerStats atk, DefenderStats def, int casterFactionId = 0, int targetFactionId = 0)
        {
            if (skill == null || skillLevel <= 0)
                return new DamageResult();

            // 1) Sát thương cơ bản của chiêu thức
            int skillMin = 0;
            int skillMax = 0;
            DamageType mainType = atk.type;

            // Đọc thuộc tính ngũ hành môn phái từ level data của skill
            var levelData = skill.GetPcLevelData(skillLevel);
            if (levelData != null)
            {
                foreach (var attr in levelData.AllAttributes())
                {
                    switch (attr.kind)
                    {
                        case MagicAttributeKind.PhysicsDamageV:
                            skillMin += attr.value1;
                            skillMax += attr.value2;
                            mainType = DamageType.Physics;
                            break;
                        case MagicAttributeKind.FireDamageV:
                            skillMin += attr.value1;
                            skillMax += attr.value2;
                            mainType = DamageType.Fire;
                            break;
                        case MagicAttributeKind.PoisonDamageV:
                            skillMin += attr.value1;
                            skillMax += attr.value2;
                            mainType = DamageType.Poison;
                            break;
                        // Hỗ trợ ngũ hành khác từ trang bị/buff
                        default:
                            break;
                    }
                }
            }

            // 2) Kết hợp sát thương cơ bản của nhân vật + sát thương chiêu thức
            int finalMin = atk.minDamage + skillMin;
            int finalMax = atk.maxDamage + skillMax;

            // 3) Ngũ hành tương khắc (Ngũ hành sinh khắc của 10 phái JX1)
            // Kim khắc Mộc, Mộc khắc Thổ, Thổ khắc Thủy, Thủy khắc Hỏa, Hỏa khắc Kim.
            float relationMultiplier = GetElementRelationMultiplier(casterFactionId, targetFactionId);
            finalMin = Mathf.RoundToInt(finalMin * relationMultiplier);
            finalMax = Mathf.RoundToInt(finalMax * relationMultiplier);

            var adjustedAtk = new AttackerStats
            {
                minDamage = finalMin,
                maxDamage = finalMax,
                type = mainType,
                isMelee = atk.isMelee
            };

            // 4) Đưa qua pipeline DamageFormulaService
            return _formulaService.Compute(adjustedAtk, def);
        }

        /// <summary>
        /// Lấy hệ số ngũ hành sinh khắc của môn phái JX1.
        /// Sinh khắc tăng 30% sát thương (multiplier = 1.3), bị khắc giảm 30% (multiplier = 0.7).
        /// </summary>
        public static float GetElementRelationMultiplier(int casterFaction, int targetFaction)
        {
            if (casterFaction <= 0 || targetFaction <= 0) return 1.0f;

            int casterElement = CombatFactionExt.ToCharClass(casterFaction);
            int targetElement = CombatFactionExt.ToCharClass(targetFaction);

            // Kim=1, Thủy=2, Mộc=3, Hỏa=4, Thổ=5.
            // Sinh khắc của PC JX1:
            // 1 khắc 3 (Kim khắc Mộc)
            // 3 khắc 5 (Mộc khắc Thổ)
            // 5 khắc 2 (Thổ khắc Thủy)
            // 2 khắc 4 (Thủy khắc Hỏa)
            // 4 khắc 1 (Hỏa khắc Kim)
            if (casterElement == 1 && targetElement == 3) return 1.3f;
            if (casterElement == 3 && targetElement == 5) return 1.3f;
            if (casterElement == 5 && targetElement == 2) return 1.3f;
            if (casterElement == 2 && targetElement == 4) return 1.3f;
            if (casterElement == 4 && targetElement == 1) return 1.3f;

            // Ngược lại (Bị khắc)
            if (casterElement == 3 && targetElement == 1) return 0.7f;
            if (casterElement == 5 && targetElement == 3) return 0.7f;
            if (casterElement == 2 && targetElement == 5) return 0.7f;
            if (casterElement == 4 && targetElement == 2) return 0.7f;
            if (casterElement == 1 && targetElement == 4) return 0.7f;

            return 1.0f;
        }
    }
}
