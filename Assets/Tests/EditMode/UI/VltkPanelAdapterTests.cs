// -----------------------------------------------------------------------------
// VLTK Mobile — VltkPanelAdapter EditMode tests
// Phase 2 Commit 2f. Tests the unified adapter for NpcDialog/Faction/Guild/
// Mail/Shop/Login panels. Category: HUD.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HUD")]
    public class VltkPanelAdapterTests
    {
        private class FakePanelsBus : IPanelsCommandBus
        {
            public event System.Action<PanelType> OnPanelClosed;
            public event System.Action<PanelType, string> OnPanelActionSelected;

            public int CloseCount, ActionCount;
            public PanelType? LastClosedPanel;
            public PanelType? LastActionPanel;
            public string LastActionText;

            public void PublishPanelClosed(PanelType panelType) { CloseCount++; LastClosedPanel = panelType; OnPanelClosed?.Invoke(panelType); }
            public void PublishPanelActionSelected(PanelType panelType, string action) { ActionCount++; LastActionPanel = panelType; LastActionText = action; OnPanelActionSelected?.Invoke(panelType, action); }
        }

        private static VisualElement MakeRoot(string prefix)
        {
            var root = new VisualElement();
            root.Add(new Label { name = prefix + "Title" });
            root.Add(new Label { name = prefix + "Content" });
            root.Add(new VisualElement { name = prefix + "ActionList" });
            root.Add(new VisualElement { name = prefix + "CloseBtn" });
            return root;
        }

        [Test]
        public void NpcDialog_SetTitle_UpdatesLabel()
        {
            var bus = new FakePanelsBus();
            var root = MakeRoot("VltkNpc");
            var adapter = new VltkPanelAdapter(root, bus, PanelType.NpcDialog);
            adapter.Bind();

            adapter.SetTitle("Lão Quái");

            Assert.AreEqual("Lão Quái", root.Q<Label>("VltkNpcTitle").text);
        }

        [Test]
        public void NpcDialog_SetContent_UpdatesLabel()
        {
            var bus = new FakePanelsBus();
            var root = MakeRoot("VltkNpc");
            var adapter = new VltkPanelAdapter(root, bus, PanelType.NpcDialog);
            adapter.Bind();

            adapter.SetContent("Ngươi muốn học võ công gì?");

            Assert.AreEqual("Ngươi muốn học võ công gì?", root.Q<Label>("VltkNpcContent").text);
        }

        [Test]
        public void NpcDialog_SetActions_CreatesButtons()
        {
            var bus = new FakePanelsBus();
            var root = MakeRoot("VltkNpc");
            var adapter = new VltkPanelAdapter(root, bus, PanelType.NpcDialog);
            adapter.Bind();

            adapter.SetActions(new List<string> { "Học Cái Bang", "Học Võ Đang", "Tạm biệt" });

            Assert.AreEqual(3, adapter.ActionCount);
            Assert.AreEqual(3, root.Q("VltkNpcActionList").childCount);
        }

        [Test]
        public void NpcDialog_SimulateActionClick_PublishesAction()
        {
            var bus = new FakePanelsBus();
            var root = MakeRoot("VltkNpc");
            var adapter = new VltkPanelAdapter(root, bus, PanelType.NpcDialog);
            adapter.Bind();
            adapter.SetActions(new List<string> { "Học Cái Bang", "Tạm biệt" });

            adapter.SimulateActionClick(0);

            Assert.AreEqual(1, bus.ActionCount);
            Assert.AreEqual(PanelType.NpcDialog, bus.LastActionPanel);
            Assert.AreEqual("Học Cái Bang", bus.LastActionText);
        }

        [Test]
        public void NpcDialog_SimulateCloseClick_PublishesClose()
        {
            var bus = new FakePanelsBus();
            var root = MakeRoot("VltkNpc");
            var adapter = new VltkPanelAdapter(root, bus, PanelType.NpcDialog);
            adapter.Bind();

            adapter.SimulateCloseClick();

            Assert.AreEqual(1, bus.CloseCount);
            Assert.AreEqual(PanelType.NpcDialog, bus.LastClosedPanel);
        }

        [Test]
        public void Guild_SimulateCloseClick_PublishesCorrectPanelType()
        {
            var bus = new FakePanelsBus();
            var root = MakeRoot("VltkGuild");
            var adapter = new VltkPanelAdapter(root, bus, PanelType.Guild);
            adapter.Bind();

            adapter.SimulateCloseClick();

            Assert.AreEqual(PanelType.Guild, bus.LastClosedPanel);
        }

        [Test]
        public void Shop_SetActions_WorksForShopPanel()
        {
            var bus = new FakePanelsBus();
            var root = MakeRoot("VltkShop");
            var adapter = new VltkPanelAdapter(root, bus, PanelType.Shop);
            adapter.Bind();

            adapter.SetActions(new List<string> { "Mua", "Bán", "Đóng" });

            Assert.AreEqual(3, adapter.ActionCount);
            Assert.AreEqual(PanelType.Shop, adapter.PanelType);
        }

        [Test]
        public void Login_SetTitle_WorksForLoginPanel()
        {
            var bus = new FakePanelsBus();
            var root = MakeRoot("VltkLogin");
            var adapter = new VltkPanelAdapter(root, bus, PanelType.Login);
            adapter.Bind();

            adapter.SetTitle("Đăng Nhập");

            Assert.AreEqual("Đăng Nhập", root.Q<Label>("VltkLoginTitle").text);
        }

        [Test]
        public void Mail_SimulateActionClick_OutOfRangeDoesNothing()
        {
            var bus = new FakePanelsBus();
            var root = MakeRoot("VltkMail");
            var adapter = new VltkPanelAdapter(root, bus, PanelType.Mail);
            adapter.Bind();
            adapter.SetActions(new List<string> { "Đọc" });

            adapter.SimulateActionClick(5);

            Assert.AreEqual(0, bus.ActionCount);
        }

        [Test]
        public void Faction_SetActions_EmptyListClearsActionList()
        {
            var bus = new FakePanelsBus();
            var root = MakeRoot("VltkFaction");
            var adapter = new VltkPanelAdapter(root, bus, PanelType.Faction);
            adapter.Bind();
            adapter.SetActions(new List<string> { "Tham gia", "Rời đi" });
            Assert.AreEqual(2, root.Q("VltkFactionActionList").childCount);

            adapter.SetActions(new List<string>());

            Assert.AreEqual(0, adapter.ActionCount);
            Assert.AreEqual(0, root.Q("VltkFactionActionList").childCount);
        }

        [Test]
        public void SetActions_NullDoesNotCrash()
        {
            var bus = new FakePanelsBus();
            var root = MakeRoot("VltkNpc");
            var adapter = new VltkPanelAdapter(root, bus, PanelType.NpcDialog);
            adapter.Bind();

            Assert.DoesNotThrow(() => adapter.SetActions(null));
        }
    }
}
