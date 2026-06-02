using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcCaiBangModTuning
    {
        public static bool Applies(int skillId) => skillId is 357 or 359 or 1073 or 1074;

        public static PcModSkillSpec AtLevel(int skillId, int level)
        {
            int lv = Mathf.Max(1, level);
            return skillId switch
            {
                357 => PhiLongAtLevel(lv),
                359 => ThienHaVoCauAtLevel(lv),
                1073 => ThanThuLenhLongAtLevel(lv),
                1074 => BongHoanhLuocMaAtLevel(lv),
                _ => default,
            };
        }

        private static PcModSkillSpec PhiLongAtLevel(int lv)
        {
            return new PcModSkillSpec
            {
                missileForm = (SkillMissileForm)InterpolateInt(lv, new[] { (1,1), (10,1), (11,0), (20,0) }),
                missileCount = InterpolateInt(lv, new[] { (1,1), (11,1), (12,2), (15,2), (16,3), (20,4) }),
                param1 = InterpolateInt(lv, new[] { (1,0), (10,0), (11,32), (20,32) }),
                missileSpeed = InterpolateInt(lv, new[] { (1,20), (20,24) }),
                attackRadius = InterpolateInt(lv, new[] { (1,448), (20,512) }),
                manaCost = InterpolateInt(lv, new[] { (1,10), (20,65) }),
                seriesDamageP = InterpolateInt(lv, new[] { (1,20), (15,20), (20,60) }),
            };
        }

        private static PcModSkillSpec ThienHaVoCauAtLevel(int lv)
        {
            return new PcModSkillSpec
            {
                missileForm = SkillMissileForm.Single,
                missileCount = InterpolateInt(lv, new[] { (1,1), (20,3) }),
                param1 = 32,
                missileSpeed = InterpolateInt(lv, new[] { (1,20), (20,24) }),
                attackRadius = InterpolateInt(lv, new[] { (1,448), (20,512) }),
                manaCost = InterpolateInt(lv, new[] { (1,20), (20,50) }),
                seriesDamageP = InterpolateInt(lv, new[] { (1,20), (15,20), (20,60) }),
            };
        }

        private static PcModSkillSpec ThanThuLenhLongAtLevel(int lv)
        {
            return new PcModSkillSpec
            {
                missileForm = SkillMissileForm.Single,
                missileCount = 1,
                missileSpeed = InterpolateInt(lv, new[] { (1,24), (20,40) }),
                attackRadius = InterpolateInt(lv, new[] { (1,448), (20,512) }),
                manaCost = InterpolateInt(lv, new[] { (1,12), (20,78) }),
                seriesDamageP = InterpolateInt(lv, new[] { (1,40), (15,40), (20,80) }),
            };
        }

        private static PcModSkillSpec BongHoanhLuocMaAtLevel(int lv)
        {
            return new PcModSkillSpec
            {
                missileForm = SkillMissileForm.Single,
                missileCount = InterpolateInt(lv, new[] { (1,1), (20,5) }),
                missileSpeed = 24,
                attackRadius = InterpolateInt(lv, new[] { (1,448), (20,512) }),
                manaCost = InterpolateInt(lv, new[] { (1,20), (20,50) }),
                seriesDamageP = InterpolateInt(lv, new[] { (1,40), (15,40), (20,80) }),
            };
        }

        private static int InterpolateInt(int level, (int level, int value)[] points)
        {
            if (points == null || points.Length == 0) return 0;
            if (level <= points[0].level) return points[0].value;
            for (int i = 1; i < points.Length; i++)
            {
                var prev = points[i - 1];
                var next = points[i];
                if (level <= next.level)
                {
                    if (next.level <= prev.level) return next.value;
                    float t = (level - prev.level) / (float)(next.level - prev.level);
                    return Mathf.FloorToInt(Mathf.Lerp(prev.value, next.value, t));
                }
            }
            return points[points.Length - 1].value;
        }
    }

    public struct PcModSkillSpec
    {
        public SkillMissileForm missileForm;
        public int missileCount;
        public int param1;
        public int missileSpeed;
        public int attackRadius;
        public int manaCost;
        public int seriesDamageP;
    }
}
