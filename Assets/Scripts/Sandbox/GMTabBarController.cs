using UnityEngine;
using UnityEngine.UI;

namespace VLTK.Sandbox
{
    public class GMTabBarController : MonoBehaviour
    {
        [System.Serializable]
        public class TabEntry
        {
            public string label;
            public Button button;
            public int index;
        }

        public TabEntry[] tabs;
        public Color activeColor = new Color(0.3f, 0.6f, 1f, 1f);
        public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private GMPanelController _panel;

        public void Initialize(GMPanelController panel)
        {
            _panel = panel;
            foreach (var tab in tabs)
            {
                if (tab.button != null)
                {
                    int idx = tab.index;
                    tab.button.onClick.AddListener(() =>
                    {
                        _panel.SwitchTab(idx);
                        RefreshColors(idx);
                    });
                }
            }
        }

        public void RefreshColors(int activeIndex)
        {
            foreach (var tab in tabs)
            {
                if (tab.button != null)
                {
                    var colors = tab.button.colors;
                    colors.normalColor = tab.index == activeIndex ? activeColor : inactiveColor;
                    colors.selectedColor = activeColor;
                    tab.button.colors = colors;
                }
            }
        }
    }
}
