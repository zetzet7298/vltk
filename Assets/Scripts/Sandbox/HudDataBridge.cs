using System;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Runtime data the production HUD is allowed to read. This is a stable runtime
    /// contract — the HUD consumes it instead of reaching into conversion internals
    /// (parsers, importers, contract bundles). M6.4 AC#1.
    /// </summary>
    public interface IRuntimeStateProvider
    {
        bool HasActiveMap { get; }
        int ActiveMapId { get; }
        string ActiveMapName { get; }
        MapDefinition ActiveMapDefinition { get; }
        Vector2 PlayerWorldPosition { get; }
        int PlayerLevel { get; }
        int PlayerCurrentLife { get; }
        int PlayerMaxLife { get; }
        int PlayerCurrentMana { get; }
        int PlayerMaxMana { get; }       // vltkunity MaxInner (replaces hardcoded 100)
        int PlayerCurrentStamina { get; } // vltkunity CurStamina
        int PlayerMaxStamina { get; }     // vltkunity MaxStamina
        long PlayerExp { get; }
        long PlayerMaxExp { get; }        // real EXP denominator (fixes ComputeExpFraction fudge)

        // Minimap projection (recon §1a / M1). vltkunity miniMapHandle.xRatio/yRatio.
        // Per-map offset used to project player world coords onto the minimap.
        float MiniMapXRatio { get; }
        float MiniMapYRatio { get; }

        // Currency (recon §3). vltkunity Money.prefab has no source binding;
        // these read from the runtime economy wallet. Vietnamese: Đồng/Vàng/Bạc.
        int PlayerCopper { get; }   // tongqian
        int PlayerGold { get; }     // jinbi
        int PlayerSilver { get; }   // yinliang
    }

    /// <summary>Snapshot the HUD renders (M6.4 AC#1).</summary>
    public struct HudSnapshot
    {
        public bool valid;
        public int mapId;
        public string mapName;
        public MapDefinition activeMap;
        public Vector2 playerPosition;
        public int level;
        public int currentLife;
        public int maxLife;
        public float lifeFraction;
        public int currentMana;
        public int maxMana;
        public float manaFraction;
        public int currentStamina;
        public int maxStamina;
        public float staminaFraction;
        public long currentExp;
        public long maxExp;
        public float expFraction;
        // Minimap projection (recon §1a / M1).
        public float miniMapXRatio;
        public float miniMapYRatio;
        // Currency (recon §3).
        public int copper;
        public int gold;
        public int silver;
    }

    /// <summary>
    /// M6.4 — Bridges the production HUD to sandbox-proven runtime systems. Pure C#
    /// (no MonoBehaviour) so it is fully EditMode-testable. The HUD reads a
    /// <see cref="HudSnapshot"/> from the runtime state provider (AC#1) rather than
    /// from conversion internals, GM remains openable in development builds (AC#2),
    /// and GM/debug surfaces are hidden in release builds (AC#3). Keeping debug and
    /// production on the same runtime contract prevents divergence.
    /// </summary>
    public class HudDataBridge
    {
        private readonly IRuntimeStateProvider _runtime;

        /// <summary>Whether this is a development build (drives GM availability).</summary>
        public bool IsDevelopmentBuild { get; set; }

        /// <summary>
        /// Raised when <see cref="BuildSnapshot"/> produces a snapshot that differs
        /// from the previous one in any field the HUD cares about. vltkunity port
        /// adapters subscribe here instead of polling inside Update(); controllers
        /// should call <see cref="RefreshAndPublish"/> from their normal update tick.
        /// </summary>
        public event Action<HudSnapshot> SnapshotChanged;

        public HudDataBridge(IRuntimeStateProvider runtime, bool isDevelopmentBuild = false)
        {
            _runtime = runtime;
            IsDevelopmentBuild = isDevelopmentBuild;
        }

        /// <summary>AC#1 — build the HUD snapshot from runtime systems only.</summary>
        public HudSnapshot BuildSnapshot()
        {
            if (_runtime == null || !_runtime.HasActiveMap)
                return new HudSnapshot { valid = false };

            int maxLife = Math.Max(1, _runtime.PlayerMaxLife);
            int curLife = Mathf.Clamp(_runtime.PlayerCurrentLife, 0, maxLife);
            int maxMana = Math.Max(1, _runtime.PlayerMaxMana);
            int curMana = Mathf.Clamp(_runtime.PlayerCurrentMana, 0, maxMana);
            int maxStamina = Math.Max(1, _runtime.PlayerMaxStamina);
            int curStamina = Mathf.Clamp(_runtime.PlayerCurrentStamina, 0, maxStamina);
            // When the runtime max stamina is invalid (<=0), render an empty bar
            // (0) rather than a full one — clamping current to the guarded max
            // of 1 would otherwise yield a misleading 100% full bar (recon §2a).
            float staminaFraction = _runtime.PlayerMaxStamina <= 0
                ? 0f
                : (float)curStamina / maxStamina;
            long maxExp = Math.Max(1L, _runtime.PlayerMaxExp);
            long curExp = Math.Min(Math.Max(0L, _runtime.PlayerExp), maxExp);
            return new HudSnapshot
            {
                valid = true,
                mapId = _runtime.ActiveMapId,
                mapName = _runtime.ActiveMapName,
                activeMap = _runtime.ActiveMapDefinition,
                playerPosition = _runtime.PlayerWorldPosition,
                level = _runtime.PlayerLevel,
                currentLife = curLife,
                maxLife = maxLife,
                lifeFraction = (float)curLife / maxLife,
                currentMana = curMana,
                maxMana = maxMana,
                manaFraction = (float)curMana / maxMana,
                currentStamina = curStamina,
                maxStamina = maxStamina,
                staminaFraction = staminaFraction,
                currentExp = curExp,
                maxExp = maxExp,
                expFraction = Mathf.Clamp01((float)curExp / maxExp),
                miniMapXRatio = _runtime.MiniMapXRatio,
                miniMapYRatio = _runtime.MiniMapYRatio,
                copper = _runtime.PlayerCopper,
                gold = _runtime.PlayerGold,
                silver = _runtime.PlayerSilver,
            };
        }

        /// <summary>AC#2 — GM Panel can be opened in development builds.</summary>
        public bool CanOpenGmPanel() => IsDevelopmentBuild;

        /// <summary>
        /// AC#3 — release builds must not expose debug controls. Returns true only
        /// when GM is safe to surface (development build).
        /// </summary>
        public bool DebugControlsAllowed() => IsDevelopmentBuild;

        /// <summary>
        /// AC#3 — guard a debug action; logs and refuses when running in a release
        /// build so debug controls are never exposed unintentionally.
        /// </summary>
        public bool TryRunDebugAction(string actionName, Action action)
        {
            if (!IsDevelopmentBuild)
            {
                SubsystemLog.Warn("HUD", $"Debug action '{actionName}' blocked in release build");
                return false;
            }
            action?.Invoke();
            return true;
        }

        private HudSnapshot _lastSnapshot;
        private bool _hasLastSnapshot;

        /// <summary>
        /// Builds a fresh snapshot and raises <see cref="SnapshotChanged"/> when it
        /// differs from the previous one. Controllers call this once per update tick
        /// (typically inside their MonoBehaviour.Update). Returns true when the
        /// snapshot changed and a notification was dispatched.
        /// </summary>
        public bool RefreshAndPublish()
        {
            var next = BuildSnapshot();
            bool changed = !_hasLastSnapshot || !SnapshotsEqual(_lastSnapshot, next);
            _lastSnapshot = next;
            _hasLastSnapshot = true;
            if (changed)
                SnapshotChanged?.Invoke(next);
            return changed;
        }

        private static bool SnapshotsEqual(HudSnapshot a, HudSnapshot b)
        {
            if (a.valid != b.valid) return false;
            if (!a.valid) return true;
            return a.mapId == b.mapId
                && a.level == b.level
                && a.currentLife == b.currentLife
                && a.maxLife == b.maxLife
                && a.currentMana == b.currentMana
                && a.maxMana == b.maxMana
                && a.currentStamina == b.currentStamina
                && a.maxStamina == b.maxStamina
                && a.currentExp == b.currentExp
                && a.maxExp == b.maxExp
                && a.playerPosition == b.playerPosition
                && a.mapName == b.mapName
                && a.miniMapXRatio == b.miniMapXRatio
                && a.miniMapYRatio == b.miniMapYRatio
                && a.copper == b.copper
                && a.gold == b.gold
                && a.silver == b.silver;
        }
    }
}
