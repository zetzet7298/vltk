// -----------------------------------------------------------------------------
// VLTK Mobile — Sandbox Runtime State Provider
// Bridges sandbox systems (MapManager, SandboxPlayerController) to HUD via
// IRuntimeStateProvider. Attach to any GameObject in the Sandbox scene.
// -----------------------------------------------------------------------------

using UnityEngine;
using VLTK.Model;

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

        private SandboxPlayerController Player => _player != null ? _player : (_player = FindObjectOfType<SandboxPlayerController>());
        private MapManager MapManager => SandboxManager.Instance != null ? SandboxManager.Instance.MapManager : null;
        private PlayerProgressionState Progression => SandboxManager.Instance != null ? SandboxManager.Instance.PlayerProgression : null;

        public bool HasActiveMap => Player != null && MapManager?.ActiveMap != null;
        public int ActiveMapId => MapManager?.ActiveMapId ?? 0;
        public string ActiveMapName => MapManager?.ActiveMap?.catalogEntry?.displayNameRaw
            ?? MapManager?.ActiveMap?.catalogEntry?.displayNameNormalized
            ?? "Sandbox";
        public MapDefinition ActiveMapDefinition => MapManager?.ActiveMap;

        public Vector2 PlayerWorldPosition => Player != null
            ? (Vector2)Player.transform.position
            : Vector2.zero;

        public int PlayerLevel => Progression?.level ?? 1;
        public int PlayerCurrentLife => 100;
        public int PlayerMaxLife => 100;
    }
}
