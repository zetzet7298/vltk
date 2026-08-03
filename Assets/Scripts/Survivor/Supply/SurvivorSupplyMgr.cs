// -----------------------------------------------------------------------------
// VLTK.Survivor — Supply: heal / bomb / magnet / full-clear (ticket 33).
// 4 supply slot, mỗi slot cooldown RIÊNG (own-design, LvlData PC toàn 0 — verify
// ticket 27 nên không đọc số từ PcSkills):
//  - Heal: SkillDef SupplyTag=Heal (lifereplenish_v/lifemax_v) — hồi qua impact 28
//    (BuffDot heal variant, SourceBuffer, KHÔNG vào damage ledger). Lượng hồi =
//    HealRatio × FinalMaxHp (attr nguồn từ HealAttr — player attr wire ticket 29).
//  - Bomb: SkillDef SupplyTag=Bomb (physicsdamage_v / *bomb.lua) — dmg vùng tâm
//    center, radius = AttackRadius px ÷ PxPerUnit (default khi def không có),
//    attribution SkillImpactSource{def.Id} (kill credit → ledger).
//  - Magnet: own (KHÔNG có nguồn JX skill — spec D2) — scale MagnetRadius của
//    gem hiện tại lên toàn màn (×MagnetRadiusScale) trong MagnetDuration, restore
//    khi hết giờ. Gem spawn sau khi active không được hút (ponytail: chấp nhận,
//    upgrade khi cần = director đọc MagnetActiveTime khi spawn gem).
//  - FullClear: own — dmg tất cả monster hiện tại.
// Fail-closed: SkillDef null / tag thiếu / slot chưa enabled → TryUse=false,
// Use* no-op — KHÔNG crash, KHÔNG bịa path/số. Magnet/FullClear luôn enabled
// (own feature, không cần def staged).
// Core thuần (Setup/Tick/TryUse/Use*) — EditMode test không scene (spec Testing
// Decisions). Wiring player/director = ticket 29 (HUD) qua public fields + list
// params; class không giữ scene ref.
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Survivor
{
    public enum SupplyKind
    {
        Heal = 0,
        Bomb = 1,
        Magnet = 2,
        FullClear = 3,
    }

    public sealed class SurvivorSupplyMgr
    {
        // ---- own-design balance (rationale file header) ----
        public const float HealCooldown = 10f;
        public const float BombCooldown = 12f;
        public const float MagnetCooldown = 18f;
        public const float FullClearCooldown = 30f;

        public const float HealRatio = 0.5f;          // hồi 50% FinalMaxHp (impact 28 heal variant)
        public const float BombDefaultRadius = 3.5f;  // units, khi def.AttackRadius = 0
        public const float BombDamage = 8f;
        public const float FullClearDamage = 6f;
        public const float MagnetDuration = 4f;
        public const float MagnetRadiusScale = 20f;   // 1.6×20 = 32u — phủ toàn arena (diagonal ~3.4u nửa)

        // own attribution ids khi slot không có SkillDef (magnet/full-clear/heal fallback)
        public const int HealSkillId = 9001;
        public const int BombSkillId = 9002;
        public const int MagnetSkillId = 9003;
        public const int FullClearSkillId = 9004;
        public const int HealBuffId = 9101;           // buff heal do supply tạo (không trùng skill ids)

        /// <summary>
        /// Adapter SurvivorPlayer → ISurvivorDamageable (supply heal target, ticket
        /// 43 wiring). Heal qua ApplyDot(IsHeal) — KHÔNG vào damage ledger (BuffDot
        /// heal variant).
        /// </summary>
        public sealed class SurvivorPlayerDamageable : ISurvivorDamageable
        {
            private readonly SurvivorPlayer _player;
            public SurvivorPlayerDamageable(SurvivorPlayer p) { _player = p; }
            public int Hp => _player != null ? _player.Hp : 0;
            public int MaxHp => _player != null ? _player.MaxHp : 0;
            public void ApplyDot(DamageInfo info)
            {
                if (_player == null) return;
                if (info.IsHeal) _player.Heal(Mathf.Max(1, info.Damage));
                else _player.TakeDamage(info.Damage);
            }
        }

        /// <summary>1 slot supply — trạng thái + cd riêng.</summary>
        public sealed class SupplySlot
        {
            public SupplyKind Kind;
            public bool Enabled;      // false = fail-closed (def chưa staged) → TryUse false
            public float Cooldown;    // giây, per-slot
            public float Remaining;   // cd còn lại; <= 0 = sẵn sàng
            public int SkillId;       // attribution; own id khi không có def
            public float Radius;      // bomb AoE radius (units) — chỉ dùng cho Bomb
        }

        // ---- wiring (ticket 29 HUD nối; class thuần không scene) ----
        public object Caster;                      // attribution bomb/full-clear (player)
        public ISurvivorDamageable HealTarget;    // target heal (player adapter — 29)
        public SurvivorActorAttr HealAttr;        // attr nguồn heal; null → dùng target.MaxHp

        private readonly SupplySlot[] _slots = new SupplySlot[4];
        public float MagnetActiveTime;            // > 0 = đang hút toàn màn
        private readonly List<XpGem> _magnetGems = new List<XpGem>();

        public SurvivorSupplyMgr()
        {
            // enabled mặc định: own features. Heal/Bomb chờ Setup(defs) — fail-closed.
            for (int i = 0; i < 4; i++)
            {
                var kind = (SupplyKind)i;
                _slots[i] = new SupplySlot
                {
                    Kind = kind,
                    Enabled = kind == SupplyKind.Magnet || kind == SupplyKind.FullClear,
                    Cooldown = CooldownFor(kind),
                    Remaining = 0f,
                    SkillId = OwnSkillId(kind),
                    Radius = BombDefaultRadius,
                };
            }
        }

        public SupplySlot GetSlot(SupplyKind kind) => _slots[(int)kind];

        /// <summary>
        /// Map SkillDef staged → slot. Aura = passive buff (KHÔNG phải supply slot, skip).
        /// defs null / không tag → Heal/Bomb disabled fail-closed (không crash, không bịa).
        /// </summary>
        public void Setup(IEnumerable<SkillDef> defs)
        {
            if (defs == null) return;
            foreach (var def in defs)
            {
                if (def == null) continue;
                switch (def.SupplyTag)
                {
                    case SurvivorSupplyTag.Heal:
                        GetSlot(SupplyKind.Heal).Enabled = true;
                        GetSlot(SupplyKind.Heal).SkillId = def.Id;
                        break;
                    case SurvivorSupplyTag.Bomb:
                        GetSlot(SupplyKind.Bomb).Enabled = true;
                        GetSlot(SupplyKind.Bomb).SkillId = def.Id;
                        GetSlot(SupplyKind.Bomb).Radius = def.AttackRadius > 0
                            ? def.AttackRadius / SkillCastRuntime.PxPerUnit : BombDefaultRadius;
                        break;
                    case SurvivorSupplyTag.Aura:
                        break; // passive buff — không có slot
                    default:
                        break;
                }
            }
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i].Remaining > 0f) _slots[i].Remaining -= dt;

            if (MagnetActiveTime <= 0f) return;
            MagnetActiveTime -= dt;
            if (MagnetActiveTime > 0f) return;
            // hết hiệu lực — restore radius gốc cho gem còn sống (gem đã pickup → Unity fake-null)
            foreach (var g in _magnetGems)
                if (g != null) g.Settings.MagnetRadius = CollectSettings.Default().MagnetRadius;
            _magnetGems.Clear();
        }

        /// <summary>
        /// Cooldown FSM: slot enabled + cd hết → đặt cd, trả true. Effect riêng (Use*)
        /// do caller chạy sau — tách để core test được không cần scene (spec Testing).
        /// </summary>
        public bool TryUse(SupplyKind kind)
        {
            var slot = GetSlot(kind);
            if (!slot.Enabled || slot.Remaining > 0f) return false;
            slot.Remaining = slot.Cooldown;
            return true;
        }

        // ------------------------------------------------------------------
        // Effects (fail-closed: dependency thiếu → no-op, không crash)
        // ------------------------------------------------------------------

        /// <summary>Heal qua impact 28: BuffDot heal variant, TickWhenAdd → áp NGAY.</summary>
        public bool UseHeal()
        {
            var slot = GetSlot(SupplyKind.Heal);
            if (!slot.Enabled || HealTarget == null) return false;
            var attr = HealAttr ?? new SurvivorActorAttr { BaseMaxHp = HealTarget.MaxHp };
            attr.Recompute(); // idempotent — đảm bảo Final* đúng trước khi BuffDot đọc

            var def = new BuffDef
            {
                BuffId = HealBuffId,
                TimeType = BuffTimeType.Infinit,
                ReplaceType = BuffReplaceType.Refresh,
            };
            var lvl = new BuffAttrConfig
            {
                StackNum = 1,
                DurTime = 0f,
                DotDamageData = new SkillAttrDamageData
                {
                    AttrType = ActorAttrDataType.MaxHp,
                    Param1 = HealRatio,
                    IsHeal = true,
                },
                DotTick = new BuffDotTickConfig(1f, tickWhenAdd: true, removeAfterDot: true),
            };
            def.Levels.Add(lvl);
            var inst = new BuffInstance { Def = def, Stack = 1, AttrConfig = lvl, Remaining = -1f };
            var dot = new BuffDot();
            dot.Init(inst, HealTarget, Caster, lvl.DotDamageData, lvl.DotTick,
                new SkillImpactSource(slot.SkillId, 0), null, null, attr);
            return true;
        }

        /// <summary>Bomb: dmg vùng quanh center, attribution skill id (kill credit → ledger).</summary>
        public void UseBomb(Vector2 center, List<SurvivorMonster> monsters)
        {
            var slot = GetSlot(SupplyKind.Bomb);
            if (!slot.Enabled || monsters == null) return;
            var src = new SkillImpactSource(slot.SkillId, 0);
            float r2 = slot.Radius * slot.Radius;
            for (int i = monsters.Count - 1; i >= 0; i--)
            {
                var m = monsters[i];
                if (m == null) continue;
                if (((Vector2)m.transform.position - center).sqrMagnitude > r2) continue;
                m.TakeDamage(BombDamage, src, Caster);
            }
        }

        /// <summary>Magnet: hút toàn màn — scale MagnetRadius gem hiện tại, restore khi hết giờ.</summary>
        public void UseMagnet(List<XpGem> gems)
        {
            if (gems == null) return;
            _magnetGems.Clear();
            _magnetGems.AddRange(gems);
            MagnetActiveTime = MagnetDuration;
            float boosted = CollectSettings.Default().MagnetRadius * MagnetRadiusScale;
            for (int i = 0; i < _magnetGems.Count; i++)
                if (_magnetGems[i] != null) _magnetGems[i].Settings.MagnetRadius = boosted;
        }

        /// <summary>FullClear: dmg TẤT CẢ monster hiện tại (không radius check).</summary>
        public void UseFullClear(List<SurvivorMonster> monsters)
        {
            var slot = GetSlot(SupplyKind.FullClear);
            if (monsters == null) return;
            var src = new SkillImpactSource(slot.SkillId, 0);
            for (int i = monsters.Count - 1; i >= 0; i--)
            {
                var m = monsters[i];
                if (m == null) continue;
                m.TakeDamage(FullClearDamage, src, Caster);
            }
        }

        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------

        public static float CooldownFor(SupplyKind kind)
        {
            switch (kind)
            {
                case SupplyKind.Heal: return HealCooldown;
                case SupplyKind.Bomb: return BombCooldown;
                case SupplyKind.Magnet: return MagnetCooldown;
                default: return FullClearCooldown;
            }
        }

        public static int OwnSkillId(SupplyKind kind)
        {
            switch (kind)
            {
                case SupplyKind.Heal: return HealSkillId;
                case SupplyKind.Bomb: return BombSkillId;
                case SupplyKind.Magnet: return MagnetSkillId;
                default: return FullClearSkillId;
            }
        }
    }
}
