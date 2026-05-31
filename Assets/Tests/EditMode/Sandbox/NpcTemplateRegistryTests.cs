using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M3.1 — NPC Template Registry tests. Registration from converter output (AC#1),
    /// spawn-to-template resolution (AC#2), and missing-resource reporting (AC#3).
    /// </summary>
    public class NpcTemplateRegistryTests
    {
        private SourceAssetId SpriteId(int uid, string path)
            => new SourceAssetId { sourcePath = path, uid = uid, resourceKind = ResourceKind.Sprite };

        private NpcTemplate MakeTemplate(int id, SourceAssetId sprite = null, string script = null)
            => new NpcTemplate
            {
                templateId = id,
                nameNormalized = $"Guard{id}",
                level = 10,
                maxLife = 100,
                spriteSourceId = sprite,
                scriptRef = script,
            };

        // --- AC#1: registry includes id/name/stats/resource/script refs ---

        [Test]
        public void Register_StoresTemplateWithFields()
        {
            var reg = new NpcTemplateRegistry();
            reg.Register(MakeTemplate(1, SpriteId(100, "npc/guard.spr"), "scripts/guard.lua"));

            var t = reg.Resolve(1);
            Assert.IsNotNull(t);
            Assert.AreEqual("Guard1", t.DisplayName);
            Assert.AreEqual(10, t.level);
            Assert.AreEqual("scripts/guard.lua", t.scriptRef);
            Assert.AreEqual(1, reg.Count);
        }

        [Test]
        public void DisplayName_FallsBackToId_WhenNamesEmpty()
        {
            var t = new NpcTemplate { templateId = 42 };
            Assert.AreEqual("NPC_42", t.DisplayName);
        }

        // --- AC#2: spawn reference resolves template ---

        [Test]
        public void Resolve_KnownId_ReturnsTemplate()
        {
            var reg = new NpcTemplateRegistry();
            reg.Register(MakeTemplate(7, SpriteId(7, "npc/7.spr")));
            var spawn = new NpcSpawn { templateId = 7 };

            var resolved = reg.Resolve(spawn.templateId);
            Assert.IsNotNull(resolved);
            Assert.AreEqual(7, resolved.templateId);
        }

        [Test]
        public void Resolve_UnknownId_ReturnsNull()
        {
            var reg = new NpcTemplateRegistry();
            Assert.IsNull(reg.Resolve(999));
            Assert.IsFalse(reg.Contains(999));
        }

        // --- AC#3: missing resource reported ---

        [Test]
        public void ValidateResources_MissingSprite_Reported()
        {
            var assets = new AssetRegistry();
            // Register no sprite → resolution fails.
            var reg = new NpcTemplateRegistry(assets);
            reg.Register(MakeTemplate(1, SpriteId(100, "npc/missing.spr")));

            var issues = reg.ValidateResources();
            Assert.IsTrue(issues.Any(i => i.kind == "sprite" && i.templateId == 1));
            Assert.IsFalse(reg.Resolve(1).spriteResolved);
        }

        [Test]
        public void ValidateResources_AvailableSprite_NoIssue()
        {
            var assets = new AssetRegistry();
            var sprite = SpriteId(100, "npc/guard.spr");
            assets.Register(new AssetRegistryEntry
            {
                sourceId = sprite,
                artifactType = ArtifactType.SpriteAtlas,
                status = AssetStatus.Available,
            });
            var reg = new NpcTemplateRegistry(assets);
            reg.Register(MakeTemplate(1, sprite));

            var issues = reg.ValidateResources();
            Assert.IsFalse(issues.Any(i => i.kind == "sprite" && i.templateId == 1));
            Assert.IsTrue(reg.Resolve(1).spriteResolved);
        }

        [Test]
        public void ValidateResources_NoSpriteRef_ReportedAsMissing()
        {
            var reg = new NpcTemplateRegistry(new AssetRegistry());
            reg.Register(MakeTemplate(5, sprite: null));
            var issues = reg.ValidateResources();
            Assert.IsTrue(issues.Any(i => i.templateId == 5 && i.kind == "sprite" && i.sourceKey == "<none>"));
        }

        [Test]
        public void ValidateResources_MissingScript_Reported()
        {
            var assets = new AssetRegistry();
            var sprite = SpriteId(100, "npc/guard.spr");
            assets.Register(new AssetRegistryEntry { sourceId = sprite, status = AssetStatus.Available });
            var reg = new NpcTemplateRegistry(assets);
            reg.Register(MakeTemplate(1, sprite, script: "scripts/missing.lua"));

            var issues = reg.ValidateResources();
            Assert.IsTrue(issues.Any(i => i.kind == "script" && i.templateId == 1));
            Assert.IsFalse(reg.Resolve(1).scriptResolved);
        }

        [Test]
        public void ValidateResources_NoScript_NotAnIssue()
        {
            var assets = new AssetRegistry();
            var sprite = SpriteId(100, "npc/guard.spr");
            assets.Register(new AssetRegistryEntry { sourceId = sprite, status = AssetStatus.Available });
            var reg = new NpcTemplateRegistry(assets);
            reg.Register(MakeTemplate(1, sprite, script: null));

            var issues = reg.ValidateResources();
            Assert.IsFalse(issues.Any(i => i.kind == "script"));
        }
    }
}
