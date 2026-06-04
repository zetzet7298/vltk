// -----------------------------------------------------------------------------
// VLTK Mobile — Gameplay Loop Integration
// Wires: Combat → Damage → Death → Respawn → Reward → Progression → Equipment
// This is the central orchestrator connecting all sandbox services.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    // ── Actor Runtime State (NPC + Player) ─────────────────────────────────

    public sealed class GameplayActor
    {
        public int actorId;
        public string nameVi;
        public bool isPlayer;
        public CombatActorState combat;
        public Vector2 worldPos;
        public GameObject view;           // Visual GameObject (sprite, nameplate, etc.)
        public int npcTemplateId;         // 0 = player
        public int level;
        public float deathTimestamp = -1f; // Khi nào chết
        public float respawnDelay = 5f;
        public bool isDead => combat != null && combat.currentLife <= 0;

        public GameplayActor(int id, string name, bool player = false)
        {
            actorId = id;
            nameVi = name;
            isPlayer = player;
            combat = new CombatActorState
            {
                actorId = id,
                level = 1,
                currentLife = 100,
                maxLife = 100,
                currentMana = 100,
                position = Vector2.zero,
            };
        }
    }

    // ── Gameplay Loop Events ───────────────────────────────────────────────

    public struct GameplayDamageEvent
    {
        public int attackerId;
        public int targetId;
        public int damage;
        public DamageType type;
        public int skillId;
    }

    public struct GameplayDeathEvent
    {
        public int victimId;
        public string victimNameVi;
        public int killerId;
        public bool isPlayer;
        public long expReward;
        public int silverReward;
    }

    public struct GameplayRespawnEvent
    {
        public int actorId;
        public Vector2 position;
    }

    public struct GameplayLevelUpEvent
    {
        public int actorId;
        public int oldLevel;
        public int newLevel;
    }

    // ── Core Integration Service ───────────────────────────────────────────

    /// <summary>
    /// Dịch vụ trung tâm nối tất cả subsystem thành gameplay loop hoàn chỉnh.
    /// Flow: Input → Skill Cast → Damage → Death → Reward → Progression → Respawn
    /// 
    /// Subservices managed:
    /// - CombatRuntimeService (skill cast, damage, projectiles)
    /// - DeathFlowService (death penalty, loot)
    /// - PlayerLevelService (EXP, level up)
    /// - PlayerStatService (stat calculation)
    /// - AutoTargetService (target selection)
    /// - PkCombatService (PK rules)
    /// - EconomyService (currency)
    /// - EnhanceRefineService (item upgrade)
    /// - BossHoangKimService (world bosses)
    /// </summary>
    public class GameplayLoopService
    {
        // ── Sub-services ───────────────────────────────────────────────────

        public CombatRuntimeService Combat { get; }
        public DeathFlowService DeathFlow { get; }
        public PlayerLevelService LevelService { get; }
        // AutoTargetService is static — use AutoAcquireTarget() instead
        public PkCombatService PkRules { get; }
        public EconomyService Economy { get; }
        public BossHoangKimService BossService { get; }
        public TongJinBattleService TongJin { get; }
        public BangChienService BangChien { get; }

        // ── Actor Registry ─────────────────────────────────────────────────

        private readonly Dictionary<int, GameplayActor> _actors = new();
        private readonly List<GameplayActor> _enemies = new();
        private GameplayActor _player;
        private float _gameTime;
        private float _manaRegenAccumulator;

        public IReadOnlyDictionary<int, GameplayActor> Actors => _actors;
        public IReadOnlyList<GameplayActor> Enemies => _enemies;
        public GameplayActor Player => _player;

        // ── Events ─────────────────────────────────────────────────────────

        public event Action<GameplayDamageEvent> OnDamage;
        public event Action<GameplayDeathEvent> OnDeath;
        public event Action<GameplayRespawnEvent> OnRespawn;
        public event Action<GameplayLevelUpEvent> OnLevelUp;

        // ── Init ───────────────────────────────────────────────────────────

        public GameplayLoopService(
            SkillCatalog catalog,
            int playerFactionId = 5,
            int playerBangId = 0,
            int initialSilver = 1000)
        {
            Combat = new CombatRuntimeService(catalog);
            LevelService = new PlayerLevelService();
            DeathFlow = new DeathFlowService(LevelService);
            PkRules = new PkCombatService(playerFactionId, playerBangId);
            Economy = new EconomyService(maxStashSlots: 100, initialSilver);
            BossService = new BossHoangKimService();
            TongJin = new TongJinBattleService();
            BangChien = new BangChienService();

            // Wire DeathFlow events
            DeathFlow.OnDeath += HandleDeath;
            DeathFlow.OnRespawn += (actorId, pos) =>
            {
                OnRespawn?.Invoke(new GameplayRespawnEvent { actorId = actorId, position = pos });
                SubsystemLog.Info("Gameplay", $"Actor {actorId} respawn tại ({pos.x:F0}, {pos.y:F0})");
            };

            // Wire Boss events
            BossService.OnBossKilled += (boss, killerId) =>
            {
                Economy.EarnSilver(boss.killRewardSilver);
                SubsystemLog.Info("Gameplay", $"Boss {boss.nameVi} bị giết! +{boss.killRewardSilver} Bạc, +{boss.killRewardExp} EXP");
            };

            // Wire Tong Jin events
            TongJin.OnMatchEnded += state =>
            {
                SubsystemLog.Info("Gameplay", $"Tống Kim kết thúc: Tống {state.songScore} - Kim {state.jinScore}");
            };

            // Wire Bang Chien events
            BangChien.OnBangChienEnded += (winner, s1, s2) =>
            {
                SubsystemLog.Info("Gameplay", $"Bang Chiến kết thúc: {s1}-{s2}. Winner: Bang {winner}");
            };
        }

        // ── Actor Management ───────────────────────────────────────────────

        /// <summary>Đăng ký player vào gameplay loop.</summary>
        public GameplayActor RegisterPlayer(int actorId, string nameVi, int level, Vector2 pos)
        {
            _player = new GameplayActor(actorId, nameVi, player: true)
            {
                level = level,
                worldPos = pos,
            };
            _player.combat.level = level;
            _player.combat.faction = CombatFaction.CaiBang;
            _player.combat.currentLife = CalculateMaxLife(level);
            _player.combat.maxLife = _player.combat.currentLife;
            _player.combat.currentMana = 100;
            _player.combat.position = pos;

            _actors[actorId] = _player;
            SubsystemLog.Info("Gameplay", $"Player '{nameVi}' Lv{level} đã đăng ký");
            return _player;
        }

        /// <summary>Đăng ký enemy/NPC vào gameplay loop.</summary>
        public GameplayActor RegisterEnemy(int actorId, string nameVi, int templateId, int level, Vector2 pos, CombatFaction faction = CombatFaction.None)
        {
            var enemy = new GameplayActor(actorId, nameVi)
            {
                npcTemplateId = templateId,
                level = level,
                worldPos = pos,
            };
            enemy.combat.level = level;
            enemy.combat.faction = faction;
            enemy.combat.currentLife = CalculateMaxLife(level);
            enemy.combat.maxLife = enemy.combat.currentLife;
            enemy.combat.position = pos;
            enemy.respawnDelay = 30f; // 30s respawn

            _actors[actorId] = enemy;
            _enemies.Add(enemy);
            return enemy;
        }

        /// <summary>Xóa enemy khỏi loop.</summary>
        public void RemoveActor(int actorId)
        {
            _actors.Remove(actorId);
            _enemies.RemoveAll(e => e.actorId == actorId);
        }

        // ── Core Gameplay Actions ──────────────────────────────────────────

        /// <summary>Player dùng skill tấn công target.</summary>
        public CombatCastReport PlayerCastSkill(int skillId, int? targetActorId = null, Vector2? targetPoint = null)
        {
            if (_player == null || _player.isDead) return null;

            var target = targetActorId.HasValue && _actors.TryGetValue(targetActorId.Value, out var t) ? t : null;
            var relation = DetermineRelation(_player, target);
            var tPoint = targetPoint ?? (target != null ? target.worldPos : _player.worldPos);

            var report = Combat.Cast(
                _player.combat,
                target?.combat,
                skillId,
                tPoint,
                relation);

            if (report.success && target != null && report.damageResults.Count > 0)
            {
                ApplyDamageResults(_player, target, report.damageResults, skillId);
            }

            return report;
        }

        /// <summary>Enemy tấn công player (auto-attack).</summary>
        public void EnemyAttackPlayer(GameplayActor enemy)
        {
            if (_player == null || _player.isDead || enemy.isDead) return;

            // Simple melee damage
            int damage = UnityEngine.Random.Range(enemy.combat.minDamage, enemy.combat.maxDamage + 1);
            _player.combat.currentLife = Mathf.Max(0, _player.combat.currentLife - damage);

            OnDamage?.Invoke(new GameplayDamageEvent
            {
                attackerId = enemy.actorId,
                targetId = _player.actorId,
                damage = damage,
                type = DamageType.Physics,
                skillId = 0,
            });

            if (_player.combat.currentLife <= 0)
                ProcessActorDeath(_player, enemy);
        }

        /// <summary>Kiểm tra PK trước khi tấn công.</summary>
        public PkResult CheckPkAndAttack(CombatActorState attacker, CombatActorState target)
        {
            var pkResult = PkRules.CanAttack(attacker, target);
            if (pkResult.canAttack && pkResult.karmaChange > 0)
                PkRules.ApplyKillPenalty(pkResult);
            return pkResult;
        }

        // ── Auto Target ────────────────────────────────────────────────────

        /// <summary>Tìm target gần nhất trong phạm vi.</summary>
        public GameplayActor FindNearestEnemy(Vector2 fromPos, float range)
        {
            GameplayActor best = null;
            float bestDist = range * range;

            foreach (var enemy in _enemies)
            {
                if (enemy.isDead) continue;
                float distSq = Vector2.SqrMagnitude(enemy.worldPos - fromPos);
                if (distSq < bestDist)
                {
                    bestDist = distSq;
                    best = enemy;
                }
            }
            return best;
        }

        /// <summary>Tìm target bằng AutoTargetService.</summary>
        public GameplayActor AutoAcquireTarget(Vector2 playerPos, float range)
        {
            var candidates = new List<CombatActorState>();
            var idMap = new Dictionary<CombatActorState, int>();

            foreach (var enemy in _enemies)
            {
                if (enemy.isDead) continue;
                candidates.Add(enemy.combat);
                idMap[enemy.combat] = enemy.actorId;
            }

            var target = AutoTargetService.FindBestTarget(playerPos, range, candidates);
            if (target != null && idMap.TryGetValue(target, out var id))
                return _actors.TryGetValue(id, out var actor) ? actor : null;
            return null;
        }

        // ── Tick / Update ──────────────────────────────────────────────────

        /// <summary>Gọi mỗi frame.</summary>
        public void Tick(float deltaTime)
        {
            _gameTime += deltaTime;

            // Combat tick (PC 18fps)
            Combat.AdvanceTime(Mathf.FloorToInt(deltaTime * 18f));

            // Mana regen (PC: 1 mana/tick)
            _manaRegenAccumulator += deltaTime;
            if (_manaRegenAccumulator >= 0.5f)
            {
                _manaRegenAccumulator -= 0.5f;
                if (_player != null && !_player.isDead)
                    _player.combat.currentMana = Mathf.Min(_player.combat.maxLife, _player.combat.currentMana + 1);
            }

            // Enemy AI: attack player if in range
            foreach (var enemy in _enemies)
            {
                if (enemy.isDead) continue;
                if (_player == null || _player.isDead) continue;

                float dist = Vector2.Distance(enemy.worldPos, _player.worldPos);
                if (dist < 64f) // PC attack range
                {
                    EnemyAttackPlayer(enemy);
                }
            }

            // Respawn dead enemies
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _enemies[i];
                if (enemy.isDead && enemy.deathTimestamp >= 0)
                {
                    if (_gameTime - enemy.deathTimestamp >= enemy.respawnDelay)
                        RespawnActor(enemy);
                }
            }

            // Player death: auto-respawn after 5s
            if (_player != null && _player.isDead && _player.deathTimestamp >= 0)
            {
                if (_gameTime - _player.deathTimestamp >= _player.respawnDelay)
                    RespawnActor(_player);
            }

            // Boss timers
            BossService.Tick(deltaTime);
            TongJin.Tick(deltaTime);
        }

        // ── Internal ───────────────────────────────────────────────────────

        private void ApplyDamageResults(GameplayActor attacker, GameplayActor target, List<DamageResult> results, int skillId)
        {
            int totalDamage = 0;
            foreach (var r in results)
                totalDamage += r.finalDamage;

            OnDamage?.Invoke(new GameplayDamageEvent
            {
                attackerId = attacker.actorId,
                targetId = target.actorId,
                damage = totalDamage,
                type = DamageType.Physics,
                skillId = skillId,
            });

            // Check death
            if (target.combat.currentLife <= 0)
                ProcessActorDeath(target, attacker);
        }

        private void ProcessActorDeath(GameplayActor victim, GameplayActor killer)
        {
            victim.deathTimestamp = _gameTime;

            bool isPlayer = victim.isPlayer;
            long expReward = 0;
            int silverReward = 0;

            if (!isPlayer)
            {
                // EXP reward based on victim level
                expReward = CalculateExpReward(victim.level, killer?.level ?? 1);
                silverReward = victim.level * 5;

                // Grant to player
                if (killer != null && killer.isPlayer)
                {
                    LevelService.AddExp(expReward);
                    Economy.EarnSilver(silverReward);

                    // Check level up
                    int oldLevel = killer.level;
                    killer.level = LevelService.Level;
                    killer.combat.level = killer.level;
                    if (killer.level > oldLevel)
                    {
                        OnLevelUp?.Invoke(new GameplayLevelUpEvent
                        {
                            actorId = killer.actorId,
                            oldLevel = oldLevel,
                            newLevel = killer.level,
                        });
                        SubsystemLog.Info("Gameplay", $"LEVEL UP! {oldLevel} → {killer.level}");

                        // Update max life on level up
                        killer.combat.maxLife = CalculateMaxLife(killer.level);
                        killer.combat.currentLife = killer.combat.maxLife;
                    }
                }

                // Check if boss
                if (BossService.IsBossAlive(victim.npcTemplateId) == false)
                {
                    // Already handled by BossService event
                }
            }

            OnDeath?.Invoke(new GameplayDeathEvent
            {
                victimId = victim.actorId,
                victimNameVi = victim.nameVi,
                killerId = killer?.actorId ?? 0,
                isPlayer = isPlayer,
                expReward = expReward,
                silverReward = silverReward,
            });

            SubsystemLog.Info("Gameplay", $"{victim.nameVi} bị giết bởi {killer?.nameVi ?? "???"} +{expReward}EXP +{silverReward}Bạc");
        }

        private void RespawnActor(GameplayActor actor)
        {
            actor.deathTimestamp = -1f;
            actor.combat.currentLife = actor.combat.maxLife;
            actor.combat.currentMana = 100;

            OnRespawn?.Invoke(new GameplayRespawnEvent
            {
                actorId = actor.actorId,
                position = actor.worldPos,
            });
        }

        private void HandleDeath(DeathEvent e)
        {
            // Internal forwarding from DeathFlowService
        }

        private CombatRelation DetermineRelation(GameplayActor attacker, GameplayActor target)
        {
            if (target == null) return CombatRelation.Self;
            if (attacker.isPlayer && !target.isPlayer) return CombatRelation.Enemy;
            if (!attacker.isPlayer && target.isPlayer) return CombatRelation.Enemy;
            return CombatRelation.Ally;
        }

        private static int CalculateMaxLife(int level) => 100 + level * 20;

        private static long CalculateExpReward(int victimLevel, int killerLevel)
        {
            // PC: EXP scales with victim level, reduced if killer is much higher
            long baseExp = victimLevel * 50;
            int diff = killerLevel - victimLevel;
            if (diff > 10) baseExp = baseExp * 50 / 100; // 50% penalty
            if (diff > 20) baseExp = baseExp * 20 / 100; // 80% penalty
            return (long)Mathf.Max(1, baseExp);
        }

        // ── Query Helpers ──────────────────────────────────────────────────

        public GameplayActor GetActor(int actorId) =>
            _actors.TryGetValue(actorId, out var a) ? a : null;

        /// <summary>Tổng trạng thái hiện tại (cho HUD/Debug).</summary>
        public string GetStatusSummary()
        {
            if (_player == null) return "Chưa đăng ký player";
            int aliveEnemies = _enemies.FindAll(e => !e.isDead).Count;
            return $"Lv{_player.level} HP:{_player.combat.currentLife}/{_player.combat.maxLife} " +
                   $"MP:{_player.combat.currentMana} Bạc:{Economy.Wallet.silver} " +
                   $"Quái sống:{aliveEnemies}/{_enemies.Count} " +
                   $"EXP:{LevelService.CurrentExp}";
        }
    }
}
