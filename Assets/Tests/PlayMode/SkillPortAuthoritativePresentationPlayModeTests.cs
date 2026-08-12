using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.Sprites;

namespace VLTK.Tests.PlayMode
{
    [Category("SkillPort")]
    public class SkillPortAuthoritativePresentationPlayModeTests
    {
        [UnityTest]
        public IEnumerator ServerOwnedMissile_RemainsStableAcrossFrameUntilLifecycleUpdate()
        {
            SkillCatalog catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var service = new SkillEffectVisualService(new SprRuntimeService(), catalog);
            SkillDefinition skill = catalog.Resolve(117);
            ActiveSkillEffect effect = service.SpawnAuthoritativeMissile(
                "playmode-missile-1",
                skill,
                new Vector2(10f, 20f),
                new Vector2(200f, 220f),
                20);

            Assert.NotNull(effect);
            Vector2 spawnPosition = effect.currentMissilePos;
            yield return null;

            service.Update(Mathf.Max(Time.deltaTime, 1f / 18f));
            Assert.AreEqual(spawnPosition, effect.currentMissilePos);
            Assert.AreEqual(SkillEffectPhase.Missile, effect.phase);

            Vector2 serverPosition = new Vector2(30f, 40f);
            Assert.IsTrue(service.UpdateAuthoritativeMissile(
                "playmode-missile-1",
                serverPosition,
                playFlightSound: false));
            Assert.AreEqual(serverPosition, effect.currentMissilePos);
            Assert.IsTrue(service.VanishAuthoritativeMissile("playmode-missile-1"));
            Assert.AreEqual(0, service.ActiveEffectCount);
        }
    }
}
