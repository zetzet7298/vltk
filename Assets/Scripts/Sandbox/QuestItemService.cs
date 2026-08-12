// -----------------------------------------------------------------------------
// VLTK Mobile — ST-06.x Quest Item Service (Vật Phẩm Nhiệm Vụ)
// Quản lý vật phẩm nhiệm vụ: chìa khoá, đá quý, bảo đồ, lệnh bài, ...
// PC source: settings/item/questkey.txt + 60 file PcItemFull.
// Runtime inventory tracks owned count per encoded itemId (genre << 24 | detail << 8 | particular).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý vật phẩm nhiệm vụ (Túi Đồ Nhiệm Vụ).</summary>
    public class QuestItemService
    {
        public const string LogTag = "QuestItem";

        private readonly PcQuestItemRegistry _registry;
        private readonly Dictionary<int, int> _inventory = new();
        private IQuestItemHost _host;

        /// <summary>Event kích hoạt khi số lượng vật phẩm thay đổi (itemId, oldCount, newCount).</summary>
        public event Action<int, int, int> OnQuestItemChanged;

        public int OwnedItemCount => _inventory.Count;
        public int TotalQuantity
        {
            get
            {
                int total = 0;
                foreach (var v in _inventory.Values) total += v;
                return total;
            }
        }
        public int CatalogCount => _registry.Count;

        public QuestItemService() : this(null, null) { }
        public QuestItemService(PcQuestItemRegistry registry) : this(registry, null) { }
        public QuestItemService(PcQuestItemRegistry registry, IQuestItemHost host)
        {
            _registry = registry ?? new PcQuestItemRegistry();
            _host = host;
        }

        public void AttachHost(IQuestItemHost host) { _host = host; }

        /// <summary>Mã hoá (genre, detail, particular) thành 1 itemId duy nhất.</summary>
        public static int EncodeItemId(int genre, int detail, int particular)
            => ((genre & 0xFF) << 24) | ((detail & 0xFFFF) << 8) | (particular & 0xFF);

        /// <summary>Giải mã itemId thành (genre, detail, particular).</summary>
        public static (int genre, int detail, int particular) DecodeItemId(int itemId)
            => ((itemId >> 24) & 0xFF, (itemId >> 8) & 0xFFFF, itemId & 0xFF);

        /// <summary>Tra cứu thông tin vật phẩm nhiệm vụ từ itemId đã mã hoá.</summary>
        public PcQuestItemEntry GetQuestItem(int itemId)
        {
            var (g, d, p) = DecodeItemId(itemId);
            return _registry.Get(g, d, p);
        }

        /// <summary>Tra cứu questkey PC theo DetailType dùng bởi Lua HaveItem/DelItem(id).</summary>
        public PcQuestItemEntry GetPcQuestKeyDetail(int detailType)
            => _registry.GetByDetailType(detailType);

        public bool TryEncodePcQuestKeyDetailId(int detailType, out int itemId)
        {
            itemId = 0;
            var entry = GetPcQuestKeyDetail(detailType);
            if (entry == null) return false;
            itemId = EncodeItemId(entry.itemGenre, entry.detailType, entry.particularType);
            return true;
        }

        /// <summary>Số lượng hiện có trong túi đồ.</summary>
        public int GetQuestItemCount(int itemId)
            => _inventory.TryGetValue(itemId, out var c) ? c : 0;

        /// <summary>Thêm vật phẩm vào túi (Nhận). Trả về số lượng mới.</summary>
        public int AddQuestItem(int itemId, int count)
        {
            if (count <= 0) return GetQuestItemCount(itemId);
            int old = GetQuestItemCount(itemId);
            int newCount = old + count;
            _inventory[itemId] = newCount;
            SubsystemLog.Info(LogTag, $"Nhận vật phẩm nhiệm vụ #{itemId}: {old} → {newCount}");
            OnQuestItemChanged?.Invoke(itemId, old, newCount);
            if (_host != null)
            {
                _host.OnQuestItemReceived(itemId, old, newCount, count);
                _host.LogQuestItemEvent(itemId, old, newCount);
                _host.PlayItemSFX(itemId, "receive");
                _host.ShowQuestItemUI(_inventory.Count, TotalQuantity);
                _host.SaveQuestItemState(_inventory.Count, TotalQuantity);
            }
            return newCount;
        }

        /// <summary>Sử dụng / bỏ vật phẩm. Trả về true nếu đủ để trừ.</summary>
        public bool RemoveQuestItem(int itemId, int count)
        {
            if (count <= 0) return true;
            int old = GetQuestItemCount(itemId);
            if (old < count)
            {
                _host?.OnQuestItemInsufficient(itemId, count, old);
                return false;
            }
            int newCount = old - count;
            if (newCount <= 0) _inventory.Remove(itemId);
            else _inventory[itemId] = newCount;
            SubsystemLog.Info(LogTag, $"Sử dụng vật phẩm nhiệm vụ #{itemId}: {old} → {newCount}");
            OnQuestItemChanged?.Invoke(itemId, old, newCount);
            if (_host != null)
            {
                _host.OnQuestItemRemoved(itemId, old, newCount, count);
                _host.LogQuestItemEvent(itemId, old, newCount);
                _host.PlayItemSFX(itemId, "use");
                _host.ShowQuestItemUI(_inventory.Count, TotalQuantity);
                _host.SaveQuestItemState(_inventory.Count, TotalQuantity);
            }
            return true;
        }

        /// <summary>Kiểm tra đủ vật phẩm để giao nộp.</summary>
        public bool HasQuestItem(int itemId, int minCount)
        {
            if (minCount <= 0) return true;
            return GetQuestItemCount(itemId) >= minCount;
        }

        public int GetPcQuestKeyDetailCount(int detailType)
            => TryEncodePcQuestKeyDetailId(detailType, out int itemId) ? GetQuestItemCount(itemId) : 0;

        public int AddPcQuestKeyDetail(int detailType, int count)
        {
            if (!TryEncodePcQuestKeyDetailId(detailType, out int itemId))
            {
                SubsystemLog.Warn(LogTag, $"Không tìm thấy questkey PC DetailType={detailType}; bỏ qua AddEventItem/HaveItem bridge");
                return 0;
            }
            return AddQuestItem(itemId, count);
        }

        public bool RemovePcQuestKeyDetail(int detailType, int count)
        {
            if (!TryEncodePcQuestKeyDetailId(detailType, out int itemId))
            {
                SubsystemLog.Warn(LogTag, $"Không tìm thấy questkey PC DetailType={detailType}; bỏ qua DelItem bridge");
                return false;
            }
            return RemoveQuestItem(itemId, count);
        }

        public bool HasPcQuestKeyDetail(int detailType, int minCount)
            => TryEncodePcQuestKeyDetailId(detailType, out int itemId) && HasQuestItem(itemId, minCount);

        public bool HaveItem(int pcQuestKeyDetailType, int minCount = 1)
            => HasPcQuestKeyDetail(pcQuestKeyDetailType, minCount);

        public bool DelItem(int pcQuestKeyDetailType, int count = 1)
            => RemovePcQuestKeyDetail(pcQuestKeyDetailType, count);

        public bool AddEventItem(int pcQuestKeyDetailType, int count = 1)
        {
            if (!TryEncodePcQuestKeyDetailId(pcQuestKeyDetailType, out int itemId))
            {
                SubsystemLog.Warn(LogTag, $"Không tìm thấy questkey PC DetailType={pcQuestKeyDetailType}; bỏ qua AddEventItem bridge");
                return false;
            }
            AddQuestItem(itemId, count);
            return true;
        }

        /// <summary>Toàn bộ vật phẩm nhiệm vụ trong catalog.</summary>
        public IEnumerable<PcQuestItemEntry> GetAllQuestItems() => _registry.All;

        /// <summary>Owned vật phẩm nhiệm vụ (itemId, count).</summary>
        public IEnumerable<(int itemId, int count)>
            GetAllOwnedQuestItems()
        {
            foreach (var kv in _inventory) yield return (kv.Key, kv.Value);
        }

        /// <summary>Xoá sạch túi đồ (reset state khi logout hoặc đổi nhân vật).</summary>
        public void Clear()
        {
            var keys = new List<int>(_inventory.Keys);
            int clearedCount = _inventory.Count;
            _inventory.Clear();
            foreach (var k in keys) OnQuestItemChanged?.Invoke(k, 0, 0);
            if (_host != null)
            {
                _host.OnQuestItemCleared(clearedCount);
                _host.ShowQuestItemUI(0, 0);
                _host.SaveQuestItemState(0, 0);
                _host.PlayItemSFX(0, "clear");
            }
        }

        /// <summary>Load từ StreamingAssets/Reference/PcItemFull.</summary>
        public static QuestItemService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcItemFull");
            var reg = PcQuestItemParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} vật phẩm nhiệm vụ từ {dir}");
            return new QuestItemService(reg);
        }
    }
}
