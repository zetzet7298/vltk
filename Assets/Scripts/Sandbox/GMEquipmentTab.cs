using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public class GMEquipmentTab : MonoBehaviour
    {
        private string _activeCategory = "Helmet";
        private int _selectedItemId = -1;
        private string _searchQuery = "";

        private RectTransform _contentTrans;
        private InputField _searchInput;
        private Text _statusText;

        private Button _btnHelmet;
        private Button _btnArmor;
        private Button _btnWeapon;
        private Button _btnEquip;

        private readonly List<ItemRow> _itemRows = new();

        private class ItemRow
        {
            public ItemDefinition item;
            public GameObject rowGo;
            public Image bgImage;
        }

        private void Start()
        {
            // Background
            var bgImage = gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.11f, 0.16f, 0.95f);

            // Title
            CreateTitle();

            // Left Side Panel
            CreateLeftControlPanel();

            // Right Side ScrollView
            CreateRightScrollView();

            // Initial refresh
            RefreshCategoryButtons();
            RefreshItemList();
        }

        private void CreateTitle()
        {
            var titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(transform, false);
            var rect = titleGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.88f);
            rect.anchorMax = new Vector2(0.95f, 0.96f);
            rect.sizeDelta = Vector2.zero;

            var txt = titleGo.AddComponent<Text>();
            txt.text = "TRANG BỊ NGƯỜI CHƠI (GM PANEL)";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            txt.fontSize = 16;
            txt.fontStyle = FontStyle.Bold;
            txt.color = new Color(0.9f, 0.95f, 1f, 1f);
            txt.alignment = TextAnchor.MiddleLeft;
        }

        private void CreateLeftControlPanel()
        {
            // Search Input Label
            CreateLabel("Tìm kiếm trang bị:", new Vector2(0.05f, 0.78f), new Vector2(0.3f, 0.83f));

            // Search InputField
            var inputGo = new GameObject("SearchInput");
            inputGo.transform.SetParent(transform, false);
            var inputRect = inputGo.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.05f, 0.70f);
            inputRect.anchorMax = new Vector2(0.3f, 0.76f);
            inputRect.sizeDelta = Vector2.zero;

            var inputBg = inputGo.AddComponent<Image>();
            inputBg.color = new Color(0.12f, 0.15f, 0.22f, 1f);

            _searchInput = inputGo.AddComponent<InputField>();
            
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(inputGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-10, -10); // padding

            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;

            _searchInput.textComponent = text;
            _searchInput.onValueChanged.AddListener((val) =>
            {
                _searchQuery = val;
                RefreshItemList();
            });

            // Category Buttons
            _btnHelmet = CreateGMButton("MŨ (NÓN)", new Vector2(0.05f, 0.56f), new Vector2(0.3f, 0.63f), () =>
            {
                _activeCategory = "Helmet";
                _selectedItemId = -1;
                RefreshCategoryButtons();
                RefreshItemList();
            });

            _btnArmor = CreateGMButton("ÁO (GIÁP)", new Vector2(0.05f, 0.46f), new Vector2(0.3f, 0.53f), () =>
            {
                _activeCategory = "Armor";
                _selectedItemId = -1;
                RefreshCategoryButtons();
                RefreshItemList();
            });

            _btnWeapon = CreateGMButton("VŨ KHÍ", new Vector2(0.05f, 0.36f), new Vector2(0.3f, 0.43f), () =>
            {
                _activeCategory = "Weapon";
                _selectedItemId = -1;
                RefreshCategoryButtons();
                RefreshItemList();
            });

            // Equip Button
            _btnEquip = CreateGMButton("TRANG BỊ (LOAD)", new Vector2(0.05f, 0.18f), new Vector2(0.3f, 0.27f), () =>
            {
                EquipSelectedItem();
            });
            // Customize equip button style
            var equipImg = _btnEquip.GetComponent<Image>();
            if (equipImg != null) equipImg.color = new Color(0.15f, 0.55f, 0.40f, 1f);

            // Status Text
            var statusGo = new GameObject("StatusText");
            statusGo.transform.SetParent(transform, false);
            var statusRect = statusGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0.08f);
            statusRect.anchorMax = new Vector2(0.3f, 0.15f);
            statusRect.sizeDelta = Vector2.zero;

            _statusText = statusGo.AddComponent<Text>();
            _statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_statusText.font == null) _statusText.font = Font.CreateDynamicFontFromOSFont("Arial", 12);
            _statusText.fontSize = 12;
            _statusText.color = new Color(0.7f, 0.8f, 0.9f, 1f);
            _statusText.text = "Chưa chọn trang bị";
            _statusText.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateRightScrollView()
        {
            // ScrollView container
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(transform, false);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var sRectTrans = scrollGo.GetComponent<RectTransform>();
            sRectTrans.anchorMin = new Vector2(0.35f, 0.08f);
            sRectTrans.anchorMax = new Vector2(0.95f, 0.84f);
            sRectTrans.sizeDelta = Vector2.zero;

            // Viewport
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var vRectTrans = viewportGo.AddComponent<RectTransform>();
            vRectTrans.anchorMin = Vector2.zero;
            vRectTrans.anchorMax = Vector2.one;
            vRectTrans.sizeDelta = Vector2.zero;
            viewportGo.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.10f, 0.9f);
            viewportGo.AddComponent<Mask>();

            // Content
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            _contentTrans = contentGo.AddComponent<RectTransform>();
            _contentTrans.anchorMin = new Vector2(0f, 1f);
            _contentTrans.anchorMax = new Vector2(1f, 1f);
            _contentTrans.pivot = new Vector2(0.5f, 1f);
            _contentTrans.anchoredPosition = Vector2.zero;
            _contentTrans.sizeDelta = new Vector2(0, 0);

            var layout = contentGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 3f;
            layout.padding = new RectOffset(6, 6, 6, 6);

            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vRectTrans;
            scrollRect.content = _contentTrans;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 25f;
        }

        private void CreateLabel(string text, Vector2 min, Vector2 max)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.sizeDelta = Vector2.zero;

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 12);
            txt.fontSize = 12;
            txt.color = new Color(0.7f, 0.8f, 0.9f, 1f);
            txt.alignment = TextAnchor.MiddleLeft;
        }

        private Button CreateGMButton(string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject("Button_" + label);
            btnGo.transform.SetParent(transform, false);
            var rect = btnGo.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.sizeDelta = Vector2.zero;

            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.18f, 0.24f, 0.35f, 1f);

            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 13);
            txt.fontSize = 13;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }

        private void RefreshCategoryButtons()
        {
            Color activeColor = new Color(0.25f, 0.45f, 0.8f, 1f);
            Color inactiveColor = new Color(0.18f, 0.24f, 0.35f, 1f);

            if (_btnHelmet != null) _btnHelmet.GetComponent<Image>().color = _activeCategory == "Helmet" ? activeColor : inactiveColor;
            if (_btnArmor != null) _btnArmor.GetComponent<Image>().color = _activeCategory == "Armor" ? activeColor : inactiveColor;
            if (_btnWeapon != null) _btnWeapon.GetComponent<Image>().color = _activeCategory == "Weapon" ? activeColor : inactiveColor;
        }

        private List<ItemDefinition> GetItemsByCategory(string category)
        {
            var results = new List<ItemDefinition>();
            var mgr = SandboxManager.Instance;
            if (mgr == null || mgr.ItemDb == null) return results;

            var all = mgr.ItemDb.AllItems;
            foreach (var item in all)
            {
                int catId = item.itemId / 100000;
                bool isMockWeapon = (item.itemId >= 1001 && item.itemId <= 1042);
                bool isMockArmor = (item.itemId >= 2001 && item.itemId <= 2004);
                bool isMockHelmet = (item.itemId >= 3001 && item.itemId <= 3003);

                if (category == "Helmet" && (catId == 2 || isMockHelmet))
                {
                    results.Add(item);
                }
                else if (category == "Armor" && (catId == 1 || isMockArmor))
                {
                    results.Add(item);
                }
                else if (category == "Weapon" && (catId == 9 || catId == 10 || isMockWeapon))
                {
                    results.Add(item);
                }
            }

            // Filter search
            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                string q = _searchQuery.Trim().ToLowerInvariant();
                bool isId = int.TryParse(q, out var idVal);
                results = results.FindAll(it => 
                    (isId && it.itemId == idVal) || 
                    (!string.IsNullOrEmpty(it.nameRaw) && it.nameRaw.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(it.nameNormalized) && it.nameNormalized.ToLowerInvariant().Contains(q))
                );
            }

            results.Sort((a, b) => a.itemId.CompareTo(b.itemId));
            return results;
        }

        private void RefreshItemList()
        {
            // Clear existing rows
            foreach (var r in _itemRows)
            {
                Destroy(r.rowGo);
            }
            _itemRows.Clear();

            var items = GetItemsByCategory(_activeCategory);
            foreach (var item in items)
            {
                var rowGo = new GameObject("ItemRow_" + item.itemId);
                rowGo.transform.SetParent(_contentTrans, false);
                var rowRect = rowGo.AddComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(0, 32); // height of row

                var bgImg = rowGo.AddComponent<Image>();
                bgImg.color = item.itemId == _selectedItemId ? new Color(0.2f, 0.35f, 0.6f, 0.7f) : new Color(0.1f, 0.13f, 0.18f, 0.5f);

                var btn = rowGo.AddComponent<Button>();
                int id = item.itemId;
                btn.onClick.AddListener(() =>
                {
                    SelectRow(id);
                });

                var txtGo = new GameObject("Text");
                txtGo.transform.SetParent(rowGo.transform, false);
                var txtRect = txtGo.AddComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = new Vector2(-20, 0); // margin left/right

                var txt = txtGo.AddComponent<Text>();
                txt.text = $"[{item.itemId}] {item.DisplayName}  -  ResId: {item.resId}";
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 13);
                txt.fontSize = 13;
                txt.color = new Color(0.9f, 0.9f, 0.95f, 1f);
                txt.alignment = TextAnchor.MiddleLeft;

                _itemRows.Add(new ItemRow
                {
                    item = item,
                    rowGo = rowGo,
                    bgImage = bgImg
                });
            }
        }

        private void SelectRow(int itemId)
        {
            _selectedItemId = itemId;
            var mgr = SandboxManager.Instance;
            if (mgr != null && mgr.ItemDb != null)
            {
                var it = mgr.ItemDb.Resolve(itemId);
                if (it != null)
                {
                    _statusText.text = $"Đã chọn: {it.DisplayName} (ResId: {it.resId})";
                }
            }

            // Update row backgrounds
            foreach (var row in _itemRows)
            {
                row.bgImage.color = row.item.itemId == _selectedItemId 
                    ? new Color(0.2f, 0.35f, 0.6f, 0.7f) 
                    : new Color(0.1f, 0.13f, 0.18f, 0.5f);
            }
        }

        private void EquipSelectedItem()
        {
            if (_selectedItemId <= 0)
            {
                _statusText.text = "Vui lòng chọn trang bị!";
                return;
            }

            var mgr = SandboxManager.Instance;
            if (mgr == null || mgr.InventoryService == null)
            {
                _statusText.text = "Lỗi hệ thống!";
                return;
            }

            EquipSlot slot;
            if (_activeCategory == "Helmet")
                slot = EquipSlot.Helmet;
            else if (_activeCategory == "Armor")
                slot = EquipSlot.Armor;
            else
                slot = EquipSlot.Weapon;

            try
            {
                mgr.InventoryService.Equip(slot, _selectedItemId);
                _statusText.text = $"Đã load trang bị ID: {_selectedItemId} thành công!";
                SubsystemLog.Info("GMEquipmentTab", $"GM equipped item {_selectedItemId} to slot {slot}");
            }
            catch (Exception ex)
            {
                _statusText.text = "Lỗi khi trang bị!";
                SubsystemLog.Error("GMEquipmentTab", $"Failed to equip item {_selectedItemId} to slot {slot}: {ex.Message}");
            }
        }
    }
}
