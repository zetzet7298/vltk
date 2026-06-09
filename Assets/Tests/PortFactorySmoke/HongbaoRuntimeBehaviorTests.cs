using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.PortFactorySmoke
{
    public class HongbaoRuntimeBehaviorTests
    {
        private InventoryService _inventory;
        private HongbaoService _hongbaoData;
        private CityHongbaoService _cityHongbaoData;
        private HongbaoRuntimeBehaviorService _service;

        [SetUp]
        public void Setup()
        {
            var registry = new PcHongbaoRegistry();
            registry.Register(new PcHongbaoEntry
            {
                id = 1,
                proba = 100,
                type = HongbaoOpenResultService.PcItemType,
                itemGenre = 6,
                itemDetail = 1,
                itemParticular = 200,
                msg = "Test item"
            });
            _hongbaoData = new HongbaoService(registry);

            var cityRegistry = new PcCityHongbaoRegistry();
            cityRegistry.Add(new PcCityHongbaoEntry
            {
                Id = 1,
                Proba = 100,
                Type = CityHongbaoOpenResultService.PcItemType,
                Genre = 6,
                Detail = 1,
                Particular = 300,
                Msg = "Test city item"
            });
            _cityHongbaoData = new CityHongbaoService(cityRegistry);

            // Mock database doesn't actually populate definitions by default, but InventoryService allows adding if we provide an ItemContractImporter.
            // Wait, InventoryService uses db.ResolvePcItem. We can just mock it or rely on the actual behavior if we add stub items, but let's test if inventory tracks the AddPcItem calls.
            // Wait, AddPcItem checks if ResolvePcItem returns null. Since db is null, it might return false. Let's make a mock item importer or just check the call surface.
            // Actually we can pass null for ItemContractImporter, which makes ResolvePcItem return null, thus failing to add.
            // Let's create a dummy ItemContractImporter or just use the inventory count to verify space.
            var stubDb = new ItemContractImporter();
            stubDb.LoadRecords(new System.Collections.Generic.List<ItemContractRecord>
            {
                new ItemContractRecord { id = 1001, genre = 6, detail = 1, part = 200 },
                new ItemContractRecord { id = 1002, genre = 6, detail = 1, part = 300 }
            });

            _inventory = new InventoryService(stubDb);
            _service = new HongbaoRuntimeBehaviorService(_inventory, _hongbaoData, _cityHongbaoData);
        }

        [Test]
        public void OpenHongbao_WithSpace_MutatesInventory()
        {
            var result = _service.OpenHongbao(50, "Tester");
            Assert.AreEqual(HongbaoOpenStatus.RewardSelected, result.Status);
            Assert.IsTrue(_inventory.HasPcItem(6, 1, 200));
        }

        [Test]
        public void OpenHongbao_NoSpace_Fails()
        {
            for (int i = 0; i < 28; i++) // Fill inventory
                _inventory.AddItem(1001, 1);

            var result = _service.OpenHongbao(50, "Tester");
            Assert.AreEqual(HongbaoOpenStatus.InsufficientInventorySpace, result.Status);
            // Count shouldn't exceed max since it was already full
            Assert.AreEqual(28, _inventory.Inventory.Count);
        }

        [Test]
        public void OpenCityHongbao_WithSpace_MutatesInventory()
        {
            var result = _service.OpenCityHongbao(50, "Tester");
            Assert.AreEqual(CityHongbaoOpenStatus.RewardSelected, result.Status);
            Assert.IsTrue(_inventory.HasPcItem(6, 1, 300));
        }
    }
}
