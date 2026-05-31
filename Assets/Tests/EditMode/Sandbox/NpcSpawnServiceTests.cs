using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M3.2 — NPC Spawn in Sandbox tests. Toggle spawns/despawns placeholders (AC#1),
    /// resolves a decoded sprite per template (AC#2), surfaces source ids for the
    /// inspector (AC#3), and despawns without reloading (AC#4).
    /// </summary>
    public class NpcSpawnServiceTests
    {
        private NpcTemplateRegistry MakeRegistry(bool withSprite)
        {
            var reg = new NpcTemplateRegistry();
            var t = new NpcTemplate
            {
                templateId = 7,
                nameNormalized = "Guard",
                spriteClipRef = withSprite ? "atlas/guard" : null,
                spriteResolved = withSprite,
            };
            reg.Register(t);
            return reg;
        }

        private RegionSpawnManifest MakeManifest(params int[] templateIds)
        {
            var m = new RegionSpawnManifest { mapId = 1, regionX = 0, regionY = 0 };
            int idx = 0;
            foreach (var tid in templateIds)
            {
                m.npcSpawns.Add(new NpcSpawn
                {
                    spawnIndex = idx++,
                    templateId = tid,
                    scriptRef = tid == 7 ? "scripts/guard.lua" : null,
                    posX = 10 + tid,
                    posY = 20 + tid,
                });
            }
            m.totalNpcs = m.npcSpawns.Count;
            return m;
        }

        // --- AC#1: toggle on spawns placeholders ---

        [Test]
        public void ToggleNpcs_On_SpawnsFromManifest()
        {
            var svc = new NpcSpawnService(MakeRegistry(true));
            svc.ToggleNpcs(true, MakeManifest(7, 7));
            Assert.IsTrue(svc.NpcsVisible);
            Assert.AreEqual(2, svc.LiveCount);
        }

        [Test]
        public void SpawnFrom_NullManifest_NoSpawns()
        {
            var svc = new NpcSpawnService(MakeRegistry(true));
            svc.SpawnFrom(null);
            Assert.AreEqual(0, svc.LiveCount);
        }

        [Test]
        public void SpawnFrom_PositionsInstanceAtSpawnCoords()
        {
            var svc = new NpcSpawnService(MakeRegistry(true));
            svc.SpawnFrom(MakeManifest(7));
            var inst = svc.Live[0];
            Assert.AreEqual(17f, inst.worldPosition.x, 0.001f); // 10 + 7
            Assert.AreEqual(27f, inst.worldPosition.y, 0.001f); // 20 + 7
        }

        // --- AC#2: resolved sprite/template ---

        [Test]
        public void SpawnFrom_TemplateWithSprite_ResolvesClip()
        {
            var svc = new NpcSpawnService(MakeRegistry(withSprite: true));
            svc.SpawnFrom(MakeManifest(7));
            var inst = svc.Live[0];
            Assert.IsNotNull(inst.template);
            Assert.IsTrue(inst.spriteResolved);
            Assert.AreEqual("atlas/guard", inst.spriteClipRef);
        }

        [Test]
        public void SpawnFrom_UnknownTemplate_InstanceHasNoTemplate()
        {
            var svc = new NpcSpawnService(MakeRegistry(true));
            svc.SpawnFrom(MakeManifest(999)); // not registered
            var inst = svc.Live[0];
            Assert.IsNull(inst.template);
            Assert.IsFalse(inst.spriteResolved);
        }

        [Test]
        public void SpawnFrom_TemplateWithoutSprite_SpriteUnresolved()
        {
            var svc = new NpcSpawnService(MakeRegistry(withSprite: false));
            svc.SpawnFrom(MakeManifest(7));
            Assert.IsFalse(svc.Live[0].spriteResolved);
        }

        // --- AC#3: inspector shows source ids ---

        [Test]
        public void InspectorSummary_ShowsTemplateSpawnScriptSprite()
        {
            var svc = new NpcSpawnService(MakeRegistry(true));
            svc.SpawnFrom(MakeManifest(7));
            var summary = svc.Live[0].InspectorSummary();
            StringAssert.Contains("template=7", summary);
            StringAssert.Contains("script=scripts/guard.lua", summary);
            StringAssert.Contains("sprite=atlas/guard", summary);
        }

        [Test]
        public void InspectorSummary_MissingTemplate_Flagged()
        {
            var svc = new NpcSpawnService(MakeRegistry(true));
            svc.SpawnFrom(MakeManifest(999));
            var summary = svc.Live[0].InspectorSummary();
            StringAssert.Contains("missing", summary);
        }

        [Test]
        public void GetInstance_ReturnsByInstanceId()
        {
            var svc = new NpcSpawnService(MakeRegistry(true));
            svc.SpawnFrom(MakeManifest(7, 7));
            var first = svc.Live[0];
            Assert.AreSame(first, svc.GetInstance(first.instanceId));
            Assert.IsNull(svc.GetInstance(99999));
        }

        // --- AC#4: despawn without reloading ---

        [Test]
        public void ToggleNpcs_Off_DespawnsAll()
        {
            var svc = new NpcSpawnService(MakeRegistry(true));
            svc.ToggleNpcs(true, MakeManifest(7, 7, 7));
            Assert.AreEqual(3, svc.LiveCount);

            svc.ToggleNpcs(false, null);
            Assert.IsFalse(svc.NpcsVisible);
            Assert.AreEqual(0, svc.LiveCount);
        }

        [Test]
        public void DespawnAll_OnEmpty_DoesNotThrow()
        {
            var svc = new NpcSpawnService(MakeRegistry(true));
            Assert.DoesNotThrow(() => svc.DespawnAll());
        }

        [Test]
        public void Respawn_AssignsFreshInstanceIds()
        {
            var svc = new NpcSpawnService(MakeRegistry(true));
            svc.SpawnFrom(MakeManifest(7));
            int firstId = svc.Live[0].instanceId;
            svc.SpawnFrom(MakeManifest(7));
            int secondId = svc.Live[0].instanceId;
            Assert.AreNotEqual(firstId, secondId);
        }
    }
}
