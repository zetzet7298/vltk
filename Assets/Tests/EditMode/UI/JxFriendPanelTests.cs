// -----------------------------------------------------------------------------
// VLTK Mobile — JX friend list panel E4 tests
// Port proof for jx-cocos KuiFriendListVN.cpp (3 relation groups, dedup, sync).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxFriendPanelTests
    {
        // ---- Constants / source layout ----

        [Test]
        public void RelationCodes_MatchSourceSectionSwitch()
        {
            // Source section switch: 2 brothers, 3 pks, 5 blacklist.
            Assert.AreEqual(2, (int)JxFriendRelation.Friend);
            Assert.AreEqual(3, (int)JxFriendRelation.Enemy);
            Assert.AreEqual(5, (int)JxFriendRelation.Blacklist);
        }

        [Test]
        public void DefaultRelation_IsFriend_Section2()
        {
            Assert.AreEqual(JxFriendRelation.Friend, JxFriendPanelState.DefaultRelation);
            var s = new JxFriendPanelState();
            Assert.AreEqual(JxFriendRelation.Friend, s.CurrentRelation);
        }

        [Test]
        public void ListViewLayout_MatchesSourceConstants()
        {
            Assert.AreEqual(124f, JxFriendPanelState.ListViewWidth);
            Assert.AreEqual(267f, JxFriendPanelState.ListViewHeight);
            Assert.AreEqual(3f, JxFriendPanelState.ListViewPosX);
            Assert.AreEqual(27f, JxFriendPanelState.ListViewPosY);
            Assert.AreEqual(114f, JxFriendPanelState.ItemWidth);
            Assert.AreEqual(16f, JxFriendPanelState.ItemHeight);
        }

        [Test]
        public void Colors_OnlineWhite_OfflineGray()
        {
            Assert.AreEqual(Color.white, JxFriendPanelState.OnlineColor);
            Assert.AreEqual(new Color(150f / 255f, 150f / 255f, 150f / 255f, 1f), JxFriendPanelState.OfflineColor);
            Assert.AreEqual(JxFriendPanelState.OnlineColor, JxFriendPanelState.ColorFor(true));
            Assert.AreEqual(JxFriendPanelState.OfflineColor, JxFriendPanelState.ColorFor(false));
        }

        // ---- Open / close / selection ----

        [Test]
        public void OpenClose_TogglesIsOpen_AndResetsSelection()
        {
            var s = new JxFriendPanelState();
            Assert.IsFalse(s.IsOpen);
            s.Open();
            Assert.IsTrue(s.IsOpen);
            Assert.AreEqual(-1, s.SelectedIndex);
            s.AddFriend("A");
            s.Select(0);
            Assert.AreEqual(0, s.SelectedIndex);
            s.Close();
            Assert.IsFalse(s.IsOpen);
            Assert.AreEqual(-1, s.SelectedIndex);
        }

        [Test]
        public void SelectRelation_ChangesTabAndClearsSelection()
        {
            var s = new JxFriendPanelState();
            s.Open();
            s.AddFriend("A");
            s.Select(0);
            Assert.IsTrue(s.SelectRelation(JxFriendRelation.Enemy));
            Assert.AreEqual(JxFriendRelation.Enemy, s.CurrentRelation);
            Assert.AreEqual(-1, s.SelectedIndex);
        }

        [Test]
        public void Select_ValidIndex_SetsIndexAndName()
        {
            var s = new JxFriendPanelState();
            s.Open();
            s.AddFriend("Anh");
            s.AddFriend("Bình");
            Assert.IsTrue(s.Select(1));
            Assert.AreEqual(1, s.SelectedIndex);
            Assert.AreEqual("Bình", s.SelectedName);
        }

        [Test]
        public void Select_OutOfRange_ReturnsFalseAndClears()
        {
            var s = new JxFriendPanelState();
            s.Open();
            s.AddFriend("A");
            Assert.IsFalse(s.Select(5));
            Assert.AreEqual(-1, s.SelectedIndex);
        }

        // ---- Mutators: dedup ----

        [Test]
        public void AddFriend_DedupByName_SkipsExisting()
        {
            var s = new JxFriendPanelState();
            Assert.IsTrue(s.AddFriend("Cường"));
            Assert.IsFalse(s.AddFriend("Cường"));
            Assert.AreEqual(1, s.Count(JxFriendRelation.Friend));
        }

        [Test]
        public void Add_RoutesIntoCorrectGroup()
        {
            var s = new JxFriendPanelState();
            s.Add(JxFriendRelation.Friend, "A", true);
            s.Add(JxFriendRelation.Enemy, "B", false);
            s.Add(JxFriendRelation.Blacklist, "C", true);
            Assert.AreEqual(1, s.Count(JxFriendRelation.Friend));
            Assert.AreEqual(1, s.Count(JxFriendRelation.Enemy));
            Assert.AreEqual(1, s.Count(JxFriendRelation.Blacklist));
        }

        [Test]
        public void Add_IgnoresNullOrEmptyName()
        {
            var s = new JxFriendPanelState();
            Assert.IsFalse(s.Add(JxFriendRelation.Friend, "", true));
            Assert.IsFalse(s.Add(JxFriendRelation.Friend, null, true));
            Assert.AreEqual(0, s.Count(JxFriendRelation.Friend));
        }

        [Test]
        public void AddDefaultOnline_FromSourceState1()
        {
            // Source AddFriend/AddBlackList set State=1 (online) until server sync.
            var s = new JxFriendPanelState();
            s.AddFriend("Dũng");
            s.AddBlacklist("Ác");
            Assert.IsTrue(s.GetEntries(JxFriendRelation.Friend)[0].Online);
            Assert.IsTrue(s.GetEntries(JxFriendRelation.Blacklist)[0].Online);
        }

        // ---- Erase / SyncState across all groups ----

        [Test]
        public void Erase_RemovesFromAllGroups()
        {
            var s = new JxFriendPanelState();
            s.Add(JxFriendRelation.Friend, "X", true);
            s.Add(JxFriendRelation.Enemy, "X", false);
            s.Add(JxFriendRelation.Blacklist, "X", true);
            Assert.AreEqual(3, s.Erase("X"));
            Assert.AreEqual(0, s.Count(JxFriendRelation.Friend));
            Assert.AreEqual(0, s.Count(JxFriendRelation.Enemy));
            Assert.AreEqual(0, s.Count(JxFriendRelation.Blacklist));
        }

        [Test]
        public void SyncState_UpdatesAllGroups_RetainsCount()
        {
            var s = new JxFriendPanelState();
            s.Add(JxFriendRelation.Friend, "Y", true);
            s.Add(JxFriendRelation.Enemy, "Y", true);
            int updated = s.SyncState("Y", false);
            Assert.AreEqual(2, updated);
            Assert.IsFalse(s.GetEntries(JxFriendRelation.Friend)[0].Online);
            Assert.IsFalse(s.GetEntries(JxFriendRelation.Enemy)[0].Online);
            Assert.AreEqual(1, s.Count(JxFriendRelation.Friend));
        }

        [Test]
        public void Erase_SelectedRow_ClearsSelection()
        {
            var s = new JxFriendPanelState();
            s.Open();
            s.AddFriend("Anh");
            s.Select(0);
            s.Erase("Anh");
            Assert.AreEqual(-1, s.SelectedIndex);
            Assert.AreEqual(string.Empty, s.SelectedName);
        }
    }
}
