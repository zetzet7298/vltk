// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests cho Weather, Music, Guild Workshop, Huo Yeu Du,
// City Defence, Activity services. Vietnamese test descriptions.
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class WeatherServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = WeatherService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            }, "WeatherService phải load được dù thư mục data có/không");
        }

        [Test]
        public void GetWeatherForMap_ReturnsForKnownMap()
        {
            var reg = new PcWeatherRegistry();
            reg.Register(new PcWeatherEntry
            {
                mapId = 42,
                weatherType = 1, // mưa
                startHour = 0,
                endHour = 23,
                probability = 100,
                effectId = 7,
                nameRaw = "Rain42",
            });
            reg.Register(new PcWeatherEntry
            {
                mapId = 42,
                weatherType = 0,
                startHour = 0,
                endHour = 6,
                probability = 50,
                nameRaw = "Sun42",
            });
            var svc = new WeatherService(reg);
            var e = svc.GetWeatherForMap(42, 12);
            Assert.IsNotNull(e, "Map 42 hour 12 phải có entry thời tiết");
            Assert.AreEqual(42, e.mapId);
            // hour 03 — chỉ Sun42 (0..6) khớp, Rain42 cũng khớp (0..23); trả về match đầu tiên
            var night = svc.GetWeatherForMap(42, 3);
            Assert.IsNotNull(night);
        }

        [Test]
        public void GetAllWeatherForMap_Filters()
        {
            var reg = new PcWeatherRegistry();
            reg.Register(new PcWeatherEntry { mapId = 1, weatherType = 0, startHour = 0, endHour = 23 });
            reg.Register(new PcWeatherEntry { mapId = 1, weatherType = 1, startHour = 0, endHour = 23 });
            reg.Register(new PcWeatherEntry { mapId = 2, weatherType = 2, startHour = 0, endHour = 23 });
            var svc = new WeatherService(reg);
            var list = svc.GetAllWeatherForMap(1);
            Assert.AreEqual(2, list.Count);
        }
    }

    public class MusicServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = MusicService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void GetByScene_FiltersCorrectly()
        {
            var reg = new PcMusicRegistry();
            reg.Register(new PcMusicEntry { trackId = 1, filePath = "a.mp3", sceneType = 0, volume = 80 });
            reg.Register(new PcMusicEntry { trackId = 2, filePath = "b.mp3", sceneType = 1, volume = 80 });
            reg.Register(new PcMusicEntry { trackId = 3, filePath = "c.mp3", sceneType = 2, volume = 80 });
            reg.Register(new PcMusicEntry { trackId = 4, filePath = "d.mp3", sceneType = 0, volume = 90 });
            var svc = new MusicService(reg);
            var city = svc.GetByScene(0);
            Assert.AreEqual(2, city.Count);
            foreach (var t in city) Assert.AreEqual(0, t.sceneType);
        }
    }

    public class GuildWorkshopServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = GuildWorkshopService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            var reg = new PcGuildWorkshopRegistry();
            reg.Register(new PcGuildWorkshopEntry { level = 1, workshopType = 0, upgradeCost = 100 });
            reg.Register(new PcGuildWorkshopEntry { level = 1, workshopType = 1, upgradeCost = 200 });
            reg.Register(new PcGuildWorkshopEntry { level = 2, workshopType = 0, upgradeCost = 300 });
            var svc = new GuildWorkshopService(reg);
            var store = svc.GetByType(0);
            Assert.AreEqual(2, store.Count);
            foreach (var e in store) Assert.AreEqual(0, e.workshopType);
        }
    }

    public class HuoYueDuServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = HuoYueDuService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            var reg = new PcHuoYueDuRegistry();
            reg.Register(new PcHuoYueDuEntry { activityId = 1, type = 0, nameRaw = "BOSS", dailyLimit = 1 });
            reg.Register(new PcHuoYueDuEntry { activityId = 2, type = 0, nameRaw = "Boss2", dailyLimit = 1 });
            reg.Register(new PcHuoYueDuEntry { activityId = 3, type = 1, nameRaw = "Tống Kim", dailyLimit = 1 });
            var svc = new HuoYueDuService(reg);
            var boss = svc.GetByType(0);
            Assert.AreEqual(2, boss.Count);
            foreach (var e in boss) Assert.AreEqual(0, e.type);
        }
    }

    public class CityDefenceServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = CityDefenceService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void GetDefence_ReturnsNullForInvalid()
        {
            var svc = new CityDefenceService(new PcCityDefenceRegistry());
            var list = svc.GetDefence(999_999);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count, "Map không tồn tại → list rỗng");
        }

        [Test]
        public void TriggerWave_FiresEvent()
        {
            var svc = new CityDefenceService(new PcCityDefenceRegistry());
            int fired = 0;
            svc.OnWaveTriggered += (m, w) => fired++;
            svc.TriggerWave(100, 1);
            Assert.AreEqual(1, fired);
        }
    }

    public class ActivityServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = ActivityService.LoadFromStreamingAssets();
                Assert.IsNotNull(svc);
            });
        }

        [Test]
        public void GetActiveAtHour_FiltersCorrectly()
        {
            var reg = new PcActivityRegistry();
            // open=8, close=22 (ban ngày)
            reg.Register(new PcActivityEntry { activityId = 1, nameRaw = "Daytime", type = 0, openHour = 8, closeHour = 22 });
            // open=22, close=6 (qua đêm)
            reg.Register(new PcActivityEntry { activityId = 2, nameRaw = "Overnight", type = 0, openHour = 22, closeHour = 6 });
            // open=close=0 (luôn mở)
            reg.Register(new PcActivityEntry { activityId = 3, nameRaw = "Always", type = 0, openHour = 0, closeHour = 0 });
            var svc = new ActivityService(reg);

            var noon = svc.GetActiveAtHour(12);
            Assert.AreEqual(2, noon.Count); // Daytime + Always
            foreach (var a in noon) Assert.That(a.activityId, Is.EqualTo(1).Or.EqualTo(3));

            var midnight = svc.GetActiveAtHour(1);
            // Overnight (22..6) + Always → 2
            Assert.AreEqual(2, midnight.Count);
        }
    }
}
