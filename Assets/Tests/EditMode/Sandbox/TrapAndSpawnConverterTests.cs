using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>M1.6 — Trap and Trigger Region Conversion tests.</summary>
    public class TrapSectionConverterTests
    {
        private TrapSectionData MakeTrapData(int count = 2, bool hasScript = true)
        {
            var d = new TrapSectionData { count = (uint)count };
            for (int i = 0; i < count; i++)
                d.entries.Add(new TrapRawEntry
                {
                    x = i * 10, y = 0, width = 10, height = 10,
                    scriptId = hasScript ? (uint)(1000 + i) : 0u,
                    triggerType = 1,
                    scriptName = hasScript ? $"scripts/trap{i}.lua" : "",
                });
            return d;
        }

        // AC#1: TrapDefinition with bounds, scriptId/name, triggerType
        [Test]
        public void Convert_TrapDefinition_HasBoundsAndScript()
        {
            var data = MakeTrapData(2);
            var manifest = TrapSectionConverter.Convert(data, 1, 0, 0);

            Assert.AreEqual(2, manifest.traps.Count, "AC#1: should have one TrapDefinition per entry");
            var t = manifest.traps[0];
            Assert.AreEqual(0f, t.boundsRect.x);  // entry[0].x = 0*10 = 0
            Assert.AreEqual(10f, t.boundsRect.width, "boundsRect.width should be set (AC#1)");
            Assert.IsNotNull(t.scriptRef, "scriptRef must be set (AC#1)");
            Assert.AreEqual(TrapTriggerType.Enter, t.triggerType, "triggerType=1 maps to Enter (AC#1)");
        }

        [Test]
        public void Convert_TrapDefinition_IndexSequential()
        {
            var data = MakeTrapData(3);
            var manifest = TrapSectionConverter.Convert(data, 1, 0, 0);
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(i, manifest.traps[i].trapIndex);
        }

        // AC#4: Missing script reported
        [Test]
        public void Convert_MissingScript_ReportedInWarnings()
        {
            var data = MakeTrapData(2, hasScript: false);
            var manifest = TrapSectionConverter.Convert(data, 1, 0, 0);

            Assert.Greater(manifest.missingScripts, 0, "AC#4: traps with scriptId=0 and no name should be flagged");
            Assert.Greater(manifest.traps[0].warnings.Count, 0, "warning should be on individual TrapDefinition");
        }

        [Test]
        public void Convert_NullData_ReturnsNotStarted()
        {
            var manifest = TrapSectionConverter.Convert(null, 1, 0, 0);
            Assert.AreEqual(ConversionStatus.NotStarted, manifest.status);
        }

        [Test]
        public void Convert_AllScriptsPresent_StatusComplete()
        {
            var data = MakeTrapData(2, hasScript: true);
            var manifest = TrapSectionConverter.Convert(data, 1, 0, 0);
            Assert.AreEqual(ConversionStatus.Complete, manifest.status);
        }

        // Parser tests
        [Test]
        public void TrapSectionParser_Parse_EmptyData_ReturnsNull()
        {
            var result = TrapSectionParser.Parse(new byte[2]);
            Assert.IsNull(result);
        }

        [Test]
        public void TrapSectionParser_Parse_ZeroCount_ReturnsEmpty()
        {
            var data = new byte[4]; // count = 0
            var result = TrapSectionParser.Parse(data);
            Assert.IsNotNull(result);
            Assert.AreEqual(0u, result.count);
            Assert.AreEqual(0, result.entries.Count);
        }
    }

    /// <summary>M1.7 — NPC/Object Spawn Table Conversion tests.</summary>
    public class SpawnSectionConverterTests
    {
        private NpcSectionData MakeNpcData(int count = 2, bool validTemplate = true)
        {
            var d = new NpcSectionData { count = (uint)count };
            for (int i = 0; i < count; i++)
                d.entries.Add(new NpcRawEntry
                {
                    posX = i * 50, posY = i * 30,
                    templateId = validTemplate ? (uint)(100 + i) : 0u,
                    direction = 2,
                    scriptRef = validTemplate ? $"scripts/npc{i}.lua" : "",
                });
            return d;
        }

        private ObjSectionData MakeObjData(int count = 1, bool foreground = false)
        {
            var d = new ObjSectionData { count = (uint)count };
            for (int i = 0; i < count; i++)
                d.entries.Add(new ObjRawEntry
                {
                    posX = i * 20, posY = 0,
                    spriteId = (uint)(200 + i),
                    frame = 0,
                    layer = 1,
                    flags = foreground ? (byte)0x01 : (byte)0x00,
                    spritePath = $"spr/obj{i}.spr",
                });
            return d;
        }

        // AC#1: NpcSpawn entries include templateId, position, region, direction, scriptRef
        [Test]
        public void Convert_NpcSpawn_HasAllRequiredFields()
        {
            var npc = MakeNpcData(2);
            var manifest = SpawnSectionConverter.Convert(npc, null, 1, 3, 5);

            Assert.AreEqual(2, manifest.npcSpawns.Count, "AC#1: should have one NpcSpawn per entry");
            var spawn = manifest.npcSpawns[0];
            Assert.AreEqual(100, spawn.templateId, "AC#1: templateId must match");
            Assert.AreEqual(0f, spawn.posX);
            Assert.AreEqual(0f, spawn.posY);
            Assert.AreEqual(NpcDirection.West, spawn.direction, "direction 2 = West");
            Assert.AreEqual(3, spawn.regionX, "AC#1: regionX from converter params");
            Assert.AreEqual(5, spawn.regionY);
            Assert.IsNotNull(spawn.scriptRef, "AC#1: scriptRef must be populated");
        }

        [Test]
        public void Convert_ObjectPlacement_Foreground_FlagSet()
        {
            var manifest = SpawnSectionConverter.Convert(null, MakeObjData(1, foreground: true), 1, 0, 0);
            Assert.AreEqual(1, manifest.objects.Count);
            Assert.IsTrue(manifest.objects[0].isForeground, "AC#1: foreground flag from obj flags byte");
        }

        // AC#3: Missing template reported separately from missing spawn
        [Test]
        public void Convert_MissingTemplate_ReportedSeparately()
        {
            var npc = MakeNpcData(2, validTemplate: false);
            var manifest = SpawnSectionConverter.Convert(npc, null, 1, 0, 0);

            Assert.AreEqual(2, manifest.missingTemplates, "AC#3: templateId=0 should count as missing template");
            Assert.AreEqual(2, manifest.npcSpawns.Count, "AC#3: spawn entries must still be present (not filtered)");
            Assert.IsFalse(manifest.npcSpawns[0].templateFound, "templateFound should be false");
        }

        [Test]
        public void Convert_ValidTemplate_TemplateFoundTrue()
        {
            var npc = MakeNpcData(1, validTemplate: true);
            var manifest = SpawnSectionConverter.Convert(npc, null, 1, 0, 0);
            Assert.IsTrue(manifest.npcSpawns[0].templateFound);
            Assert.AreEqual(0, manifest.missingTemplates);
        }

        [Test]
        public void Convert_WithObjData_ObjectsIncluded()
        {
            var obj = MakeObjData(3);
            var manifest = SpawnSectionConverter.Convert(null, obj, 1, 0, 0);
            Assert.AreEqual(3, manifest.totalObjects);
            Assert.AreEqual(3, manifest.objects.Count);
        }

        [Test]
        public void Convert_NullInputs_ReturnsEmptyManifest()
        {
            var manifest = SpawnSectionConverter.Convert(null, null, 1, 0, 0);
            Assert.IsNotNull(manifest);
            Assert.AreEqual(0, manifest.totalNpcs);
            Assert.AreEqual(0, manifest.totalObjects);
        }

        // Parser roundtrip: zero-count buffers
        [Test]
        public void NpcSectionParser_Parse_ZeroCount_EmptyEntries()
        {
            var data = new byte[4]; // count = 0
            var result = NpcSectionParser.Parse(data);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.entries.Count);
        }

        [Test]
        public void ObjSectionParser_Parse_ZeroCount_EmptyEntries()
        {
            var data = new byte[4];
            var result = ObjSectionParser.Parse(data);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.entries.Count);
        }
    }
}
