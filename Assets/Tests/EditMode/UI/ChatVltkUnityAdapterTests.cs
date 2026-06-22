// -----------------------------------------------------------------------------
// VLTK Mobile — Chat vltkunity adapter EditMode tests
// Phase 2 Commit 2b. Tests the ChatVltkUnityAdapter in isolation with a real
// ChatService (events can't be invoked from subclasses since they're declared
// as `event`) and a fake IChatCommandBus. Category: HUD.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HUD")]
    public class ChatVltkUnityAdapterTests
    {
        private class FakeChatBus : IChatCommandBus
        {
            public event System.Action OnChatOpenRequested;
            public event System.Action OnChatCloseRequested;
            public event System.Action<string> OnChatSendRequested;
            public event System.Action<int> OnChatCategoryChanged;

            public int OpenCount, CloseCount, CategoryChangeCount;
            public int SendCount;
            public string LastSentMessage;
            public int LastCategoryId = -1;

            public void PublishChatOpenRequested() { OpenCount++; OnChatOpenRequested?.Invoke(); }
            public void PublishChatCloseRequested() { CloseCount++; OnChatCloseRequested?.Invoke(); }
            public void PublishChatSendRequested(string message) { SendCount++; LastSentMessage = message; OnChatSendRequested?.Invoke(message); }
            public void PublishChatCategoryChanged(int categoryId) { CategoryChangeCount++; LastCategoryId = categoryId; OnChatCategoryChanged?.Invoke(categoryId); }
        }

        [Test]
        public void Bind_RendersMessagesFromService()
        {
            var service = new ChatService();
            service.PostSystemMessage("hello world");
            var bus = new FakeChatBus();
            var root = new VisualElement();
            var msgList = new VisualElement { name = "VltkChatMessageList" };
            root.Add(msgList);

            var adapter = new ChatVltkUnityAdapter(root, service, bus);
            adapter.Bind();

            Assert.GreaterOrEqual(adapter.RenderCount, 1);
            Assert.GreaterOrEqual(msgList.childCount, 1);
        }

        [Test]
        public void SelectCategory_PublishesCategoryChanged()
        {
            var service = new ChatService();
            var bus = new FakeChatBus();
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkChatMessageList" });

            var adapter = new ChatVltkUnityAdapter(root, service, bus);
            adapter.Bind();

            adapter.SelectCategory(3);

            Assert.AreEqual(1, bus.CategoryChangeCount);
            Assert.AreEqual(3, bus.LastCategoryId);
            Assert.AreEqual(3, adapter.ActiveCategoryId);
        }

        [Test]
        public void SendMessage_PublishesViaBus()
        {
            var service = new ChatService();
            var bus = new FakeChatBus();
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkChatMessageList" });

            var adapter = new ChatVltkUnityAdapter(root, service, bus);
            adapter.Bind();

            adapter.SendMessage("test message");

            Assert.AreEqual("test message", bus.LastSentMessage);
        }

        [Test]
        public void SendMessage_EmptyTextDoesNotPublish()
        {
            var service = new ChatService();
            var bus = new FakeChatBus();
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkChatMessageList" });

            var adapter = new ChatVltkUnityAdapter(root, service, bus);
            adapter.Bind();

            adapter.SendMessage("");
            adapter.SendMessage("   ");

            Assert.IsNull(bus.LastSentMessage);
        }

        [Test]
        public void SimulateSendClick_TriggersBus()
        {
            var service = new ChatService();
            var bus = new FakeChatBus();
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkChatMessageList" });
            root.Add(new VisualElement { name = "VltkChatSendBtn" });

            var adapter = new ChatVltkUnityAdapter(root, service, bus);
            adapter.Bind();

            adapter.SimulateSendClick();

            Assert.AreEqual(1, bus.SendCount);
        }

        [Test]
        public void SimulateCloseClick_PublishesCloseRequested()
        {
            var service = new ChatService();
            var bus = new FakeChatBus();
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkChatMessageList" });
            var closeBtn = new VisualElement { name = "VltkChatCloseBtn" };
            root.Add(closeBtn);

            var adapter = new ChatVltkUnityAdapter(root, service, bus);
            adapter.Bind();

            adapter.SimulateCloseClick();

            Assert.AreEqual(1, bus.CloseCount);
        }

        [Test]
        public void SimulateResizeClick_TogglesFullSize()
        {
            var service = new ChatService();
            var bus = new FakeChatBus();
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkChatMessageList" });
            var resizeBtn = new VisualElement { name = "VltkChatResizeBtn" };
            root.Add(resizeBtn);

            var adapter = new ChatVltkUnityAdapter(root, service, bus);
            adapter.Bind();

            Assert.IsFalse(adapter.IsFullSize);
            adapter.SimulateResizeClick();
            Assert.IsTrue(adapter.IsFullSize);
            adapter.SimulateResizeClick();
            Assert.IsFalse(adapter.IsFullSize);
        }

        [Test]
        public void RegisterCategory_CreatesClickableTab()
        {
            var service = new ChatService();
            var bus = new FakeChatBus();
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkChatMessageList" });

            var adapter = new ChatVltkUnityAdapter(root, service, bus);
            adapter.Bind();

            var tabBtn = new VisualElement();
            adapter.RegisterCategory(2, "Bang", tabBtn);

            adapter.SelectCategory(2);

            Assert.AreEqual(1, bus.CategoryChangeCount);
            Assert.AreEqual(2, bus.LastCategoryId);
        }

        [Test]
        public void Dispose_StopsRendering()
        {
            var service = new ChatService();
            var bus = new FakeChatBus();
            var root = new VisualElement();
            var msgList = new VisualElement { name = "VltkChatMessageList" };
            root.Add(msgList);

            var adapter = new ChatVltkUnityAdapter(root, service, bus);
            adapter.Bind();
            int countBefore = adapter.RenderCount;

            adapter.Dispose();
            service.PostSystemMessage("after dispose");

            Assert.AreEqual(countBefore, adapter.RenderCount);
        }

        [Test]
        public void RenderMessages_ClearsAndRebuilds()
        {
            var service = new ChatService();
            service.PostSystemMessage("sys1");
            service.PostSystemMessage("sys2");
            var bus = new FakeChatBus();
            var root = new VisualElement();
            var msgList = new VisualElement { name = "VltkChatMessageList" };
            root.Add(msgList);

            var adapter = new ChatVltkUnityAdapter(root, service, bus);
            adapter.Bind();

            int childrenAfterBind = msgList.childCount;
            adapter.RenderMessages();

            Assert.AreEqual(childrenAfterBind, msgList.childCount);
            Assert.GreaterOrEqual(msgList.childCount, 2);
        }
    }
}
