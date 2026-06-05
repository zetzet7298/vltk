using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class DropRateRegistryTests
    {
        private static string SampleDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcDropRate");

        [Test]
        public void LoadDirectory_PicksUpAllSampleFiles()
        {
            var reg = new DropRateRegistry();
            reg.LoadDirectory(SampleDir);

            Assert.GreaterOrEqual(reg.TableCount, 3, "Expected at least 3 sample tables (10, 50, datushashiwei)");

            Assert.IsTrue(reg.TryGetTable("npcdroprate10_sample", out var t10));
            Assert.IsTrue(reg.TryGetTable("npcdroprate50_sample", out var t50));
            Assert.IsTrue(reg.TryGetTable("datushashiwei_sample", out var boss));
            Assert.IsNotNull(t10);
            Assert.IsNotNull(t50);
            Assert.IsNotNull(boss);
            Assert.AreEqual(33000, t10.randRange);
            Assert.AreEqual(200000, t50.randRange);
            Assert.AreEqual(1000000, boss.randRange);
        }

        [Test]
        public void GetTablesForLevel_ReturnsAtLeastOneForMidLevelNpc()
        {
            var reg = new DropRateRegistry();
            reg.LoadDirectory(SampleDir);

            var for50 = reg.GetTablesForLevel(50).ToList();
            Assert.Greater(for50.Count, 0, "Level 50 must match at least one loaded table");
        }

        [Test]
        public void GetTable_ReturnsNullForUnknownName()
        {
            var reg = new DropRateRegistry();
            reg.LoadDirectory(SampleDir);
            Assert.IsNull(reg.GetTable("does_not_exist"));
        }

        [Test]
        public void SpecialTable_ResolvesByNameOnly()
        {
            var reg = new DropRateRegistry();
            reg.LoadDirectory(SampleDir);

            var datu = reg.GetTable("datushashiwei_sample");
            Assert.IsNotNull(datu);
            CollectionAssert.AllItemsAreInstancesOfType(reg.SpecialTables.ToList(), typeof(DropRateTable));
        }
    }
}
