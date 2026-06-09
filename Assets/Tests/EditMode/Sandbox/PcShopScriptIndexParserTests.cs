using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcShopScriptIndexParserTests
    {
        private const string MainSourceRoot = "Server 6.0/server/home_jxser/server1";
        private const string BachKimSourceRoot = "Server 6.0/server/home_jxser_bachkim_6.0/server1";

        private static string IndexPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcShopScript/shop_script_index.txt");

        private static string IndexDir => Path.GetDirectoryName(IndexPath);

        [Test]
        public void ParseFile_ReadsOnlyScopedPcShopRelatedLuaSourceCatalog()
        {
            Assert.IsTrue(File.Exists(IndexPath));
            var rows = PcShopScriptIndexParser.ParseFile(IndexPath);

            Assert.AreEqual(32, rows.Count,
                "Two PC server roots each contain 16 Lua files across script/shop, script/item/dynamic_shop, and script/item/ib_shop.");
            Assert.AreEqual(32, rows.FindAll(r => r.isLua).Count);
            Assert.AreEqual(0, rows.FindAll(r => r.extension != "lua").Count);
        }

        [Test]
        public void Registry_PreservesRepresentativeSourceFileHashesAndSizes()
        {
            var registry = PcShopScriptIndexParser.BuildRegistry(IndexDir);
            var checkMap = registry.GetBySourceAndRelativePath(MainSourceRoot, "script/shop/shop_checkmap.lua");
            var dynamicItem = registry.GetBySourceAndRelativePath(MainSourceRoot, "script/item/dynamic_shop/item.lua");
            var ibShopBook = registry.GetBySourceAndRelativePath(MainSourceRoot, "script/item/ib_shop/jinengmiji_90.lua");

            Assert.IsNotNull(checkMap);
            Assert.AreEqual("script/shop", checkMap.sourceSubdir);
            Assert.AreEqual(3162, checkMap.sizeBytes);
            Assert.AreEqual("9c3d893ae7fb3dcd45a45ef21e87917c25e632945344cac986e420c3e03699a7", checkMap.sha256);

            Assert.IsNotNull(dynamicItem);
            Assert.AreEqual(5102, dynamicItem.sizeBytes);
            Assert.AreEqual("815f68b14d43dbb58c1baa8d88f3161f071f422dff9659774a5bd06f3772e8d0", dynamicItem.sha256);

            Assert.IsNotNull(ibShopBook);
            Assert.AreEqual(4610, ibShopBook.sizeBytes);
            Assert.AreEqual("615534b2acd6fc5c95dc5c5f7603aaf1876f70856d4152545364c36130a9cfaa", ibShopBook.sha256);
        }

        [Test]
        public void Registry_TracksPcRootDuplicatesWithoutClaimingRuntimeSemantics()
        {
            var registry = PcShopScriptIndexParser.BuildRegistry(IndexDir);

            Assert.AreEqual(32, registry.Count);
            Assert.AreEqual(32, registry.LuaFileCount);
            Assert.AreEqual(16, registry.UniqueRelativePathCount);
            Assert.AreEqual(16, registry.DuplicateRelativePathCount);
            Assert.AreEqual(2, registry.GetByRelativePath("script/item/ib_shop/yirongmishu.lua").Count);
            Assert.IsNotNull(registry.GetBySourceAndRelativePath(BachKimSourceRoot, "script/item/ib_shop/yirongmishu.lua"));
            Assert.Greater(registry.TotalSizeBytes, 0L,
                "Total bytes are catalog evidence only; parser/service do not execute Lua or enable shop runtime behavior.");
        }

        [Test]
        public void Registry_GroupsByRequestedPcSubdirectories()
        {
            var registry = PcShopScriptIndexParser.BuildRegistry(IndexDir);

            Assert.AreEqual(2, registry.GetBySourceSubdir("script/shop").Count);
            Assert.AreEqual(10, registry.GetBySourceSubdir("script/item/dynamic_shop").Count);
            Assert.AreEqual(20, registry.GetBySourceSubdir("script/item/ib_shop").Count);
        }

        [Test]
        public void Service_LoadsDefaultStreamingAssetsIndex()
        {
            var service = ShopScriptIndexService.LoadFromStreamingAssets();

            Assert.AreEqual(32, service.Count);
            Assert.AreEqual(16, service.UniqueRelativePathCount);
            Assert.AreEqual(16, service.DuplicateRelativePathCount);
            Assert.AreEqual(10, service.GetSourceSubdirCount("script/item/dynamic_shop"));
            Assert.AreEqual(2, service.GetByRelativePath("script/shop/shop_checkmap.lua").Count);
        }

        [Test]
        public void MissingInputs_ReturnEmptyCatalog()
        {
            Assert.AreEqual(0, PcShopScriptIndexParser.ParseFile("/tmp/not-a-real-shop-script-index.txt").Count);
            Assert.AreEqual(0, PcShopScriptIndexParser.BuildRegistry("/tmp/not-a-real-shop-script-index-dir").Count);
            Assert.AreEqual(0, ShopScriptIndexService.LoadFromFile("/tmp/not-a-real-shop-script-index.txt").Count);
        }
    }
}
