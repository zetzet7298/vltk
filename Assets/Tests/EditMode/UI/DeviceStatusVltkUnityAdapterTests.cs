using System;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.UI
{
    [TestFixture]
    [Category("HUD")]
    public class DeviceStatusVltkUnityAdapterTests
    {
        private VisualElement _root;
        private VisualElement _wifi, _battery;
        private Label _time, _rtt;
        private HudDataBridge _bridge;
        private HudCommandBus _bus;
        private FakeDevice _device;
        private DeviceStatusVltkUnityAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement { name = "GameHud" };
            _wifi = new VisualElement { name = "DeviceWifi" };
            _battery = new VisualElement { name = "DeviceBattery" };
            _time = new Label { name = "DeviceTime" };
            _rtt = new Label { name = "DeviceRtt" };

            _root.Add(_wifi);
            _root.Add(_battery);
            _root.Add(_time);
            _root.Add(_rtt);

            _bridge = new HudDataBridge(new DeviceRuntime(), false);
            _bus = new HudCommandBus();
            _device = new FakeDevice();
            _adapter = new DeviceStatusVltkUnityAdapter(_root, _bridge, _bus, _device);
            _adapter.Bind();
        }

        [TearDown]
        public void TearDown() => _adapter?.Dispose();

        [Test]
        public void Constructor_NullRoot_Throws()
            => Assert.Throws<ArgumentNullException>(() => new DeviceStatusVltkUnityAdapter(null, _bridge, _bus, _device));

        [Test]
        public void Constructor_NullBridge_Throws()
            => Assert.Throws<ArgumentNullException>(() => new DeviceStatusVltkUnityAdapter(_root, null, _bus, _device));

        [Test]
        public void Constructor_NullBus_Throws()
            => Assert.Throws<ArgumentNullException>(() => new DeviceStatusVltkUnityAdapter(_root, _bridge, null, _device));

        [Test]
        public void Constructor_NullDevice_Throws()
            => Assert.Throws<ArgumentNullException>(() => new DeviceStatusVltkUnityAdapter(_root, _bridge, _bus, null));

        [Test]
        public void Tick_SetsTimeToHHmmss()
        {
            _device.Now = new DateTime(2026, 6, 23, 7, 36, 50);
            _adapter.Tick();
            // recon §5b: placeholder "07:36:50"
            Assert.AreEqual("07:36:50", _time.text);
        }

        [Test]
        public void Tick_SetsRttToMilliseconds()
        {
            _device.PingMs = 40;
            _adapter.Tick();
            // recon §5b: placeholder "40ms"
            Assert.AreEqual("40ms", _rtt.text);
        }

        [Test]
        public void Tick_UnreachableShowsDashRtt()
        {
            _device.PingMs = -1;
            _adapter.Tick();
            Assert.AreEqual("--ms", _rtt.text);
        }

        [Test]
        public void Tick_InternetReachable_ShowsWifi()
        {
            _device.InternetReachable = true;
            _adapter.Tick();
            Assert.AreEqual(DisplayStyle.Flex, _wifi.style.display.value);
        }

        [Test]
        public void Tick_InternetUnreachable_HidesWifi()
        {
            _device.InternetReachable = false;
            _adapter.Tick();
            Assert.AreEqual(DisplayStyle.None, _wifi.style.display.value);
        }

        [Test]
        public void Tick_BatteryPresent_ShowsBattery()
        {
            _device.BatteryLevel = 0.8f;
            _adapter.Tick();
            Assert.AreEqual(DisplayStyle.Flex, _battery.style.display.value);
        }

        [Test]
        public void Tick_OnAcPower_HidesBattery()
        {
            _device.BatteryLevel = -1f;
            _adapter.Tick();
            Assert.AreEqual(DisplayStyle.None, _battery.style.display.value);
        }

        [Test]
        public void Tick_IncrementsTickCount()
        {
            int before = _adapter.TickCount;
            _adapter.Tick();
            Assert.AreEqual(before + 1, _adapter.TickCount);
        }

        private sealed class FakeDevice : IDeviceStateProvider
        {
            public DateTime Now { get; set; } = new DateTime(2026, 6, 23, 0, 0, 0);
            public int PingMs { get; set; } = 40;
            public float BatteryLevel { get; set; } = 1f;
            public bool InternetReachable { get; set; } = true;
        }

        private sealed class DeviceRuntime : IRuntimeStateProvider
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
