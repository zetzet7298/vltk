// -----------------------------------------------------------------------------
// VLTK Mobile — Achievement Service Host Interface (Unity → sandbox)
// PC source: settings/achievement/achievement.txt — Thành Tựu (250+).
// Unity runtime dispatches registry load / achievement query / progress /
// completion events to a host implementation that owns UI (achievement panel,
// toast), persistence, and reward grant.
// Vietnamese: "Thành Tựu", "Hoàn Thành", "Phần Thưởng", "Tiến Độ".
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host boundary cho <see cref="AchievementService"/>. Decouples sandbox
    /// logic (registry parse, lookup, progress, completion) from Unity-side
    /// UI (achievement panel, toast), persistence, and reward grant.
    ///
    /// All methods are best-effort callbacks. Implementations must tolerate
    /// null/invalid args — sandbox never throws.
    /// </summary>
    public interface IAchievementServiceHost
    {
        // ── Registry lifecycle ─────────────────────────────────────────────
        /// <summary>Achievement catalog loaded — count of registered achievements.</summary>
        void OnAchievementRegistryAttached(int achievementCount);

        // ── Query dispatch ────────────────────────────────────────────────
        /// <summary>GetAchievement resolved by id — null if not found.</summary>
        void OnAchievementResolved(int achievementId, int category, string nameRaw);

        /// <summary>GetByCategory — count of achievements in the given category.</summary>
        void OnAchievementsByCategoryQueried(int category, int resultCount, string categoryNameVi);

        // ── Progress / completion ─────────────────────────────────────────
        /// <summary>CanEarn evaluated — true if player can earn the achievement.</summary>
        void OnCanEarnEvaluated(int achievementId, bool canEarn, int playerLevel, long progress);

        /// <summary>TryComplete dispatched — success or fail.</summary>
        void OnTryCompleteDispatched(int achievementId, bool success, long progress);

        /// <summary>GetProgressPercent queried — % of progress toward completion.</summary>
        void OnProgressQueried(int achievementId, float percent, long progress);

        // ── UI / SFX / Persistence ────────────────────────────────────────
        /// <summary>Show achievement panel / unlock toast.</summary>
        void ShowAchievementUI(int achievementId, string nameRaw, int category);

        /// <summary>Log an achievement event (load, query, progress, complete) for the GM / log file.</summary>
        void LogAchievementEvent(string eventType, int achievementId, string detailVi);

        /// <summary>Play an achievement-related SFX: "load" / "unlock" / "complete" / "progress".</summary>
        void PlayAchievementSFX(string action, int achievementId);

        /// <summary>Save achievement progress to local cache / PlayerPrefs.</summary>
        void SaveAchievementState(int achievementId, long progress, int category);
    }
}
