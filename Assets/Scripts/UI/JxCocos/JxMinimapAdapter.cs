// -----------------------------------------------------------------------------
// VLTK Mobile — JX Minimap rendering adapter (UI Toolkit, port KuiMinMapVN.cpp)
//
// Nguồn: KuiMinMapVN.cpp.
//  - Viewport 128x128 (clipper) ở góc trên-phải, player trung tâm, map scroll.
//  - Coord text "X,Y" (pMainPointLabel/ptestLabel), map name (pMapNameLabel).
//  - NPC/POI dots (DrawNode/_npcDrawNode) định vị theo RelativeCenterOffset.
//  - Click minimap → mở big map (KuiMaxMapVN). Close button → ẩn minimap.
//
// Thuần C# (không MonoBehaviour) — EditMode-testable. Mỗi element có tên cố định;
// USS đổi màu/icon POI. Public Click() coordinator cho event/test.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VLTK.UI.JxCocos
{
    /// <summary>Adapter UI Toolkit cho JX minimap.</summary>
    public sealed class JxMinimapAdapter
    {
        private readonly VisualElement _root;
        private readonly JxMinimapState _state;
        private readonly IJxHudCommandBus _bus;

        /// <summary>Element-name constants (stable for UXML + tests).</summary>
        public static class Names
        {
            public const string Viewport = "jx_minimap_viewport";
            public const string Coord = "jx_minimap_coord";
            public const string MapName = "jx_minimap_mapname";
            public const string Close = "jx_minimap_close";
            public const string PoiLayer = "jx_minimap_pois";
            public const string PlayerDot = "jx_minimap_player";
        }

        public JxMinimapAdapter(VisualElement root, JxMinimapState state, IJxHudCommandBus bus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <summary>Khám phá các element + gắn click handler. Trả về false nếu thiếu viewport.</summary>
        public bool Bind()
        {
            var viewport = Find(_root, Names.Viewport);
            if (viewport == null) return false;

            var close = Find(_root, Names.Close);
            if (close != null)
                close.RegisterCallback<ClickEvent>(_ => OnClose());

            // Click viewport (not on close) → mở big map.
            viewport.RegisterCallback<ClickEvent>(OnViewportClicked);
            Render();
            return true;
        }

        private void OnViewportClicked(EventBase evt)
        {
            // Close button đã handle riêng (sẽ stop propagation). Click trúng viewport
            // → mở big map (KuiMaxMapVN overlay) qua command bus.
            _bus?.PublishPanelRequested(JxHudPanel.WorldMap);
        }

        private void OnClose()
        {
            _state.IsOpen = false;
            Render();
        }

        /// <summary>Coordinator cho click viewport (event + test share 1 path).</summary>
        public void ClickOpenWorldMap()
        {
            _bus?.PublishPanelRequested(JxHudPanel.WorldMap);
        }

        /// <summary>Coordinator cho nút close (event + test share 1 path).</summary>
        public void Close()
        {
            _state.IsOpen = false;
            Render();
        }

        /// <summary>Render state → labels + POI dots. POI ngoài viewport bị ẩn.</summary>
        public void Render()
        {
            var viewport = Find(_root, Names.Viewport);
            if (viewport == null) return;

            // Visible state (isOpen).
            viewport.style.display = _state.IsOpen
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (!_state.IsOpen) return;

            // Coord text.
            var coord = Find(_root, Names.Coord) as Label;
            if (coord != null) coord.text = _state.CoordText;

            // Map name.
            var mapName = Find(_root, Names.MapName) as Label;
            if (mapName != null) mapName.text = _state.MapName;

            // Player dot at center (always present).
            var playerDot = Find(_root, Names.PlayerDot);
            if (playerDot != null)
            {
                playerDot.style.left = JxMinimapState.ViewportSize * 0.5f;
                playerDot.style.top = JxMinimapState.ViewportSize * 0.5f;
            }

            // POI dots: rebuild each render from state.Pois, vị trí theo RelativeCenterOffset.
            var poiLayer = Find(_root, Names.PoiLayer);
            if (poiLayer != null)
            {
                poiLayer.Clear();
                foreach (var poi in _state.Pois)
                {
                    var offset = _state.RelativeCenterOffset(poi.WorldPos);
                    if (!_state.IsInViewport(offset)) continue; // ngoài 128x128 → skip
                    var dot = new VisualElement { name = "jx_minimap_poi" };
                    dot.AddToClassList(JxMinimapPoiStyles.ClassFor(poi.Kind));
                    // left/top measured from viewport top-left; center is 64,64.
                    dot.style.left = JxMinimapState.ViewportSize * 0.5f + offset.x;
                    dot.style.top = JxMinimapState.ViewportSize * 0.5f - offset.y;
                    poiLayer.Add(dot);
                }
            }
        }

        private static VisualElement Find(VisualElement root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            var q = new Queue<VisualElement>();
            q.Enqueue(root);
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur.name == name) return cur;
                int n = cur.childCount;
                for (int i = 0; i < n; i++) q.Enqueue(cur[i]);
            }
            return null;
        }
    }
}
