using System;
using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>
    /// Wave driver. parity dhcd LevelMonsterMgr lifecycle:
    /// StartSpawn → WaveFuncByX.Trigger → CreateCurWave → WaveRefresh batch spawn → Finish.
    /// Wave table tự author (WaveManager.DefaultTable) nạp qua DIY hook InitByDiyLevelWave.
    /// Giữ API Tick(dt, spawnAt); spawnAt giờ nhận MonsterSpawnInfo (boss/elite/ratio).
    /// </summary>
    public sealed class WaveSpawner
    {
        private readonly WaveManager _mgr = new WaveManager();

        public WaveSpawner()
        {
            _mgr.InitByDiyLevelWave(WaveManager.DefaultTable());
            _mgr.BornPos = PerimeterPos;
            _mgr.StartSpawn();
        }

        public bool WaveCleanupMonsters => _mgr.LastWaveCleanupMonsters;
        public bool TableDone => _mgr.TableDone;
        public int CurrentWaveIndex => _mgr.WaveIndex;

        public void Tick(float dt, Action<MonsterSpawnInfo> spawnAt)
        {
            var d = SurvivorGameDirector.Instance;
            if (d == null) return;
            var ctx = new WaveTriggerContext
            {
                BossHpPercent = d.BossHpPercent,
                SkillCastCount = d.SkillCastCount,
                OccupiedMask = d.OccupiedMask,
            };
            _mgr.Tick(dt, spawnAt, ctx);
        }

        public void OnMonsterKilled(int monsterId) => _mgr.OnMonsterKilled(monsterId);

        public bool ConsumeWaveFinished() => _mgr.ConsumeWaveFinished();

        private Vector3 PerimeterPos()
        {
            var d = SurvivorGameDirector.Instance;
            var half = d != null ? d.ArenaHalf : new Vector2(3.3f, 5.8f);
            float r = Mathf.Max(half.x, half.y) + 1f;
            float ang = UnityEngine.Random.value * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
        }
    }
}
