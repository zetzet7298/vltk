// -----------------------------------------------------------------------------
// VLTK Mobile — IFriendHost: giao diện host cho FriendService.
// Cho phép runtime dispatch các side-effect khi player thêm/xóa bạn, gửi
// tin nhắn, thay đổi thân mật, cập nhật online status (UI friends, chat, SFX).
// PC source: Friend list, mail, intimacy system + lua friend_event.
// PC surfaces: UpdateFriendUI, SendChatMessage, PlayFriendSFX, Msg2Player.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho FriendService. Implement bởi UI/Chat/Audio.
    /// </summary>
    public interface IFriendHost
    {
        /// <summary>Thêm bạn mới (PC UpdateFriendUI + AddFriendNotify).</summary>
        void OnFriendAdded(int playerId, int friendId, int newFriendRecordId, string friendName);

        /// <summary>Xóa bạn (PC UpdateFriendUI + RemoveFriendNotify).</summary>
        void OnFriendRemoved(int playerId, int friendId, int friendRecordId);

        /// <summary>Thân mật thay đổi (PC UpdateIntimacyUI).</summary>
        void OnIntimacyChanged(int playerId, int friendId, int newIntimacy, int delta);

        /// <summary>Friend đăng nhập/đăng xuất (PC UpdateOnlineStatus).</summary>
        void OnFriendOnlineStatusChanged(int playerId, int friendId, bool isOnline, long lastLoginSec);

        /// <summary>Gửi tin nhắn cho bạn (PC SendChatMessage + UpdateMailUI).</summary>
        void OnMessageSent(int fromPlayerId, int toPlayerId, string message);

        /// <summary>Phát SFX khi thao tác bạn bè (PC PlayFriendSFX).</summary>
        void PlayFriendSFX(int playerId, string action);

        /// <summary>Log thông báo bạn bè lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogFriendEvent(int playerId, string message);

        /// <summary>Lưu danh sách bạn bè vào DB (PC SaveFriendList).</summary>
        void SaveFriendList(int playerId, int count);
    }
}
