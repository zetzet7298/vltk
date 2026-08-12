// -----------------------------------------------------------------------------
// VLTK Mobile — Battle Script Service Host Interface (Unity → sandbox)
// PC source: settings/battlescripts.txt — Kịch Bản Chiến Đấu (Tống Kim,
// Công Thành Chiến, Võ Lâm Liên Đấu, Phong Hỏa Liên Thành, Bách Bảo Lâu).
// Unity runtime dispatches load / query events to a host implementation that
// owns UI, save/load, log, and trigger logic.
// Vietnamese: "Kịch Bản", "Chiến Đấu", "Bắt Đầu", "Kết Thúc", "Giết Boss".
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host boundary cho <see cref="BattleScriptService"/>. Decouples sandbox
    /// logic (registry parse, query by id / map / trigger type) from Unity-side
    /// UI (script list panel, trigger toast), persistence (script progress),
    /// and trigger evaluation (Tống Kim join, boss kill, player death).
    ///
    /// All methods are best-effort callbacks. Implementations must tolerate
    /// null/invalid args (script not found, etc.) — sandbox never throws.
    /// </summary>
    public interface IBattleScriptServiceHost
    {
        // ── Registry lifecycle ─────────────────────────────────────────────
        /// <summary>Script catalog loaded — count of registered scripts.</summary>
        void OnScriptRegistryAttached(int scriptCount);

        /// <summary>Script entry resolved by id — null if not found.</summary>
        void OnScriptResolved(int scriptId, string scriptName, int mapId, int triggerType);

        // ── Query dispatch ────────────────────────────────────────────────
        /// <summary>GetScriptsForMap — count of scripts for a given mapId.</summary>
        void OnScriptsForMapQueried(int mapId, int resultCount);

        /// <summary>GetScriptsByTrigger — count of scripts for a given triggerType.</summary>
        void OnScriptsByTriggerQueried(int triggerType, int resultCount);

        // ── Trigger dispatch (called by gameplay code) ─────────────────────
        /// <summary>Trigger "start" fired for a script (0 = start).</summary>
        void OnScriptStartTriggered(int scriptId, int mapId, int npcId);

        /// <summary>Trigger "end" fired for a script (1 = end).</summary>
        void OnScriptEndTriggered(int scriptId, int mapId, int rewardId, int rewardCount, int scoreReward);

        /// <summary>Trigger "kill boss" fired for a script (2 = kill_boss).</summary>
        void OnScriptKillBossTriggered(int scriptId, int mapId, int npcId);

        /// <summary>Trigger "death" fired for a script (3 = death).</summary>
        void OnScriptDeathTriggered(int scriptId, int mapId, int npcId);

        // ── UI / SFX / Persistence ────────────────────────────────────────
        /// <summary>Show script list panel / trigger UI.</summary>
        void ShowScriptUI(int scriptId, string scriptName, int triggerType);

        /// <summary>Log a script event (load, query, trigger) for the GM / log file.</summary>
        void LogScriptEvent(string eventType, int scriptId, string detailVi);

        /// <summary>Play a script-related SFX: "load" / "start" / "end" / "kill" / "death".</summary>
        void PlayScriptSFX(string action, int scriptId);

        /// <summary>Save script state / progress to local cache.</summary>
        void SaveScriptState(int scriptId, int progressPercent, int mapId);
    }
}
