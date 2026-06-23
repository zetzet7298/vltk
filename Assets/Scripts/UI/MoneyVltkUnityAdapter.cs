// -----------------------------------------------------------------------------
// VLTK Mobile — Money vltkunity adapter
// Phase 1 port of vltkunity's Money.prefab (recon §3). vltkunity has no Money.cs
// (prefab-only); this adapter mirrors the prefab layout: 3 currency rows
// (Copper/Gold/Silver), each = icon + amount text + add-button, in a horizontal
// layout (maps vltkunity HorizontalLayoutGroup + ContentSizeFitter).
//
// Source values (recon §3b):
//   - rows: tongqian.png (Đồng tiền/copper), jinbi.png (Vàng/gold),
//     yinliang.png (Bạc/silver); add button btn_plus.png
//   - text: UTM Cafeta #19.ttf, size 24, yellow 0.96/1/0.41, alignment Right
//   - placeholder amount: "151160"
// Art is applied via USS (Resources/WorldGameUI/money/...); this adapter drives
// the amount labels and wires the add buttons through HudCommandBus.
// -----------------------------------------------------------------------------

using System;
using UnityEngine.UIElements;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>
    /// UI Toolkit adapter for the currency (money) display. Pure C# (no
    /// MonoBehaviour) so EditMode tests construct it directly with a synthetic
    /// VisualElement tree. Vietnamese labels: Đồng tiền / Vàng / Bạc.
    /// </summary>
    public sealed class MoneyVltkUnityAdapter : IDisposable
    {
        private readonly VisualElement _root;
        private readonly HudDataBridge _bridge;
        private readonly IHudCommandBus _bus;

        private Label _copperAmount;
        private Label _goldAmount;
        private Label _silverAmount;
        private VisualElement _copperAddBtn;
        private VisualElement _goldAddBtn;
        private VisualElement _silverAddBtn;

        private bool _subscribed;

        public int UpdateCount { get; private set; }

        public MoneyVltkUnityAdapter(VisualElement root, HudDataBridge bridge, IHudCommandBus bus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public void Bind()
        {
            CacheElements();
            WireButtons();
            Subscribe();
            if (_bridge != null)
                Apply(_bridge.BuildSnapshot());
        }

        private void CacheElements()
        {
            if (_root == null) return;
            _copperAmount = FindLabel("MoneyCopperAmount");
            _goldAmount = FindLabel("MoneyGoldAmount");
            _silverAmount = FindLabel("MoneySilverAmount");
            _copperAddBtn = FindByName("MoneyCopperAddBtn");
            _goldAddBtn = FindByName("MoneyGoldAddBtn");
            _silverAddBtn = FindByName("MoneySilverAddBtn");
        }

        private void WireButtons()
        {
            var bus = _bus;
            if (bus == null) return;
            RegisterClick(_copperAddBtn, () => bus.PublishRechargeRequested(CurrencyType.Copper));
            RegisterClick(_goldAddBtn, () => bus.PublishRechargeRequested(CurrencyType.Gold));
            RegisterClick(_silverAddBtn, () => bus.PublishRechargeRequested(CurrencyType.Silver));
        }

        private void Subscribe()
        {
            if (_subscribed || _bridge == null) return;
            _bridge.SnapshotChanged += OnSnapshotChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _bridge == null) return;
            _bridge.SnapshotChanged -= OnSnapshotChanged;
            _subscribed = false;
        }

        private void OnSnapshotChanged(HudSnapshot snapshot) => Apply(snapshot);

        /// <summary>Push a snapshot through the adapter. Tests use this directly.</summary>
        public void Apply(HudSnapshot snapshot)
        {
            UpdateCount++;
            if (!snapshot.valid) return;
            SetAmounts(snapshot.copper, snapshot.gold, snapshot.silver);
        }

        /// <summary>
        /// Update the three currency amount labels directly (testable independent
        /// of the binding source). vltkunity prefab shows only placeholder text.
        /// </summary>
        public void SetAmounts(int copper, int gold, int silver)
        {
            if (_copperAmount != null) _copperAmount.text = copper.ToString();
            if (_goldAmount != null) _goldAmount.text = gold.ToString();
            if (_silverAmount != null) _silverAmount.text = silver.ToString();
        }

        // Test/QA hooks — invoke the wired add-button handlers directly.
        public void SimulateCopperAddClick() => _bus?.PublishRechargeRequested(CurrencyType.Copper);
        public void SimulateGoldAddClick() => _bus?.PublishRechargeRequested(CurrencyType.Gold);
        public void SimulateSilverAddClick() => _bus?.PublishRechargeRequested(CurrencyType.Silver);

        private VisualElement FindByName(string name)
        {
            if (_root == null) return null;
            var queue = new System.Collections.Generic.Queue<VisualElement>();
            queue.Enqueue(_root);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.name == name) return current;
                int childCount = current.childCount;
                for (int i = 0; i < childCount; i++)
                    queue.Enqueue(current[i]);
            }
            return null;
        }

        private Label FindLabel(string name) => FindByName(name) as Label;

        private static void RegisterClick(VisualElement target, Action handler)
        {
            if (target == null || handler == null) return;
            target.pickingMode = PickingMode.Position;
            target.RegisterCallback<ClickEvent>(_ => handler());
        }

        public void Dispose() => Unsubscribe();
    }
}
