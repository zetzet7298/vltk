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
    public class TopBarVltkUnityAdapterTests
    {
        private VisualElement _root;
        private VisualElement _hpFill, _mpFill, _staminaFill, _expFill;
        private Label _levelText, _hpText, _mpText, _staminaText, _expText, _rankText;
        private HudDataBridge _bridge;
        private HudCommandBus _bus;
        private TopBarVltkUnityAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement { name = "GameHud" };
            _hpFill = new VisualElement { name = "HpBarFill" };
            _mpFill = new VisualElement { name = "MpBarFill" };
            _staminaFill = new VisualElement { name = "StaminaBarFill" };
            _expFill = new VisualElement { name = "ExpBarFill" };
            _levelText = new Label("1") { name = "LevelText" };
            _hpText = new Label { name = "HpText" };
            _mpText = new Label { name = "MpText" };
            _staminaText = new Label { name = "StaminaText" };
            _expText = new Label { name = "ExpText" };
            _rankText = new Label { name = "RankText" };

            _root.Add(_hpFill);
            _root.Add(_mpFill);
            _root.Add(_staminaFill);
            _root.Add(_expFill);
            _root.Add(_levelText);
            _root.Add(_hpText);
            _root.Add(_mpText);
            _root.Add(_staminaText);
            _root.Add(_expText);
            _root.Add(_rankText);

            _bridge = new HudDataBridge(new FullLifeRuntime(), false);
            _bus = new HudCommandBus();
            _adapter = new TopBarVltkUnityAdapter(_root, _bridge, _bus);
            _adapter.Bind();
        }

        [TearDown]
        public void TearDown() => _adapter?.Dispose();

        [Test]
        public void Constructor_NullRoot_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new TopBarVltkUnityAdapter(null, _bridge, _bus));
        }

        [Test]
        public void Constructor_NullBridge_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new TopBarVltkUnityAdapter(_root, null, _bus));
        }

        [Test]
        public void Constructor_NullBus_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new TopBarVltkUnityAdapter(_root, _bridge, null));
        }

        [Test]
        public void Apply_HalfLife_SetsHpBarWidthTo50Percent()
        {
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                currentLife = 50,
                maxLife = 100,
                lifeFraction = 0.5f,
                currentMana = 50,
                maxMana = 100,
                manaFraction = 0.5f,
                level = 12,
            });

            Assert.AreEqual(new Length(50f, LengthUnit.Percent), _hpFill.style.width.value);
            Assert.AreEqual(new Length(50f, LengthUnit.Percent), _mpFill.style.width.value);
            Assert.AreEqual("12", _levelText.text);
            Assert.AreEqual("50/100", _hpText.text);
        }

        [Test]
        public void Apply_ZeroLife_DoesNotDivideByZero()
        {
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                currentLife = 0,
                maxLife = 100,
                lifeFraction = 0f,
                currentMana = 0,
                maxMana = 100,
                manaFraction = 0f,
            });

            Assert.AreEqual(new Length(0f, LengthUnit.Percent), _hpFill.style.width.value);
        }

        [Test]
        public void Apply_InvalidSnapshot_DoesNotChangeElements()
        {
            int callsBefore = _adapter.UpdateCount;
            _adapter.Apply(new HudSnapshot { valid = false });
            Assert.AreEqual(callsBefore + 1, _adapter.UpdateCount);
            Assert.AreEqual("1", _levelText.text);
        }

        [Test]
        public void RequestProfile_PublishesToBus()
        {
            int hits = 0;
            _bus.OnProfileRequested += () => hits++;

            _adapter.RequestProfile();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void RequestScreenshot_PublishesToBus()
        {
            int hits = 0;
            _bus.OnScreenshotRequested += () => hits++;

            _adapter.RequestScreenshot();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void SnapshotChanged_FiresAdapterApply()
        {
            int callsBefore = _adapter.UpdateCount;
            _bridge.RefreshAndPublish();
            Assert.GreaterOrEqual(_adapter.UpdateCount, callsBefore);
        }

        [Test]
        public void Dispose_StopsApplyingSnapshots()
        {
            int before = _adapter.UpdateCount;
            _adapter.Dispose();
            _bridge.RefreshAndPublish();
            Assert.AreEqual(before, _adapter.UpdateCount);
        }

        private sealed class FullLifeRuntime : IRuntimeStateProvider
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
            public long PlayerExp => 0;
        }
    }
}
