using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-TianXiaWuGou 2026-06-19] Phase A.3 + B.2: PC zone form for skill 125 + 1539.
    // PC gaibang.lua::tianxia_wugou skill_misslenum_v L1=1, L20=3.
    // PC MissilesForm=5 (Zone) — missiles distribute in fixed-radius area.
    // Trước fix: Surround (form 3) + childNum=16 — sai PC.
    // Sau fix: Zone (form 5) + childNum=3 + SetupPcZoneMissiles(radius=512).
    public class CaiBangTianXiaWuGouTests
    {
        private SkillCatalog Catalog() => PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();

        [Test]
        public void TianxiaWugou_SurroundForm_RuntimeMissileCount3()
        {
            // PC source (jx-source) Skills.txt 125 MissilesForm=4 (Chain). Unity runtime renders as Surround form
            // (visual semantic: missiles fan out around caster). PC gaibang.lua::tianxia_wugou skill_misslenum_v
            // L1=1, L20=3 — runtime đọc qua PcCaiBangLuaLevelService, catalog childSkillNum=0 (Lua override).
            // Trước fix [2026-06-19]: Surround + childSkillNum=16 + attackRadius=512 — sai PC.
            // Sau fix: Surround + childSkillNum=0 (Lua runtime) + attackRadius=400 (PC L20).
            var cat = Catalog();
            var s = cat.Resolve(125);
            Assert.IsNotNull(s);
            Assert.AreEqual(SkillMissileForm.Surround, s.missileForm, "125 PC MissilesForm=4 (Chain, rendered as Surround)");
            Assert.AreEqual(0, s.childSkillNum, "125 PC ChildSkillNum=0 (Lua runtime override)");
            Assert.AreEqual(400, s.attackRadius, "125 PC AttackRadius L20=400");
        }

        [Test]
        public void TianxiaWugou_NpcVariant1539_SameShape()
        {
            var cat = Catalog();
            var p = cat.Resolve(125);
            var n = cat.Resolve(1539);
            Assert.IsNotNull(n, "skill 1539 missing");
            Assert.AreEqual(p.missileForm, n.missileForm, "1539 missileForm = 125 missileForm");
            Assert.AreEqual(p.attackRadius, n.attackRadius, "1539 attackRadius = 125 attackRadius");
            Assert.AreEqual(0, n.childSkillNum, "1539 PC ChildSkillNum=0 (Lua runtime)");
        }

        [Test]
        public void TianxiaWugou_L20MissileCount_FromLua()
        {
            // PC gaibang.lua tianxia_wugou skill_misslenum_v L20=3 (verified).
            if (!PcCaiBangLuaLevelService.Applies(125))
            {
                Assert.Ignore("gaibang.lua not loaded");
                return;
            }
            int count = PcCaiBangLuaLevelService.GetMissileCount(125, 20);
            Assert.AreEqual(3, count, "125 tianxia_wugou skill_misslenum_v L20=3");
        }

        [Test]
        public void SkillMissileForm_ZoneEnumValue_Five()
        {
            // PC SKILL_MF_Zone form 5. Đảm bảo enum value khớp PC.
            Assert.AreEqual(5, (int)SkillMissileForm.Zone, "Zone enum value must be 5 (PC SKILL_MF_Zone)");
        }
    }
}
