using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.Sandbox.ItemData;

namespace VLTK.Tests.Sandbox
{
    public class GmTestServerItemTests
    {
        [Test]
        public void MagicScriptParser_LoadsGmTestServerToken()
        {
            string path = Path.Combine(Application.dataPath, "StreamingAssets/Reference/PcItemFull/magicscript.txt");
            var rows = PcMagicScriptItemParser.ParseFile(path);
            var token = rows.Find(i => i.itemGenre == 6 && i.detailType == 1 && i.particularType == 4890);

            Assert.IsNotNull(token);
            Assert.AreEqual(4890, token.itemId);
            Assert.AreEqual("Lệnh bài GM Test Server", token.DisplayName);
            Assert.AreEqual("Lệnh bài này chỉ được GM sử dụng.", token.description);
            Assert.AreEqual(@"\script\item\gmroleitem2.lua", token.scriptPath);
            Assert.AreEqual(@"\spr\item\script\yupai_haozhao.spr", token.iconSourceId.sourcePath);
        }

        [Test]
        public void Importer_ResolvesByPcTuple()
        {
            var importer = new ItemContractImporter();
            importer.Import(new ItemContractBundle { items = new List<ItemDefinition> { PcItem(4890, "GM") } });

            var token = importer.ResolvePcItem(6, 1, 4890);
            Assert.IsNotNull(token);
            Assert.AreEqual(4890, token.itemId);
        }

        [Test]
        public void Service_MainMenuMatchesPcOrder()
        {
            var svc = new GmTestServerItemService(null, null, GmAccessService.AllowForTests());
            var menu = svc.GetMenu(GmTestServerItemService.MainMenuId);

            Assert.AreEqual("Test Server (mọi thứ cần ở đây nha)", menu[0].label);
            Assert.AreEqual("Tạo Bãi", menu[1].label);
            Assert.AreEqual("Xóa toàn bộ item trong hành trang", menu[2].label);
            Assert.AreEqual("Hỗ trợ làm nhiệm vụ hoàng kim nhanh", menu[3].label);
            Assert.AreEqual("shop", menu[menu.Count - 1].label);
        }

        [Test]
        public void Service_DeniesNonGmUse()
        {
            var svc = new GmTestServerItemService(null, null, GmAccessService.DenyForTests());
            var result = svc.Execute("SkillsSystem");
            Assert.AreEqual(GmItemActionStatus.Blocked, result.status);
        }

        [Test]
        public void Service_ClearInventoryRequiresConfirmThenRestoresPcGmItems()
        {
            var importer = new ItemContractImporter();
            importer.Import(new ItemContractBundle
            {
                items = new List<ItemDefinition>
                {
                    PcItem(438, "Thổ Địa Phù"),
                    PcItem(1266, "Thần Hành Phù"),
                    PcItem(4850, "Lệnh bài GM Quản Lý Chức Năng"),
                    PcItem(4890, "Lệnh bài GM Test Server"),
                    PcItem(4852, "Túi máu Tân Thủ"),
                    PcItem(4908, "GM extra"),
                }
            });
            var inv = new InventoryService(importer);
            inv.AddPcItem(6, 1, 4890);
            var svc = new GmTestServerItemService(null, inv, GmAccessService.AllowForTests());

            var confirm = svc.Execute("XoaItemHanhTrangGM");
            Assert.AreEqual(GmItemActionStatus.NeedsConfirmation, confirm.status);

            var done = svc.Execute("XoaItemHanhTrangGM", confirmed: true);
            Assert.IsTrue(done.success);
            Assert.AreEqual(6, inv.Inventory.Count);
            Assert.IsTrue(inv.HasPcItem(6, 1, 4890));
            Assert.IsTrue(inv.HasPcItem(6, 1, 1266));
        }


        [Test]
        public void TeleportCatalog_LoadsAllPcMapAliases()
        {
            var manager = new MapManager();
            manager.LoadCatalog();
            var svc = new GmTeleportCatalogService(manager, PcMapDir());
            var destinations = svc.GetAllDestinations();
            var ids = new HashSet<int>();
            foreach (var d in destinations) ids.Add(d.mapId);

            Assert.GreaterOrEqual(ids.Count, 1005);
            Assert.IsNotNull(svc.FindByMapId(1));
            Assert.IsNotNull(svc.FindByMapId(1009));
        }

        [Test]
        public void TeleportCatalog_UsesRevivePosBeforeFallback()
        {
            var manager = new MapManager();
            manager.LoadCatalog();
            var svc = new GmTeleportCatalogService(manager, PcMapDir());
            var dest = svc.FindByMapId(1);
            var expected = MapEnemyDatabase.MpsToWorld(51104, 102592);

            Assert.IsNotNull(dest);
            Assert.AreEqual(expected.x, dest.worldPosition.x, 0.01f);
            Assert.AreEqual(expected.y, dest.worldPosition.y, 0.01f);
            StringAssert.Contains("revivepos", dest.coordinateSource);
        }

        [Test]
        public void TeleportCatalog_UsesWaypointCellCoordinatesWhenNoRevivePos()
        {
            var manager = new MapManager();
            manager.LoadCatalog();
            var svc = new GmTeleportCatalogService(manager, PcMapDir());
            var dest = svc.FindByMapId(4);
            var expected = MapEnemyDatabase.MpsToWorld(1596 * 32, 3282 * 32);

            Assert.IsNotNull(dest);
            Assert.AreEqual(expected.x, dest.worldPosition.x, 0.01f);
            Assert.AreEqual(expected.y, dest.worldPosition.y, 0.01f);
            StringAssert.Contains("waypoint", dest.coordinateSource);
        }

        [Test]
        public void TeleportCatalog_ScriptTravelConvertsPcNewWorldCells()
        {
            Assert.IsTrue(GmTeleportCatalogService.TryGetScriptDestination("goto_tinsu", null, out var dest));
            var expected = MapEnemyDatabase.MpsToWorld(3024 * 32, 5086 * 32);

            Assert.AreEqual(11, dest.mapId);
            Assert.AreEqual(expected.x, dest.worldPosition.x, 0.01f);
            Assert.AreEqual(expected.y, dest.worldPosition.y, 0.01f);
        }

        [Test]
        public void Service_TravelMenuIncludesAllMapsBrowserAction()
        {
            var svc = new GmTestServerItemService(null, null, GmAccessService.AllowForTests());
            var menu = svc.GetMenu(GmTestServerItemService.TravelMenuId);

            Assert.AreEqual("Tất cả bản đồ", menu[0].label);
            Assert.AreEqual(GmTestServerItemService.AllMapsActionId, menu[0].actionId);
            Assert.AreEqual(GmTestServerItemService.AllMapsActionId, svc.Execute(menu[0].actionId).message);
        }

        private static string PcMapDir()
            => Path.Combine(Application.dataPath, "StreamingAssets/Reference/PcMap");

        private static ItemDefinition PcItem(int particular, string name)
        {
            return new ItemDefinition
            {
                itemId = particular,
                itemGenre = 6,
                detailType = 1,
                particularType = particular,
                nameNormalized = name,
                scriptPath = particular == 4890 ? @"\script\item\gmroleitem2.lua" : @"\script\item\noscript.lua",
            };
        }
    }
}
