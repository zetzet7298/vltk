// -----------------------------------------------------------------------------
// VLTK.Survivor — EndlessMode (ticket 41, endless driver)
// Trách nhiệm: gate "running" (ngắt khi player chết — poll, fail-closed) +
// boss schedule delegate → DifficultyCurve (IsBossWave / BossHpScale).
//
// Loop vô hạn + xoay vòng wave templates = WaveManager.LoopTable (đã có từ 30,
// placeholder ticket 41) — EndlessMode không lặp lại queue đó. Ramp tuyến tính
// = DifficultyCurve (WaveManager áp qua RampCopy/WrapSpawn). Boss-wave insert +
// template pool = WaveManager.CreateWave (consult Endless.IsBossWave).
//
// Runtime death-stop: SurvivorGameDirector.OnPlayerDied đã pause (timescale 0 +
// _paused → Update dừng) nên spawn ngừng tự nhiên. IsPlayerDead poll đây là
// double-guard fail-closed: nếu director chưa dừng (hoặc mode tương lai không
// pause), Running=false chặn tạo wave mới tại WaveManager.Tick.
//
// Thuần core (Running/IsBossWave/BossHpScale) — không scene, EditMode test được.
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Survivor
{
    /// <summary>
    /// Driver gate cho endless. WaveManager giữ 1 instance mặc định = ON
    /// (như LoopTable=true). Stop() → WaveManager ngừng mọi create wave mới.
    /// Fail-closed: IsPlayerDead null / không có director → không tự dừng.
    /// </summary>
    public sealed class EndlessMode
    {
        /// <summary>Curve dùng cho boss schedule (IsBossWave/BossHpScale). WaveManager
        /// sync về Curve của nó trong ctor → ramp + boss đọc cùng 1 bộ số.</summary>
        public DifficultyCurve Curve = new DifficultyCurve();

        /// <summary>true = endless chạy. Stop() set false — không resume (gameover path).</summary>
        public bool Running = true;

        /// <summary>Poll player dead mỗi Tick → Stop(). Runtime tự nối tới director.</summary>
        public bool PollPlayerDead = true;

        /// <summary>Hook poll: null = không poll. Editor test inject Func giả.</summary>
        public Func<bool> IsPlayerDead;

        public EndlessMode()
        {
            IsPlayerDead = DefaultPlayerDead;
        }

        public void Stop()
        {
            Running = false;
        }

        /// <summary>Wave này là boss wave (theo curve schedule).</summary>
        public bool IsBossWave(int wave) => Curve.IsBossWave(wave);

        /// <summary>Boss HP scale cho wave (respawn scale > 1 từ boss thứ 2).</summary>
        public float BossHpScale(int wave) => Curve.BossHpScale(wave);

        /// <summary>Mặc định: director có player dead? null instance → false (fail-closed).</summary>
        private static bool DefaultPlayerDead()
        {
            var d = SurvivorGameDirector.Instance;
            return d != null && d.Player != null && d.Player.Dead;
        }
    }
}