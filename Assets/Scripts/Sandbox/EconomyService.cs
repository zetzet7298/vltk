// -----------------------------------------------------------------------------
// VLTK Mobile — ST-05.3 Economy: Stash, Trade, Currency
// PC source: Stash (kho đồ), trade system, silver/gold currency.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    [Serializable]
    public class CurrencyWallet
    {
        public int silver;       // Bạc (thường)
        public int gold;         // Kim Bảo (premium)
        public int huyenTinh;    // Huyền Tinh (upgrade material)
    }

    [Serializable]
    public class StashSlot
    {
        public int itemId;
        public int count;
    }

    /// <summary>
    /// Service quản lý Stash (Kho Đồ), Trade (Giao Dịch), và Economy.
    /// PC source: KNpc::Stash, Trade dialog, Silver currency.
    /// </summary>
    public class EconomyService
    {
        private readonly CurrencyWallet _wallet = new();
        private readonly List<StashSlot> _stash = new();
        private readonly int _maxStashSlots;

        public CurrencyWallet Wallet => _wallet;
        public IReadOnlyList<StashSlot> Stash => _stash;
        public int StashUsed => _stash.Count;
        public int StashRemaining => _maxStashSlots - _stash.Count;

        public event Action<int, int> OnSilverChanged; // (newAmount, delta)
        public event Action<int, int> OnGoldChanged;

        public EconomyService(int maxStashSlots = 100, int initialSilver = 0)
        {
            _maxStashSlots = maxStashSlots;
            _wallet.silver = initialSilver;
        }

        // ── Currency ───────────────────────────────────────────────────────

        public bool SpendSilver(int amount)
        {
            if (_wallet.silver < amount) return false;
            _wallet.silver -= amount;
            OnSilverChanged?.Invoke(_wallet.silver, -amount);
            return true;
        }

        public void EarnSilver(int amount)
        {
            _wallet.silver += amount;
            OnSilverChanged?.Invoke(_wallet.silver, amount);
        }

        public bool SpendGold(int amount)
        {
            if (_wallet.gold < amount) return false;
            _wallet.gold -= amount;
            OnGoldChanged?.Invoke(_wallet.gold, -amount);
            return true;
        }

        public void EarnGold(int amount)
        {
            _wallet.gold += amount;
            OnGoldChanged?.Invoke(_wallet.gold, amount);
        }

        // ── Stash ──────────────────────────────────────────────────────────

        /// <summary>Lưu vật phẩm vào stash.</summary>
        public bool DepositToStash(int itemId, int count)
        {
            if (count <= 0) return false;
            if (_stash.Count >= _maxStashSlots)
            {
                SubsystemLog.Warn("Stash", "Kho đồ đã đầy!");
                return false;
            }

            // Stack vào slot có sẵn
            var existing = _stash.Find(s => s.itemId == itemId);
            if (existing != null)
            {
                existing.count += count;
            }
            else
            {
                _stash.Add(new StashSlot { itemId = itemId, count = count });
            }

            SubsystemLog.Info("Stash", $"Lưu vào kho: Item {itemId} x{count}");
            return true;
        }

        /// <summary>Lấy vật phẩm từ stash.</summary>
        public bool WithdrawFromStash(int itemId, int count)
        {
            var slot = _stash.Find(s => s.itemId == itemId);
            if (slot == null || slot.count < count) return false;

            slot.count -= count;
            if (slot.count <= 0)
                _stash.Remove(slot);

            SubsystemLog.Info("Stash", $"Lấy từ kho: Item {itemId} x{count}");
            return true;
        }

        // ── Trade (Player-to-Player) ───────────────────────────────────────

        /// <summary>Tạo yêu cầu giao dịch.</summary>
        public TradeSession CreateTradeSession(int initiatorId, int targetId)
        {
            return new TradeSession
            {
                initiatorId = initiatorId,
                targetId = targetId,
                initiatorLocked = false,
                targetLocked = false,
            };
        }

        // ── Shop (NPC Buy/Sell) ────────────────────────────────────────────

        /// <summary>Mua vật phẩm từ NPC shop.</summary>
        public bool BuyFromShop(int itemId, int count, int unitPrice)
        {
            int totalCost = unitPrice * count;
            if (!SpendSilver(totalCost)) return false;

            SubsystemLog.Info("Shop", $"Mua Item {itemId} x{count} với {totalCost} Bạc");
            return true;
        }

        /// <summary>Bán vật phẩm cho NPC shop (giá 50%).</summary>
        public int SellToShop(int itemId, int count, int unitPrice)
        {
            int sellPrice = (unitPrice * count) / 2;
            EarnSilver(sellPrice);
            SubsystemLog.Info("Shop", $"Bán Item {itemId} x{count} được {sellPrice} Bạc");
            return sellPrice;
        }
    }

    [Serializable]
    public class TradeSession
    {
        public int initiatorId;
        public int targetId;
        public readonly List<StashSlot> initiatorItems = new();
        public readonly List<StashSlot> targetItems = new();
        public int initiatorSilver;
        public int targetSilver;
        public bool initiatorLocked;
        public bool targetLocked;

        public bool IsReady => initiatorLocked && targetLocked;

        public void AddItem(int actorId, int itemId, int count)
        {
            var list = actorId == initiatorId ? initiatorItems : targetItems;
            var existing = list.Find(s => s.itemId == itemId);
            if (existing != null) existing.count += count;
            else list.Add(new StashSlot { itemId = itemId, count = count });
        }

        public void SetSilver(int actorId, int amount)
        {
            if (actorId == initiatorId) initiatorSilver = amount;
            else targetSilver = amount;
        }

        public void Lock(int actorId)
        {
            if (actorId == initiatorId) initiatorLocked = true;
            else targetLocked = true;
        }
    }
}
