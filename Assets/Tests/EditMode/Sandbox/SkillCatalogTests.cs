using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M4.1 — Skill Catalog tests. Generates SkillDefinition entries (AC#1),
    /// validates icon/effect/missile asset links (AC#2), and exposes selected-skill
    /// details (AC#3).
    /// </summary>
    public class SkillCatalogTests
    {
        private SourceAssetId Id(int uid, string path, ResourceKind kind = ResourceKind.Sprite)
            => new SourceAssetId { sourcePath = path, uid = uid, resourceKind = kind };

        private SkillDefinition MakeSkill(int id, SkillMissileForm form = SkillMissileForm.None,
            SourceAssetId icon = null, SourceAssetId effect = null, SourceAssetId missile = null)
            => new SkillDefinition
            {
                skillId = id,
                nameNormalized = $"Skill{id}",
                reqLevel = 5,
                cost = 20,
                attackRadius = 50,
                isPhysical = true,
                missileForm = form,
                iconSourceId = icon,
                effectSourceId = effect,
                missileSpriteId = missile,
                damageLevels =
                {
                    new SkillDamageLevel { level = 1, baseDamage = 100, attackRatio = 1f, isPhysical = true },
                    new SkillDamageLevel { level = 5, baseDamage = 250, attackRatio = 1.5f, isPhysical = true },
                },
            };

        private AssetRegistry RegistryWith(params SourceAssetId[] available)
        {
            var reg = new AssetRegistry();
            foreach (var id in available)
                reg.Register(new AssetRegistryEntry { sourceId = id, status = AssetStatus.Available });
            return reg;
        }

        // --- AC#1: SkillDefinition entries generated ---

        [Test]
        public void Register_StoresSkill()
        {
            var cat = new SkillCatalog();
            cat.Register(MakeSkill(1));
            var s = cat.Resolve(1);
            Assert.IsNotNull(s);
            Assert.AreEqual("Skill1", s.DisplayName);
            Assert.AreEqual(1, cat.Count);
        }

        [Test]
        public void GetLevel_ReturnsHighestNotExceeding()
        {
            var s = MakeSkill(1);
            Assert.AreEqual(100, s.GetLevel(1).baseDamage);
            Assert.AreEqual(100, s.GetLevel(4).baseDamage);
            Assert.AreEqual(250, s.GetLevel(5).baseDamage);
            Assert.AreEqual(250, s.GetLevel(99).baseDamage);
        }

        // --- AC#2: asset links validated ---

        [Test]
        public void ValidateAssets_AllAvailable_NoIssues()
        {
            var icon = Id(1, "skill/icon1.spr");
            var effect = Id(2, "skill/fx1.spr");
            var missile = Id(3, "skill/missile1.spr");
            var cat = new SkillCatalog(RegistryWith(icon, effect, missile));
            cat.Register(MakeSkill(1, SkillMissileForm.Single, icon, effect, missile));

            var issues = cat.ValidateAssets();
            Assert.IsEmpty(issues);
            Assert.IsTrue(cat.Resolve(1).iconResolved);
            Assert.IsTrue(cat.Resolve(1).effectResolved);
        }

        [Test]
        public void ValidateAssets_MissingIcon_Reported()
        {
            var cat = new SkillCatalog(RegistryWith()); // nothing available
            cat.Register(MakeSkill(1, SkillMissileForm.None, icon: Id(1, "skill/missing.spr")));

            var issues = cat.ValidateAssets();
            Assert.IsTrue(issues.Any(i => i.kind == "icon" && i.skillId == 1));
            Assert.IsFalse(cat.Resolve(1).iconResolved);
        }

        [Test]
        public void ValidateAssets_MissileFormButNoMissileAsset_Reported()
        {
            var icon = Id(1, "skill/icon.spr");
            var cat = new SkillCatalog(RegistryWith(icon));
            // Has missile form but no missile sprite ref → required, reported.
            cat.Register(MakeSkill(1, SkillMissileForm.Single, icon: icon, missile: null));

            var issues = cat.ValidateAssets();
            Assert.IsTrue(issues.Any(i => i.kind == "missile" && i.skillId == 1));
        }

        [Test]
        public void ValidateAssets_NoMissileForm_MissileNotRequired()
        {
            var icon = Id(1, "skill/icon.spr");
            var cat = new SkillCatalog(RegistryWith(icon));
            cat.Register(MakeSkill(1, SkillMissileForm.None, icon: icon, missile: null));

            var issues = cat.ValidateAssets();
            Assert.IsFalse(issues.Any(i => i.kind == "missile"));
        }

        // --- AC#3: selected skill details ---

        [Test]
        public void Select_KnownSkill_ReturnsAndExposesDetails()
        {
            var cat = new SkillCatalog();
            cat.Register(MakeSkill(7, SkillMissileForm.Fan));
            var s = cat.Select(7);
            Assert.IsNotNull(s);
            Assert.AreEqual(7, cat.SelectedSkillId);
            var details = cat.SelectedDetails();
            StringAssert.Contains("Skill7", details);
            StringAssert.Contains("range=50", details);
            StringAssert.Contains("missile=Fan", details);
        }

        [Test]
        public void Select_UnknownSkill_ClearsSelection()
        {
            var cat = new SkillCatalog();
            Assert.IsNull(cat.Select(999));
            Assert.AreEqual(-1, cat.SelectedSkillId);
            Assert.AreEqual("No skill selected", cat.SelectedDetails());
        }
    }
}
