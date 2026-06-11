using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using VLTK.Core;


namespace VLTK.Sandbox
{
    public enum GMTab
    {
        Overview,
        Map,
        Player,
        World,
        Assets,
        Logs,
        Tools,
        Equipment,
    }

    public class GMPanelController : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        private UnityEngine.UI.Image _backgroundImage;
        private bool _initialized;

        [Header("Tab Buttons")]
        public GMTabBarController tabBar;

        [Header("Tab Panels")]
        public GameObject overviewPanel;
        public GameObject mapPanel;
        public GameObject playerPanel;
        public GameObject worldPanel;
        public GameObject assetsPanel;
        public GameObject logsPanel;
        public GameObject toolsPanel;

        public GMTab ActiveTab { get; private set; } = GMTab.Map;
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private const string TOGGLE_KEY = "g";

        private GameObject _equipmentPanel;
        private GameObject _joystickGo;

        private void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            _backgroundImage = GetComponent<UnityEngine.UI.Image>();
            
            SetupEquipmentTabDynamically();

            if (tabBar != null)
            {
                tabBar.Initialize(this);
            }
        }

        private void SetupEquipmentTabDynamically()
        {
            if (tabBar == null || tabBar.tabs == null || tabBar.tabs.Length == 0) return;

            // 1. Clone the last button (Tools tab button)
            var lastEntry = tabBar.tabs[tabBar.tabs.Length - 1];
            if (lastEntry.button == null) return;

            var newButtonGo = Instantiate(lastEntry.button.gameObject, lastEntry.button.transform.parent);
            newButtonGo.name = "TabButton_Equipment";

            // Update text of cloned button
            var txt = newButtonGo.GetComponentInChildren<Text>();
            if (txt != null) txt.text = "Trang bị";

            var btn = newButtonGo.GetComponent<Button>();
            btn.onClick.RemoveAllListeners(); // Clear cloned listeners

            // Create new TabEntry
            var newEntry = new GMTabBarController.TabEntry
            {
                label = "Trang bị",
                button = btn,
                index = 7 // GMTab.Equipment
            };

            // Expand tabBar.tabs array
            var newTabs = new GMTabBarController.TabEntry[tabBar.tabs.Length + 1];
            for (int i = 0; i < tabBar.tabs.Length; i++)
            {
                newTabs[i] = tabBar.tabs[i];
            }
            newTabs[tabBar.tabs.Length] = newEntry;
            tabBar.tabs = newTabs;

            // 2. Clone/Create the Panel under same parent as toolsPanel
            var templatePanel = toolsPanel != null ? toolsPanel : playerPanel;
            if (templatePanel == null) return;

            _equipmentPanel = new GameObject("Panel_Equipment");
            _equipmentPanel.transform.SetParent(templatePanel.transform.parent, false);

            var rect = _equipmentPanel.AddComponent<RectTransform>();
            var tempRect = templatePanel.GetComponent<RectTransform>();
            if (tempRect != null)
            {
                rect.anchorMin = tempRect.anchorMin;
                rect.anchorMax = tempRect.anchorMax;
                rect.anchoredPosition = tempRect.anchoredPosition;
                rect.sizeDelta = tempRect.sizeDelta;
                rect.pivot = tempRect.pivot;
            }
            else
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
            }

            _equipmentPanel.AddComponent<GMEquipmentTab>();
            _equipmentPanel.SetActive(false);
        }

        private void Awake()
        {
            EnsureInitialized();
            // Start with inner panel hidden, keep GMPanel active for shortcut detection
            if (panelRoot != null) panelRoot.SetActive(false);
            if (_backgroundImage != null) _backgroundImage.enabled = false;
        }

        private void Update()
        {
            if (IsTypingInInput()) return;

            // Support both InputSystem and legacy Input for keyboard shortcut G
            bool gPressed = false;
            try
            {
                gPressed = Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
            }
            catch { }

            // Fallback to legacy Input (for Editor without InputSystem focus)
            if (!gPressed)
            {
                try { gPressed = Input.GetKeyDown(KeyCode.G); } catch { }
            }

            if (gPressed)
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (panelRoot == null) return;
            SetOpen(!panelRoot.activeSelf);
        }

        public void Open()
        {
            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        public void OnGMButtonClicked()
        {
            Toggle();
        }

        public void SwitchTab(int tabIndex)
        {
            if (tabIndex >= 0 && tabIndex < 8)
            {
                ActiveTab = (GMTab)tabIndex;
                UpdateTabPanels();
                if (tabBar != null) tabBar.RefreshColors(tabIndex);
                SubsystemLog.Info("GM", $"Switched to tab: {ActiveTab}");
            }
        }

        private void SetOpen(bool open)
        {
            EnsureInitialized();
            if (panelRoot != null) panelRoot.SetActive(open);
            if (_backgroundImage != null) _backgroundImage.enabled = open;
            if (open)
            {
                transform.SetAsLastSibling();
                UpdateTabPanels();
                if (tabBar != null) tabBar.RefreshColors((int)ActiveTab);
                SubsystemLog.Info("GM", "Panel opened");

                // Hide joystick to prevent blocking left equipment buttons
                var joystick = UnityEngine.Object.FindAnyObjectByType<MobileJoystick>();
                if (joystick != null)
                {
                    _joystickGo = joystick.gameObject;
                    _joystickGo.SetActive(false);
                }

                // Hide UI Toolkit HUD to prevent blocking clicks on the left buttons (especially ChatInputRow/ChatBar)
                var hudType = System.Type.GetType("VLTK.UI.GameHudController, Assembly-CSharp");
                if (hudType != null)
                {
                    var hud = UnityEngine.Object.FindAnyObjectByType(hudType) as MonoBehaviour;
                    if (hud != null)
                    {
                        var uiDoc = hud.GetComponent<UnityEngine.UIElements.UIDocument>();
                        if (uiDoc != null)
                        {
                            uiDoc.enabled = false;
                        }
                    }
                }

                // Hide IMGUI HUD Overlay
                var overlayType = System.Type.GetType("VLTK.UI.PcHudVietnameseTextOverlay, Assembly-CSharp");
                if (overlayType != null)
                {
                    var overlay = UnityEngine.Object.FindAnyObjectByType(overlayType) as MonoBehaviour;
                    if (overlay != null)
                    {
                        overlay.enabled = false;
                    }
                }

                // Hide Chat panel and button to prevent overlapping/blocking clicks
                var mgr = SandboxManager.Instance;
                if (mgr != null)
                {
                    if (mgr.ChatPanel != null)
                    {
                        mgr.ChatPanel.gameObject.SetActive(false);
                    }
                    var uiRoot = mgr.ChatPanel != null && mgr.ChatPanel.transform.parent != null
                        ? mgr.ChatPanel.transform.parent.parent
                        : null;
                    var chatBtn = uiRoot != null ? uiRoot.Find("SandboxCanvas/ChatBtn")?.gameObject : null;
                    if (chatBtn != null) chatBtn.SetActive(false);
                }
            }
            else
            {
                SubsystemLog.Info("GM", "Panel closed");

                // Restore joystick
                if (_joystickGo != null)
                {
                    _joystickGo.SetActive(true);
                    _joystickGo = null;
                }

                // Restore UI Toolkit HUD
                var hudType = System.Type.GetType("VLTK.UI.GameHudController, Assembly-CSharp");
                if (hudType != null)
                {
                    var hud = UnityEngine.Object.FindAnyObjectByType(hudType) as MonoBehaviour;
                    if (hud != null)
                    {
                        var uiDoc = hud.GetComponent<UnityEngine.UIElements.UIDocument>();
                        if (uiDoc != null)
                        {
                            uiDoc.enabled = true;
                        }
                    }
                }

                // Restore IMGUI HUD Overlay
                var overlayType = System.Type.GetType("VLTK.UI.PcHudVietnameseTextOverlay, Assembly-CSharp");
                if (overlayType != null)
                {
                    var overlay = UnityEngine.Object.FindAnyObjectByType(overlayType) as MonoBehaviour;
                    if (overlay != null)
                    {
                        overlay.enabled = true;
                    }
                }

                // Restore Chat panel and button
                var mgr = SandboxManager.Instance;
                if (mgr != null)
                {
                    if (mgr.ChatPanel != null)
                    {
                        mgr.ChatPanel.gameObject.SetActive(true);
                    }
                    var uiRoot = mgr.ChatPanel != null && mgr.ChatPanel.transform.parent != null
                        ? mgr.ChatPanel.transform.parent.parent
                        : null;
                    var chatBtn = uiRoot != null ? uiRoot.Find("SandboxCanvas/ChatBtn")?.gameObject : null;
                    if (chatBtn != null) chatBtn.SetActive(true);
                }
            }
        }

        private void UpdateTabPanels()
        {
            SetPanelActive(overviewPanel, ActiveTab == GMTab.Overview);
            SetPanelActive(mapPanel, ActiveTab == GMTab.Map);
            SetPanelActive(playerPanel, ActiveTab == GMTab.Player);
            SetPanelActive(worldPanel, ActiveTab == GMTab.World);
            SetPanelActive(assetsPanel, ActiveTab == GMTab.Assets);
            SetPanelActive(logsPanel, ActiveTab == GMTab.Logs);
            SetPanelActive(toolsPanel, ActiveTab == GMTab.Tools);
            SetPanelActive(_equipmentPanel, ActiveTab == GMTab.Equipment);
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }

        private bool IsTypingInInput()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return false;
            var selected = es.currentSelectedGameObject;
            if (selected == null) return false;
            return selected.GetComponent<UnityEngine.UI.InputField>() != null;
        }
    }
}
