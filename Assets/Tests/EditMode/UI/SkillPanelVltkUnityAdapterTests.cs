// -----------------------------------------------------------------------------
// VLTK Mobile — Skill panel vltkunity adapter EditMode tests
// Phase 2 Commit 2c. Tests the SkillPanelVltkUnityAdapter with synthetic
// PcSkillPanelSnapshot and a fake ISkillCommandBus. Category: HUD.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HUD")]
    public class SkillPanelVltkUnityAdapterTests
    {
        private class FakeSkillBus : ISkillCommandBus
        {
            public event System.Action<int> OnSkillPageChanged;
            public event System.Action<int> OnSkillSelected;
            public event System.Action<int> OnSkillUpgradeRequested;
            public event System.Action OnSkillCloseRequested;

            public int PageChangeCount, SelectCount, UpgradeCount, CloseCount;
            public int LastPageId = -1, LastSkillId = -1, LastUpgradeSkillId = -1;

            public void PublishSkillPageChanged(int pageIndex) { PageChangeCount++; LastPageId = pageIndex; OnSkillPageChanged?.Invoke(pageIndex); }
            public void PublishSkillSelected(int skillId) { SelectCount++; LastSkillId = skillId; OnSkillSelected?.Invoke(skillId); }
            public void PublishSkillUpgradeRequested(int skillId) { UpgradeCount++; LastUpgradeSkillId = skillId; OnSkillUpgradeRequested?.Invoke(skillId); }
            public void PublishSkillCloseRequested() { CloseCount++; OnSkillCloseRequested?.Invoke(); }
        }

        private static PcSkillPanelSnapshot MakeSnapshot(int selectedId = 0)
        {
            var rows = new List<PcSkillPanelRow>
            {
                new PcSkillPanelRow(115, "Khí Huyết", 1, 20, 5, 10, true, "Tăng khí huyết", "Tăng thêm", "Có thể nâng"),
                new PcSkillPanelRow(117, "Phi Long Tại Thiên", 10, 20, 3, 8, false, "Sát thương lửa", "", "Chưa đủ điều kiện"),
            };
            return new PcSkillPanelSnapshot
            {
                playerLevel = 30,
                skillPoints = 5,
                selectedSkillId = selectedId,
                selectedRow = selectedId > 0 ? rows[0] : null,
                rows = rows,
            };
        }

        [Test]
        public void Apply_RendersSkillRows()
        {
            var bus = new FakeSkillBus();
            var root = new VisualElement();
            var list = new VisualElement { name = "VltkSkillList" };
            root.Add(list);

            var adapter = new SkillPanelVltkUnityAdapter(root, bus);
            adapter.Bind();
            adapter.Apply(MakeSnapshot());

            Assert.GreaterOrEqual(adapter.RenderCount, 1);
            Assert.AreEqual(2, list.childCount);
        }

        [Test]
        public void Apply_UpdatesDetailPanel()
        {
            var bus = new FakeSkillBus();
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkSkillList" });
            var detail = new VisualElement { name = "VltkSkillDetail" };
            var detailName = new Label { name = "VltkSkillDetailName" };
            var detailLevel = new Label { name = "VltkSkillDetailLevel" };
            var detailDesc = new Label { name = "VltkSkillDetailDesc" };
            detail.Add(detailName);
            detail.Add(detailLevel);
            detail.Add(detailDesc);
            root.Add(detail);

            var adapter = new SkillPanelVltkUnityAdapter(root, bus);
            adapter.Bind();
            adapter.Apply(MakeSnapshot(115));

            Assert.AreEqual("Khí Huyết", detailName.text);
            Assert.AreEqual("5 / 20", detailLevel.text);
        }

        [Test]
        public void SimulatePageSwitchClick_TogglesAndPublishes()
        {
            var bus = new FakeSkillBus();
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkSkillList" });
            root.Add(new VisualElement { name = "VltkSkillPageSwitchBtn" });

            var adapter = new SkillPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            Assert.IsFalse(adapter.IsPageTwo);
            adapter.SimulatePageSwitchClick();
            Assert.IsTrue(adapter.IsPageTwo);
            Assert.AreEqual(1, bus.PageChangeCount);
            Assert.AreEqual(1, bus.LastPageId);

            adapter.SimulatePageSwitchClick();
            Assert.IsFalse(adapter.IsPageTwo);
            Assert.AreEqual(2, bus.PageChangeCount);
            Assert.AreEqual(0, bus.LastPageId);
        }

        [Test]
        public void SimulateCloseClick_PublishesClose()
        {
            var bus = new FakeSkillBus();
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkSkillList" });
            root.Add(new VisualElement { name = "VltkSkillCloseBtn" });

            var adapter = new SkillPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            adapter.SimulateCloseClick();

            Assert.AreEqual(1, bus.CloseCount);
        }

        [Test]
        public void SimulateUpgradeClick_PublishesUpgrade()
        {
            var bus = new FakeSkillBus();
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkSkillList" });
            root.Add(new VisualElement { name = "VltkSkillUpgradeBtn" });

            var adapter = new SkillPanelVltkUnityAdapter(root, bus);
            adapter.Bind();
            adapter.Apply(MakeSnapshot(117));

            adapter.SimulateUpgradeClick();

            Assert.AreEqual(1, bus.UpgradeCount);
            Assert.AreEqual(117, bus.LastUpgradeSkillId);
        }

        [Test]
        public void SelectSkill_PublishesAndUpdatesDetail()
        {
            var bus = new FakeSkillBus();
            var root = new VisualElement();
            var list = new VisualElement { name = "VltkSkillList" };
            root.Add(list);
            var detail = new VisualElement { name = "VltkSkillDetail" };
            var detailName = new Label { name = "VltkSkillDetailName" };
            detail.Add(detailName);
            root.Add(detail);

            var adapter = new SkillPanelVltkUnityAdapter(root, bus);
            adapter.Bind();
            adapter.Apply(MakeSnapshot());

            adapter.SimulateSelectSkill(115);

            Assert.AreEqual(1, bus.SelectCount);
            Assert.AreEqual(115, bus.LastSkillId);
            Assert.AreEqual(115, adapter.SelectedSkillId);
            Assert.AreEqual("Khí Huyết", detailName.text);
        }

        [Test]
        public void Apply_NullSnapshotDoesNotCrash()
        {
            var bus = new FakeSkillBus();
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkSkillList" });

            var adapter = new SkillPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            Assert.DoesNotThrow(() => adapter.Apply(null));
        }

        [Test]
        public void Apply_EmptySnapshotClearsList()
        {
            var bus = new FakeSkillBus();
            var root = new VisualElement();
            var list = new VisualElement { name = "VltkSkillList" };
            root.Add(list);

            var adapter = new SkillPanelVltkUnityAdapter(root, bus);
            adapter.Bind();
            adapter.Apply(MakeSnapshot());
            Assert.AreEqual(2, list.childCount);

            var emptySnap = new PcSkillPanelSnapshot
            {
                rows = new List<PcSkillPanelRow>(),
            };
            adapter.Apply(emptySnap);

            Assert.AreEqual(0, list.childCount);
        }
    }
}
