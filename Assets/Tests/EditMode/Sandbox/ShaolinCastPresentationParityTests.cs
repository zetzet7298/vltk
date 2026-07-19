// SKL-S-CAST-PRESENT-001: exact presentation rows from pinned vltktool Shaolin slice.
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;
using VLTK.Sprites;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("Shaolin")]
    public class ShaolinCastPresentationParityTests
    {
        // (SkillId, CharAnimId, WaitTime, ManCastSnd, signed GB2312 PreCastSpr UID or null).
        private static readonly (int id, int charAnim, int waitTime, string manCast, string preCastUid)[] CanonicalRows =
        {
            (3, 14, 0, null, null), (4, 14, 0, null, null), (6, 14, 0, null, null),
            (8, 14, 0, null, null), (9, 14, 0, null, null),
            (10, 9, 0, @"\sound\skill\sound_k001.wav", null),
            (11, 10, 0, @"\sound\skill\sound_k002.wav", null), (12, 14, 0, null, null),
            (13, 11, 5, @"\sound\skill\sound_k011.wav", "ccea16f5"),
            (14, 11, 5, @"\sound\skill\sound_k003.wav", null),
            (15, 11, 5, @"\sound\skill\不动明王咒.wav", "dd035109"),
            (16, 14, 0, null, null), (17, 10, 0, @"\sound\skill\sound_k004.wav", null),
            (18, 11, 0, @"\sound\skill\慧眼咒.wav", "afe532e2"),
            (19, 11, 5, @"\sound\skill\摩诃无量.wav", null),
            (20, 11, 2, @"\sound\skill\狮子吼.wav", "65707acf"), (21, 14, 0, null, null),
        };

        [Test]
        public void Catalog_ShaolinCastPresentation_MatchesPinnedSlice()
        {
            var skills = PcCombatCatalogFactory.CreateShaolinSkills();
            foreach (var (id, charAnim, waitTime, manCast, preCastUid) in CanonicalRows)
            {
                var skill = skills.SingleOrDefault(item => item.skillId == id);
                Assert.IsNotNull(skill, $"missing Shaolin skill {id}");
                Assert.AreEqual(charAnim, skill.charAnimId, $"skill {id} CharAnimId mismatch");
                Assert.AreEqual(waitTime, skill.waitTime, $"skill {id} WaitTime mismatch");
                Assert.AreEqual(manCast, skill.manCastSndPath, $"skill {id} ManCastSnd mismatch");
                Assert.IsTrue(string.IsNullOrEmpty(skill.fmCastSndPath), $"skill {id} canonical FMCastSnd is empty or 0");

                if (preCastUid == null)
                {
                    Assert.IsNull(skill.effectSourceId, $"skill {id} has no canonical PreCastSpr");
                    continue;
                }

                Assert.IsNotNull(skill.effectSourceId, $"skill {id} canonical PreCastSpr missing");
                Assert.AreEqual(preCastUid,
                    SprRuntimeService.ComputePathUidHex(skill.effectSourceId.sourcePath, "GB2312", signedBytes: true),
                    $"skill {id} PreCastSpr UID mismatch");
            }
        }

        [Test]
        public void Catalog_ShaolinLearnedOnlyRoots_RemainUnregisteredUntilCatalogEvidenceExists()
        {
            var ids = PcCombatCatalogFactory.CreateShaolinSkills().Select(skill => skill.skillId).ToArray();
            foreach (var id in new[] { 271, 273, 318, 319, 321, 709, 1055, 1056, 1057 })
                CollectionAssert.DoesNotContain(ids, id, $"learned-only root {id} has no owned Shaolin factory row");
        }
    }
}
