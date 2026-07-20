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
        public void ImplementsPcSheetHints_ForPopupShell()
        {
            var content = new TeamContent(null);
            Assert.IsInstanceOf<IPopupLayoutHint>(content);
            Assert.AreEqual(PopupChromeKind.PcTeam, content.Chrome);
            Assert.AreEqual(340f, content.Width);
            Assert.AreEqual(229f, content.Height);
        }

        [Test]
        public void Build_CreatesPcSheetListsAndButtons()
        {
            var content = new TeamContent(null);
            var body = new VisualElement();
            content.Build(body);

            Assert.IsNotNull(body.Q("TeamPanel"));
            Assert.IsNotNull(body.Q("TeamMemberList"));
            Assert.IsNotNull(body.Q("TeamNearbyList"));
            Assert.IsNotNull(body.Q<Button>("Invite"));
            Assert.IsNotNull(body.Q<Button>("Kick"));
            Assert.IsNotNull(body.Q<Button>("Appoint"));
            Assert.IsNotNull(body.Q<Button>("Refresh"));
            Assert.IsNotNull(body.Q<Button>("Leave"));
            Assert.IsNotNull(body.Q<Button>("CloseTeam"));
            Assert.IsNotNull(body.Q<Button>("Close"));
        }

        [Test]
        public void Build_DisablesBackendButtonsButKeepsCloseEnabled()
        {
            var content = new TeamContent(null);
            var body = new VisualElement();
            content.Build(body);

            Assert.IsFalse(body.Q<Button>("Invite").enabledSelf);
            Assert.IsFalse(body.Q<Button>("Kick").enabledSelf);
            Assert.IsFalse(body.Q<Button>("Appoint").enabledSelf);
            Assert.IsFalse(body.Q<Button>("Refresh").enabledSelf);
            Assert.IsTrue(body.Q<Button>("Close").enabledSelf);
        }

        [Test]
        public void Build_NullParty_ShowsNotReadyRow()
        {
            var content = new TeamContent(null);
            var body = new VisualElement();
            content.Build(body);

            var roster = body.Q("TeamMemberList");
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
            Assert.IsNotNull(body.Q("TeamMemberList"));
        }
    }
}
