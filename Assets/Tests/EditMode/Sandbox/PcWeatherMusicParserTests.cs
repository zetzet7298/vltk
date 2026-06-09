using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcWeatherMusicParserTests
    {
        private static string IndexDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcWeatherMusic");

        [Test]
        public void ParseFile_LoadsPcWeatherAndMusicSourceIndex()
        {
            var rows = PcWeatherMusicParser.ParseFile(Path.Combine(IndexDir, "weather_music_index.txt"));

            Assert.AreEqual(3, rows.Count);
            Assert.IsTrue(rows.TrueForAll(r => r.clientServerByteIdentical));
            Assert.AreEqual(1624, rows[0].bytes);
            Assert.AreEqual("e0b3738b63dd8114847e2a28e01071b20758da3a377a1861a5344f54ffebd8cf", rows[0].sha256);
        }

        [Test]
        public void Registry_ExposesWeatherAndMusicCounts()
        {
            var registry = PcWeatherMusicParser.BuildRegistry(IndexDir);
            var weather = registry.GetByKey("weather");
            var musicFight = registry.GetByKey("musicfight");
            var musicSet = registry.GetByKey("musicset");

            Assert.IsNotNull(weather);
            Assert.AreEqual("weather.ini", weather.fileName);
            Assert.AreEqual(94, weather.lineCount);
            Assert.AreEqual(8, weather.sectionCount);
            Assert.AreEqual(57, weather.dataRowCount);

            Assert.IsNotNull(musicFight);
            Assert.AreEqual(322, musicFight.lineCount);
            Assert.AreEqual(16, musicFight.sectionCount);
            Assert.AreEqual(255, musicFight.dataRowCount);

            Assert.IsNotNull(musicSet);
            Assert.AreEqual(250, musicSet.lineCount);
            Assert.AreEqual(0, musicSet.sectionCount);
            Assert.AreEqual(249, musicSet.dataRowCount);
        }

        [Test]
        public void Service_LoadsCommittedWeatherMusicIndexOnly()
        {
            var service = WeatherMusicIndexService.LoadFromDirectory(IndexDir);

            Assert.AreEqual(3, service.Count);
            Assert.AreEqual(3, service.ClientServerIdenticalCount);
            Assert.AreEqual(15527, service.TotalBytes);
            Assert.AreEqual(561, service.TotalDataRows);
            Assert.AreEqual("musicset", service.GetByFileName("musicset.txt").key);
            Assert.IsTrue(WeatherMusicIndexService.NoRuntimeClaim.Contains("no runtime"));
        }
    }
}
