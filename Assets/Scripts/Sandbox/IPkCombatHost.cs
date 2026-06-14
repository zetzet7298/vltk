// -----------------------------------------------------------------------------
// VLTK Mobile — IPkCombatHost: giao diện host cho PkCombatService.
// Cho phép runtime dispatch các side-effect khi chuyển chế độ PK, đánh nhau,
// tăng/giảm sát khí (UI PK mode, red name, log, save).
// PC source: KNpc::IsEnemy, PK mode, RedName/Karma system.
// PC surfaces: UpdatePKModeUI, Msg2Player, SetPlayerNameColor, SaveKarma.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho PkCombatService. Implement bởi UI/Chat/DB.
    /// </summary>
    public interface IPkCombatHost
    {
        /// <summary>Chuyển chế độ PK (PC UpdatePKModeUI).</summary>
        void OnPkModeChanged(PkMode oldMode, PkMode newMode);

        /// <summary>Quyết định tấn công (PC Msg2Player + SetNameColor).</summary>
        void OnAttackResolved(int attackerId, int targetId, bool canAttack, string reasonVi, PkPenaltyType penalty, int karmaChange);

        /// <summary>Sát khí thay đổi (PC UpdateKarmaUI).</summary>
        void OnKarmaChanged(int newKarma, int delta, bool isRedName);

        /// <summary>Player trở nên đỏ tên (PC SetPlayerNameColor(red)).</summary>
        void OnBecameRedName(int actorId, int karma);

        /// <summary>Player hết đỏ tên (PC SetPlayerNameColor(normal)).</summary>
        void OnClearedRedName(int actorId);

        /// <summary>Log thông báo PK lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogPkEvent(int actorId, string message);

        /// <summary>Phát SFX khi đánh PK (PC PlayPKSFX).</summary>
        void PlayPkSFX(int attackerId, int targetId, string combatType);

        /// <summary>Lưu PK state vào DB (PC SaveKarma).</summary>
        void SaveKarma(int actorId, int karma, PkMode mode);
    }
}
