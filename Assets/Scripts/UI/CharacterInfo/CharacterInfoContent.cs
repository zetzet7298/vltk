// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Character Info popup content (PC parity, combined panel)
// Renders the PC combined "TRANG BỊ VÀ THUỘC TÍNH" panel (config UID 2711122c,
// 428×430). The panel sprite (装备和属性-男/女.spr, UID e3ecbac9/6ce319ab from
// update03.pak) already bakes in every Vietnamese label, the male/female
// silhouette and the equipment slot frames, so this content only overlays:
//   - live stat VALUES at their PC INI coordinates
//   - 12 equipment hit-zones (Cap/Weapon/Cloth/... per 2711122c INI)
//   - the +/- spend buttons (状态加点按钮改.spr, UID 9e87942b from update01.pak)
//   - Item (opens Hành trang) + Close affordances at the bottom
//   - BtnLock/BtnBind/BtnUnBind (disabled until backend lands)
//
// PC source INI: 2711122c (update03.pak winner by package.ini priority 20).
// Art + SHA-256 provenance: Assets/UI/Popup/CharacterInfo/Art/PROVENANCE.md.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Core;
using VLTK.Sandbox;
using VLTK.UI.Popup;

namespace VLTK.UI.CharacterInfo
{
    /// <summary>
    /// Character Info popup content rendered over the real PC combined panel
    /// sprite. Reads <see cref="PcCharacterPanelState"/>; mutates only via the
    /// state callbacks. The +/- spend buttons call DistributePotential and
    /// render their disabled PC frame when no point remains.
    /// </summary>
    public sealed class CharacterInfoContent : IPopupContent, IPopupLayoutHint, IPopupChromeHint
    {
        // PC combined panel: 428×430 (2711122c [Male]/[Female]). Centred in the
        // 1280×720 design space by PopupWindow (PcCharacter chrome).
        public string TitleVi => "Thông Tin Nhân Vật";
        public float Width => 428f;
        public float Height => 430f;
        public float Left => 0f;
        public float Top => 0f;
        public PopupChromeKind Chrome => PopupChromeKind.PcCharacter;

        private const string ArtRoot = "Assets/UI/Popup/CharacterInfo/Art";
        private const string PanelMalePath = ArtRoot + "/panel_male.png";
        private const string PanelFemalePath = ArtRoot + "/panel_female.png";

        private readonly PcCharacterPanelState _state;

        private VisualElement _panel;
        private VisualElement _paperdoll;
        // Stats value labels keyed by INI section name.
        private readonly Dictionary<string, Label> _statLabels = new();
        // +/- buttons keyed by potential kind.
        private readonly Dictionary<PcPotentialKind, VisualElement> _addButtons = new();

        public CharacterInfoContent(PcCharacterPanelState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>Legacy convenience constructor used by tests that only need stats.</summary>
        public CharacterInfoContent(Func<PcStatsSnapshot> statsProvider)
            : this(new PcCharacterPanelState(statsProvider)) { }

        public void Build(VisualElement body)
        {
            body.Clear();
            body.AddToClassList("char-info-body");

            // PC panel sprite — the labels, silhouette and slot frames are baked in.
            // Male/female swapped at OnShow based on PlayerController.isFemale.
            _panel = new VisualElement { name = "Panel" };
            _panel.AddToClassList("char-panel");
            _panel.style.position = Position.Absolute;
            _panel.style.left = 0;
            _panel.style.top = 0;
            _panel.style.width = 428;
            _panel.style.height = 430;
            body.Add(_panel);

            // 12 equipment hit-zones (2711122c INI). Transparent overlays that
            // tint equipped slots; the slot frame itself is in the panel sprite.
            _paperdoll = new VisualElement { name = "Paperdoll" };
            _paperdoll.AddToClassList("char-paperdoll");
            _paperdoll.style.position = Position.Absolute;
            _paperdoll.style.left = 0;
            _paperdoll.style.top = 0;
            _paperdoll.style.width = 428;
            _paperdoll.style.height = 430;
            foreach (var zone in EquipZones)
            {
                var cell = new VisualElement { name = "Zone_" + zone.key };
                cell.AddToClassList("char-equip-zone");
                if (zone.gameplaySlot.HasValue)
                    cell.AddToClassList("char-equip-zone--bindable");
                PlaceByIni(cell, zone.left, zone.top, zone.width, zone.height);
                _paperdoll.Add(cell);
            }
            body.Add(_paperdoll);

            // Header / identity stats.
            AddStatLabel(body, "Name",      44, 68, 155, 14, "char-stat-value-green");
            AddStatLabel(body, "Title",     44, 83, 155, 14, "char-stat-value-gold");
            AddStatLabel(body, "Level",     54, 113, 60, 14, "char-stat-value-green");
            AddStatLabel(body, "WorldRank", 150, 113, 60, 14, "char-stat-value-green");
            AddStatLabel(body, "Prestige",  150, 99, 60, 14, "char-stat-value-green");
            AddStatLabel(body, "Luck",      54, 99, 60, 14, "char-stat-value-green");
            AddStatLabel(body, "PKValue",   368, 92, 46, 14, "char-stat-value-green");

            // Vitals.
            AddStatLabel(body, "Life",    128, 140, 81, 14, "char-stat-value-green");
            AddStatLabel(body, "Mana",    128, 155, 81, 14, "char-stat-value-green");
            AddStatLabel(body, "Stamina", 128, 170, 81, 14, "char-stat-value-green");
            AddStatLabel(body, "Status",  128, 185, 81, 14, "char-stat-value-green");

            // EXP.
            AddStatLabel(body, "Exp", 43, 209, 156, 14, "char-stat-value-gold");

            // Potential + spend buttons.
            AddStatLabel(body, "Strength",  43, 224, 45, 14, "char-stat-value-gold");
            AddStatLabel(body, "Dexterity", 139, 224, 45, 14, "char-stat-value-gold");
            AddStatLabel(body, "Vitality",  43, 239, 45, 14, "char-stat-value-gold");
            AddStatLabel(body, "Energy",    139, 239, 45, 14, "char-stat-value-gold");
            AddAddPointButton(body, "AddStrength",  89, 224, PcPotentialKind.Strength);
            AddAddPointButton(body, "AddDexterity", 185, 224, PcPotentialKind.Dexterity);
            AddAddPointButton(body, "AddVitality",  89, 239, PcPotentialKind.Vitality);
            AddAddPointButton(body, "AddEnergy",    185, 239, PcPotentialKind.InnerEnergy);

            // Damage + combat summary.
            AddStatLabel(body, "LeftDamage",  67, 254, 132, 14, "char-stat-value-gold");
            AddStatLabel(body, "RightDamage", 67, 269, 132, 12, "char-stat-value-gold");
            AddStatLabel(body, "Attack",      43, 287, 56, 14, "char-stat-value-gold");
            AddStatLabel(body, "Defense",     43, 302, 56, 14, "char-stat-value-gold");
            AddStatLabel(body, "MoveSpeed",   43, 317, 56, 14, "char-stat-value-gold");
            AddStatLabel(body, "AttackSpeed", 43, 332, 56, 14, "char-stat-value-gold");
            AddStatLabel(body, "RemainPoint", 43, 347, 56, 14, "char-stat-value-green");
            AddStatLabel(body, "ResistPhy",      143, 287, 56, 14, "char-stat-value-gold");
            AddStatLabel(body, "ResistCold",     143, 302, 56, 14, "char-stat-value-gold");
            AddStatLabel(body, "ResistLighting", 143, 317, 56, 14, "char-stat-value-gold");
            AddStatLabel(body, "ResistFire",     143, 332, 56, 14, "char-stat-value-gold");
            AddStatLabel(body, "ResistPoison",   143, 347, 56, 14, "char-stat-value-gold");

            // Trang-bị backend-missing buttons (disabled).
            var lockBtn = MakeActionButton("BtnLock",   "Khóa",  220, 345, 64, 20, "char-equip-btn");
            var bindBtn = MakeActionButton("BtnBind",   "Đính",  288, 345, 64, 20, "char-equip-btn");
            var unbindBtn = MakeActionButton("BtnUnBind","Tháo",  356, 345, 64, 20, "char-equip-btn");
            lockBtn.SetEnabled(false);
            bindBtn.SetEnabled(false);
            unbindBtn.SetEnabled(false);
            body.Add(lockBtn);
            body.Add(bindBtn);
            body.Add(unbindBtn);

            // Footer: Item + Close. Captions/borders are baked into the PC sprite;
            // these transparent buttons only provide hit targets. Item opens inventory;
            // Close fires the popup shell close (wired by PopupWindow).
            var itemBtn = new Button { name = "Item", text = string.Empty };
            itemBtn.AddToClassList("char-footer-btn");
            UseReadableFont(itemBtn);
            PlaceByIni(itemBtn, 7, 369, 207, 29);
            itemBtn.clicked += () => _state.OpenInventory?.Invoke();
            body.Add(itemBtn);

            var closeBtn = new Button { name = "Close", text = string.Empty };
            closeBtn.AddToClassList("char-footer-btn");
            UseReadableFont(closeBtn);
            PlaceByIni(closeBtn, 214, 369, 207, 29);
            body.Add(closeBtn);
        }

        public void OnShow()
        {
            RefreshPanelBackground();
            RefreshStats();
            RefreshEquipment();
        }

        public void OnClose()
        {
            _statLabels.Clear();
            _addButtons.Clear();
            _panel = null;
            _paperdoll = null;
        }

        /// <summary>Footer Close button — PopupManager subscribes to fire Close.</summary>
        public Button GetCloseButton(VisualElement body) => body?.Q<Button>("Close");

        // ---------- refresh ----------

        private void RefreshPanelBackground()
        {
            if (_panel == null) return;
            bool isFemale = _state.IsFemaleProvider?.Invoke() ?? false;
            var path = isFemale ? PanelFemalePath : PanelMalePath;
            var tex = LoadTexture(path);
            if (tex != null)
                _panel.style.backgroundImage = new StyleBackground(tex);
            _panel.EnableInClassList("char-panel--female", isFemale);
        }

        private void RefreshStats()
        {
            var s = _state.ReadStats();
            SetText("Name",      string.IsNullOrEmpty(s.nameVi) ? "Vô Danh" : s.nameVi);
            SetText("Title",     s.titleVi);
            SetText("Level",     FormatInt(s.level));
            SetText("WorldRank", FormatInt(s.worldRank));
            SetText("Prestige",  FormatInt(s.prestige));
            SetText("Luck",      FormatInt(s.luck));
            SetText("PKValue",   FormatInt(0));
            SetText("Life",      FormatInt(s.currentLife));
            SetText("Mana",      FormatInt(s.currentMana));
            SetText("Stamina",   FormatInt(s.currentStamina));
            SetText("Status",    "—");
            SetText("Exp",       FormatRange(s.currentExp, s.maxExp));
            SetText("Strength",  FormatInt(s.strength));
            SetText("Dexterity", FormatInt(s.dexterity));
            SetText("Vitality",  FormatInt(s.vitality));
            SetText("Energy",    FormatInt(s.innerEnergy));
            SetText("LeftDamage",  string.IsNullOrEmpty(s.leftDamage) ? "0/0" : s.leftDamage);
            SetText("RightDamage", string.IsNullOrEmpty(s.rightDamage) ? "0/0" : s.rightDamage);
            SetText("Attack",      FormatInt(s.attack));
            SetText("Defense",     FormatInt(s.defense));
            SetText("MoveSpeed",   FormatInt(s.moveSpeed));
            SetText("AttackSpeed", FormatInt(s.attackSpeed));
            SetText("RemainPoint", FormatInt(s.remainPoint));
            SetText("ResistPhy",      FormatInt(s.resistPhy));
            SetText("ResistCold",     FormatInt(s.resistCold));
            SetText("ResistLighting", FormatInt(s.resistLightning));
            SetText("ResistFire",     FormatInt(s.resistFire));
            SetText("ResistPoison",   FormatInt(s.resistPoison));

            // +/- enabled iff a point remains (disabled frame state when zero).
            bool canAdd = s.remainPoint > 0 && _state.DistributePotential != null;
            foreach (var pair in _addButtons)
            {
                pair.Value.SetEnabled(canAdd);
                pair.Value.EnableInClassList("char-add-point--disabled", !canAdd);
            }
        }

        private void RefreshEquipment()
        {
            if (_paperdoll == null) return;
            var equipped = _state.EquipmentStateProvider?.Invoke();
            foreach (var zone in EquipZones)
            {
                var cell = _paperdoll.Q("Zone_" + zone.key);
                if (cell == null) continue;
                bool bound = zone.gameplaySlot.HasValue
                    && equipped != null
                    && equipped.TryGetValue(zone.gameplaySlot.Value, out bool isEquipped)
                    && isEquipped;
                cell.EnableInClassList("char-equip-zone--equipped", bound);
                cell.EnableInClassList("char-equip-zone--empty",
                    zone.gameplaySlot.HasValue && !bound);
            }
        }

        // ---------- helpers ----------

        private void AddStatLabel(VisualElement parent, string key, int x, int y, int w, int h, string ussClass)
        {
            var lbl = new Label { name = "Stat_" + key, text = "—" };
            lbl.AddToClassList(ussClass);
            UseReadableFont(lbl);
            PlaceByIni(lbl, x, y, w, h);
            parent.Add(lbl);
            _statLabels[key] = lbl;
        }

        private void AddAddPointButton(VisualElement parent, string name, int left, int top, PcPotentialKind kind)
        {
            var btn = new VisualElement { name = name };
            btn.AddToClassList("char-add-point");
            btn.pickingMode = PickingMode.Position;
            PlaceByIni(btn, left, top, 14, 14);
            // Default to the "up" frame; disabled state swaps to a dimmed USS tint.
            var tex = LoadTexture(ArtRoot + "/btn_addpoint_up.png");
            if (tex != null)
                btn.style.backgroundImage = new StyleBackground(tex);
            PcPotentialKind captured = kind;
            btn.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (_state.DistributePotential == null) return;
                if (_state.DistributePotential(captured))
                {
                    RefreshStats();
                    SubsystemLog.Info("Popup.CharacterInfo",
                        string.Format(CultureInfo.InvariantCulture, "Distributed 1 point to {0}", captured));
                }
                evt.StopPropagation();
            });
            parent.Add(btn);
            _addButtons[kind] = btn;
        }

        private Button MakeActionButton(string name, string textVi, int x, int y, int w, int h, string ussClass)
        {
            var btn = new Button { name = name, text = textVi };
            btn.AddToClassList(ussClass);
            UseReadableFont(btn);
            PlaceByIni(btn, x, y, w, h);
            return btn;
        }

        private static void PlaceByIni(VisualElement el, int x, int y, int w, int h)
        {
            el.style.position = Position.Absolute;
            el.style.left = x;
            el.style.top = y;
            el.style.width = w;
            el.style.height = h;
        }

        private void SetText(string key, string value)
        {
            if (_statLabels.TryGetValue(key, out var lbl))
                lbl.text = value;
        }

        private static string FormatInt(int v) => v.ToString(CultureInfo.InvariantCulture);
        private static string FormatRange(long cur, long max)
            => string.Format(CultureInfo.InvariantCulture, "{0}/{1}", cur, max);

        private static void UseReadableFont(TextElement element)
        {
            if (element == null) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                element.style.unityFont = font;
        }

        private static Texture2D LoadTexture(string assetsRelativePngPath)
        {
            // Editor-only sync load; null-safe at runtime (returns null → USS tint shows).
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(assetsRelativePngPath);
#else
            return null;
#endif
        }

        /// <summary>
        /// Internal test/runtime seam: spend one point on the given potential via the
        /// state callback and re-render. Mirrors SkillContent.TryUpgrade.
        /// </summary>
        internal bool TryDistribute(PcPotentialKind kind)
        {
            if (_state.DistributePotential == null) return false;
            if (_state.DistributePotential(kind))
            {
                RefreshStats();
                return true;
            }
            return false;
        }

        // ---------- data tables (PC INI 2711122c) ----------

        /// <summary>
        /// 12 trang-bị hit-zones with their PC INI coordinates and gameplay slot
        /// binding. Zones without a gameplay slot are framework (no EquipSlot enum
        /// yet — Bangle is the wrist slot the sandbox has not modelled).
        /// </summary>
        public static readonly IReadOnlyList<PcEquipZone> EquipZones = new[]
        {
            new PcEquipZone("Cap",     EquipSlot.Helmet,   280, 76,  50, 50),
            new PcEquipZone("Weapon",  EquipSlot.Weapon,   362, 160, 50, 102),
            new PcEquipZone("Necklace",EquipSlot.Necklace, 362, 120, 50, 24),
            new PcEquipZone("Mask",    EquipSlot.Mask,     225, 68,  26, 47),
            new PcEquipZone("Bangle",  null,               225, 120, 24, 50),
            new PcEquipZone("Cloth",   EquipSlot.Armor,    280, 137, 50, 76),
            new PcEquipZone("Sash",    EquipSlot.Belt,     280, 224, 50, 24),
            new PcEquipZone("Ring1",   EquipSlot.Ring,     225, 178, 24, 24),
            new PcEquipZone("Ring2",   EquipSlot.Ring2,    225, 208, 24, 24),
            new PcEquipZone("Pendant", EquipSlot.Pendant,  225, 242, 24, 50),
            new PcEquipZone("Shoes",   EquipSlot.Boots,    362, 291, 50, 50),
            new PcEquipZone("Horse",   EquipSlot.Mount,    280, 265, 50, 76),
        };
    }
}
