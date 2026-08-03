// -----------------------------------------------------------------------------
// VLTK.Survivor — WaveManager: lifecycle wave table
// Parity: LevelMonsterMgr + LevelWave
// StartSpawn → WaveFuncByX.Trigger (WaveTriggerEvaluator) → CreateCurWave
// (WaveRefresh) → batch spawn → TimeOver/Finish → wave kế → hết table →
// LoopTable/TableDone (BattleFinsh hook cho director).
// DIY hook InitByDiyLevelWave = wave table tự author (KHÔNG binary cfg dhcd).
// Own: ramp theo wave index (interval giảm / count tăng), TriggerTimeout
// fail-safe, 1-wave-active (dhcd cho chồng wave — lệch chấp nhận, mở rộng sau),
// LoopTable placeholder (endless thật = ticket 41), mọi con số.
// Ticket 41 (endless): Endless=ON mặc định — loop vô hạn (LoopTable), boss wave
// định kỳ (DifficultyCurve.IsBossWave) chèn thay pool rotation (template boss từ
// table row đầu tiên có IsBoss; fail-closed: không template → không chèn), boss HP
// scale tăng mỗi lần tái qua Curve.BossHpScale, ngắt khi player dead (Endless poll +
// director pause). Curve mặc định = công thức ramp cũ → hành vi ticket 30 giữ nguyên.
// Thuần (không MonoBehaviour, không scene) → EditMode test sequence/ramp.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Survivor
{
    public sealed class WaveManager
    {
        public bool LoopTable = true;      // own: hết table → loop từ đầu (endless ticket 41)
        public bool TableDone { get; private set; }
        public int WaveIndex { get; private set; }     // cumulative (loop không reset) — ramp + test
        public WaveRefresh Active { get; private set; }
        public bool LastWaveCleanupMonsters { get; private set; } // IsDeleteAllMonster wave vừa finish
        public Func<Vector3> BornPos { get; set; }

        // Ticket 41: curve ramp + endless driver (mặc định ON — LoopTable=true đã loop vô hạn;
        // Curve giá trị mặc định = legacy ramp cũ). Ctor sync Endless.Curve → Curve,
        // ramp & boss schedule cùng 1 bộ số.
        public DifficultyCurve Curve = new DifficultyCurve();
        public EndlessMode Endless = new EndlessMode();

        private readonly List<LevelWaveConfig> _table = new List<LevelWaveConfig>();
        private int _activeWave;          // 1-based wave đang active (ticket 41: ramp/boss schedule)
        private int _curIdx;
        private LevelWaveConfig _pending;   // wave chờ trigger
        private LevelWaveConfig _curCfg;
        private float _pendingSince;
        private float _elapsed;
        private bool _started;
        private bool _waveFinishedFlag;

        public WaveManager()
        {
            // Cùng curve cho ramp (WaveManager.Curve) và boss schedule (Endless.Curve).
            Endless.Curve = Curve;
        }
        private readonly Dictionary<int, int> _spawnedById = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _aliveById = new Dictionary<int, int>();

        /// <summary>Parity hook LevelMonsterMgr.InitByDiyLevelWave — nạp wave table tự author.</summary>
        public void InitByDiyLevelWave(List<LevelWaveConfig> waves)
        {
            _table.Clear();
            if (waves != null) _table.AddRange(waves);
            _curIdx = 0;
            _pending = _table.Count > 0 ? _table[0] : null;
            _pendingSince = 0f;
            TableDone = false;
            WaveIndex = 0;
            Active = null;
        }

        /// <summary>Parity LevelMonsterMgr.StartSpawn.</summary>
        public void StartSpawn() { _started = true; _elapsed = 0f; }

        public void Tick(float dt, Action<MonsterSpawnInfo> spawn, WaveTriggerContext ctx)
        {
            if (!_started) return;
            // Endless gate (ticket 41): double-guard fail-closed — director đã pause khi
            // player chết (gameover timescale 0); poll đây chặn thêm tạo wave mới nếu
            // director chưa pause (hoặc mode tương lai không pause). Stop = vĩnh viễn.
            if (Endless != null)
            {
                if (Endless.PollPlayerDead && Endless.IsPlayerDead != null && Endless.IsPlayerDead())
                    Endless.Stop();
                if (!Endless.Running) return;
            }
            _elapsed += dt;
            if (_pending != null) TryTriggerPending(ctx);
            if (Active != null)
            {
                Active.Tick(dt, WrapSpawn(spawn));
                if (Active.Finished) FinalizeWave();
            }
        }

        /// <summary>Director báo 1 monster chết (kèm id). Chỉ tính monster thuộc wave hiện tại.</summary>
        public void OnMonsterKilled(int monsterId)
        {
            if (!_spawnedById.TryGetValue(monsterId, out _)) return;
            if (_aliveById.TryGetValue(monsterId, out int a)) _aliveById[monsterId] = a - 1;
            Active?.OnMonsterKilled();
        }

        /// <summary>Director poll 1 lần/frame sau Tick: wave vừa finish → dọn monster còn sống.</summary>
        public bool ConsumeWaveFinished()
        {
            bool f = _waveFinishedFlag;
            _waveFinishedFlag = false;
            return f;
        }

        // --- internal ---

        private void TryTriggerPending(WaveTriggerContext ctx)
        {
            (int killed, int total) = TargetKillCounts();
            ctx.Elapsed = _elapsed;
            bool met = WaveTriggerEvaluator.Evaluate(_pending, ctx, DiePercent(), killed, total);
            // own fail-safe: trigger chờ quá TriggerTimeout (vd boss chết sớm → HP% không bao giờ
            // met) → force start, không kẹt game
            if (!met && _pending.TriggerTimeout > 0f && _elapsed - _pendingSince >= _pending.TriggerTimeout)
                met = true;
            if (met) CreateWave(_pending);
        }

        private void CreateWave(LevelWaveConfig cfg)
        {
            if (Active != null) FinalizeActive(); // own: 1 wave active — dhcd cho chồng, chấp nhận lệch

            // Ticket 41: boss wave theo Endless schedule → pool = template boss trong table
            // (row đầu tiên có IsBoss monster); template HP ×= BossHpScale (respawn tần lượt
            // ×1.5, ×2,... — Curve.BossHpScale). Fail-closed: không row boss → src = pool
            // rotation (vẫn wave thường). Template dùng TRIGGER của wave rotation (không đổi
            // trình tự), chỉ thay pool.
            int wave = WaveIndex + 1; // 1-based
            WavePoolConfig src = cfg.Pool;
            float bossHpMul = 1f;
            if (Endless != null && Endless.Running && Endless.IsBossWave(wave))
            {
                var template = BossTemplatePool();
                if (template != null)
                {
                    src = template;
                    bossHpMul = Endless.BossHpScale(wave);
                }
            }
            var pool = RampCopy(src, wave, bossHpMul); // own ramp theo wave (ticket 41 curve)
            var waveRefresh = new WaveRefresh();
            waveRefresh.Init(cfg, pool, BornPos);
            waveRefresh.Start();
            Active = waveRefresh;
            _curCfg = cfg;
            _activeWave = wave;
            _spawnedById.Clear();
            _aliveById.Clear();
            WaveIndex++;
            _curIdx++;
            _pending = _curIdx < _table.Count ? _table[_curIdx] : null;
            _pendingSince = _elapsed;
        }

        /// <summary>Pool boss-template — row đầu tiên trong table có monster IsBoss (fail-closed: null nếu không có).</summary>
        private WavePoolConfig BossTemplatePool()
        {
            for (int i = 0; i < _table.Count; i++)
            {
                var p = _table[i].Pool;
                if (p == null) continue;
                for (int j = 0; j < p.Monsters.Count; j++)
                    if (p.Monsters[j].IsBoss) return p;
            }
            return null;
        }

        private void FinalizeActive()
        {
            LastWaveCleanupMonsters = _curCfg != null && _curCfg.IsDeleteAllMonster;
            Active = null;
            _waveFinishedFlag = true;
        }

        private void FinalizeWave()
        {
            FinalizeActive();
            if (_pending == null)
            {
                if (LoopTable)
                {
                    _curIdx = 0;
                    _pending = _table.Count > 0 ? _table[0] : null;
                    _pendingSince = _elapsed;
                }
                else TableDone = true;
            }
        }

        /// <summary>Parity LevelWave.GetCurWaveDieMonstePercent — kill% của wave đang chạy.</summary>
        private float DiePercent()
        {
            if (Active == null || Active.SpawnedTotal <= 0) return 0f;
            return Active.KilledCount * 100f / Active.SpawnedTotal;
        }

        private (int killed, int total) TargetKillCounts()
        {
            if (_pending == null || Active == null) return (0, 0);
            int target = _pending.TriggerParams != null && _pending.TriggerParams.Length > 0
                ? _pending.TriggerParams[0] : 0;
            if (target == 0 || !_spawnedById.TryGetValue(target, out int total)) return (0, 0);
            int alive = _aliveById.TryGetValue(target, out int a) ? Mathf.Max(0, a) : 0;
            return (Mathf.Min(total, total - alive), total);
        }

        private Action<MonsterSpawnInfo> WrapSpawn(Action<MonsterSpawnInfo> spawn)
        {
            return info =>
            {
                // Ticket 41: speed ramp theo wave (curve). HP/ATK ramp áp ở RampCopy
                // (WavePoolConfig.HpRatio/AtkRatio); speed không có pool ratio → nhân ở đây (thang spawn).
                if (Curve != null)
                {
                    float sm = Curve.SpeedScale(_activeWave);
                    if (Mathf.Abs(sm - 1f) > 1e-4f)
                        info = new MonsterSpawnInfo(info.Pos, info.MonsterId, info.IsBoss, info.IsElite,
                            info.HpMul, info.AtkMul, info.SpeedMul * sm);
                }
                _spawnedById.TryGetValue(info.MonsterId, out int s);
                _spawnedById[info.MonsterId] = s + 1;
                _aliveById.TryGetValue(info.MonsterId, out int a);
                _aliveById[info.MonsterId] = a + 1;
                spawn(info);
            };
        }

        /// <summary>
        /// Ramp theo DifficultyCurve (ticket 41) — field default = công thức linear cũ:
        /// interval ×max(0.45, 1−0.04·(wave−1)), quota +1/4 wave, cap +1/8 wave → khớp
        /// legacy ticket 30 (đầu wave IntervalMul(1)=1, wave 2 = 0.96...). MỚI: Hp/Atk
        /// ratio nhân curve, SingleNum +1/6 wave, bossWave nhân thêm BossHpScale.
        ///</summary>
        private WavePoolConfig RampCopy(WavePoolConfig src, int wave, float bossHpMul)
        {
            var p = new WavePoolConfig
            {
                Time = src.Time,
                SingleNum = src.SingleNum + Curve.BatchAdd(wave),
                MonsterNum = src.MonsterNum + Curve.QuotaAdd(wave),
                Interval = src.Interval * Curve.IntervalMul(wave),
                DynamicMonsterTime = src.DynamicMonsterTime,
                DynamicLoopNum = src.DynamicLoopNum,
                // Cap chỉ áp khi source dùng dynamic swarm (base > 0) — base 0 mà cộng
                // CapAdd (wave//8) sẽ biến non-dynamic thành cap Alive=1 → chặn spawn (fix 41).
                DynamicMonsterMaxNum = src.DynamicMonsterMaxNum > 0 ? src.DynamicMonsterMaxNum + Curve.CapAdd(wave) : src.DynamicMonsterMaxNum,
                Isloop = src.Isloop,
                EliteRatio = src.EliteRatio,
                HpRatio = src.HpRatio * Curve.HpMul(wave) * bossHpMul,
                AtkRatio = src.AtkRatio * Curve.AtkMul(wave),
            };
            p.Monsters.AddRange(src.Monsters);
            return p;
        }

        // --- own wave table authoring (D17 config authoring; balance rationale inline) ---

        private static WavePoolConfig Pool(float time, float interval, int single, int num,
            float eliteRatio = 0f, float dynTime = 0f, float dynLoop = 1f, int dynMax = 0, bool isLoop = false)
        {
            return new WavePoolConfig
            {
                Time = time, Interval = interval, SingleNum = single, MonsterNum = num,
                EliteRatio = eliteRatio, DynamicMonsterTime = dynTime,
                DynamicLoopNum = dynLoop, DynamicMonsterMaxNum = dynMax, Isloop = isLoop,
            };
        }

        private static LevelWaveConfig W(int id, WaveEventFuncType t, float triggerTime, int[] triggerParams,
            WavePoolConfig pool, int endType, bool deleteAll, float timeout)
        {
            return new LevelWaveConfig
            {
                WaveId = id, WaveType = t, TriggerTime = triggerTime, TriggerParams = triggerParams,
                Pool = pool, EndType = endType, IsDeleteAllMonster = deleteAll, TriggerTimeout = timeout,
            };
        }

        /// <summary>
        /// Wave table default (own-design, KHÔNG clone dhcd — D1).
        /// Balance rationale: P1 player auto-dps ~4 dmg/s; monster thường 3 HP
        /// → wave 6 con clear ~10-15s. Boss 30 HP (3×10) → ~8s đấu.
        /// W1 time-trigger làm quen → W2/W3 kill% (elite ratio từ W3) → W4 boss
        /// (boss outlive wave nhờ IsDeleteAllMonster=false → W5 trigger HP% — parity
        /// dhcd boss entity sống xuyên wave) → W5 swarm dynamic (Isloop + dynamic caps)
        /// → W6 clear (EndType 1 kill-all) → loop table (ramp tiếp).
        /// Type 4/7/8/9 không dùng trong table (P2 skill-cast + capture-mode sẽ nối), chỉ test.
        /// </summary>
        public static List<LevelWaveConfig> DefaultTable()
        {
            var list = new List<LevelWaveConfig>();

            // W1: khởi động — time trigger 1s
            list.Add(W(1, WaveEventFuncType.WAVE_TIME_TRIGGER, 1f, null,
                Pool(time: 18f, interval: 2.5f, single: 2, num: 6), 0, true, 10f));

            // W2: kill% 50 của wave 1
            list.Add(W(2, WaveEventFuncType.WAVE_MONSTER_COUNT_PERCENT, 0f, new[] { 50 },
                Pool(time: 20f, interval: 2.2f, single: 2, num: 9), 0, true, 15f));

            // W3: kill% 60 + elite ratio 20% (own: tinh anh xen kẽ từ wave 3)
            list.Add(W(3, WaveEventFuncType.WAVE_MONSTER_COUNT_PERCENT, 0f, new[] { 60 },
                Pool(time: 22f, interval: 2f, single: 2, num: 8, eliteRatio: 0.2f), 0, true, 15f));

            // W4: kill% 70 → boss wave. Boss 30 HP, speed 0.75×, đánh 2.
            // deleteAll=false → boss sống tiếp cho W5 trigger HP%.
            var boss = Pool(time: 30f, interval: 1.8f, single: 2, num: 5);
            boss.Monsters.Add(new WaveMonsterConfig { MonsterId = 2000, IsBoss = true });
            boss.Monsters.Add(new WaveMonsterConfig { MonsterId = 1000 });
            boss.Monsters.Add(new WaveMonsterConfig { MonsterId = 1001 });
            list.Add(W(4, WaveEventFuncType.WAVE_MONSTER_COUNT_PERCENT, 0f, new[] { 70 }, boss, 0, false, 20f));

            // W5: boss HP ≤50% → swarm: dynamic phase (interval co, batch theo thiếu hụt) + Isloop,
            // alive cap 14; 2 elite id 1002 chủ động (demo elite flag + kill-all id sau này)
            var swarm = Pool(time: 25f, interval: 1.2f, single: 3, num: 10,
                dynTime: 6f, dynLoop: 1.5f, dynMax: 14, isLoop: true);
            swarm.Monsters.Add(new WaveMonsterConfig { MonsterId = 1000 });
            swarm.Monsters.Add(new WaveMonsterConfig { MonsterId = 1001 });
            swarm.Monsters.Add(new WaveMonsterConfig { MonsterId = 1002, IsElite = true });
            swarm.Monsters.Add(new WaveMonsterConfig { MonsterId = 1002, IsElite = true });
            list.Add(W(5, WaveEventFuncType.WAVE_MONSTER_HP_PERCENT, 0f, new[] { 50 }, swarm, 0, true, 30f));

            // W6: kill% 60 của swarm → clear wave EndType 1 (kill-all) rồi loop table.
            list.Add(W(6, WaveEventFuncType.WAVE_MONSTER_COUNT_PERCENT, 0f, new[] { 60 },
                Pool(time: 20f, interval: 1.5f, single: 2, num: 6), 1, true, 25f));

            return list;
        }
    }
}
