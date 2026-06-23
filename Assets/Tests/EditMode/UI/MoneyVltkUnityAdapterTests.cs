using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.UI
{
    [TestFixture]
    [Category("HUD")]
    public class MoneyVltkUnityAdapterTests
    {
        private VisualElement _root;
        private Label _copperAmount, _goldAmount, _silverAmount;
        private VisualElement _copperAddBtn, _goldAddBtn, _silverAddBtn;
        private HudDataBridge _bridge;
        private HudCommandBus _bus;
        private MoneyVltkUnityAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement { name = "GameHud" };
            _copperAmount = new Label { name = "MoneyCopperAmount" };
            _goldAmount = new Label { name = "MoneyGoldAmount" };
            _silverAmount = new Label { name = "MoneySilverAmount" };
            _copperAddBtn = new VisualElement { name = "MoneyCopperAddBtn" };
            _goldAddBtn = new VisualElement { name = "MoneyGoldAddBtn" };
            _silverAddBtn = new VisualElement { name = "MoneySilverAddBtn" };

            _root.Add(_copperAmount);
            _root.Add(_goldAmount);
            _root.Add(_silverAmount);
            _root.Add(_copperAddBtn);
            _root.Add(_goldAddBtn);
            _root.Add(_silverAddBtn);

            _bridge = new HudDataBridge(new MoneyRuntime(), false);
            _bus = new HudCommandBus();
            _adapter = new MoneyVltkUnityAdapter(_root, _bridge, _bus);
            _adapter.Bind();
        }

        [TearDown]
        public void TearDown() => _adapter?.Dispose();

        [Test]
        public void Constructor_NullRoot_Throws()
            => Assert.Throws<System.ArgumentNullException>(() => new MoneyVltkUnityAdapter(null, _bridge, _bus));

        [Test]
        public void Constructor_NullBridge_Throws()
            => Assert.Throws<System.ArgumentNullException>(() => new MoneyVltkUnityAdapter(_root, null, _bus));

        [Test]
        public void Constructor_NullBus_Throws()
            => Assert.Throws<System.ArgumentNullException>(() => new MoneyVltkUnityAdapter(_root, _bridge, null));

        [Test]
        public void SetAmounts_UpdatesThreeLabels()
        {
            _adapter.SetAmounts(100, 5, 151160);

            Assert.AreEqual("100", _copperAmount.text);
            Assert.AreEqual("5", _goldAmount.text);
            Assert.AreEqual("151160", _silverAmount.text);
        }

        [Test]
        public void Apply_Snapshot_UpdatesAmountsFromCurrency()
        {
            _adapter.Apply(new HudSnapshot
            {
                valid = true,
                copper = 250,
                gold = 3,
                silver = 99000,
            });

            Assert.AreEqual("250", _copperAmount.text);
            Assert.AreEqual("3", _goldAmount.text);
            Assert.AreEqual("99000", _silverAmount.text);
        }

        [Test]
        public void Apply_InvalidSnapshot_DoesNotChangeLabels()
        {
            _adapter.SetAmounts(1, 2, 3);
            _adapter.Apply(new HudSnapshot { valid = false });
            Assert.AreEqual("1", _copperAmount.text);
        }

        [Test]
        public void CopperAddClick_PublishesRechargeRequestedCopper()
        {
            CurrencyType captured = CurrencyType.Silver;
            int hits = 0;
            _bus.OnRechargeRequested += type => { captured = type; hits++; };

            _adapter.SimulateCopperAddClick();

            Assert.AreEqual(1, hits);
            Assert.AreEqual(CurrencyType.Copper, captured);
        }

        [Test]
        public void GoldAddClick_PublishesRechargeRequestedGold()
        {
            CurrencyType captured = (CurrencyType)(-1);
            _bus.OnRechargeRequested += type => captured = type;

            _adapter.SimulateGoldAddClick();

            Assert.AreEqual(CurrencyType.Gold, captured);
        }

        [Test]
        public void SilverAddClick_PublishesRechargeRequestedSilver()
        {
            CurrencyType captured = (CurrencyType)(-1);
            _bus.OnRechargeRequested += type => captured = type;

            _adapter.SimulateSilverAddClick();

            Assert.AreEqual(CurrencyType.Silver, captured);
        }

        private sealed class MoneyRuntime : IRuntimeStateProvider
        {
            public bool HasActiveMap => true;
            public int ActiveMapId => 1;
            public string ActiveMapName => "Test";
            public VLTK.Model.MapDefinition ActiveMapDefinition => null;
            public UnityEngine.Vector2 PlayerWorldPosition => UnityEngine.Vector2.zero;
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
