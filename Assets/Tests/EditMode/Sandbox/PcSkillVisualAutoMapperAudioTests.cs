using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("TangMen")]
    public sealed class PcSkillVisualAutoMapperAudioTests
    {
        private const string FlightPath = @"\sound\skill\飘雪穿云.wav";
        private static string StreamingAssetsPath => Path.Combine(Directory.GetCurrentDirectory(), "Assets", "StreamingAssets");

        [Test]
        public void Event352_UsesOwnMissileFlightAudio_AndKeepsOwnEmptyCollisionSlot()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(
                includeWuDang: false, includeShaolin: false, includeTangMen: true,
                includeEMei: false, includeTianWang: false, includeWuDu: false,
                includeCuiYan: false, includeTianRen: false, includeKunLun: false);
            var skill = catalog.Resolve(352);
            var mapper = new PcSkillVisualAutoMapper();
            mapper.Initialize(StreamingAssetsPath);
            var config = mapper.GetVisualConfig(skill);

            Assert.AreEqual(162, config.missileId, "visuals stay on canonical child missile");
            Assert.AreEqual(FlightPath, config.flightSoundPath, "event missile 352 SndFile2/MS_DoFly wins");
            Assert.IsTrue(string.IsNullOrEmpty(config.impactSoundPath),
                "event missile 352 SndFile4 is empty; do not fall back to child collision audio");
        }

        [Test]
        public void Event352_FlightPath_ReachesSkillVisualSoundCallback()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(
                includeWuDang: false, includeShaolin: false, includeTangMen: true,
                includeEMei: false, includeTianWang: false, includeWuDu: false,
                includeCuiYan: false, includeTianRen: false, includeKunLun: false);
            var service = new SkillEffectVisualService(null, catalog);
            var sounds = new List<string>();
            service.OnCastSound = sounds.Add;
            var effect = service.PlaySkillCast(catalog.Resolve(352), Vector2.zero, Vector2.right * 64f, 1);

            sounds.Clear();
            service.Update(effect.preCastDuration);
            CollectionAssert.AreEqual(new[] { FlightPath }, sounds,
                "MS_DoFly dispatches canonical event-352 SndFile2 once; no device playback asserted");
        }
    }
}
