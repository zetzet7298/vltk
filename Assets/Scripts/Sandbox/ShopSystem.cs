// -----------------------------------------------------------------------------
// VLTK Mobile — Shop/Trade System
// NPC shop, buy/sell items, price calculation.
// PC source: NPC shop dialogs, item pricing from item tables.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Shop item entry with pricing.</summary>
    [Serializable]
    public class ShopEntry
    {
        public int itemId;
        public string nameVi;
        public int buyPrice;        // Giá mua (Bạc)
        public int sellPrice;       // Giá bán (Bạc, thường = buyPrice / 4)
        public int stock;           // -1 = vô hạn
        public int requiredLevel;   // Cấp yêu cầu
        public string categoryVi;   // Phân loại
    }

    /// <summary>Shop type from PC data.</summary>
    public enum ShopType
    {
        General = 0,    // Tiệm tổng hợp
        Weapon = 1,     // Tiệm vũ khí
        Armor = 2,      // Tiệm giáp
        Medicine = 3,   // Tiệm thuốc
        Material = 4,   // Tiệm nguyên liệu
        Special = 5,    // Tiệm đặc biệt
    }

    /// <summary>
    /// Shop service — manages NPC shops, buy/sell logic, pricing.
    /// Pure C#, fully testable.
    /// </summary>
    public class ShopService
    {
        private readonly ItemDatabase _itemDb;
        private readonly Dictionary<int, List<ShopEntry>> _shops = new(); // npcTemplateId → items
        private int _playerSilver;

        public int PlayerSilver => _playerSilver;
        public event Action<int> OnSilverChanged;
        public event Action<string> OnTransaction;

        public ShopService(ItemDatabase itemDb, int initialSilver = 1000)
        {
            _itemDb = itemDb;
            _playerSilver = initialSilver;
            LoadDefaultShops();
        }

        /// <summary>Get shop items for a specific NPC.</summary>
        public List<ShopEntry> GetShopItems(int npcTemplateId)
        {
            return _shops.TryGetValue(npcTemplateId, out var items) ? items : new List<ShopEntry>();
        }

        /// <summary>Buy an item from shop.</summary>
        public bool BuyItem(int npcTemplateId, int itemId, int count = 1)
        {
            var items = GetShopItems(npcTemplateId);
            var entry = items.Find(e => e.itemId == itemId);
            if (entry == null)
            {
                SubsystemLog.Warn("Shop", $"Item {itemId} không có trong cửa hàng");
                return false;
            }

            int totalCost = entry.buyPrice * count;
            if (_playerSilver < totalCost)
            {
                SubsystemLog.Warn("Shop", $"Không đủ Bạc ({_playerSilver} < {totalCost})");
                return false;
            }

            if (entry.stock > 0)
            {
                if (entry.stock < count) { return false; }
                entry.stock -= count;
            }

            _playerSilver -= totalCost;
            OnSilverChanged?.Invoke(_playerSilver);
            OnTransaction?.Invoke($"Mua {entry.nameVi} x{count} → -{totalCost} Bạc");
            SubsystemLog.Info("Shop", $"Mua: {entry.nameVi} x{count} (-{totalCost} Bạc, còn {_playerSilver})");
            return true;
        }

        /// <summary>Sell an item to shop.</summary>
        public int SellItem(int itemId, int count = 1)
        {
            var itemDef = _itemDb?.Resolve(itemId);
            if (itemDef == null) return 0;

            // Sell price = 25% of a computed base value
            int sellPrice = ComputeSellPrice(itemId) * count;
            _playerSilver += sellPrice;
            OnSilverChanged?.Invoke(_playerSilver);
            OnTransaction?.Invoke($"Bán {itemDef.DisplayName} x{count} → +{sellPrice} Bạc");
            SubsystemLog.Info("Shop", $"Bán: {itemDef.DisplayName} x{count} (+{sellPrice} Bạc)");
            return sellPrice;
        }

        public void AddSilver(int amount)
        {
            _playerSilver += amount;
            OnSilverChanged?.Invoke(_playerSilver);
        }

        public bool SpendSilver(int amount)
        {
            if (_playerSilver < amount) return false;
            _playerSilver -= amount;
            OnSilverChanged?.Invoke(_playerSilver);
            return true;
        }

        private int ComputeSellPrice(int itemId)
        {
            var item = _itemDb?.Resolve(itemId);
            if (item == null) return 1;
            int attack = item.SumAttr(ItemDatabase.ATTR_ATTACK, ItemStatStage.Base);
            int defense = item.SumAttr(ItemDatabase.ATTR_DEFENSE, ItemStatStage.Base);
            return Mathf.Max(5, (attack + defense) * 3);
        }

        private void LoadDefaultShops()
        {
            // Võ Sư shop (templateId 311) — training/basic items
            _shops[311] = new List<ShopEntry>
            {
                new() { itemId = 7001, nameVi = "Thuốc Hồi Máu", buyPrice = 50, sellPrice = 12, stock = -1, requiredLevel = 1, categoryVi = "Thuốc" },
                new() { itemId = 7002, nameVi = "Thuốc Hồi Khí", buyPrice = 60, sellPrice = 15, stock = -1, requiredLevel = 1, categoryVi = "Thuốc" },
                new() { itemId = 7003, nameVi = "Thuốc Hồi Sinh", buyPrice = 500, sellPrice = 125, stock = -1, requiredLevel = 1, categoryVi = "Thuốc" },
                new() { itemId = 1001, nameVi = "Thanh Kiếm Sắt", buyPrice = 200, sellPrice = 50, stock = -1, requiredLevel = 1, categoryVi = "Vũ Khí" },
                new() { itemId = 2001, nameVi = "Áo Vải", buyPrice = 150, sellPrice = 37, stock = -1, requiredLevel = 1, categoryVi = "Giáp" },
            };

            // Tiệm vũ khí (templateId 601)
            _shops[601] = new List<ShopEntry>
            {
                new() { itemId = 1001, nameVi = "Thanh Kiếm Sắt", buyPrice = 200, sellPrice = 50, stock = -1, requiredLevel = 1, categoryVi = "Kiếm" },
                new() { itemId = 1002, nameVi = "Đao Luyện", buyPrice = 250, sellPrice = 62, stock = -1, requiredLevel = 3, categoryVi = "Đao" },
                new() { itemId = 1021, nameVi = "Trượng Thiết", buyPrice = 280, sellPrice = 70, stock = -1, requiredLevel = 5, categoryVi = "Trượng" },
                new() { itemId = 1041, nameVi = "Song Kiếm", buyPrice = 350, sellPrice = 87, stock = -1, requiredLevel = 8, categoryVi = "Song Khí" },
                new() { itemId = 1003, nameVi = "Kiếm Thanh Phong", buyPrice = 800, sellPrice = 200, stock = -1, requiredLevel = 10, categoryVi = "Kiếm" },
                new() { itemId = 1004, nameVi = "Đao Xích Thố", buyPrice = 900, sellPrice = 225, stock = -1, requiredLevel = 12, categoryVi = "Đao" },
            };

            // Tiệm giáp (templateId 602)
            _shops[602] = new List<ShopEntry>
            {
                new() { itemId = 2001, nameVi = "Áo Vải", buyPrice = 150, sellPrice = 37, stock = -1, requiredLevel = 1, categoryVi = "Giáp" },
                new() { itemId = 2002, nameVi = "Áo Da", buyPrice = 400, sellPrice = 100, stock = -1, requiredLevel = 5, categoryVi = "Giáp" },
                new() { itemId = 3001, nameVi = "Mũ Vải", buyPrice = 80, sellPrice = 20, stock = -1, requiredLevel = 1, categoryVi = "Mũ" },
                new() { itemId = 4001, nameVi = "Giày Vải", buyPrice = 60, sellPrice = 15, stock = -1, requiredLevel = 1, categoryVi = "Giày" },
                new() { itemId = 2003, nameVi = "Giáp Sắt", buyPrice = 1200, sellPrice = 300, stock = -1, requiredLevel = 10, categoryVi = "Giáp" },
                new() { itemId = 3002, nameVi = "Mũ Sắt", buyPrice = 500, sellPrice = 125, stock = -1, requiredLevel = 8, categoryVi = "Mũ" },
                new() { itemId = 4002, nameVi = "Giày Da", buyPrice = 350, sellPrice = 87, stock = -1, requiredLevel = 5, categoryVi = "Giày" },
            };

            // Tiệm thuốc (templateId 603)
            _shops[603] = new List<ShopEntry>
            {
                new() { itemId = 7001, nameVi = "Thuốc Hồi Máu", buyPrice = 50, sellPrice = 12, stock = -1, requiredLevel = 1, categoryVi = "Thuốc" },
                new() { itemId = 7002, nameVi = "Thuốc Hồi Khí", buyPrice = 60, sellPrice = 15, stock = -1, requiredLevel = 1, categoryVi = "Thuốc" },
                new() { itemId = 7003, nameVi = "Thuốc Hồi Sinh", buyPrice = 500, sellPrice = 125, stock = -1, requiredLevel = 5, categoryVi = "Thuốc" },
                new() { itemId = 7004, nameVi = "Thuốc Tốc Độ", buyPrice = 200, sellPrice = 50, stock = -1, requiredLevel = 10, categoryVi = "Thuốc" },
                new() { itemId = 5001, nameVi = "Dây Chuyền Đồng", buyPrice = 300, sellPrice = 75, stock = -1, requiredLevel = 5, categoryVi = "Trang Sức" },
                new() { itemId = 6001, nameVi = "Nhẫn Đồng", buyPrice = 250, sellPrice = 62, stock = -1, requiredLevel = 5, categoryVi = "Trang Sức" },
            };
        }
    }

    /// <summary>
    /// Shop UI panel — shows shop items, buy/sell interface.
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        private ShopService _shop;
        private InventoryService _inventory;
        private ItemDatabase _itemDb;
        private GameObject _panelRoot;
        private Transform _itemListRoot;
        private Text _silverText;
        private int _currentShopNpcId;
        private Font _font;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        public void Initialize(ShopService shop, InventoryService inventory, ItemDatabase itemDb)
        {
            _shop = shop;
            _inventory = inventory;
            _itemDb = itemDb;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 14);
            BuildUI();
        }

        public void OpenShop(int npcTemplateId)
        {
            _currentShopNpcId = npcTemplateId;
            _isOpen = true;
            if (_panelRoot != null)
                _panelRoot.SetActive(true);
            Refresh();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            if (_panelRoot != null)
                _panelRoot.SetActive(_isOpen);
            if (_isOpen) Refresh();
        }

        public void Refresh()
        {
            if (_itemListRoot == null || _shop == null) return;
            for (int i = _itemListRoot.childCount - 1; i >= 0; i--)
                Destroy(_itemListRoot.GetChild(i).gameObject);

            var items = _shop.GetShopItems(_currentShopNpcId);
            if (items.Count == 0)
            {
                AddLabel(_itemListRoot, "  Cửa hàng trống", 22, new Color(0.6f, 0.6f, 0.6f));
                return;
            }

            foreach (var entry in items)
            {
                var row = new GameObject($"Item_{entry.itemId}");
                row.transform.SetParent(_itemListRoot, false);
                var rowRt = row.AddComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(0f, 36f);
                var rowImg = row.AddComponent<Image>();
                rowImg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

                // Label
                var lbl = new GameObject("Label");
                lbl.transform.SetParent(row.transform, false);
                var lRt = lbl.AddComponent<RectTransform>();
                lRt.anchorMin = Vector2.zero; lRt.anchorMax = new Vector2(0.7f, 1f);
                lRt.offsetMin = new Vector4(6, 0, 0, 0);
                var lTxt = lbl.AddComponent<Text>();
                lTxt.text = $"  {entry.nameVi} — {entry.buyPrice} Bạc";
                lTxt.font = _font;
                lTxt.fontSize = 20;
                lTxt.color = Color.white;
                lTxt.alignment = TextAnchor.MiddleLeft;

                // Buy button
                var buyGo = new GameObject("BuyBtn");
                buyGo.transform.SetParent(row.transform, false);
                var bRt = buyGo.AddComponent<RectTransform>();
                bRt.anchorMin = new Vector2(0.7f, 0.05f);
                bRt.anchorMax = new Vector2(0.98f, 0.95f);
                var bImg = buyGo.AddComponent<Image>();
                bImg.color = new Color(0.15f, 0.45f, 0.2f, 0.9f);
                var bBtn = buyGo.AddComponent<Button>();
                bBtn.targetGraphic = bImg;
                int itemId = entry.itemId;
                bBtn.onClick.AddListener(() =>
                {
                    if (_shop.BuyItem(_currentShopNpcId, itemId))
                    {
                        _inventory?.AddItem(itemId);
                        Refresh();
                    }
                });
                var bTxt = buyGo.AddComponent<Text>();
                bTxt.text = "Mua";
                bTxt.font = _font;
                bTxt.fontSize = 18;
                bTxt.color = Color.white;
                bTxt.alignment = TextAnchor.MiddleCenter;
            }

            if (_silverText != null)
                _silverText.text = $"Bạc: {_shop.PlayerSilver}";
        }

        private GameObject AddLabel(Transform parent, string text, int fontSize, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, fontSize + 8);
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = _font;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            return go;
        }

        private void BuildUI()
        {
            _panelRoot = new GameObject("ShopPanel");
            _panelRoot.transform.SetParent(transform, false);
            _panelRoot.SetActive(false);

            var mainRt = _panelRoot.AddComponent<RectTransform>();
            mainRt.anchorMin = new Vector2(0.15f, 0.12f);
            mainRt.anchorMax = new Vector2(0.85f, 0.88f);

            var bg = _panelRoot.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.06f, 0.04f, 0.95f);

            // Title
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(_panelRoot.transform, false);
            var tRt = titleBar.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 0.92f);
            tRt.anchorMax = new Vector2(1f, 1f);
            var tBg = titleBar.AddComponent<Image>();
            tBg.color = new Color(0.18f, 0.3f, 0.12f, 0.95f);

            var titleTextGo = new GameObject("TitleText");
            titleTextGo.transform.SetParent(titleBar.transform, false);
            var ttRt = titleTextGo.AddComponent<RectTransform>();
            ttRt.anchorMin = Vector2.zero;
            ttRt.anchorMax = Vector2.one;
            ttRt.sizeDelta = Vector2.zero;
            var tTxt = titleTextGo.AddComponent<Text>();
            tTxt.text = "Cửa Hàng";
            tTxt.font = _font;
            tTxt.fontSize = 32;
            tTxt.color = new Color(0.9f, 1f, 0.8f);
            tTxt.alignment = TextAnchor.MiddleCenter;

            // Close
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(titleBar.transform, false);
            var cRt = closeGo.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.9f, 0f);
            cRt.anchorMax = new Vector2(1f, 1f);
            var cImg = closeGo.AddComponent<Image>();
            cImg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            var cBtn = closeGo.AddComponent<Button>();
            cBtn.targetGraphic = cImg;
            cBtn.onClick.AddListener(() => Toggle());
            var cTxt = closeGo.AddComponent<Text>();
            cTxt.text = "✕";
            cTxt.font = _font;
            cTxt.fontSize = 28;
            cTxt.color = Color.white;
            cTxt.alignment = TextAnchor.MiddleCenter;

            // Silver bar
            var silverGo = new GameObject("Silver");
            silverGo.transform.SetParent(_panelRoot.transform, false);
            var sRt = silverGo.AddComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.02f, 0.87f);
            sRt.anchorMax = new Vector2(0.5f, 0.92f);
            _silverText = silverGo.AddComponent<Text>();
            _silverText.font = _font;
            _silverText.fontSize = 22;
            _silverText.color = new Color(1f, 0.9f, 0.4f);
            _silverText.alignment = TextAnchor.MiddleLeft;

            // Item list
            var listGo = new GameObject("ItemList");
            listGo.transform.SetParent(_panelRoot.transform, false);
            var lRt = listGo.AddComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0.02f, 0.02f);
            lRt.anchorMax = new Vector2(0.98f, 0.86f);
            _itemListRoot = listGo.transform;
            var vl = listGo.AddComponent<VerticalLayoutGroup>();
            vl.childAlignment = TextAnchor.UpperLeft;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.spacing = 3f;
            vl.padding = new RectOffset(4, 4, 4, 4);
        }
    }
}
