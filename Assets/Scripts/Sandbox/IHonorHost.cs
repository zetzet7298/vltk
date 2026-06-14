// -----------------------------------------------------------------------------
// VLTK Mobile — IHonorHost: giao diện host cho HonorService.
// Cho phép runtime dispatch các side-effect khi vinh danh đạt được
// (UI hào quang, danh hiệu thưởng, broadcast, log).
// PC source: settings/honor.txt (vinh danh) + lua honor_event.
// PC surfaces: AddTitle, AddSkillBuff, Msg2Player, broadcast, SetAura.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho HonorService. Implement bởi UI/Title/Aura/Chat.
    /// </summary>
    public interface IHonorHost
    {
        /// <summary>Thưởng danh hiệu cho player khi đạt vinh danh (PC AddTitle).</summary>
        void GrantTitle(int playerId, int honorId, int titleId);

        /// <summary>Kích hoạt hào quang / buff visual cho player (PC SetAura / AddSkillBuff).</summary>
        void ActivateAura(int playerId, int honorId, int auraSkillId);

        /// <summary>Hiển thị thông báo vinh danh trên UI minimap/notification.</summary>
        void ShowHonorNotice(int playerId, int honorId, string honorName);

        /// <summary>Thông báo broadcast khi player đạt vinh danh cao (PC broadcast).</summary>
        void OnHonorAchieved(int playerId, int honorId, string honorName, int points);

        /// <summary>Phát SFX/hiệu ứng khi đạt vinh danh (PC PlayHonorSFX).</summary>
        void PlayHonorSFX(int playerId, int honorId);

        /// <summary>Log thông báo vinh danh lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogHonorEvent(int playerId, int honorId, string message);

        /// <summary>Lưu vinh danh vào DB player (PC SaveHonor).</summary>
        void SaveHonorProgress(int playerId, int honorId, int points, bool achieved);
    }
}
