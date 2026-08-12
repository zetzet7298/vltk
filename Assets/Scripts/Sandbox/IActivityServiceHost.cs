// -----------------------------------------------------------------------------
// VLTK Mobile — Activity Service Host Interface (Unity → sandbox)
// PC source: settings/activitysys/activity.txt (21 entries).
// Hệ thống hoạt động runtime. Unity runtime dispatches load / query /
// start-activity events to a host implementation that owns UI (activity
// panel, countdown, banner), persistence, and Vietnamese localization.
// Vietnamese: "Hoạt Động", "Hằng Ngày", "Hằng Tuần", "Hằng Tháng", "Đang Mở".
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host boundary cho <see cref="ActivityService"/>. Decouples sandbox logic
    /// (registry parse, query by id / type / hour, start activity) from
    /// Unity-side UI (activity panel, banner, countdown timer).
    ///
    /// All methods are best-effort callbacks. Implementations must tolerate
    /// null/invalid args — sandbox never throws.
    /// </summary>
    public interface IActivityServiceHost
    {
        // ── Registry lifecycle ─────────────────────────────────────────────
        /// <summary>Activity catalog loaded — count of registered activities.</summary>
        void OnActivityRegistryAttached(int activityCount);

        // ── Query dispatch ────────────────────────────────────────────────
        /// <summary>GetActivity resolved by id — null if not found.</summary>
        void OnActivityResolved(int activityId, string nameRaw, int type, int openHour, int closeHour);

        /// <summary>GetByType — count of activities for the given type (daily/weekly/monthly).</summary>
        void OnActivitiesByTypeQueried(int type, int resultCount, string typeNameVi);

        /// <summary>GetActiveAtHour — count of activities active at the given hour.</summary>
        void OnActivitiesAtHourQueried(int hour, int resultCount);

        /// <summary>GetAllActivities — total count of activities in registry.</summary>
        void OnAllActivitiesQueried(int resultCount);

        // ── Start activity dispatch ───────────────────────────────────────
        /// <summary>StartActivity called — success or warn-not-found.</summary>
        void OnActivityStartDispatched(int activityId, bool success, string detailVi);

        // ── UI / SFX / Persistence ────────────────────────────────────────
        /// <summary>Show activity panel / banner.</summary>
        void ShowActivityUI(int activityId, string nameRaw, int type);

        /// <summary>Log an activity event (load, query, start) for the GM / log file.</summary>
        void LogActivityEvent(string eventType, int activityId, string detailVi);

        /// <summary>Play an activity-related SFX: "load" / "start" / "close" / "tick".</summary>
        void PlayActivitySFX(string action, int activityId);

        /// <summary>Save activity progress / state to local cache.</summary>
        void SaveActivityState(int activityId, int type, int currentHour);
    }
}
