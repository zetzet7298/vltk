// -----------------------------------------------------------------------------
// VLTK.Survivor — WaveRefresh: nhịp spawn 1 wave
// Batch theo Interval/SingleNum, dynamic caps swarm, Isloop, elite roll,
// TimeOver/Finish.
// Parity: BattleCore.WaveRefresh (Start set refreshMonsterTime = interval;
// OnUpdate batch spawn khi time >= refreshMonsterTime, refresh += interval;
// quota m_monsterNumMax stop; round-robin m_curMonsterIndex; const
// OneFrameMaxCreateNum = 100; dynamic interval swap + spawn-count tỉ lệ thiếu hụt).
// Own: công thức dynamic phase, elite roll, EndType 0/1, hardcap chống stall.
// Thuần (không MonoBehaviour, không scene) → EditMode test batch math.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Survivor
{
    public sealed class WaveRefresh
    {
        public const int OneFrameMaxCreateNum = 100; // parity const WaveRefresh.cs

        public bool Finished { get; private set; }
        public int AliveCount { get; private set; }
        public int KilledCount { get; private set; }
        public int SpawnedTotal { get; private set; }
        public float Interval => _pool != null ? _pool.Interval : 0f; // sau ramp

        private LevelWaveConfig _cfg;
        private WavePoolConfig _pool;
        private readonly List<WaveMonsterConfig> _list = new List<WaveMonsterConfig>();
        private Func<Vector3> _bornPos;
        private int _curIndex;
        private float _refreshTime;
        private float _elapsed;
        private float _hardCap;

        public void Init(LevelWaveConfig cfg, Func<Vector3> bornPos)
        {
            Init(cfg, cfg.Pool, bornPos);
        }

        public void Init(LevelWaveConfig cfg, WavePoolConfig pool, Func<Vector3> bornPos)
        {
            _cfg = cfg;
            _pool = pool;
            _list.Clear();
            if (_pool.Monsters.Count == 0) _list.Add(new WaveMonsterConfig()); // fallback id 1000
            else _list.AddRange(_pool.Monsters);
            _bornPos = bornPos;
        }

        /// <summary>Parity WaveRefresh.Start: m_startTime = now; m_refreshMonsterTime = interval.</summary>
        public void Start()
        {
            Finished = false;
            _curIndex = 0;
            _elapsed = 0f;
            AliveCount = 0; KilledCount = 0; SpawnedTotal = 0;
            _refreshTime = Mathf.Max(0.05f, _pool.Interval);
            // own: hardcap chống stall — wave EndType 1 (kill-all) không bao giờ đủ kill
            _hardCap = _pool.Time > 0f ? _pool.Time * 3f : 60f;
        }

        public void Tick(float dt, Action<MonsterSpawnInfo> spawn)
        {
            if (Finished) return;
            _elapsed += dt;
            bool dynamic = _pool.DynamicMonsterTime > 0f && _elapsed >= _pool.DynamicMonsterTime;
            // parity: dynamic phase → interval co lại (own: 0.6×, floor 0.2s)
            float interval = dynamic ? Mathf.Max(0.2f, _pool.Interval * 0.6f) : _pool.Interval;
            if (_elapsed >= _refreshTime)
            {
                _refreshTime += interval;
                int count = BatchCount(dynamic);
                for (int i = 0; i < count && CanSpawnMore(); i++) SpawnOne(spawn);
            }
            CheckFinish();
        }

        /// <summary>Monster chết (director báo qua WaveManager, chỉ đếm monster thuộc wave này).</summary>
        public void OnMonsterKilled()
        {
            KilledCount++;
            AliveCount = Mathf.Max(0, AliveCount - 1);
            CheckFinish(); // kill cuối quota → finish ngay, không chờ Tick
        }

        private int MaxAlive()
        {
            // own: dynamic cap thay quota làm alive cap; parity m_dynamicMonsterMaxNum
            return _pool.DynamicMonsterMaxNum > 0 ? _pool.DynamicMonsterMaxNum : _pool.MonsterNum;
        }

        private bool CanSpawnMore()
        {
            int maxAlive = MaxAlive();
            if (maxAlive > 0 && AliveCount >= maxAlive) return false; // parity quota stop
            return _pool.Isloop || SpawnedTotal < _pool.MonsterNum;
        }

        private int BatchCount(bool dynamic)
        {
            int single = Mathf.Max(1, _pool.SingleNum);
            if (dynamic && _pool.DynamicMonsterMaxNum > 0)
            {
                // own (shape parity): dynamic phase → batch tỉ lệ số monster thiếu × DynamicLoopNum
                int missing = Mathf.Max(0, _pool.DynamicMonsterMaxNum - AliveCount);
                int dyn = Mathf.Max(single, Mathf.CeilToInt(missing * Mathf.Max(0f, _pool.DynamicLoopNum)));
                return Mathf.Min(OneFrameMaxCreateNum, dyn);
            }
            if (_pool.Isloop) return Mathf.Min(OneFrameMaxCreateNum, single);
            int remaining = _pool.MonsterNum - SpawnedTotal;
            return Mathf.Min(OneFrameMaxCreateNum, Mathf.Min(single, Mathf.Max(0, remaining)));
        }

        private void SpawnOne(Action<MonsterSpawnInfo> spawn)
        {
            var cfg = _list[_curIndex % _list.Count]; // round-robin parity m_curMonsterIndex
            _curIndex++;
            SpawnedTotal++;
            AliveCount++;
            bool elite = cfg.IsElite || (_pool.EliteRatio > 0f && UnityEngine.Random.value < _pool.EliteRatio);
            // own tier (dhcd chỉ có IsBoss binary; elite self-design):
            // boss trâu (10×HP) chậm (0.75 speed), đánh 2; elite dai (2.5×HP) nhanh (1.2 speed), đánh 1
            float tierHp = 1f, tierAtk = 1f, tierSpeed = 1f;
            if (cfg.IsBoss) { tierHp = 10f; tierAtk = 2f; tierSpeed = 0.75f; }
            else if (elite) { tierHp = 2.5f; tierAtk = 1f; tierSpeed = 1.2f; }
            var pos = _bornPos != null ? _bornPos() : Vector3.zero;
            spawn(new MonsterSpawnInfo(pos, cfg.MonsterId, cfg.IsBoss, elite,
                _pool.HpRatio * cfg.HpRatio * tierHp,
                _pool.AtkRatio * cfg.AtkRatio * tierAtk,
                tierSpeed));
        }

        private void CheckFinish()
        {
            bool timeCapped = _pool.Time > 0f;
            bool quotaDone = !_pool.Isloop && SpawnedTotal >= _pool.MonsterNum;
            bool allDead = quotaDone && KilledCount >= SpawnedTotal;
            if (timeCapped && _elapsed >= _pool.Time)
            {
                // EndType 1 (kill-all): chờ đủ kill tới hardcap, chống stall
                if (_cfg.EndType == 1 && quotaDone && !allDead && _elapsed < _hardCap) return;
                Finish();
                return;
            }
            if (quotaDone && allDead && (_cfg.EndType == 1 || !timeCapped)) Finish();
            else if (quotaDone && _cfg.EndType == 1 && _elapsed >= _hardCap) Finish();
        }

        private void Finish()
        {
            Finished = true;
            AliveCount = 0;
        }
    }
}
