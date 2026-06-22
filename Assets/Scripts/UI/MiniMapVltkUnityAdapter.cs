// -----------------------------------------------------------------------------
// VLTK Mobile — MiniMap vltkunity adapter
// Phase 1 port of vltkunity's MiniMap.cs. Drives the player dot, the scene
// name/position text, and forwards the four minimap button clicks through
// HudCommandBus. The static minimap background (Ba Ling Xian world map) is
// applied in GameHud.uss/uxml and is not swapped at runtime in Phase 1.
// No MainCanvas.instance singleton — the controller subscribes to the bus and
// opens the right panel.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public sealed class MiniMapVltkUnityAdapter : IDisposable
    {
        private readonly VisualElement _root;
        private readonly HudDataBridge _bridge;
        private readonly IHudCommandBus _bus;

        private VisualElement _content;
        private VisualElement _playerDot;
        private Label _sceneName;
        private Label _scenePos;
        private VisualElement _markerBtn;
        private VisualElement _toggleBtn;
        private VisualElement _worldMapBtn;
        private VisualElement _caveMapBtn;

        private int _lastMapId = int.MinValue;
        private Vector2 _lastPlayerPos;
        private bool _subscribed;

        public int BindCount { get; private set; }
        public int PlayerDotUpdateCount { get; private set; }
        public int SceneTextUpdateCount { get; private set; }
        public Vector2 LastDotPosition { get; private set; }

        public MiniMapVltkUnityAdapter(VisualElement root, HudDataBridge bridge, IHudCommandBus bus)
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
            _content = FindByName("MinimapContent");
            _playerDot = FindByName("PlayerDot");
            _sceneName = FindByName("SceneName") as Label;
            _scenePos = FindByName("ScenePos") as Label;
            _markerBtn = FindByName("MinimapMarkerBtn");
            _toggleBtn = FindByName("ToggleMapBtn");
            _worldMapBtn = FindByName("WorldMapBtn");
            _caveMapBtn = FindByName("CaveMapBtn");
            BindCount++;
        }

        private void WireButtons()
        {
            var bus = _bus;
            if (bus == null) return;
            _markerClick = bus.PublishMinimapMarkerRequested;
            _toggleClick = bus.PublishToggleMapSizeRequested;
            _worldMapClick = bus.PublishWorldMapRequested;
            _caveMapClick = bus.PublishCaveMapRequested;
            RegisterClick(_markerBtn, _markerClick);
            RegisterClick(_toggleBtn, _toggleClick);
            RegisterClick(_worldMapBtn, _worldMapClick);
            RegisterClick(_caveMapBtn, _caveMapClick);
        }

        private System.Action _markerClick, _toggleClick, _worldMapClick, _caveMapClick;

        // Test/QA hooks — invoke the wired button handlers directly without
        // depending on UI Toolkit event dispatch mechanics.
        public void SimulateMarkerClick() => _markerClick?.Invoke();
        public void SimulateToggleMapSizeClick() => _toggleClick?.Invoke();
        public void SimulateWorldMapClick() => _worldMapClick?.Invoke();
        public void SimulateCaveMapClick() => _caveMapClick?.Invoke();

        private static void RegisterClick(VisualElement target, Action handler)
        {
            if (target == null || handler == null) return;
            target.pickingMode = PickingMode.Position;
            target.RegisterCallback<ClickEvent>(_ => handler());
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

        private void OnSnapshotChanged(HudSnapshot snapshot) => Apply(snapshot);

        /// <summary>Push a snapshot through the adapter. Tests use this directly.</summary>
        public void Apply(HudSnapshot snapshot)
        {
            if (!snapshot.valid) return;
            ApplySceneText(snapshot);
            ApplyPlayerPosition(snapshot);
        }

        private void ApplySceneText(HudSnapshot snapshot)
        {
            bool mapChanged = snapshot.mapId != _lastMapId;
            _lastMapId = snapshot.mapId;
            if (_sceneName != null) _sceneName.text = snapshot.mapName ?? string.Empty;
            if (_scenePos != null)
                _scenePos.text = $"{(int)snapshot.playerPosition.x}:{(int)snapshot.playerPosition.y}";
            SceneTextUpdateCount++;
        }

        private void ApplyPlayerPosition(HudSnapshot snapshot)
        {
            if (_playerDot == null) return;
            if (snapshot.playerPosition == _lastPlayerPos) return;
            _lastPlayerPos = snapshot.playerPosition;
            _playerDot.style.left = snapshot.playerPosition.x;
            _playerDot.style.top = snapshot.playerPosition.y;
            LastDotPosition = snapshot.playerPosition;
            PlayerDotUpdateCount++;
        }

        public void Dispose() => Unsubscribe();
    }
}
