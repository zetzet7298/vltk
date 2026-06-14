// -----------------------------------------------------------------------------
// VLTK Mobile — IBuffStateHost: giao diện host cho BuffStateService.
// Cho phép runtime dispatch các side-effect khi buff/debuff được áp dụng hoặc
// hết hạn (UI effect icon, haptics, log, SFX, particle).
// PC source: KNpc::AddState / m_StateSpecial, lua state_notify.
// PC surfaces: SetStateEffect, PlayStateSFX, Msg2Player state log.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho BuffStateService. Implement bởi UI/Haptics/Audio.
    /// </summary>
    public interface IBuffStateHost
    {
        /// <summary>Hiển thị icon hiệu ứng trên thanh trạng thái nhân vật (PC SetStateEffect).</summary>
        void ShowStateEffect(int actorId, int skillId, int level, float durationRemaining, bool isHaptic);

        /// <summary>Ẩn icon hiệu ứng khi buff hết hạn (PC ClearStateEffect).</summary>
        void HideStateEffect(int actorId, int skillId);

        /// <summary>Phát âm thanh khi áp dụng buff (PC PlayStateSFX).</summary>
        void PlayStateSFX(int actorId, int skillId, bool isHaptic);

        /// <summary>Rung thiết bị (mobile haptics) khi nhận buff quan trọng (PC Handheld.Vibrate).</summary>
        void TriggerHapticFeedback(int actorId, int skillId);

        /// <summary>Log thông báo buff lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogStateNotice(int actorId, int skillId, int level, bool added);
    }
}
