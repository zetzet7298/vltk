// -----------------------------------------------------------------------------
// VLTK Mobile — JX skill slots EditMode tests (port of KgameWorldVN.cpp)
// Verifies: 8 aux slots (MAX_FUZHUSKILL_COUNT), MAX_SKILL=2000 validation,
// assign main/aux, swap reorder, cooldown _nextUseTime math, ACC ini persist
// snapshot round-trip, render empty/ready/cooldown. Category: HudJxCocos.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxSkillSlotTests
    {
        private class FakeBus : IJxHudCommandBus
        {
            public List<JxHudPanel> PanelRequests = new();
            public List<JxHudAction> ActionRequests = new();
            public void PublishPanelRequested(JxHudPanel panel) => PanelRequests.Add(panel);
            public void PublishActionRequested(JxHudAction action) => ActionRequests.Add(action);
        }

        // ---- Port constants ----

        [Test]
        public void AuxSlotCount_Is8_PerSource()
        {
            // Nguồn: MAX_FUZHUSKILL_COUNT 8.
            Assert.That(JxSkillSlotState.AuxiliarySlotCount, Is.EqualTo(8));
        }

        [Test]
        public void MaxSkill_Is2000_PerSource()
        {
            // Nguồn: SkillDef.h MAX_SKILL 2000.
            Assert.That(JxSkillSlotState.MaxSkill, Is.EqualTo(2000));
        }

        // ---- SkillId validation (port) ----

        [Test]
        public void IsValidSkillId_Rejects0_Negative_AndMaxOrAbove()
        {
            // Nguồn: skillIdx > 0 && skillIdx < MAX_SKILL.
            Assert.IsFalse(JxSkillSlotState.IsValidSkillId(0));
            Assert.IsFalse(JxSkillSlotState.IsValidSkillId(-5));
            Assert.IsFalse(JxSkillSlotState.IsValidSkillId(2000));
            Assert.IsFalse(JxSkillSlotState.IsValidSkillId(5000));
            Assert.IsTrue(JxSkillSlotState.IsValidSkillId(1));
            Assert.IsTrue(JxSkillSlotState.IsValidSkillId(1999));
        }

        // ---- Assignment ----

        [Test]
        public void AssignMain_SetsSkillGenrePath()
        {
            var s = new JxSkillSlotState();
            Assert.IsTrue(s.AssignMain(42, 3, "spr/skillicon42.spr"));
            Assert.That(s.Main.SkillId, Is.EqualTo(42));
            Assert.That(s.Main.Genre, Is.EqualTo(3));
            Assert.That(s.Main.IconPath, Is.EqualTo("spr/skillicon42.spr"));
        }

        [Test]
        public void AssignMain_RejectsInvalidSkillId()
        {
            var s = new JxSkillSlotState();
            Assert.IsFalse(s.AssignMain(0, 1, "x"));
            Assert.IsFalse(s.AssignMain(2000, 1, "x"));
            Assert.That(s.Main.SkillId, Is.EqualTo(0));
        }

        [Test]
        public void AssignAux_SetsSlot_AndClearWithZero()
        {
            var s = new JxSkillSlotState();
            Assert.IsTrue(s.AssignAux(3, 77, 2, "spr/x.spr"));
            Assert.That(s.Aux(3).SkillId, Is.EqualTo(77));
            // Clear with skillId=0.
            Assert.IsTrue(s.ClearAux(3));
            Assert.That(s.Aux(3).SkillId, Is.EqualTo(0));
            Assert.That(s.Aux(3).IconPath, Is.EqualTo(string.Empty));
        }

        [Test]
        public void AssignAux_RejectsBadIndex()
        {
            var s = new JxSkillSlotState();
            Assert.IsFalse(s.AssignAux(-1, 1, 0, "x"));
            Assert.IsFalse(s.AssignAux(8, 1, 0, "x"));
        }

        // ---- Swap (drag reorder) ----

        [Test]
        public void SwapAux_ExchangesSlotContents()
        {
            var s = new JxSkillSlotState();
            s.AssignAux(0, 10, 1, "a");
            s.AssignAux(5, 20, 2, "b");
            Assert.IsTrue(s.SwapAux(0, 5));
            Assert.That(s.Aux(0).SkillId, Is.EqualTo(20));
            Assert.That(s.Aux(0).IconPath, Is.EqualTo("b"));
            Assert.That(s.Aux(5).SkillId, Is.EqualTo(10));
            Assert.That(s.Aux(5).IconPath, Is.EqualTo("a"));
        }

        [Test]
        public void SwapAux_SameIndex_NoOp_ReturnsTrue()
        {
            var s = new JxSkillSlotState();
            s.AssignAux(2, 30, 1, "c");
            Assert.IsTrue(s.SwapAux(2, 2));
            Assert.That(s.Aux(2).SkillId, Is.EqualTo(30));
        }

        // ---- Cooldown ----

        [Test]
        public void Cooldown_NextUseTime_Math()
        {
            // _nextUseTime = now + duration.
            var s = new JxSkillSlotState();
            s.SetAuxCooldown(1, nowMs: 1000, durationMs: 5000);
            // now=1000 → on cooldown, remain 5000.
            Assert.IsTrue(JxSkillSlotState.IsOnCooldown(s.Aux(1), 1000));
            Assert.That(JxSkillSlotState.CooldownRemainingMs(s.Aux(1), 1000), Is.EqualTo(5000));
            // now=4000 → remain 2000.
            Assert.IsTrue(JxSkillSlotState.IsOnCooldown(s.Aux(1), 4000));
            Assert.That(JxSkillSlotState.CooldownRemainingMs(s.Aux(1), 4000), Is.EqualTo(2000));
            // now=6000 → ready (>= nextUseTime).
            Assert.IsFalse(JxSkillSlotState.IsOnCooldown(s.Aux(1), 6000));
            Assert.That(JxSkillSlotState.CooldownRemainingMs(s.Aux(1), 6000), Is.EqualTo(0));
        }

        [Test]
        public void CooldownFraction_ZeroWhenReady_OneAtStart()
        {
            var s = new JxSkillSlotState();
            s.SetMainCooldown(nowMs: 0, durationMs: 10000);
            Assert.That(JxSkillSlotState.CooldownFraction(s.Main, 0, 10000), Is.EqualTo(1f));
            Assert.That(JxSkillSlotState.CooldownFraction(s.Main, 5000, 10000), Is.EqualTo(0.5f));
            Assert.That(JxSkillSlotState.CooldownFraction(s.Main, 10000, 10000), Is.EqualTo(0f));
        }

        // ---- Persist (ACC ini snapshot round-trip) ----

        [Test]
        public void SaveLoadSnapshot_RoundTripsMainAndAux()
        {
            var s = new JxSkillSlotState();
            s.AssignMain(99, 4, "spr/main.spr");
            s.AssignAux(0, 11, 1, "spr/a.spr");
            s.AssignAux(7, 22, 2, "spr/b.spr");
            var snap = s.SaveSnapshot("TestChar");

            var s2 = new JxSkillSlotState();
            s2.LoadSnapshot("TestChar", snap);
            Assert.That(s2.Main.SkillId, Is.EqualTo(99));
            Assert.That(s2.Main.IconPath, Is.EqualTo("spr/main.spr"));
            Assert.That(s2.Aux(0).SkillId, Is.EqualTo(11));
            Assert.That(s2.Aux(7).SkillId, Is.EqualTo(22));
            // Unassigned aux stays empty.
            Assert.That(s2.Aux(3).SkillId, Is.EqualTo(0));
        }

        [Test]
        public void LoadSnapshot_RejectsInvalidSkillId()
        {
            var dict = new Dictionary<string, string>
            {
                { "skill_X.left", "5000" },  // > MAX_SKILL → ignored
            };
            var s = new JxSkillSlotState();
            s.LoadSnapshot("X", dict);
            Assert.That(s.Main.SkillId, Is.EqualTo(0));
        }

        // ---- Adapter: render ----

        private static VisualElement MakeBar()
        {
            var root = new VisualElement();
            var bar = new VisualElement { name = JxSkillBarAdapter.Names.Bar };
            var main = new VisualElement { name = JxSkillBarAdapter.Names.MainSlot };
            main.Add(new VisualElement { name = JxSkillBarAdapter.Names.Icon });
            main.Add(new VisualElement { name = JxSkillBarAdapter.Names.Cooldown });
            main.Add(new Label { name = JxSkillBarAdapter.Names.CdLabel });
            bar.Add(main);
            for (int i = 0; i < JxSkillSlotState.AuxiliarySlotCount; i++)
            {
                var aux = new VisualElement { name = JxSkillBarAdapter.Names.AuxSlotPrefix + i };
                aux.Add(new VisualElement { name = JxSkillBarAdapter.Names.Icon });
                aux.Add(new VisualElement { name = JxSkillBarAdapter.Names.Cooldown });
                aux.Add(new Label { name = JxSkillBarAdapter.Names.CdLabel });
                bar.Add(aux);
            }
            root.Add(bar);
            return root;
        }

        [Test]
        public void Adapter_Bind_ReturnsTrue_WhenBarPresent()
        {
            var adapter = new JxSkillBarAdapter(MakeBar(), new JxSkillSlotState(), new FakeBus());
            Assert.IsTrue(adapter.Bind());
        }

        [Test]
        public void Adapter_Render_EmptySlotGetsEmptyClass()
        {
            var root = MakeBar();
            var state = new JxSkillSlotState(); // all empty
            var adapter = new JxSkillBarAdapter(root, state, new FakeBus());
            adapter.Bind();
            var main = root.Q<VisualElement>(JxSkillBarAdapter.Names.MainSlot);
            Assert.IsTrue(main.ClassListContains(JxSkillBarAdapter.EmptyClass));
        }

        [Test]
        public void Adapter_Render_AssignedReadySlotGetsReadyClass()
        {
            var root = MakeBar();
            var state = new JxSkillSlotState();
            state.AssignAux(2, 50, 1, "spr/x.spr");
            var adapter = new JxSkillBarAdapter(root, state, new FakeBus());
            adapter.SetNow(0);
            adapter.Bind();
            var aux2 = root.Q<VisualElement>(JxSkillBarAdapter.Names.AuxSlotPrefix + 2);
            Assert.IsTrue(aux2.ClassListContains(JxSkillBarAdapter.ReadyClass));
            Assert.IsFalse(aux2.ClassListContains(JxSkillBarAdapter.EmptyClass));
        }

        [Test]
        public void Adapter_Render_CooldownSlotShowsCdLabelAndClass()
        {
            var root = MakeBar();
            var state = new JxSkillSlotState();
            state.AssignAux(1, 60, 1, "spr/y.spr");
            state.SetAuxCooldown(1, nowMs: 0, durationMs: 3500); // 3.5s
            var adapter = new JxSkillBarAdapter(root, state, new FakeBus());
            adapter.SetNow(0);
            adapter.Bind();
            var aux1 = root.Q<VisualElement>(JxSkillBarAdapter.Names.AuxSlotPrefix + 1);
            Assert.IsTrue(aux1.ClassListContains(JxSkillBarAdapter.CooldownClass));
            var cdLabel = aux1.Q<Label>(JxSkillBarAdapter.Names.CdLabel);
            Assert.That(cdLabel.text, Is.EqualTo("4s")); // ceil(3.5) = 4
        }

        [Test]
        public void Adapter_ClickAux_ReturnsFalseWhenEmptyOrCooldown()
        {
            var root = MakeBar();
            var state = new JxSkillSlotState();
            var adapter = new JxSkillBarAdapter(root, state, new FakeBus());
            adapter.SetNow(0);
            adapter.Bind();
            // Empty → false.
            Assert.IsFalse(adapter.ClickAux(0));
            // Assign + cooldown → false.
            state.AssignAux(0, 70, 1, "x");
            state.SetAuxCooldown(0, 0, 5000);
            Assert.IsFalse(adapter.ClickAux(0));
        }

        [Test]
        public void Adapter_ClickAux_ReturnsTrueWhenReady()
        {
            var root = MakeBar();
            var bus = new FakeBus();
            var state = new JxSkillSlotState();
            state.AssignAux(4, 80, 1, "x");
            var adapter = new JxSkillBarAdapter(root, state, bus);
            adapter.SetNow(0);
            adapter.Bind();
            Assert.IsTrue(adapter.ClickAux(4));
            Assert.That(bus.ActionRequests.Count, Is.EqualTo(1));
        }
    }
}
