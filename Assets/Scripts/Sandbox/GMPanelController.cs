using UnityEngine;
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
    }

    public class GMPanelController : MonoBehaviour
    {
        [Header("Panel")]
        public GameObject panelRoot;

        private UnityEngine.UI.Image _backgroundImage;
        private GameObject _gmPanelGameObject;

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

        public GMTab ActiveTab { get; private set; } = GMTab.Overview;
        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private const string TOGGLE_KEY = "q";

        private void Awake()
        {
            _backgroundImage = GetComponent<UnityEngine.UI.Image>();
            _gmPanelGameObject = gameObject;
            // Start with entire GMPanel hidden
            _gmPanelGameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsTypingInInput() && Input.GetKeyDown(TOGGLE_KEY))
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
            if (tabIndex >= 0 && tabIndex < 7)
            {
                ActiveTab = (GMTab)tabIndex;
                UpdateTabPanels();
                if (tabBar != null) tabBar.RefreshColors(tabIndex);
                SubsystemLog.Info("GM", $"Switched to tab: {ActiveTab}");
            }
        }

        private void SetOpen(bool open)
        {
            if (_gmPanelGameObject == null) return;
            _gmPanelGameObject.SetActive(open);
            if (open)
            {
                if (panelRoot != null) panelRoot.SetActive(true);
                if (_backgroundImage != null) _backgroundImage.enabled = true;
                UpdateTabPanels();
                SubsystemLog.Info("GM", "Panel opened");
            }
            else
            {
                SubsystemLog.Info("GM", "Panel closed");
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
