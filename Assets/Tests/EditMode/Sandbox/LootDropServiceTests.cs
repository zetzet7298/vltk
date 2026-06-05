using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class LootDropServiceTests
    {
        private static string SampleDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcDropRate");

        [Test]
        public void ResolveDrops_AtLeastOneItemForSampleNpc()
        {
            var reg = new DropRateRegistry();
            reg.LoadDirectory(SampleDir);

            var service = new LootDropService(null);
            service.AttachRegistry(reg);

            var drops = service.ResolveDrops(42, 50, 1234);
            Assert.IsNotNull(drops, "ResolveDrops should never return null");
            Assert.Greater(drops.Count, 0, "Sample NPC level 50 must produce at least one drop from the registry");
            foreach (var d in drops)
            {
                Assert.Greater(d.itemId, 0, "Each drop must have a positive itemId");
                Assert.Greater(d.count, 0, "Each drop must have count > 0");
            }
        }

        [Test]
        public void ComputeDrops_FallsBackToDefaultTableWhenRegistryEmpty()
        {
            var service = new LootDropService(null);
            var results = service.ComputeDrops(31, 5, 99);
            Assert.IsNotNull(results);
            Assert.Greater(results.Count, 0, "Default table should still produce at least one silver drop");
        }

        [Test]
        public void ResolveDrop_ExposesItemIds()
        {
            var reg = new DropRateRegistry();
            reg.LoadDirectory(SampleDir);
            var service = new LootDropService(null);
            service.AttachRegistry(reg);
            var ids = service.ResolveDrop(42, 50);
            Assert.Greater(ids.Count, 0, "ResolveDrop convenience method should return at least one itemId for sample NPC");
        }
    }
}
