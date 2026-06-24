// -----------------------------------------------------------------------------
// VLTK Mobile — JX Minimap EditMode tests (port of KuiMinMapVN.cpp)
// Verifies: 128x128 viewport, world→map texel (/16,/32) coord math from source,
// player-center offset, POI in-viewport cull, coord text "X,Y", click→open
// worldmap via command bus, close hides. Category: HudJxCocos.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxMinimapTests
    {
        private class FakeBus : IJxHudCommandBus
        {
            public List<JxHudPanel> PanelRequests = new();
            public List<JxHudAction> ActionRequests = new();
            public void PublishPanelRequested(JxHudPanel panel) => PanelRequests.Add(panel);
            public void PublishActionRequested(JxHudAction action) => ActionRequests.Add(action);
        }

        // ---- Port constants ----

        [Test]
        public void ViewportSize_Is128_PerSource()
        {
            Assert.That(JxMinimapState.ViewportSize, Is.EqualTo(128));
        }

        [Test]
        public void WorldToTexel_IsX16_Y32_PerSource()
        {
            // draw_: nNpcOffsetX = .../16, nNpcOffsetY = .../32.
            Assert.That(JxMinimapState.WorldToTexel.x, Is.EqualTo(16f));
            Assert.That(JxMinimapState.WorldToTexel.y, Is.EqualTo(32f));
        }

        // ---- Coord text ----

        [Test]
        public void CoordText_RendersIntXCommaY()
        {
            // setMpsPos → "X,Y" (int PC mps).
            var s = new JxMinimapState();
            s.SetPlayerPos(1234.6f, 789.1f);
            Assert.That(s.CoordText, Is.EqualTo("1235,789"));
        }

        // ---- World→map offset (origin-based) ----

        [Test]
        public void WorldToMapOffset_DividesByTexelFromOrigin()
        {
            var s = new JxMinimapState();
            s.SetMap("Tân Thủ Thôn", new Vector2(5120, 10240)); // origin = 512,1024 *10
            // world point 5120+160 , 10240+64  →  offset (160/16, 64/32) = (10, 2).
            var off = s.WorldToMapOffset(new Vector2(5280, 10304));
            Assert.That(off.x, Is.EqualTo(10f));
            Assert.That(off.y, Is.EqualTo(2f));
        }

        // ---- Player-center offset ----

        [Test]
        public void RelativeCenterOffset_PlayerIsZero()
        {
            var s = new JxMinimapState();
            s.SetMap("Map", new Vector2(0, 0));
            s.SetPlayerPos(1000, 1000);
            var off = s.RelativeCenterOffset(new Vector2(1000, 1000));
            Assert.That(off.x, Is.EqualTo(0f));
            Assert.That(off.y, Is.EqualTo(0f));
        }

        [Test]
        public void RelativeCenterOffset_NpcEastOfPlayer_IsPositiveX()
        {
            // NPC east (+X) of player → +X screen pixel offset.
            var s = new JxMinimapState();
            s.SetMap("Map", new Vector2(0, 0));
            s.SetPlayerPos(1000, 1000);
            // 80 world-X east → 80/16 = +5 px.
            var off = s.RelativeCenterOffset(new Vector2(1080, 1000));
            Assert.That(off.x, Is.EqualTo(5f));
        }

        [Test]
        public void RelativeCenterOffset_NpcNorthOfPlayer_IsPositiveScreenY()
        {
            // NPC north (+Y world, cocos up) → appears ABOVE player → +Y screen (Y lật).
            var s = new JxMinimapState();
            s.SetMap("Map", new Vector2(0, 0));
            s.SetPlayerPos(1000, 1000);
            // 64 world-Y north → -(-64)/32 = +2 px screen.
            var off = s.RelativeCenterOffset(new Vector2(1000, 1064));
            Assert.That(off.y, Is.EqualTo(2f));
        }

        // ---- Viewport cull ----

        [Test]
        public void IsInViewport_True_Within64pxHalf()
        {
            var s = new JxMinimapState();
            Assert.IsTrue(s.IsInViewport(new Vector2(63, 63)));
            Assert.IsTrue(s.IsInViewport(new Vector2(-64, 64)));
        }

        [Test]
        public void IsInViewport_False_Outside128()
        {
            var s = new JxMinimapState();
            Assert.IsFalse(s.IsInViewport(new Vector2(65, 0)));
            Assert.IsFalse(s.IsInViewport(new Vector2(0, -65)));
        }

        // ---- POI list ----

        [Test]
        public void SetPois_ReplacesPrevious()
        {
            var s = new JxMinimapState();
            s.SetPois(new[]
            {
                new JxMinimapPoi { WorldPos = Vector2.zero, Kind = JxMinimapPoiKind.Npc, Name = "NPC" }
            });
            Assert.That(s.Pois.Count, Is.EqualTo(1));
            s.SetPois(new[]
            {
                new JxMinimapPoi { WorldPos = Vector2.zero, Kind = JxMinimapPoiKind.Door },
                new JxMinimapPoi { WorldPos = Vector2.zero, Kind = JxMinimapPoiKind.Item },
            });
            Assert.That(s.Pois.Count, Is.EqualTo(2));
            Assert.IsFalse(s.Pois.Any(p => p.Kind == JxMinimapPoiKind.Npc));
        }

        // ---- Adapter: render + click ----

        private static VisualElement MakeTree()
        {
            var root = new VisualElement();
            var viewport = new VisualElement { name = JxMinimapAdapter.Names.Viewport };
            viewport.Add(new Label { name = JxMinimapAdapter.Names.Coord });
            viewport.Add(new Label { name = JxMinimapAdapter.Names.MapName });
            viewport.Add(new Button { name = JxMinimapAdapter.Names.Close });
            viewport.Add(new VisualElement { name = JxMinimapAdapter.Names.PoiLayer });
            viewport.Add(new VisualElement { name = JxMinimapAdapter.Names.PlayerDot });
            root.Add(viewport);
            return root;
        }

        [Test]
        public void Adapter_Bind_ReturnsTrue_WhenViewportPresent()
        {
            var root = MakeTree();
            var state = new JxMinimapState { IsOpen = true };
            var adapter = new JxMinimapAdapter(root, state, new FakeBus());
            Assert.IsTrue(adapter.Bind());
        }

        [Test]
        public void Adapter_Bind_ReturnsFalse_WhenViewportMissing()
        {
            var root = new VisualElement();
            var adapter = new JxMinimapAdapter(root, new JxMinimapState(), new FakeBus());
            Assert.IsFalse(adapter.Bind());
        }

        [Test]
        public void Adapter_Render_SetsCoordAndMapName()
        {
            var root = MakeTree();
            var state = new JxMinimapState { IsOpen = true };
            state.SetMap("Tân Thủ Thôn", new Vector2(0, 0));
            state.SetPlayerPos(500, 300);
            var adapter = new JxMinimapAdapter(root, state, new FakeBus());
            adapter.Bind();

            var coord = root.Q<Label>(JxMinimapAdapter.Names.Coord);
            var mapName = root.Q<Label>(JxMinimapAdapter.Names.MapName);
            Assert.That(coord.text, Is.EqualTo("500,300"));
            Assert.That(mapName.text, Is.EqualTo("Tân Thủ Thôn"));
        }

        [Test]
        public void Adapter_Render_HidesViewport_WhenClosed()
        {
            var root = MakeTree();
            var state = new JxMinimapState { IsOpen = false };
            var adapter = new JxMinimapAdapter(root, state, new FakeBus());
            adapter.Bind();
            var viewport = root.Q<VisualElement>(JxMinimapAdapter.Names.Viewport);
            Assert.That(viewport.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void Adapter_Render_CullsPoisOutsideViewport()
        {
            var root = MakeTree();
            var state = new JxMinimapState { IsOpen = true };
            state.SetMap("Map", new Vector2(0, 0));
            state.SetPlayerPos(1000, 1000);
            // Near NPC: 16 px east → inside.
            // Far NPC: 2000 px east (125 px) → outside (>64).
            state.SetPois(new[]
            {
                new JxMinimapPoi { WorldPos = new Vector2(1000 + 16*16, 1000), Kind = JxMinimapPoiKind.Npc },
                new JxMinimapPoi { WorldPos = new Vector2(1000 + 2000, 1000), Kind = JxMinimapPoiKind.Item },
            });
            var adapter = new JxMinimapAdapter(root, state, new FakeBus());
            adapter.Bind();

            var poiLayer = root.Q<VisualElement>(JxMinimapAdapter.Names.PoiLayer);
            // Only the in-viewport NPC survives cull.
            Assert.That(poiLayer.childCount, Is.EqualTo(1));
        }

        [Test]
        public void Adapter_ClickOpenWorldMap_PublishesWorldMap()
        {
            var root = MakeTree();
            var bus = new FakeBus();
            var adapter = new JxMinimapAdapter(root, new JxMinimapState { IsOpen = true }, bus);
            adapter.Bind();
            adapter.ClickOpenWorldMap();
            Assert.That(bus.PanelRequests, Does.Contain(JxHudPanel.WorldMap));
        }

        [Test]
        public void Adapter_Close_HidesAndClearsOpen()
        {
            var root = MakeTree();
            var state = new JxMinimapState { IsOpen = true };
            var adapter = new JxMinimapAdapter(root, state, new FakeBus());
            adapter.Bind();
            adapter.Close();
            Assert.IsFalse(state.IsOpen);
            var viewport = root.Q<VisualElement>(JxMinimapAdapter.Names.Viewport);
            Assert.That(viewport.style.display.value, Is.EqualTo(DisplayStyle.None));
        }
    }
}
