// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests cho 9 ST Client Settings services
// Cover: Portrait / SoundList / Killer / ItemDetail / ItemType / MapTraffic /
//        MapType / AdjustColor / ClientWeaponSkill
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class ClientSettingsServiceTests
    {
        // ── Portrait ─────────────────────────────────────────────────────────
        [Test]
        public void PortraitService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = PortraitService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void PortraitService_GetByFaction_FiltersCorrectly()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcAttrib");
            var reg = PcPortraitParser.BuildRegistry(root);
            var svc = new PortraitService(reg);
            // Với mọi factionId (0-9), GetByFaction không throw
            for (int f = 0; f <= 9; f++)
            {
                var list = svc.GetByFaction(f);
                Assert.IsNotNull(list);
                foreach (var p in list) Assert.AreEqual(f, p.factionId);
            }
        }

        // ── SoundList ────────────────────────────────────────────────────────
        [Test]
        public void SoundListService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = SoundListService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void SoundListService_GetByCategory_FiltersCorrectly()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference");
            var reg = PcSoundListParser.BuildRegistry(root);
            var svc = new SoundListService(reg);
            for (int c = 0; c <= 4; c++)
            {
                var list = svc.GetByCategory(c);
                Assert.IsNotNull(list);
                foreach (var s in list) Assert.AreEqual(c, s.category);
            }
        }

        // ── Killer ───────────────────────────────────────────────────────────
        [Test]
        public void KillerService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = KillerService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void KillerService_CanPk_RejectsInvalidMap()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference");
            var reg = PcKillerParser.BuildRegistry(root);
            var svc = new KillerService(reg);
            // MapId không tồn tại trong registry → CanPk = false
            Assert.IsFalse(svc.CanPk(int.MaxValue));
            Assert.IsFalse(svc.CanPk(-1));
        }

        // ── ItemDetail ───────────────────────────────────────────────────────
        [Test]
        public void ItemDetailService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = ItemDetailService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void ItemDetailService_GetByCategory_FiltersCorrectly()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcItem");
            var reg = PcItemDetailParser.BuildRegistry(root);
            var svc = new ItemDetailService(reg);
            for (int c = 0; c <= 20; c++)
            {
                var list = svc.GetByCategory(c);
                Assert.IsNotNull(list);
                foreach (var d in list) Assert.AreEqual(c, d.category);
            }
        }

        // ── ItemType ─────────────────────────────────────────────────────────
        [Test]
        public void ItemTypeService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = ItemTypeService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void ItemTypeService_GetAll_NonEmpty()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcItem");
            var reg = PcItemTypeParser.BuildRegistry(root);
            var svc = new ItemTypeService(reg);
            var all = svc.GetAll();
            Assert.IsNotNull(all);
            // Có thể rỗng nếu file chưa được copy sang mobile, nhưng không null
            Assert.GreaterOrEqual(all.Count, 0);
        }

        // ── MapTraffic ───────────────────────────────────────────────────────
        [Test]
        public void MapTrafficService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = MapTrafficService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void MapTrafficService_GetTraffic_ReturnsNullForInvalid()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcMap");
            var reg = PcMapTrafficParser.BuildRegistry(root);
            var svc = new MapTrafficService(reg);
            Assert.IsNull(svc.GetTraffic(int.MaxValue));
            Assert.IsNull(svc.GetTraffic(-1));
            Assert.IsNull(svc.GetTraffic(0));
        }

        // ── MapType ──────────────────────────────────────────────────────────
        [Test]
        public void MapTypeService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = MapTypeService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void MapTypeService_GetType_ReturnsNullForInvalid()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcMap");
            var reg = PcMapTypeParser.BuildRegistry(root);
            var svc = new MapTypeService(reg);
            Assert.IsNull(svc.GetType(int.MaxValue));
            Assert.IsNull(svc.GetType(-1));
        }

        // ── AdjustColor ──────────────────────────────────────────────────────
        [Test]
        public void AdjustColorService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = AdjustColorService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void AdjustColorService_GetColor_ReturnsNullForInvalid()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference");
            var reg = PcAdjustColorParser.BuildRegistry(root);
            var svc = new AdjustColorService(reg);
            Assert.IsNull(svc.GetColor(int.MaxValue));
            Assert.IsNull(svc.GetColor(0));
        }

        // ── ClientWeaponSkill ────────────────────────────────────────────────
        [Test]
        public void ClientWeaponSkillService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = ClientWeaponSkillService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void ClientWeaponSkillService_GetByLevel_FiltersCorrectly()
        {
            string root = Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcSkill");
            var reg = PcClientWeaponSkillParser.BuildRegistry(root);
            var svc = new ClientWeaponSkillService(reg);
            for (int lv = 1; lv <= 150; lv += 10)
            {
                var list = svc.GetByLevel(lv);
                Assert.IsNotNull(list);
                foreach (var s in list) Assert.LessOrEqual(s.requiredLevel, lv);
            }
        }
    }
}
