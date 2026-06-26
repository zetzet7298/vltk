using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
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
            Assert.IsNull(PopupManager.Instance);
            Assert.DoesNotThrow(() => InvokePrivateMethod("OnTeamClick"));
        }

        [Test]
        public void OnFactionClick_WithoutPopupManager_DoesNotThrow()
        {
            // BtnFaction now opens the Faction popup via PopupManager (see FactionContentTests).
            // It must no longer toggle the StallCurrencySelector, and must degrade gracefully
            // when PopupManager is not initialised.
            Assert.IsNull(PopupManager.Instance);
            Assert.IsTrue(_stallCurrencySelector.ClassListContains("hidden"));
            Assert.DoesNotThrow(() => InvokePrivateMethod("OnFactionClick"));
            // Stall selector must remain untouched by the faction handler now.
            Assert.IsTrue(_stallCurrencySelector.ClassListContains("hidden"));
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

    }
}
