using System;
using UnityEngine;
using VLTK.Core;

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
        Vector2 PlayerWorldPosition { get; }
        int PlayerLevel { get; }
        int PlayerCurrentLife { get; }
        int PlayerMaxLife { get; }
    }

    /// <summary>Snapshot the HUD renders (M6.4 AC#1).</summary>
    public struct HudSnapshot
    {
        public bool valid;
        public int mapId;
        public string mapName;
        public Vector2 playerPosition;
        public int level;
        public int currentLife;
        public int maxLife;
        public float lifeFraction;
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
            return new HudSnapshot
            {
                valid = true,
                mapId = _runtime.ActiveMapId,
                mapName = _runtime.ActiveMapName,
                playerPosition = _runtime.PlayerWorldPosition,
                level = _runtime.PlayerLevel,
                currentLife = curLife,
                maxLife = maxLife,
                lifeFraction = (float)curLife / maxLife,
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
    }
}
