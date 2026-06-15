// -----------------------------------------------------------------------------
// VLTK Mobile — Chat Service Host Interface (Unity → sandbox)
// PC source: chat UI from Ui3 INI files, uiconfig.ini SetChannelTextColor.
// Unity runtime dispatches channel change / message-post / history query events
// to a host implementation that owns UI rendering, audio, persistence.
// Vietnamese: "Kênh Thế Giới", "Bang Hội", "Mật", "Hệ Thống".
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host boundary cho <see cref="ChatService"/>. Decouples sandbox logic
    /// (channel switch, message history, system/combat logs) from Unity-side
    /// UI (chat panel, tabs, input field) and persistence (chat log file).
    ///
    /// All methods are best-effort callbacks. Implementations must tolerate
    /// null/empty args — sandbox never throws.
    /// </summary>
    public interface IChatServiceHost
    {
        // ── Channel lifecycle ──────────────────────────────────────────────
        /// <summary>Active channel changed by SetChannel().</summary>
        void OnChannelChanged(int channelId, string channelNameVi);

        // ── Message dispatch ───────────────────────────────────────────────
        /// <summary>Player message posted (SendPlayerMessage).</summary>
        void OnPlayerMessageSent(int channelId, string senderName, string textVi);

        /// <summary>System message posted (PostSystemMessage).</summary>
        void OnSystemMessagePosted(string textVi);

        /// <summary>Combat log message posted (PostCombatLog).</summary>
        void OnCombatLogPosted(string textVi);

        /// <summary>Empty / whitespace message rejected by SendPlayerMessage.</summary>
        void OnEmptyMessageRejected(int channelId, string senderName);

        // ── History query ──────────────────────────────────────────────────
        /// <summary>GetFilteredMessages snapshot — count of messages for the active channel.</summary>
        void OnFilteredMessagesQueried(int resultCount, int activeChannelId, int maxCount);

        // ── UI / SFX / Persistence ─────────────────────────────────────────
        /// <summary>Show chat panel / refresh messages list.</summary>
        void ShowChatUI(int channelId);

        /// <summary>Log a chat event (send, system, combat) for the GM / log file.</summary>
        void LogChatEvent(string eventType, int channelId, string detailVi);

        /// <summary>Play a chat-related SFX: "send" / "receive" / "system" / "combat".</summary>
        void PlayChatSFX(string action, int channelId);

        /// <summary>Save chat log to local cache (last 200 messages, etc.).</summary>
        void SaveChatLog(int channelId, string textVi, long timestampUnix);
    }
}
