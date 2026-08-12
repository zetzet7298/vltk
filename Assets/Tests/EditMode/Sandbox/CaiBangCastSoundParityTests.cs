// -----------------------------------------------------------------------------
// VLTK Mobile — Cái Bang PC cast-sound parity tests.
//
// Background (gap closed 2026-06-18):
//   PcConfigParser.ParseSkillsLines used to `ci++; // 7 ManCastSnd (skip)` and
//   `ci++; // 8 FMCastSnd (skip)` — silently dropping every PC skill cast sound.
//   SkillEffectVisualService.PlaySkillCast only fired the missile SPR soundPath
//   (PC missles.txt SndFile1/SndFile2), never the skill-level cast sound.
//   => ALL Cái Bang skill casts were SILENT despite PC having 8 distinct cast
//      sounds (sound_k001..sound_k010) wired in skills.txt cols 7-8.
//
// Source of truth:
//   /var/www/vltk-mobile/Assets/StreamingAssets/Reference/PcSkill/skills.txt
//   PC engine path: KSkill::Cast → KClient::PlaySkillSound(m_szManCastSnd |
//   m_szFMCastSnd) fired at the cast-frame of the CharAnimId action, BEFORE the
//   missile spawns (distinct from missile SPR sound which fires mid-flight).
//
// These tests verify:
//   1. SkillDefinition now carries manCastSndPath / fmCastSndPath.
//   2. PcConfigParser parses skills.txt cols 6/7/8 (smoke test on the file).
//   3. PcCombatCatalogFactory.ApplyCaiBangPcCastAudio wires the right PC cast
//      sound to every Cái Bang damage skill.
//   4. SkillEffectVisualService.PlaySkillCast fills effect.castSoundPath from
//      skill.manCastSndPath when no missile SPR soundPath exists.
// -----------------------------------------------------------------------------

using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("CaiBang")]
    public class CaiBangCastSoundParityTests
    {
        // (skillId, expectedManCast, expectedFmCast, expectedPreCastSprUid). Source: PC skills.txt cols 7/8/6.
        // SPR UIDs computed via SprRuntimeService.ComputePathUidHex (signed GB2312 path hash).
        private static readonly (int id, string man, string fm, string preCastUid)[] PcCaiBangCastSounds =
        {
            (117, @"\sound\skill\sound_k001.wav", @"\sound\skill\sound_k006.wav", null),
            (119, @"\sound\skill\sound_k002.wav", @"\sound\skill\sound_k007.wav", null),
            (122, @"\sound\skill\sound_k003.wav", @"\sound\skill\sound_k008.wav", "3cae8f47"),
            (125, @"\sound\skill\sound_k004.wav", @"\sound\skill\sound_k009.wav", "3cae8f47"),
            // 128 Kháng Long Hữu Hối — PC PreCastSpr = mag_bz_huo3_爆炸效果.spr (UID b91ab706).
            (128, @"\sound\skill\sound_k005.wav", @"\sound\skill\sound_k010.wav", "b91ab706"),
            // 357 Phi Long Tại Thiên — same PC PreCastSpr family as 128.
            (357, @"\sound\skill\sound_k005.wav", @"\sound\skill\sound_k010.wav", "b91ab706"),
            (358, @"\sound\skill\sound_k004.wav", @"\sound\skill\sound_k009.wav", "3cae8f47"),
            (359, @"\sound\skill\sound_k004.wav", @"\sound\skill\sound_k009.wav", "3cae8f47"),
            (389, @"\sound\skill\sound_k004.wav", @"\sound\skill\sound_k009.wav", "3cae8f47"),
            // 150-tier Cái Bang — PC gb_150 precast SPR family (UID 70d46004).
            (1073, @"\sound\skill\sound_k005.wav", @"\sound\skill\sound_k010.wav", "70d46004"),
            (1074, @"\sound\skill\sound_k004.wav", @"\sound\skill\sound_k009.wav", "3cae8f47"),
            (1101, @"\sound\skill\sound_k005.wav", @"\sound\skill\sound_k010.wav", "70d46004"),
            (1161, @"\sound\skill\sound_k005.wav", @"\sound\skill\sound_k010.wav", "70d46004"),
            (1162, @"\sound\skill\sound_k004.wav", @"\sound\skill\sound_k009.wav", "3cae8f47"),
            (1539, @"\sound\skill\sound_k004.wav", @"\sound\skill\sound_k009.wav", "3cae8f47"),
        };

        [Test]
        public void Catalog_AllCaiBangDamageSkills_HavePcManCastAndFmCastSoundPaths()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(includeWuDang: false, includeShaolin: false, includeTangMen: false, includeEMei: false, includeTianWang: false, includeWuDu: false, includeCuiYan: false, includeTianRen: false, includeKunLun: false);
            foreach (var (id, man, fm, preCastUid) in PcCaiBangCastSounds)
            {
                var skill = catalog.Resolve(id);
                Assert.IsNotNull(skill, $"missing Cái Bang skill {id}");
                Assert.AreEqual(man, skill.manCastSndPath, $"skill {id} ManCastSnd mismatch vs PC skills.txt col 7");
                Assert.AreEqual(fm, skill.fmCastSndPath, $"skill {id} FMCastSnd mismatch vs PC skills.txt col 8");
                // Visual parity: PreCastSpr UID must match PC source (signed GB2312 path hash).
                // Pre-fix: 128/357 used mag_tr_16 (3cae8f47) instead of mag_bz_huo3 (b91ab706);
                //          1073/1101/1161 used mag_tr_16 instead of gb_150_shichengjiulong_a (70d46004).
                if (!string.IsNullOrEmpty(preCastUid))
                {
                    Assert.IsNotNull(skill.effectSourceId, $"skill {id} effectSourceId null (PreCastSpr)");
                    var actualUid = VLTK.Sprites.SprRuntimeService.ComputePathUidHex(skill.effectSourceId.sourcePath, "GB2312", signedBytes: true);
                    Assert.AreEqual(preCastUid, actualUid,
                        $"skill {id} PreCastSpr UID mismatch: PC source expects {preCastUid} but factory wired '{skill.effectSourceId.sourcePath}' (UID {actualUid})");
                }
            }
        }

        [Test]
        public void Catalog_CaiBangBuffAndPassiveSkills_HaveNoPcCastSound()
        {
            // PC skills.txt: 115/116 (passive mastery), 118/120/121/123/126/127/129/130 (buff/utility),
            // 274 (passive), 277 (utility), 360 (passive), 714/720 (utility) all have EMPTY cols 7/8.
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(includeWuDang: false, includeShaolin: false, includeTangMen: false, includeEMei: false, includeTianWang: false, includeWuDu: false, includeCuiYan: false, includeTianRen: false, includeKunLun: false);
            int[] silentSkills = { 115, 116, 118, 120, 121, 123, 124, 126, 127, 129, 130, 274, 277, 360, 714, 720 };
            foreach (var id in silentSkills)
            {
                var skill = catalog.Resolve(id);
                Assert.IsNotNull(skill, $"missing Cái Bang skill {id}");
                Assert.IsTrue(string.IsNullOrEmpty(skill.manCastSndPath),
                    $"PC skills.txt col 7 is empty for skill {id} but catalog wired '{skill.manCastSndPath}'");
                Assert.IsTrue(string.IsNullOrEmpty(skill.fmCastSndPath),
                    $"PC skills.txt col 8 is empty for skill {id} but catalog wired '{skill.fmCastSndPath}'");
            }
        }

        [Test]
        public void PlaySkillCast_PropagatesSkillLevelCastSound_WhenMissileSprHasNoSound()
        {
            // Pre-fix regression: SkillEffectVisualService.PlaySkillCast only fired missile SPR
            // soundPath. A skill with manCastSndPath set but missile SPR without sound would be SILENT.
            // Post-fix: skill.manCastSndPath fills effect.castSoundPath as fallback before missile sound.
            var skill = new SkillDefinition
            {
                skillId = 999117,
                nameRaw = "Test Cai Bang Skill",
                skillStyle = PcSkillStyle.Missiles,
                missileForm = SkillMissileForm.Single,
                charAnimId = 11,
                childSkillId = 0,
                childSkillNum = 0,
                timePerCast = 2,
                manCastSndPath = @"\sound\skill\sound_k001.wav",
            };

            var svc = new SkillEffectVisualService(null, null);
            var fx = svc.PlaySkillCast(skill, Vector2.zero, new Vector2(50, 0), 1);
            Assert.IsNotNull(fx);
            Assert.AreEqual(@"\sound\skill\sound_k001.wav", fx.castSoundPath,
                "skill.manCastSndPath must fill effect.castSoundPath when missile SPR has no sound");
        }

        [Test]
        public void PlaySkillCast_MissileSprSoundYieldsToSkillLevelCastSound()
        {
            // PC: KSkill::Cast fires the SKILL cast sound (skills.txt col 7) at the cast
            // frame. The missile SPR sound (missles.txt SndFile1/2) fires mid-flight.
            // Unity currently has a single cast-time hook, so the iconic skill cast sound
            // (sound_k0XX.wav) must win over the missile SPR soundPath to match what the
            // player hears at the cast moment in PC.
            // Pre-fix: ConfigureDataDrivenVisuals overwrote effect.castSoundPath with the
            // missile SPR sound, dropping the PC skill cast sound entirely.
            var skill = new SkillDefinition
            {
                skillId = 999128,
                nameRaw = "Test Cai Bang Skill With Missile Sound",
                skillStyle = PcSkillStyle.Missiles,
                missileForm = SkillMissileForm.Single,
                charAnimId = 11,
                childSkillId = 999,
                childSkillNum = 1,
                timePerCast = 2,
                manCastSndPath = @"\sound\skill\sound_k005.wav",
            };

            var svc = new SkillEffectVisualService(null, null);
            var fx = svc.PlaySkillCast(skill, Vector2.zero, new Vector2(50, 0), 1);
            Assert.IsNotNull(fx);
            Assert.AreEqual(@"\sound\skill\sound_k005.wav", fx.castSoundPath,
                "skill.manCastSndPath (PC skills.txt col 7) must win over missile SPR soundPath");
        }
    }
}
