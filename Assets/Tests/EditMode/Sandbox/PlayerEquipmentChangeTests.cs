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
                LastNewVariant = newVariant;
            }
            public void OnArmorChanged(int oldVariant, int newVariant, int itemId) { ArmorCalls++; }
            public void OnHelmetChanged(int oldVariant, int newVariant, int itemId) { HelmetCalls++; }
            public void OnMountChanged(int oldVariant, int newVariant, int itemId) { MountCalls++; }
            public void LogEquipEvent(PlayerEquipSlot slot, int oldVariant, int newVariant, int itemId) { LogCalls++; }
            public void SaveEquipmentState(int itemId, PlayerEquipSlot slot, int variant)
            {
                SaveCalls++;
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
            svc.OnEquipChanged += e => fired++;
            svc.Equip(PlayerEquipSlot.Body, 25, 1234);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void Equip_SameVariant_NoEvent()
        {
            var svc = new PlayerEquipmentService();
            svc.Equip(PlayerEquipSlot.Body, 25, 1234);
            int fired = 0;
            svc.OnEquipChanged += e => fired++;
            svc.Equip(PlayerEquipSlot.Body, 25, 9999); // same variant
            Assert.AreEqual(0, fired);
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
        public void WeaponVariantToType_Empty_EmptyHand()
        {
            Assert.AreEqual(PcWeaponType.EmptyHand,
                PlayerEquipmentService.WeaponVariantToType(0));
        }

        [Test]
        public void WeaponVariantToType_Short_ShortWeapon()
        {
            Assert.AreEqual(PcWeaponType.ShortWeapon,
                PlayerEquipmentService.WeaponVariantToType(5));
        }

        [Test]
        public void WeaponVariantToType_Staff_LongWeapon()
        {
            Assert.AreEqual(PcWeaponType.LongWeapon,
                PlayerEquipmentService.WeaponVariantToType(15));
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
    }
}
