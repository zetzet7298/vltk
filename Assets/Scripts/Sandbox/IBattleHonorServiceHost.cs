// -----------------------------------------------------------------------------
// VLTK Mobile — Battle Honor Service Host Interface (Unity → sandbox)
// PC source: battlehonor.txt — Vinh Danh Chiến Trường (battlefield honor).
// Unity runtime dispatches registry load / honor query / score-match events
// to a host implementation that owns UI (honor list, badge, title), persistence.
// Vietnamese: "Vinh Danh Chiến Trường", "Điểm", "Danh Hiệu Thưởng".
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host boundary cho <see cref="BattleHonorService"/>. Decouples sandbox
    /// logic (registry parse, score-based honor lookup) from Unity-side UI
    /// (honor panel, badge icon, title display), persistence, and notification.
    ///
    /// All methods are best-effort callbacks. Implementations must tolerate
    /// null/invalid args — sandbox never throws.
    /// </summary>
    public interface IBattleHonorServiceHost
    {
        // ── Registry lifecycle ─────────────────────────────────────────────
        /// <summary>Honor catalog loaded — count of registered honors.</summary>
        void OnBattleHonorRegistryAttached(int honorCount);

        /// <summary>Empty or null registry detected — empty-state warning.</summary>
        void OnBattleHonorRegistryEmpty();

        // ── Query dispatch ────────────────────────────────────────────────
        /// <summary>GetHonor resolved by id — null if not found.</summary>
        void OnHonorResolved(int honorId, int battleType, string nameVi, int requiredScore, string bonusTitle);

        /// <summary>GetByBattleType — count of honors for the given battle type.</summary>
        void OnHonorsByBattleTypeQueried(int battleType, int resultCount);

        /// <summary>GetHonorForScore — best honor matched for score.</summary>
        void OnHonorForScoreQueried(int battleType, int score, int matchedHonorId, int matchedScore, bool found);

        // ── Score evaluation (called by gameplay code) ────────────────────
        /// <summary>Player earned honor — score threshold reached.</summary>
        void OnHonorEarned(int honorId, int battleType, int finalScore, string bonusTitle);

        // ── UI / SFX / Persistence ────────────────────────────────────────
        /// <summary>Show honor panel / badge / title notification.</summary>
        void ShowHonorUI(int honorId, string nameVi, int requiredScore, string bonusTitle);

        /// <summary>Log a battle-honor event (load, query, earn) for the GM / log file.</summary>
        void LogBattleHonorEvent(string eventType, int honorId, string detailVi);

        /// <summary>Play a battle-honor-related SFX: "load" / "earn" / "title" / "badge".</summary>
        void PlayBattleHonorSFX(string action, int honorId);

        /// <summary>Save earned honors / score to local cache / PlayerPrefs.</summary>
        void SaveBattleHonorState(int honorId, int battleType, int currentScore);
    }
}
