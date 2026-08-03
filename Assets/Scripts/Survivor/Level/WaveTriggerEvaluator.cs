// -----------------------------------------------------------------------------
// VLTK.Survivor — WaveTriggerEvaluator: đánh giá điều kiện start của 1 wave
// Parity: WaveFuncCtr.CreateWaveFunc cast (WaveEventFuncType)cfg + WaveFuncByX.Trigger
// (BattleCore.WaveFuncCtr.cs / WaveFuncByTime.cs / WaveFuncByMonsterCount.cs ...).
// Thuần static → EditMode self-check trực tiếp, không cần scene.
// -----------------------------------------------------------------------------

namespace VLTK.Survivor
{
    public static class WaveTriggerEvaluator
    {
        /// <summary>
        /// Trigger met?
        /// activeDiePercent — kill% của WAVE ĐANG CHẠY (parity LevelWave.GetCurWaveDieMonstePercent);
        /// killedOfTargetId/totalOfTargetId — kill-all theo MonsterID (type 5/6).
        /// Fail-closed: type không biết → false.
        /// </summary>
        public static bool Evaluate(LevelWaveConfig cfg, WaveTriggerContext ctx,
            float activeDiePercent, int killedOfTargetId, int totalOfTargetId)
        {
            if (cfg == null) return false;
            int p0 = cfg.TriggerParams != null && cfg.TriggerParams.Length > 0 ? cfg.TriggerParams[0] : 0;
            switch (cfg.WaveType)
            {
                case WaveEventFuncType.WAVE_TIME_TRIGGER:
                    return ctx.Elapsed >= cfg.TriggerTime;

                case WaveEventFuncType.WAVE_MONSTER_COUNT_PERCENT:
                    return activeDiePercent >= p0;

                case WaveEventFuncType.WAVE_MONSTER_HP_PERCENT:
                    return p0 > 0 && ctx.BossHpPercent <= p0 / 100f;

                case WaveEventFuncType.WAVE_MONSTER_PLAY_SKILL:
                    return ctx.SkillCastCount >= p0;

                case WaveEventFuncType.WAVE_TYPE_KILL_ALL_MONSTER_THISID:
                    return totalOfTargetId > 0 && killedOfTargetId >= totalOfTargetId;

                case WaveEventFuncType.WAVE_TYPE_KILLALL_AND_TIMEOVER_THISID:
                    return totalOfTargetId > 0 && killedOfTargetId >= totalOfTargetId && ctx.Elapsed >= cfg.TriggerTime;

                case WaveEventFuncType.WAVE_TYPE_OCCUPY_START:
                    return (ctx.OccupiedMask & 0xFF) != 0; // bất kỳ điểm nào trong 8 điểm đầu

                case WaveEventFuncType.WAVE_TYPE_OCCUPY_END:
                    return p0 >= 0 && p0 < 32 && (ctx.OccupiedMask & (1 << p0)) != 0;

                case WaveEventFuncType.WAVE_TYPE_OCCUPY_ALLEND:
                    return p0 != 0 && (ctx.OccupiedMask & p0) == p0; // own: p0 = bitmask điểm cần chiếm

                default:
                    return false; // fail-closed
            }
        }
    }
}
