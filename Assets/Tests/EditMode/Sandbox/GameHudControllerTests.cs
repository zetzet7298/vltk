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
        private TextField _chatInput;
        private VisualElement _sendBtn;
        private VisualElement _chatTabAll;
        private VisualElement _chatTabPrivate;
        private VisualElement _chatTabRoom;
        private VisualElement _chatTabGuild;
        private VisualElement _chatTabFaction;
        private VisualElement _chatTabOther;
        private VisualElement _chatTabs;
        private Label _chatWarning;
        private VisualElement _utilityDock;
        private VisualElement _utilityActionRow;
        private VisualElement _utilityMenuRowA;
        private VisualElement _utilityMenuRowB;
        private VisualElement _utilityToggleBtn;
        private Label _utilityToggleLabel;
        private VisualElement _utilitySwitchBtn;
        private Label _utilitySwitchLabel;
        private VisualElement _skillPanel;
        private ScrollView _skillList;
        private Label _skillSummary;
        private VisualElement _invWindow;
        private ScrollView _invGrid;
        private Label _invMoney;
        private VisualElement _pcToolPanel;
        private VisualElement _pcToolClose;
        private ScrollView _pcToolList;
        private Label _pcToolTitle;
        private VisualElement _pcShortcutDock;
        private VisualElement _minimapPanel;
        private VisualElement _pcShortcutToggleBtn;
        private Label _pcShortcutToggleLabel;

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
            _chatInput = new TextField { name = "ChatInput" };
            _sendBtn = new VisualElement { name = "SendBtn" };
            _sendBtn.Add(new VisualElement { name = "SendBtnIcon" });
            _chatTabAll = new VisualElement { name = "ChatTabAll" };
            _chatTabPrivate = new VisualElement { name = "ChatTabPrivate" };
            _chatTabRoom = new VisualElement { name = "ChatTabRoom" };
            _chatTabGuild = new VisualElement { name = "ChatTabGuild" };
            _chatTabFaction = new VisualElement { name = "ChatTabFaction" };
            _chatTabOther = new VisualElement { name = "ChatTabOther" };
            _chatTabs = new VisualElement { name = "ChatTabs" };
            _chatTabs.Add(_chatTabAll);
            _chatTabs.Add(_chatTabPrivate);
            _chatTabs.Add(_chatTabRoom);
            _chatTabs.Add(_chatTabGuild);
            _chatTabs.Add(_chatTabFaction);
            _chatTabs.Add(_chatTabOther);
            _chatWarning = new Label { name = "ChatWarning" };
            _chatWarning.AddToClassList("hidden");
            _chatTabAll.AddToClassList("active");

            _utilityToggleBtn = new VisualElement { name = "UtilityToggleBtn" };
            _utilityToggleLabel = new Label { name = "UtilityToggleLabel" };
            _utilityToggleBtn.Add(_utilityToggleLabel);
            _utilityDock = new VisualElement { name = "MobileUtilityDock" };
            _utilityDock.AddToClassList("hidden");
            _utilitySwitchBtn = new VisualElement { name = "UtilitySwitchBtn" };
            _utilitySwitchLabel = new Label { name = "UtilitySwitchLabel" };
            _utilitySwitchBtn.Add(_utilitySwitchLabel);
            _utilityActionRow = new VisualElement { name = "MobileUtilityActionRow" };
            _utilityMenuRowA = new VisualElement { name = "MobileUtilityMenuRowA" };
            _utilityMenuRowB = new VisualElement { name = "MobileUtilityMenuRowB" };
            foreach (string name in new[] { "BtnSit", "BtnRun", "BtnHorse", "BtnExchange", "BtnRec", "BtnPK", "BtnTreasure" })
                _utilityActionRow.Add(CreateUtilityButton(name));
            foreach (string name in new[] { "BtnStatus", "BtnItems", "BtnItemEx", "BtnSkills", "BtnTask" })
                _utilityMenuRowA.Add(CreateUtilityButton(name));
            foreach (string name in new[] { "BtnFriend", "BtnTeam", "BtnFaction", "BtnChatRoom", "BtnOptions" })
                _utilityMenuRowB.Add(CreateUtilityButton(name));
            _utilityDock.Add(_utilitySwitchBtn);
            _utilityDock.Add(_utilityActionRow);
            _utilityDock.Add(_utilityMenuRowA);
            _utilityDock.Add(_utilityMenuRowB);

            _pcShortcutToggleBtn = new VisualElement { name = "PcShortcutToggleBtn" };
            _pcShortcutToggleLabel = new Label { name = "PcShortcutToggleLabel" };
            _pcShortcutToggleBtn.Add(_pcShortcutToggleLabel);
            _pcShortcutDock = new VisualElement { name = "PcShortcutDock" };
            _pcShortcutDock.AddToClassList("hidden");
            for (int i = 0; i < 9; i++)
                _pcShortcutDock.Add(new VisualElement { name = $"PcItemSlot{i}" });
            _pcShortcutDock.Add(new VisualElement { name = "PcLeftSkillBtn" });
            _pcShortcutDock.Add(new VisualElement { name = "PcRightSkillBtn" });

            foreach (string name in new[] { "IconBarArenaBtn", "IconBarActivityBtn", "IconBarTreasureBtn", "IconBarShopBtn", "IconBarPetBtn", "IconBarLoginPrizeBtn", "IconBarFuncPrizeBtn" })
                _root.Add(new Button { name = name });

            _skillPanel = new VisualElement { name = "CaiBangSkillPanel" };
            _skillPanel.AddToClassList("hidden");
            _skillSummary = new Label { name = "CaiBangSkillSummary" };
            _skillList = new ScrollView { name = "CaiBangSkillList" };
            _skillPanel.Add(_skillSummary);
            _skillPanel.Add(_skillList);

            _invWindow = new VisualElement { name = "InventoryWindow" };
            _invWindow.AddToClassList("hidden");
            _invGrid = new ScrollView { name = "InventoryGrid" };
            _invMoney = new Label { name = "InventoryMoney" };
            _invWindow.Add(_invMoney);
            _invWindow.Add(_invGrid);

            _minimapPanel = new VisualElement { name = "MinimapPanel" };

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
            _root.Add(_chatInput);
            _root.Add(_sendBtn);
            _root.Add(_chatWarning);
            _root.Add(_chatTabs);
            _root.Add(_utilityToggleBtn);
            _root.Add(_utilityDock);
            _root.Add(_pcShortcutToggleBtn);
            _root.Add(_pcShortcutDock);
            _root.Add(_minimapPanel);
            _root.Add(_skillPanel);
            _root.Add(_invWindow);
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
            SetPrivateField("_chatInput", _chatInput);
            SetPrivateField("_chatTabs", _chatTabs);
            SetPrivateField("_chatWarning", _chatWarning);
            SetPrivateField("_utilityDock", _utilityDock);
            SetPrivateField("_utilityActionRow", _utilityActionRow);
            SetPrivateField("_utilityMenuRowA", _utilityMenuRowA);
            SetPrivateField("_utilityMenuRowB", _utilityMenuRowB);
            SetPrivateField("_utilityToggleLabel", _utilityToggleLabel);
            SetPrivateField("_utilitySwitchLabel", _utilitySwitchLabel);
            SetPrivateField("_pcShortcutDock", _pcShortcutDock);
            SetPrivateField("_pcShortcutToggleBtn", _pcShortcutToggleBtn);
            SetPrivateField("_pcShortcutToggleLabel", _pcShortcutToggleLabel);
            SetPrivateField("_minimapPanel", _minimapPanel);
            SetPrivateField("_skillPanel", _skillPanel);
            SetPrivateField("_skillList", _skillList);
            SetPrivateField("_skillSummary", _skillSummary);
            SetPrivateField("_invWindow", _invWindow);
            SetPrivateField("_invGrid", _invGrid);
            SetPrivateField("_invMoney", _invMoney);
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

        private static VisualElement CreateUtilityButton(string name)
        {
            var button = new VisualElement { name = name };
            button.Add(new VisualElement { name = name + "Icon" });
            return button;
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

        private object InvokePrivateMethod(string methodName, params object[] args)
        {
            var method = typeof(GameHudController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method {methodName} not found on GameHudController");
            return method.Invoke(_hud, args);
        }

        private void InvokeAndAssertPcTool(HashSet<string> covered, string pcKey, string methodName, string title, string rowContains = null)
        {
            covered.Add(pcKey);
            InvokePrivateMethod(methodName);
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"), methodName + " should open PcToolPanel");
            Assert.AreEqual(title, _pcToolTitle.text, methodName + " title");
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.Greater(labels.Count, 0, methodName + " should render at least one row");
            if (!string.IsNullOrEmpty(rowContains))
                Assert.IsTrue(labels.Exists(l => l.text.Contains(rowContains)), methodName + " should mention " + rowContains);
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
        public void MinimapPcButtons_ExposeFlagMarkerActionOnly()
        {
            InvokePrivateMethod("OnMinimapMarkerClick");
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Đánh dấu bản đồ", _pcToolTitle.text);
            Assert.IsTrue(GetPrivateField<Vector2?>("_lastMoveTarget").HasValue, "PC BtnFlag must immediately create a map flag target.");
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("Cắm cờ")));
            Assert.IsTrue(labels.Exists(l => l.text.Contains("FlagImage=地图小旗帜.spr")));
        }

        [Test]
        public void ChatTabsAndSendButton_ExposePcBottomChatControls()
        {
            InvokePrivateMethod("SelectChatChannel", ChatChannel.Guild);

            Assert.AreEqual(ChatChannel.Guild, GetPrivateField<ChatChannel>("_selectedChatChannel"));
            Assert.IsFalse(_chatTabAll.ClassListContains("active"));
            Assert.IsTrue(_chatTabGuild.ClassListContains("active"));
            Assert.IsFalse(_chatTabFaction.ClassListContains("active"));

            _chatInput.value = "xin chào bang";
            InvokePrivateMethod("OnSendChatClick");

            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Chat", _pcToolTitle.text);
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("Tin nhắn nháp: xin chào bang")));
        }

        [Test]
        public void ChatRailButtons_ToggleChannelsAndOpenHistoryPanel()
        {
            Assert.IsFalse(_chatTabs.ClassListContains("hidden"));

            InvokePrivateMethod("OnChatSizeClick");
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Chat", _pcToolTitle.text);

            InvokePrivateMethod("OnChatMoveClick");
            Assert.AreEqual("Chat", _pcToolTitle.text);

            InvokePrivateMethod("OnChatShadowClick");
            Assert.AreEqual("Chat", _pcToolTitle.text);

            InvokePrivateMethod("OnChatChannelToggleClick");
            Assert.IsTrue(_chatTabs.ClassListContains("hidden"));
            Assert.AreEqual("Kênh chat", _pcToolTitle.text);

            InvokePrivateMethod("OnChatScrollUpClick");
            Assert.AreEqual("Lịch sử chat", _pcToolTitle.text);
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("Chưa có tin nhắn") || l.text.Contains("Kênh:")));

            InvokePrivateMethod("OnChatScrollDownClick");
            Assert.AreEqual("Lịch sử chat", _pcToolTitle.text);

            InvokePrivateMethod("OnChatSystemOpenClick");
            Assert.IsFalse(_chatWarning.ClassListContains("hidden"));
            Assert.AreEqual("Nhắc nhở hệ thống", _pcToolTitle.text);

            InvokePrivateMethod("OnChatSystemUpClick");
            Assert.AreEqual("Nhắc nhở hệ thống", _pcToolTitle.text);
            InvokePrivateMethod("OnChatSystemDownClick");
            Assert.AreEqual("Nhắc nhở hệ thống", _pcToolTitle.text);
        }

        [Test]
        public void ChatService_UsesPcBottomChatChannelLabels()
        {
            Assert.AreEqual("Tất Cả", ChatService.ChannelNameVi(ChatChannel.All));
            Assert.AreEqual("Mật", ChatService.ChannelNameVi(ChatChannel.Private));
            Assert.AreEqual("Phòng", ChatService.ChannelNameVi(ChatChannel.Room));
            Assert.AreEqual("Bang Hội", ChatService.ChannelNameVi(ChatChannel.Guild));
            Assert.AreEqual("Môn Phái", ChatService.ChannelNameVi(ChatChannel.Faction));
            Assert.AreEqual("Khác", ChatService.ChannelNameVi(ChatChannel.Other));

            var chat = new ChatService();
            chat.SetChannel(ChatChannel.Guild);
            chat.SendPlayerMessage(ChatChannel.Guild, "Bang chủ", "Tập hợp");
            var filtered = chat.GetFilteredMessages();
            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual(ChatChannel.Guild, filtered[0].channel);
        }

        [Test]
        public void UtilityToggle_OpenCloseAndSwitchTogglesActionMenu()
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
            Assert.IsFalse(_utilitySwitchBtn.ClassListContains("active"));
            Assert.AreEqual("Ẩn", _utilityToggleLabel.text);
            Assert.AreEqual("Menu", _utilitySwitchLabel.text);

            InvokePrivateMethod("OnUtilitySwitchClick");
            Assert.AreEqual(2, _hud.CurrentUtilityBarMode);
            Assert.IsFalse(_utilityDock.ClassListContains("action-mode"));
            Assert.IsTrue(_utilityDock.ClassListContains("menu-mode"));
            Assert.IsTrue(_utilityActionRow.ClassListContains("hidden"));
            Assert.IsFalse(_utilityMenuRowA.ClassListContains("hidden"));
            Assert.IsFalse(_utilityMenuRowB.ClassListContains("hidden"));
            Assert.IsTrue(_utilitySwitchBtn.ClassListContains("active"));
            Assert.AreEqual("Ẩn", _utilityToggleLabel.text);
            Assert.AreEqual("Tác", _utilitySwitchLabel.text);

            InvokePrivateMethod("OnUtilitySwitchClick");
            Assert.AreEqual(1, _hud.CurrentUtilityBarMode);
            Assert.IsTrue(_utilityDock.ClassListContains("action-mode"));
            Assert.IsFalse(_utilityDock.ClassListContains("menu-mode"));
            Assert.IsFalse(_utilitySwitchBtn.ClassListContains("active"));
            Assert.AreEqual("Menu", _utilitySwitchLabel.text);

            InvokePrivateMethod("OnUtilityToggleClick");
            Assert.AreEqual(0, _hud.CurrentUtilityBarMode);
            Assert.IsTrue(_utilityDock.ClassListContains("hidden"));
            Assert.IsFalse(_utilityDock.ClassListContains("action-mode"));
            Assert.IsFalse(_utilityDock.ClassListContains("menu-mode"));
            Assert.IsFalse(_utilityToggleBtn.ClassListContains("active"));
            Assert.AreEqual("Mở", _utilityToggleLabel.text);
        }

        [Test]
        public void PcShortcutDock_TogglesAndRoutesPcHotkeys()
        {
            Assert.IsTrue(_pcShortcutDock.ClassListContains("hidden"));

            InvokePrivateMethod("OnUtilityToggleClick");
            Assert.AreEqual(1, _hud.CurrentUtilityBarMode);

            InvokePrivateMethod("OnPcShortcutToggleClick");
            Assert.AreEqual(0, _hud.CurrentUtilityBarMode, "Shortcut and utility bars must be mutually exclusive near the minimap.");
            Assert.IsTrue(_utilityDock.ClassListContains("hidden"));
            Assert.IsFalse(_pcShortcutDock.ClassListContains("hidden"));
            Assert.IsTrue(_pcShortcutToggleBtn.ClassListContains("active"));
            Assert.AreEqual("Ẩn", _pcShortcutToggleLabel.text);

            InvokePrivateMethod("OnPcItemShortcutClick", 2);
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Phím tắt vật phẩm 3", _pcToolTitle.text);
            var itemRows = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(itemRows.Exists(l => l.text.Contains("ShortcutUseItem(2)")));

            InvokePrivateMethod("OnPcShortcutToggleClick");
            Assert.IsTrue(_pcShortcutDock.ClassListContains("hidden"));
            Assert.IsFalse(_pcShortcutToggleBtn.ClassListContains("active"));
            Assert.AreEqual("1-9", _pcShortcutToggleLabel.text);
        }

        [Test]
        public void PcSkillShortcut_FallsBackToPcSkillPanelWhenCombatPickerUnavailable()
        {
            Assert.IsTrue(_skillPanel.ClassListContains("hidden"));

            InvokePrivateMethod("OnPcSkillShortcutClick", 1);

            Assert.IsFalse(_skillPanel.ClassListContains("hidden"));
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Kỹ năng phải", _pcToolTitle.text);
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("CombatSkillSlotController chưa sẵn sàng")));
        }

        [Test]
        public void AllPcUtilityHandlers_ProduceExpectedMobileSideEffects()
        {
            var covered = new HashSet<string>();
            SetPrivateField("_recCaptureToDisk", false);

            covered.Add("Run");
            InvokePrivateMethod("OnRunClick");
            Assert.IsTrue(_root.Q("BtnRun").ClassListContains("active"));

            InvokeAndAssertPcTool(covered, "Sit", "OnSitClick", "Ngồi", "Đang ngồi");
            Assert.IsTrue(_root.Q("BtnSit").ClassListContains("active"));

            InvokeAndAssertPcTool(covered, "Horse", "OnHorseClick", "Lên xuống ngựa", "Player runtime");

            InvokeAndAssertPcTool(covered, "Exchange", "OnExchangeClick", "Giao dịch", "PC [OkBtn] Khóa giao dịch");
            Assert.IsFalse(_tradeInfoPanel.ClassListContains("hidden"));
            StringAssert.Contains("Chưa chọn người chơi", _tradePartnerName.text);

            InvokeAndAssertPcTool(covered, "Rec", "OnRecClick", "Quay phim", "Player_Recorder");
            Assert.IsTrue(_root.Q("BtnRec").ClassListContains("active"));

            InvokeAndAssertPcTool(covered, "PK", "OnPKClick", "PK", "Tự do");
            Assert.IsTrue(_root.Q("BtnPK").ClassListContains("active"));

            InvokeAndAssertPcTool(covered, "Treasure", "OnTreasureClick", "Bảo Vật", "PC 9e5f75d1 [PrePaid] Nạp thẻ");
            InvokeAndAssertPcTool(covered, "Status", "OnStatusClick", CharacterPanelService.Title, "Sinh lực");

            covered.Add("Items");
            InvokePrivateMethod("OnItemsClick");
            Assert.IsTrue(_hud.IsInventoryVisible);
            Assert.AreEqual(InventoryPanelService.GridSlotCount, _hud.InventorySlotCount);

            InvokeAndAssertPcTool(covered, "ItemEx", "OnItemExClick", "Túi hành trang", "Tổng rương");

            covered.Add("Skills");
            InvokePrivateMethod("OnSkillsClick");
            Assert.IsTrue(_hud.IsSkillPanelVisible);
            Assert.Greater(_hud.PcSkillPanelRowCount, 0);

            InvokeAndAssertPcTool(covered, "Task", "OnTaskClick", "Nhiệm vụ", "PC [Task] Player_Task");
            InvokeAndAssertPcTool(covered, "Friend", "OnFriendClick", "Bằng hữu", "PC [FindBtn] Thêm bạn hữu");

            InvokeAndAssertPcTool(covered, "Team", "OnTeamClick", "Tổ đội", "PC [Invite] Mời vào đội");
            Assert.IsFalse(_teamPreview.ClassListContains("hidden"));
            StringAssert.Contains("Chưa tham gia đội", _teamPreview.Q<Label>().text);

            InvokeAndAssertPcTool(covered, "Faction", "OnFactionClick", "Bang phái", "PC 223e63d0 [BtnUpgradeBuildLevel] Nâng công trình");
            InvokeAndAssertPcTool(covered, "ChatRoom", "OnChatRoomClick", "Phòng chat", "Channel14: CH_CUSTOM");
            InvokeAndAssertPcTool(covered, "Options", "OnOptionsClick", "Hệ thống", "Treo máy offline");

            CollectionAssert.AreEquivalent(HudBottomBarPcSpec.ToolControlBar.Keys, covered);
        }




        [Test]
        public void PcTreasureMallControls_AreClickableAndExecutePcActions()
        {
            InvokePrivateMethod("OnTreasureClick");
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Bảo Vật", _pcToolTitle.text);
            var actionRows = _pcToolList.Query<VisualElement>(className: "hud-pc-tool-action-row").ToList();
            Assert.AreEqual(TreasureMallPanelService.PcControls.Count, actionRows.Count, "PC mall/cart/treasure controls must be action rows, not inert text.");

            InvokePrivateMethod("OnPcTreasureMallControlClick", "RightBtn");
            Assert.AreEqual(1, GetPrivateField<int>("_mallPage"));
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [RightBtn]: đã sang trang hàng")));

            InvokePrivateMethod("OnPcTreasureMallControlClick", "ShoppingCart");
            Assert.IsTrue(GetPrivateField<bool>("_mallCartOpen"));
            labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [ShoppingCart]: đã mở giỏ hàng")));

            InvokePrivateMethod("OnPcTreasureMallControlClick", "GoodsInfo_AddCount");
            Assert.AreEqual(2, GetPrivateField<int>("_mallQuantity"));

            InvokePrivateMethod("OnPcTreasureMallControlClick", "btn_cathectic2");
            Assert.AreEqual(10, GetPrivateField<int>("_treasureChestBet"));

            InvokePrivateMethod("OnPcTreasureMallControlClick", "btn_begin");
            Assert.IsTrue(GetPrivateField<bool>("_treasureChestSpun"));

            InvokePrivateMethod("OnPcTreasureMallControlClick", "CloseCartBtn");
            Assert.IsFalse(GetPrivateField<bool>("_mallCartOpen"));
        }

        [Test]
        public void PcExchangeControls_AreClickableAndExecutePcActions()
        {
            InvokePrivateMethod("OnExchangeClick");
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Giao dịch", _pcToolTitle.text);
            var actionRows = _pcToolList.Query<VisualElement>(className: "hud-pc-tool-action-row").ToList();
            Assert.AreEqual(ExchangePanelService.PcControls.Count, actionRows.Count, "PC d84aceb8 trade command buttons must be action rows, not inert text.");

            InvokePrivateMethod("OnPcExchangeControlClick", "OkBtn");
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [OkBtn]: chưa có phiên giao dịch")));

            InvokePrivateMethod("OnPcExchangeControlClick", "AddMoney");
            labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [AddMoney]: chưa có phiên giao dịch")));

            InvokePrivateMethod("OnPcExchangeControlClick", "TradeBtn");
            labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [TradeBtn]: đang chờ cả hai bên khóa")));

            InvokePrivateMethod("OnPcExchangeControlClick", "CancelBtn");
            Assert.IsTrue(_pcToolPanel.ClassListContains("hidden"));
            Assert.IsTrue(_tradeInfoPanel.ClassListContains("hidden"));
        }

        [Test]
        public void PcTeamControls_AreClickableAndExecutePcActions()
        {
            InvokePrivateMethod("OnTeamClick");
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Tổ đội", _pcToolTitle.text);
            var actionRows = _pcToolList.Query<VisualElement>(className: "hud-pc-tool-action-row").ToList();
            Assert.AreEqual(TeamPanelService.PcControls.Count, actionRows.Count, "PC a05d7a2c team command buttons must be action rows, not inert text.");

            InvokePrivateMethod("OnPcTeamControlClick", "CloseTeam");
            Assert.IsTrue(GetPrivateField<bool>("_teamNearbyListClosed"));
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [CloseTeam]: đã đóng danh sách lân cận")));

            InvokePrivateMethod("OnPcTeamControlClick", "Refresh");
            labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [Refresh]: đã làm mới")));

            InvokePrivateMethod("OnPcTeamControlClick", "Cancel");
            Assert.IsTrue(_pcToolPanel.ClassListContains("hidden"));
            Assert.IsTrue(_teamPreview.ClassListContains("hidden"));
        }


        [Test]
        public void PcGuildControls_AreClickableAndExecutePcActions()
        {
            InvokePrivateMethod("OnFactionClick");
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Bang phái", _pcToolTitle.text);
            var actionRows = _pcToolList.Query<VisualElement>(className: "hud-pc-tool-action-row").ToList();
            Assert.AreEqual(GuildPanelService.PcControls.Count, actionRows.Count, "PC guild controls from 223e63d0/120ebf4e/f5054c2e must be action rows, not inert text.");

            InvokePrivateMethod("OnPcGuildControlClick", "BtnOnlinePriority");
            Assert.IsTrue(GetPrivateField<bool>("_guildOnlinePriority"));
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [BtnOnlinePriority]: ưu tiên thành viên online")));

            InvokePrivateMethod("OnPcGuildControlClick", "BtnNextPage");
            Assert.AreEqual(1, GetPrivateField<int>("_guildPage"));
            labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [BtnNextPage]: đã sang trang")));

            InvokePrivateMethod("OnPcGuildControlClick", "BtnAnnounce");
            Assert.AreEqual("BtnAnnounce", GetPrivateField<string>("_guildRecordTab"));
            labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("chuyển tab Thông báo")));

            InvokePrivateMethod("OnPcGuildControlClick", "Save");
            labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [Save]: đã lưu cấu hình tuyển người")));
        }

        [Test]
        public void PcChatRoomChannels_AreClickableAndSelectRuntimeChannels()
        {
            InvokePrivateMethod("OnChatRoomClick");
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Phòng chat", _pcToolTitle.text);
            var actionRows = _pcToolList.Query<VisualElement>(className: "hud-pc-tool-action-row").ToList();
            Assert.AreEqual(15, actionRows.Count, "PC [Channels] Channel0..Channel14 must be action rows, not inert text.");

            InvokePrivateMethod("OnPcChatRoomChannelClick", ChatRoomPanelService.PcChannels[2]);
            Assert.AreEqual(ChatChannel.World, GetPrivateField<ChatChannel>("_selectedChatChannel"));
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [Channels] Channel2: CH_WORLD")));
            Assert.IsTrue(labels.Exists(l => l.text.Contains("60000ms/2")));

            InvokePrivateMethod("OnPcChatRoomChannelClick", ChatRoomPanelService.PcChannels[8]);
            Assert.AreEqual(ChatChannel.Room, GetPrivateField<ChatChannel>("_selectedChatChannel"));

            InvokePrivateMethod("OnPcChatRoomChannelClick", ChatRoomPanelService.PcChannels[14]);
            Assert.AreEqual(ChatChannel.Other, GetPrivateField<ChatChannel>("_selectedChatChannel"));
        }

        [Test]
        public void PcFriendControls_AreClickableAndExecutePcActions()
        {
            InvokePrivateMethod("OnFriendClick");
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Bằng hữu", _pcToolTitle.text);
            var actionRows = _pcToolList.Query<VisualElement>(className: "hud-pc-tool-action-row").ToList();
            Assert.AreEqual(10, actionRows.Count, "PC 2b9c5056 active controls must be action rows, not inert text.");

            InvokePrivateMethod("OnPcFriendControlClick", "UnitBtnEnemy");
            Assert.AreEqual("UnitBtnEnemy", GetPrivateField<string>("_friendFilter"));
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("lọc Cừu nhân")));

            InvokePrivateMethod("OnPcFriendControlClick", "GroupBtn");
            Assert.IsFalse(GetPrivateField<bool>("_friendGroupExpanded"));
            labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("Nhóm bằng hữu đang thu gọn")));

            InvokePrivateMethod("OnPcFriendControlClick", "ScrollDown");
            Assert.AreEqual(1, GetPrivateField<int>("_friendScrollOffset"));
            InvokePrivateMethod("OnPcFriendControlClick", "ScrollUp");
            Assert.AreEqual(0, GetPrivateField<int>("_friendScrollOffset"));

            InvokePrivateMethod("OnPcFriendControlClick", "Invisible");
            Assert.AreEqual("Đồng hành", _pcToolTitle.text);
            Assert.IsTrue(GetPrivateField<bool>("_friendInvisible"));
            labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("Đã bật trạng thái")));

            InvokePrivateMethod("OnPcFriendControlClick", "FindBtn");
            Assert.AreEqual("Thêm bạn hữu", _pcToolTitle.text);
            labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [FindBtn]")));

            _pcToolPanel.RemoveFromClassList("hidden");
            InvokePrivateMethod("OnPcFriendControlClick", "CloseBtn");
            Assert.IsTrue(_pcToolPanel.ClassListContains("hidden"));
        }

        [Test]
        public void PcSystemMenuRows_AreClickableAndExecutePcActions()
        {
            InvokePrivateMethod("OnOptionsClick");
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Hệ thống", _pcToolTitle.text);
            var actionRows = _pcToolList.Query<VisualElement>(className: "hud-pc-tool-action-row").ToList();
            Assert.AreEqual(5, actionRows.Count, "PC e6641da3 rows must be action rows, not inert text.");

            InvokePrivateMethod("OnPcSystemMenuRowClick", SystemMenuPanelService.MenuOffLine);
            Assert.AreEqual("Treo máy offline", _pcToolTitle.text);
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("Đã bật treo máy offline")));
            Assert.IsTrue(GetPrivateField<bool>("_offlineMode"));

            InvokePrivateMethod("OnPcSystemMenuRowClick", SystemMenuPanelService.MenuOptions);
            Assert.AreEqual("Tùy chọn", _pcToolTitle.text);
            labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("PC [Options]")));

            InvokePrivateMethod("OnPcSystemMenuRowClick", SystemMenuPanelService.MenuGameHelp);
            Assert.AreEqual("Trợ giúp", _pcToolTitle.text);

            InvokePrivateMethod("OnPcSystemMenuRowClick", SystemMenuPanelService.MenuExitGame);
            Assert.AreEqual("Thoát game", _pcToolTitle.text);
            labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("Yêu cầu xác nhận")));

            _pcToolPanel.RemoveFromClassList("hidden");
            InvokePrivateMethod("OnPcSystemMenuRowClick", SystemMenuPanelService.MenuContinueGame);
            Assert.IsTrue(_pcToolPanel.ClassListContains("hidden"));
        }

        [Test]
        public void WorldMapButton_OpensPcWorldMapCatalog()
        {
            InvokePrivateMethod("OnWorldMapClick");

            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Bản đồ thế giới", _pcToolTitle.text);
            var labels = _pcToolList.Query<Label>().ToList();
            Assert.IsTrue(labels.Exists(l => l.text.Contains("Bản đồ thế giới PC")));
            Assert.IsTrue(labels.Exists(l => l.text.Contains("小地图－世界大地图按钮.spr")));
        }

        [Test]
        public void ToggleMapButton_SwitchesBetweenPcSmallAndLargeMinimap()
        {
            Assert.IsFalse(_minimapPanel.ClassListContains("hud-minimap-large"));

            InvokePrivateMethod("OnToggleMapClick");

            Assert.IsTrue(GetPrivateField<bool>("_minimapExpanded"));
            Assert.IsTrue(_minimapPanel.ClassListContains("hud-minimap-large"));
            Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
            Assert.AreEqual("Chuyển bản đồ nhỏ/lớn", _pcToolTitle.text);
            Assert.IsTrue(_pcToolList.Query<Label>().ToList().Exists(l => l.text.Contains("小地图_小.ini")));

            InvokePrivateMethod("OnToggleMapClick");

            Assert.IsFalse(GetPrivateField<bool>("_minimapExpanded"));
            Assert.IsFalse(_minimapPanel.ClassListContains("hud-minimap-large"));
        }

        [Test]
        public void PcIconBarButtons_OpenRuntimeBackedPanels()
        {
            for (int i = 0; i < 7; i++)
            {
                InvokePrivateMethod("OnIconBarClick", i);
                Assert.IsFalse(_pcToolPanel.ClassListContains("hidden"));
                Assert.Greater(_pcToolList.contentContainer.childCount, 1);
                var labels = _pcToolList.Query<Label>().ToList();
                Assert.IsTrue(labels.Exists(l => l.text.Contains("PC source: Ui3/icon_bar.ini")), "Icon bar panel must cite PC source.");
            }

            InvokePrivateMethod("OnIconBarClick", 0);
            Assert.AreEqual("Đấu trường", _pcToolTitle.text);
            StringAssert.Contains("Đấu trường PC loaded", _pcToolList.Query<Label>().ToList()[2].text);

            InvokePrivateMethod("OnIconBarClick", 6);
            Assert.AreEqual("Thưởng chức năng", _pcToolTitle.text);
            Assert.IsTrue(_root.Q("IconBarFuncPrizeBtn").ClassListContains("active"));
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
        public void BeginExchangeSession_UsesEconomyTradeSessionForOnlinePartyTarget()
        {
            var economy = new EconomyService(initialSilver: 1234);
            var members = new List<PartyMember>
            {
                new PartyMember { memberId = SandboxManager.PlayerActorId, nameVi = "Bản thân", level = 50, factionId = 7, isOnline = true },
                new PartyMember { memberId = 2, nameVi = "Đồng Đội", level = 48, factionId = 3, isOnline = true },
            };

            var session = (TradeSession)InvokePrivateMethod("BeginExchangeSession", economy, members);

            Assert.IsNotNull(session);
            Assert.AreEqual(SandboxManager.PlayerActorId, session.initiatorId);
            Assert.AreEqual(2, session.targetId);
            Assert.AreSame(session, GetPrivateField<TradeSession>("_tradeSession"));
            Assert.AreEqual("Đồng Đội", GetPrivateField<PartyMember>("_tradeTarget").nameVi);
            Assert.AreSame(economy, GetPrivateField<EconomyService>("_tradeEconomy"));

            InvokePrivateMethod("PopulateTradeInfo");
            StringAssert.Contains("Đồng Đội", _tradePartnerName.text);
            StringAssert.Contains("1234", _tradePartnerLevel.text);
            StringAssert.Contains("1->2", _tradePartnerFaction.text);
            StringAssert.Contains("Đặt bạc: 0", _tradePartnerGuild.text);
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
