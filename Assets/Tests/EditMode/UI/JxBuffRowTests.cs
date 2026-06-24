// -----------------------------------------------------------------------------
// VLTK Mobile — JX Buff row EditMode tests (port of KuiStateSkillControlVN.cpp)
// Verifies: countdown text logic (Nh/Nm/Ns/N/A) with exact source boundaries,
// grid layout (10 cols), color constant, visibility, tick decay, render. Category: HudJxCocos.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxBuffRowTests
    {
        // ---- Port constants ----

        [Test]
        public void TicksPerSecond_Is18_PerSource()
        {
            // Nguồn: time = m_LeftTime / 18.
            Assert.That(JxBuffRowState.TicksPerSecond, Is.EqualTo(18));
        }

        [Test]
        public void GridColumns_Is10_PerSource()
        {
            // Nguồn: if (nCountX > 9) wrap.
            Assert.That(JxBuffRowState.GridColumns, Is.EqualTo(10));
        }

        [Test]
        public void Color_IsGreen_0_255_54_PerSource()
        {
            // Nguồn: ccc3(0, 255, 54).
            Assert.That(JxBuffRowState.CountdownColor.r, Is.EqualTo(0f / 255f));
            Assert.That(JxBuffRowState.CountdownColor.g, Is.EqualTo(255f / 255f));
            Assert.That(JxBuffRowState.CountdownColor.b, Is.EqualTo(54f / 255f));
        }

        // ---- Countdown text (port-critical, exact boundaries) ----

        [Test]
        public void Countdown_LessOrEqual18Ticks_IsNA()
        {
            // Nguồn: if (m_LeftTime <= 18) → "N/A".
            Assert.That(JxBuffRowState.CountdownText(0), Is.EqualTo("N/A"));
            Assert.That(JxBuffRowState.CountdownText(18), Is.EqualTo("N/A"));
        }

        [Test]
        public void Countdown_JustOver18_IsSeconds()
        {
            // leftTime=19 → time=1.05s → 1.05/60 = 0.017 (not >1) → "1s".
            Assert.That(JxBuffRowState.CountdownText(19), Is.EqualTo("1s"));
            // leftTime=18*60=1080 → time=60s → 60/60=1 (not >1) → "60s" (boundary!).
            Assert.That(JxBuffRowState.CountdownText(1080), Is.EqualTo("60s"));
        }

        [Test]
        public void Countdown_Over60Seconds_IsMinutes()
        {
            // leftTime=18*61=1098 → time=61s → 61/60=1.016 >1 → Nm=(int)61/60=1 → "1m".
            Assert.That(JxBuffRowState.CountdownText(1098), Is.EqualTo("1m"));
            // leftTime=18*120=2160 → time=120s → "2m".
            Assert.That(JxBuffRowState.CountdownText(2160), Is.EqualTo("2m"));
        }

        [Test]
        public void Countdown_Exactly3600Seconds_Shows60m_Not1h_BoundaryQuirk()
        {
            // leftTime=18*3600=64800 → time=3600s → 3600/3600=1 (not >1) → minutes branch
            // → 3600/60=60 >1 → "60m" (source boundary quirk: exactly 1h shows "60m").
            Assert.That(JxBuffRowState.CountdownText(64800), Is.EqualTo("60m"));
        }

        [Test]
        public void Countdown_Over1Hour_IsHours()
        {
            // leftTime=18*3601=64818 → time=3601s → 3601/3600=1.0002 >1 → "1h".
            Assert.That(JxBuffRowState.CountdownText(64818), Is.EqualTo("1h"));
            // leftTime=18*7200 → time=7200s → "2h".
            Assert.That(JxBuffRowState.CountdownText(18 * 7200), Is.EqualTo("2h"));
        }

        // ---- Grid ----

        [Test]
        public void GridCell_WrapsAtTenColumns()
        {
            Assert.That(JxBuffRowState.GridCell(0), Is.EqualTo((0, 0)));
            Assert.That(JxBuffRowState.GridCell(9), Is.EqualTo((9, 0)));
            Assert.That(JxBuffRowState.GridCell(10), Is.EqualTo((0, 1)));
            Assert.That(JxBuffRowState.GridCell(23), Is.EqualTo((3, 2)));
        }

        // ---- Visibility ----

        [Test]
        public void IsVisible_True_WhenOpenAndHasBuffs()
        {
            var s = new JxBuffRowState { IsOpen = true };
            Assert.IsFalse(s.IsVisible); // no buffs
            s.AddBuff(new JxBuff { SkillId = 1, LeftTime = 100 });
            Assert.IsTrue(s.IsVisible);
        }

        [Test]
        public void IsVisible_False_WhenClosed()
        {
            var s = new JxBuffRowState { IsOpen = false };
            s.AddBuff(new JxBuff { SkillId = 1, LeftTime = 100 });
            Assert.IsFalse(s.IsVisible);
        }

        // ---- Tick decay ----

        [Test]
        public void Tick_DecrementsLeftTimeAndRemovesExpired()
        {
            var s = new JxBuffRowState();
            s.AddBuff(new JxBuff { SkillId = 1, LeftTime = 5 });
            s.AddBuff(new JxBuff { SkillId = 2, LeftTime = 1 });
            s.Tick();
            Assert.That(s.Buffs.Count, Is.EqualTo(1));
            Assert.That(s.Buffs[0].SkillId, Is.EqualTo(1));
            Assert.That(s.Buffs[0].LeftTime, Is.EqualTo(4));
        }

        [Test]
        public void SetBuffs_ReplacesPrevious()
        {
            var s = new JxBuffRowState();
            s.AddBuff(new JxBuff { SkillId = 1, LeftTime = 10 });
            s.SetBuffs(new[]
            {
                new JxBuff { SkillId = 2, LeftTime = 20 },
                new JxBuff { SkillId = 3, LeftTime = 30 },
            });
            Assert.That(s.Buffs.Count, Is.EqualTo(2));
            Assert.That(s.Buffs.Any(b => b.SkillId == 1), Is.False);
        }

        // ---- Adapter: render ----

        private static VisualElement MakeTree()
        {
            var root = new VisualElement();
            root.Add(new VisualElement { name = JxBuffRowAdapter.Names.Layer });
            return root;
        }

        [Test]
        public void Adapter_Bind_ReturnsTrue_WhenLayerPresent()
        {
            var root = MakeTree();
            var adapter = new JxBuffRowAdapter(root, new JxBuffRowState());
            Assert.IsTrue(adapter.Bind());
        }

        [Test]
        public void Adapter_Bind_ReturnsFalse_WhenLayerMissing()
        {
            var adapter = new JxBuffRowAdapter(new VisualElement(), new JxBuffRowState());
            Assert.IsFalse(adapter.Bind());
        }

        [Test]
        public void Adapter_Render_HidesLayer_WhenNoBuffs()
        {
            var root = MakeTree();
            var adapter = new JxBuffRowAdapter(root, new JxBuffRowState { IsOpen = true });
            adapter.Bind();
            var layer = root.Q<VisualElement>(JxBuffRowAdapter.Names.Layer);
            Assert.That(layer.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void Adapter_Render_OneIconPerBuff_WithCountdownLabel()
        {
            var root = MakeTree();
            var state = new JxBuffRowState { IsOpen = true };
            state.AddBuff(new JxBuff { SkillId = 1, LeftTime = 1098 }); // "1m"
            state.AddBuff(new JxBuff { SkillId = 2, LeftTime = 19 });   // "1s"
            var adapter = new JxBuffRowAdapter(root, state);
            adapter.Bind();

            var layer = root.Q<VisualElement>(JxBuffRowAdapter.Names.Layer);
            var icons = layer.Children().Where(c => c.name == JxBuffRowAdapter.Names.Icon).ToList();
            Assert.That(icons.Count, Is.EqualTo(2));

            // First buff label = "1m".
            var label0 = icons[0].Q<Label>(JxBuffRowAdapter.Names.Time);
            Assert.That(label0.text, Is.EqualTo("1m"));
            // Second buff label = "1s".
            var label1 = icons[1].Q<Label>(JxBuffRowAdapter.Names.Time);
            Assert.That(label1.text, Is.EqualTo("1s"));

            // Layer visible.
            Assert.That(layer.style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [Test]
        public void Adapter_Render_GridPosition_SpacingX26Y36()
        {
            // Icon 0 → (0,0); icon 10 → col0 row1 (y += 36).
            var root = MakeTree();
            var state = new JxBuffRowState { IsOpen = true };
            for (int i = 0; i < 11; i++)
                state.AddBuff(new JxBuff { SkillId = i + 1, LeftTime = 100 });
            var adapter = new JxBuffRowAdapter(root, state);
            adapter.Bind();

            var layer = root.Q<VisualElement>(JxBuffRowAdapter.Names.Layer);
            var icons = layer.Children().Where(c => c.name == JxBuffRowAdapter.Names.Icon).ToList();

            // icon[0] at x=StartOffsetX(13).
            Assert.That(icons[0].style.left.value.value, Is.EqualTo(13f));
            // icon[1] at x=13+26=39.
            Assert.That(icons[1].style.left.value.value, Is.EqualTo(39f));
            // icon[10] at row1: x=13, y=StartOffsetYFromTop(87) + 36 = 123.
            Assert.That(icons[10].style.left.value.value, Is.EqualTo(13f));
            Assert.That(icons[10].style.top.value.value, Is.EqualTo(87f + 36f));
        }
    }
}
