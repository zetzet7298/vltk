// -----------------------------------------------------------------------------
// VLTK Mobile — Sandbox Runtime State Provider
// Bridges sandbox systems (GameplayLoop, MapManager) to HUD.
// -----------------------------------------------------------------------------

using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
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
        private GameplayLoopService Loop => SandboxManager.Instance?.GameplayLoop;

        public bool HasActiveMap => Player != null && MapManager?.ActiveMap != null;
        public int ActiveMapId => MapManager?.ActiveMapId ?? 0;
        public string ActiveMapName => MapManager?.ActiveMap?.catalogEntry?.displayNameRaw
            ?? MapManager?.ActiveMap?.catalogEntry?.displayNameNormalized
            ?? "Sandbox";
        public MapDefinition ActiveMapDefinition => MapManager?.ActiveMap;

        public Vector2 PlayerWorldPosition => Player != null
            ? (Vector2)Player.transform.position
            : Vector2.zero;

        // ── Gameplay Loop Data ─────────────────────────────────────────────

        public int PlayerLevel => Loop?.Player?.level ?? Progression?.level ?? 1;

        public int PlayerCurrentLife => Loop?.Player?.combat?.currentLife ?? 100;
        public int PlayerMaxLife => Loop?.Player?.combat?.maxLife ?? 100;
        public int PlayerCurrentMana => Loop?.Player?.combat?.currentMana ?? 100;
        public int PlayerSilver => Loop?.Economy?.Wallet?.silver ?? 0;
        public long PlayerExp => Loop?.LevelService?.CurrentExp ?? 0;

        public string StatusSummary => Loop?.GetStatusSummary() ?? "Chưa sẵn sàng";
        public int TotalEnemies => Loop?.Enemies?.Count ?? 0;
        public int PlayerKarma => Loop?.PkRules?.Karma ?? 0;
        public bool IsRedName => Loop?.PkRules?.IsRedName ?? false;
    }
}
