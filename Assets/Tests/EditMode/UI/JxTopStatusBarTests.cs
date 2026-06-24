// -----------------------------------------------------------------------------
// VLTK Mobile — JX Top Status Bar EditMode tests (port of KuiTopControlVN)
// Verifies the upRoleInfo() contract, fill-fraction clamp01, label texts, and
// the UI Toolkit adapter render. Category: HudJxCocos.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxTopStatusBarTests
    {
        // ---- State: upRoleInfo + fractions ----

        [Test]
        public void UpRoleInfo_Hp_SetsCurAndMax()
        {
            var s = new JxTopStatusBarState();
            s.UpRoleInfo(75, 100, JxTopStatusBarState.Kind.Hp);
            Assert.That(s.HpCurrent, Is.EqualTo(75));
            Assert.That(s.HpMax, Is.EqualTo(100));
        }

        [Test]
        public void HpFraction_ClampsBetween0And1()
        {
            var s = new JxTopStatusBarState();
            s.UpRoleInfo(75, 100, JxTopStatusBarState.Kind.Hp);
            Assert.That(s.HpFraction, Is.EqualTo(0.75f));
            s.UpRoleInfo(150, 100, JxTopStatusBarState.Kind.Hp);
            Assert.That(s.HpFraction, Is.EqualTo(1f)); // >1 -> 1 (source guard)
            s.UpRoleInfo(-5, 100, JxTopStatusBarState.Kind.Hp);
            Assert.That(s.HpFraction, Is.EqualTo(0f)); // <0 -> 0 (source guard)
        }

        [Test]
        public void Fraction_MaxZero_GuardsToZero_NotFullBar()
        {
            // max<=0 must NOT render a misleading full bar.
            Assert.That(JxTopStatusBarState.ClampFraction(50, 0), Is.EqualTo(0f));
            Assert.That(JxTopStatusBarState.ClampFraction(50, -1), Is.EqualTo(0f));
        }

        [Test]
        public void UpRoleInfo_AllKinds_BindCorrectly()
        {
            var s = new JxTopStatusBarState();
            s.UpRoleInfo(30, 60, JxTopStatusBarState.Kind.Mana);
            s.UpRoleInfo(20, 40, JxTopStatusBarState.Kind.Stamina);
            s.UpRoleInfo(5, 10, JxTopStatusBarState.Kind.Exp);
            Assert.That(s.ManaFraction, Is.EqualTo(0.5f));
            Assert.That(s.StaminaFraction, Is.EqualTo(0.5f));
            Assert.That(s.ExpFraction, Is.EqualTo(0.5f));
        }

        [Test]
        public void Level_Zero_RendersZeroText()
        {
            // Source: nMinVer > 0 ? number : RANK_WORLD_ZERO.
            var s = new JxTopStatusBarState();
            s.UpRoleInfo(0, 0, JxTopStatusBarState.Kind.Level);
            Assert.That(s.LevelText, Is.EqualTo(JxTopStatusBarState.LevelZeroText));
            s.UpRoleInfo(42, 0, JxTopStatusBarState.Kind.Level);
            Assert.That(s.LevelText, Is.EqualTo("42"));
        }

        [Test]
        public void ExpText_RendersPercentRounded()
        {
            // Source EXP label: "%%%0.0f" of (100*scale).
            var s = new JxTopStatusBarState();
            s.UpRoleInfo(1, 3, JxTopStatusBarState.Kind.Exp); // 33.3% -> "33%"
            Assert.That(s.ExpText, Is.EqualTo("33%"));
            s.UpRoleInfo(2, 3, JxTopStatusBarState.Kind.Exp); // 66.7% -> "67%"
            Assert.That(s.ExpText, Is.EqualTo("67%"));
        }

        [Test]
        public void ResourceText_RendersCurSlashMax()
        {
            var s = new JxTopStatusBarState();
            s.UpRoleInfo(75, 100, JxTopStatusBarState.Kind.Hp);
            Assert.That(s.HpText, Is.EqualTo("75/100"));
        }

        [Test]
        public void Name_Kind_BindsString()
        {
            var s = new JxTopStatusBarState();
            s.UpRoleInfo(0, 0, JxTopStatusBarState.Kind.Name, "Hiệp Sĩ");
            Assert.That(s.NameText, Is.EqualTo("Hiệp Sĩ"));
        }

        [Test]
        public void SetGender_DrivesAvatarSelection()
        {
            var s = new JxTopStatusBarState();
            Assert.That(s.IsFemale, Is.False);
            s.SetGender(true);
            Assert.That(s.IsFemale, Is.True);
        }

        [Test]
        public void Kind7_PaiStub_IsNoOp()
        {
            // kind 7 (PaiLabel) is a commented-out stub in source — must not throw.
            var s = new JxTopStatusBarState();
            Assert.DoesNotThrow(() => s.UpRoleInfo(5, 5, 7));
        }

        // ---- Adapter: render into a synthetic tree ----

        private static VisualElement MakeTree()
        {
            var root = new VisualElement();
            foreach (var n in new[] {
                JxTopStatusBarAdapter.Names.HpFill, JxTopStatusBarAdapter.Names.ManaFill,
                JxTopStatusBarAdapter.Names.StaminaFill, JxTopStatusBarAdapter.Names.ExpFill })
            {
                root.Add(new VisualElement { name = n });
            }
            foreach (var n in new[] {
                JxTopStatusBarAdapter.Names.HpText, JxTopStatusBarAdapter.Names.ManaText,
                JxTopStatusBarAdapter.Names.StaminaText, JxTopStatusBarAdapter.Names.ExpText,
                JxTopStatusBarAdapter.Names.LevelText, JxTopStatusBarAdapter.Names.RankText,
                JxTopStatusBarAdapter.Names.NameText })
            {
                root.Add(new Label { name = n });
            }
            root.Add(new VisualElement { name = JxTopStatusBarAdapter.Names.Avatar });
            return root;
        }

        [Test]
        public void Adapter_Render_SetsBarWidthAndLabels()
        {
            var state = new JxTopStatusBarState();
            state.UpRoleInfo(50, 100, JxTopStatusBarState.Kind.Hp);
            state.UpRoleInfo(1, 4, JxTopStatusBarState.Kind.Exp);
            state.UpRoleInfo(10, 0, JxTopStatusBarState.Kind.Level);
            state.UpRoleInfo(0, 0, JxTopStatusBarState.Kind.Name, "Test");

            var root = MakeTree();
            var adapter = new JxTopStatusBarAdapter(root, state);
            adapter.Bind();

            var hpFill = root.Q<VisualElement>(JxTopStatusBarAdapter.Names.HpFill);
            Assert.That(hpFill.style.width.value, Is.EqualTo(new Length(50f, LengthUnit.Percent)));

            var expText = root.Q<Label>(JxTopStatusBarAdapter.Names.ExpText);
            Assert.That(expText.text, Is.EqualTo("25%"));

            var levelText = root.Q<Label>(JxTopStatusBarAdapter.Names.LevelText);
            Assert.That(levelText.text, Is.EqualTo("10"));

            var nameText = root.Q<Label>(JxTopStatusBarAdapter.Names.NameText);
            Assert.That(nameText.text, Is.EqualTo("Test"));
        }

        [Test]
        public void Adapter_Render_TogglesAvatarGenderClass()
        {
            var state = new JxTopStatusBarState();
            state.SetGender(true);
            var root = MakeTree();
            var adapter = new JxTopStatusBarAdapter(root, state);
            adapter.Bind();

            var avatar = root.Q<VisualElement>(JxTopStatusBarAdapter.Names.Avatar);
            Assert.IsTrue(avatar.ClassListContains("jx-avatar-female"));
            Assert.IsFalse(avatar.ClassListContains("jx-avatar-male"));
        }
    }
}
