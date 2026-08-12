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
        public static readonly ItemExchangeLingpaiDefinition[] LingpaiDefinitions =
        {
            new ItemExchangeLingpaiDefinition { NameVi = "Thanh Cầu Lệnh", PcFunction = "exchange_lingpai_qingju", RequiredMagicLevel = 1000, Genre = 6, Detail = 1, Particular = 4867 },
            new ItemExchangeLingpaiDefinition { NameVi = "Vân Lộc Lệnh", PcFunction = "exchange_lingpai_yunlu", RequiredMagicLevel = 5000, Genre = 6, Detail = 1, Particular = 4868 },
            new ItemExchangeLingpaiDefinition { NameVi = "Thương Lang Lệnh", PcFunction = "exchange_lingpai_canglang", RequiredMagicLevel = 10000, Genre = 6, Detail = 1, Particular = 4869 },
            new ItemExchangeLingpaiDefinition { NameVi = "Huyền Viên Lệnh", PcFunction = "exchange_lingpai_xuanyuan", RequiredMagicLevel = 20000, Genre = 6, Detail = 1, Particular = 4870 },
            new ItemExchangeLingpaiDefinition { NameVi = "Tử Mãng Lệnh", PcFunction = "exchange_lingpai_zimang", RequiredMagicLevel = 30000, Genre = 6, Detail = 1, Particular = 4871 },
            new ItemExchangeLingpaiDefinition { NameVi = "Kim Ô Lệnh", PcFunction = "exchange_lingpai_wujin", RequiredMagicLevel = 50000, Genre = 6, Detail = 1, Particular = 4872 },
            new ItemExchangeLingpaiDefinition { NameVi = "Bạch Hổ Lệnh", PcFunction = "exchange_lingpai_baihu", RequiredMagicLevel = 70000, Genre = 6, Detail = 1, Particular = 4873 },
            new ItemExchangeLingpaiDefinition { NameVi = "Xích Lân Lệnh", PcFunction = "exchange_lingpai_xichlan", RequiredMagicLevel = 90000, Genre = 6, Detail = 1, Particular = 4874 },
            new ItemExchangeLingpaiDefinition { NameVi = "Minh Phượng Lệnh", PcFunction = "exchange_lingpai_minhphuong", RequiredMagicLevel = 120000, Genre = 6, Detail = 1, Particular = 4875 },
            new ItemExchangeLingpaiDefinition { NameVi = "Đằng Long Lệnh", PcFunction = "exchange_lingpai_danglong", RequiredMagicLevel = 150000, Genre = 6, Detail = 1, Particular = 4876 },
        };

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

        public static ItemExchangePlan BuildExchangeOldItemPlan(ItemExchangePlanInput input)
        {
            if (input == null) return ItemExchangePlan.Fail("exchange_olditem", "exchange_olditem_compse_confirm", "missing input");
            if (input.GivenItemCount <= 0 || input.ItemIndex <= 0)
                return ItemExchangePlan.Fail("exchange_olditem", "exchange_olditem_compose", "missing item");
            if (input.GivenItemCount > 1)
                return ItemExchangePlan.Fail("exchange_olditem", "exchange_olditem_compose", "only one item per exchange");
            if (input.BindState != 0)
                return ItemExchangePlan.Fail("exchange_olditem", "exchange_olditem_compose", "bound item is rejected");
            if ((input.UseTime > 0 && input.UseTime != unchecked((int)4294967295)) || input.ExpireTime > 0)
                return ItemExchangePlan.Fail("exchange_olditem", "exchange_olditem_compose", "time-limited item is rejected");
            if (input.ExchangeValue <= 0)
                return ItemExchangePlan.Fail("exchange_olditem", "exchange_olditem_compose", "exchange value must be positive");

            var plan = ItemExchangePlan.Ok("exchange_olditem", "exchange_olditem_compse_confirm");
            plan.AwardName = "Hồn Thạch";
            plan.Commands.Add(ItemExchangeHostCommand.Create("WriteLog", input.ItemName, input.ItemQuality, input.ExchangeValue));
            plan.Commands.Add(ItemExchangeHostCommand.Create("RemoveItemByIndex", null, input.ItemIndex));
            plan.Commands.Add(ItemExchangeHostCommand.Create("AddGoldItem", "Hồn Thạch", 0, 6, 1, 2356, 1, 0, 0, input.ExchangeValue));
            plan.Commands.Add(ItemExchangeHostCommand.Create("WriteLog", "Equip Exchange", input.ExchangeValue));
            return plan;
        }

        public ItemExchangePlan BuildLingpaiPlan(ItemExchangePlanInput input, string lingpaiName)
        {
            var definition = FindLingpaiDefinition(lingpaiName);
            if (definition == null) return ItemExchangePlan.Fail("exchange_lingpai", "exchange_lingpai_compose", "unknown lingpai");
            if (input == null) return ItemExchangePlan.Fail("exchange_lingpai", definition.PcFunction, "missing input");
            if (input.GivenItemCount <= 0 || input.ItemIndex <= 0)
                return ItemExchangePlan.Fail("exchange_lingpai", "exchange_lingpai_compose", "missing soul stone");
            if (input.GivenItemCount > 1)
                return ItemExchangePlan.Fail("exchange_lingpai", "exchange_lingpai_compose", "only one soul stone per exchange");
            if (input.Genre != 6 || input.Detail != 1 || input.Particular != 2356)
                return ItemExchangePlan.Fail("exchange_lingpai", "exchange_lingpai_compose", "requires soul stone 6/1/2356");
            if (input.MagicLevel < definition.RequiredMagicLevel)
                return ItemExchangePlan.Fail("exchange_lingpai", "exchange_lingpai_compose", "not enough soul-stone energy");
            if (input.FreeBagCells < 1)
                return ItemExchangePlan.Fail("exchange_lingpai", "exchange_lingpai_confirm", "bag has no free cell");

            var plan = ItemExchangePlan.Ok("exchange_lingpai", "exchange_lingpai_confirm");
            plan.AwardName = definition.NameVi;
            plan.RequiredMagicLevel = definition.RequiredMagicLevel;
            plan.OverflowMagicLevel = Math.Max(0, input.MagicLevel - definition.RequiredMagicLevel);
            plan.Commands.Add(ItemExchangeHostCommand.Create("WriteLog", definition.NameVi, input.MagicLevel, definition.RequiredMagicLevel));
            plan.Commands.Add(ItemExchangeHostCommand.Create("RemoveItemByIndex", null, input.ItemIndex));
            if (plan.OverflowMagicLevel > 0)
                plan.Commands.Add(ItemExchangeHostCommand.Create("AddGoldItem", "Hồn Thạch hoàn trả", 0, 6, 1, 2356, 1, 0, 0, plan.OverflowMagicLevel));
            plan.Commands.Add(ItemExchangeHostCommand.Create("AddGoldItem", definition.NameVi, 0, definition.Genre, definition.Detail, definition.Particular, 1, 0, 0, 0));
            plan.Commands.Add(ItemExchangeHostCommand.Create("WriteLog", "Exchange Token", definition.RequiredMagicLevel));
            return plan;
        }

        public static ItemExchangePlan BuildJinglianPutInPlan(ItemExchangePlanInput input)
        {
            if (input == null) return ItemExchangePlan.Fail("jinglian_putin", "PutIn", "missing input");
            if (input.ItemIndex <= 0) return ItemExchangePlan.Fail("jinglian_putin", "PutIn", "missing soul stone box");
            if (input.ConsumeCount <= 0) return ItemExchangePlan.Fail("jinglian_putin", "PutIn", "consume count must be positive");
            if (input.Energy < input.ConsumeCount) return ItemExchangePlan.Fail("jinglian_putin", "PutIn", "not enough energy");

            var plan = ItemExchangePlan.Ok("jinglian_putin", "PutIn.pProductFun");
            plan.AwardName = "Tinh lực nhập Hồn Thạch";
            int newMagicLevel = input.MagicLevel + input.ConsumeCount;
            plan.Commands.Add(ItemExchangeHostCommand.Create("ConsumeItem", "ReduceEnergy", input.ConsumeCount));
            plan.Commands.Add(ItemExchangeHostCommand.Create("SetItemMagicLevel", null, input.ItemIndex, 1, newMagicLevel));
            plan.Commands.Add(ItemExchangeHostCommand.Create("SyncItem", null, input.ItemIndex));
            plan.Commands.Add(ItemExchangeHostCommand.Create("SetItemBindState", null, input.ItemIndex, input.BindState));
            plan.Commands.Add(ItemExchangeHostCommand.Create("WriteLog", "Jinglian PutIn", newMagicLevel));
            return plan;
        }


        // -----------------------------------------------------------------
        // ExecutePlan — apply ItemExchangePlan.Commands vào IItemExchangeInventory.
        // PC source: Server 6.0/script/misc/itemexchangevalue/itemexchangevalue.lua
        // + itemexchange_setting/{normal,rare,level_exp,rolevalue}.*
        //
        // Dispatch theo ApiName:
        //   WriteLog            → inv.WriteLog(TextArg)
        //   RemoveItemByIndex   → inv.TakeItem(IntArgs[0])
        //   AddGoldItem         → inv.GiveItem(genre, detail, particular, level=1, count, magicLevel)
        //   AddItem             → inv.GiveItem(genre, detail, particular, level, count)
        //   AddItemEx           → inv.GiveItem(genre, detail, particular, level, count, magicLevel)
        //   GiveGold            → inv.GiveGold(amount)
        //   ConsumeItem         → inv.ConsumeItem(itemIndex=0, count) (jinglian)
        //   SetItemMagicLevel   → inv.SetItemMagicLevel(itemIndex, newMagicLevel)
        //   SyncItem            → inv.SyncItem(itemIndex)
        //   SetItemBindState    → inv.SetItemBindState(itemIndex, bindState)
        //
        // Pre-flight: đếm số ô GiveItem/GiveGold sẽ chiếm; nếu vượt FreeBagCells
        // thì trả false NGAY TRƯỚC khi remove gì (PC semantic: preflight inventory).
        // Rollback: nếu GiveItem fail sau khi RemoveItemByIndex đã thành công,
        // hoàn trả các item đã remove theo thứ tự ngược.
        // -----------------------------------------------------------------
        public bool ExecutePlan(ItemExchangePlan plan, IItemExchangeInventory inv, out string error)
        {
            error = string.Empty;
            if (plan == null) { error = "null plan"; return false; }
            if (!plan.Success) { error = plan.FailureReason; return false; }
            if (inv == null) { error = "null inventory"; return false; }
            if (plan.Commands == null || plan.Commands.Count == 0) return true;

            // --- Pre-flight: đếm ô yêu cầu ---
            int giveCount = 0;
            foreach (var cmd in plan.Commands)
            {
                if (cmd.ApiName == "AddGoldItem") giveCount++;
                else if (cmd.ApiName == "AddItem") giveCount++;
                else if (cmd.ApiName == "AddItemEx") giveCount++;
                else if (cmd.ApiName == "GiveGold") giveCount++;
            }
            if (giveCount > inv.FreeBagCells())
            {
                error = $"InsufficientBagCells: cần {giveCount}, có {inv.FreeBagCells()}";
                return false;
            }

            // --- Execute commands với rollback tracking ---
            var takenItems = new System.Collections.Generic.List<(int index, int count)>();
            try
            {
                foreach (var cmd in plan.Commands)
                {
                    switch (cmd.ApiName)
                    {
                        case "WriteLog":
                            inv.WriteLog(cmd.TextArg ?? string.Empty);
                            break;

                        case "RemoveItemByIndex":
                            {
                                int idx = IntArg(cmd, 0);
                                if (!inv.TakeItem(idx))
                                {
                                    RollbackTakes(inv, takenItems);
                                    error = $"RemoveItemByIndex({idx}) failed: thiếu item";
                                    return false;
                                }
                                takenItems.Add((idx, 1));
                            }
                            break;

                        case "AddGoldItem":
                            {
                                // [arg0, genre, detail, particular, count, _, _, _, magicLevel]
                                int genre = IntArg(cmd, 1);
                                int detail = IntArg(cmd, 2);
                                int particular = IntArg(cmd, 3);
                                int count = IntArg(cmd, 4);
                                int magicLevel = IntArg(cmd, 8);
                                if (!inv.GiveItem(genre, detail, particular, 1, count, magicLevel))
                                {
                                    RollbackTakes(inv, takenItems);
                                    error = $"AddGoldItem({genre}/{detail}/{particular}) failed";
                                    return false;
                                }
                            }
                            break;

                        case "AddItem":
                            {
                                int genre = IntArg(cmd, 0);
                                int detail = IntArg(cmd, 1);
                                int particular = IntArg(cmd, 2);
                                int level = IntArg(cmd, 3);
                                int count = IntArg(cmd, 4);
                                if (!inv.GiveItem(genre, detail, particular, level, count))
                                {
                                    RollbackTakes(inv, takenItems);
                                    error = $"AddItem({genre}/{detail}/{particular}) failed";
                                    return false;
                                }
                            }
                            break;

                        case "AddItemEx":
                            {
                                int genre = IntArg(cmd, 0);
                                int detail = IntArg(cmd, 1);
                                int particular = IntArg(cmd, 2);
                                int level = IntArg(cmd, 3);
                                int count = IntArg(cmd, 4);
                                int magicLevel = IntArg(cmd, 5);
                                if (!inv.GiveItem(genre, detail, particular, level, count, magicLevel))
                                {
                                    RollbackTakes(inv, takenItems);
                                    error = $"AddItemEx({genre}/{detail}/{particular}) failed";
                                    return false;
                                }
                            }
                            break;

                        case "GiveGold":
                            {
                                int amount = IntArg(cmd, 0);
                                if (!inv.GiveGold(amount))
                                {
                                    RollbackTakes(inv, takenItems);
                                    error = $"GiveGold({amount}) failed";
                                    return false;
                                }
                            }
                            break;

                        case "ConsumeItem":
                            {
                                // PC PutIn: [consumeCount, ...] — trừ energy theo count.
                                int consume = IntArg(cmd, 0);
                                if (!inv.ConsumeItem(0, consume))
                                {
                                    error = $"ConsumeItem({consume}) failed";
                                    return false;
                                }
                            }
                            break;

                        case "SetItemMagicLevel":
                            {
                                int idx = IntArg(cmd, 0);
                                int newMagicLevel = IntArg(cmd, 2);
                                inv.SetItemMagicLevel(idx, newMagicLevel);
                            }
                            break;

                        case "SyncItem":
                            inv.SyncItem(IntArg(cmd, 0));
                            break;

                        case "SetItemBindState":
                            inv.SetItemBindState(IntArg(cmd, 0), IntArg(cmd, 1));
                            break;

                        default:
                            // Unknown command — skip gracefully (forward-compat).
                            SubsystemLog.Warn(LogTag, $"ExecutePlan: unknown ApiName '{cmd.ApiName}' — skipped");
                            break;
                    }
                }
                return true;
            }
            catch (System.Exception e)
            {
                RollbackTakes(inv, takenItems);
                error = $"exception: {e.Message}";
                return false;
            }
        }

        private static void RollbackTakes(IItemExchangeInventory inv, System.Collections.Generic.List<(int index, int count)> taken)
        {
            // Hoàn trả theo thứ tự ngược để khôi phục gần đúng thứ tự gốc.
            for (int i = taken.Count - 1; i >= 0; i--)
            {
                try { inv.GiveItem(0, 0, taken[i].index, 1, taken[i].count); }
                catch { /* best-effort rollback; log only */ }
            }
            taken.Clear();
        }

        private static int IntArg(ItemExchangeHostCommand cmd, int index)
        {
            return (cmd.IntArgs != null && index >= 0 && index < cmd.IntArgs.Count)
                ? cmd.IntArgs[index] : 0;
        }

        public static ItemExchangeLingpaiDefinition FindLingpaiDefinition(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            foreach (var definition in LingpaiDefinitions)
            {
                if (string.Equals(definition.NameVi, name, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(definition.PcFunction, name, StringComparison.OrdinalIgnoreCase))
                    return definition;
            }
            return null;
        }
    }
}
