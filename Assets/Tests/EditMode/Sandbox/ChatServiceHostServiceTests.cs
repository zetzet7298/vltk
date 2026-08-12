// -----------------------------------------------------------------------------
// VLTK Mobile — ChatService host dispatch tests
// PC source: chat UI from Ui3 INI files, uiconfig.ini SetChannelTextColor.
// Verifies IChatServiceHost receives expected events for channel switch,
// player/system/combat message dispatch, history query, empty-reject.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class ChatServiceHostServiceTests
    {
        private sealed class FakeHost : IChatServiceHost
        {
            public int ChannelChangedCalls;
            public int LastChannelId;
            public string LastChannelNameVi;

            public int PlayerMessageSentCalls;
            public int LastPlayerChannelId;
            public string LastPlayerSenderName;
            public string LastPlayerTextVi;

            public int SystemPostedCalls;
            public string LastSystemTextVi;

            public int CombatPostedCalls;
            public string LastCombatTextVi;

            public int EmptyRejectedCalls;
            public int LastEmptyChannelId;
            public string LastEmptySenderName;

            public int FilteredQueriedCalls;
            public int LastFilteredCount;
            public int LastFilteredChannelId;
            public int LastFilteredMaxCount;

            public int UIShowCalls;
            public int LastUIShowChannelId;

            public int LogCalls;
            public int LastLogChannelId;
            public string LastLogEventType;
            public string LastLogDetail;

            public int SFXCalls;
            public int LastSFXChannelId;
            public string LastSFXAction;

            public int SaveCalls;
            public int LastSaveChannelId;
            public string LastSaveTextVi;
            public long LastSaveTimestampUnix;

            public void OnChannelChanged(int channelId, string channelNameVi)
            {
                ChannelChangedCalls++;
                LastChannelId = channelId;
                LastChannelNameVi = channelNameVi;
            }
            public void OnPlayerMessageSent(int channelId, string senderName, string textVi)
            {
                PlayerMessageSentCalls++;
                LastPlayerChannelId = channelId;
                LastPlayerSenderName = senderName;
                LastPlayerTextVi = textVi;
            }
            public void OnSystemMessagePosted(string textVi)
            {
                SystemPostedCalls++;
                LastSystemTextVi = textVi;
            }
            public void OnCombatLogPosted(string textVi)
            {
                CombatPostedCalls++;
                LastCombatTextVi = textVi;
            }
            public void OnEmptyMessageRejected(int channelId, string senderName)
            {
                EmptyRejectedCalls++;
                LastEmptyChannelId = channelId;
                LastEmptySenderName = senderName;
            }
            public void OnFilteredMessagesQueried(int resultCount, int activeChannelId, int maxCount)
            {
                FilteredQueriedCalls++;
                LastFilteredCount = resultCount;
                LastFilteredChannelId = activeChannelId;
                LastFilteredMaxCount = maxCount;
            }
            public void ShowChatUI(int channelId)
            {
                UIShowCalls++;
                LastUIShowChannelId = channelId;
            }
            public void LogChatEvent(string eventType, int channelId, string detailVi)
            {
                LogCalls++;
                LastLogEventType = eventType;
                LastLogChannelId = channelId;
                LastLogDetail = detailVi;
            }
            public void PlayChatSFX(string action, int channelId)
            {
                SFXCalls++;
                LastSFXAction = action;
                LastSFXChannelId = channelId;
            }
            public void SaveChatLog(int channelId, string textVi, long timestampUnix)
            {
                SaveCalls++;
                LastSaveChannelId = channelId;
                LastSaveTextVi = textVi;
                LastSaveTimestampUnix = timestampUnix;
            }
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────
        [Test]
        public void Ctor_Default_NoHost()
        {
            var svc = new ChatService();
            Assert.AreEqual(0, svc.History.Count);
            // PC default channel is CH_SYSTEM (default send label "Nhắc nhở").
            Assert.AreEqual(ChatChannel.System, svc.ActiveChannel);
        }

        [Test]
        public void Ctor_WithHost_Accepts()
        {
            var host = new FakeHost();
            var svc = new ChatService(host);
            Assert.AreEqual(0, svc.History.Count);
        }

        [Test]
        public void AttachHost_NullSafe()
        {
            var svc = new ChatService();
            Assert.DoesNotThrow(() => svc.AttachHost(null));
        }

        // ── SendPlayerMessage dispatch ──────────────────────────────────────
        [Test]
        public void SendPlayerMessage_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new ChatService(host);
            svc.SendPlayerMessage(ChatChannel.World, "Alice", "Hello world");
            Assert.AreEqual(1, host.PlayerMessageSentCalls);
            Assert.AreEqual((int)ChatChannel.World, host.LastPlayerChannelId);
            Assert.AreEqual("Alice", host.LastPlayerSenderName);
            Assert.AreEqual("Hello world", host.LastPlayerTextVi);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual("player_send", host.LastLogEventType);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual("send", host.LastSFXAction);
            Assert.AreEqual(1, host.SaveCalls);
            Assert.AreEqual(1, svc.History.Count);
        }

        [Test]
        public void SendPlayerMessage_Empty_RejectsNoAdd()
        {
            var host = new FakeHost();
            var svc = new ChatService(host);
            svc.SendPlayerMessage(ChatChannel.World, "Alice", "   ");
            Assert.AreEqual(1, host.EmptyRejectedCalls);
            Assert.AreEqual(0, host.PlayerMessageSentCalls);
            Assert.AreEqual(0, svc.History.Count);
        }

        [Test]
        public void SendPlayerMessage_NullText_Rejects()
        {
            var host = new FakeHost();
            var svc = new ChatService(host);
            svc.SendPlayerMessage(ChatChannel.Team, "Bob", null);
            Assert.AreEqual(1, host.EmptyRejectedCalls);
            Assert.AreEqual(0, svc.History.Count);
        }

        // ── PostSystemMessage dispatch ──────────────────────────────────────
        [Test]
        public void PostSystemMessage_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new ChatService(host);
            svc.PostSystemMessage("Server sẽ bảo trì sau 10 phút");
            Assert.AreEqual(1, host.SystemPostedCalls);
            Assert.AreEqual("Server sẽ bảo trì sau 10 phút", host.LastSystemTextVi);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual("system_post", host.LastLogEventType);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual("system", host.LastSFXAction);
            Assert.AreEqual(1, host.SaveCalls);
            Assert.AreEqual(1, svc.History.Count);
        }

        // ── PostCombatLog dispatch ──────────────────────────────────────────
        [Test]
        public void PostCombatLog_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new ChatService(host);
            svc.PostCombatLog("Bạn nhận 100 damage");
            Assert.AreEqual(1, host.CombatPostedCalls);
            Assert.AreEqual("Bạn nhận 100 damage", host.LastCombatTextVi);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual("combat_log", host.LastLogEventType);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual("combat", host.LastSFXAction);
        }

        // ── SetChannel dispatch ─────────────────────────────────────────────
        [Test]
        public void SetChannel_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new ChatService(host);
            svc.SetChannel(ChatChannel.Guild);
            Assert.AreEqual(1, host.ChannelChangedCalls);
            Assert.AreEqual((int)ChatChannel.Guild, host.LastChannelId);
            Assert.AreEqual("Bang Hội", host.LastChannelNameVi);
            Assert.AreEqual(1, host.UIShowCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual("channel_changed", host.LastLogEventType);
            Assert.AreEqual(1, host.SFXCalls);
            Assert.AreEqual("channel", host.LastSFXAction);
        }

        // ── GetFilteredMessages dispatch ────────────────────────────────────
        [Test]
        public void GetFilteredMessages_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new ChatService(host);
            svc.SetChannel(ChatChannel.All);
            svc.SendPlayerMessage(ChatChannel.World, "Alice", "msg1");
            svc.PostSystemMessage("system1");
            var list = svc.GetFilteredMessages(50);
            // All=0 → returns all messages (including System)
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(1, host.FilteredQueriedCalls);
            Assert.AreEqual(2, host.LastFilteredCount);
            Assert.AreEqual((int)ChatChannel.All, host.LastFilteredChannelId);
            Assert.AreEqual(50, host.LastFilteredMaxCount);
        }

        [Test]
        public void GetFilteredMessages_SpecificChannel_Dispatches()
        {
            var host = new FakeHost();
            var svc = new ChatService(host);
            svc.SendPlayerMessage(ChatChannel.World, "Alice", "w1");
            svc.SendPlayerMessage(ChatChannel.Team, "Bob", "t1");
            svc.PostSystemMessage("sys1");
            svc.SetChannel(ChatChannel.Team);
            var list = svc.GetFilteredMessages(50);
            // Team=3 → 1 team message + 1 system message (system always shown)
            Assert.GreaterOrEqual(list.Count, 1);
            Assert.AreEqual((int)ChatChannel.Team, host.LastFilteredChannelId);
        }

        // ── ChannelNameVi static helper ─────────────────────────────────────
        [Test]
        public void ChannelNameVi_AllKnownChannels()
        {
            Assert.AreEqual("Tất Cả", ChatService.ChannelNameVi(ChatChannel.All));
            Assert.AreEqual("Thế Giới", ChatService.ChannelNameVi(ChatChannel.World));
            Assert.AreEqual("Khu Vực", ChatService.ChannelNameVi(ChatChannel.Map));
            Assert.AreEqual("Đội", ChatService.ChannelNameVi(ChatChannel.Team));
            Assert.AreEqual("Môn Phái", ChatService.ChannelNameVi(ChatChannel.Faction));
            Assert.AreEqual("Mật", ChatService.ChannelNameVi(ChatChannel.Private));
            Assert.AreEqual("Hệ Thống", ChatService.ChannelNameVi(ChatChannel.System));
            Assert.AreEqual("Phòng", ChatService.ChannelNameVi(ChatChannel.Room));
            Assert.AreEqual("Bang Hội", ChatService.ChannelNameVi(ChatChannel.Guild));
            Assert.AreEqual("Khác", ChatService.ChannelNameVi(ChatChannel.Other));
        }

        // ── No-host path is silent ─────────────────────────────────────────
        [Test]
        public void NoHost_OperationsDoNotThrow()
        {
            var svc = new ChatService();
            Assert.DoesNotThrow(() => svc.SendPlayerMessage(ChatChannel.World, "A", "B"));
            Assert.DoesNotThrow(() => svc.PostSystemMessage("X"));
            Assert.DoesNotThrow(() => svc.PostCombatLog("Y"));
            Assert.DoesNotThrow(() => svc.SetChannel(ChatChannel.Team));
            Assert.DoesNotThrow(() => svc.GetFilteredMessages(10));
        }

        // ── History trim ────────────────────────────────────────────────────
        [Test]
        public void HistoryTrimmedAtMax()
        {
            var svc = new ChatService();
            for (int i = 0; i < 250; i++)
                svc.SendPlayerMessage(ChatChannel.World, "x", "msg" + i);
            // Max history is 200 (private)
            Assert.LessOrEqual(svc.History.Count, 200);
        }
    }
}
