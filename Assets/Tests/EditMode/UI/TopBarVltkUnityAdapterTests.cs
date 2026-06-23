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

        // ── T1: stamina bar must bind stamina, NOT life ──────────────────────

        [Test]
        public void Apply_HalfStamina_BindsStaminaBarWidthTo50Percent()
        {
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                currentLife = 100, maxLife = 100, lifeFraction = 1f,
                currentMana = 100, maxMana = 100, manaFraction = 1f,
                currentStamina = 50, maxStamina = 100, staminaFraction = 0.5f,
                currentExp = 0, maxExp = 1000, expFraction = 0f,
            });

            Assert.AreEqual(new Length(50f, LengthUnit.Percent), _staminaFill.style.width.value);
            Assert.AreEqual("50/100", _staminaText.text);
        }

        [Test]
        public void Apply_StaminaText_ShowsCurrentSlashMaxStamina_NotLife()
        {
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                currentLife = 80, maxLife = 100, lifeFraction = 0.8f,
                currentMana = 100, maxMana = 100, manaFraction = 1f,
                currentStamina = 30, maxStamina = 60, staminaFraction = 0.5f,
                currentExp = 0, maxExp = 1000, expFraction = 0f,
            });

            // Must be stamina (30/60), NOT life (80/100).
            Assert.AreEqual("30/60", _staminaText.text);
            Assert.AreNotEqual(_hpText.text, _staminaText.text);
        }

        // ── T3: MP bar must use maxMana from snapshot, not hardcoded 100 ──────

        [Test]
        public void Apply_MpBar_UsesRealMaxManaFromSnapshot()
        {
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                currentLife = 100, maxLife = 100, lifeFraction = 1f,
                currentMana = 150, maxMana = 300, manaFraction = 0.5f,
                currentStamina = 100, maxStamina = 100, staminaFraction = 1f,
                currentExp = 0, maxExp = 1000, expFraction = 0f,
            });

            Assert.AreEqual(new Length(50f, LengthUnit.Percent), _mpFill.style.width.value);
            Assert.AreEqual("150/300", _mpText.text);
        }

        // ── T4: EXP bar must use real expFraction, not the fudge ─────────────

        [Test]
        public void Apply_ExpBar_UsesRealExpFractionAndMaxExp()
        {
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                currentLife = 100, maxLife = 100, lifeFraction = 1f,
                currentMana = 100, maxMana = 100, manaFraction = 1f,
                currentStamina = 100, maxStamina = 100, staminaFraction = 1f,
                currentExp = 500, maxExp = 1000, expFraction = 0.5f,
            });

            Assert.AreEqual(new Length(50f, LengthUnit.Percent), _expFill.style.width.value);
            Assert.AreEqual("500/1000", _expText.text);
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
