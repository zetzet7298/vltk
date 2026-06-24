// -----------------------------------------------------------------------------
// VLTK Mobile — JX toolbar menu E0 tests
// Port proof for KgameWorldVN.cpp 9 CCMenuItemSprite toolbar buttons.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxToolbarMenuTests
    {
        [Test]
        public void SourceConfig_HasNineButtons_InCocosOrder()
        {
            Assert.AreEqual(9, JxToolbarMenuState.Buttons.Length);
            Assert.AreEqual(JxHudPanel.Character, JxToolbarMenuState.Buttons[0].Panel);
            Assert.AreEqual(JxHudPanel.Inventory, JxToolbarMenuState.Buttons[1].Panel);
            Assert.AreEqual(JxHudPanel.Skill, JxToolbarMenuState.Buttons[2].Panel);
            Assert.AreEqual(JxHudPanel.Quest, JxToolbarMenuState.Buttons[3].Panel);
            Assert.AreEqual(JxHudPanel.Friend, JxToolbarMenuState.Buttons[4].Panel);
            Assert.AreEqual(JxHudPanel.Team, JxToolbarMenuState.Buttons[5].Panel);
            Assert.AreEqual(JxHudPanel.Guild, JxToolbarMenuState.Buttons[6].Panel);
            Assert.AreEqual(JxHudPanel.Settings, JxToolbarMenuState.Buttons[7].Panel);
            Assert.AreEqual(JxHudPanel.Shop, JxToolbarMenuState.Buttons[8].Panel);
        }

        [Test]
        public void SpritePaths_UseExactUiVnToolbarPngs()
        {
            var role = JxToolbarMenuState.Get(JxHudPanel.Character);
            Assert.AreEqual("ui_vn/toolbar/nhanvat.png", role.NormalSprite);
            Assert.AreEqual("ui_vn/toolbar/nhanvat2.png", role.SelectedSprite);
            Assert.AreEqual("ui_vn/toolbar/nhanvat2.png", role.DisabledSprite);

            var inventory = JxToolbarMenuState.Get(JxHudPanel.Inventory);
            Assert.AreEqual("ui_vn/toolbar/hanhtrang.png", inventory.NormalSprite);
            Assert.AreEqual("ui_vn/toolbar/hanhtrang2.png", inventory.SelectedSprite);
            Assert.AreEqual("ui_vn/toolbar/hanhtrang2.png", inventory.DisabledSprite);

            var skill = JxToolbarMenuState.Get(JxHudPanel.Skill);
            Assert.AreEqual("ui_vn/toolbar/vocong.png", skill.NormalSprite);
            Assert.AreEqual("ui_vn/toolbar/vocong2.png", skill.SelectedSprite);
            Assert.AreEqual("ui_vn/toolbar/vocong2.png", skill.DisabledSprite);
        }

        [Test]
        public void Shop_UsesDistinctThirdDisabledSprite_AndSourceOverridePositionScale()
        {
            var shop = JxToolbarMenuState.Get(JxHudPanel.Shop);
            Assert.AreEqual("ui_vn/toolbar/kytrancac1.png", shop.NormalSprite);
            Assert.AreEqual("ui_vn/toolbar/kytrancac2.png", shop.SelectedSprite);
            Assert.AreEqual("ui_vn/toolbar/kytrancac3.png", shop.DisabledSprite);
            Assert.AreEqual(0.9f, shop.Scale);
            Assert.IsTrue(shop.HasExplicitLocalPosition);
            Assert.AreEqual(new Vector2(215f, 0f), shop.LocalPosition);
        }

        [Test]
        public void NonShopButtons_UseSourceDefaultScale085()
        {
            for (int i = 0; i < JxToolbarMenuState.Buttons.Length - 1; i++)
                Assert.AreEqual(0.85f, JxToolbarMenuState.Buttons[i].Scale, "button " + i);
        }

        [Test]
        public void MenuLayout_UsesHorizontalPaddingAndTopPositionFormula()
        {
            var state = new JxToolbarMenuState();
            Assert.AreEqual(5f, JxToolbarMenuState.HorizontalPadding);
            Assert.AreEqual(new Vector2(400f, 570f), state.MenuPosition(800f, 600f, Vector2.zero));
            Assert.AreEqual(new Vector2(410f, 590f), state.MenuPosition(800f, 600f, new Vector2(10f, 5f), 15f));
        }

        [Test]
        public void Policies_MatchSourceCallbacks()
        {
            Assert.AreEqual(JxToolbarButtonPolicy.AlwaysReopen, JxToolbarMenuState.Get(JxHudPanel.Character).Policy);
            Assert.AreEqual(JxToolbarButtonPolicy.Toggle, JxToolbarMenuState.Get(JxHudPanel.Inventory).Policy);
            Assert.AreEqual(JxToolbarButtonPolicy.OpenIfClosed, JxToolbarMenuState.Get(JxHudPanel.Skill).Policy);
            Assert.AreEqual(JxToolbarButtonPolicy.NoticeOnly, JxToolbarMenuState.Get(JxHudPanel.Quest).Policy);
            Assert.AreEqual(JxToolbarButtonPolicy.Toggle, JxToolbarMenuState.Get(JxHudPanel.Friend).Policy);
            Assert.AreEqual(JxToolbarButtonPolicy.OpenIfClosed, JxToolbarMenuState.Get(JxHudPanel.Team).Policy);
            Assert.AreEqual(JxToolbarButtonPolicy.Toggle, JxToolbarMenuState.Get(JxHudPanel.Guild).Policy);
            Assert.AreEqual(JxToolbarButtonPolicy.AlwaysReopen, JxToolbarMenuState.Get(JxHudPanel.Settings).Policy);
            Assert.AreEqual(JxToolbarButtonPolicy.AlwaysReopen, JxToolbarMenuState.Get(JxHudPanel.Shop).Policy);
        }

        [Test]
        public void Callbacks_NamesMatchSourceMethods()
        {
            Assert.AreEqual("mRoleStatusCallback", JxToolbarMenuState.Get(JxHudPanel.Character).Callback);
            Assert.AreEqual("mItemsCallback", JxToolbarMenuState.Get(JxHudPanel.Inventory).Callback);
            Assert.AreEqual("mSkillsCallback", JxToolbarMenuState.Get(JxHudPanel.Skill).Callback);
            Assert.AreEqual("lambdaTaskNotice", JxToolbarMenuState.Get(JxHudPanel.Quest).Callback);
            Assert.AreEqual("mFriendCallback", JxToolbarMenuState.Get(JxHudPanel.Friend).Callback);
            Assert.AreEqual("mTeamCallback", JxToolbarMenuState.Get(JxHudPanel.Team).Callback);
            Assert.AreEqual("mFactionCallback", JxToolbarMenuState.Get(JxHudPanel.Guild).Callback);
            Assert.AreEqual("mOptionsCallback", JxToolbarMenuState.Get(JxHudPanel.Settings).Callback);
            Assert.AreEqual("mQizCallback", JxToolbarMenuState.Get(JxHudPanel.Shop).Callback);
        }

        [Test]
        public void InventoryPolicy_TogglesOpenAndClose()
        {
            var state = new JxToolbarMenuState();
            var open = state.Press(JxHudPanel.Inventory);
            Assert.IsTrue(open.Open);
            Assert.IsFalse(open.Close);
            Assert.IsTrue(state.IsOpen(JxHudPanel.Inventory));

            var close = state.Press(JxHudPanel.Inventory);
            Assert.IsFalse(close.Open);
            Assert.IsTrue(close.Close);
            Assert.IsFalse(state.IsOpen(JxHudPanel.Inventory));
        }

        [Test]
        public void SkillPolicy_OpenIfClosed_NoOpWhenAlreadyOpen()
        {
            var state = new JxToolbarMenuState();
            var open = state.Press(JxHudPanel.Skill);
            Assert.IsTrue(open.Open);
            Assert.IsTrue(state.IsOpen(JxHudPanel.Skill));

            var second = state.Press(JxHudPanel.Skill);
            Assert.IsFalse(second.Open);
            Assert.IsFalse(second.Close);
            Assert.IsTrue(state.IsOpen(JxHudPanel.Skill));
        }

        [Test]
        public void CharacterPolicy_ReopensWhenAlreadyOpen()
        {
            var state = new JxToolbarMenuState();
            state.Press(JxHudPanel.Character);
            var second = state.Press(JxHudPanel.Character);
            Assert.IsTrue(second.Close);
            Assert.IsTrue(second.Reopen);
            Assert.IsTrue(second.Open);
            Assert.IsTrue(state.IsOpen(JxHudPanel.Character));
        }

        [Test]
        public void QuestPolicy_ShowsNoticeOnly_DoesNotOpenPanel()
        {
            var state = new JxToolbarMenuState();
            var cmd = state.Press(JxHudPanel.Quest);
            Assert.IsFalse(cmd.Open);
            Assert.IsFalse(cmd.Close);
            Assert.IsFalse(state.IsOpen(JxHudPanel.Quest));
            Assert.AreEqual(JxToolbarMenuState.TaskNotice, cmd.Notice);
            Assert.AreEqual("Đại hiệp có thể xem tại Tiếu Ngạo Giang Hồ Lục", cmd.Notice);
        }

        [Test]
        public void IndexOf_ReturnsCocosOrderOrMinusOne()
        {
            Assert.AreEqual(0, JxToolbarMenuState.IndexOf(JxHudPanel.Character));
            Assert.AreEqual(8, JxToolbarMenuState.IndexOf(JxHudPanel.Shop));
            Assert.AreEqual(-1, JxToolbarMenuState.IndexOf(JxHudPanel.WorldMap));
        }
    }
}
