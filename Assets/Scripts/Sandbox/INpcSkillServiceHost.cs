// -----------------------------------------------------------------------------
// VLTK Mobile — NPC Skill Service Host Interface (Unity → sandbox)
// PC source: settings/npcskills.txt — Kỹ Năng Quái / Boss Skill (43).
// Unity runtime dispatches registry load / skill query / cast-plan build events
// to a host implementation that owns UI (skill icon, tooltip), AI (use skill),
// and persistence (boss skill log).
// Vietnamese: "Kỹ Năng Quái", "Boss Skill", "AI Dùng Skill".
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host boundary cho <see cref="NpcSkillService"/>. Decouples sandbox logic
    /// (registry parse, skill lookup, cast plan build) from Unity-side visuals
    /// (skill icon, tooltip), AI (skill usage), and persistence.
    ///
    /// All methods are best-effort callbacks. Implementations must tolerate
    /// null/invalid args — sandbox never throws.
    /// </summary>
    public interface INpcSkillServiceHost
    {
        // ── Registry lifecycle ─────────────────────────────────────────────
        /// <summary>Skill catalog loaded — count of registered skills.</summary>
        void OnNpcSkillRegistryAttached(int skillCount);

        // ── Query dispatch ────────────────────────────────────────────────
        /// <summary>GetNpcSkill resolved by id — null if not found.</summary>
        void OnNpcSkillResolved(int skillId, string nameRaw, int skillStyle, int attackRadius);

        /// <summary>GetByNpcTemplate — count of skills for the given template id.</summary>
        void OnNpcTemplateSkillsQueried(int templateId, int resultCount);

        // ── Cast plan ──────────────────────────────────────────────────────
        /// <summary>BuildCastPlan dispatched — canCast flag + guard reason if any.</summary>
        void OnCastPlanBuilt(int skillId, bool canCast, bool missingScriptGuard, string guardReasonVi);

        /// <summary>Cast-plan built for a missing skill (no registry entry).</summary>
        void OnCastPlanMissingSkill(int skillId, string reasonVi);

        // ── AI dispatch (called by NPC AI code) ────────────────────────────
        /// <summary>NPC decided to cast a skill.</summary>
        void OnNpcCastSkill(int skillId, int casterTemplateId, int targetTemplateId);

        /// <summary>NPC skill cast completed (success or interrupted).</summary>
        void OnNpcCastCompleted(int skillId, int casterTemplateId, bool success);

        // ── UI / SFX / Persistence ────────────────────────────────────────
        /// <summary>Show NPC skill tooltip / cast bar.</summary>
        void ShowNpcSkillUI(int skillId, string nameRaw, int skillStyle);

        /// <summary>Log an NPC skill event (load, query, cast) for the GM / log file.</summary>
        void LogNpcSkillEvent(string eventType, int skillId, string detailVi);

        /// <summary>Play an NPC skill-related SFX: "load" / "cast" / "complete" / "interrupt".</summary>
        void PlayNpcSkillSFX(string action, int skillId);

        /// <summary>Save NPC skill cooldown / state to local cache.</summary>
        void SaveNpcSkillState(int skillId, int casterTemplateId, int cooldownTicks);
    }
}
