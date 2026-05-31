using System;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M1.9 — MonoBehaviour that drives <see cref="RegionStreamingService"/> from a
    /// target (player/camera) world position. Re-evaluation is throttled and only
    /// does load/unload work when the active region changes, giving deterministic
    /// boundary crossing (AC#2). It invokes a configurable region load action and
    /// reports success/failure back to the service so the runtime keeps running
    /// when a region fails (AC#4). The budget cap is enforced by the service (AC#5).
    /// The streaming math lives in the pure service; this wrapper only adapts Unity
    /// frame/transform input to it.
    /// </summary>
    public class RegionStreamController : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Player/camera transform to stream around.")]
        public Transform target;

        [Header("Grid")]
        public int regionCountX = 8;
        public int regionCountY = 8;
        public float regionWidth = 512f;
        public float regionHeight = 1024f;
        public Vector2 worldOrigin = Vector2.zero;

        [Header("Streaming")]
        [Tooltip("Neighbor ring radius (1 = 3x3 around active).")]
        public int ringRadius = 1;
        [Tooltip("Mobile memory budget: max simultaneously loaded regions.")]
        public int maxLoaded = 9;
        [Tooltip("Seconds between streaming re-evaluations.")]
        public float updateInterval = 0.25f;

        /// <summary>The pure streaming logic this controller drives.</summary>
        public RegionStreamingService Service { get; private set; }

        /// <summary>Last plan produced by <see cref="Tick"/> (null if nothing changed).</summary>
        public RegionStreamPlan LastPlan { get; private set; }

        /// <summary>
        /// Region load action. Returns true on success, false on failure (AC#4).
        /// When null, loads are treated as successful. SandboxManager/MapRenderer
        /// can wire a real loader.
        /// </summary>
        public Func<RegionCoord, bool> LoadRegion;

        /// <summary>Optional region unload action invoked for AC#2 unloads.</summary>
        public Action<RegionCoord> UnloadRegion;

        private float _timer;
        private bool _hasLastActive;
        private RegionCoord _lastActive;

        private void Awake()
        {
            BuildService();
        }

        /// <summary>(Re)create the streaming service from the current inspector config.</summary>
        public void BuildService()
        {
            Service = new RegionStreamingService(
                regionCountX, regionCountY,
                regionWidth, regionHeight,
                worldOrigin, ringRadius, maxLoaded);
            _hasLastActive = false;
            _timer = 0f;
        }

        private void Update()
        {
            if (Service == null || target == null) return;
            _timer += Time.deltaTime;
            if (_timer < updateInterval) return;
            _timer = 0f;
            Tick(target.position);
        }

        /// <summary>
        /// AC#1/AC#2/AC#5 — re-evaluate streaming for a world position. Only does
        /// work when the active region changes, so repeated calls within the same
        /// region produce no churn (deterministic boundary crossing). Public so Play
        /// Mode and EditMode tests can drive it directly without the frame loop.
        /// Returns the plan that was applied, or null when nothing changed.
        /// </summary>
        public RegionStreamPlan Tick(Vector2 worldPos)
        {
            if (Service == null) return null;

            var active = Service.WorldToRegion(worldPos);
            if (_hasLastActive && active.Equals(_lastActive) && Service.HasActive)
                return null; // still inside the same region → no boundary crossing

            var plan = Service.Update(worldPos);
            LastPlan = plan;

            if (!plan.activeInBounds)
            {
                // Player left the map: keep current state, no churn (AC#4 continue).
                return plan;
            }

            _hasLastActive = true;
            _lastActive = plan.active;

            // AC#2 — unload regions that left the desired set.
            if (UnloadRegion != null)
                foreach (var c in plan.toUnload)
                    UnloadRegion(c);

            // AC#1/AC#4 — load newly desired regions, reporting failure without aborting.
            foreach (var c in plan.toLoad)
            {
                bool ok = LoadRegion == null || LoadRegion(c);
                if (ok) Service.MarkLoaded(c);
                else Service.MarkFailed(c);
            }

            return plan;
        }
    }
}
