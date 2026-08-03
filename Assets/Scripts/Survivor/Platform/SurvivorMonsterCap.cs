// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorMonsterCap (ticket 42, backstop runtime)
// File riêng: 1 MonoBehaviour/file (Unity 6 resolve scene reference fail với
// multi-class file — PerfBudgetMonitor đã dính).
//
//  - Enforce cap: LateUpdate đếm SurvivorGameDirector.Monsters, vượt cap →
//    trim front-first (kẻ sống lâu nhất), KHÔNG bao giờ đụng boss
//    (exempt = ActiveBoss.Monster), KHÔNG gọi OnMonsterKilled (despawn ≠ kill:
//    không XP/gem/kills fake).
//  - Spawn-gate: đăng ký SurvivorGameDirector.MonsterSpawnGate (đòn bẩy 60fps —
//    không tạo monster rồi trim). Đăng ký lazy (LateUpdate, an toàn thứ tự Awake
//    với director); OnDestroy hủy đăng ký.
//  - Fail-closed: director null → không đụng gì; cap cấu hình ≤ 0 → DefaultCap;
//    không đủ non-exempt → trim ít hơn, không bao giờ vượt excess.
//
// ponytail: trim destroy trực tiếp → _monsterIds/_monsterWave (private trong
// director) giữ entry stale; bounded bởi tổng spawn, chi phí = vài entry —
// chấp nhận. Upgrade khi director có RemoveMonster public.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>
    /// Enforce cap runtime. Scene: thêm component này lên SurvivorDirector GO.
    /// Trim = Destroy + Monsters.Remove — không XP/gem (despawn, fail-closed).
    /// </summary>
    public sealed class SurvivorMonsterCap : MonoBehaviour
    {
        [Tooltip("Cap config — default 80 (own, cap table profiling plan).")]
        public MonsterCapConfig Config = new MonsterCapConfig();

        [Tooltip("Tắt → chỉ theo dõi, không trim (debug).")]
        public bool TrimExcess = true;

        /// <summary>Số trim frame gần nhất (profiling/debug).</summary>
        public int LastTrimCount { get; private set; }

        public int ActiveCount => SurvivorGameDirector.Instance != null
            ? SurvivorGameDirector.Instance.Monsters.Count : 0;

        private System.Func<int, bool> _gate; // cache delegate — không alloc/frame ở LateUpdate

        private void Awake()
        {
            // gate nhận activeCount từ director mỗi lần gọi → số liệu tươi
            _gate = active => MonsterCapPolicy.CanSpawn(active,
                Config != null ? Config.MaxMonsters : 0);
        }

        private void LateUpdate()
        {
            EnsureGate();
            if (!TrimExcess) return;
            var d = SurvivorGameDirector.Instance;
            if (d == null) return; // fail-closed: không director → không đụng gì
            int cap = MonsterCapPolicy.EffectiveCap(Config != null ? Config.MaxMonsters : 0);
            var boss = d.ActiveBoss != null ? d.ActiveBoss.Monster : null;
            var toTrim = MonsterCapPolicy.PickTrimIndices(d.Monsters.Count, cap,
                i => boss != null && d.Monsters[i] == boss);
            LastTrimCount = toTrim.Count;
            if (LastTrimCount == 0) return;
            for (int k = toTrim.Count - 1; k >= 0; k--) // index giảm dần → không lệch list
            {
                var m = d.Monsters[toTrim[k]];
                if (m == null) continue;
                d.Monsters.Remove(m); // KHÔNG OnMonsterKilled — despawn, không XP/kills fake
                Destroy(m.gameObject);
            }
        }

        /// <summary>
        /// Đăng ký spawn-gate khi director sẵn sàng (LateUpdate chạy sau mọi Awake).
        /// Chỉ set khi null — không cướp gate của component khác. OnDestroy hủy.
        /// </summary>
        private void EnsureGate()
        {
            var d = SurvivorGameDirector.Instance;
            if (d == null || _gate == null) return;
            if (d.MonsterSpawnGate == null) d.MonsterSpawnGate = _gate;
        }

        private void OnDestroy()
        {
            var d = SurvivorGameDirector.Instance;
            if (d != null && _gate != null && d.MonsterSpawnGate == _gate)
                d.MonsterSpawnGate = null;
        }
    }
}
