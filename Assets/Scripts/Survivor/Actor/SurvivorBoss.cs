// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorBoss (ticket 31, boss multi-phase)
// Phase-switch DAMAGE-WINDOW keyed, KHÔNG timer. parity dhcd:
//   BossChangeBehaviorCmpt.OnHpChg(percent, isDecrease) →
//   GetJiangHuBossPhaseConfig(lossHp) → OnChangePhaseId(phaseConfig, damage);
//   JiangHuBossPhaseConfig = { Phase, MonsterAI, BossDamageMin, BossDamageMax,
//   BootyID, Skill[] } (research 06 §3, diffable-cs BossChangeBehaviorCmpt.cs /
//   JiangHuBossPhaseConfig.cs). lossHp = damage TÍCH LŨY vào boss, không phải
//   timer / skill-count.
//
// SurvivorMonster (ticket 34) là sealed → boss = WRAPPER cùng GO:
//   - inner SurvivorMonster đăng ký trong director.Monsters (đạn/melee player
//     hit đúng, ledger/kill-credit giữ nguyên — không sửa SurvivorMonster.cs).
//   - wrapper poll inner.Hp mỗi frame → loss = MaxHp − Hp → phase table lookup.
//   - AI mode Chase = inner.enabled (đuổi + contact damage P1); Cast = inner
//     disabled, wrapper giữ khoảng cách + cast skill qua SkillCastRuntime
//     (pool = SurvivorSkillPool.BossNpc từ ticket 26, subset theo phase
//     SkillIds — parity MonsterCfg.Skills / phase Skill[]).
//   - Chết → booty lớn: gem burst + DropTable roll theo BootyID của phase
//     active (parity BootyID → collect pool; SurvivorCollectItemMgr.RollActorDrop).
//
// Fail-closed: SkillCatalog rỗng / SkillIds rỗng → boss chỉ chase/kit (vẫn
// đổi phase + booty); melee skill (Form 12) trong subset → bỏ (hit player
// trực diện + child-missile visual = P2); DropTable null / BootyId ≤ 0 →
// chỉ gem burst. KHÔNG bịa skill id.
//
// Cast đạn dùng SurvivorEnemyProjectile (file này) — Projectile.cs (Combat/)
// chỉ hit Monsters, đạn boss phải hit SurvivorPlayer; không sửa Projectile.cs.
// Core thuần (BossPhaseTable / BossPhaseMachine) = test EditMode không scene.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>AI mode boss — parity MonsterAI (AI tree id PC); own: 2 mode.</summary>
    public enum BossAiMode
    {
        Chase,  // inner SurvivorMonster: đuổi + contact damage
        Cast,   // giữ khoảng cách + cast skill subset qua SkillCastRuntime
    }

    /// <summary>1 dòng phase — parity JiangHuBossPhaseConfig (MonsterAI→AiMode, Skill[]→SkillIds, BootyID→BootyId).</summary>
    [System.Serializable]
    public sealed class BossPhaseDef
    {
        public int Phase;
        public float BossDamageMin;
        /// <summary>Cửa sổ damage tích lũy [Min,Max] inclusive; 0 = open-ended (∞).</summary>
        public float BossDamageMax;
        public BossAiMode AiMode = BossAiMode.Chase;
        /// <summary>Subset skill id (SurvivorSkillPool.BossNpc). Rỗng → không cast (fail-closed).</summary>
        public int[] SkillIds = new int[0];
        /// <summary>Parity BootyID — drop pool khi chết ở phase này.</summary>
        public int BootyId;
    }

    /// <summary>Phase table lookup — pure (parity GetJiangHuBossPhaseConfig(lossHp)).</summary>
    public static class BossPhaseTable
    {
        /// <summary>lossHp nằm trong window [Min,Max] inclusive; Max ≤ 0 = open-ended.</summary>
        public static bool InWindow(BossPhaseDef p, float lossHp)
        {
            if (lossHp < p.BossDamageMin) return false;
            return p.BossDamageMax <= 0f || lossHp <= p.BossDamageMax;
        }

        /// <summary>
        /// Phase hiện tại = row cuối (scan order) có window chứa lossHp.
        /// Khoảng trống giữa 2 window → giữ phase trước (không regress).
        /// Table rỗng → −1 (không phase).
        /// </summary>
        public static int CurrentPhaseIndex(IReadOnlyList<BossPhaseDef> phases, float lossHp)
        {
            if (phases == null) return -1;
            // Phase hiện tại = row cuối đã MỞ (lossHp ≥ Min). Window chứa → chọn;
            // gap (vượt Max p trước, chưa tới Min p sau) → giữ phase đang mở gần nhất.
            int cur = -1;
            for (int i = 0; i < phases.Count; i++)
                if (lossHp >= phases[i].BossDamageMin) cur = i;
            return cur;
        }

        /// <summary>Lọc catalog (BossNpc pool) theo ids, giữ thứ tự ids; id thiếu → bỏ (fail-closed).</summary>
        public static List<SkillDef> Subset(IReadOnlyList<SkillDef> catalog, IReadOnlyList<int> ids)
        {
            var res = new List<SkillDef>();
            if (catalog == null || ids == null) return res;
            for (int i = 0; i < ids.Count; i++)
            {
                SkillDef found = null;
                for (int j = 0; j < catalog.Count; j++)
                    if (catalog[j].Id == ids[i]) { found = catalog[j]; break; }
                if (found != null) res.Add(found);
            }
            return res;
        }
    }

    /// <summary>State machine phase — pure, test EditMode. Phase chỉ đổi theo loss damage, không time.</summary>
    public sealed class BossPhaseMachine
    {
        public readonly List<BossPhaseDef> Phases;
        public int PhaseIndex { get; private set; } = -1;
        public float LossHp { get; private set; }

        public BossPhaseDef Current => PhaseIndex >= 0 && PhaseIndex < Phases.Count ? Phases[PhaseIndex] : null;

        public BossPhaseMachine(List<BossPhaseDef> phases)
        {
            Phases = phases ?? new List<BossPhaseDef>();
        }

        /// <summary>
        /// Báo HP hiện tại. loss = max(0, maxHp − hp). HP tăng (heal) → không
        /// regress phase (monster P1 không heal; fail-closed). true = phase switch.
        /// </summary>
        public bool ReportHp(float maxHp, float hp)
        {
            float loss = Mathf.Max(0f, maxHp - hp);
            if (loss < LossHp) return false;
            LossHp = loss;
            int idx = BossPhaseTable.CurrentPhaseIndex(Phases, LossHp);
            if (idx < 0 || idx == PhaseIndex) return false;
            PhaseIndex = idx;
            return true;
        }
    }

    /// <summary>
    /// Boss wrapper (cùng GO với inner SurvivorMonster). Spawn qua
    /// SurvivorGameDirector.SpawnMonsterAt (info.IsBoss). Phase table mặc định
    /// = DefaultPhases(); scene author gán BossPhases/BossSkillPool/DropTable
    /// trên director.
    /// </summary>
    public sealed class SurvivorBoss : MonoBehaviour
    {
        public List<BossPhaseDef> Phases = new List<BossPhaseDef>();
        /// <summary>Boss/npc skill pool (ticket 26). Rỗng → không cast (fail-closed).</summary>
        public List<SkillDef> SkillCatalog = new List<SkillDef>();
        /// <summary>Inner SurvivorMonster — registry trong director.Monsters.</summary>
        public SurvivorMonster Monster;

        [Header("Booty (own balance — boss xứng đáng)")]
        public int BootyGems = 8;       // gem burst ngoài XpDrop của inner
        public int BootyGemAmount = 3;  // xp/gem
        [Header("Cast AI (own)")]
        public float CastRange = 2.2f;  // khoảng cách giữ
        public float MinRange = 1.5f;   // lùi khi quá gần
        public float CastSpeedMul = 0.8f;

        /// <summary>BootyID phase active lúc chết — director dùng roll DropTable.</summary>
        public int BootyId { get; private set; }
        public int PhaseIndex => _machine != null ? _machine.PhaseIndex : -1;
        public float TotalLossHp => _machine != null ? _machine.LossHp : 0f;

        private BossPhaseMachine _machine;
        private SkillCastRuntime _skills;
        private bool _bootySpawned;

        /// <summary>
        /// Phase table default (own, boss 30 HP = tier 10 × base 3):
        /// P1 [0,10] chase — 1/3 máu đầu tập đánh; P2 [11,20] cast — giữ khoảng
        /// cách + skill; P3 [21,∞] chase enrage — phase cuối trước khi chết
        /// (booty = BootyID phase này). SkillIds rỗng mặc định — catalog thật
        /// nối khi ticket 29/33 wire pool runtime.
        /// </summary>
        public static List<BossPhaseDef> DefaultPhases()
        {
            return new List<BossPhaseDef>
            {
                new BossPhaseDef { Phase = 1, BossDamageMin = 0f,  BossDamageMax = 10f, AiMode = BossAiMode.Chase, BootyId = 1001 },
                new BossPhaseDef { Phase = 2, BossDamageMin = 11f, BossDamageMax = 20f, AiMode = BossAiMode.Cast,  BootyId = 1002 },
                new BossPhaseDef { Phase = 3, BossDamageMin = 21f, BossDamageMax = 0f,  AiMode = BossAiMode.Chase, BootyId = 1003 },
            };
        }

        public void Init(SurvivorMonster inner, List<BossPhaseDef> phases, List<SkillDef> catalog)
        {
            Monster = inner;
            Phases = phases != null ? phases : DefaultPhases();
            SkillCatalog = catalog != null ? catalog : new List<SkillDef>();
            _machine = new BossPhaseMachine(Phases);
            _skills = new SkillCastRuntime();
            _bootySpawned = false;
            // lossHp 0 → phase 1 ngay (window đầu Min = 0)
            if (_machine.ReportHp(inner.MaxHp, inner.Hp)) ApplyPhase(_machine.PhaseIndex);
        }

        private void Update()
        {
            var m = Monster;
            if (m == null || _bootySpawned) return;
            // ticket 43 (council FAIL): ReportHp TRƯỚC death check — đòn chí mạng
            // phải chạm window phase cuối (spawn booty phase active đúng) trước khi
            // Update này thấy Hp <= 0. hp clamp 0 để loss = maxHp (phase cuối).
            if (_skills != null) _skills.Tick(Time.deltaTime);
            if (_machine != null && _machine.ReportHp(m.MaxHp, Mathf.Max(0f, m.Hp))) ApplyPhase(_machine.PhaseIndex);
            // death poll: nếu Update wrapper chạy sau đòn chí mạng trong frame —
            // inner Die() đã Destroy GO (deferred end-of-frame) → vẫn spawn booty.
            // Update chạy TRƯỚC đòn chí mạng → OnDestroy fallback bắt (dưới).
            if (m.Hp <= 0f) { SpawnBooty(); return; }
            var def = _machine != null ? _machine.Current : null;
            if (def != null && def.AiMode == BossAiMode.Cast) DoCastAi(m);
        }

        private void OnDestroy()
        {
            // fallback death: boss chết trong frame mà Update wrapper chạy trước
            // đòn chí mạng → OnDestroy end-of-frame vẫn bắt được (guard Hp + flag).
            // Scene unload/cleanup (boss còn sống) → Hp > 0 → skip.
            if (Monster != null && !_bootySpawned && Monster.Hp <= 0f) SpawnBooty();
        }

        private void ApplyPhase(int idx)
        {
            if (_machine == null || idx < 0 || idx >= Phases.Count) return;
            var def = Phases[idx];
            BootyId = def.BootyId; // parity BootyID — booty khi chết ở phase này
            if (def.AiMode == BossAiMode.Cast)
            {
                if (Monster != null) Monster.enabled = false;
                RebuildRoster(def.SkillIds);
            }
            else if (Monster != null)
            {
                Monster.enabled = true;
            }
            Debug.Log($"[SurvivorBoss] phase {def.Phase} loss={_machine.LossHp:F1} mode={def.AiMode} skills={def.SkillIds.Length} booty={def.BootyId}");
        }

        private void RebuildRoster(int[] ids)
        {
            _skills.Roster.Clear();
            var subset = BossPhaseTable.Subset(SkillCatalog, ids);
            for (int i = 0; i < subset.Count; i++)
            {
                // melee boss skill = P2 (child-missile visual + hit player trực diện) — skip fail-closed
                if (subset[i].Form == 12) continue;
                _skills.Learn(subset[i], 1);
            }
        }

        private void DoCastAi(SurvivorMonster m)
        {
            var d = SurvivorGameDirector.Instance;
            if (d == null || d.Player == null) return;
            var toPlayer = (Vector2)(d.Player.transform.position - transform.position);
            float dist = toPlayer.magnitude;
            var dir = dist > 0.001f ? toPlayer.normalized : Vector2.right;
            Vector2 move = Vector2.zero;
            if (dist < MinRange) move = -dir;
            else if (dist > CastRange) move = dir;
            if (move.sqrMagnitude > 0.001f)
                transform.position += (Vector3)(move * (m.Speed * CastSpeedMul * Time.deltaTime));
            if (_skills != null && _skills.TryCast(dir, out var plan)) SpawnCast(plan);
        }

        /// <summary>Cast → đạn boss (SurvivorEnemyProjectile hit player). Ranged only (melee đã lọc roster).</summary>
        private void SpawnCast(SkillCastPlan plan)
        {
            var d = SurvivorGameDirector.Instance;
            if (d == null) return;
            for (int i = 0; i < plan.Missiles.Length; i++)
            {
                var go = new GameObject("boss_proj");
                var p = go.AddComponent<SurvivorEnemyProjectile>();
                p.Init(transform.position + (Vector3)plan.Missiles[i].Offset,
                    plan.Missiles[i].Dir, plan.Damage,
                    plan.MissileSpeed, plan.MissileLife, plan.MissileSprUid);
            }
        }

        private void SpawnBooty()
        {
            if (_bootySpawned) return;
            _bootySpawned = true;
            SurvivorGameDirector.Instance?.OnBossKilled(this);
        }
    }

    /// <summary>
    /// Đạn boss — hit SurvivorPlayer (Projectile.cs chỉ hit Monsters, không
    /// đụng). Visual child missile staged qua SpriteLoader; thiếu → proxy màu
    /// (fail-closed, KHÔNG bịa path).
    /// </summary>
    public sealed class SurvivorEnemyProjectile : MonoBehaviour
    {
        private Vector2 _dir;
        private float _speed;
        private float _damage;
        private float _life;
        private float _t;

        public void Init(Vector3 pos, Vector2 dir, float dmg, float speed, float life, string spriteUid)
        {
            transform.position = pos;
            _dir = dir;
            _damage = dmg;
            _speed = speed;
            _life = life;
            _t = 0f;
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ProxyVisuals.White();
            sr.color = new Color(1f, 0.35f, 0.2f);
            sr.drawMode = SpriteDrawMode.Simple;
            transform.localScale = new Vector3(0.32f, 0.32f, 1f);
            if (!string.IsNullOrEmpty(spriteUid))
            {
                var sp = SpriteLoader.Resolve(spriteUid);
                if (sp != null) { sr.sprite = sp; sr.color = Color.white; }
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.position += (Vector3)(_dir * _speed * dt);
            _t += dt;
            var d = SurvivorGameDirector.Instance;
            if (_t > _life || d == null || d.Player == null) { Destroy(gameObject); return; }
            if ((d.Player.transform.position - transform.position).sqrMagnitude < 0.36f)
            {
                d.Player.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(_damage)));
                Destroy(gameObject);
            }
        }
    }
}
