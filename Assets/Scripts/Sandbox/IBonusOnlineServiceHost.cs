// -----------------------------------------------------------------------------
// VLTK Mobile — Bonus Online Service Host Interface (Unity → sandbox)
// ST-10.15 Thưởng Online. PC source: settings/bonus_onlinetime/bonus_online.txt.
// Unity runtime dispatches registry load / bonus query / claim events to a
// host impl that owns UI (bonus panel, online timer), persistence.
// Vietnamese: "Thưởng Online", "Phần Thưởng Đăng Nhập", "Tích Lũy Phút".
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host boundary cho <see cref="BonusOnlineService"/>. Decouples sandbox
    /// logic (registry parse, bonus lookup, claim) from Unity-side UI (bonus
    /// panel, online timer, claim toast), persistence, and timer tick.
    ///
    /// All methods are best-effort callbacks. Implementations must tolerate
    /// null/invalid args — sandbox never throws.
    /// </summary>
    public interface IBonusOnlineServiceHost
    {
        // ── Registry lifecycle ─────────────────────────────────────────────
        /// <summary>Bonus catalog loaded — count of registered bonuses.</summary>
        void OnBonusRegistryAttached(int bonusCount);

        // ── Query dispatch ────────────────────────────────────────────────
        /// <summary>GetBonus resolved by id — null if not found.</summary>
        void OnBonusResolved(int bonusId, int requiredMinutes, int rewardId, int rewardCount, int vipRequired);

        /// <summary>GetBonusForMinutes — count of bonuses for the given minute count.</summary>
        void OnBonusForMinutesQueried(int minutes, int resultCount);

        /// <summary>GetBonusByVip — count of bonuses for the given VIP level.</summary>
        void OnBonusByVipQueried(int vipLevel, int resultCount);

        /// <summary>GetAll snapshot — count of all bonuses.</summary>
        void OnAllBonusQueried(int resultCount);

        // ── Claim / eligibility ───────────────────────────────────────────
        /// <summary>CanClaim evaluated — true if player can claim.</summary>
        void OnCanClaimEvaluated(int bonusId, bool canClaim, int currentMinutes, int vipLevel);

        /// <summary>ClaimBonus dispatched — success or warn-already-claimed.</summary>
        void OnBonusClaimDispatched(int bonusId, bool success, string detailVi);

        /// <summary>Online timer tick (every minute).</summary>
        void OnOnlineTick(int currentMinutes, int vipLevel);

        // ── UI / SFX / Persistence ────────────────────────────────────────
        /// <summary>Show bonus panel / claim toast.</summary>
        void ShowBonusUI(int bonusId, int requiredMinutes, int rewardId);

        /// <summary>Log a bonus event (load, query, claim) for the GM / log file.</summary>
        void LogBonusEvent(string eventType, int bonusId, string detailVi);

        /// <summary>Play a bonus-related SFX: "load" / "tick" / "claim" / "miss".</summary>
        void PlayBonusSFX(string action, int bonusId);

        /// <summary>Save claimed bonuses / online minutes to local cache.</summary>
        void SaveBonusState(int bonusId, int currentMinutes, int vipLevel);
    }
}
