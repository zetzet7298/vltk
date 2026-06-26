// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Team popup tests (EditMode, Popup)
// Verifies BtnTeam content uses PC-derived manifests (a05d7a2c) and Vietnamese
// labels, and is null-safe when PartyService is unavailable.
// -----------------------------------------------------------------------------
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI.Team;
using VLTK.UI.Popup;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("Popup")]
    public class TeamContentTests
    {
        [Test]
        public void TitleVi_IsVietnamese()
        {
            var content = new TeamContent(null);
            Assert.AreEqual("Đội", content.TitleVi);
        }

        [Test]
        public void ImplementsLayoutHint_ForPopupShell()
        {
            var content = new TeamContent(null);
            Assert.IsInstanceOf<IPopupLayoutHint>(content);
            var hint = (IPopupLayoutHint)content;
            Assert.AreEqual(480f, hint.Width);
            Assert.AreEqual(520f, hint.Height);
        }

        [Test]
        public void Build_CreatesRosterAndControlsSections()
        {
            var content = new TeamContent(null);
            var body = new VisualElement();
            content.Build(body);

            Assert.IsNotNull(body.Q("TeamRoster"));
            Assert.IsNotNull(body.Q("TeamControls"));
        }

        [Test]
        public void Build_PopulatesPcControlManifestRows()
        {
            var content = new TeamContent(null);
            var body = new VisualElement();
            content.Build(body);

            var controls = body.Q("TeamControlList");
            Assert.IsNotNull(controls);
            Assert.AreEqual(8, controls.childCount, "PC a05d7a2c has 8 team controls");
            Assert.AreEqual("Mời vào đội", controls[0].Q<Label>("ControlLabel").text);
            Assert.AreEqual("a05d7a2c / Invite", controls[0].Q<Label>("ControlSource").text);
        }

        [Test]
        public void Build_NullParty_ShowsNotReadyRow()
        {
            var content = new TeamContent(null);
            var body = new VisualElement();
            content.Build(body);

            var roster = body.Q("TeamRosterList");
            Assert.IsNotNull(roster);
            Assert.GreaterOrEqual(roster.childCount, 1, "Null party should still render a status row");
        }

        [Test]
        public void OnShow_RefreshesWithoutServices()
        {
            var content = new TeamContent(null);
            var body = new VisualElement();
            content.Build(body);

            Assert.DoesNotThrow(() => content.OnShow());
            Assert.IsNotNull(body.Q("TeamRosterList"));
        }
    }
}
