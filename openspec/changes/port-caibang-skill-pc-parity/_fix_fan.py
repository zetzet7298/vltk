import io

p = r"Assets/Scripts/Sandbox/SkillEffectVisualService.cs"
s = io.open(p, encoding="utf-8").read()

old = """        private void SetupPcCircleOutwardMissiles(ActiveSkillEffect fx, int count)
        {
            fx.missileCount = count;
            fx.missilePositions = new Vector2[count];
            fx.missileTargets = new Vector2[count];
            float angleStep = 360f / count;
            float distance = Mathf.Max(1f, fx.pcMissileSpeedPerTick * fx.pcMissileLifeTicks);
            fx.missileDuration = fx.pcMissileLifeTicks / 18f;
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Deg2Rad * (i * angleStep);
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                fx.missilePositions[i] = fx.casterPos;
                fx.missileTargets[i] = fx.casterPos + dir * distance;
            }
        }"""
new = """        // PC KSkills.cpp CastSpread (SKILL_MF_Spread, e.g. 165 "Vô Ngã Vô Kiếm" wudang.lua):
        // missiles fan around castDir (caster->target). nCurMSRadius = childNum/2,
        // dir_i = nDir + Value1*(i - half) in MaxMissleDir=64 dir units; spawn offset
        // nFirstStep = Value2 px along dir_i (0 = at caster). Trước fix: full 360° xoay
        // quanh caster không theo castDir (tia bay lung tung, "quạt quay" sai PC).
        private void SetupPcFanMissiles(SkillDefinition skill, ActiveSkillEffect fx, int count)
        {
            fx.missileCount = count;
            fx.missilePositions = new Vector2[count];
            fx.missileTargets = new Vector2[count];
            Vector2 baseDir = fx.targetPos - fx.casterPos;
            float targetDist = Mathf.Max(1f, baseDir.magnitude);
            baseDir /= targetDist;
            float stepRad = Mathf.Deg2Rad * (360f / 64f) * (skill != null && skill.missileDirStep > 0 ? skill.missileDirStep : 1);
            int half = count / 2;
            float firstStep = skill != null ? Mathf.Max(0, skill.missileFirstStep) : 0f;
            float distance = Mathf.Max(1f, fx.pcMissileSpeedPerTick * fx.pcMissileLifeTicks);
            if (distance < targetDist) distance = targetDist;
            fx.missileDuration = fx.pcMissileLifeTicks / 18f;
            for (int i = 0; i < count; i++)
            {
                float angle = (i - half) * stepRad;
                float c = Mathf.Cos(angle), sn = Mathf.Sin(angle);
                var dir = new Vector2(baseDir.x * c - baseDir.y * sn,
                                      baseDir.x * sn + baseDir.y * c);
                fx.missilePositions[i] = fx.casterPos + dir * firstStep;
                fx.missileTargets[i] = fx.casterPos + dir * (firstStep + distance);
            }
        }"""
assert s.count(old) == 1, "fan setup block not unique"
s = s.replace(old, new)

old = "                          SetupPcCircleOutwardMissiles(fx, count);"
assert s.count(old) == 1, "fan call site not unique"
s = s.replace(old, "                          SetupPcFanMissiles(skill, fx, count);")

io.open(p, "w", encoding="utf-8", newline="\n").write(s)
print("ok service")
