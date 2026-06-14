// -----------------------------------------------------------------------------
// VLTK Mobile — WeatherService EditMode tests.
// Kiểm tra weather resolution theo map/giờ, host dispatch (particle + ambient
// SFX + fog/sky + UI notice), clear weather khi rời map.
// PC source: settings/weather/weather.ini + weather.txt + lua weather_cycle.
// PC surfaces: SetWeatherEffect, SetFogColor, SetSkyColor, SetAmbientSFX.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class WeatherServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IWeatherHost
        {
            public int ApplyCalls;
            public int SfxCalls;
            public int ClearCalls;
            public int FogCalls;
            public int SkyCalls;
            public int NoticeCalls;
            public int LogCalls;
            public int LastMapId;
            public int LastWeather;
            public int LastEffectId;
            public float LastProbability;
            public int LastOldWeather;

            public void ApplyWeatherEffect(int mapId, int weatherType, int effectId, float probability)
            {
                ApplyCalls++;
                LastMapId = mapId;
                LastWeather = weatherType;
                LastEffectId = effectId;
                LastProbability = probability;
            }
            public void PlayAmbientSFX(int mapId, int weatherType) { SfxCalls++; }
            public void ClearWeatherEffect(int mapId) { ClearCalls++; }
            public void SetFogColor(int mapId, int weatherType) { FogCalls++; }
            public void SetSkyColor(int mapId, int weatherType) { SkyCalls++; }
            public void ShowWeatherNotice(int mapId, int weatherType) { NoticeCalls++; }
            public void LogWeatherChange(int mapId, int oldWeather, int newWeather)
            {
                LogCalls++;
                LastOldWeather = oldWeather;
            }
        }

        private static PcWeatherRegistry BuildRegistry(params (int mapId, int weatherType, int startHour, int endHour, int probability)[] rows)
        {
            var reg = new PcWeatherRegistry();
            foreach (var r in rows)
            {
                reg.Register(new PcWeatherEntry
                {
                    mapId = r.mapId,
                    weatherType = r.weatherType,
                    startHour = r.startHour,
                    endHour = r.endHour,
                    probability = r.probability,
                    effectId = r.weatherType + 100,
                    nameRaw = $"weather{r.weatherType}",
                });
            }
            return reg;
        }

        // ── WeatherType enum ────────────────────────────────────────────────

        [Test]
        public void WeatherType_HasFiveConditions()
        {
            CollectionAssert.AreEquivalent(
                new[] { "Sunny", "Rain", "Snow", "Fog", "Storm" },
                System.Enum.GetNames(typeof(WeatherType)));
        }

        // ── Empty registry ──────────────────────────────────────────────────

        [Test]
        public void GetWeatherForMap_EmptyRegistry_ReturnsNull()
        {
            var svc = new WeatherService();
            Assert.IsNull(svc.GetWeatherForMap(1, 12));
        }

        [Test]
        public void GetAllWeatherForMap_EmptyRegistry_ReturnsEmpty()
        {
            var svc = new WeatherService();
            Assert.AreEqual(0, Count(svc.GetAllWeatherForMap(1)));
        }

        [Test]
        public void GetAllWeather_EmptyRegistry_ReturnsEmpty()
        {
            var svc = new WeatherService();
            Assert.AreEqual(0, Count(svc.GetAllWeather()));
        }

        // ── Registry-based lookups ──────────────────────────────────────────

        [Test]
        public void GetWeatherForMap_WithinHour_ReturnsEntry()
        {
            var reg = BuildRegistry((1, (int)WeatherType.Rain, 0, 23, 100));
            var svc = new WeatherService(reg);
            var e = svc.GetWeatherForMap(1, 12);
            Assert.IsNotNull(e);
            Assert.AreEqual((int)WeatherType.Rain, e.weatherType);
        }

        [Test]
        public void GetWeatherForMap_OutsideHour_ReturnsFallback()
        {
            // Single entry for map 1: 6-18 only. Hour 0 = outside, should fallback to first entry.
            var reg = BuildRegistry((1, (int)WeatherType.Sunny, 6, 18, 100));
            var svc = new WeatherService(reg);
            var e = svc.GetWeatherForMap(1, 0);
            Assert.IsNotNull(e);
            Assert.AreEqual((int)WeatherType.Sunny, e.weatherType);
        }

        [Test]
        public void GetWeatherForMap_DifferentMap_ReturnsNull()
        {
            var reg = BuildRegistry((1, (int)WeatherType.Rain, 0, 23, 100));
            var svc = new WeatherService(reg);
            Assert.IsNull(svc.GetWeatherForMap(99, 12));
        }

        [Test]
        public void GetAllWeatherForMap_MultipleRows()
        {
            var reg = BuildRegistry(
                (1, (int)WeatherType.Sunny, 6, 12, 50),
                (1, (int)WeatherType.Rain, 13, 18, 80),
                (2, (int)WeatherType.Snow, 0, 23, 100)
            );
            var svc = new WeatherService(reg);
            Assert.AreEqual(2, Count(svc.GetAllWeatherForMap(1)));
            Assert.AreEqual(1, Count(svc.GetAllWeatherForMap(2)));
        }

        // ── ResolveAndApply ─────────────────────────────────────────────────

        [Test]
        public void ResolveAndApply_ValidEntry_DispatchesAllCallbacks()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, (int)WeatherType.Rain, 0, 23, 75));
            var svc = new WeatherService(reg, host);
            var entry = svc.ResolveAndApply(1, 12);
            Assert.IsNotNull(entry);
            Assert.AreEqual((int)WeatherType.Rain, entry.weatherType);
            Assert.AreEqual(1, host.ApplyCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.FogCalls);
            Assert.AreEqual(1, host.SkyCalls);
            Assert.AreEqual(1, host.NoticeCalls);
            Assert.AreEqual(1, host.LogCalls);
        }

        [Test]
        public void ResolveAndApply_PassesCorrectArgs()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, (int)WeatherType.Snow, 0, 23, 60));
            var svc = new WeatherService(reg, host);
            svc.ResolveAndApply(1, 12);
            Assert.AreEqual(1, host.LastMapId);
            Assert.AreEqual((int)WeatherType.Snow, host.LastWeather);
            Assert.AreEqual(102, host.LastEffectId); // weatherType + 100
            Assert.AreEqual(60f, host.LastProbability);
        }

        [Test]
        public void ResolveAndApply_NoEntry_ReturnsNullNoDispatch()
        {
            var host = new FakeHost();
            var svc = new WeatherService(null, host);
            Assert.IsNull(svc.ResolveAndApply(1, 12));
            Assert.AreEqual(0, host.ApplyCalls);
        }

        [Test]
        public void ResolveAndApply_UpdatesLastApplied()
        {
            var reg = BuildRegistry((1, (int)WeatherType.Storm, 0, 23, 100));
            var svc = new WeatherService(reg);
            svc.ResolveAndApply(1, 12);
            Assert.AreEqual(1, svc.LastAppliedMapId);
            Assert.AreEqual((int)WeatherType.Storm, svc.LastAppliedWeather);
        }

        [Test]
        public void ResolveAndApply_FiresOnWeatherChangedEvent()
        {
            var reg = BuildRegistry((1, (int)WeatherType.Rain, 0, 23, 100));
            var svc = new WeatherService(reg);
            int fired = 0;
            int lastW = 0;
            svc.OnWeatherChanged += (m, w) => { fired++; lastW = w; };
            svc.ResolveAndApply(1, 12);
            Assert.AreEqual(1, fired);
            Assert.AreEqual((int)WeatherType.Rain, lastW);
        }

        [Test]
        public void ResolveAndApply_WithoutHost_DoesNotThrow()
        {
            var reg = BuildRegistry((1, (int)WeatherType.Rain, 0, 23, 100));
            var svc = new WeatherService(reg);
            Assert.DoesNotThrow(() => svc.ResolveAndApply(1, 12));
        }

        [Test]
        public void ResolveAndApply_LogPassesOldWeather()
        {
            var host = new FakeHost();
            var reg = BuildRegistry(
                (1, (int)WeatherType.Sunny, 0, 11, 100),
                (1, (int)WeatherType.Rain, 12, 23, 100)
            );
            var svc = new WeatherService(reg, host);
            svc.ResolveAndApply(1, 8);  // sunny first
            host.LastOldWeather = -999;
            svc.ResolveAndApply(1, 16); // rain second
            Assert.AreEqual((int)WeatherType.Sunny, host.LastOldWeather);
        }

        // ── ClearWeather ────────────────────────────────────────────────────

        [Test]
        public void ClearWeather_DispatchesToHost()
        {
            var host = new FakeHost();
            var svc = new WeatherService(null, host);
            svc.ClearWeather(1);
            Assert.AreEqual(1, host.ClearCalls);
        }

        [Test]
        public void ClearWeather_ResetsLastApplied()
        {
            var reg = BuildRegistry((1, (int)WeatherType.Rain, 0, 23, 100));
            var svc = new WeatherService(reg);
            svc.ResolveAndApply(1, 12);
            Assert.AreEqual(1, svc.LastAppliedMapId);
            svc.ClearWeather(1);
            Assert.AreEqual(-1, svc.LastAppliedMapId);
            Assert.AreEqual(-1, svc.LastAppliedWeather);
        }

        [Test]
        public void ClearWeather_WithoutHost_DoesNotThrow()
        {
            var svc = new WeatherService();
            Assert.DoesNotThrow(() => svc.ClearWeather(1));
        }

        // ── Registry attach lifecycle ──────────────────────────────────────

        [Test]
        public void AttachRegistry_FiresOnWeatherLoaded()
        {
            var svc = new WeatherService();
            int fired = 0;
            svc.OnWeatherLoaded += () => fired++;
            svc.AttachRegistry(BuildRegistry((1, (int)WeatherType.Sunny, 0, 23, 100)));
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void AttachRegistry_NullRegistry_FiresOnWeatherLoaded()
        {
            var svc = new WeatherService();
            int fired = 0;
            svc.OnWeatherLoaded += () => fired++;
            svc.AttachRegistry(null);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachHost_ReplacesHost()
        {
            var host1 = new FakeHost();
            var host2 = new FakeHost();
            var reg = BuildRegistry((1, (int)WeatherType.Rain, 0, 23, 100));
            var svc = new WeatherService(reg, host1);
            svc.AttachHost(host2);
            svc.ResolveAndApply(1, 12);
            Assert.AreEqual(0, host1.ApplyCalls);
            Assert.AreEqual(1, host2.ApplyCalls);
        }

        // ── Helper ──────────────────────────────────────────────────────────

        private static int Count<T>(System.Collections.Generic.IEnumerable<T> e)
        {
            int n = 0;
            foreach (var _ in e) n++;
            return n;
        }
    }
}
