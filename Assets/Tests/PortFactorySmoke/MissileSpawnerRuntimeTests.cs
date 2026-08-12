using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.PortFactorySmoke
{
    public class MissileSpawnerRuntimeTests
    {
        [Test]
        public void SpawnMissiles_WithValidPcMissileId_UsesPcSpeedAndLifetime()
        {
            // 1. Setup mock projectile service
            var pService = new ProjectileService();
            var spawner = new MissileSpawner(pService);

            // 2. Initialize PcMissileRegistry with a dummy streaming assets path or rely on existing init
            try { PcMissileRegistry.Initialize(Application.streamingAssetsPath); } catch {}

            // 3. Find a missile from registry
            PcMissileEntry pcMissile = null;
            for (int i = 1; i <= 500; i++)
            {
                if (PcMissileRegistry.TryGet(i, out var m) && m.speed > 0 && m.lifetime > 0)
                {
                    pcMissile = m;
                    break;
                }
            }
            if (pcMissile == null)
            {
                Assert.Ignore("No valid missile found in registry. Skip test.");
                return;
            }

            // 4. Create dummy skill pointing to this missile
            var skill = new SkillDefinition
            {
                skillId = 9999,
                childSkillId = pcMissile.missileId,
                missileForm = SkillMissileForm.Single
            };

            // 5. Spawn
            var origin = Vector2.zero;
            var target = new Vector2(100, 0);
            var spawned = spawner.SpawnMissiles(skill, origin, target, 1, 0f);

            Assert.IsNotNull(spawned);
            Assert.AreEqual(1, spawned.Count);
            
            var p = spawned[0];
            Assert.AreEqual(pcMissile.speed * 18f, p.speed, 0.01f, "Speed should match PC speed converted to pixels per second.");
            Assert.AreEqual(pcMissile.lifetime / 18f, p.duration, 0.01f, "Duration should match PC lifetime converted to seconds.");
        }
    }
}
