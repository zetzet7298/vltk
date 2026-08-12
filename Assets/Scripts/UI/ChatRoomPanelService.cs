// -----------------------------------------------------------------------------
// VLTK Mobile — PC ChatRoom Panel Service (BtnChatRoom / Player_ChatRoom)
// PC source: 7e20a7ac.ini / c9c8a750.ini [Channels], CH_* sections.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct PcChatChannelRow
    {
        public readonly int index;
        public readonly string pcName;
        public readonly string labelVi;
        public readonly int sendIntervalMs;
        public readonly int sendMsgNum;

        public PcChatChannelRow(int index, string pcName, string labelVi, int sendIntervalMs, int sendMsgNum)
        {
            this.index = index;
            this.pcName = pcName ?? string.Empty;
            this.labelVi = labelVi ?? string.Empty;
            this.sendIntervalMs = sendIntervalMs;
            this.sendMsgNum = sendMsgNum;
        }
    }

    public sealed class ChatRoomPanelSnapshot
    {
        public string defaultChannel;
        public string defaultSendNameVi;
        public IReadOnlyList<PcChatChannelRow> channels;
        public IReadOnlyList<string> historyRows;
    }

    public static class ChatRoomPanelService
    {
        public const string DefaultChannel = "CH_SYSTEM";
        public const string DefaultChannelSendNameVi = "Nhắc nhở";

        public static readonly IReadOnlyList<PcChatChannelRow> PcChannels = new List<PcChatChannelRow>
        {
            new PcChatChannelRow(0, "CH_NEARBY", "Gần / người chơi phụ cận", 2000, 2),
            new PcChatChannelRow(1, "CH_TEAM", "Đội", 800, 2),
            new PcChatChannelRow(2, "CH_WORLD", "Thế giới", 60000, 2),
            new PcChatChannelRow(3, "CH_FACTION", "Môn phái", 10000, 2),
            new PcChatChannelRow(4, "CH_SYSTEM", "Hệ thống / GM / Nhắc nhở", 15000, 0),
            new PcChatChannelRow(5, "CH_CITY", "Thành thị", 20000, 2),
            new PcChatChannelRow(6, "CH_TONG", "Bang hội", 10000, 2),
            new PcChatChannelRow(7, "CH_TONGUNION", "Liên minh bang hội", 10000, 2),
            new PcChatChannelRow(8, "CH_CHATROOM", "Phòng chat", 2000, 2),
            new PcChatChannelRow(9, "CH_ATTACK", "Tấn công", 1000, 2),
            new PcChatChannelRow(10, "CH_DEFEND", "Phòng thủ", 1000, 2),
            new PcChatChannelRow(11, "CH_JABBER", "Rảnh rỗi", 2000, 2),
            new PcChatChannelRow(12, "CH_SONG", "Tống", 2000, 2),
            new PcChatChannelRow(13, "CH_JIN", "Kim", 2000, 2),
            new PcChatChannelRow(14, "CH_CUSTOM", "Tự định nghĩa", 2000, 2),
        };

        public static ChatRoomPanelSnapshot BuildSnapshot(ChatService chat, int historyLimit)
        {
            var rows = new List<string>();
            if (chat != null)
            {
                var messages = chat.GetFilteredMessages(historyLimit);
                if (messages.Count == 0)
                    rows.Add("Chưa có tin nhắn.");
                foreach (var msg in messages)
                    rows.Add($"[{ChatService.ChannelNameVi(msg.channel)}] {msg.senderName}: {msg.text}");
            }
            else
            {
                rows.Add("Chat runtime chưa sẵn sàng.");
            }

            return new ChatRoomPanelSnapshot
            {
                defaultChannel = DefaultChannel,
                defaultSendNameVi = DefaultChannelSendNameVi,
                channels = PcChannels,
                historyRows = rows,
            };
        }
    }
}
