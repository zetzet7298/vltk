// -----------------------------------------------------------------------------
// VLTK Mobile — FriendService EditMode tests.
// Kiểm tra friend lifecycle: add/remove friend, intimacy, online status, send
// message, top friends, host dispatch chain.
// PC source: Friend list, mail, intimacy system + lua friend_event.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class FriendServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IFriendHost
        {
            public int AddCalls;
            public int RemoveCalls;
            public int IntimacyCalls;
            public int OnlineCalls;
            public int MsgCalls;
            public int SfxCalls;
            public int LogCalls;
            public int SaveCalls;
            public int LastPlayerId;
            public int LastFriendId;
            public int LastRecordId;
            public int LastIntimacy;
            public int LastDelta;
            public bool LastOnline;
            public long LastLoginSec;
            public string LastMsg;
            public int LastCount;

            public void OnFriendAdded(int playerId, int friendId, int newFriendRecordId, string friendName)
            {
                AddCalls++;
                LastPlayerId = playerId;
                LastFriendId = friendId;
                LastRecordId = newFriendRecordId;
            }
            public void OnFriendRemoved(int playerId, int friendId, int friendRecordId)
            {
                RemoveCalls++;
                LastRecordId = friendRecordId;
            }
            public void OnIntimacyChanged(int playerId, int friendId, int newIntimacy, int delta)
            {
                IntimacyCalls++;
                LastIntimacy = newIntimacy;
                LastDelta = delta;
            }
            public void OnFriendOnlineStatusChanged(int playerId, int friendId, bool isOnline, long lastLoginSec)
            {
                OnlineCalls++;
                LastOnline = isOnline;
                LastLoginSec = lastLoginSec;
            }
            public void OnMessageSent(int fromPlayerId, int toPlayerId, string message) { MsgCalls++; LastMsg = message; }
            public void PlayFriendSFX(int playerId, string action) { SfxCalls++; }
            public void LogFriendEvent(int playerId, string message) { LogCalls++; }
            public void SaveFriendList(int playerId, int count) { SaveCalls++; LastCount = count; }
        }

        // ── Ctor / Count ────────────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new FriendService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void Constructor_WithRegistry()
        {
            var reg = new PcFriendRegistry();
            var svc = new FriendService(reg);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var svc = new FriendService(null, host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new FriendService();
            svc.AttachHost(host);
            svc.AddFriend(1, 2);
            Assert.AreEqual(1, host.AddCalls);
        }

        // ── AddFriend ───────────────────────────────────────────────────────

        [Test]
        public void AddFriend_Success()
        {
            var svc = new FriendService();
            Assert.IsTrue(svc.AddFriend(1, 2));
        }

        [Test]
        public void AddFriend_InvalidIds_ReturnsFalse()
        {
            var svc = new FriendService();
            Assert.IsFalse(svc.AddFriend(0, 2));
            Assert.IsFalse(svc.AddFriend(1, 0));
            Assert.IsFalse(svc.AddFriend(-1, 2));
        }

        [Test]
        public void AddFriend_Self_ReturnsFalse()
        {
            var svc = new FriendService();
            Assert.IsFalse(svc.AddFriend(1, 1));
        }

        [Test]
        public void AddFriend_Duplicate_ReturnsFalse()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            Assert.IsFalse(svc.AddFriend(1, 2));
        }

        [Test]
        public void AddFriend_AtMax_ReturnsFalse()
        {
            var svc = new FriendService();
            for (int i = 1; i <= FriendService.MaxFriends; i++)
                svc.AddFriend(1, 1000 + i);
            Assert.IsFalse(svc.AddFriend(1, 9999));
        }

        [Test]
        public void AddFriend_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new FriendService(null, host);
            svc.AddFriend(1, 2);
            Assert.AreEqual(1, host.AddCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SaveCalls);
            Assert.AreEqual(1, host.LastCount);
        }

        [Test]
        public void AddFriend_FiresOnFriendAddedEvent()
        {
            var svc = new FriendService();
            int fired = 0;
            svc.OnFriendAdded += (pl, fr) => fired++;
            svc.AddFriend(1, 2);
            Assert.AreEqual(1, fired);
        }

        // ── RemoveFriend ────────────────────────────────────────────────────

        [Test]
        public void RemoveFriend_Exists()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            Assert.IsTrue(svc.RemoveFriend(1, 2));
        }

        [Test]
        public void RemoveFriend_NotExists_ReturnsFalse()
        {
            var svc = new FriendService();
            Assert.IsFalse(svc.RemoveFriend(1, 2));
        }

        [Test]
        public void RemoveFriend_NoPlayerList_ReturnsFalse()
        {
            var svc = new FriendService();
            Assert.IsFalse(svc.RemoveFriend(99, 1));
        }

        [Test]
        public void RemoveFriend_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new FriendService(null, host);
            svc.AddFriend(1, 2);
            svc.RemoveFriend(1, 2);
            Assert.AreEqual(1, host.RemoveCalls);
        }

        [Test]
        public void RemoveFriend_FiresOnFriendRemovedEvent()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            int fired = 0;
            svc.OnFriendRemoved += (pl, fr) => fired++;
            svc.RemoveFriend(1, 2);
            Assert.AreEqual(1, fired);
        }

        // ── GetFriends / GetOnlineFriends / GetBestFriends ──────────────────

        [Test]
        public void GetFriends_Empty()
        {
            var svc = new FriendService();
            Assert.AreEqual(0, svc.GetFriends(1).Count);
        }

        [Test]
        public void GetFriends_AfterAdd()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            svc.AddFriend(1, 3);
            Assert.AreEqual(2, svc.GetFriends(1).Count);
        }

        [Test]
        public void GetOnlineFriends_OnlyOnline()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            svc.AddFriend(1, 3);
            svc.SetOnline(1, 2, true);
            Assert.AreEqual(1, svc.GetOnlineFriends(1).Count);
        }

        [Test]
        public void GetBestFriends_SortedByIntimacy()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            svc.AddFriend(1, 3);
            svc.AddFriend(1, 4);
            svc.AddIntimacy(1, 3, 100);
            svc.AddIntimacy(1, 2, 50);
            var best = svc.GetBestFriends(1, 2);
            Assert.AreEqual(2, best.Count);
            Assert.AreEqual(3, best[0].friendPlayerId);
            Assert.AreEqual(2, best[1].friendPlayerId);
        }

        [Test]
        public void GetBestFriends_N_Limit()
        {
            var svc = new FriendService();
            for (int i = 1; i <= 5; i++) svc.AddFriend(1, i);
            Assert.AreEqual(2, svc.GetBestFriends(1, 2).Count);
        }

        // ── AddIntimacy ─────────────────────────────────────────────────────

        [Test]
        public void AddIntimacy_Success()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            int newIntimacy = svc.AddIntimacy(1, 2, 50);
            Assert.AreEqual(50, newIntimacy);
        }

        [Test]
        public void AddIntimacy_Accumulates()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            svc.AddIntimacy(1, 2, 30);
            svc.AddIntimacy(1, 2, 70);
            Assert.AreEqual(100, svc.AddIntimacy(1, 2, 0));
        }

        [Test]
        public void AddIntimacy_ClampsAtZero()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            svc.AddIntimacy(1, 2, 50);
            svc.AddIntimacy(1, 2, -100);
            Assert.AreEqual(0, svc.AddIntimacy(1, 2, 0));
        }

        [Test]
        public void AddIntimacy_NotFriend_ReturnsZero()
        {
            var svc = new FriendService();
            Assert.AreEqual(0, svc.AddIntimacy(1, 2, 50));
        }

        [Test]
        public void AddIntimacy_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new FriendService(null, host);
            svc.AddFriend(1, 2);
            svc.AddIntimacy(1, 2, 50);
            Assert.AreEqual(1, host.IntimacyCalls);
            Assert.AreEqual(50, host.LastIntimacy);
            Assert.AreEqual(50, host.LastDelta);
        }

        [Test]
        public void AddIntimacy_FiresEvent()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            int fired = 0;
            svc.OnIntimacyChanged += (pl, fr, ix) => fired++;
            svc.AddIntimacy(1, 2, 50);
            Assert.AreEqual(1, fired);
        }

        // ── SetOnline ───────────────────────────────────────────────────────

        [Test]
        public void SetOnline_Success()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            Assert.IsTrue(svc.SetOnline(1, 2, true));
        }

        [Test]
        public void SetOnline_Updates()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            svc.SetOnline(1, 2, true);
            var f = svc.GetFriends(1)[0];
            Assert.IsTrue(f.isOnline);
        }

        [Test]
        public void SetOnline_NotFriend_ReturnsFalse()
        {
            var svc = new FriendService();
            Assert.IsFalse(svc.SetOnline(1, 2, true));
        }

        [Test]
        public void SetOnline_WithLoginSec()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 2);
            svc.SetOnline(1, 2, true, 1234567890);
            Assert.AreEqual(1234567890, svc.GetFriends(1)[0].lastLoginSec);
        }

        [Test]
        public void SetOnline_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new FriendService(null, host);
            svc.AddFriend(1, 2);
            svc.SetOnline(1, 2, true);
            Assert.AreEqual(1, host.OnlineCalls);
            Assert.IsTrue(host.LastOnline);
        }

        // ── SendMessage / GetMessages ───────────────────────────────────────

        [Test]
        public void SendMessage_Success()
        {
            var svc = new FriendService();
            Assert.IsTrue(svc.SendMessage(1, 2, "Hello"));
        }

        [Test]
        public void SendMessage_NullOrEmpty_ReturnsFalse()
        {
            var svc = new FriendService();
            Assert.IsFalse(svc.SendMessage(1, 2, null));
            Assert.IsFalse(svc.SendMessage(1, 2, ""));
        }

        [Test]
        public void SendMessage_Format()
        {
            var svc = new FriendService();
            svc.SendMessage(1, 2, "Hello");
            var msgs = svc.GetMessages(2);
            Assert.AreEqual(1, msgs.Count);
            Assert.That(msgs[0], Does.Contain("1:Hello"));
        }

        [Test]
        public void SendMessage_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new FriendService(null, host);
            svc.SendMessage(1, 2, "Hi");
            Assert.AreEqual(1, host.MsgCalls);
        }

        [Test]
        public void SendMessage_FiresEvent()
        {
            var svc = new FriendService();
            int fired = 0;
            svc.OnMessageSent += (pl, fr) => fired++;
            svc.SendMessage(1, 2, "Hi");
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void GetMessages_Empty()
        {
            var svc = new FriendService();
            Assert.AreEqual(0, svc.GetMessages(1).Count);
        }

        [Test]
        public void GetMessages_Multiple()
        {
            var svc = new FriendService();
            svc.SendMessage(1, 2, "msg1");
            svc.SendMessage(3, 2, "msg2");
            var msgs = svc.GetMessages(2);
            Assert.AreEqual(2, msgs.Count);
        }

        // ── AttachRegistry ──────────────────────────────────────────────────

        [Test]
        public void AttachRegistry_BuildsCache()
        {
            var reg = new PcFriendRegistry();
            reg.Register(new PcFriendEntry { friendId = 1, playerId = 10, friendPlayerId = 20, addedTimeUnix = 100, intimacy = 5 });
            var svc = new FriendService();
            svc.AttachRegistry(reg);
            var friends = svc.GetFriends(10);
            Assert.AreEqual(1, friends.Count);
            Assert.AreEqual(5, friends[0].intimacy);
        }

        [Test]
        public void FriendService_WithoutHost_DoesNotThrow()
        {
            var svc = new FriendService();
            Assert.DoesNotThrow(() => svc.AddFriend(1, 2));
            Assert.DoesNotThrow(() => svc.AddIntimacy(1, 2, 10));
            Assert.DoesNotThrow(() => svc.SetOnline(1, 2, true));
            Assert.DoesNotThrow(() => svc.SendMessage(1, 2, "X"));
            Assert.DoesNotThrow(() => svc.RemoveFriend(1, 2));
        }
    }
}
