// -----------------------------------------------------------------------------
// VLTK Mobile — TopBar vltkunity adapter
// Phase 1 port of vltkunity's TopBar.cs. Renders HP/MP/SP/EXP bars and the
// player level text inside the existing UI Toolkit tree. Subscribes to
// HudDataBridge.SnapshotChanged (event-driven, no Update() polling) and
// publishes profile/screenshot intents via HudCommandBus so the controller
// can wire them without a MainCanvas.instance singleton.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>
    /// UI Toolkit adapter for the top status bar. Pure C# (no MonoBehaviour) so
    /// EditMode tests can construct it directly with a synthetic <see cref="UIDocument"/>.
    /// </summary>
    public sealed class TopBarVltkUnityAdapter : IDisposable
    {
        private readonly VisualElement _root;
        private readonly HudDataBridge _bridge;
        private readonly IHudCommandBus _bus;

        private VisualElement _hpFill;
        private VisualElement _mpFill;
        private VisualElement _staminaFill;
        private VisualElement _expFill;
        private Label _levelText;
        private Label _hpText;
        private Label _mpText;
        private Label _staminaText;
        private Label _expText;
        private Label _rankText;

        private HudProgressBar _hpBar;
        private HudProgressBar _mpBar;
        private HudProgressBar _staminaBar;
        private HudProgressBar _expBar;

        private bool _subscribed;

        public int UpdateCount { get; private set; }

        public TopBarVltkUnityAdapter(VisualElement root, HudDataBridge bridge, IHudCommandBus bus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public void Bind()
        {
            CacheElements();
            Subscribe();
            if (_bridge != null)
                Apply(_bridge.BuildSnapshot());
        }

        private void CacheElements()
        {
            if (_root == null) return;
            _hpFill = FindByName("HpBarFill");
            _mpFill = FindByName("MpBarFill");
            _staminaFill = FindByName("StaminaBarFill");
            _expFill = FindByName("ExpBarFill");
            _levelText = FindLabel("LevelText");
            _hpText = FindLabel("HpText");
            _mpText = FindLabel("MpText");
            _staminaText = FindLabel("StaminaText");
            _expText = FindLabel("ExpText");
            _rankText = FindLabel("RankText");

            // Wrap the cached fills/labels in HudProgressBar helpers (recon §6b, P1).
            _hpBar = new HudProgressBar(_hpFill, _hpText);
            _mpBar = new HudProgressBar(_mpFill, _mpText);
            _staminaBar = new HudProgressBar(_staminaFill, _staminaText);
            _expBar = new HudProgressBar(_expFill, _expText);
        }

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

            // vltkunity TopBar.cs binds HP/MP/SP from CurLife/MaxLife, CurInner/MaxInner,
            // CurStamina/MaxStamina (recon §2a). EXP is a mobile addition; use the
            // bridge-computed expFraction (real maxExp denominator), not a fudge.
            _hpBar.Set(snapshot.lifeFraction, snapshot.currentLife, snapshot.maxLife);
            _mpBar.Set(snapshot.manaFraction, snapshot.currentMana, snapshot.maxMana);
            // T1 FIX: stamina bar/text now bind to stamina (was lifeFraction).
            _staminaBar.Set(snapshot.staminaFraction, snapshot.currentStamina, snapshot.maxStamina);
            // T4 FIX: real expFraction from snapshot (was ComputeExpFraction fudge).
            _expBar.Set(snapshot.expFraction, (int)snapshot.currentExp, (int)snapshot.maxExp);

            if (_levelText != null) _levelText.text = snapshot.level.ToString();
        }

        public void RequestProfile()
        {
            _bus?.PublishProfileRequested();
        }

        public void RequestScreenshot()
        {
            _bus?.PublishScreenshotRequested();
        }

        public void Dispose() => Unsubscribe();
    }
}
