// -----------------------------------------------------------------------------
// VLTK Mobile — IPlayerLevelHost: giao diện host cho PlayerLevelService.
// Cho phép runtime dispatch các side-effect khi lên cấp / cộng EXP
// (UI thông báo, âm thanh, thưởng kỹ năng, cập nhật bảng thông tin nhân vật).
// PC source: KNpc.cpp::LevelUp, NotifyPlayerLevelUp, lua player/levelup_notify.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho PlayerLevelService. Implement bởi UI/Audio/Notify.
    /// </summary>
    public interface IPlayerLevelHost
    {
        /// <summary>Thông báo EXP thay đổi (current, requiredForNext).</summary>
        void OnExpChanged(long currentExp, long requiredExp);

        /// <summary>Thông báo lên cấp (oldLevel, newLevel, potentialGranted, skillGranted).</summary>
        void OnLevelUp(int oldLevel, int newLevel, int potentialGranted, int skillGranted);

        /// <summary>Phát âm thanh khi lên cấp (PC SFX level_up). Trả về false nếu host không có audio.</summary>
        bool TryPlayLevelUpSfx();

        /// <summary>Log thông báo lên cấp lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogLevelUpNotice(int oldLevel, int newLevel);

        /// <summary>Cấp thưởng vật phẩm khi lên cấp (PC item reward table cho level milestone).</summary>
        void GrantLevelUpReward(int oldLevel, int newLevel);
    }
}
