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
        public string ActiveMapName
        {
            get
            {
                int mapId = ActiveMapId;
                if (MapPortManifest.TryGet(mapId, out var portEntry))
                    return portEntry.nameVi;
                return MapManager?.ActiveMap?.catalogEntry?.displayNameRaw
                    ?? MapManager?.ActiveMap?.catalogEntry?.displayNameNormalized
                    ?? "Sandbox";
            }
        }
        public MapDefinition ActiveMapDefinition => MapManager?.ActiveMap;

        public Vector2 PlayerWorldPosition => Player != null
            ? (Vector2)Player.transform.position
            : Vector2.zero;

        // ── Gameplay Loop Data ─────────────────────────────────────────────

        public int PlayerLevel => Loop?.Player?.level ?? Progression?.level ?? 1;

        public int PlayerCurrentLife => Loop?.Player?.combat?.currentLife ?? 100;
        public int PlayerMaxLife => Loop?.Player?.combat?.maxLife ?? 100;
        public int PlayerCurrentMana => Loop?.Player?.combat?.currentMana ?? 100;
        public int PlayerMaxMana => Loop?.Player?.combat?.maxMana ?? 100;
        // CombatActorState does not yet track stamina; default to the MountService
        // cap (100) so the stamina bar renders correctly until a stamina field is
        // added to CombatActorState.
        public int PlayerCurrentStamina => MountService.MaxStamina;
        public int PlayerMaxStamina => MountService.MaxStamina;
        public long PlayerExp => Loop?.LevelService?.CurrentExp ?? 0;
        // Real EXP denominator for the current level (PC PlayerStatService table).
        // Fixes the previous ComputeExpFraction fudge (currentExp/(currentExp+1)).
        public long PlayerMaxExp
        {
            get
            {
                int level = PlayerLevel;
                return PlayerStatService.GetExpRequired(level);
            }
        }

        // Minimap projection (recon §1a / M1). Defaults to 0 (no offset) until
        // a runtime minimap handle supplies per-map xRatio/yRatio.
        public float MiniMapXRatio => 0f;
        public float MiniMapYRatio => 0f;

        // Currency (recon §3). Vietnamese: Đồng tiền / Vàng / Bạc.
        // Wallet tracks silver + gold; copper is not yet tracked (default 0).
        public int PlayerCopper => 0;
        public int PlayerGold => Loop?.Economy?.Wallet?.gold ?? 0;
        public int PlayerSilver => Loop?.Economy?.Wallet?.silver ?? 0;

        public string StatusSummary => Loop?.GetStatusSummary() ?? "Chưa sẵn sàng";
        public int TotalEnemies => Loop?.Enemies?.Count ?? 0;
        public int PlayerKarma => Loop?.PkRules?.Karma ?? 0;
        public bool IsRedName => Loop?.PkRules?.IsRedName ?? false;
    }
}
