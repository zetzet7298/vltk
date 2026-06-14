// -----------------------------------------------------------------------------
// VLTK Mobile — IPlayerMountHost: giao diện host cho PlayerMountService.
// Cho phép runtime dispatch các side-effect khi cưỡi / xuống ngựa
// (refresh SPR, animation, SFX, log, broadcast, save).
// PC source: NpcS.txt HorseType, npcres/horse SPR set, 男主角骑马关联表.txt.
// PC surfaces: SetNpcRes, PlayMountSFX, SaveMountState, Msg2Player.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho PlayerMountService. Implement bởi Visual/Audio/Chat.
    /// </summary>
    public interface IPlayerMountHost
    {
        /// <summary>Refresh SPR visual khi mount thay đổi (PC SetNpcRes / ChangeBody).</summary>
        void RefreshMountVisual(int horseType, MountState newState, float speedMultiplier);

        /// <summary>Phát âm thanh khi mount / dismount (PC PlayMountSFX).</summary>
        void PlayMountSFX(int horseType, bool isMounting);

        /// <summary>Thông báo khi player bắt đầu cưỡi ngựa (PC broadcast / Msg2Player).</summary>
        void OnMountStarted(int horseType, float transitionTime);

        /// <summary>Thông báo khi mount hoàn tất (sau transition) (PC MountComplete).</summary>
        void OnMountCompleted(int horseType, float speedMultiplier);

        /// <summary>Thông báo khi player bắt đầu xuống ngựa (PC DismountStarted).</summary>
        void OnDismountStarted(int horseType, float transitionTime);

        /// <summary>Thông báo khi dismount hoàn tất (sau transition).</summary>
        void OnDismountCompleted();

        /// <summary>Log thông báo mount lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogMountEvent(int horseType, string message);

        /// <summary>Lưu mount state vào DB player (PC SaveMountState).</summary>
        void SaveMountState(int horseType, MountState state, bool isMounted);
    }
}
