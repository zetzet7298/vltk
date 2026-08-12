// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 PopupManager lifecycle tests (EditMode, Category "Popup")
// Spec REQ-2 (single-focus/backdrop), REQ-10 (EditMode coverage).
// Pure-VisualElement: no UIDocument / MonoBehaviour needed.
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI.Popup;
using PopupWindow = VLTK.UI.Popup.PopupWindow;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("Popup")]
    public class PopupManagerTests
    {
        /// <summary>Stub content recording lifecycle calls + a body marker element.</summary>
        private sealed class StubContent : IPopupContent
        {
            public string TitleVi => "Kiểm Tra";
            public int BuildCalls;
            public int ShowCalls;
            public int CloseCalls;
            public readonly List<VisualElement> Bodies = new();

            public void Build(VisualElement body)
            {
                BuildCalls++;
                Bodies.Add(body);
                body.Add(new Label("body-marker"));
            }
            public void OnShow() => ShowCalls++;
            public void OnClose() => CloseCalls++;
        }

        private VisualElement _host;
        private PopupManager _manager;

        [SetUp]
        public void SetUp()
        {
            _host = new VisualElement();
            _manager = new PopupManager(_host);
        }

        [Test]
        public void Show_AddsBackdropAndWindow_AndIsOpenTrue()
        {
            var content = new StubContent();

            _manager.Show(content);

            Assert.IsTrue(_manager.IsOpen, "IsOpen should be true after Show");
            Assert.AreEqual(2, _host.childCount, "host should hold backdrop+window");
            // one is backdrop, one is the PopupWindow
            bool hasBackdrop = false, hasWindow = false;
            foreach (var c in _host.Children())
            {
                if (c.ClassListContains("popup-backdrop")) hasBackdrop = true;
                if (c is PopupWindow) hasWindow = true;
            }
            Assert.IsTrue(hasBackdrop, "backdrop should be present");
            Assert.IsTrue(hasWindow, "PopupWindow should be present");
        }

        [Test]
        public void Show_BuildsBodyOnce_AndFiresOnShow()
        {
            var content = new StubContent();

            _manager.Show(content);

            Assert.AreEqual(1, content.BuildCalls, "Build should run once");
            Assert.AreEqual(1, content.ShowCalls, "OnShow should fire once");
        }

        [Test]
        public void Close_RemovesWindowAndBackdrop_AndFiresOnClose()
        {
            var content = new StubContent();
            _manager.Show(content);

            _manager.Close();

            Assert.IsFalse(_manager.IsOpen, "IsOpen should be false after Close");
            Assert.AreEqual(0, _host.childCount, "host should be empty after Close");
            Assert.AreEqual(1, content.CloseCalls, "OnClose should fire once");
        }

        [Test]
        public void Show_WhenAlreadyOpen_ClosesPriorFirst_SingleFocus()
        {
            var first = new StubContent();
            var second = new StubContent();
            _manager.Show(first);

            _manager.Show(second);

            Assert.IsTrue(_manager.IsOpen);
            // single-focus: only one backdrop + one window remain
            int backdrops = 0, windows = 0;
            foreach (var c in _host.Children())
            {
                if (c.ClassListContains("popup-backdrop")) backdrops++;
                if (c is PopupWindow) windows++;
            }
            Assert.AreEqual(1, backdrops, "exactly one backdrop");
            Assert.AreEqual(1, windows, "exactly one window (single-focus)");
            Assert.AreEqual(1, first.CloseCalls, "prior content OnClose must fire");
            Assert.AreEqual(1, second.ShowCalls, "new content OnShow must fire");
        }

        [Test]
        public void Show_RejectsNullContent()
        {
            Assert.Throws<System.ArgumentNullException>(() => _manager.Show(null));
        }

        [Test]
        public void Close_WhenNotOpen_IsNoOp()
        {
            Assert.DoesNotThrow(() => _manager.Close());
            Assert.IsFalse(_manager.IsOpen);
        }

        [Test]
        public void CloseButton_RaisesManagerClose()
        {
            var content = new StubContent();
            _manager.Show(content);

            // find the active window and fire its close affordance
            PopupWindow window = null;
            foreach (var c in _host.Children())
                if (c is PopupWindow w) { window = w; break; }
            Assert.IsNotNull(window, "window should be present");

            window.RaiseClosed();

            Assert.IsFalse(_manager.IsOpen, "close affordance should close the manager");
        }

        [Test]
        public void Show_CreatesBackdropWithClickToCloseClass()
        {
            // Backdrop element must exist and carry the click-to-close marker.
            // The backdrop-click-closes behavior itself is verified in Phase F
            // (integration screenshot/interaction) because dispatching a real
            // PointerDownEvent in EditMode is API-fragile across Unity versions.
            var content = new StubContent();

            _manager.Show(content);

            VisualElement backdrop = null;
            foreach (var c in _host.Children())
                if (c.ClassListContains("popup-backdrop")) backdrop = c;
            Assert.IsNotNull(backdrop, "backdrop should be present after Show");
            Assert.IsTrue(backdrop.pickingMode == PickingMode.Position,
                "backdrop must capture pointer events (Position) to allow click-to-close");
        }
    }
}
