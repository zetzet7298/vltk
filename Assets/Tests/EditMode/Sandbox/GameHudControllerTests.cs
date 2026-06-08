using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.UI;
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
        private VisualElement _teamPreview;
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
        private VisualElement _utilityDock;
        private VisualElement _utilityActionRow;
        private VisualElement _utilityMenuRowA;
        private VisualElement _utilityMenuRowB;
        private VisualElement _utilityToggleBtn;
        private Label _utilityToggleLabel;
        private VisualElement _pcToolPanel;
        private VisualElement _pcToolClose;
        private ScrollView _pcToolList;
        private Label _pcToolTitle;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject("GameHudControllerTestsGo");
            _hud = _go.AddComponent<GameHudController>();

            _root = new VisualElement { name = "GameHud" };
            _buffPanel = new VisualElement { name = "BuffPanel" };
            _teamPreview = new VisualElement { name = "TeamPreview" };
            _teamPreview.AddToClassList("hidden");

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

            _utilityToggleBtn = new VisualElement { name = "UtilityToggleBtn" };
            _utilityToggleLabel = new Label { name = "UtilityToggleLabel" };
            _utilityToggleBtn.Add(_utilityToggleLabel);
            _utilityDock = new VisualElement { name = "MobileUtilityDock" };
            _utilityDock.AddToClassList("hidden");
            _utilityActionRow = new VisualElement { name = "MobileUtilityActionRow" };
            _utilityMenuRowA = new VisualElement { name = "MobileUtilityMenuRowA" };
            _utilityMenuRowB = new VisualElement { name = "MobileUtilityMenuRowB" };
            _utilityDock.Add(_utilityActionRow);
            _utilityDock.Add(_utilityMenuRowA);
            _utilityDock.Add(_utilityMenuRowB);

            _pcToolPanel = new VisualElement { name = "PcToolPanel" };
            _pcToolPanel.AddToClassList("hidden");
            _pcToolClose = new VisualElement { name = "PcToolClose" };
            _pcToolTitle = new Label { name = "PcToolTitle" };
            _pcToolList = new ScrollView { name = "PcToolList" };
            _pcToolPanel.Add(_pcToolClose);
            _pcToolPanel.Add(_pcToolTitle);
            _pcToolPanel.Add(_pcToolList);

            _root.Add(_buffPanel);
            _root.Add(_teamPreview);
            _root.Add(_tradeInfoPanel);
            _root.Add(_stallCurrencySelector);
            _root.Add(_facePickerOverlay);
            _root.Add(_faceBtn);
            _root.Add(_utilityToggleBtn);
            _root.Add(_utilityDock);
            _root.Add(_pcToolPanel);

            // Set private fields via reflection
            SetPrivateField("_buffPanel", _buffPanel);
            SetPrivateField("_teamPreview", _teamPreview);
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
            SetPrivateField("_utilityDock", _utilityDock);
            SetPrivateField("_utilityActionRow", _utilityActionRow);
            SetPrivateField("_utilityMenuRowA", _utilityMenuRowA);
            SetPrivateField("_utilityMenuRowB", _utilityMenuRowB);
            SetPrivateField("_utilityToggleLabel", _utilityToggleLabel);
            SetPrivateField("_pcToolPanel", _pcToolPanel);
            SetPrivateField("_pcToolClose", _pcToolClose);
            SetPrivateField("_pcToolList", _pcToolList);
            SetPrivateField("_pcToolTitle", _pcToolTitle);
            SetPrivateField("_boundRoot", _root);
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

        private T GetPrivateField<T>(string fieldName)
        {
            var field = typeof(GameHudController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field {fieldName} not found on GameHudController");
            return (T)field.GetValue(_hud);
        }

        private void InvokePrivateMethod(string methodName, params object[] args)
        {
            var method = typeof(GameHudController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method {methodName} not found on GameHudController");
            method.Invoke(_hud, args);
        }

        [Test]
        public void OnTeamClick_TogglesTeamPreviewWithoutFakeMembers()
        {
            // Initially hidden
            Assert.IsTrue(_teamPreview.ClassListContains("hidden"));
            Assert.AreEqual(0, _teamPreview.childCount);

            // Show
            InvokePrivateMethod("OnTeamClick");
            Assert.IsFalse(_teamPreview.ClassListContains("hidden"));
            Assert.AreEqual(1, _teamPreview.childCount, "No Sandbox PartyService should show an empty party row, not fake members");
            var emptyLabels = _teamPreview.Query<Label>().ToList();
            Assert.AreEqual(1, emptyLabels.Count);
            StringAssert.Contains("Chưa tham gia đội", emptyLabels[0].text);

            // Hide again
            InvokePrivateMethod("OnTeamClick");
            Assert.IsTrue(_teamPreview.ClassListContains("hidden"));
        }

        [Test]
        public void PopulateTeamPreviewFromMembers_UsesRuntimePartyMembers()
        {
            var members = new List<PartyMember>
            {
                new PartyMember { memberId = 1, nameVi = "Thiếu Hiệp", level = 45, factionId = 7, hpCurrent = 80, hpMax = 100, mpCurrent = 30, mpMax = 60, isLeader = true, isOnline = true },
                new PartyMember { memberId = 2, nameVi = "Đồng Đội", level = 42, factionId = 3, hpCurrent = 60, hpMax = 90, mpCurrent = 90, mpMax = 100, isLeader = false, isOnline = true },
            };

            InvokePrivateMethod("PopulateTeamPreviewFromMembers", members);

            Assert.AreEqual(2, _teamPreview.childCount);
            var labels = _teamPreview.Query<Label>().ToList();
            Assert.AreEqual(2, labels.Count);
            StringAssert.Contains("Thiếu Hiệp", labels[0].text);
            StringAssert.Contains("Cái Bang", labels[0].text);
            StringAssert.Contains("Đồng Đội", labels[1].text);
            StringAssert.Contains("Nga My", labels[1].text);
        }

        [Test]
        public void UtilityToggle_CyclesHiddenActionMenuHidden()
        {
            Assert.AreEqual(0, _hud.CurrentUtilityBarMode);
            Assert.IsTrue(_utilityDock.ClassListContains("hidden"));

            InvokePrivateMethod("OnUtilityToggleClick");
            Assert.AreEqual(1, _hud.CurrentUtilityBarMode);
            Assert.IsFalse(_utilityDock.ClassListContains("hidden"));
            Assert.IsTrue(_utilityDock.ClassListContains("action-mode"));
            Assert.IsFalse(_utilityDock.ClassListContains("menu-mode"));
            Assert.IsFalse(_utilityActionRow.ClassListContains("hidden"));
            Assert.IsTrue(_utilityMenuRowA.ClassListContains("hidden"));
            Assert.IsTrue(_utilityMenuRowB.ClassListContains("hidden"));
            Assert.IsTrue(_utilityToggleBtn.ClassListContains("active"));
            Assert.AreEqual("Tác", _utilityToggleLabel.text);

            InvokePrivateMethod("OnUtilityToggleClick");
            Assert.AreEqual(2, _hud.CurrentUtilityBarMode);
            Assert.IsFalse(_utilityDock.ClassListContains("action-mode"));
            Assert.IsTrue(_utilityDock.ClassListContains("menu-mode"));
            Assert.IsTrue(_utilityActionRow.ClassListContains("hidden"));
            Assert.IsFalse(_utilityMenuRowA.ClassListContains("hidden"));
            Assert.IsFalse(_utilityMenuRowB.ClassListContains("hidden"));
            Assert.AreEqual("Menu", _utilityToggleLabel.text);

            InvokePrivateMethod("OnUtilityToggleClick");
            Assert.AreEqual(0, _hud.CurrentUtilityBarMode);
            Assert.IsTrue(_utilityDock.ClassListContains("hidden"));
            Assert.IsFalse(_utilityDock.ClassListContains("action-mode"));
            Assert.IsFalse(_utilityDock.ClassListContains("menu-mode"));
            Assert.IsFalse(_utilityToggleBtn.ClassListContains("active"));
            Assert.AreEqual("Mở", _utilityToggleLabel.text);
        }

        [Test]
        public void OnFactionClick_OpensPcToolPanelWithGuildSummary()
        {
            Assert.IsTrue(_pcToolPanel.ClassListContains("hidden"));

            InvokePrivateMethod("OnFactionClick");

            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Bang phái", _pcToolTitle.text);
            Assert.Greater(_pcToolList.contentContainer.childCount, 0);
        }

        [Test]
        public void OnExchangeClick_TogglesTradeInfoPanelWithoutFakePartnerInfo()
        {
            // Initially hidden
            Assert.IsTrue(_tradeInfoPanel.ClassListContains("hidden"));

            // Show
            InvokePrivateMethod("OnExchangeClick");
            Assert.IsFalse(_tradeInfoPanel.ClassListContains("hidden"));
            StringAssert.Contains("Chưa chọn người chơi", _tradePartnerName.text);
            StringAssert.DoesNotContain("Dã Tẩu", _tradePartnerName.text);
            StringAssert.DoesNotContain("200", _tradePartnerLevel.text);
            StringAssert.DoesNotContain("Võ Đang", _tradePartnerFaction.text);
            StringAssert.DoesNotContain("Thiên Hạ", _tradePartnerGuild.text);

            // Hide again
            InvokePrivateMethod("OnExchangeClick");
            Assert.IsTrue(_tradeInfoPanel.ClassListContains("hidden"));
        }

        [Test]
        public void OnRecClick_StartsFrameRecorderAndWritesCaptureMetadata()
        {
            SetPrivateField("_recCaptureToDisk", false);

            InvokePrivateMethod("OnRecClick");

            Assert.IsTrue(GetPrivateField<bool>("_recEnabled"));
            Assert.AreEqual(1, GetPrivateField<int>("_recFrameCount"));
            string firstPath = GetPrivateField<string>("_recLastCapturePath");
            StringAssert.Contains("VltkRecorder", firstPath);
            StringAssert.EndsWith(".png", firstPath);
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Quay phim", _pcToolTitle.text);
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("Đang ghi hình HUD")));
            Assert.IsTrue(labels.Exists(l => l.text.Contains("Player_Recorder")));

            InvokePrivateMethod("UpdateRecorder", 5f);
            Assert.AreEqual(2, GetPrivateField<int>("_recFrameCount"));

            InvokePrivateMethod("OnRecClick");
            Assert.IsFalse(GetPrivateField<bool>("_recEnabled"));
            Assert.IsFalse(_root.Q("BtnRec")?.ClassListContains("active") ?? false);
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
