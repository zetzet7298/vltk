// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX Item Exchange Service (Đổi Vật Phẩm runtime)
// Wraps PcItemExchangeRegistry. PC source: settings/item_exchange.txt.
// Hỗ trợ đổi vật phẩm: trừ nguyên liệu theo recipe, cộng vật phẩm mới.
// Vietnamese: "Đổi Vật Phẩm", "Công Thức", "Nguyên Liệu".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Kết quả đổi vật phẩm.</summary>
    [Serializable]
    public class ItemExchangeResult
    {
        public bool success;
        public string error;
        public int exchangedId;
        public int requireGenre;
        public int requireDetail;
        public int requireParticular;
        public int requireCount;
        public int getGenre;
        public int getDetail;
        public int getParticular;
        public int getCount;
    }

    /// <summary>
    /// Service quản lý đổi vật phẩm (Công Thức Đổi Đồ).
    /// PC source: settings/item_exchange.txt.
    /// </summary>
    public class ItemExchangeService
    {
        public const string LogTag = "ItemExchange";

        private PcItemExchangeRegistry _registry;

        public event Action<ItemExchangeResult> OnExchanged;

        public int Count => _registry != null ? _registry.Count : 0;

        public ItemExchangeService() : this(null) { }

        public ItemExchangeService(PcItemExchangeRegistry registry)
        {
            RegisterRegistry(registry);
        }

        public void RegisterRegistry(PcItemExchangeRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"Đổi Vật Phẩm loaded: {Count} công thức");
        }

        public PcItemExchangeEntry GetExchange(int id)
            => _registry != null ? _registry.Get(id) : null;

        public IEnumerable<PcItemExchangeEntry> GetAllExchanges()
            => _registry != null ? _registry.GetAll() : (IEnumerable<PcItemExchangeEntry>)Array.Empty<PcItemExchangeEntry>();

        /// <summary>
        /// Mã hoá (genre, detail, particular) → key 32-bit để tra inventory dictionary.
        /// </summary>
        public static int EncodeItemKey(int genre, int detail, int particular)
            => ((genre & 0xFF) << 16) | ((detail & 0xFF) << 8) | (particular & 0xFF);

        /// <summary>
        /// Thực hiện đổi vật phẩm. inventory: dict&lt;int itemKey, int count&gt;.
        /// Hàm này KHÔNG tự trừ/cộng inventory — chỉ validate. Caller chịu trách nhiệm mutate.
        /// </summary>
        public ItemExchangeResult TryExchange(int id, int playerLevel, Dictionary<int, int> inventory)
        {
            var recipe = GetExchange(id);
            if (recipe == null)
            {
                return new ItemExchangeResult
                {
                    success = false,
                    error = $"Không tìm thấy công thức #{id}",
                    exchangedId = id,
                };
            }
            if (recipe.minLevel > 0 && playerLevel < recipe.minLevel)
            {
                return new ItemExchangeResult
                {
                    success = false,
                    error = $"Cấp {playerLevel} chưa đủ (cần {recipe.minLevel})",
                    exchangedId = id,
                };
            }
            if (inventory == null)
            {
                return new ItemExchangeResult
                {
                    success = false,
                    error = "Túi đồ rỗng",
                    exchangedId = id,
                };
            }
            int requireKey = EncodeItemKey(recipe.requireGenre, recipe.requireDetail, recipe.requireParticular);
            int have = inventory.TryGetValue(requireKey, out var c) ? c : 0;
            if (have < recipe.requireCount)
            {
                return new ItemExchangeResult
                {
                    success = false,
                    error = $"Thiếu nguyên liệu (cần {recipe.requireCount}, có {have})",
                    exchangedId = id,
                    requireGenre = recipe.requireGenre,
                    requireDetail = recipe.requireDetail,
                    requireParticular = recipe.requireParticular,
                    requireCount = recipe.requireCount,
                };
            }

            // Đủ điều kiện: trừ nguyên liệu, cộng vật phẩm mới
            inventory[requireKey] = have - recipe.requireCount;
            int getKey = EncodeItemKey(recipe.getGenre, recipe.getDetail, recipe.getParticular);
            inventory[getKey] = inventory.TryGetValue(getKey, out var g) ? g + recipe.getCount : recipe.getCount;

            var result = new ItemExchangeResult
            {
                success = true,
                error = string.Empty,
                exchangedId = id,
                requireGenre = recipe.requireGenre,
                requireDetail = recipe.requireDetail,
                requireParticular = recipe.requireParticular,
                requireCount = recipe.requireCount,
                getGenre = recipe.getGenre,
                getDetail = recipe.getDetail,
                getParticular = recipe.getParticular,
                getCount = recipe.getCount,
            };
            SubsystemLog.Info(LogTag,
                $"Đổi thành công công thức #{id} ({recipe.nameRaw})");
            OnExchanged?.Invoke(result);
            return result;
        }

        public static ItemExchangeService LoadFromStreamingAssets(string subdir = "Reference/PcItemExchange")
        {
            var svc = new ItemExchangeService();
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            if (Directory.Exists(dir))
            {
                var reg = PcItemExchangeParser.BuildRegistry(dir);
                svc.RegisterRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"ItemExchangeService: directory không tồn tại {dir}");
            }
            return svc;
        }
    }
}
