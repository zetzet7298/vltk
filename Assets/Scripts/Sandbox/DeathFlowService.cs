// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.2 Death Flow Service
// PC Death flow and respawn logic matching KNpc::OnDeath.
// Source: PC death penalties, loot drops, exp rewards.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public struct DeathEvent
    {
        public int victimId;
        public int killerId;
        public bool isPlayer;
        public long expReward;
    }

    /// <summary>
    /// Service xử lý vòng đời khi NPC/Quái hoặc Người chơi chết.
    /// PC source: KNpc::OnDeath / KNpc::Revive.
    /// </summary>
    public class DeathFlowService
    {
        private readonly PlayerLevelService _levelService;

        /// <summary>Event kích hoạt khi có đối tượng tử trận.</summary>
        public event Action<DeathEvent> OnDeath;

        /// <summary>Event kích hoạt khi đối tượng hồi sinh.</summary>
        public event Action<int, Vector2> OnRespawn; // (actorId, position)

        public DeathFlowService(PlayerLevelService levelService = null)
        {
            _levelService = levelService;
        }

        /// <summary>
        /// Xử lý tử trận cho một CombatActorState.
        /// </summary>
        public void ProcessDeath(CombatActorState victim, CombatActorState killer)
        {
            if (victim == null) return;

            bool isPlayer = victim.actorId == SandboxManager.PlayerActorId; // Mặc định player actorId = 1
            long expGranted = 0;

            if (isPlayer)
            {
                ProcessPlayerDeath(victim);
            }
            else
            {
                expGranted = CalculateExpReward(victim.level, killer != null ? killer.level : 1);
                if (killer != null && killer.actorId == SandboxManager.PlayerActorId && _levelService != null)
                {
                    _levelService.AddExp(expGranted);
                }
                ProcessNpcDeath(victim, killer);
            }

            OnDeath?.Invoke(new DeathEvent
            {
                victimId = victim.actorId,
                killerId = killer != null ? killer.actorId : 0,
                isPlayer = isPlayer,
                expReward = expGranted
            });
        }

        /// <summary>
        /// Người chơi chết: trừ % kinh nghiệm của cấp độ hiện tại (hình phạt tử trận PC).
        /// Hồi sinh sau 5 giây.
        /// </summary>
        public void ProcessPlayerDeath(CombatActorState player)
        {
            SubsystemLog.Info("DeathFlow", $"Player death. Applying JX PC exp penalty.");

            if (_levelService != null)
            {
                // Trừ 2% exp của cấp hiện tại (PC JX1 penalty)
                long req = PlayerStatService.GetExpRequired(_levelService.Level);
                long penalty = Mathf.RoundToInt(req * 0.02f);
                _levelService.AddExp(-penalty);
            }

            player.currentLife = 0;
            player.fightMode = false; // Tạm thời rời trạng thái chiến đấu

            // Giả lập hồi sinh tại Ba Lăng Huyện (MPS: 200, 200) sau 5 giây
            Vector2 respawnPoint = BaLangEnemyDatabase.MpsToWorld(1600 * 8, 3200 * 8); // Tọa độ thị trấn Ba Lăng
            TriggerRespawn(player, respawnPoint, 5f);
        }

        /// <summary>
        /// Quái chết: rơi đồ (DropRateFile) và hồi sinh sau cooldown.
        /// </summary>
        public void ProcessNpcDeath(CombatActorState npc, CombatActorState killer)
        {
            npc.currentLife = 0;
            SubsystemLog.Info("DeathFlow", $"NPC {npc.actorId} slayed. Generating drop tables.");

            // Giả lập drop
            // PC: check DropRateFile và tạo ItemInstance tại tọa độ quái

            // Hồi sinh quái sau 10 giây tại vị trí cũ
            TriggerRespawn(npc, npc.position, 10f);
        }

        // ── Helper ─────────────────────────────────────────────────────────

        private long CalculateExpReward(int npcLevel, int killerLevel)
        {
            // Công thức PC: Exp = Base(npcLevel) * hệ số chênh lệch cấp độ
            long baseExp = npcLevel * 10;
            int diff = npcLevel - killerLevel;

            if (diff >= 5) return baseExp * 2;       // Vượt cấp sát quái
            if (diff <= -5) return baseExp / 10;     // Quá outlevel quái
            return baseExp;
        }

        private void TriggerRespawn(CombatActorState actor, Vector2 targetPos, float delaySeconds)
        {
            // Trong game thực tế dùng Coroutine hoặc timer.
            // Ở service pure C# này, chúng ta mô phỏng hồi sinh lập tức hoặc qua callback.
            actor.currentLife = actor.maxLife;
            actor.position = targetPos;
            actor.fightMode = true;

            OnRespawn?.Invoke(actor.actorId, targetPos);
        }
    }
}
