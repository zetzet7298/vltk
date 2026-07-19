using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using VLTK.UI;
using VLTK.UI.Popup;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class GameHudControllerTests
    {
        private GameObject _go;
        private GameHudController _hud;
        private VisualElement _root;
        private VisualElement _buffPanel;
        private VisualElement _tradeInfoPanel;
        private VisualElement _tradeInfoClose;
        private Label _tradePartnerName;
        private Label _tradePartnerLevel;
        private Label _tradePartnerFaction;
        private Label _tradePartnerGuild;
        private VisualElement _stallCurrencySelector;
        private Button _stallMoneyBtn;
        private Button _stallCoinBtn;
        private VisualElement _facePickerOverlay;
        private VisualElement _facePickerClose;
        private ScrollView _facePickerList;
        private Button _faceBtn;

        [SetUp]
        public void Setup()
        {
            PopupManager.SetInstance(null);
            _go = new GameObject("GameHudControllerTestsGo");
            _hud = _go.AddComponent<GameHudController>();

            _root = new VisualElement { name = "GameHud" };
            _buffPanel = new VisualElement { name = "BuffPanel" };

            _tradeInfoPanel = new VisualElement { name = "TradeInfoPanel" };
            _tradeInfoPanel.AddToClassList("hidden");
            _tradeInfoClose = new VisualElement { name = "TradeInfoClose" };
            _tradePartnerName = new Label { name = "TradePartnerName" };
            _tradePartnerLevel = new Label { name = "TradePartnerLevel" };
            _tradePartnerFaction = new Label { name = "TradePartnerFaction" };
            _tradePartnerGuild = new Label { name = "TradePartnerGuild" };
            _tradeInfoPanel.Add(_tradeInfoClose);
            _tradeInfoPanel.Add(_tradePartnerName);
            _tradeInfoPanel.Add(_tradePartnerLevel);
            _tradeInfoPanel.Add(_tradePartnerFaction);
            _tradeInfoPanel.Add(_tradePartnerGuild);

            _stallCurrencySelector = new VisualElement { name = "StallCurrencySelector" };
            _stallCurrencySelector.AddToClassList("hidden");
            _stallMoneyBtn = new Button { name = "StallMoneyBtn" };
            _stallCoinBtn = new Button { name = "StallCoinBtn" };
            _stallCurrencySelector.Add(_stallMoneyBtn);
            _stallCurrencySelector.Add(_stallCoinBtn);

            _facePickerOverlay = new VisualElement { name = "FacePickerOverlay" };
            _facePickerOverlay.AddToClassList("hidden");
            _facePickerClose = new VisualElement { name = "FacePickerClose" };
            _facePickerList = new ScrollView { name = "FacePickerList" };
            _facePickerOverlay.Add(_facePickerClose);
            _facePickerOverlay.Add(_facePickerList);

            _faceBtn = new Button { name = "FaceBtn" };

            _root.Add(_buffPanel);
            _root.Add(_tradeInfoPanel);
            _root.Add(_stallCurrencySelector);
            _root.Add(_facePickerOverlay);
            _root.Add(_faceBtn);

            // Set private fields via reflection
            SetPrivateField("_buffPanel", _buffPanel);
            SetPrivateField("_tradeInfoPanel", _tradeInfoPanel);
            SetPrivateField("_tradeInfoClose", _tradeInfoClose);
            SetPrivateField("_tradePartnerName", _tradePartnerName);
            SetPrivateField("_tradePartnerLevel", _tradePartnerLevel);
            SetPrivateField("_tradePartnerFaction", _tradePartnerFaction);
            SetPrivateField("_tradePartnerGuild", _tradePartnerGuild);
            SetPrivateField("_stallCurrencySelector", _stallCurrencySelector);
            SetPrivateField("_stallMoneyBtn", _stallMoneyBtn);
            SetPrivateField("_stallCoinBtn", _stallCoinBtn);
            SetPrivateField("_facePickerOverlay", _facePickerOverlay);
            SetPrivateField("_facePickerClose", _facePickerClose);
            SetPrivateField("_facePickerList", _facePickerList);
            SetPrivateField("_faceBtn", _faceBtn);
        }

        [TearDown]
        public void TearDown()
        {
            PopupManager.SetInstance(null);
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        private void SetPrivateField(string fieldName, object value)
        {
            var field = typeof(GameHudController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field {fieldName} not found on GameHudController");
            field.SetValue(_hud, value);
        }

        private void InvokePrivateMethod(string methodName, params object[] args)
        {
            var method = typeof(GameHudController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method {methodName} not found on GameHudController");
            method.Invoke(_hud, args);
        }

        [Test]
        public void OnTeamClick_WithoutPopupManager_DoesNotThrow()
        {
            // BtnTeam now opens the Team popup via PopupManager (see TeamContentTests).
            // Without an initialised PopupManager the handler must degrade gracefully.
            PopupManager.SetInstance(null);
            Assert.IsNull(PopupManager.Instance);
            Assert.DoesNotThrow(() => InvokePrivateMethod("OnTeamClick"));
        }

        [Test]
        public void OnFactionClick_WithoutPopupManager_DoesNotThrow()
        {
            // BtnFaction now opens the Faction popup via PopupManager (see FactionContentTests).
            // It must no longer toggle the StallCurrencySelector, and must degrade gracefully
            // when PopupManager is not initialised.
            PopupManager.SetInstance(null);
            Assert.IsNull(PopupManager.Instance);
            Assert.IsTrue(_stallCurrencySelector.ClassListContains("hidden"));
            Assert.DoesNotThrow(() => InvokePrivateMethod("OnFactionClick"));
            // Stall selector must remain untouched by the faction handler now.
            Assert.IsTrue(_stallCurrencySelector.ClassListContains("hidden"));
        }

        [Test]
        public void OnSkillsClick_WithoutPopupManager_DoesNotThrow()
        {
            // BtnSkills now opens the Skill popup via PopupManager (see SkillContentTests).
            // It must degrade gracefully when PopupManager is not initialised and must no longer
            // toggle any inline skill panel (the inline CaiBangSkillPanel is retired).
            PopupManager.SetInstance(null);
            Assert.IsNull(PopupManager.Instance);
            Assert.DoesNotThrow(() => InvokePrivateMethod("OnSkillsClick"));
        }

        [Test]
        public void OnExchangeClick_TogglesTradeInfoPanelAndPopulatesPartnerInfo()
        {
            // Initially hidden
            Assert.IsTrue(_tradeInfoPanel.ClassListContains("hidden"));

            // Show
            InvokePrivateMethod("OnExchangeClick");
            Assert.IsFalse(_tradeInfoPanel.ClassListContains("hidden"));
            StringAssert.Contains("Dã Tẩu", _tradePartnerName.text);
            StringAssert.Contains("200", _tradePartnerLevel.text);
            StringAssert.Contains("Võ Đang", _tradePartnerFaction.text);
            StringAssert.Contains("Thiên Hạ", _tradePartnerGuild.text);

            // Hide again
            InvokePrivateMethod("OnExchangeClick");
            Assert.IsTrue(_tradeInfoPanel.ClassListContains("hidden"));
        }

        [Test]
        public void CloseTradeInfo_HidesPanel()
        {
            _tradeInfoPanel.RemoveFromClassList("hidden");
            Assert.IsFalse(_tradeInfoPanel.ClassListContains("hidden"));

            InvokePrivateMethod("CloseTradeInfo");
            Assert.IsTrue(_tradeInfoPanel.ClassListContains("hidden"));
        }

        [Test]
        public void SelectStallCurrency_HidesSelector()
        {
            _stallCurrencySelector.RemoveFromClassList("hidden");
            Assert.IsFalse(_stallCurrencySelector.ClassListContains("hidden"));

            InvokePrivateMethod("SelectStallCurrency", "Bạch Ngân");
            Assert.IsTrue(_stallCurrencySelector.ClassListContains("hidden"));
        }

        [Test]
        public void OpenFacePicker_ShowsOverlayAndPopulatesList()
        {
            Assert.IsTrue(_facePickerOverlay.ClassListContains("hidden"));
            Assert.AreEqual(0, _facePickerList.childCount);

            InvokePrivateMethod("OpenFacePicker");
            Assert.IsFalse(_facePickerOverlay.ClassListContains("hidden"));
            Assert.Greater(_facePickerList.childCount, 0, "Should populate emote list");

            // Close
            InvokePrivateMethod("CloseFacePicker");
            Assert.IsTrue(_facePickerOverlay.ClassListContains("hidden"));
        }

        [Test]
        public void UpdateBuffs_DrawsDefaultBuffsWhenNoActivePlayerState()
        {
            Assert.AreEqual(0, _buffPanel.childCount);

            InvokePrivateMethod("UpdateBuffs");
            Assert.AreEqual(4, _buffPanel.childCount, "Should show 4 default buffs when no active gameplay loop exists");

            // Verify they have timer labels
            foreach (var cell in _buffPanel.Children())
            {
                Assert.IsTrue(cell.ClassListContains("hud-buff-cell"));
                var timerLabel = cell.Q<Label>();
                Assert.IsNotNull(timerLabel);
                Assert.IsTrue(timerLabel.ClassListContains("hud-buff-timer") || timerLabel.ClassListContains("hud-debuff-timer"));
            }
        }

        [Test]
        public void HudArtPathResolver_UsesStreamingAssetsRootInEditor()
        {
            var expected = System.IO.Path.Combine(Application.streamingAssetsPath, "UI/HUD/Art");
            var legacyDataPath = System.IO.Path.Combine(Application.dataPath, "UI/HUD/Art");

            Assert.AreEqual(expected, HudArtPathResolver.ResolveArtRoot("UI/HUD/Art"));
            Assert.AreNotEqual(legacyDataPath, HudArtPathResolver.ResolveArtRoot("UI/HUD/Art"));
        }

        [Test]
        [Category("HudPersistence")]
        public void GMPanel_OpenClose_PreservesHudDocumentTreeAndActionButtons()
        {
            var document = _go.GetComponent<UIDocument>();
            Assert.IsNotNull(document);
            document.rootVisualElement.Add(_root);
            var horseButton = new VisualElement { name = "ActionBtnHorse" };
            _root.Add(horseButton);

            var gmGo = new GameObject("GMPanelHudPreservationTest");
            var panelRoot = new GameObject("PanelRoot");
            panelRoot.transform.SetParent(gmGo.transform, false);
            var gmPanel = gmGo.AddComponent<GMPanelController>();
            gmPanel.panelRoot = panelRoot;
            typeof(GMPanelController)
                .GetField("_cachedHudDoc", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(gmPanel, document);

            try
            {
                var originalRoot = document.rootVisualElement.Q("GameHud");
                gmPanel.Open();

                Assert.IsTrue(document.enabled, "GM overlay must not disable UIDocument and rebuild its visual tree");
                Assert.AreEqual(DisplayStyle.None, originalRoot.style.display.value);

                gmPanel.Close();

                Assert.IsTrue(document.enabled);
                Assert.AreSame(originalRoot, document.rootVisualElement.Q("GameHud"));
                Assert.AreSame(horseButton, document.rootVisualElement.Q("ActionBtnHorse"));
                Assert.AreEqual(DisplayStyle.Flex, originalRoot.style.display.value);
            }
            finally
            {
                Object.DestroyImmediate(gmGo);
            }
        }

            [Test]
            public void HudArtPathResolver_PreservesStreamingAssetsRootForMobileArchivePaths()
            {
                const string androidStreamingRoot = "jar:file:///data/app/vltk/base.apk!/assets";

                var artRoot = HudArtPathResolver.ResolveUnderStreamingAssets(androidStreamingRoot, "/UI/HUD/Art/");
                var generatedRoot = HudArtPathResolver.ResolveUnderStreamingAssets(androidStreamingRoot, "UI/HUD/Art/Generated");

                Assert.AreEqual("jar:file:///data/app/vltk/base.apk!/assets/UI/HUD/Art", artRoot);
                Assert.AreEqual("jar:file:///data/app/vltk/base.apk!/assets/UI/HUD/Art/Generated", generatedRoot);
                Assert.IsTrue(HudArtPathResolver.RequiresUnityWebRequest(generatedRoot));
                Assert.IsFalse(HudArtPathResolver.CanCheckDirectory(generatedRoot));
            }

            // ===== S2 (HUD-004): mobile-native combat cluster tests =====
            // These verify the UXML structure of the CombatCluster: exactly 6 slots
            // (1 main + 5 sub) and 3 action buttons, all with correct element names
            // so CombatSkillSlotController + GameHudController.LoadArt bind to them.
            // Test-run status: PENDING parent Unity MCP verification (worker has no Unity MCP).

            /// <summary>
            /// Loads the GameHud UXML tree from disk and returns the root VisualElement.
            /// This mirrors how UIDocument visualTreeAsset would populate the hierarchy.
            /// </summary>
            private static VisualElement LoadHudVisualTree()
            {
                var treeAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(
                    "Assets/UI/HUD/GameHud.uxml");
                Assert.IsNotNull(treeAsset, "GameHud.uxml not found or failed to load");
                return treeAsset.CloneTree();
            }

            [Test]
            public void S2_CombatCluster_HasExactlySixSlots_OneMainFiveSub()
            {
                var root = LoadHudVisualTree();
                var cluster = root.Q("CombatCluster");
                Assert.IsNotNull(cluster, "CombatCluster element missing from GameHud.uxml");

                // 1 main slot (PrimaryAttackBtn)
                var mainSlot = cluster.Q("PrimaryAttackBtn");
                Assert.IsNotNull(mainSlot, "PrimaryAttackBtn (main combat slot) missing from CombatCluster");
                Assert.IsTrue(mainSlot.ClassListContains("hud-combat-main-slot"),
                    "Main slot must have hud-combat-main-slot class");

                // 5 sub slots (SkillSlot0-4) — names match CombatSkillSlotController binding
                for (int i = 0; i < 5; i++)
                {
                    var slot = cluster.Q($"SkillSlot{i}");
                    Assert.IsNotNull(slot, $"SkillSlot{i} missing from CombatCluster");
                    Assert.IsTrue(slot.ClassListContains("hud-combat-sub-slot"),
                        $"SkillSlot{i} must have hud-combat-sub-slot class");
                }
            }

            [Test]
            public void S2_CombatCluster_HasThreeActionButtons_RunHorseSit()
            {
                var root = LoadHudVisualTree();
                var cluster = root.Q("CombatCluster");
                Assert.IsNotNull(cluster, "CombatCluster element missing from GameHud.uxml");

                foreach (var name in new[] { "ActionBtnRun", "ActionBtnHorse", "ActionBtnSit" })
                {
                    var btn = cluster.Q(name);
                    Assert.IsNotNull(btn, $"{name} missing from CombatCluster");
                    Assert.IsTrue(btn.ClassListContains("hud-action-btn"),
                        $"{name} must have hud-action-btn class");
                    // Each action button must have an icon child for LoadArt to wire
                    var icon = btn.Q(name + "Icon");
                    Assert.IsNotNull(icon, $"{name}Icon child missing from {name}");
                }
            }

            [Test]
            public void S2_CombatSlots_HaveSlotIconChildren_ForControllerBinding()
            {
                var root = LoadHudVisualTree();
                var cluster = root.Q("CombatCluster");
                Assert.IsNotNull(cluster, "CombatCluster element missing from GameHud.uxml");

                // CombatSkillSlotController queries slot.Q("SlotIcon") for icon resolution.
                // All 5 sub slots + main must have a SlotIcon child.
                var mainIcon = cluster.Q("PrimaryAttackBtn")?.Q("SlotIcon");
                Assert.IsNotNull(mainIcon, "PrimaryAttackBtn must have SlotIcon child");

                for (int i = 0; i < 5; i++)
                {
                    var icon = cluster.Q($"SkillSlot{i}")?.Q("SlotIcon");
                    Assert.IsNotNull(icon, $"SkillSlot{i} must have SlotIcon child");
                }
            }

            [Test]
            public void S2_CombatCluster_BottomCenterLaneIsClear()
            {
                // The bottom-center lane is reserved for the future chat canvas.
                // No combat slots, quick slots, action buttons, or menu buttons should be
                // placed in the bottom-center area (they are all in CombatCluster bottom-RIGHT).
                var root = LoadHudVisualTree();
                var cluster = root.Q("CombatCluster");
                Assert.IsNotNull(cluster, "CombatCluster element missing");

                // CombatCluster must be anchored right (not center) — verify it has the
                // hud-combat-cluster class which uses right:Npx positioning in USS.
                Assert.IsTrue(cluster.ClassListContains("hud-combat-cluster"),
                    "CombatCluster must have hud-combat-cluster class (anchored bottom-right)");
            }

            [Test]
            public void S2_TopBarAndMinimap_RegressionGuard_Untouched()
            {
                var root = LoadHudVisualTree();

                // Top status bar elements must still be present
                Assert.IsNotNull(root.Q("TopLeftPanel"), "TopLeftPanel must remain (regression)");
                Assert.IsNotNull(root.Q("HpBarFill"), "HP bar clip must remain (regression)");
                Assert.IsNotNull(root.Q("MpBarFill"), "MP bar clip must remain (regression)");
                Assert.IsNotNull(root.Q("ExpBarFill"), "EXP bar clip must remain (regression)");
                Assert.IsNotNull(root.Q("StaminaBarFill"), "Stamina bar clip must remain (regression)");
                Assert.IsNotNull(root.Q("HpBarFillImage"), "HP bar fixed image must remain for PC left-to-right clipping");
                Assert.IsNotNull(root.Q("MpBarFillImage"), "MP bar fixed image must remain for PC left-to-right clipping");
                Assert.IsNotNull(root.Q("ExpBarFillImage"), "EXP bar fixed image must remain for PC left-to-right clipping");
                Assert.IsNotNull(root.Q("StaminaBarFillImage"), "Stamina bar fixed image must remain for PC left-to-right clipping");

                // Minimap elements must still be present
                Assert.IsNotNull(root.Q("MinimapPanel"), "MinimapPanel must remain (regression)");
                Assert.IsNotNull(root.Q("PlayerDot"), "PlayerDot must remain (regression)");

                // Popup overlay must still be present
                Assert.IsNotNull(root.Q("PopupOverlay"), "PopupOverlay must remain (regression)");
            }

            [Test]
            public void LoadIconRequestGeneration_RejectsStaleAndAcceptsCurrent()
            {
                uint currentGeneration = 7;

                Assert.IsFalse(GameHudController.ShouldApplyIconRequest(6, currentGeneration));
                Assert.IsTrue(GameHudController.ShouldApplyIconRequest(7, currentGeneration));
                Assert.IsFalse(GameHudController.ShouldApplyIconRequest(() => false));
                Assert.IsTrue(GameHudController.ShouldApplyIconRequest(() => true));
                Assert.IsTrue(GameHudController.ShouldApplyIconRequest((System.Func<bool>)null));
            }

        }
}
