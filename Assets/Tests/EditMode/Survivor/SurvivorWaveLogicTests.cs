// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorWaveLogicTests
// Ticket 30 self-check: trigger eval 9 loại + batch math + wave sequence/ramp.
// Pure logic (class thuần init-able) — không scene, không PlayMode.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorWaveLogicTests
    {
        // --- helpers ---

        private static LevelWaveConfig Cfg(WaveEventFuncType t, float triggerTime = 0f, params int[] triggerParams)
        {
            return new LevelWaveConfig
            {
                WaveType = t,
                TriggerTime = triggerTime,
                TriggerParams = triggerParams != null && triggerParams.Length > 0 ? triggerParams : null,
            };
        }

        private static WaveTriggerContext Ctx(float elapsed, float bossHp = 1f, int casts = 0, int mask = 0)
        {
            return new WaveTriggerContext
            {
                Elapsed = elapsed, BossHpPercent = bossHp, SkillCastCount = casts, OccupiedMask = mask,
            };
        }

        private static WavePoolConfig PoolSimple(int num, float interval, int single = 2, float time = 0f)
        {
            var p = new WavePoolConfig { Time = time, Interval = interval, SingleNum = single, MonsterNum = num };
            p.Monsters.Add(new WaveMonsterConfig { MonsterId = 1 });
            return p;
        }

        private static WaveRefresh StartWave(WavePoolConfig pool, int endType = 0)
        {
            var w = new WaveRefresh();
            w.Init(new LevelWaveConfig { Pool = pool, EndType = endType }, null);
            w.Start();
            return w;
        }

        // --- trigger eval: 9 loại ---

        [Test]
        public void Trigger_Time_ElapsedVsTriggerTime()
        {
            var c = Cfg(WaveEventFuncType.WAVE_TIME_TRIGGER, 2f);
            Assert.IsFalse(WaveTriggerEvaluator.Evaluate(c, Ctx(1.9f), 0f, 0, 0));
            Assert.IsTrue(WaveTriggerEvaluator.Evaluate(c, Ctx(2f), 0f, 0, 0));
        }

        [Test]
        public void Trigger_KillPercent_Threshold()
        {
            var c = Cfg(WaveEventFuncType.WAVE_MONSTER_COUNT_PERCENT, 0f, 50);
            Assert.IsFalse(WaveTriggerEvaluator.Evaluate(c, Ctx(0f), 49.9f, 0, 0));
            Assert.IsTrue(WaveTriggerEvaluator.Evaluate(c, Ctx(0f), 50f, 0, 0));
        }

        [Test]
        public void Trigger_BossHpPercent_Threshold()
        {
            var c = Cfg(WaveEventFuncType.WAVE_MONSTER_HP_PERCENT, 0f, 50);
            Assert.IsFalse(WaveTriggerEvaluator.Evaluate(c, Ctx(0f, bossHp: 0.51f), 0f, 0, 0));
            Assert.IsTrue(WaveTriggerEvaluator.Evaluate(c, Ctx(0f, bossHp: 0.5f), 0f, 0, 0));
            Assert.IsTrue(WaveTriggerEvaluator.Evaluate(c, Ctx(0f, bossHp: 0f), 0f, 0, 0));
        }

        [Test]
        public void Trigger_SkillCast_CountThreshold()
        {
            var c = Cfg(WaveEventFuncType.WAVE_MONSTER_PLAY_SKILL, 0f, 3);
            Assert.IsFalse(WaveTriggerEvaluator.Evaluate(c, Ctx(0f, casts: 2), 0f, 0, 0));
            Assert.IsTrue(WaveTriggerEvaluator.Evaluate(c, Ctx(0f, casts: 3), 0f, 0, 0));
        }

        [Test]
        public void Trigger_KillAllMonsterId_NeedsAllDead()
        {
            var c = Cfg(WaveEventFuncType.WAVE_TYPE_KILL_ALL_MONSTER_THISID, 0f, 1002);
            Assert.IsFalse(WaveTriggerEvaluator.Evaluate(c, Ctx(0f), 0f, 0, 0), "chưa spawn target");
            Assert.IsFalse(WaveTriggerEvaluator.Evaluate(c, Ctx(0f), 0f, 1, 2), "mới chết 1/2");
            Assert.IsTrue(WaveTriggerEvaluator.Evaluate(c, Ctx(0f), 0f, 2, 2), "chết hết");
        }

        [Test]
        public void Trigger_KillAllAndTimeOver_NeedsBoth()
        {
            var c = Cfg(WaveEventFuncType.WAVE_TYPE_KILLALL_AND_TIMEOVER_THISID, 5f, 1002);
            Assert.IsFalse(WaveTriggerEvaluator.Evaluate(c, Ctx(4.9f), 0f, 2, 2), "kill đủ nhưng chưa đủ time");
            Assert.IsTrue(WaveTriggerEvaluator.Evaluate(c, Ctx(5f), 0f, 2, 2));
        }

        [Test]
        public void Trigger_OccupyStart_AnyPoint()
        {
            var c = Cfg(WaveEventFuncType.WAVE_TYPE_OCCUPY_START);
            Assert.IsFalse(WaveTriggerEvaluator.Evaluate(c, Ctx(0f, mask: 0), 0f, 0, 0));
            Assert.IsTrue(WaveTriggerEvaluator.Evaluate(c, Ctx(0f, mask: 1), 0f, 0, 0));
            Assert.IsFalse(WaveTriggerEvaluator.Evaluate(c, Ctx(0f, mask: 0x100), 0f, 0, 0), "điểm ngoài 8 đầu");
        }

        [Test]
        public void Trigger_OccupyEnd_SpecificPoint()
        {
            var c = Cfg(WaveEventFuncType.WAVE_TYPE_OCCUPY_END, 0f, 3);
            Assert.IsFalse(WaveTriggerEvaluator.Evaluate(c, Ctx(0f, mask: 1 << 2), 0f, 0, 0));
            Assert.IsTrue(WaveTriggerEvaluator.Evaluate(c, Ctx(0f, mask: 1 << 3), 0f, 0, 0));
        }

        [Test]
        public void Trigger_OccupyAllEnd_MaskEquals()
        {
            var c = Cfg(WaveEventFuncType.WAVE_TYPE_OCCUPY_ALLEND, 0f, 0b1011);
            Assert.IsFalse(WaveTriggerEvaluator.Evaluate(c, Ctx(0f, mask: 0b0011), 0f, 0, 0));
            Assert.IsTrue(WaveTriggerEvaluator.Evaluate(c, Ctx(0f, mask: 0b1011), 0f, 0, 0));
        }

        [Test]
        public void Trigger_UnknownType_FailClosed()
        {
            var c = Cfg((WaveEventFuncType)99);
            Assert.IsFalse(WaveTriggerEvaluator.Evaluate(c, Ctx(999f), 999f, 999, 999));
        }

        // --- batch math ---

        [Test]
        public void Batch_QuotaCap_SingleNum()
        {
            var w = StartWave(PoolSimple(num: 10, interval: 2f, single: 2, time: 999f));
            int spawned = 0;
            w.Tick(2f, _ => spawned++);  // t=2 → 2
            w.Tick(2f, _ => spawned++);  // t=4 → 4
            w.Tick(2f, _ => spawned++);  // t=6 → 6
            w.Tick(2f, _ => spawned++);  // t=8 → 8
            w.Tick(2f, _ => spawned++);  // t=10 → 10 (quota hết)
            w.Tick(2f, _ => spawned++);  // t=12 → không spawn thêm
            Assert.AreEqual(10, spawned, "quota cap");
            Assert.AreEqual(10, w.SpawnedTotal);
        }

        [Test]
        public void Batch_RoundRobin_MonsterList()
        {
            var pool = PoolSimple(num: 4, interval: 1f, single: 2, time: 999f);
            pool.Monsters.Clear();
            pool.Monsters.Add(new WaveMonsterConfig { MonsterId = 1 });
            pool.Monsters.Add(new WaveMonsterConfig { MonsterId = 2 });
            var w = StartWave(pool);
            var ids = new List<int>();
            w.Tick(1f, i => ids.Add(i.MonsterId));
            w.Tick(1f, i => ids.Add(i.MonsterId));
            CollectionAssert.AreEqual(new[] { 1, 2, 1, 2 }, ids);
        }

        [Test]
        public void Batch_DynamicPhase_LoopAndMissingProportion()
        {
            var pool = new WavePoolConfig
            {
                Time = 999f, Interval = 1f, SingleNum = 2, MonsterNum = 100, Isloop = true,
                DynamicMonsterTime = 5f, DynamicLoopNum = 1.5f, DynamicMonsterMaxNum = 8,
            };
            pool.Monsters.Add(new WaveMonsterConfig { MonsterId = 1 });
            var w = StartWave(pool);
            int spawned = 0;
            for (int t = 1; t <= 4; t++) w.Tick(1f, _ => spawned++); // 2/batch → alive 8 = cap
            Assert.AreEqual(8, spawned, "alive cap chặn batch thường");
            w.Tick(1f, _ => spawned++); // t=5 dynamic on, missing 0 → không spawn thêm
            Assert.AreEqual(8, spawned);
            w.OnMonsterKilled();
            w.OnMonsterKilled();        // alive 8→6
            w.Tick(0.6f, _ => spawned++); // t=5.6 dynamic: missing 2 ×1.5=3 → spawn tới cap → 2 con
            Assert.AreEqual(10, spawned, "batch dynamic tỉ lệ thiếu hụt");
            Assert.AreEqual(8, w.AliveCount);
        }

        [Test]
        public void Elite_Ratio_AllOrNone()
        {
            var pool = PoolSimple(num: 4, interval: 1f, single: 1, time: 999f);
            pool.EliteRatio = 1f;
            var w = StartWave(pool);
            int elites = 0;
            for (int t = 0; t < 4; t++) w.Tick(1f, i => { if (i.IsElite) elites++; });
            Assert.AreEqual(4, elites, "ratio 1 → toàn elite");

            pool.EliteRatio = 0f;
            var w2 = StartWave(pool);
            int elites2 = 0;
            for (int t = 0; t < 4; t++) w2.Tick(1f, i => { if (i.IsElite) elites2++; });
            Assert.AreEqual(0, elites2, "ratio 0 → không elite");
        }

        [Test]
        public void Spawn_BossAndEliteTierMultipliers()
        {
            var pool = PoolSimple(num: 3, interval: 1f, single: 3, time: 999f);
            pool.Monsters.Clear();
            pool.Monsters.Add(new WaveMonsterConfig { MonsterId = 1, IsBoss = true });
            pool.Monsters.Add(new WaveMonsterConfig { MonsterId = 2, IsElite = true });
            pool.Monsters.Add(new WaveMonsterConfig { MonsterId = 3 });
            var w = StartWave(pool);
            MonsterSpawnInfo boss = default, elite = default, normal = default;
            w.Tick(1f, i =>
            {
                if (i.IsBoss) boss = i;
                if (i.IsElite && !i.IsBoss) elite = i;
                if (!i.IsBoss && !i.IsElite) normal = i;
            });
            Assert.AreEqual(10f, boss.HpMul, 1e-4f);
            Assert.AreEqual(2f, boss.AtkMul, 1e-4f);
            Assert.AreEqual(0.75f, boss.SpeedMul, 1e-4f);
            Assert.AreEqual(2.5f, elite.HpMul, 1e-4f);
            Assert.AreEqual(1f, normal.HpMul, 1e-4f);
        }

        [Test]
        public void Finish_TimeOver_EndsAtTime()
        {
            var w = StartWave(PoolSimple(num: 100, interval: 1f, time: 10f));
            w.Tick(9.9f, _ => { });
            Assert.IsFalse(w.Finished);
            w.Tick(0.1f, _ => { });
            Assert.IsTrue(w.Finished, "elapsed ≥ Time → finish");
        }

        [Test]
        public void Finish_KillAll_WaitsQuotaDead()
        {
            var pool = PoolSimple(num: 4, interval: 1f, single: 2, time: 0f); // không giới hạn time
            var w = StartWave(pool, endType: 1);
            w.Tick(1f, _ => { }); // t=1 → 2 con
            w.Tick(1f, _ => { }); // t=2 → 4 con (quota done)
            Assert.AreEqual(4, w.SpawnedTotal);
            Assert.IsFalse(w.Finished);
            for (int i = 0; i < 3; i++) w.OnMonsterKilled();
            w.Tick(0.1f, _ => { });
            Assert.IsFalse(w.Finished, "chưa chết hết quota");
            w.OnMonsterKilled();
            Assert.IsTrue(w.Finished, "quota chết hết → finish");
        }

        // --- wave manager: sequence / trigger / ramp / loop ---

        [Test]
        public void Manager_Sequence_KillPercentTriggersNextWave_AndRamp()
        {
            var table = new List<LevelWaveConfig>
            {
                new LevelWaveConfig
                {
                    WaveId = 1, WaveType = WaveEventFuncType.WAVE_TIME_TRIGGER, TriggerTime = 1f,
                    Pool = PoolSimple(num: 4, interval: 2f), TriggerTimeout = 99f,
                },
                new LevelWaveConfig
                {
                    WaveId = 2, WaveType = WaveEventFuncType.WAVE_MONSTER_COUNT_PERCENT,
                    TriggerParams = new[] { 50 }, Pool = PoolSimple(num: 6, interval: 1f), TriggerTimeout = 99f,
                },
            };
            var mgr = new WaveManager();
            mgr.InitByDiyLevelWave(table);
            mgr.StartSpawn();
            var spawned = new List<MonsterSpawnInfo>();
            var ctx = new WaveTriggerContext { BossHpPercent = 1f };

            mgr.Tick(1f, spawned.Add, ctx); // t=1 → W1 active (interval 2 → chưa spawn)
            Assert.IsNotNull(mgr.Active);
            Assert.AreEqual(1, mgr.WaveIndex, "WaveIndex +1 sau create");
            Assert.AreEqual(0, spawned.Count);

            mgr.Tick(1f, spawned.Add, ctx); // t=2 → batch 2/4
            Assert.AreEqual(2, spawned.Count);
            mgr.OnMonsterKilled(spawned[0].MonsterId);
            mgr.OnMonsterKilled(spawned[1].MonsterId); // 2/4 = 50% → W2 fire
            mgr.Tick(0.1f, spawned.Add, ctx);

            Assert.AreEqual(2, mgr.WaveIndex, "W2 đã create");
            Assert.IsTrue(mgr.LastWaveCleanupMonsters, "W1 finish → cleanup flag");
            Assert.AreEqual(0.96f, mgr.Active.Interval, 1e-4f, "ramp wave idx 1: 1 × (1-0.04)");
        }

        [Test]
        public void Manager_TriggerTimeout_ForceStarts()
        {
            var table = new List<LevelWaveConfig>
            {
                new LevelWaveConfig
                {
                    WaveId = 1, WaveType = WaveEventFuncType.WAVE_TIME_TRIGGER, TriggerTime = 0.5f,
                    Pool = PoolSimple(num: 2, interval: 1f, single: 2, time: 0f), TriggerTimeout = 99f,
                },
                new LevelWaveConfig
                {
                    WaveId = 2, WaveType = WaveEventFuncType.WAVE_MONSTER_HP_PERCENT,
                    TriggerParams = new[] { 50 }, Pool = PoolSimple(num: 2, interval: 1f),
                    TriggerTimeout = 3f,
                },
            };
            var mgr = new WaveManager();
            mgr.InitByDiyLevelWave(table);
            mgr.StartSpawn();
            var ctx = new WaveTriggerContext { BossHpPercent = 1f }; // boss chết → HP% không bao giờ met
            var spawned = new List<MonsterSpawnInfo>();

            mgr.Tick(1f, spawned.Add, ctx); // t=1 → W1 active + batch 2/2 (quota done)
            mgr.OnMonsterKilled(spawned[0].MonsterId);
            mgr.OnMonsterKilled(spawned[1].MonsterId); // W1 finish (quota+allDead)
            mgr.Tick(1f, spawned.Add, ctx); // t=2 → W1 finalize, W2 pending từ ~t=1
            Assert.IsNull(mgr.Active);
            Assert.AreEqual(1, mgr.WaveIndex);
            mgr.Tick(3.2f, spawned.Add, ctx); // t=5.2 > pendingSince(≈1)+timeout(3) → force start
            Assert.IsNotNull(mgr.Active, "timeout force start");
            Assert.AreEqual(2, mgr.WaveIndex);
        }

        [Test]
        public void Manager_KillAllId_TriggersNextWave()
        {
            var pool = PoolSimple(num: 4, interval: 1f, single: 2, time: 0f);
            pool.Monsters.Clear();
            pool.Monsters.Add(new WaveMonsterConfig { MonsterId = 1000 });
            pool.Monsters.Add(new WaveMonsterConfig { MonsterId = 1002, IsElite = true });
            var table = new List<LevelWaveConfig>
            {
                new LevelWaveConfig { WaveId = 1, WaveType = WaveEventFuncType.WAVE_TIME_TRIGGER, TriggerTime = 0f,
                    Pool = pool, TriggerTimeout = 99f },
                new LevelWaveConfig { WaveId = 2, WaveType = WaveEventFuncType.WAVE_TYPE_KILL_ALL_MONSTER_THISID,
                    TriggerParams = new[] { 1002 }, Pool = PoolSimple(num: 2, interval: 1f), TriggerTimeout = 99f },
            };
            var mgr = new WaveManager();
            mgr.InitByDiyLevelWave(table);
            mgr.StartSpawn();
            var spawned = new List<MonsterSpawnInfo>();
            var ctx = new WaveTriggerContext { BossHpPercent = 1f };

            mgr.Tick(1f, spawned.Add, ctx); // batch 1: 1000, 1002
            mgr.Tick(1f, spawned.Add, ctx); // batch 2: 1000, 1002 → 2 elite spawn
            Assert.AreEqual(2, spawned.FindAll(i => i.MonsterId == 1002).Count);
            mgr.OnMonsterKilled(1002);
            mgr.OnMonsterKilled(1002); // kill-all id 1002
            mgr.Tick(0.1f, spawned.Add, ctx);
            Assert.AreEqual(2, mgr.WaveIndex, "W2 fire khi elite id 1002 chết hết");
            Assert.IsNotNull(mgr.Active);
        }

        [Test]
        public void Manager_OnMonsterKilled_IgnoresForeignId()
        {
            var table = new List<LevelWaveConfig>
            {
                new LevelWaveConfig { WaveId = 1, WaveType = WaveEventFuncType.WAVE_TIME_TRIGGER, TriggerTime = 0f,
                    Pool = PoolSimple(num: 2, interval: 1f), TriggerTimeout = 99f },
            };
            var mgr = new WaveManager();
            mgr.InitByDiyLevelWave(table);
            mgr.StartSpawn();
            var spawned = new List<MonsterSpawnInfo>();
            mgr.Tick(1f, spawned.Add, new WaveTriggerContext { BossHpPercent = 1f });
            mgr.OnMonsterKilled(999); // id không thuộc wave
            Assert.AreEqual(0, mgr.Active.KilledCount, "kill id lạ không đếm");
        }

        [Test]
        public void Manager_LoopTable_RampsAcrossLoops_OrTableDone()
        {
            var one = new LevelWaveConfig
            {
                WaveId = 1, WaveType = WaveEventFuncType.WAVE_TIME_TRIGGER, TriggerTime = 0f,
                Pool = PoolSimple(num: 2, interval: 1f, single: 2, time: 0f), TriggerTimeout = 99f,
            };
            var table = new List<LevelWaveConfig> { one };

            // LoopTable = true (default): hết table → loop, WaveIndex cumulative → ramp tiếp
            var mgr = new WaveManager();
            mgr.InitByDiyLevelWave(table);
            mgr.StartSpawn();
            var spawned = new List<MonsterSpawnInfo>();
            var ctx = new WaveTriggerContext { BossHpPercent = 1f };
            mgr.Tick(1f, spawned.Add, ctx); // W1 batch 2/2
            mgr.OnMonsterKilled(spawned[0].MonsterId);
            mgr.OnMonsterKilled(spawned[1].MonsterId);
            mgr.Tick(1f, spawned.Add, ctx); // W1 finish → loop → pending = W1 (idx 1)
            Assert.IsNull(mgr.Active, "wave vừa finish, chờ trigger vòng mới");
            mgr.Tick(1f, spawned.Add, ctx); // loop fire → W1 vòng 2
            Assert.IsNotNull(mgr.Active, "loop lại wave đầu");
            Assert.AreEqual(2, mgr.WaveIndex);
            Assert.AreEqual(0.96f, mgr.Active.Interval, 1e-4f, "ramp áp lại từ đầu vòng 2");

            // LoopTable = false: hết table → TableDone
            var mgr2 = new WaveManager { LoopTable = false };
            mgr2.InitByDiyLevelWave(table);
            mgr2.StartSpawn();
            var spawned2 = new List<MonsterSpawnInfo>();
            mgr2.Tick(1f, spawned2.Add, ctx);
            mgr2.OnMonsterKilled(spawned2[0].MonsterId);
            mgr2.OnMonsterKilled(spawned2[1].MonsterId);
            mgr2.Tick(1f, spawned2.Add, ctx);
            Assert.IsTrue(mgr2.TableDone, "table hết → BattleFinsh hook");
            Assert.IsNull(mgr2.Active);
        }

        [Test]
        public void DefaultTable_HasCoverage_Boss_Swarm_Elite_Ratio()
        {
            var t = WaveManager.DefaultTable();
            Assert.AreEqual(6, t.Count);
            Assert.AreEqual(WaveEventFuncType.WAVE_TIME_TRIGGER, t[0].WaveType);
            Assert.IsTrue(t[3].Pool.Monsters.Exists(m => m.IsBoss), "W4 có boss flag");
            Assert.IsFalse(t[3].IsDeleteAllMonster, "boss wave giữ boss sống cho W5 HP%");
            Assert.IsTrue(t[4].Pool.Isloop && t[4].Pool.DynamicMonsterMaxNum > 0, "W5 swarm dynamic");
            Assert.IsTrue(t[4].Pool.Monsters.Exists(m => m.IsElite), "W5 elite flag chủ động");
            Assert.AreEqual(0.2f, t[2].Pool.EliteRatio, 1e-4f, "W3 elite ratio");
            Assert.AreEqual(WaveEventFuncType.WAVE_MONSTER_HP_PERCENT, t[4].WaveType);
        }
    }
}
