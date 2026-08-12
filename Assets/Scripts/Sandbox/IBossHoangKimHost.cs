// -----------------------------------------------------------------------------
// VLTK Mobile — ST-08.13 Boss Hoàng Kim Host Interface (Unity → sandbox)
// PC source: settings/boss/bosshoangkim.txt, jx_linux_y + script/battles/boss
// Unity runtime dispatches load / spawn / kill / respawn events to a host
// implementation that owns visuals, audio, UI, save/load.
// Vietnamese: "Boss Hoàng Kim", "Hồi Sinh", "Trọng Thương", "Tọa Độ".
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host boundary cho <see cref="BossHoangKimService"/>. Decouples sandbox
    /// logic (registry parse, kill/respawn timers, active-boss filter) from
    /// Unity-side visuals (boss bar, kill toast, reward popup) and persistence
    /// (last death time, kill announcements, log file).
    ///
    /// All methods are best-effort callbacks. Implementations must tolerate
    /// null/invalid args (boss not found, etc.) — sandbox never throws.
    /// </summary>
    public interface IBossHoangKimHost
    {
        // ── Registry lifecycle ────────────────────────────────────────────
        /// <summary>Boss catalog loaded — count of registered bosses.</summary>
        void OnBossRegistryAttached(int bossCount);

        /// <summary>Boss entry resolved by id — null if not found.</summary>
        void OnBossResolved(int bossId, int mapId, int respawnSec, int level);

        // ── Spawn / Death lifecycle ───────────────────────────────────────
        /// <summary>Boss spawn dispatched (initial spawn or respawn).</summary>
        void OnBossSpawned(int bossId, int mapId, int spawnX, int spawnY, int level);

        /// <summary>Boss died — killer actor id provided.</summary>
        void OnBossKilled(int bossId, int killerActorId, int respawnMinutes);

        /// <summary>Respawn timer ticked — remaining seconds.</summary>
        void OnBossRespawnTicked(int bossId, int remainingSeconds);

        /// <summary>Boss respawned (timer hit 0).</summary>
        void OnBossRespawned(int bossId, int mapId);

        // ── Active-boss query ─────────────────────────────────────────────
        /// <summary>GetActiveBosses snapshot — count of bosses that are alive now.</summary>
        void OnActiveBossesQueried(int aliveCount, DateTime now);

        // ── UI / SFX / Persistence ────────────────────────────────────────
        /// <summary>Show boss nameplate / world bar.</summary>
        void ShowBossUI(int bossId, string nameVi, int mapId, int hpPercent);

        /// <summary>Log a boss event (kill, respawn, query) for the GM / log file.</summary>
        void LogBossEvent(string eventType, int bossId, string detailVi);

        /// <summary>Play a boss-related SFX: "spawn" / "kill" / "respawn" / "near".</summary>
        void PlayBossSFX(string action, int bossId);

        /// <summary>Save / load boss state to local cache (last death time, etc.).</summary>
        void SaveBossState(int bossId, DateTime killedAtUtc, int respawnSec);
    }
}
