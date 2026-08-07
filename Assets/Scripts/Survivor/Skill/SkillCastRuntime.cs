// -----------------------------------------------------------------------------
// VLTK.Survivor — SkillCastRuntime (ticket 27)
// Cast pipeline cho roster SkillDef:
//  - Form 7 (ranged): đạn bay hướng cast. Fan spread ĐÚNG công thức PC
//    KSkill::CastSpread (KSkills.cpp:1770): dir_i = castDir + Param1×(i−half),
//    Param1 đơn vị 1/64 vòng (MaxMissleDir=64), half = ChildSkillNum/2 (int div),
//    spawn offset = Param2 px DỌC THEO dir_i (nDesSubX = nRefPX + nCosAB×nFirstStep).
//    KHÔNG chia 360° quanh caster. Số đạn = ChildSkillNum (col 22, magic
//    misslenum → m_nChildSkillNum). PC cnt=0 → 0 đạn (form khác code path);
//    own: max(1, cnt) để form-7 luôn cast được.
//  - Form 12 (melee): hit NGAY trong bán kính AttackRadius px (÷40 ppu) nửa
//    mặt trước hướng cast; visual qua child missile, KHÔNG cần PreCastSpr (PC).
//    IsMelee set sai → fail-closed: MissileSprUid = "" → proxy màu, không crash.
//  - Precast: PreCastSprUid staged (JxPathHash) → hiển thị SPR; rỗng → proxy
//    màu (fail-closed, KHÔNG bịa path). Melee không precast.
//  - Attribution: hit ghi SkillImpactSource{skillId} + caster vào
//    SurvivorMonster.Ledger; kill → KillSource = TopSource. XP credit player
//    giữ nguyên qua gem drop P1 (single-player: gem pickup = credit về player).
// Damage/cooldown = own-design (verify LvlData toàn 0 → PC không có bảng số):
//   damage = (2 + 1.5×(level−1)) × 1.2 melee; fan >1 đạn × 0.8 mỗi đạn
//   cd = max(0.4, 1.0 − 0.02×(level−1))s, melee × 0.8 (P1 auto 0.6s — skill
//   chậm hơn nhưng mạnh hơn, fan tổng DPS ~ bằng đạn đơn).
// Core thuần (PlanCast/FanCount/DamageFor/CooldownFor) = test EditMode không
// scene (spec Testing Decisions). Scene glue (SkillCastSpawner/SpriteLoader/
// SkillPrecastFx) tách cùng file, không gọi từ core.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Sprites;

namespace VLTK.Survivor
{
    /// <summary>1 đạn fan: hướng bay + offset spawn (Param2 px → unit ÷ PxPerUnit).</summary>
    public struct CastMissile
    {
        public Vector2 Dir;
        public Vector2 Offset;
    }

    /// <summary>Kết quả cast 1 skill — pure data, scene glue đọc để spawn.</summary>
    public struct SkillCastPlan
    {
        public int SkillId;
        public SkillDef SourceDef; // PC frame-exact VFX adapter needs full def
        public int Level;
        public bool IsMelee;
        public Vector2 CastDir;
        public float Damage;
        public float Cooldown;
        /// <summary>Staged precast SPR uid; "" → proxy màu (fail-closed). Melee luôn "".</summary>
        public string PreCastSprUid;
        /// <summary>Child missile staged uid; "" → proxy. Melee IsMelee sai → "" (fail-closed).</summary>
        public string MissileSprUid;
        public float MissileSpeed;
        public float MissileLife;
        /// <summary>Melee hit radius (AttackRadius px ÷ 40).</summary>
        public float MeleeRadius;
        /// <summary>Ranged: ≥1 đạn (dir + offset). Melee: null.</summary>
        public CastMissile[] Missiles;
    }

    /// <summary>
    /// Roster cast runtime (pure logic, không scene). Player giữ 1 instance;
    /// orchestrator seed debug roster 1 skill cho tới ticket 29 (skill choice).
    /// </summary>
    public sealed class SkillCastRuntime
    {
        public const float MaxMissileDir = 64f; // PC MaxMissleDir — 1/64 vòng
        public const float PxPerUnit = 40f;     // JX SPR ppu (quyết định P1.5)

        // own-design balance (rationale file header) — LvlData PC toàn 0 đã verify.
        public const float BaseDamage = 2f;
        public const float DamagePerLevel = 1.5f;
        public const float MeleeDamageMul = 1.2f;
        public const float FanDamageMul = 0.8f;
        public const float BaseCooldown = 1f;
        public const float CooldownPerLevel = 0.02f;
        public const float MinCooldown = 0.4f;
        public const float MeleeCooldownMul = 0.8f;
        public const float DefaultMissileSpeed = 12f; // missles.txt Speed nếu có
        public const float DefaultMissileLife = 2f;   // missles.txt LifeTime nếu có

        public sealed class ActiveSkill
        {
            public readonly SkillDef Def;
            public int Level;
            public float CooldownRemaining;
            public ActiveSkill(SkillDef def, int level) { Def = def; Level = level; }
        }

        public readonly List<ActiveSkill> Roster = new List<ActiveSkill>();

        public bool HasAnySkill => Roster.Count > 0;

        public void Tick(float dt)
        {
            for (int i = 0; i < Roster.Count; i++)
            {
                var a = Roster[i];
                if (a.CooldownRemaining > 0f) a.CooldownRemaining -= dt;
            }
        }

        /// <summary>Học skill: trùng id → cộng level (cap MaxLevel); mới → thêm roster.</summary>
        public ActiveSkill Learn(SkillDef def, int level = 1)
        {
            for (int i = 0; i < Roster.Count; i++)
            {
                if (Roster[i].Def.Id != def.Id) continue;
                Roster[i].Level = ClampLevel(def, Roster[i].Level + level);
                return Roster[i];
            }
            var a = new ActiveSkill(def, ClampLevel(def, level));
            Roster.Add(a);
            return a;
        }

        public int GetLevel(int skillId)
        {
            for (int i = 0; i < Roster.Count; i++)
                if (Roster[i].Def.Id == skillId) return Roster[i].Level;
            return 0;
        }

        /// <summary>
        /// Cast skill ready đầu tiên (cooldown hết) → plan + đặt cd. Không skill
        /// ready → false (auto-attack P1 tiếp tục — player hook dùng return này).
        /// </summary>
        public bool TryCast(Vector2 castDir, out SkillCastPlan plan)
        {
            for (int i = 0; i < Roster.Count; i++)
            {
                var a = Roster[i];
                if (a.CooldownRemaining > 0f) continue;
                plan = PlanCast(a.Def, a.Level, castDir);
                a.CooldownRemaining = plan.Cooldown;
                return true;
            }
            plan = default;
            return false;
        }

        public static int ClampLevel(SkillDef def, int level)
        {
            int max = def.MaxLevel > 0 ? def.MaxLevel : 99;
            return Mathf.Clamp(level, 1, max);
        }

        /// <summary>Số đạn fan = max(1, ChildSkillNum); melee → 0.</summary>
        public static int FanCount(SkillDef def)
        {
            if (def.Form == 12) return 0;
            return Mathf.Max(1, def.ChildSkillNum);
        }

        public static float DamageFor(SkillDef def, int level)
        {
            float dmg = BaseDamage + DamagePerLevel * (level - 1);
            if (def.Form == 12) dmg *= MeleeDamageMul;
            return dmg;
        }

        public static float CooldownFor(SkillDef def, int level)
        {
            float cd = BaseCooldown - CooldownPerLevel * (level - 1);
            if (def.Form == 12) cd *= MeleeCooldownMul;
            return Mathf.Max(MinCooldown, cd);
        }

        /// <summary>
        /// Fan spread parity PC CastSpread (KSkills.cpp:1770):
        ///   n = max(1, ChildSkillNum); half = n/2 (int div — PC nCurMSRadius)
        ///   dir_i = castDir + Param1×(i−half), Param1 đơn vị 1/64 vòng
        ///   offset_i = dir_i × Param2 px (dọc theo hướng đạn — nFirstStep)
        /// KHÔNG chia 360° quanh caster. Param1=0 → mọi đạn cùng hướng castDir.
        /// </summary>
        public static SkillCastPlan PlanCast(SkillDef def, int level, Vector2 castDir)
        {
            bool melee = def.Form == 12;
            var plan = new SkillCastPlan
            {
                SkillId = def.Id,
                SourceDef = def,
                Level = level,
                IsMelee = melee,
                CastDir = castDir,
                Damage = DamageFor(def, level),
                Cooldown = CooldownFor(def, level),
                PreCastSprUid = melee ? "" : def.PreCastSprUid,
                MissileSprUid = ChildVisualUid(def, melee),
                MissileSpeed = def.ChildMissile != null && def.ChildMissile.Speed > 0f
                    ? def.ChildMissile.Speed : DefaultMissileSpeed,
                MissileLife = def.ChildMissile != null && def.ChildMissile.LifeTime > 0f
                    ? def.ChildMissile.LifeTime : DefaultMissileLife,
                MeleeRadius = def.AttackRadius / PxPerUnit,
            };
            if (melee) return plan;

            int n = FanCount(def);
            int half = n / 2;
            if (n > 1) plan.Damage *= FanDamageMul; // own: fan tổng DPS không bùng
            float stepRad = def.FanParam1 / MaxMissileDir * (Mathf.PI * 2f);
            float offsetUnits = def.FanParam2 / PxPerUnit;
            plan.Missiles = new CastMissile[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 dir = Rotate(castDir, (i - half) * stepRad);
                plan.Missiles[i] = new CastMissile { Dir = dir, Offset = dir * offsetUnits };
            }
            return plan;
        }

        /// <summary>Child missile visual; fail-closed: melee mà IsMelee sai → bỏ visual (proxy).</summary>
        private static string ChildVisualUid(SkillDef def, bool melee)
        {
            string uid = def.ChildMissile != null ? def.ChildMissile.AnimFileUid : "";
            if (melee && !def.IsMelee) return "";
            return uid;
        }

        private static Vector2 Rotate(Vector2 v, float a)
        {
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }

    /// <summary>Staged SPR loader fail-closed: uid rỗng / file thiếu lúc runtime → null → proxy.</summary>
    public static class SpriteLoader
    {
        private static SprRuntimeService _svc;

        public static Sprite Resolve(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            if (_svc == null) _svc = new SprRuntimeService();
            return _svc.ResolveSprite(uid, 32, 32);
        }
    }

    /// <summary>Scene glue: SkillCastPlan → projectiles / melee hit / precast fx.</summary>
    public static class SkillCastSpawner
    {
        public static void Spawn(SurvivorGameDirector director, SkillCastPlan plan, Vector3 pos, object caster)
        {
            if (director == null) return;
            var source = new SkillImpactSource(plan.SkillId, 0);

            // PC frame-exact VFX via Sandbox pipeline (4 active Cai Bang skills).
            // Visual ONLY -- gameplay damage stays in MeleeHit / SpawnProjectile below.
            bool usePcVfx = director.SkillFx != null
                            && plan.SourceDef != null
                            && CaiBangActiveSkillSet.IsActive(plan.SkillId);

            if (plan.IsMelee)
            {
                if (usePcVfx)
                    director.SkillFx.Cast(plan.SourceDef, pos, (Vector2)pos + plan.CastDir * 2f, plan.Level);
                else
                    ShowMeleeFlash(plan, pos);
                MeleeHit(director, plan, pos, source, caster);
                return;
            }

            if (usePcVfx)
            {
                // PC parity: homing + impact (KMissle.cpp MISSLE_MMK_Follow).
                // Pick nearest alive monster in the cast half-plane; if found the missile
                // chases its live position (getCurrentTargetPos) so it no longer fades at a
                // fixed point, and the impact SPR renders at the hit location (service
                // renders impact via SpawnCollideSubEffect when onMissileCollided is null).
                // No monster -> getCurrentTargetPos null -> straight line like before.
                Vector2 staticTarget = (Vector2)pos + plan.CastDir * (plan.MissileSpeed * plan.MissileLife);
                SurvivorMonster homingTarget = PickHomingTarget(director, pos, plan.CastDir);
                if (homingTarget != null)
                {
                    const float k = 1f / PxPerUnit; // post-normalize space (world / PxPerUnit)
                    var captured = homingTarget;
                    Vector2 fallback = staticTarget * k;
                    Func<Vector2> getCurrentTargetPos = () =>
                        (captured != null && captured.Hp > 0f)
                            ? (Vector2)captured.transform.position * k
                            : fallback;
                    director.SkillFx.Cast(plan.SourceDef, pos, staticTarget, plan.Level, getCurrentTargetPos, null);
                }
                else
                {
                    director.SkillFx.Cast(plan.SourceDef, pos, staticTarget, plan.Level);
                }
            }
            else
            {
                ShowPreCast(plan, pos);
            }

            for (int i = 0; i < plan.Missiles.Length; i++)
            {
                director.SpawnProjectile(
                    pos + (Vector3)plan.Missiles[i].Offset,
                    plan.Missiles[i].Dir, plan.Damage,
                    plan.MissileSpeed, plan.MissileLife,
                    usePcVfx ? "" : plan.MissileSprUid,
                    source, caster);
            }
        }

        /// <summary>
        /// Nearest alive monster in the cast half-plane (Dot(castDir, to) >= 0).
        /// Gives the missile VFX a homing target (PC KMissle follows the nearest NPC).
        /// Returns null if none -- caller falls back to a straight-line missile.
        /// </summary>
        private static SurvivorMonster PickHomingTarget(SurvivorGameDirector director, Vector3 pos, Vector2 castDir)
        {
            var list = director.Monsters;
            SurvivorMonster best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m == null || m.Hp <= 0f) continue;
                Vector2 to = (Vector2)(m.transform.position - pos);
                float dist = to.magnitude;
                if (dist < 0.0001f) continue;
                if (Vector2.Dot(castDir, to / dist) < 0f) continue; // behind caster
                if (dist < bestDist) { bestDist = dist; best = m; }
            }
            return best;
        }
        /// <summary>Melee: hit mọi monster trong MeleeRadius nửa mặt trước hướng cast + attribution.</summary>
        private static void MeleeHit(SurvivorGameDirector director, SkillCastPlan plan, Vector3 pos,
            SkillImpactSource source, object caster)
        {
            var list = director.Monsters;
            float r2 = plan.MeleeRadius * plan.MeleeRadius;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var m = list[i];
                if (m == null) continue;
                Vector2 to = (Vector2)(m.transform.position - pos);
                if (to.sqrMagnitude > r2) continue;
                if (to.sqrMagnitude > 0.0001f && Vector2.Dot(plan.CastDir, to.normalized) <= 0f) continue;
                m.TakeDamage(plan.Damage, source, caster);
            }
        }

        private static void ShowPreCast(SkillCastPlan plan, Vector3 pos)
        {
            var go = new GameObject("skill_precast");
            var sr = go.AddComponent<SpriteRenderer>();
            var sp = SpriteLoader.Resolve(plan.PreCastSprUid);
            if (sp != null) sr.sprite = sp;
            else { sr.sprite = ProxyVisuals.White(); sr.color = new Color(0.55f, 0.8f, 1f, 0.9f); }
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            go.AddComponent<SkillPrecastFx>();
        }

        private static void ShowMeleeFlash(SkillCastPlan plan, Vector3 pos)
        {
            var go = new GameObject("skill_melee");
            var sr = go.AddComponent<SpriteRenderer>();
            var sp = SpriteLoader.Resolve(plan.MissileSprUid);
            if (sp != null) sr.sprite = sp;
            else { sr.sprite = ProxyVisuals.White(); sr.color = new Color(0.5f, 0.95f, 0.9f, 0.8f); }
            go.transform.position = pos;
            float sz = Mathf.Max(1f, plan.MeleeRadius * 2f);
            go.transform.localScale = new Vector3(sz, sz, 1f);
            go.AddComponent<SkillPrecastFx>();
        }
    }

    /// <summary>FX tạm: fade + tự destroy (precast/melee flash).</summary>
    public sealed class SkillPrecastFx : MonoBehaviour
    {
        private const float Duration = 0.25f;
        private float _t = Duration;

        private void Update()
        {
            _t -= Time.deltaTime;
            if (_t <= 0f) { Destroy(gameObject); return; }
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            var c = sr.color;
            c.a = Mathf.Clamp01(_t / Duration);
            sr.color = c;
        }
    }
}
