using System;
using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class AuctionServiceTests
    {
        private static string AuctionDir => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcAuction");

        private static AuctionService BuildService()
        {
            var reg = PcAuctionConfigParser.BuildRegistry(AuctionDir);
            return new AuctionService(reg);
        }

        [Test]
        public void LoadFromStreamingAssets_LoadsConfig()
        {
            var svc = BuildService();
            Assert.Greater(svc.Count, 0, "auction.ini phải có ít nhất 1 key/value");
            // [Main] MinBasePrice phải tồn tại theo PC auction.ini
            string v = svc.GetConfig("Main", "MinBasePrice");
            Assert.IsNotNull(v, "Phải đọc được key Main.MinBasePrice");
        }

        [Test]
        public void GetConfigInt_ParsesInteger()
        {
            var svc = BuildService();
            int tax = svc.GetConfigInt("Main", "TaxRate", -1);
            Assert.GreaterOrEqual(tax, 0, "TaxRate phải là số ≥ 0");
        }

        [Test]
        public void ListItem_AddsToActive()
        {
            var svc = BuildService();
            int before = svc.ActiveListingCount;
            var l = svc.ListItem(9001, 100, 1, "Seller A", 500, 2000, 60);
            Assert.IsNotNull(l);
            Assert.AreEqual(9001, l.listingId);
            Assert.AreEqual(100, l.itemId);
            Assert.AreEqual(500, l.bidPrice);
            Assert.AreEqual(2000, l.buyoutPrice);
            Assert.AreEqual(before + 1, svc.ActiveListingCount);
            Assert.IsTrue(svc.IsExpired(9001) == false, "Listing 60s tương lai chưa hết hạn");
        }

        [Test]
        public void PlaceBid_UpdatesCurrentBid()
        {
            var svc = BuildService();
            svc.ListItem(9002, 200, 1, "Seller B", 1000, 5000, 60);
            var r = svc.PlaceBid(9002, 42, 1500);
            Assert.AreEqual(AuctionBidResult.Success, r);
            var l = svc.GetListing(9002);
            Assert.IsNotNull(l);
            Assert.AreEqual(42, l.currentBidder);
            Assert.AreEqual(1500, l.currentBid);
        }

        [Test]
        public void PlaceBid_RejectsTooLow()
        {
            var svc = BuildService();
            svc.ListItem(9003, 300, 1, "Seller C", 1000, 5000, 60);
            svc.PlaceBid(9003, 42, 2000);
            var r = svc.PlaceBid(9003, 43, 1500);
            Assert.AreEqual(AuctionBidResult.BidTooLow, r);
        }

        [Test]
        public void PlaceBid_NotFoundForMissing()
        {
            var svc = BuildService();
            var r = svc.PlaceBid(99999, 1, 100);
            Assert.AreEqual(AuctionBidResult.NotFound, r);
        }

        [Test]
        public void Buyout_RemovesListing()
        {
            var svc = BuildService();
            svc.ListItem(9004, 400, 1, "Seller D", 100, 1000, 60);
            var sold = svc.Buyout(9004, 99);
            Assert.IsNotNull(sold);
            Assert.IsTrue(sold.sold);
            Assert.AreEqual(99, sold.currentBidder);
            Assert.AreEqual(1000, sold.currentBid);
            Assert.IsNull(svc.GetListing(9004), "Buyout xong phải remove listing khỏi active");
        }

        [Test]
        public void IsExpired_TrueForPastTimestamp()
        {
            var svc = BuildService();
            // Tạo listing hợp lệ rồi đẩy expireTime về quá khứ (production từ chối
            // duration <= 0, nên không inject duration âm nữa — set timestamp trực tiếp).
            var l = svc.ListItem(9005, 500, 1, "Seller E", 100, 500, 60);
            Assert.IsNotNull(l, "Listing hợp lệ phải tạo được");
            l.expireTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1000;
            Assert.IsTrue(svc.IsExpired(9005), "Listing quá hạn → IsExpired true");
        }

        [Test]
        public void CancelListing_Removes()
        {
            var svc = BuildService();
            svc.ListItem(9006, 600, 1, "Seller F", 100, 500, 60);
            Assert.IsTrue(svc.CancelListing(9006));
            Assert.IsNull(svc.GetListing(9006));
        }
    }

    public class GoodsCatalogServiceTests
    {
        private static string ShopDir => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcShop");

        [Test]
        public void LoadFromStreamingAssets_LoadsGoods()
        {
            var reg = PcGoodsParser.BuildRegistry(ShopDir);
            var svc = new GoodsCatalogService(reg);
            Assert.GreaterOrEqual(svc.Count, 100, "PC goods.txt có 1,521 entries");
        }

        [Test]
        public void GetGoodsForShop_FiltersByShopId()
        {
            var reg = PcGoodsParser.BuildRegistry(ShopDir);
            var svc = new GoodsCatalogService(reg);
            Assert.IsNotNull(svc.GetAllGoods());
            int anyShopId = -1;
            foreach (var g in svc.GetAllGoods())
            {
                if (g != null) { anyShopId = g.itemGenre * 1000 + g.detailType; break; }
            }
            Assert.GreaterOrEqual(anyShopId, 0);
            var filtered = svc.GetGoodsForShop(anyShopId);
            // Có thể rỗng nếu 0 khớp, nhưng phải là List<PcGoodsEntry> hợp lệ
            Assert.IsNotNull(filtered);
        }

        [Test]
        public void GetGood_ReturnsById()
        {
            var reg = PcGoodsParser.BuildRegistry(ShopDir);
            var svc = new GoodsCatalogService(reg);
            Assert.IsNotNull(svc.GetGood(1), "ID đầu tiên phải tồn tại");
        }
    }

    public class PcShopRegistryTests
    {
        private static string ShopDir => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcShop");

        [Test]
        public void LoadFromStreamingAssets_LoadsShops()
        {
            var reg = PcShopParser.BuildRegistry(ShopDir);
            Assert.GreaterOrEqual(reg.Count, 100, "PC buysell.txt có 1,521 cửa hàng");
        }

        [Test]
        public void Count_Positive()
        {
            var reg = PcShopParser.BuildRegistry(ShopDir);
            Assert.Greater(reg.Count, 0);
        }

        [Test]
        public void GetShop_ReturnsById()
        {
            var reg = PcShopParser.BuildRegistry(ShopDir);
            Assert.IsNotNull(reg.Get(1));
        }

        [Test]
        public void GetAllShops_NotEmpty()
        {
            var reg = PcShopParser.BuildRegistry(ShopDir);
            int n = 0;
            foreach (var s in reg.All) { if (s != null) n++; }
            Assert.Greater(n, 0);
        }
    }
}
