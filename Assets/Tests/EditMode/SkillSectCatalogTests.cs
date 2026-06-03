// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.1 Skill Sect Catalog Tests
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class SkillSectCatalogTests
    {
        [Test]
        public void All_10_Factions_Have_Skills()
        {
            foreach (var fid in CombatFactionExt.AllFactions)
            {
                var sect = SkillSectCatalog.GetSect(fid);
                Assert.IsFalse(string.IsNullOrEmpty(sect.nameVi),
                    $"Faction {fid} missing Vietnamese name");
                Assert.IsNotNull(sect.skills,
                    $"Faction {fid} ({sect.nameVi}) has null skills");
                Assert.Greater(sect.skills.Count, 0,
                    $"Faction {fid} ({sect.nameVi}) has 0 skills");
            }
        }

        [Test]
        public void Each_Faction_Has_Correct_ViName()
        {
            Assert.AreEqual("Thiếu Lâm", CombatFactionExt.ShaolinId.FactionViName());
            Assert.AreEqual("Thiên Vương", CombatFactionExt.TianWangId.FactionViName());
            Assert.AreEqual("Đường Môn", CombatFactionExt.TangMenId.FactionViName());
            Assert.AreEqual("Ngũ Độc", CombatFactionExt.WuDuId.FactionViName());
            Assert.AreEqual("Cái Bang", CombatFactionExt.CaiBangId.FactionViName());
            Assert.AreEqual("Thiên Nhẫn", CombatFactionExt.TianRenId.FactionViName());
            Assert.AreEqual("Nga My", CombatFactionExt.EMeiId.FactionViName());
            Assert.AreEqual("Thúy Yên", CombatFactionExt.CuiYanId.FactionViName());
            Assert.AreEqual("Võ Đang", CombatFactionExt.WuDangId.FactionViName());
            Assert.AreEqual("Côn Lôn", CombatFactionExt.KunLunId.FactionViName());
        }

        [Test]
        public void CharClass_Mapping_Is_Correct()
        {
            Assert.AreEqual(1, CombatFactionExt.ShaolinId.ToCharClass());
            Assert.AreEqual(1, CombatFactionExt.TianWangId.ToCharClass());
            Assert.AreEqual(2, CombatFactionExt.EMeiId.ToCharClass());
            Assert.AreEqual(2, CombatFactionExt.CuiYanId.ToCharClass());
            Assert.AreEqual(3, CombatFactionExt.TangMenId.ToCharClass());
            Assert.AreEqual(3, CombatFactionExt.WuDuId.ToCharClass());
            Assert.AreEqual(4, CombatFactionExt.CaiBangId.ToCharClass());
            Assert.AreEqual(4, CombatFactionExt.TianRenId.ToCharClass());
            Assert.AreEqual(5, CombatFactionExt.WuDangId.ToCharClass());
            Assert.AreEqual(5, CombatFactionExt.KunLunId.ToCharClass());
        }

        [Test]
        public void Lua_Script_Maps_To_Correct_Faction()
        {
            Assert.AreEqual(CombatFactionExt.ShaolinId,
                CombatFactionExt.FactionFromLuaScript("\\script\\skill\\shaolin.lua"));
            Assert.AreEqual(CombatFactionExt.CaiBangId,
                CombatFactionExt.FactionFromLuaScript("\\script\\skill\\gaibang.lua"));
            Assert.AreEqual(CombatFactionExt.EMeiId,
                CombatFactionExt.FactionFromLuaScript("\\script\\skill\\emei.lua"));
            Assert.AreEqual(CombatFactionExt.WuDangId,
                CombatFactionExt.FactionFromLuaScript("\\script\\skill\\wudang.lua"));
        }

        [Test]
        public void Shaolin_Has_Known_Skill_Count()
        {
            var skills = SkillSectCatalog.GetSkills(CombatFactionExt.ShaolinId);
            Assert.GreaterOrEqual(skills.Count, 15,
                $"Thiếu Lâm should have >= 15 skills, got {skills.Count}");
        }

        [Test]
        public void CaiBang_Has_All_Core_Skills()
        {
            var skills = SkillSectCatalog.GetSkills(CombatFactionExt.CaiBangId);
            var ids = new HashSet<int>();
            foreach (var s in skills) ids.Add(s.skillId);
            Assert.IsTrue(ids.Contains(115), "Missing 115 Cái Bang Bổng Pháp");
            Assert.IsTrue(ids.Contains(116), "Missing 116 Cái Bang Chưởng Pháp");
            Assert.IsTrue(ids.Contains(125), "Missing 125 Bổng Đả ác Cẩu");
            Assert.IsTrue(ids.Contains(128), "Missing 128 Kháng Long Hữu Hối");
            Assert.IsTrue(ids.Contains(130), "Missing 130 Túy Điệp Cuồng Vũ");
        }

        [Test]
        public void Level_Curve_Interpolation_Matches_PC_Pattern()
        {
            var points = new[] { (1, 320), (20, 384) };
            Assert.AreEqual(320, PcSkillTuningRegistry.InterpolateInt(1, points));
            Assert.AreEqual(384, PcSkillTuningRegistry.InterpolateInt(20, points));
            int mid = PcSkillTuningRegistry.InterpolateInt(10, points);
            Assert.Greater(mid, 320);
            Assert.Less(mid, 384);
        }

        [Test]
        public void SkillLevelCurveService_Returns_Stats()
        {
            var stats = SkillLevelCurveService.GetStats(125, 20, CombatFactionExt.CaiBangId);
            Assert.AreEqual(20, stats.level);
            Assert.Greater(stats.attackRadius, 0);
        }

        [Test]
        public void All_Sects_Dictionary_Has_10_Entries()
        {
            Assert.AreEqual(10, SkillSectCatalog.AllSects.Count);
        }

        [Test]
        public void Each_Faction_Has_Element_Description()
        {
            foreach (var fid in CombatFactionExt.AllFactions)
            {
                var sect = SkillSectCatalog.GetSect(fid);
                Assert.IsFalse(string.IsNullOrEmpty(sect.elementDesc),
                    $"Faction {fid} ({sect.nameVi}) missing elementDesc");
            }
        }

        [Test]
        public void Passive_Skills_Have_Zero_Cost()
        {
            foreach (var fid in CombatFactionExt.AllFactions)
            {
                var skills = SkillSectCatalog.GetSkills(fid);
                foreach (var s in skills)
                {
                    if (s.tier == SkillTier.Passive)
                    {
                        var stats = SkillLevelCurveService.GetStats(s.skillId, 1, fid);
                        Assert.AreEqual(0, stats.skillCost,
                            $"Passive skill {s.skillId} ({s.nameVi}) should cost 0");
                    }
                }
            }
        }

        [Test]
        public void FactionsByCharClass_Groups_Correctly()
        {
            var cc1 = CombatFactionExt.FactionsByCharClass[1];
            Assert.AreEqual(2, cc1.Length);
            Assert.AreEqual(CombatFactionExt.ShaolinId, cc1[0]);
            Assert.AreEqual(CombatFactionExt.TianWangId, cc1[1]);
        }
    }
}
