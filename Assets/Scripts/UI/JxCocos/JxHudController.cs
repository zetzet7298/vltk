// -----------------------------------------------------------------------------
// VLTK Mobile — JX HUD controller (jx-cocos port wiring)
//
// Purpose: connect the ported pure-state/adapters (Assets/Scripts/UI/JxCocos)
// into the LIVE scene so they actually render. Slice A wires the top status
// bar (JxTopStatusBarAdapter + JxTopStatusBarState), fed by the same
// SandboxRuntimeState (IRuntimeStateProvider) the legacy HUD reads.
//
// This controller is intentionally isolated: it owns its own UIDocument and
// does not touch GameHudController, so the legacy HUD stays untouched while
// the jx-cocos HUD is verified slice-by-slice in play mode.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Sandbox;

namespace VLTK.UI.JxCocos
{
    /// <summary>
    /// Renders the jx-cocos HUD from the runtime snapshot. Requires a UIDocument
    /// whose VisualTreeAsset declares the jx_* elements the adapters query.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class JxHudController : MonoBehaviour
    {
        private UIDocument _document;
        private VisualElement _root;

        private JxTopStatusBarState _topState;
        private JxTopStatusBarAdapter _topAdapter;

        private HudDataBridge _bridge;
        private IRuntimeStateProvider _provider;
        private float _rescanTimer;

        private const float RescanInterval = 0.5f;

        private void OnEnable()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnDisable()
        {
            _root = null;
        }

        private void Update()
        {
            EnsureProvider();
            EnsureBound();
            if (_topAdapter == null) return;

            // PanelSettings uses ScaleWithScreenSize: it scales content but does
            // NOT size the rootVisualElement container, which otherwise collapses
            // to height 0 and hides everything. Size it each frame (cheap) the same
            // way the legacy HUD does, so the jx-cocos overlay fills the screen.
            SizeRootToScreen();

            FeedTopBar();
            _topAdapter.Render();
        }

        private void SizeRootToScreen()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            const float referenceWidth = 1280f;
            const float referenceHeight = 720f;
            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);
            float scale = Mathf.Min(screenWidth / referenceWidth, screenHeight / referenceHeight);
            float w = screenWidth / scale;
            float h = screenHeight / scale;
            var container = _document.rootVisualElement;
            // Anchor the panel container to the top-left and size it to the full
            // scaled screen. Without position:absolute + top/left 0, the
            // ScaleWithScreenSize panel centers/shrinks the rootVisualElement and
            // offsets the whole tree vertically (observed y=360 offset on 720 tall).
            container.style.position = Position.Absolute;
            container.style.left = 0;
            container.style.top = 0;
            container.style.right = new StyleLength(StyleKeyword.Auto);
            container.style.bottom = new StyleLength(StyleKeyword.Auto);
            container.style.width = w;
            container.style.height = h;
            // The first child is our JxHudRoot — stretch it to the sized container.
            if (container.childCount > 0)
            {
                var jxRoot = container[0];
                jxRoot.style.position = Position.Absolute;
                jxRoot.style.left = 0;
                jxRoot.style.top = 0;
                jxRoot.style.right = new StyleLength(StyleKeyword.Auto);
                jxRoot.style.bottom = new StyleLength(StyleKeyword.Auto);
                jxRoot.style.width = w;
                jxRoot.style.height = h;
            }
        }

        private void EnsureProvider()
        {
            if (_provider != null) return;
            _rescanTimer -= Time.unscaledDeltaTime;
            if (_rescanTimer > 0f) return;
            _rescanTimer = RescanInterval;

            // Reuse the same runtime provider the legacy HUD uses.
            var runtime = FindObjectOfType<SandboxRuntimeState>();
            if (runtime != null)
            {
                _provider = runtime;
                _bridge = new HudDataBridge(_provider, Debug.isDebugBuild);
            }
        }

        private void EnsureBound()
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document == null) return;
            if (_root == null)
            {
                _root = _document.rootVisualElement;
                if (_root == null) return;
                // Never let the overlay steal touches (game world / joystick stay interactive).
                _root.pickingMode = PickingMode.Ignore;
            }

            if (_topAdapter == null)
            {
                _topState = new JxTopStatusBarState();
                _topAdapter = new JxTopStatusBarAdapter(_root, _topState);
                _topAdapter.Bind();
                // UI Toolkit text renders blank when labels fall back to the project's
                // broken default runtime font (no glyphs). The GameHud.uss stylesheet
                // (loaded from Resources) sets the authoritative HudDefaultFont on
                // labels via a global rule, so attaching it fixes all JxHud text too —
                // same proven pattern GameHudController uses.
                EnsureStyleSheetLoaded(_root);
                EnsureFont(_root);
            }
        }

        private static UnityEngine.UIElements.StyleSheet _hudStyleSheet;
        private static void EnsureStyleSheetLoaded(VisualElement root)
        {
            if (root == null) return;
            if (_hudStyleSheet == null)
                _hudStyleSheet = Resources.Load<UnityEngine.UIElements.StyleSheet>("GameHud");
            if (_hudStyleSheet == null) return;
            if (!root.styleSheets.Contains(_hudStyleSheet))
                root.styleSheets.Add(_hudStyleSheet);
        }

        private static UnityEngine.TextCore.Text.FontAsset _uiFont;
        private static void EnsureFont(VisualElement root)
        {
            if (root == null) return;
            if (_uiFont == null)
                _uiFont = Resources.Load<UnityEngine.TextCore.Text.FontAsset>("HudDefaultFont");
            if (_uiFont == null) return;
            var stack = new System.Collections.Generic.Stack<VisualElement>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var el = stack.Pop();
                if (el is Label lbl)
                    lbl.style.unityFontDefinition = new StyleFontDefinition(_uiFont);
                int n = el.childCount;
                for (int i = 0; i < n; i++) stack.Push(el[i]);
            }
        }

        private void FeedTopBar()
        {
            if (_topState == null) return;

            // The top status bar shows ROLE stats (level/HP/MP/stamina/EXP) which
            // exist as soon as the player character exists — they do NOT depend on a
            // loaded map. The HudDataBridge snapshot is gated on HasActiveMap (it also
            // carries map/minimap data), so reading it here would blank the bars before
            // a map loads. Read the provider fields directly so the role bar is always
            // accurate, matching jx-cocos KuiTopControlVN (renders role info on any
            // scene as long as the character exists).
            if (_provider != null)
            {
                _topState.UpRoleInfo(_provider.PlayerCurrentLife, _provider.PlayerMaxLife, JxTopStatusBarState.Kind.Hp);
                _topState.UpRoleInfo(_provider.PlayerCurrentMana, _provider.PlayerMaxMana, JxTopStatusBarState.Kind.Mana);
                _topState.UpRoleInfo(_provider.PlayerCurrentStamina, _provider.PlayerMaxStamina, JxTopStatusBarState.Kind.Stamina);
                _topState.UpRoleInfo(_provider.PlayerExp, _provider.PlayerMaxExp, JxTopStatusBarState.Kind.Exp);
                _topState.UpRoleInfo(_provider.PlayerLevel, 0, JxTopStatusBarState.Kind.Level);
                _topState.SetGender(false);
                return;
            }

            // No provider at all: show a clearly "no data" default so it is obvious
            // the wiring works even before the runtime state is ready.
            _topState.UpRoleInfo(0, 100, JxTopStatusBarState.Kind.Hp);
            _topState.UpRoleInfo(0, 100, JxTopStatusBarState.Kind.Mana);
            _topState.UpRoleInfo(0, 100, JxTopStatusBarState.Kind.Stamina);
            _topState.UpRoleInfo(0, 100, JxTopStatusBarState.Kind.Exp);
            _topState.UpRoleInfo(0, 0, JxTopStatusBarState.Kind.Level);
        }
    }
}
