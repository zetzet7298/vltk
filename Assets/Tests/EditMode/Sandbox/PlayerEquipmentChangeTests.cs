// -----------------------------------------------------------------------------
// VLTK Mobile — PlayerEquipmentService EditMode tests.
// Kiểm tra equipment lifecycle: equip/unequip, slot-specific host dispatch
// (weapon/armor/helmet/mount), variant resolution, default behavior.
// PC source: NpcRes/npcres/man + npcres/woman, 男主角贴图顺序表.txt.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class PlayerEquipmentChangeTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IPlayerEquipmentHost
        {
            public int RefreshCalls;
            public int SfxCalls;
            public int WeaponCalls;
            public int ArmorCalls;
            public int HelmetCalls;
            public int MountCalls;
            public int LogCalls;
            public int SaveCalls;
            public PlayerEquipSlot LastSlot;
            public int LastOldVariant;
            public int LastNewVariant;
            public int LastItemId;
            public int LastOldItemId;
            public int LastNewItemId;
            public int LastSaveItemId;
            public int LastSaveVariant;

            public void RefreshVisual(PlayerEquipSlot slot, int oldVariant, int newVariant, int itemId)
            {
                RefreshCalls++;
                LastSlot = slot;
                LastOldVariant = oldVariant;
                LastNewVariant = newVariant;
                LastItemId = itemId;
            }
            public void PlayEquipSFX(PlayerEquipSlot slot, int itemId) { SfxCalls++; }
            public void OnWeaponChanged(int oldItemId, int newItemId, int newVariant)
            {
                WeaponCalls++;
                LastOldItemId = oldItemId;
                LastNewItemId = newItemId;
                LastNewVariant = newVariant;
            }
            public void OnArmorChanged(int oldVariant, int newVariant, int itemId) { ArmorCalls++; }
            public void OnHelmetChanged(int oldVariant, int newVariant, int itemId) { HelmetCalls++; }
            public void OnMountChanged(int oldVariant, int newVariant, int itemId) { MountCalls++; }
            public void LogEquipEvent(PlayerEquipSlot slot, int oldVariant, int newVariant, int itemId) { LogCalls++; }
            public void SaveEquipmentState(int itemId, PlayerEquipSlot slot, int variant)
            {
                SaveCalls++;
                LastSaveItemId = itemId;
                LastSaveVariant = variant;
            }
        }

        // ── Default variant ─────────────────────────────────────────────────

        [Test]
        public void GetVariant_EmptySlot_ReturnsDefault()
        {
            var svc = new PlayerEquipmentService();
            // Body default = 1
            Assert.AreEqual(1, svc.GetVariant(PlayerEquipSlot.Body));
        }

        [Test]
        public void GetArmorVariant_NoEquip_Returns19()
        {
            var svc = new PlayerEquipmentService();
            Assert.AreEqual(1, svc.GetArmorVariant());
        }

        [Test]
        public void GetHelmetVariant_NoEquip_Returns19()
        {
            var svc = new PlayerEquipmentService();
            Assert.AreEqual(28, svc.GetHelmetVariant());
        }

        // ── Equip ───────────────────────────────────────────────────────────

        [Test]
        public void Equip_Body_StoresVariant()
        {
            var svc = new PlayerEquipmentService();
            svc.Equip(PlayerEquipSlot.Body, 25, 1234);
            Assert.AreEqual(25, svc.GetVariant(PlayerEquipSlot.Body));
        }

        [Test]
        public void Equip_FiresOnEquipChangedEvent()
        {
            var svc = new PlayerEquipmentService();
            int fired = 0;
            EquipChangeEvent last = default;
            svc.OnEquipChanged += e =>
            {
                fired++;
                last = e;
            };
            svc.Equip(PlayerEquipSlot.Body, 25, 1234);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(PlayerEquipSlot.Body, last.slot);
            Assert.AreEqual(1, last.oldVariant);
            Assert.AreEqual(25, last.newVariant);
            Assert.AreEqual(0, last.oldItemId);
            Assert.AreEqual(1234, last.itemId);
        }

        [Test]
        public void Equip_SameItemAndVariant_NoEvent()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService(host);
            svc.Equip(PlayerEquipSlot.Body, 25, 1234);
            int fired = 0;
            svc.OnEquipChanged += e => fired++;
            svc.Equip(PlayerEquipSlot.Body, 25, 1234);
            Assert.AreEqual(0, fired);
            Assert.AreEqual(1, host.RefreshCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void Equip_SameVariantDifferentItem_FiresOnce()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService(host);
            svc.Equip(PlayerEquipSlot.Body, 25, 1234);
            int fired = 0;
            EquipChangeEvent last = default;
            svc.OnEquipChanged += e =>
            {
                fired++;
                last = e;
            };

            svc.Equip(PlayerEquipSlot.Body, 25, 9999);

            Assert.AreEqual(1, fired);
            Assert.AreEqual(2, host.RefreshCalls);
            Assert.AreEqual(2, host.SaveCalls);
            Assert.AreEqual(25, last.oldVariant);
            Assert.AreEqual(25, last.newVariant);
            Assert.AreEqual(1234, last.oldItemId);
            Assert.AreEqual(9999, last.itemId);
            Assert.AreEqual(9999, host.LastItemId);
            Assert.AreEqual(9999, host.LastSaveItemId);
        }

        [Test]
        public void Equip_Weapon_StoresItemId()
        {
            var svc = new PlayerEquipmentService();
            svc.Equip(PlayerEquipSlot.Weapon, 5, 7777);
            Assert.AreEqual(PcWeaponType.ShortWeapon, svc.GetCurrentWeaponType());
        }

        // ── Host dispatch ──────────────────────────────────────────────────

        [Test]
        public void Equip_DispatchesHostRefresh()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService(host);
            svc.Equip(PlayerEquipSlot.Body, 25, 1234);
            Assert.AreEqual(1, host.RefreshCalls);
            Assert.AreEqual(PlayerEquipSlot.Body, host.LastSlot);
            Assert.AreEqual(25, host.LastNewVariant);
            Assert.AreEqual(1234, host.LastItemId);
        }

        [Test]
        public void Equip_DispatchesAllCallbacks()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService(host);
            svc.Equip(PlayerEquipSlot.Body, 25, 1234);
            Assert.AreEqual(1, host.RefreshCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void Equip_Body_DispatchesOnArmorChanged()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService(host);
            svc.Equip(PlayerEquipSlot.Body, 25, 1234);
            Assert.AreEqual(1, host.ArmorCalls);
            Assert.AreEqual(0, host.WeaponCalls);
            Assert.AreEqual(0, host.HelmetCalls);
            Assert.AreEqual(0, host.MountCalls);
        }

        [Test]
        public void Equip_Head_DispatchesOnHelmetChanged()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService(host);
            svc.Equip(PlayerEquipSlot.Head, 30, 5555);
            Assert.AreEqual(1, host.HelmetCalls);
            Assert.AreEqual(0, host.ArmorCalls);
        }

        [Test]
        public void Equip_Weapon_DispatchesOnWeaponChanged()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService(host);
            svc.Equip(PlayerEquipSlot.Weapon, 5, 7777);
            Assert.AreEqual(1, host.WeaponCalls);
            Assert.AreEqual(0, host.LastOldItemId);
            Assert.AreEqual(7777, host.LastNewItemId);
        }

        [Test]
        public void Equip_WeaponSameItemAndVariant_Idempotent()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService(host);
            svc.Equip(PlayerEquipSlot.Weapon, 5, 7777);
            int fired = 0;
            svc.OnEquipChanged += e => fired++;

            svc.Equip(PlayerEquipSlot.Weapon, 5, 7777);

            Assert.AreEqual(0, fired);
            Assert.AreEqual(1, host.RefreshCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SaveCalls);
            Assert.AreEqual(1, host.WeaponCalls);
            Assert.AreEqual(PcWeaponType.ShortWeapon, svc.GetCurrentWeaponType());
        }

        [Test]
        public void Equip_WeaponSameVariantDifferentItem_ReportsTrueOldNewItemIds()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService();
            svc.Equip(PlayerEquipSlot.Weapon, 5, 1111);
            svc.AttachHost(host);
            int fired = 0;
            EquipChangeEvent last = default;
            svc.OnEquipChanged += e =>
            {
                fired++;
                last = e;
            };

            svc.Equip(PlayerEquipSlot.Weapon, 5, 2222);

            Assert.AreEqual(1, fired);
            Assert.AreEqual(1, host.RefreshCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SaveCalls);
            Assert.AreEqual(1, host.WeaponCalls);
            Assert.AreEqual(5, last.oldVariant);
            Assert.AreEqual(5, last.newVariant);
            Assert.AreEqual(1111, last.oldItemId);
            Assert.AreEqual(2222, last.itemId);
            Assert.AreEqual(1111, host.LastOldItemId);
            Assert.AreEqual(2222, host.LastNewItemId);
            Assert.AreEqual(2222, host.LastItemId);
            Assert.AreEqual(2222, host.LastSaveItemId);
            Assert.AreEqual(5, host.LastSaveVariant);
        }

        [Test]
        public void Equip_WeaponDefaultVariantDifferentItem_FiresAndMarksEquipped()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService(host);
            int fired = 0;
            EquipChangeEvent last = default;
            svc.OnEquipChanged += e =>
            {
                fired++;
                last = e;
            };

            svc.Equip(PlayerEquipSlot.Weapon, 0, 9000);

            Assert.AreEqual(1, fired);
            Assert.IsTrue(svc.IsEquipped(PlayerEquipSlot.Weapon));
            Assert.AreEqual(0, svc.GetVariant(PlayerEquipSlot.Weapon));
            Assert.AreEqual(1, host.RefreshCalls);
            Assert.AreEqual(1, host.WeaponCalls);
            Assert.AreEqual(0, last.oldVariant);
            Assert.AreEqual(0, last.newVariant);
            Assert.AreEqual(0, last.oldItemId);
            Assert.AreEqual(9000, last.itemId);
            Assert.AreEqual(0, host.LastOldItemId);
            Assert.AreEqual(9000, host.LastNewItemId);
            Assert.AreEqual(9000, host.LastSaveItemId);
            Assert.AreEqual(0, host.LastSaveVariant);
        }

        [Test]
        public void Equip_Mount_DispatchesOnMountChanged()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService(host);
            svc.Equip(PlayerEquipSlot.Mount, 12, 9999);
            Assert.AreEqual(1, host.MountCalls);
        }

        [Test]
        public void Equip_Offhand_NoSlotSpecificDispatch()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService(host);
            svc.Equip(PlayerEquipSlot.Offhand, 3, 1234);
            Assert.AreEqual(0, host.ArmorCalls);
            Assert.AreEqual(0, host.WeaponCalls);
            Assert.AreEqual(0, host.HelmetCalls);
            Assert.AreEqual(0, host.MountCalls);
            Assert.AreEqual(1, host.RefreshCalls); // but generic dispatch
        }

        [Test]
        public void Equip_WithoutHost_DoesNotThrow()
        {
            var svc = new PlayerEquipmentService();
            Assert.DoesNotThrow(() => svc.Equip(PlayerEquipSlot.Body, 25, 1234));
        }

        // ── Unequip ────────────────────────────────────────────────────────

        [Test]
        public void Unequip_ResetsToDefault()
        {
            var svc = new PlayerEquipmentService();
            svc.Equip(PlayerEquipSlot.Body, 25, 1234);
            svc.Unequip(PlayerEquipSlot.Body);
            Assert.AreEqual(1, svc.GetVariant(PlayerEquipSlot.Body));
            Assert.IsFalse(svc.IsEquipped(PlayerEquipSlot.Body));
        }

        [Test]
        public void Unequip_WeaponDefaultVariantItem_ClearsIdentityAndDispatches()
        {
            var host = new FakeHost();
            var svc = new PlayerEquipmentService(host);
            int fired = 0;
            EquipChangeEvent last = default;
            svc.OnEquipChanged += e =>
            {
                fired++;
                last = e;
            };

            svc.Equip(PlayerEquipSlot.Weapon, 0, 9000);
            svc.Unequip(PlayerEquipSlot.Weapon);

            Assert.AreEqual(2, fired);
            Assert.IsFalse(svc.IsEquipped(PlayerEquipSlot.Weapon));
            Assert.AreEqual(0, svc.GetVariant(PlayerEquipSlot.Weapon));
            Assert.AreEqual(PcWeaponType.EmptyHand, svc.GetCurrentWeaponType());
            Assert.AreEqual(2, host.RefreshCalls);
            Assert.AreEqual(2, host.SfxCalls);
            Assert.AreEqual(2, host.LogCalls);
            Assert.AreEqual(2, host.SaveCalls);
            Assert.AreEqual(2, host.WeaponCalls);
            Assert.AreEqual(0, last.oldVariant);
            Assert.AreEqual(0, last.newVariant);
            Assert.AreEqual(9000, last.oldItemId);
            Assert.AreEqual(0, last.itemId);
            Assert.AreEqual(9000, host.LastOldItemId);
            Assert.AreEqual(0, host.LastNewItemId);
            Assert.AreEqual(0, host.LastItemId);
            Assert.AreEqual(0, host.LastSaveItemId);
            Assert.AreEqual(0, host.LastSaveVariant);
        }

        // ── AttachHost ─────────────────────────────────────────────────────

        [Test]
        public void AttachHost_Replaces()
        {
            var host1 = new FakeHost();
            var host2 = new FakeHost();
            var svc = new PlayerEquipmentService(host1);
            svc.AttachHost(host2);
            svc.Equip(PlayerEquipSlot.Body, 25, 1234);
            Assert.AreEqual(0, host1.RefreshCalls);
            Assert.AreEqual(1, host2.RefreshCalls);
        }

        // ── Static mappings ────────────────────────────────────────────────

        [Test]
        public void ItemToBodyVariant_BuiltInTest_Returns19()
        {
            Assert.AreEqual(19, PlayerEquipmentService.ItemToBodyVariant(2001));
            Assert.AreEqual(19, PlayerEquipmentService.ItemToBodyVariant(2004));
        }

        [Test]
        public void ItemToBodyVariant_Range0_Returns8()
        {
            Assert.AreEqual(8, PlayerEquipmentService.ItemToBodyVariant(100));
        }

        [Test]
        public void ItemToWeaponVariant_Range1_ReturnsShort()
        {
            Assert.AreEqual(MalePlayerSpriteCatalog.ShortWeaponVariant,
                PlayerEquipmentService.ItemToWeaponVariant(1));
            Assert.AreEqual(MalePlayerSpriteCatalog.ShortWeaponVariant,
                PlayerEquipmentService.ItemToWeaponVariant(21));
        }

        [Test]
        public void ItemToWeaponVariant_Range22_ReturnsStaff()
        {
            Assert.AreEqual(MalePlayerSpriteCatalog.StaffWeaponVariant,
                PlayerEquipmentService.ItemToWeaponVariant(22));
        }

        [Test]
        public void ItemToWeaponVariant_Range42_ReturnsDual()
        {
            Assert.AreEqual(MalePlayerSpriteCatalog.DualWeaponVariant,
                PlayerEquipmentService.ItemToWeaponVariant(42));
        }

        [Test]
        public void ItemToWeaponVariant_OutOfRange_ReturnsEmpty()
        {
            Assert.AreEqual(MalePlayerSpriteCatalog.EmptyWeaponVariant,
                PlayerEquipmentService.ItemToWeaponVariant(999));
        }

        [Test]
        public void WeaponVariantToType_MapsEveryCanonicalBaseWeaponVariant()
        {
            Assert.AreEqual(PcWeaponType.EmptyHand, PlayerEquipmentService.WeaponVariantToType(0));
            AssertWeaponVariants(PcWeaponType.ShortWeapon, 1, 2, 3, 4, 5, 6, 19, 20, 21, 22);
            AssertWeaponVariants(PcWeaponType.LongWeapon, 7, 8, 9, 10, 11, 12, 23, 24, 25, 26);
            AssertWeaponVariants(PcWeaponType.DualWeapon, 13, 14, 15, 16, 17, 18, 27, 28, 29, 30);
            Assert.AreEqual(PcWeaponType.EmptyHand, PlayerEquipmentService.WeaponVariantToType(-1));
            Assert.AreEqual(PcWeaponType.EmptyHand, PlayerEquipmentService.WeaponVariantToType(31));
        }

        [TestCase(0, 0, PcWeaponType.ShortWeapon)]
        [TestCase(0, 1, PcWeaponType.ShortWeapon)]
        [TestCase(0, 2, PcWeaponType.LongWeapon)]
        [TestCase(0, 3, PcWeaponType.LongWeapon)]
        [TestCase(0, 4, PcWeaponType.DualWeapon)]
        [TestCase(0, 5, PcWeaponType.DualWeapon)]
        [TestCase(1, 0, PcWeaponType.HiddenWeapon)]
        [TestCase(1, 99, PcWeaponType.HiddenWeapon)]
        public void PcItemTupleToWeaponType_MapsCanonicalDetailAndParticularRows(
            int detailType, int particularType, PcWeaponType expected)
        {
            Assert.AreEqual(expected,
                PlayerEquipmentService.PcItemTupleToWeaponType(0, detailType, particularType));
        }

        [Test]
        public void PcItemTupleToWeaponType_UnknownRowsFailClosedToEmptyHand()
        {
            Assert.AreEqual(PcWeaponType.EmptyHand, PlayerEquipmentService.PcItemTupleToWeaponType(1, 0, 0));
            Assert.AreEqual(PcWeaponType.EmptyHand, PlayerEquipmentService.PcItemTupleToWeaponType(0, 0, 6));
            Assert.AreEqual(PcWeaponType.EmptyHand, PlayerEquipmentService.PcItemTupleToWeaponType(0, 2, 0));
        }

        [Test]
        public void ItemToHelmetVariant_BuiltInTest_Returns19()
        {
            Assert.AreEqual(19, PlayerEquipmentService.ItemToHelmetVariant(3001));
        }

        [Test]
        public void ItemToHelmetVariant_KnownHelmet_Returns10()
        {
            // 10 and 11 are listed in the special helmet range
            Assert.AreEqual(10, PlayerEquipmentService.ItemToHelmetVariant(10));
            Assert.AreEqual(10, PlayerEquipmentService.ItemToHelmetVariant(11));
        }

        [Test]
        public void ItemToHelmetVariant_Generic_Returns9()
        {
            // Any id in 0-139 that's not in the special list returns 9
            Assert.AreEqual(9, PlayerEquipmentService.ItemToHelmetVariant(50));
        }

        private static void AssertWeaponVariants(PcWeaponType expected, params int[] variants)
        {
            foreach (int variant in variants)
                Assert.AreEqual(expected, PlayerEquipmentService.WeaponVariantToType(variant), $"variant {variant}");
        }
    }
}
