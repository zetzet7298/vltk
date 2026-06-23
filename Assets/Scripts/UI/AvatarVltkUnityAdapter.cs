// -----------------------------------------------------------------------------
// VLTK Mobile — Avatar vltkunity adapter
// Phase 1 port of vltkunity's Avatar.prefab (recon §4). vltkunity has no Avatar.cs
// (prefab-only); this adapter mirrors the prefab: a green-circular frame
// (btn_greencircular.png) containing a portrait background (btn_welfare_bg.png),
// the portrait sprite (008.png, PreserveAspect), and a level number text
// (bottom-right, yellow UTM Cafeta).
//
// Source values (recon §4b):
//   - frame: btn_greencircular.png, 65x65
//   - portrait bg: btn_welfare_bg.png, inset -10/-10
//   - portrait: 008.png, inset -30/-30, PreserveAspect (scale-to-fit)
//   - level text: "93", UTM Cafeta #19.ttf size 20, yellow 0.96/1/0.41, bottom-right
// Art applied via USS (Resources/WorldGameUI/Bag/...); this adapter drives the
// level text from snapshot.level and exposes SetPortrait for runtime binding.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>
    /// UI Toolkit adapter for the player avatar (portrait + level). Pure C# (no
    /// MonoBehaviour) so EditMode tests construct it directly with a synthetic
    /// VisualElement tree.
    /// </summary>
    public sealed class AvatarVltkUnityAdapter : IDisposable
    {
        private readonly VisualElement _root;
        private readonly HudDataBridge _bridge;
        private readonly IHudCommandBus _bus;

        private VisualElement _frame;
        private VisualElement _portraitBg;
        private VisualElement _portrait;
        private Label _levelText;

        private bool _subscribed;

        public int UpdateCount { get; private set; }

        public AvatarVltkUnityAdapter(VisualElement root, HudDataBridge bridge, IHudCommandBus bus)
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
            _frame = FindByName("AvatarFrame");
            _portraitBg = FindByName("AvatarPortraitBg");
            _portrait = FindByName("AvatarPortrait");
            _levelText = FindLabel("AvatarLevelText");
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
            // Level text binds snapshot.level (recon §4b: placeholder "93").
            if (_levelText != null) _levelText.text = snapshot.level.ToString();
        }

        /// <summary>
        /// Set the portrait texture at runtime (recon A4: 008.png is a placeholder;
        /// real portrait comes from the player face/SPR). Applied as a background
        /// image with scale-to-fit (maps vltkunity Image PreserveAspect).
        /// </summary>
        public void SetPortrait(Texture2D texture)
        {
            if (_portrait == null || texture == null) return;
            _portrait.style.backgroundImage = new StyleBackground(texture);
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

        public void Dispose() => Unsubscribe();
    }
}
