// -----------------------------------------------------------------------------
// VLTK Mobile — DeviceStatus vltkunity adapter
// Phase 1 port of vltkunity's DeviceStatus.prefab (recon §5). vltkunity's
// DeviceStatus.cs is an EMPTY STUB — the port must implement live-update logic.
// Displays Wifi icon, Battery icon, Time (HH:mm:ss), and RTT/ping (Xms) in a
// horizontal row, scaled 0.5, with a semi-transparent black background.
//
// Source values (recon §5b):
//   - hidden by default (m_IsActive: 0); localScale 0.5
//   - bg black a=0.157; padding 10/10/5/5; spacing 20; middle-center
//   - Wifi RawImage 60x60; Battery RawImage 80x75 scale 1/0.6/1
//   - Time "07:36:50", RTT "40ms"; font hysz.ttf size 40; green 0.0024/0.66/0
//
// EditMode testability: all system calls (DateTime.Now, SystemInfo,
// Application.internetReachability) are wrapped behind IDeviceStateProvider so
// tests inject fakes. The controller calls Tick() once per second (NOT per frame).
// -----------------------------------------------------------------------------

using System;
using UnityEngine.UIElements;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>
    /// Abstraction over device/time/network state so the adapter stays pure C#
    /// and EditMode-testable. Production implementation reads SystemInfo /
    /// Application / DateTime; tests inject fakes.
    /// </summary>
    public interface IDeviceStateProvider
    {
        /// <summary>Current wall-clock time (HH:mm:ss formatted by the adapter).</summary>
        DateTime Now { get; }
        /// <summary>Round-trip ping in ms, or negative when unreachable.</summary>
        int PingMs { get; }
        /// <summary>Battery level 0..1 (-1 when on AC / unknown).</summary>
        float BatteryLevel { get; }
        /// <summary>Whether the device currently has network reachability.</summary>
        bool InternetReachable { get; }
    }

    /// <summary>
    /// UI Toolkit adapter for the device-status strip (Wifi/Battery/Time/RTT).
    /// Pure C# (no MonoBehaviour) so EditMode tests construct it directly.
    /// </summary>
    public sealed class DeviceStatusVltkUnityAdapter : IDisposable
    {
        private readonly VisualElement _root;
        private readonly HudDataBridge _bridge;
        private readonly IHudCommandBus _bus;
        private readonly IDeviceStateProvider _device;

        private VisualElement _wifi;
        private VisualElement _battery;
        private Label _time;
        private Label _rtt;

        public int TickCount { get; private set; }

        public DeviceStatusVltkUnityAdapter(
            VisualElement root,
            HudDataBridge bridge,
            IHudCommandBus bus,
            IDeviceStateProvider device)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        public void Bind()
        {
            CacheElements();
            Tick();
        }

        private void CacheElements()
        {
            if (_root == null) return;
            _wifi = FindByName("DeviceWifi");
            _battery = FindByName("DeviceBattery");
            _time = FindLabel("DeviceTime");
            _rtt = FindLabel("DeviceRtt");
        }

        /// <summary>
        /// Refresh the device-status readouts. The controller calls this once per
        /// second (NOT every frame — avoids the vltkunity Update()-per-frame
        /// antipattern; recon §5a notes source stub is empty).
        /// </summary>
        public void Tick()
        {
            TickCount++;
            if (_time != null)
                _time.text = _device.Now.ToString("HH:mm:ss");
            if (_rtt != null)
                _rtt.text = _device.PingMs >= 0 ? $"{_device.PingMs}ms" : "--ms";
            if (_wifi != null)
                _wifi.style.display = _device.InternetReachable
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            if (_battery != null)
            {
                // Hide battery when on AC power / unknown (level < 0).
                _battery.style.display = _device.BatteryLevel >= 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
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

        public void Dispose() { /* no bridge subscription; nothing to detach */ }
    }

    /// <summary>
    /// Production <see cref="IDeviceStateProvider"/> reading Unity's
    /// <see cref="UnityEngine.SystemInfo"/> / <see cref="UnityEngine.Application"/> /
    /// <see cref="System.DateTime"/>. Kept separate so the adapter stays pure C#.
    /// </summary>
    public sealed class LiveDeviceStateProvider : IDeviceStateProvider
    {
        public System.DateTime Now => System.DateTime.Now;
        public int PingMs => -1; // Phase 1: no live ping probe; controller may override later
        public float BatteryLevel => UnityEngine.SystemInfo.batteryLevel;
        public bool InternetReachable =>
            UnityEngine.Application.internetReachability != UnityEngine.NetworkReachability.NotReachable;
    }
}
