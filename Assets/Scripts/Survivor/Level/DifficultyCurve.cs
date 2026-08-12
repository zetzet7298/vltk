// -----------------------------------------------------------------------------
// VLTK.Survivor — DifficultyCurve (ticket 41, endless ramp)
// Pure logic — không combat, không scene → EditMode test trực tiếp.
//
// Own-design (spec D15, research 06 O1): dhcd KHÔNG có difficulty-ramp
// declaration (chỉ wave-loop skeleton IsReposeWave + WaveRefresh dynamic caps +
// GetEndlessWaveCount). Toàn bộ hệ số own numeric, rationale inline — không
// clone dhcd data.
//
// SPEC: linear v1 (spec D15) — scale = f(wave), wave đánh số 1-based
// (wave 1 = đợt đầu). exponential/stair-step = upgrade path sau playtest
// (fog "Difficulty feel" map.md).
//
// LEGACY COMPAT: IntervalPerWave/IntervalFloor, QuotaPerWaves, CapPerWaves mặc
// định = công thức ramp cũ của WaveManager (ticket 30: interval −4%/wave floor
// 0.45, quota +1/4 wave, cap +1/8 wave) → hành vi cũ giữ nguyên, tests 30 xanh.
// MỚI (ticket 41): Hp/Atk/Speed ramp, BatchAdd, chaos mode, boss schedule +
// boss HP respawn scale.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>
    /// Ramp + chaos + boss schedule cho endless. Thuần (không state biến đổi),
    /// method tính theo wave → EditMode test đơn giản, fail-closed: chỉ số
    /// ngoài khoảng hợp lệ → clamp/saturation tại method gọi (WaveManager).
    /// </summary>
    public sealed class DifficultyCurve
    {
        // --- ramp linear (scale tăng theo wave) ---

        /// <summary>+5% HP quái mỗi wave (cumulative). Rationale: run 15 phút ≈ 30-35
        /// wave → HP ~×3.5 cuối run ép player gom card; không nhồn quá ×4.</summary>
        public float HpPerWave = 0.05f;
        /// <summary>+4% dmg mỗi wave — thấp hơn HP (player có i-frame), dmg leo chậm hơn HP để có khoảng né.</summary>
        public float AtkPerWave = 0.04f;
        /// <summary>+1.5% tốc độ mỗi wave — quái dí hơn nhưng vẫn dưới MoveSpeed player (1.6 → ~2.6 ở wave 33).</summary>
        public float SpeedPerWave = 0.015f;
        /// <summary>−4% spawn interval mỗi wave (multiplicative, chạm IntervalFloor). Legacy ticket 30.</summary>
        public float IntervalPerWave = 0.04f;
        /// <summary>Floor interval — chống spawn nhồn (batching dưới 0.45s). Legacy ticket 30.</summary>
        public float IntervalFloor = 0.45f;
        /// <summary>+1 quota tổng mỗi 4 wave. Legacy ticket 30.</summary>
        public int QuotaPerWaves = 4;
        /// <summary>+1 dynamic alive cap mỗi 8 wave (swarm). Legacy ticket 30.</summary>
        public int CapPerWaves = 8;
        /// <summary>+1 con/batch mỗi 6 wave (SingleNum) — quái "nhiều hơn" song song. MỚI ticket 41.</summary>
        public int BatchPerWaves = 6;

        // --- chaos mode — threshold normal→chaos ---

        /// <summary>wave ≥ ChaosAtWave → chaos. Wave 10 (1-based) ≈ giữa cycle 2 của
        /// DefaultTable (6 wave/cycle) — mốc "đổi nhịp" dễ nhận ra sau khi quen nhịp normal.</summary>
        public int ChaosAtWave = 10;
        /// <summary>chaos: HP ×1.5 thêm (step nhảy tại ngưỡng — tạo cảm giác mode đổi rõ).</summary>
        public float ChaosHpMul = 1.5f;
        /// <summary>chaos: dmg ×1.25 thêm.</summary>
        public float ChaosAtkMul = 1.25f;
        /// <summary>chaos: spawn interval ×0.8 (dày hơn).</summary>
        public float ChaosIntervalMul = 0.8f;

        // --- boss schedule (endless) — frequency tăng + hp respawn scale ---

        /// <summary>normal: boss mỗi 4 wave (wave 4, 8, ...). 4 = DefaultTable W4 (boss đầu) tự khớp.</summary>
        public int BossEveryBase = 4;
        /// <summary>chaos: boss mỗi 3 wave — boss gặp nhiều hơn khi vào chaos (12, 15, 18,...).</summary>
        public int BossEveryChaos = 3;
        /// <summary>boss tái xuất +50% HP mỗi lần (BossHpScale(ordinal) = 1 + 0.5×(ordinal−1);
        /// boss 2 = ×1.5, boss 3 = ×2,... linear v1 — chốt sau playtest).</summary>
        public float BossHpPerRespawn = 0.5f;

        // --- ramps ---

        /// <summary>Chaos? wave ≥ ChaosAtWave (1-based).</summary>
        public bool IsChaos(int wave) => wave >= ChaosAtWave;

        /// <summary>HP multiplier wave này. linear trước ngưỡng, ×ChaosHpMul từ ngưỡng.</summary>
        public float HpMul(int wave) => (1f + HpPerWave * (wave - 1)) * (IsChaos(wave) ? ChaosHpMul : 1f);

        /// <summary>Dmg multiplier.</summary>
        public float AtkMul(int wave) => (1f + AtkPerWave * (wave - 1)) * (IsChaos(wave) ? ChaosAtkMul : 1f);

        /// <summary>Speed multiplier (WaveManager áp lên SpeedMul spawn).</summary>
        public float SpeedScale(int wave) => 1f + SpeedPerWave * (wave - 1);

        /// <summary>Interval multiplier — giảm dần, clamp IntervalFloor, chaos ×0.8.</summary>
        public float IntervalMul(int wave) =>
            Mathf.Max(IntervalFloor, 1f - IntervalPerWave * (wave - 1)) * (IsChaos(wave) ? ChaosIntervalMul : 1f);

        /// <summary>+quota tổng.</summary>
        public int QuotaAdd(int wave) => (wave - 1) / QuotaPerWaves;

        /// <summary>+dynamic cap swarm.</summary>
        public int CapAdd(int wave) => (wave - 1) / CapPerWaves;

        /// <summary>+con/batch.</summary>
        public int BatchAdd(int wave) => (wave - 1) / BatchPerWaves;

        // --- boss ---

        /// <summary>Wave này là boss wave? Step theo mode (normal 4 → chaos 3).</summary>
        public bool IsBossWave(int wave)
        {
            if (wave <= 0) return false;
            int step = IsChaos(wave) ? BossEveryChaos : BossEveryBase;
            return wave % step == 0;
        }

        /// <summary>Số boss đã xuất hiện tính tới wave (kể cả wave này). O(wave) — run ~30-50
        /// wave nên loop nhỏ; cùng kernel với IsBossWave → không lệch đếm.</summary>
        public int BossOrdinal(int wave)
        {
            int cnt = 0;
            for (int w = 1; w <= wave; w++)
            {
                int step = IsChaos(w) ? BossEveryChaos : BossEveryBase;
                if (w % step == 0) cnt++;
            }
            return cnt;
        }

        /// <summary>Boss HP scale khi tái xuất: boss đầu = 1× (gốc), boss 2 = 1.5×, boss 3 = 2×,...</summary>
        public float BossHpScale(int wave) => 1f + BossHpPerRespawn * Mathf.Max(0, BossOrdinal(wave) - 1);
    }
}