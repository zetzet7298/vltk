// -----------------------------------------------------------------------------
// VLTK Mobile — JX Toolbar EditMode tests
// Verifies: 9-button config (sprite paths + labels + order), toggle open/close
// semantics, single-panel-open, and adapter click → command bus publish +
// selected-class render. Category: HudJxcocos.
// -----------------------------------------------------------------------------

using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxToolbarTests
    {
        private class FakeBus : IJxHudCommandBus
        {
            public System.Collections.Generic.List<JxHudPanel> PanelRequests = new();
            public System.Collections.Generic.List<JxHudAction> ActionRequests = new();
            public void PublishPanelRequested(JxHudPanel panel) => PanelRequests.Add(panel);
            public void PublishActionRequested(JxHudAction action) => ActionRequests.Add(action);
        }

        // ---- Config ----

        [Test]
        public void Config_HasNineMenuButtonsInOrder()
        {
            Assert.That(JxToolbarConfig.Count, Is.EqualTo(9));
            Assert.That(JxToolbarConfig.Menu[0].Panel, Is.EqualTo(JxHudPanel.Character));
            Assert.That(JxToolbarConfig.Menu[8].Panel, Is.EqualTo(JxHudPanel.Shop));
        }

        [Test]
        public void Config_CharacterButton_HasCorrectSpritesAndLabel()
        {
            var c = JxToolbarConfig.Get(JxHudPanel.Character);
            Assert.That(c.Label, Is.EqualTo("Nhân Vật"));
            Assert.That(c.NormalSprite, Is.EqualTo("toolbar/nhanvat"));
            Assert.That(c.SelectedSprite, Is.EqualTo("toolbar/nhanvat2"));
        }

        [Test]
        public void Config_ShopButton_UsesThreeDistinctSprites()
        {
            // Source: kytrancac1 (normal) / kytrancac2 (selected) / kytrancac3 (disabled).
            var c = JxToolbarConfig.Get(JxHudPanel.Shop);
            Assert.That(c.NormalSprite, Is.EqualTo("toolbar/kytrancac1"));
            Assert.That(c.SelectedSprite, Is.EqualTo("toolbar/kytrancac2"));
            Assert.That(c.DisabledSprite, Is.EqualTo("toolbar/kytrancac3"));
        }

        // ---- State toggle semantics ----

        [Test]
        public void Toggle_OpensThenClosesSamePanel()
        {
            var s = new JxToolbarState();
            Assert.That(s.OpenPanel, Is.EqualTo(JxHudPanel.None));
            s.Toggle(JxHudPanel.Inventory);
            Assert.That(s.OpenPanel, Is.EqualTo(JxHudPanel.Inventory));
            Assert.IsTrue(s.IsSelected(JxHudPanel.Inventory));
            s.Toggle(JxHudPanel.Inventory);
            Assert.That(s.OpenPanel, Is.EqualTo(JxHudPanel.None));
            Assert.IsFalse(s.IsSelected(JxHudPanel.Inventory));
        }

        [Test]
        public void Toggle_SwitchesPanel_SingleOpenAtATime()
        {
            var s = new JxToolbarState();
            s.Toggle(JxHudPanel.Skill);
            s.Toggle(JxHudPanel.Quest);
            Assert.That(s.OpenPanel, Is.EqualTo(JxHudPanel.Quest));
            Assert.IsFalse(s.IsSelected(JxHudPanel.Skill));
            Assert.IsTrue(s.IsSelected(JxHudPanel.Quest));
        }

        [Test]
        public void Toggle_None_IsNoOp()
        {
            var s = new JxToolbarState();
            s.Toggle(JxHudPanel.None);
            Assert.That(s.OpenPanel, Is.EqualTo(JxHudPanel.None));
        }

        // ---- Adapter ----

        private static VisualElement MakeToolbarRoot()
        {
            var root = new VisualElement();
            for (int i = 0; i < JxToolbarConfig.Menu.Length; i++)
            {
                var cfg = JxToolbarConfig.Menu[i];
                root.Add(new Button { name = JxToolbarAdapter.ButtonName(cfg.Panel) });
            }
            return root;
        }

        [Test]
        public void Adapter_Bind_RegistersAllNineButtons()
        {
            var root = MakeToolbarRoot();
            var state = new JxToolbarState();
            var bus = new FakeBus();
            var adapter = new JxToolbarAdapter(root, state, bus);
            adapter.Bind();
            // All 9 buttons should be present and none selected initially.
            for (int i = 0; i < JxToolbarConfig.Menu.Length; i++)
            {
                var btn = root.Q<VisualElement>(JxToolbarAdapter.ButtonName(JxToolbarConfig.Menu[i].Panel));
                Assert.IsNotNull(btn, "button " + JxToolbarConfig.Menu[i].Panel);
                Assert.IsFalse(btn.ClassListContains(JxToolbarAdapter.SelectedClass));
            }
        }

        [Test]
        public void Adapter_Click_TogglesStatePublishesAndHighlights()
        {
            var root = MakeToolbarRoot();
            var state = new JxToolbarState();
            var bus = new FakeBus();
            var adapter = new JxToolbarAdapter(root, state, bus);
            adapter.Bind();

            // Simulate click on Character button (coordinator path).
            var charBtn = root.Q<VisualElement>(JxToolbarAdapter.ButtonName(JxHudPanel.Character));
            adapter.Click(JxHudPanel.Character);

            Assert.That(bus.PanelRequests, Does.Contain(JxHudPanel.Character));
            Assert.IsTrue(charBtn.ClassListContains(JxToolbarAdapter.SelectedClass));

            // Click again → closes.
            adapter.Click(JxHudPanel.Character);
            Assert.IsFalse(charBtn.ClassListContains(JxToolbarAdapter.SelectedClass));
            Assert.That(state.OpenPanel, Is.EqualTo(JxHudPanel.None));
        }

        [Test]
        public void Adapter_ClickDifferentButton_SwitchesHighlight()
        {
            var root = MakeToolbarRoot();
            var state = new JxToolbarState();
            var bus = new FakeBus();
            var adapter = new JxToolbarAdapter(root, state, bus);
            adapter.Bind();

            var skillBtn = root.Q<VisualElement>(JxToolbarAdapter.ButtonName(JxHudPanel.Skill));
            var questBtn = root.Q<VisualElement>(JxToolbarAdapter.ButtonName(JxHudPanel.Quest));
            adapter.Click(JxHudPanel.Skill);
            adapter.Click(JxHudPanel.Quest);

            Assert.IsFalse(skillBtn.ClassListContains(JxToolbarAdapter.SelectedClass));
            Assert.IsTrue(questBtn.ClassListContains(JxToolbarAdapter.SelectedClass));
        }
    }
}
