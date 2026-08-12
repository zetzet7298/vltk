// -----------------------------------------------------------------------------
// VLTK Mobile — ST-08.13 Boss Hoàng Kim Service (Boss spawn runtime)
// Wraps PcBossHoangKimRegistry. PC source: settings/boss/bosshoangkim.txt.
// Vietnamese: "Boss Hoàng Kim", "Hồi Sinh", "Rơi Đồ", "Tọa Độ".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class BossHoangKimService
    {
        public const string LogTag = "BossHoangKim";
        public const string DefaultStreamingDir = "Reference/PcBoss";

        private PcBossHoangKimRegistry _registry;
        private readonly List<BossHoangKimSpawn> _legacyBosses = new();
        private readonly Dictionary<int, float> _respawnTimers = new();
        private IBossHoangKimHost _host;

        public event Action OnBossLoaded;
        public event Action<BossHoangKimSpawn> OnBossSpawned;
        public event Action<BossHoangKimSpawn, int> OnBossKilled;

        public int Count => _registry != null ? _registry.Count : 0;
        public IReadOnlyList<BossHoangKimSpawn> RegisteredBosses => _legacyBosses;

        public BossHoangKimService() { RegisterDefaultBosses(); }
        public BossHoangKimService(PcBossHoangKimRegistry registry)
        {
            RegisterDefaultBosses();
            AttachRegistry(registry);
        }

        public void AttachHost(IBossHoangKimHost host) { _host = host; }

        public void AttachRegistry(PcBossHoangKimRegistry registry)
        {
            _registry = registry ?? new PcBossHoangKimRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} boss Hoàng Kim");
            OnBossLoaded?.Invoke();
            if (_host != null)
            {
                _host.OnBossRegistryAttached(_registry.Count);
                _host.LogBossEvent("load", 0, $"Loaded {_registry.Count} boss entries");
                _host.PlayBossSFX("load", 0);
                _host.SaveBossState(0, DateTime.MinValue, 0);
            }
        }

        public PcBossHoangKimEntry GetBoss(int id)
        {
            var b = _registry != null ? _registry.Get(id) : null;
            if (_host != null)
            {
                if (b != null)
                    _host.OnBossResolved(b.bossId, b.mapId, b.respawnSec, b.level);
                else
                    _host.LogBossEvent("query_missing", id, "Boss not found in registry");
            }
            return b;
        }

        public IReadOnlyList<PcBossHoangKimEntry> GetByMap(int mapId)
            => _registry != null ? _registry.GetByMap(mapId) : Array.Empty<PcBossHoangKimEntry>();

        public void RegisterBoss(BossHoangKimSpawn boss)
        {
            if (boss != null) _legacyBosses.Add(boss);
        }

        /// <summary>Boss bị giết — trigger reward + respawn timer.</summary>
        public void OnBossDeath(int bossTemplateId, int killerActorId)
        {
            var boss = _legacyBosses.Find(b => b.bossTemplateId == bossTemplateId);
            if (boss == null) return;
            _respawnTimers[bossTemplateId] = boss.respawnMinutes * 60f;
            OnBossKilled?.Invoke(boss, killerActorId);
            SubsystemLog.Info(LogTag, $"Boss {boss.nameVi} bị giết bởi actor {killerActorId}. Respawn sau {boss.respawnMinutes} phút.");
            if (_host != null)
            {
                _host.OnBossKilled(boss.bossTemplateId, killerActorId, boss.respawnMinutes);
                _host.LogBossEvent("kill", boss.bossTemplateId, $"Boss {boss.nameVi} bị giết bởi actor {killerActorId}");
                _host.PlayBossSFX("kill", boss.bossTemplateId);
                _host.ShowBossUI(boss.bossTemplateId, boss.nameVi, boss.mapId, 0);
                _host.SaveBossState(boss.bossTemplateId, DateTime.UtcNow, boss.respawnMinutes * 60);
            }
        }

        public void Tick(float deltaTime)
        {
            var keys = new List<int>(_respawnTimers.Keys);
            foreach (var id in keys)
            {
                _respawnTimers[id] -= deltaTime;
                if (_host != null)
                    _host.OnBossRespawnTicked(id, Mathf.Max(0, (int)_respawnTimers[id]));
                if (_respawnTimers[id] <= 0)
                {
                    _respawnTimers.Remove(id);
                    var boss = _legacyBosses.Find(b => b.bossTemplateId == id);
                    if (boss != null)
                    {
                        OnBossSpawned?.Invoke(boss);
                        SubsystemLog.Info(LogTag, $"Boss {boss.nameVi} đã hồi sinh!");
                        if (_host != null)
                        {
                            _host.OnBossRespawned(boss.bossTemplateId, boss.mapId);
                            _host.OnBossSpawned(boss.bossTemplateId, boss.mapId, (int)boss.spawnX, (int)boss.spawnY, boss.level);
                            _host.LogBossEvent("respawn", boss.bossTemplateId, $"Boss {boss.nameVi} đã hồi sinh");
                            _host.PlayBossSFX("respawn", boss.bossTemplateId);
                            _host.ShowBossUI(boss.bossTemplateId, boss.nameVi, boss.mapId, 100);
                        }
                    }
                }
            }
        }

        public bool IsBossAlive(int bossTemplateId) => !_respawnTimers.ContainsKey(bossTemplateId);

        /// <summary>Tính thời điểm hồi sinh kế tiếp cho boss. Trả về now + respawnSec.</summary>
        public DateTime ComputeRespawnTime(int bossId, DateTime? killedAt = null)
        {
            var b = GetBoss(bossId);
            if (b == null) return DateTime.MinValue;
            var start = killedAt ?? DateTime.UtcNow;
            return start.AddSeconds(Math.Max(0, b.respawnSec));
        }

        /// <summary>Lọc boss hiện đang hoạt động (chưa tới giờ hồi sinh).</summary>
        public IReadOnlyList<PcBossHoangKimEntry> GetActiveBosses(
            DateTime now,
            IReadOnlyDictionary<int, DateTime> lastDeathUtc = null)
        {
            if (_registry == null)
            {
                if (_host != null) _host.OnActiveBossesQueried(0, now);
                return Array.Empty<PcBossHoangKimEntry>();
            }
            var list = new List<PcBossHoangKimEntry>();
            foreach (var b in _registry.All)
            {
                if (lastDeathUtc != null
                    && lastDeathUtc.TryGetValue(b.bossId, out var death)
                    && death.AddSeconds(b.respawnSec) > now)
                    continue; // Chưa hồi sinh
                list.Add(b);
            }
            if (_host != null) _host.OnActiveBossesQueried(list.Count, now);
            return list;
        }


        private void RegisterDefaultBosses()
        {
            if (_legacyBosses.Count > 0) return;
            _legacyBosses.Add(new BossHoangKimSpawn
            {
                bossTemplateId = 600, nameVi = "Bạch Vân Phi", mapId = 200,
                spawnX = 500, spawnY = 1000, level = 50, hp = 50000,
                killRewardExp = 10000, killRewardSilver = 5000, respawnMinutes = 60,
            });
            _legacyBosses.Add(new BossHoangKimSpawn
            {
                bossTemplateId = 601, nameVi = "Xích Diệm Ma Vương", mapId = 203,
                spawnX = 300, spawnY = 800, level = 70, hp = 100000,
                killRewardExp = 25000, killRewardSilver = 10000, respawnMinutes = 120,
            });
            _legacyBosses.Add(new BossHoangKimSpawn
            {
                bossTemplateId = 602, nameVi = "Kim Luân Pháp Vương", mapId = 204,
                spawnX = 700, spawnY = 1500, level = 90, hp = 200000,
                killRewardExp = 50000, killRewardSilver = 20000, respawnMinutes = 180,
            });
        }

        public static BossHoangKimService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new BossHoangKimService();
            if (Directory.Exists(dir))
            {
                var reg = PcBossHoangKimParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Boss Hoàng Kim: directory không tồn tại {dir}");
                svc.OnBossLoaded?.Invoke();
            }
            return svc;
        }
    }
}
