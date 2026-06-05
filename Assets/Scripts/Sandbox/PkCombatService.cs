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

        public PkMode Mode => _mode;
        public int Karma => _karma;
        public bool IsRedName => _karma > 0;

        public event Action<PkMode> OnPkModeChanged;
        public event Action<int> OnKarmaChanged;

        public PkCombatService(int factionId, int bangId = 0)
        {
            _factionId = factionId;
            _bangId = bangId;
        }

        /// <summary>Chuyển chế độ PK.</summary>
        public void SetPkMode(PkMode mode)
        {
            _mode = mode;
            OnPkModeChanged?.Invoke(mode);
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

            return result;
        }

        /// <summary>Áp dụng hình phạt sau khi giết người.</summary>
        public void ApplyKillPenalty(PkResult pkResult)
        {
            if (pkResult.karmaChange > 0)
            {
                _karma += pkResult.karmaChange;
                OnKarmaChanged?.Invoke(_karma);
                SubsystemLog.Info("PK", $"Sát khí +{pkResult.karmaChange}, hiện tại: {_karma}");
            }
        }

        /// <summary>Giảm sát khí theo thời gian (offline/online).</summary>
        public void ReduceKarma(int amount)
        {
            _karma = Mathf.Max(0, _karma - amount);
            OnKarmaChanged?.Invoke(_karma);
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

    // ── Tống Kim Battlefield ────────────────────────────────────────────────

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

    /// <summary>
    /// Service quản lý chiến trường Tống Kim.
    /// PC source: Tống Kim battlefield, 2 phe (Tống/Kim), kill = điểm cho phe.
    /// </summary>
    public class TongJinBattleService
    {
        private TongJinMatchState _state;

        public TongJinMatchState State => _state;

        public event Action<TongJinMatchState> OnMatchEnded;

        /// <summary>Bắt đầu trận Tống Kim.</summary>
        public TongJinMatchState StartMatch(int matchId, float durationSeconds = 600f)
        {
            _state = new TongJinMatchState
            {
                matchId = matchId,
                timeRemaining = durationSeconds,
                isActive = true,
            };
            SubsystemLog.Info("TongJin", $"Trận Tống Kim #{matchId} bắt đầu ({durationSeconds}s)");
            return _state;
        }

        /// <summary>Ghi nhận kill trong Tống Kim.</summary>
        public void RecordKill(bool isSongTeamKill)
        {
            if (_state == null || !_state.isActive) return;

            if (isSongTeamKill) _state.songScore++;
            else _state.jinScore++;
        }

        /// <summary>Update mỗi frame.</summary>
        public void Tick(float deltaTime)
        {
            if (_state == null || !_state.isActive) return;

            _state.timeRemaining -= deltaTime;
            if (_state.timeRemaining <= 0)
            {
                EndMatch();
            }
        }

        public void EndMatch()
        {
            if (_state == null) return;
            _state.isActive = false;
            string winner = _state.songScore > _state.jinScore ? "Tống" :
                            _state.jinScore > _state.songScore ? "Kim" : "Hòa";
            SubsystemLog.Info("TongJin", $"Trận #{_state.matchId} kết thúc: Tống {_state.songScore} - Kim {_state.jinScore} ({winner})");
            OnMatchEnded?.Invoke(_state);
        }
    }

    // ── Boss Hoàng Kim ─────────────────────────────────────────────────────

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

    /// <summary>
    /// Service quản lý Boss Hoàng Kim (World Boss).
    /// PC source: Boss Hoàng Kim spawn schedule, drop tables, respawn timer.
    /// </summary>
    public class BossHoangKimService
    {
        private readonly List<BossHoangKimSpawn> _bosses = new();
        private readonly Dictionary<int, float> _respawnTimers = new();

        public IReadOnlyList<BossHoangKimSpawn> RegisteredBosses => _bosses;

        public event Action<BossHoangKimSpawn> OnBossSpawned;
        public event Action<BossHoangKimSpawn, int> OnBossKilled; // (boss, killerActorId)

        public BossHoangKimService()
        {
            RegisterDefaultBosses();
        }

        public void RegisterBoss(BossHoangKimSpawn boss) => _bosses.Add(boss);

        /// <summary>Boss bị giết — trigger reward + respawn timer.</summary>
        public void OnBossDeath(int bossTemplateId, int killerActorId)
        {
            var boss = _bosses.Find(b => b.bossTemplateId == bossTemplateId);
            if (boss == null) return;

            _respawnTimers[bossTemplateId] = boss.respawnMinutes * 60f;
            OnBossKilled?.Invoke(boss, killerActorId);
            SubsystemLog.Info("BossHK", $"Boss {boss.nameVi} bị giết bởi actor {killerActorId}. Respawn sau {boss.respawnMinutes} phút.");
        }

        /// <summary>Tick respawn timer.</summary>
        public void Tick(float deltaTime)
        {
            var keys = new List<int>(_respawnTimers.Keys);
            foreach (var id in keys)
            {
                _respawnTimers[id] -= deltaTime;
                if (_respawnTimers[id] <= 0)
                {
                    _respawnTimers.Remove(id);
                    var boss = _bosses.Find(b => b.bossTemplateId == id);
                    if (boss != null)
                    {
                        OnBossSpawned?.Invoke(boss);
                        SubsystemLog.Info("BossHK", $"Boss {boss.nameVi} đã respawn!");
                    }
                }
            }
        }

        /// <summary>Kiểm tra boss có đang alive không.</summary>
        public bool IsBossAlive(int bossTemplateId) => !_respawnTimers.ContainsKey(bossTemplateId);

        private void RegisterDefaultBosses()
        {
            _bosses.Add(new BossHoangKimSpawn
            {
                bossTemplateId = 600, nameVi = "Bạch Vân Phi", mapId = 200,
                spawnX = 500, spawnY = 1000, level = 50, hp = 50000,
                killRewardExp = 10000, killRewardSilver = 5000, respawnMinutes = 60,
            });
            _bosses.Add(new BossHoangKimSpawn
            {
                bossTemplateId = 601, nameVi = "Xích Diệm Ma Vương", mapId = 203,
                spawnX = 300, spawnY = 800, level = 70, hp = 100000,
                killRewardExp = 25000, killRewardSilver = 10000, respawnMinutes = 120,
            });
            _bosses.Add(new BossHoangKimSpawn
            {
                bossTemplateId = 602, nameVi = "Kim Luân Pháp Vương", mapId = 204,
                spawnX = 700, spawnY = 1500, level = 90, hp = 200000,
                killRewardExp = 50000, killRewardSilver = 20000, respawnMinutes = 180,
            });
        }
    }

    // ── Bang Chiến ─────────────────────────────────────────────────────────

    /// <summary>
    /// Service quản lý Bang Chiến (Guild War).
    /// PC source: Bang chiến schedule, victory conditions, territory control.
    /// </summary>
    public class BangChienService
    {
        private int _challengerBangId;
        private int _defenderBangId;
        private int _challengerScore;
        private int _defenderScore;
        private bool _isActive;

        public bool IsActive => _isActive;

        public event Action<int, int, int> OnBangChienEnded; // (winnerBangId, score1, score2)

        /// <summary>Bắt đầu Bang Chiến.</summary>
        public void StartBangChien(int challengerBangId, int defenderBangId)
        {
            _challengerBangId = challengerBangId;
            _defenderBangId = defenderBangId;
            _challengerScore = 0;
            _defenderScore = 0;
            _isActive = true;
            SubsystemLog.Info("BangChien", $"Bang Chiến bắt đầu: Bang {challengerBangId} vs Bang {defenderBangId}");
        }

        /// <summary>Ghi nhận kill cho phe.</summary>
        public void RecordKill(bool isChallengerKill)
        {
            if (!_isActive) return;
            if (isChallengerKill) _challengerScore++;
            else _defenderScore++;
        }

        /// <summary>Kết thúc Bang Chiến.</summary>
        public int EndBangChien()
        {
            _isActive = false;
            int winner = _challengerScore > _defenderScore ? _challengerBangId :
                         _defenderScore > _challengerScore ? _defenderBangId : 0;
            OnBangChienEnded?.Invoke(winner, _challengerScore, _defenderScore);
            SubsystemLog.Info("BangChien", $"Bang Chiến kết thúc: {_challengerScore}-{_defenderScore}. Winner: Bang {winner}");
            return winner;
        }
    }
}
