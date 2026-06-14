// -----------------------------------------------------------------------------
// VLTK Mobile — AuctionService EditMode tests.
// Kiểm tra auction lifecycle: list, bid (with outbid refund), buyout, cancel,
// expire. IAuctionHost dispatch cho chat log + payment (PC Pay/EarnMoney).
// PC source: settings/auction.ini (Main + NotifyString sections).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class AuctionLifecycleTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IAuctionHost
        {
            public int ListCalls;
            public int OutBidCalls;
            public int BidWonCalls;
            public int ItemSoldCalls;
            public int ExpiredCalls;
            public int CancelledCalls;
            public Dictionary<int, int> Money = new();
            public bool DeductOk = true;
            public int LastDeductAmount;
            public int LastDeductPlayer;

            public void OnItemListed(int listingId, int itemId, string sellerName, int startingBid, int buyoutPrice)
            {
                ListCalls++;
            }
            public void OnOutBid(int listingId, int outbidPlayerId, int newBidderId, int newBid) { OutBidCalls++; }
            public void OnBidWon(int listingId, int winnerId, int finalBid) { BidWonCalls++; }
            public void OnItemSold(int listingId, int sellerId, int buyerId, int finalPrice) { ItemSoldCalls++; }
            public void OnListingExpired(int listingId, int sellerId, int itemId) { ExpiredCalls++; }
            public void OnListingCancelled(int listingId, int sellerId, int itemId) { CancelledCalls++; }
            public bool TryDeductPlayerMoney(int playerId, int amount)
            {
                LastDeductPlayer = playerId;
                LastDeductAmount = amount;
                if (!DeductOk) return false;
                if (!Money.ContainsKey(playerId)) Money[playerId] = 0;
                if (Money[playerId] < amount) return false;
                Money[playerId] -= amount;
                return true;
            }
            public void GrantPlayerMoney(int playerId, int amount)
            {
                if (!Money.ContainsKey(playerId)) Money[playerId] = 0;
                Money[playerId] += amount;
            }
        }

        private static AuctionService BuildService(IAuctionHost host = null)
            => new AuctionService(null, host);

        // ── AuctionBidResult enum ────────────────────────────────────────────

        [Test]
        public void AuctionBidResult_HasFiveOutcomes()
        {
            CollectionAssert.AreEquivalent(
                new[] { "Success", "OutBid", "Expired", "NotFound", "BidTooLow" },
                System.Enum.GetNames(typeof(AuctionBidResult)));
        }

        // ── ListItem ────────────────────────────────────────────────────────

        [Test]
        public void ListItem_ValidArgs_ReturnsListing()
        {
            var svc = BuildService();
            var l = svc.ListItem(1, 100, 1, "Alice", 50, 200, 3600);
            Assert.IsNotNull(l);
            Assert.AreEqual(1, l.listingId);
            Assert.AreEqual(100, l.itemId);
            Assert.AreEqual("Alice", l.sellerName);
            Assert.AreEqual(50, l.bidPrice);
            Assert.AreEqual(200, l.buyoutPrice);
            Assert.AreEqual(0, l.currentBidder);
            Assert.IsFalse(l.sold);
        }

        [Test]
        public void ListItem_ZeroId_ReturnsNull()
        {
            var svc = BuildService();
            Assert.IsNull(svc.ListItem(0, 100, 1, "X", 50, 200, 3600));
        }

        [Test]
        public void ListItem_NonPositiveDuration_ReturnsNull()
        {
            var svc = BuildService();
            Assert.IsNull(svc.ListItem(1, 100, 1, "X", 50, 200, 0));
            Assert.IsNull(svc.ListItem(2, 100, 1, "X", 50, 200, -1));
        }

        [Test]
        public void ListItem_DispatchesToHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.ListItem(1, 100, 1, "Alice", 50, 200, 3600);
            Assert.AreEqual(1, host.ListCalls);
        }

        [Test]
        public void ListItem_FiresOnListedEvent()
        {
            var svc = BuildService();
            int fired = 0;
            svc.OnListed += _ => fired++;
            svc.ListItem(1, 100, 1, "X", 50, 200, 3600);
            Assert.AreEqual(1, fired);
        }

        // ── PlaceBid ────────────────────────────────────────────────────────

        [Test]
        public void PlaceBid_NotFound_ReturnsNotFound()
        {
            var svc = BuildService();
            Assert.AreEqual(AuctionBidResult.NotFound, svc.PlaceBid(99, 2, 100));
        }

        [Test]
        public void PlaceBid_BelowStarting_ReturnsBidTooLow()
        {
            var svc = BuildService();
            svc.ListItem(1, 100, 1, "X", 50, 200, 3600);
            Assert.AreEqual(AuctionBidResult.BidTooLow, svc.PlaceBid(1, 2, 30));
        }

        [Test]
        public void PlaceBid_FirstValidBid_Succeeds()
        {
            var svc = BuildService();
            svc.ListItem(1, 100, 1, "X", 50, 200, 3600);
            var r = svc.PlaceBid(1, 2, 100);
            Assert.AreEqual(AuctionBidResult.Success, r);
            var l = svc.GetListing(1);
            Assert.AreEqual(100, l.currentBid);
            Assert.AreEqual(2, l.currentBidder);
        }

        [Test]
        public void PlaceBid_HigherBid_OutbidsPrevious()
        {
            var svc = BuildService();
            svc.ListItem(1, 100, 1, "X", 50, 200, 3600);
            svc.PlaceBid(1, 2, 100);
            var r = svc.PlaceBid(1, 3, 150);
            Assert.AreEqual(AuctionBidResult.Success, r);
            var l = svc.GetListing(1);
            Assert.AreEqual(150, l.currentBid);
            Assert.AreEqual(3, l.currentBidder);
        }

        [Test]
        public void PlaceBid_DeductsMoneyFromBidder()
        {
            var host = new FakeHost();
            host.Money[2] = 1000;
            var svc = BuildService(host);
            svc.ListItem(1, 100, 1, "X", 50, 200, 3600);
            svc.PlaceBid(1, 2, 100);
            Assert.AreEqual(900, host.Money[2]);
        }

        [Test]
        public void PlaceBid_RefundsOutbidPlayer()
        {
            var host = new FakeHost();
            host.Money[2] = 1000;
            host.Money[3] = 1000;
            var svc = BuildService(host);
            svc.ListItem(1, 100, 1, "X", 50, 200, 3600);
            svc.PlaceBid(1, 2, 100); // -100 from 2
            svc.PlaceBid(1, 3, 150); // -150 from 3, +100 refund to 2
            Assert.AreEqual(1000, host.Money[2]); // 1000-100+100=1000
            Assert.AreEqual(850, host.Money[3]); // 1000-150=850
            Assert.AreEqual(1, host.OutBidCalls);
        }

        [Test]
        public void PlaceBid_InsufficientFunds_ReturnsBidTooLow()
        {
            var host = new FakeHost();
            host.Money[2] = 50; // not enough
            var svc = BuildService(host);
            svc.ListItem(1, 100, 1, "X", 50, 200, 3600);
            var r = svc.PlaceBid(1, 2, 100);
            Assert.AreEqual(AuctionBidResult.BidTooLow, r);
        }

        [Test]
        public void PlaceBid_DispatchesHostOnBidWon()
        {
            var host = new FakeHost();
            host.Money[2] = 1000;
            var svc = BuildService(host);
            svc.ListItem(1, 100, 1, "X", 50, 200, 3600);
            svc.PlaceBid(1, 2, 100);
            Assert.AreEqual(1, host.BidWonCalls);
        }

        // ── Buyout ──────────────────────────────────────────────────────────

        [Test]
        public void Buyout_Valid_ReturnsListing()
        {
            var svc = BuildService();
            svc.ListItem(1, 100, 1, "X", 50, 200, 3600);
            var l = svc.Buyout(1, 2);
            Assert.IsNotNull(l);
            Assert.IsTrue(l.sold);
            Assert.AreEqual(200, l.currentBid);
        }

        [Test]
        public void Buyout_ZeroBuyoutPrice_ReturnsNull()
        {
            var svc = BuildService();
            svc.ListItem(1, 100, 1, "X", 50, 0, 3600);
            Assert.IsNull(svc.Buyout(1, 2));
        }

        [Test]
        public void Buyout_NotFound_ReturnsNull()
        {
            var svc = BuildService();
            Assert.IsNull(svc.Buyout(99, 2));
        }

        [Test]
        public void Buyout_GrantsMoneyToSeller()
        {
            var host = new FakeHost();
            host.Money[2] = 1000;
            var svc = BuildService(host);
            svc.ListItem(1, 100, 1, "Seller", 50, 200, 3600);
            svc.Buyout(1, 2);
            Assert.AreEqual(200, host.Money[1]); // seller
            Assert.AreEqual(800, host.Money[2]); // buyer
            Assert.AreEqual(1, host.ItemSoldCalls);
        }

        [Test]
        public void Buyout_AfterExistingBid_RefundsPreviousBidder()
        {
            var host = new FakeHost();
            host.Money[2] = 1000;
            host.Money[3] = 1000;
            var svc = BuildService(host);
            svc.ListItem(1, 100, 1, "Seller", 50, 200, 3600);
            svc.PlaceBid(1, 2, 100);
            svc.Buyout(1, 3); // buyer 3 buyouts at 200, refund bidder 2
            Assert.AreEqual(1000, host.Money[2]); // 1000-100+100=1000
            Assert.AreEqual(800, host.Money[3]); // 1000-200=800
        }

        // ── Cancel ──────────────────────────────────────────────────────────

        [Test]
        public void CancelListing_Exists_RemovesAndDispatches()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            svc.ListItem(1, 100, 1, "X", 50, 200, 3600);
            Assert.IsTrue(svc.CancelListing(1));
            Assert.AreEqual(1, host.CancelledCalls);
        }

        [Test]
        public void CancelListing_NotFound_ReturnsFalse()
        {
            var svc = BuildService();
            Assert.IsFalse(svc.CancelListing(99));
        }

        // ── ExpireDueListings ───────────────────────────────────────────────

        [Test]
        public void ExpireDueListings_NoExpired_ReturnsEmpty()
        {
            var svc = BuildService();
            svc.ListItem(1, 100, 1, "X", 50, 200, 3600);
            var expired = svc.ExpireDueListings();
            Assert.AreEqual(0, expired.Count);
        }

        [Test]
        public void ExpireDueListings_OneExpired_RemovesAndDispatches()
        {
            var host = new FakeHost();
            var svc = BuildService(host);
            // Use duration 1s and wait > 1s so timestamp passes
            svc.ListItem(1, 100, 1, "X", 50, 200, 1);
            System.Threading.Thread.Sleep(1500);
            var expired = svc.ExpireDueListings();
            Assert.AreEqual(1, expired.Count);
            Assert.AreEqual(1, host.ExpiredCalls);
        }

        // ── Lookup APIs ─────────────────────────────────────────────────────

        [Test]
        public void GetListing_NotFound_ReturnsNull()
        {
            var svc = BuildService();
            Assert.IsNull(svc.GetListing(99));
        }

        [Test]
        public void GetAllListings_Empty()
        {
            var svc = BuildService();
            Assert.AreEqual(0, Count(svc.GetAllListings()));
        }

        [Test]
        public void GetAllListings_AfterList()
        {
            var svc = BuildService();
            svc.ListItem(1, 100, 1, "X", 50, 200, 3600);
            svc.ListItem(2, 200, 1, "X", 50, 200, 3600);
            Assert.AreEqual(2, Count(svc.GetAllListings()));
        }

        [Test]
        public void IsExpired_NotFound_ReturnsFalse()
        {
            var svc = BuildService();
            Assert.IsFalse(svc.IsExpired(99));
        }

        // ── Config ──────────────────────────────────────────────────────────

        [Test]
        public void GetConfig_NoRegistry_ReturnsNull()
        {
            var svc = BuildService();
            Assert.IsNull(svc.GetConfig("Main", "TaxRate"));
        }

        [Test]
        public void GetConfigInt_NoRegistry_ReturnsDefault()
        {
            var svc = BuildService();
            Assert.AreEqual(42, svc.GetConfigInt("Main", "TaxRate", 42));
        }

        // ── Helper ──────────────────────────────────────────────────────────

        private static int Count<T>(IEnumerable<T> e)
        {
            int n = 0;
            foreach (var _ in e) n++;
            return n;
        }
    }
}
