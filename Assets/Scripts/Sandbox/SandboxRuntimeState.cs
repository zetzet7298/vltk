// -----------------------------------------------------------------------------
// VLTK Mobile — Sandbox Runtime State Provider
// Bridges sandbox systems (MapManager, SandboxPlayerController) to HUD via
// IRuntimeStateProvider. Attach to any GameObject in the Sandbox scene.
// -----------------------------------------------------------------------------

using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Sandbox implementation of <see cref="IRuntimeStateProvider"/>.
    /// Reads live state from <see cref="SandboxPlayerController"/> and
    /// scene systems so the HUD can render.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SandboxRuntimeState : MonoBehaviour, IRuntimeStateProvider
    {
        private SandboxPlayerController _player;

        private void Awake()
        {
            _player = FindObjectOfType<SandboxPlayerController>();
        }

        public bool HasActiveMap => _player != null;
        public int ActiveMapId => 0;
        public string ActiveMapName => "Sandbox";

        public Vector2 PlayerWorldPosition => _player != null
            ? (Vector2)_player.transform.position
            : Vector2.zero;

        public int PlayerLevel => 1;
        public int PlayerCurrentLife => 100;
        public int PlayerMaxLife => 100;
    }
}
