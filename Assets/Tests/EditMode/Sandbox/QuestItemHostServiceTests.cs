// -----------------------------------------------------------------------------
// VLTK Mobile — QuestItemService EditMode tests.
// Kiểm tra vật phẩm nhiệm vụ lifecycle: encode/decode, add/remove, insufficient,
// clear, PC HaveItem/DelItem lua bridges, host dispatch chain.
// PC source: settings/item/questkey.txt + 60 file PcItemFull.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class QuestItemHostServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IQuestItemHost
        {
            public int ReceivedCalls;
            public int RemovedCalls;
            public int InsufficientCalls;
            public int ClearedCalls;
            public int ShowCalls;
            public int LogCalls;
            public int SfxCalls;
            public int SaveCalls;
            public int LastItemId;
            public int LastOldCount;
            public int LastNewCount;
            public int LastAdded;
            public int LastRemoved;
            public int LastRequired;
            public int LastCurrent;
            public int LastClearedCount;
            public int LastItemCount;
            public int LastTotalQty;
            public string LastSfxAction;

            public void OnQuestItemReceived(int itemId, int oldCount, int newCount, int added)
            {
                ReceivedCalls++;
                LastItemId = itemId;
                LastOldCount = oldCount;
                LastNewCount = newCount;
                LastAdded = added;
            }
            public void OnQuestItemRemoved(int itemId, int oldCount, int newCount, int removed)
            {
                RemovedCalls++;
                LastRemoved = removed;
            }
            public void OnQuestItemInsufficient(int itemId, int required, int current)
            {
                InsufficientCalls++;
                LastRequired = required;
                LastCurrent = current;
            }
            public void OnQuestItemCleared(int clearedItemCount) { ClearedCalls++; LastClearedCount = clearedItemCount; }
            public void ShowQuestItemUI(int itemCount, int totalQuantity)
            {
                ShowCalls++;
                LastItemCount = itemCount;
                LastTotalQty = totalQuantity;
            }
            public void LogQuestItemEvent(int itemId, int oldCount, int newCount) { LogCalls++; }
            public void PlayItemSFX(int itemId, string action) { SfxCalls++; LastSfxAction = action; }
            public void SaveQuestItemState(int itemCount, int totalQuantity) { SaveCalls++; }
        }

        // ── Encode / Decode ──────────────────────────────────────────────────

        [Test]
        public void Encode_Standard()
        {
            int id = QuestItemService.EncodeItemId(1, 100, 5);
            Assert.AreEqual((1 << 24) | (100 << 8) | 5, id);
        }

        [Test]
        public void Encode_Zero_Zero()
        {
            int id = QuestItemService.EncodeItemId(0, 0, 0);
            Assert.AreEqual(0, id);
        }

        [Test]
        public void Encode_MaskGenre()
        {
            // genre > 0xFF gets masked to 0xFF
            int id = QuestItemService.EncodeItemId(0x1FF, 0, 0);
            Assert.AreEqual(0xFF << 24, id);
        }

        [Test]
        public void Decode_Standard()
        {
            int id = QuestItemService.EncodeItemId(5, 200, 7);
            var (g, d, p) = QuestItemService.DecodeItemId(id);
            Assert.AreEqual(5, g);
            Assert.AreEqual(200, d);
            Assert.AreEqual(7, p);
        }

        [Test]
        public void Encode_Decode_RoundTrip()
        {
            int id = QuestItemService.EncodeItemId(42, 1000, 99);
            var (g, d, p) = QuestItemService.DecodeItemId(id);
            Assert.AreEqual(42, g);
            Assert.AreEqual(1000, d);
            Assert.AreEqual(99, p);
        }

        // ── Ctor / Count ────────────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new QuestItemService();
            Assert.AreEqual(0, svc.OwnedItemCount);
            Assert.AreEqual(0, svc.TotalQuantity);
        }

        [Test]
        public void Constructor_WithRegistry()
        {
            var reg = new PcQuestItemRegistry();
            var svc = new QuestItemService(reg);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var reg = new PcQuestItemRegistry();
            var svc = new QuestItemService(reg, host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new QuestItemService();
            svc.AttachHost(host);
            svc.AddQuestItem(100, 5);
            Assert.AreEqual(1, host.ReceivedCalls);
        }

        // ── AddQuestItem ────────────────────────────────────────────────────

        [Test]
        public void AddQuestItem_Success()
        {
            var svc = new QuestItemService();
            int n = svc.AddQuestItem(100, 5);
            Assert.AreEqual(5, n);
        }

        [Test]
        public void AddQuestItem_Accumulates()
        {
            var svc = new QuestItemService();
            svc.AddQuestItem(100, 5);
            svc.AddQuestItem(100, 3);
            Assert.AreEqual(8, svc.GetQuestItemCount(100));
        }

        [Test]
        public void AddQuestItem_Zero_NoChange()
        {
            var svc = new QuestItemService();
            int n = svc.AddQuestItem(100, 0);
            Assert.AreEqual(0, n);
        }

        [Test]
        public void AddQuestItem_Negative_NoChange()
        {
            var svc = new QuestItemService();
            int n = svc.AddQuestItem(100, -5);
            Assert.AreEqual(0, n);
        }

        [Test]
        public void AddQuestItem_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new QuestItemService(null, host);
            svc.AddQuestItem(100, 5);
            Assert.AreEqual(1, host.ReceivedCalls);
            Assert.AreEqual(100, host.LastItemId);
            Assert.AreEqual(0, host.LastOldCount);
            Assert.AreEqual(5, host.LastNewCount);
            Assert.AreEqual(5, host.LastAdded);
            Assert.AreEqual(1, host.ShowCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void AddQuestItem_FiresOnQuestItemChangedEvent()
        {
            var svc = new QuestItemService();
            int fired = 0;
            svc.OnQuestItemChanged += (i, o, n) => fired++;
            svc.AddQuestItem(100, 5);
            Assert.AreEqual(1, fired);
        }

        // ── RemoveQuestItem ─────────────────────────────────────────────────

        [Test]
        public void RemoveQuestItem_Success()
        {
            var svc = new QuestItemService();
            svc.AddQuestItem(100, 10);
            Assert.IsTrue(svc.RemoveQuestItem(100, 3));
            Assert.AreEqual(7, svc.GetQuestItemCount(100));
        }

        [Test]
        public void RemoveQuestItem_Insufficient_ReturnsFalse()
        {
            var svc = new QuestItemService();
            svc.AddQuestItem(100, 2);
            Assert.IsFalse(svc.RemoveQuestItem(100, 5));
            Assert.AreEqual(2, svc.GetQuestItemCount(100));
        }

        [Test]
        public void RemoveQuestItem_Zero_ReturnsTrue()
        {
            var svc = new QuestItemService();
            Assert.IsTrue(svc.RemoveQuestItem(100, 0));
        }

        [Test]
        public void RemoveQuestItem_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new QuestItemService(null, host);
            svc.AddQuestItem(100, 10);
            host.RemovedCalls = 0;
            host.SaveCalls = 0;
            svc.RemoveQuestItem(100, 3);
            Assert.AreEqual(1, host.RemovedCalls);
            Assert.AreEqual(3, host.LastRemoved);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void RemoveQuestItem_Insufficient_Dispatches()
        {
            var host = new FakeHost();
            var svc = new QuestItemService(null, host);
            svc.AddQuestItem(100, 2);
            svc.RemoveQuestItem(100, 5);
            Assert.AreEqual(1, host.InsufficientCalls);
            Assert.AreEqual(5, host.LastRequired);
            Assert.AreEqual(2, host.LastCurrent);
        }

        [Test]
        public void RemoveQuestItem_RemovesSlot_WhenZero()
        {
            var svc = new QuestItemService();
            svc.AddQuestItem(100, 3);
            svc.RemoveQuestItem(100, 3);
            Assert.AreEqual(0, svc.OwnedItemCount);
        }

        // ── HasQuestItem ────────────────────────────────────────────────────

        [Test]
        public void HasQuestItem_True()
        {
            var svc = new QuestItemService();
            svc.AddQuestItem(100, 5);
            Assert.IsTrue(svc.HasQuestItem(100, 3));
        }

        [Test]
        public void HasQuestItem_False()
        {
            var svc = new QuestItemService();
            svc.AddQuestItem(100, 2);
            Assert.IsFalse(svc.HasQuestItem(100, 5));
        }

        [Test]
        public void HasQuestItem_ZeroMin_True()
        {
            var svc = new QuestItemService();
            Assert.IsTrue(svc.HasQuestItem(100, 0));
        }

        [Test]
        public void HasQuestItem_NoItem_False()
        {
            var svc = new QuestItemService();
            Assert.IsFalse(svc.HasQuestItem(100, 1));
        }

        // ── PC questkey bridges ─────────────────────────────────────────────

        [Test]
        public void GetPcQuestKeyDetail_NotFound_ReturnsNull()
        {
            var svc = new QuestItemService();
            Assert.IsNull(svc.GetPcQuestKeyDetail(9999));
        }

        [Test]
        public void TryEncodePcQuestKeyDetailId_NotFound_False()
        {
            var svc = new QuestItemService();
            Assert.IsFalse(svc.TryEncodePcQuestKeyDetailId(9999, out _));
        }

        [Test]
        public void AddPcQuestKeyDetail_NotFound_ReturnsZero()
        {
            var svc = new QuestItemService();
            Assert.AreEqual(0, svc.AddPcQuestKeyDetail(9999, 5));
        }

        [Test]
        public void RemovePcQuestKeyDetail_NotFound_False()
        {
            var svc = new QuestItemService();
            Assert.IsFalse(svc.RemovePcQuestKeyDetail(9999, 1));
        }

        [Test]
        public void HaveItem_DelegatesToHasPcQuestKeyDetail()
        {
            var svc = new QuestItemService();
            Assert.IsFalse(svc.HaveItem(9999, 1));
        }

        [Test]
        public void DelItem_DelegatesToRemovePcQuestKeyDetail()
        {
            var svc = new QuestItemService();
            Assert.IsFalse(svc.DelItem(9999, 1));
        }

        [Test]
        public void AddEventItem_NotFound_False()
        {
            var svc = new QuestItemService();
            Assert.IsFalse(svc.AddEventItem(9999, 1));
        }

        [Test]
        public void GetPcQuestKeyDetailCount_NotFound_Zero()
        {
            var svc = new QuestItemService();
            Assert.AreEqual(0, svc.GetPcQuestKeyDetailCount(9999));
        }

        // ── GetAllQuestItems / GetAllOwnedQuestItems ───────────────────────

        [Test]
        public void GetAllQuestItems_Empty()
        {
            var svc = new QuestItemService();
            int n = 0;
            foreach (var _ in svc.GetAllQuestItems()) n++;
            Assert.AreEqual(0, n);
        }

        [Test]
        public void GetAllOwnedQuestItems_Empty()
        {
            var svc = new QuestItemService();
            int n = 0;
            foreach (var _ in svc.GetAllOwnedQuestItems()) n++;
            Assert.AreEqual(0, n);
        }

        [Test]
        public void GetAllOwnedQuestItems_AfterAdd()
        {
            var svc = new QuestItemService();
            svc.AddQuestItem(100, 5);
            svc.AddQuestItem(200, 3);
            int n = 0;
            foreach (var _ in svc.GetAllOwnedQuestItems()) n++;
            Assert.AreEqual(2, n);
        }

        // ── TotalQuantity ────────────────────────────────────────────────────

        [Test]
        public void TotalQuantity_AfterAdds()
        {
            var svc = new QuestItemService();
            svc.AddQuestItem(100, 5);
            svc.AddQuestItem(200, 3);
            Assert.AreEqual(8, svc.TotalQuantity);
        }

        // ── Clear ───────────────────────────────────────────────────────────

        [Test]
        public void Clear_Empties()
        {
            var svc = new QuestItemService();
            svc.AddQuestItem(100, 5);
            svc.AddQuestItem(200, 3);
            svc.Clear();
            Assert.AreEqual(0, svc.OwnedItemCount);
        }

        [Test]
        public void Clear_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new QuestItemService(null, host);
            svc.AddQuestItem(100, 5);
            host.ClearedCalls = 0;
            svc.Clear();
            Assert.AreEqual(1, host.ClearedCalls);
            Assert.AreEqual(1, host.LastClearedCount);
        }

        [Test]
        public void Clear_FiresOnQuestItemChangedEvent()
        {
            var svc = new QuestItemService();
            svc.AddQuestItem(100, 5);
            int fired = 0;
            svc.OnQuestItemChanged += (i, o, n) => fired++;
            svc.Clear();
            Assert.AreEqual(1, fired);
        }

        // ── GetQuestItem ────────────────────────────────────────────────────

        [Test]
        public void GetQuestItem_NotInRegistry_ReturnsNull()
        {
            var svc = new QuestItemService();
            int id = QuestItemService.EncodeItemId(99, 99, 99);
            Assert.IsNull(svc.GetQuestItem(id));
        }

        // ── No-host ─────────────────────────────────────────────────────────

        [Test]
        public void QuestItemService_WithoutHost_DoesNotThrow()
        {
            var svc = new QuestItemService();
            Assert.DoesNotThrow(() => svc.AddQuestItem(100, 5));
            Assert.DoesNotThrow(() => svc.RemoveQuestItem(100, 3));
            Assert.DoesNotThrow(() => svc.Clear());
        }
    }
}
