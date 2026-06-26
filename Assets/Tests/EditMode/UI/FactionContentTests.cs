// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Faction popup tests (EditMode, Popup)
// Verifies BtnFaction content renders Vietnamese labels, computed totals, and
// is null-safe when FactionBonusService/data is unavailable (data file currently
// empty -> rows show a status line, totals zero).
// -----------------------------------------------------------------------------
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.Sandbox;
using VLTK.UI.Faction;
using VLTK.UI.Popup;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("Popup")]
    public class FactionContentTests
    {
        [Test]
        public void TitleVi_IsVietnamese()
        {
            var content = new FactionContent(null, 7, "Nga My", 30);
            Assert.AreEqual("Môn Phái", content.TitleVi);
        }

        [Test]
        public void ImplementsLayoutHint_ForPopupShell()
        {
            var content = new FactionContent(null, 7, "Nga My", 30);
            Assert.IsInstanceOf<IPopupLayoutHint>(content);
            var hint = (IPopupLayoutHint)content;
            Assert.AreEqual(460f, hint.Width);
            Assert.AreEqual(480f, hint.Height);
        }

        [Test]
        public void Build_ShowsHeaderTotalsAndTable()
        {
            var content = new FactionContent(null, 7, "Nga My", 30);
            var body = new VisualElement();
            content.Build(body);

            Assert.IsNotNull(body.Q("FactionHeader"));
            Assert.IsNotNull(body.Q("FactionTotals"));
            Assert.IsNotNull(body.Q("FactionTable"));
            // 4 totals: HP / MP / ATK / DEF
            var totals = body.Q("FactionTotals");
            Assert.AreEqual(4, totals.childCount);
        }

        [Test]
        public void Build_NullService_ShowsEmptyDataStatusRow()
        {
            var content = new FactionContent(null, 7, "Nga My", 30);
            var body = new VisualElement();
            content.Build(body);

            var rows = body.Q("FactionRowList");
            Assert.IsNotNull(rows);
            Assert.AreEqual(1, rows.childCount, "Null service should render a single status row");
            Assert.IsNotNull(rows[0].Q<Label>("FactionEmpty"));
        }

        [Test]
        public void Build_NullFactionName_FallsBackToFactionNameVi()
        {
            // When no explicit name is passed, the content falls back to
            // PartyService.FactionNameVi(factionId). Note this is the PC party
            // faction-id scheme (7 => "Cái Bang"), which differs from the
            // CombatFaction enum ordering — see FactionContent.cs note.
            var content = new FactionContent(null, 7, null, 1);
            var body = new VisualElement();
            content.Build(body);

            var header = body.Q<Label>("FactionHeader");
            Assert.IsNotNull(header);
            StringAssert.Contains(PartyService.FactionNameVi(7), header.text);
        }

        [Test]
        public void OnShow_RefreshesWithoutServices()
        {
            var content = new FactionContent(null, 7, "Nga My", 30);
            var body = new VisualElement();
            content.Build(body);

            Assert.DoesNotThrow(() => content.OnShow());
            Assert.IsNotNull(body.Q("FactionRowList"));
        }
    }
}
