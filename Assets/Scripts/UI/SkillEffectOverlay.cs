using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>
    /// IMGUI overlay that renders active combat skill effects.
    /// Attached to the same HUD GameObject as PcHudVietnameseTextOverlay.
    /// Draws PreCast, missile, and impact visuals matching PC JXWin style.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillEffectOverlay : MonoBehaviour
    {
        private SkillEffectRenderer _renderer;

        private void EnsureRenderer()
        {
            if (_renderer != null) return;

            var manager = SandboxManager.Instance;
            if (manager == null) return;

            // Camera.main returns null when tag != "MainCamera"; find by component.
            var cam = Camera.main;
            if (cam == null)
            {
                foreach (var c in FindObjectsOfType<Camera>())
                    if (c.orthographic && c.enabled) { cam = c; break; }
            }
            if (cam == null) return;

            _renderer = new SkillEffectRenderer(manager.SkillEffectVisual, cam);
        }

        private void OnGUI()
        {
            EnsureRenderer();
            _renderer?.Render();
        }
    }
}
