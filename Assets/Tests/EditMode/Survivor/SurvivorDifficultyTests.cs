// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorDifficultyTests
// Ticket 41 self-check: difficulty curve (ramp linear + chaos threshold) +
// endless manager behavior (loop vo han, boss respawn scale qua template
// override, death stop). Pure logic — không scene/PlayMode (spec Testing
// Decisions: EditMode pure-logic seam duy nhat).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorDifficultyTests
    {
        // --- helpers (tách file riêng, không chạm SurvivorWaveLogicTests) ---

        private static WavePoolConfig PoolQ(int num, float interval = 1f, int single = 1)
        {
            var p = new WavePoolConfig
            {
                Time = 999f, Interval = interval, SingleNum = single, MonsterNum = num,
            };
            p.Monsters.Add(new WaveMonsterConfig { MonsterId = 1 });
            return p;
        }

        /// <summary>Wave TIME trigger 0s + EndType 1 (kill quota) → chain nhanh trong test.</summary>
        private static LevelWaveConfig WaveImmediate(WavePoolConfig pool)
        {
            return new LevelWaveConfig
            {
                WaveId = 1, WaveType = WaveEventFuncType.WAVE_TIME_TRIGGER, TriggerTime = 0f,
                Pool = pool, EndType = 1, IsDeleteAllMonster = true, TriggerTimeout = 999f,
            };
        }

        /// <summary>
        /// Chạy waves liên tiếp: mỗi wave tick tạo + spawn → kill TẤT CẢ spawn mới
        /// (OnMonsterKilled theo MonsterId) → EndType 1 finish → finalize → wave kế.
        /// </summary>
        private static void RunKillChain(WaveManager mgr, int waves, List<MonsterSpawnInfo> spawned)
        {
            // Chạy đúng `waves` wave: mỗi tick spawn + kill NGAY mọi con mới (con spawn trễ
            // theo interval cũng bị kill) → wave finish chắc chắn, không kẹt, không cascade
            // (WaveIndex đạt waves thì dừng). Fix 41: trước đây snapshot kill tĩnh → con
            // spawn trễ sống vĩnh viễn → wave không finish.
            var ctx = new WaveTriggerContext { BossHpPercent = 1f };
            int guard = 0;
            while (mgr.WaveIndex < waves && guard < 4000)
            {
                int bb = spawned.Count;
                mgr.Tick(0.5f, spawned.Add, ctx);
                for (int i = bb; i < spawned.Count; i++) mgr.OnMonsterKilled(spawned[i].MonsterId);
                guard++;
            }
            // kết thúc: drain tới khi Active null (finish + wrap pending) — WaveIndex giữ
            // nguyên (guard WaveIndex <= waves chặn create wave mới) → tick tiếp theo của
            // test tạo wave kế.
            while (mgr.Active != null && mgr.WaveIndex <= waves && guard < 4000)
            {
                int bb = spawned.Count;
                mgr.Tick(0.5f, spawned.Add, ctx);
                for (int i = bb; i < spawned.Count; i++) mgr.OnMonsterKilled(spawned[i].MonsterId);
                guard++;
            }
        }

        // --- DifficultyCurve: ramp tuyến tính (spec D15 linear v1) ---

        [Test]
        public void Curve_Ramp_Monotonic_HpAtkSpeed_Up_Interval_Down()
        {
            var c = new DifficultyCurve();
            // wave 1 = gốc (multiplier 1)
            Assert.AreEqual(1f, c.HpMul(1), 1e-4f);
            Assert.AreEqual(1f, c.AtkMul(1), 1e-4f);
            Assert.AreEqual(1f, c.SpeedScale(1), 1e-4f);
            Assert.AreEqual(1f, c.IntervalMul(1), 1e-4f);
            // tăng theo wave (monotonic)
            Assert.Greater(c.HpMul(10), c.HpMul(4));
            Assert.Greater(c.AtkMul(10), c.AtkMul(4));
            Assert.Greater(c.SpeedScale(10), c.SpeedScale(4));
            Assert.Less(c.IntervalMul(10), c.IntervalMul(4), "interval giảm = spawn dày hơn");
            // cumulative (loop không reset)
            Assert.Greater(c.HpMul(21), c.HpMul(11));
        }

        [Test]
        public void Curve_Ramp_LegacyIntervalFormula_Compatible()
        {
            var c = new DifficultyCurve(); // default = legacy ticket 30 (-4%/wave, floor 0.45)
            Assert.AreEqual(1f, c.IntervalMul(1), 1e-4f);
            Assert.AreEqual(0.96f, c.IntervalMul(2), 1e-4f, "khớp test 30 (wave 2 = 0.96)");
            Assert.AreEqual(0.45f * 0.8f, c.IntervalMul(1000), 1e-4f, "chaos floor × ChaosIntervalMul");
            var noChaos = new DifficultyCurve { ChaosAtWave = 1, ChaosIntervalMul = 1f };
            Assert.AreEqual(0.45f, noChaos.IntervalMul(1000), 1e-4f, "no-chaos clamp floor 0.45");
            Assert.AreEqual(0, c.QuotaAdd(3));
            Assert.AreEqual(1, c.QuotaAdd(5));
            Assert.AreEqual(0, c.CapAdd(7));
            Assert.AreEqual(1, c.CapAdd(9));
        }

        // --- chaos threshold: normal → chaos ---

        [Test]
        public void Curve_ChaosThreshold_Switch()
        {
            var c = new DifficultyCurve { ChaosAtWave = 5, ChaosHpMul = 2f, ChaosAtkMul = 2f, ChaosIntervalMul = 0.9f };
            Assert.IsFalse(c.IsChaos(4));
            Assert.IsTrue(c.IsChaos(5), "từ wave 5 là chaos");
            Assert.AreEqual(1.15f, c.HpMul(4), 1e-4f, "trước ngưỡng: tuyến");
            Assert.AreEqual(1.2f * 2f, c.HpMul(5), 1e-4f, "tại ngưỡng: linear × ChaosHpMul");
            Assert.AreEqual(0.84f * 0.9f, c.IntervalMul(5), 1e-4f, "clamp(1-0.16)=0.84 × chaos 0.9");
            Assert.Greater(c.HpMul(6), c.HpMul(5), "vẫn monotonic qua ngưỡng");
        }

        [Test]
        public void Curve_ChaosThreshold_DefaultAtWave10()
        {
            var c = new DifficultyCurve();
            Assert.AreEqual(10, c.ChaosAtWave);
            Assert.IsFalse(c.IsChaos(9));
            Assert.IsTrue(c.IsChaos(10));
            Assert.Greater(c.HpMul(10), 1f + c.HpPerWave * 9f, "chaos nhân thêm ChaosHpMul");
        }

        // --- boss schedule: định kỳ + respawn scale cao hơn ---

        [Test]
        public void Curve_BossSchedule_FrequencyAndEscalation()
        {
            var c = new DifficultyCurve();
            Assert.IsFalse(c.IsBossWave(1), "wave 1 không boss");
            Assert.IsTrue(c.IsBossWave(4), "wave 4 boss đầu (khớp DefaultTable W4)");
            Assert.IsFalse(c.IsBossWave(5));
            Assert.IsTrue(c.IsBossWave(8));
            Assert.IsFalse(c.IsBossWave(9));
            Assert.IsTrue(c.IsBossWave(12), "chaos → boss dày hơn (3 wave)");
            Assert.IsFalse(c.IsBossWave(10), "wave 10 chưa boss");
            Assert.IsTrue(c.IsBossWave(15));
            // ordinal + hp respawn scale
            Assert.AreEqual(1, c.BossOrdinal(4));
            Assert.AreEqual(2, c.BossOrdinal(8));
            Assert.AreEqual(3, c.BossOrdinal(12));
            Assert.AreEqual(1f, c.BossHpScale(4), 1e-4f, "boss đầu không bonus");
            Assert.AreEqual(1.5f, c.BossHpScale(8), 1e-4f, "boss 2 ×1.5");
            Assert.AreEqual(2f, c.BossHpScale(12), 1e-4f, "boss 3 ×2 (linear)");
        }

        [Test]
        public void Curve_BossHpScale_Custom_RespawnStep()
        {
            var c = new DifficultyCurve { BossEveryBase = 3, BossHpPerRespawn = 0.25f };
            Assert.IsTrue(c.IsBossWave(3));
            Assert.IsTrue(c.IsBossWave(6));
            Assert.AreEqual(1f, c.BossHpScale(3), 1e-4f);
            Assert.AreEqual(1.25f, c.BossHpScale(6), 1e-4f);
            Assert.AreEqual(1.5f, c.BossHpScale(9), 1e-4f);
        }

        // --- EndlessMode driver ---

        [Test]
        public void Endless_Stop_SetsRunningFalse()
        {
            var e = new EndlessMode();
            Assert.IsTrue(e.Running);
            e.Stop();
            Assert.IsFalse(e.Running);
            e.Stop(); // gameover path — vĩnh viễn
            Assert.IsFalse(e.Running);
        }

        // --- WaveManager + Endless: loop vô hạn, ramp cumulative, không TableDone ---

        [Test]
        public void Endless_Loop_NeverStops_NoBossTable_FailClosed()
        {
            var mgr = new WaveManager();
            mgr.InitByDiyLevelWave(new List<LevelWaveConfig> { WaveImmediate(PoolQ(num: 2, single: 2)) });
            mgr.StartSpawn();
            var spawned = new List<MonsterSpawnInfo>();
            RunKillChain(mgr, 30, spawned);

            Assert.IsFalse(mgr.TableDone, "LoopTable mặc định ON → không bao giờ done");
            Assert.AreEqual(30, mgr.WaveIndex, "30 wave chạy hết (loop không kẹt)");
            // loop vẫn mở: tick tiếp → wave 31 (endless không hồi kết)
            mgr.Tick(1f, spawned.Add, new WaveTriggerContext { BossHpPercent = 1f });
            Assert.AreEqual(31, mgr.WaveIndex, "endless không dừng");
            Assert.IsNotNull(mgr.Active);
            // table không boss → không bao giờ spawn boss (fail-closed override)
            Assert.IsFalse(spawned.Exists(i => i.IsBoss), "không template boss → không chèn boss");
            // ramp cumulative: IntervalMul(31) = 0.45×0.8 (chaos) < 0.96 (wave 2)
            Assert.Less(mgr.Active.Interval, 0.96f, "ramp vẫn áp cuối chuỗi");
        }

        [Test]
        public void Endless_StopOnDeath_BlocksNewWaves()
        {
            var e = new EndlessMode { PollPlayerDead = true, IsPlayerDead = () => true };
            var mgr = new WaveManager { Endless = e };
            mgr.InitByDiyLevelWave(new List<LevelWaveConfig> { WaveImmediate(PoolQ(num: 2, single: 2)) });
            mgr.StartSpawn();
            var spawned = new List<MonsterSpawnInfo>();
            RunKillChain(mgr, 5, spawned);

            Assert.IsFalse(e.Running, "IsPlayerDead=true → Stop ở Tick đầu");
            Assert.AreEqual(0, mgr.WaveIndex, "không wave nào được tạo sau khi stop");
            Assert.IsNull(mgr.Active);
            // Stop vĩnh viễn: poll gỡ bỏ vẫn không resume
            e.IsPlayerDead = null;
            RunKillChain(mgr, 5, spawned);
            Assert.AreEqual(0, mgr.WaveIndex, "Stop không resume");
        }

        [Test]
        public void Endless_BossRespawn_ScaleHigherEachWave_WithTemplate()
        {
            // Table: row0 = boss template (có IsBoss), row1 = normal → rotation:
            // wave1,3 = boss row tự nhiên (không bonus), wave 4/8/12 = normal row
            // + override (template boss chèn) → boss HP scale tăng dần.
            var bossPool = PoolQ(num: 2, single: 2);
            bossPool.Monsters.Clear();
            bossPool.Monsters.Add(new WaveMonsterConfig { MonsterId = 2000, IsBoss = true });
            bossPool.Monsters.Add(new WaveMonsterConfig { MonsterId = 1000 });

            var normalPool = PoolQ(num: 2, single: 2);

            var curve = new DifficultyCurve { HpPerWave = 0f, AtkPerWave = 0f, ChaosAtWave = 999 }; // tắt chaos — cô lập boss scale // cô lập boss scale
            var mgr = new WaveManager { Curve = curve };
            mgr.InitByDiyLevelWave(new List<LevelWaveConfig>
            {
                WaveImmediate(bossPool),   // row 0 (boss)
                WaveImmediate(normalPool), // row 1 (normal)
            });
            mgr.StartSpawn();
            var spawned = new List<MonsterSpawnInfo>();
            RunKillChain(mgr, 12, spawned);

            // boss spawns theo thứ tự wave: 1,3 (row tự nhiên), 4/8/12 (override)
            var bossHps = spawned.FindAll(i => i.IsBoss);
            Assert.GreaterOrEqual(bossHps.Count, 3, "≥3 boss spawn (wave1 row + override 4,8,12)");
            Assert.AreEqual(10f, bossHps[0].HpMul, 1e-4f, "boss đầu (row tự nhiên) = tier 10 × base 1");
            Assert.AreEqual(20f, bossHps[^1].HpMul, 1e-4f, "wave 12 = BossOrdinal 3 → 1+0.5×2 = 2× → 10×2");
            Assert.Greater(bossHps[^1].HpMul, bossHps[0].HpMul, "boss cuối > boss đầu — respawn scale cao hơn");
            // normal monster ở wave boss override cũng nhân BossHpScale (pool-level) — fail-closed check ko fail
            Assert.GreaterOrEqual(spawned.Count, 12, "đủ spawn các wave (tối thiểu 1/wave)");
        }

        [Test]
        public void Endless_BossRespawn_ChaosEvery3_Scale()
        {
            var c = new DifficultyCurve();
            // chaos phase: 12,15,18 boss (step 3) — ordinal tăng
            Assert.AreEqual(3, c.BossOrdinal(12));
            Assert.AreEqual(4, c.BossOrdinal(15));
            Assert.AreEqual(2f, c.BossHpScale(12), 1e-4f);
            Assert.AreEqual(2.5f, c.BossHpScale(15), 1e-4f);
        }
    }
}