// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.4 PK / Tống Kim / Boss Hoàng Kim / Bang Chiến
// PC source: PK rules, Tống Kim battlefield, Boss Hoàng Kim spawn, Bang chiến.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public enum PkMode
    {
        Peace = 0,      // Hòa bình — không đánh ai
        Free = 1,        // Tự do — đánh tất cả
        Team = 2,        // Tổ đội — đánh ngoài team
        Faction = 3,     // Môn phái — đánh ngoài phái
        Bang = 4,         // Bang hội — đánh ngoài bang
    }

    public enum PkPenaltyType
    {
        None,
        RedName,          // Đỏ tên
        KarmaIncrease,    // Tăng sát khí
        GuardAttack,      // Bị vệ binh tấn công
    }

    [Serializable]
    public class PkResult
    {
        public bool canAttack;
        public PkPenaltyType penalty;
        public int karmaChange;
        public string reasonVi;
    }

    /// <summary>
    /// Service quản lý chế độ PK, sát khí, và luật giao chiến.
    /// PC source: KNpc::IsEnemy, PK mode, RedName/Karma system.
    /// </summary>
    public class PkCombatService
    {
        private PkMode _mode = PkMode.Peace;
        private int _karma;       // Sát khí (>0 = đỏ tên)
        private int _bangId;      // Bang hội ID (0 = chưa gia nhập)
        private int _factionId;
        private int _actorId = 0; // 0 = uninitialized
        private IPkCombatHost _host;

        public PkMode Mode => _mode;
        public int Karma => _karma;
        public bool IsRedName => _karma > 0;
        public int ActorId => _actorId;

        public event Action<PkMode> OnPkModeChanged;
        public event Action<int> OnKarmaChanged;

        public PkCombatService() : this(0, 0, 0, null) { }
        public PkCombatService(int factionId, int bangId) : this(factionId, bangId, 0, null) { }
        public PkCombatService(int factionId, int bangId, int actorId) : this(factionId, bangId, actorId, null) { }
        public PkCombatService(int factionId, int bangId, int actorId, IPkCombatHost host)
        {
            _factionId = factionId;
            _bangId = bangId;
            _actorId = actorId;
            _host = host;
        }

        public void AttachHost(IPkCombatHost host) { _host = host; }

        /// <summary>Chuyển chế độ PK.</summary>
        public void SetPkMode(PkMode mode)
        {
            PkMode oldMode = _mode;
            _mode = mode;
            OnPkModeChanged?.Invoke(mode);
            if (_host != null)
            {
                _host.OnPkModeChanged(oldMode, mode);
                _host.LogPkEvent(_actorId, $"Chuyển chế độ PK: {ModeNameVi(oldMode)} → {ModeNameVi(mode)}");
                _host.SaveKarma(_actorId, _karma, mode);
            }
            SubsystemLog.Info("PK", $"Chế độ PK: {ModeNameVi(mode)}");
        }

        /// <summary>Kiểm tra có thể tấn công target không.</summary>
        public PkResult CanAttack(CombatActorState attacker, CombatActorState target)
        {
            var result = new PkResult();

            if (target == null || target.actorId == attacker.actorId)
            {
                result.canAttack = false;
                result.reasonVi = "Không thể tự đánh mình";
                return result;
            }

            if (target.currentLife <= 0)
            {
                result.canAttack = false;
                result.reasonVi = "Mục tiêu đã chết";
                return result;
            }

            switch (_mode)
            {
                case PkMode.Peace:
                    result.canAttack = false;
                    result.reasonVi = "Đang chế độ Hòa Bình";
                    break;

                case PkMode.Free:
                    result.canAttack = true;
                    result.penalty = PkPenaltyType.KarmaIncrease;
                    result.karmaChange = 10;
                    if ((int)target.faction > 0 && (int)target.faction != _factionId)
                        result.karmaChange = 5; // Giảm penalty nếu khác phái
                    break;

                case PkMode.Team:
                    result.canAttack = attacker.partyId == 0 || attacker.partyId != target.partyId;
                    result.reasonVi = result.canAttack ? "" : "Cùng tổ đội";
                    result.penalty = result.canAttack ? PkPenaltyType.KarmaIncrease : PkPenaltyType.None;
                    result.karmaChange = result.canAttack ? 5 : 0;
                    break;

                case PkMode.Faction:
                    result.canAttack = (int)target.faction != _factionId;
                    result.reasonVi = result.canAttack ? "" : "Cùng môn phái";
                    result.penalty = result.canAttack ? PkPenaltyType.None : PkPenaltyType.RedName;
                    break;

                case PkMode.Bang:
                    // Bang mode: chỉ đánh người ngoài bang
                    result.canAttack = (int)target.faction != _factionId;
                    result.reasonVi = result.canAttack ? "" : "Cùng bang hội";
                    break;
            }

            _host?.OnAttackResolved(attacker.actorId, target.actorId, result.canAttack,
                result.reasonVi, result.penalty, result.karmaChange);
            if (result.canAttack) _host?.PlayPkSFX(attacker.actorId, target.actorId, _mode.ToString());

            return result;
        }

        /// <summary>Áp dụng hình phạt sau khi giết người.</summary>
        public void ApplyKillPenalty(PkResult pkResult)
        {
            if (pkResult.karmaChange > 0)
            {
                int prevKarma = _karma;
                _karma += pkResult.karmaChange;
                bool wasRed = prevKarma > 0;
                bool nowRed = _karma > 0;
                OnKarmaChanged?.Invoke(_karma);
                if (_host != null)
                {
                    _host.OnKarmaChanged(_karma, pkResult.karmaChange, nowRed);
                    if (!wasRed && nowRed) _host.OnBecameRedName(_actorId, _karma);
                    _host.SaveKarma(_actorId, _karma, _mode);
                    _host.LogPkEvent(_actorId, $"Sát khí +{pkResult.karmaChange}, hiện tại: {_karma}");
                }
                SubsystemLog.Info("PK", $"Sát khí +{pkResult.karmaChange}, hiện tại: {_karma}");
            }
        }

        /// <summary>Giảm sát khí theo thời gian (offline/online).</summary>
        public void ReduceKarma(int amount)
        {
            int prevKarma = _karma;
            _karma = Mathf.Max(0, _karma - amount);
            OnKarmaChanged?.Invoke(_karma);
            if (_host != null)
            {
                _host.OnKarmaChanged(_karma, -amount, _karma > 0);
                // Detect red-name clear
                if (prevKarma > 0 && _karma == 0) _host.OnClearedRedName(_actorId);
                _host.SaveKarma(_actorId, _karma, _mode);
            }
        }

        private static string ModeNameVi(PkMode mode) => mode switch
        {
            PkMode.Peace => "Hòa Bình",
            PkMode.Free => "Tự Do",
            PkMode.Team => "Tổ Đội",
            PkMode.Faction => "Môn Phái",
            PkMode.Bang => "Bang Hội",
            _ => "Không Rõ",
        };
    }

    // ── Shared battle runtime DTOs ───────────────────────────────────────────

    [Serializable]
    public class TongJinMatchState
    {
        public int matchId;
        public int songScore;    // Phe Tống điểm
        public int jinScore;     // Phe Kim điểm
        public float timeRemaining;
        public bool isActive;
        public int killRewardExp = 200;
    }

    [Serializable]
    public class BossHoangKimSpawn
    {
        public int bossTemplateId;
        public string nameVi;
        public int mapId;
        public float spawnX;
        public float spawnY;
        public int level;
        public int hp;
        public int killRewardExp;
        public int killRewardSilver;
        public int respawnMinutes;
    }

}
