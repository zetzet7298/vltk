using NUnit.Framework;
using VLTK.UI;

namespace VLTK.Tests.UI
{
    [TestFixture]
    [Category("HUD")]
    public class HudCommandBusTests
    {
        [Test]
        public void PublishScreenshotRequested_InvokesSubscriber()
        {
            var bus = new HudCommandBus();
            int hits = 0;
            bus.OnScreenshotRequested += () => hits++;

            bus.PublishScreenshotRequested();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void PublishProfileRequested_InvokesSubscriber()
        {
            var bus = new HudCommandBus();
            int hits = 0;
            bus.OnProfileRequested += () => hits++;

            bus.PublishProfileRequested();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void PublishMinimapMarkerRequested_InvokesSubscriber()
        {
            var bus = new HudCommandBus();
            int hits = 0;
            bus.OnMinimapMarkerRequested += () => hits++;

            bus.PublishMinimapMarkerRequested();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void PublishToggleMapSizeRequested_InvokesSubscriber()
        {
            var bus = new HudCommandBus();
            int hits = 0;
            bus.OnToggleMapSizeRequested += () => hits++;

            bus.PublishToggleMapSizeRequested();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void PublishWorldMapRequested_InvokesSubscriber()
        {
            var bus = new HudCommandBus();
            int hits = 0;
            bus.OnWorldMapRequested += () => hits++;

            bus.PublishWorldMapRequested();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void PublishCaveMapRequested_InvokesSubscriber()
        {
            var bus = new HudCommandBus();
            int hits = 0;
            bus.OnCaveMapRequested += () => hits++;

            bus.PublishCaveMapRequested();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void Unsubscribe_StopsReceivingEvents()
        {
            var bus = new HudCommandBus();
            int hits = 0;
            System.Action handler = () => hits++;
            bus.OnScreenshotRequested += handler;
            bus.PublishScreenshotRequested();
            bus.OnScreenshotRequested -= handler;
            bus.PublishScreenshotRequested();

            Assert.AreEqual(1, hits);
        }

        [Test]
        public void ClearAllSubscribers_RemovesEveryHandler()
        {
            var bus = new HudCommandBus();
            int hits = 0;
            bus.OnScreenshotRequested += () => hits++;
            bus.OnProfileRequested += () => hits++;
            bus.OnWorldMapRequested += () => hits++;

            bus.ClearAllSubscribers();
            bus.PublishScreenshotRequested();
            bus.PublishProfileRequested();
            bus.PublishWorldMapRequested();

            Assert.AreEqual(0, hits);
        }

        [Test]
        public void MultipleSubscribers_AllReceive()
        {
            var bus = new HudCommandBus();
            int a = 0, b = 0;
            bus.OnScreenshotRequested += () => a++;
            bus.OnScreenshotRequested += () => b++;

            bus.PublishScreenshotRequested();

            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
        }
    }
}
