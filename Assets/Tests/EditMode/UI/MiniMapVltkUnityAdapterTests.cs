using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.UI
{
    [TestFixture]
    [Category("HUD")]
    public class MiniMapVltkUnityAdapterTests
    {
        private VisualElement _root;
        private VisualElement _minimapContent;
        private VisualElement _playerDot;
        private Label _sceneName;
        private Label _scenePos;
        private VisualElement _markerBtn, _toggleBtn, _worldMapBtn, _caveMapBtn;
        private HudDataBridge _bridge;
        private HudCommandBus _bus;
        private MiniMapVltkUnityAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement { name = "GameHud" };
            _minimapContent = new VisualElement { name = "MinimapContent" };
            _playerDot = new VisualElement { name = "PlayerDot" };
            _sceneName = new Label { name = "SceneName" };
            _scenePos = new Label { name = "ScenePos" };
            _markerBtn = new VisualElement { name = "MinimapMarkerBtn" };
            _toggleBtn = new VisualElement { name = "ToggleMapBtn" };
            _worldMapBtn = new VisualElement { name = "WorldMapBtn" };
            _caveMapBtn = new VisualElement { name = "CaveMapBtn" };

            _root.Add(_minimapContent);
            _root.Add(_playerDot);
            _root.Add(_sceneName);
            _root.Add(_scenePos);
            _root.Add(_markerBtn);
            _root.Add(_toggleBtn);
            _root.Add(_worldMapBtn);
            _root.Add(_caveMapBtn);

            _bridge = new HudDataBridge(new StaticRuntime(), false);
            _bus = new HudCommandBus();
            _adapter = new MiniMapVltkUnityAdapter(_root, _bridge, _bus);
            _adapter.Bind();
        }

        [TearDown]
        public void TearDown() => _adapter?.Dispose();

        [Test]
        public void Constructor_NullRoot_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new MiniMapVltkUnityAdapter(null, _bridge, _bus));
        }

        [Test]
        public void Bind_CachesAllElements()
        {
            Assert.AreEqual(1, _adapter.BindCount);
        }

        [Test]
        public void Apply_ChangesPlayerPosition_UpdatesDot()
        {
            int before = _adapter.PlayerDotUpdateCount;
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                mapId = 1,
                mapName = "Ba Ling",
                playerPosition = new Vector2(123, 456),
            });
            Assert.AreEqual(before + 1, _adapter.PlayerDotUpdateCount);
            // M1: projected (xx, yy) = (left/16 + xRatio, yRatio - top/16)
            Assert.AreEqual(new Vector2(123f / 16f, -456f / 16f), _adapter.LastDotPosition);
        }

        [Test]
        public void Apply_SamePlayerPosition_DoesNotUpdateAgain()
        {
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                mapId = 1,
                playerPosition = new Vector2(50, 50),
            });
            int afterFirst = _adapter.PlayerDotUpdateCount;
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                mapId = 1,
                playerPosition = new Vector2(50, 50),
            });
            Assert.AreEqual(afterFirst, _adapter.PlayerDotUpdateCount);
        }

        [Test]
        public void Apply_SetsSceneNameAndPosText()
        {
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                mapId = 53,
                mapName = "Ba Ling",
                playerPosition = new Vector2(100, 200),
            });
            Assert.AreEqual("Ba Ling", _sceneName.text);
            // M3: vltkunity order is "{top}:{left}" → playerPosition.y : playerPosition.x
            Assert.AreEqual("200:100", _scenePos.text);
        }

        [Test]
        public void MarkerBtnClick_PublishesMarkerRequested()
        {
            int hits = 0;
            _bus.OnMinimapMarkerRequested += () => hits++;

            _adapter.SimulateMarkerClick();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void ToggleBtnClick_PublishesToggleRequested()
        {
            int hits = 0;
            _bus.OnToggleMapSizeRequested += () => hits++;

            _adapter.SimulateToggleMapSizeClick();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void WorldMapBtnClick_PublishesWorldMapRequested()
        {
            int hits = 0;
            _bus.OnWorldMapRequested += () => hits++;

            _adapter.SimulateWorldMapClick();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void CaveMapBtnClick_PublishesCaveMapRequested()
        {
            int hits = 0;
            _bus.OnCaveMapRequested += () => hits++;

            _adapter.SimulateCaveMapClick();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void Dispose_StopsApplyingSnapshots()
        {
            int before = _adapter.PlayerDotUpdateCount;
            _adapter.Dispose();
            _bridge.RefreshAndPublish();
            Assert.AreEqual(before, _adapter.PlayerDotUpdateCount);
        }

        // ── M1: position formula parity with vltkunity ─────────────────────

        [Test]
        public void Apply_PlayerDot_UsesProjectedFormula_WithRatios()
        {
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                mapId = 1,
                playerPosition = new Vector2(160, 320), // left=160, top=320
                miniMapXRatio = 10f,
                miniMapYRatio = 200f,
            });

            // xx = left/16 + xRatio = 10 + 10 = 20
            // yy = yRatio - top/16 = 200 - 20 = 180
            Assert.AreEqual(new Vector2(20f, 180f), _adapter.LastDotPosition);
        }

        [Test]
        public void Apply_PlayerDot_DividesBy16fTileScale()
        {
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                mapId = 1,
                playerPosition = new Vector2(16, 16),
                miniMapXRatio = 0f,
                miniMapYRatio = 0f,
            });

            // xx = 16/16 = 1, yy = 0 - 16/16 = -1
            Assert.AreEqual(new Vector2(1f, -1f), _adapter.LastDotPosition);
        }

        private sealed class StaticRuntime : IRuntimeStateProvider
        {
            public bool HasActiveMap => true;
            public int ActiveMapId => 1;
            public string ActiveMapName => "Test";
            public MapDefinition ActiveMapDefinition => null;
            public Vector2 PlayerWorldPosition => Vector2.zero;
            public int PlayerLevel => 1;
            public int PlayerCurrentLife => 100;
            public int PlayerMaxLife => 100;
            public int PlayerCurrentMana => 100;
            public int PlayerMaxMana => 100;
            public int PlayerCurrentStamina => 100;
            public int PlayerMaxStamina => 100;
            public long PlayerExp => 0;
            public long PlayerMaxExp => 1000;
            public float MiniMapXRatio => 0f;
            public float MiniMapYRatio => 0f;
            public int PlayerCopper => 0;
            public int PlayerGold => 0;
            public int PlayerSilver => 0;
        }
    }
}
