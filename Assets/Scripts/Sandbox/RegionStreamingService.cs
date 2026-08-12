using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>M1.9 — lifecycle state of a streamed region.</summary>
    public enum RegionStreamState
    {
        Unloaded,
        Loading,
        Loaded,
        Failed,
    }

    /// <summary>Integer region grid coordinate.</summary>
    public struct RegionCoord : IEquatable<RegionCoord>
    {
        public int x;
        public int y;

        public RegionCoord(int x, int y) { this.x = x; this.y = y; }

        public bool Equals(RegionCoord o) => x == o.x && y == o.y;
        public override bool Equals(object o) => o is RegionCoord c && Equals(c);
        public override int GetHashCode() => (x * 397) ^ y;
        public override string ToString() => $"({x},{y})";
    }

    /// <summary>Deterministic load/unload plan returned by a streaming update.</summary>
    public class RegionStreamPlan
    {
        public RegionCoord active;
        public bool activeInBounds;
        public List<RegionCoord> toLoad = new();
        public List<RegionCoord> toUnload = new();
    }

    /// <summary>
    /// M1.9 — Computes which map regions should be loaded around the player/camera.
    /// Pure logic (no MonoBehaviour) so it is fully unit-testable. Loads the active
    /// region plus a configurable neighbor ring, unloads regions that leave the set,
    /// respects a max-loaded budget, and exposes per-region state for a GM overlay.
    /// </summary>
    public class RegionStreamingService
    {
        private readonly int _countX;
        private readonly int _countY;
        private readonly float _regionW;
        private readonly float _regionH;
        private readonly Vector2 _worldOrigin;
        private readonly int _ringRadius;
        private readonly int _maxLoaded;

        // Only tracks regions that are not Unloaded (sparse).
        private readonly Dictionary<RegionCoord, RegionStreamState> _states = new();
        private RegionCoord _active;
        private bool _hasActive;
        private IRegionStreamingHost _host;
        private int _playerId = 0;

        public event Action<RegionStreamPlan> OnStreamingPlan;

        /// <param name="ringRadius">Neighbor ring radius (1 = 3x3 around active).</param>
        /// <param name="maxLoaded">Mobile memory budget: max simultaneously loaded regions.</param>
        public RegionStreamingService(
            int countX, int countY,
            float regionWidth, float regionHeight,
            Vector2 worldOrigin,
            int ringRadius = 1,
            int maxLoaded = 9) : this(countX, countY, regionWidth, regionHeight, worldOrigin, ringRadius, maxLoaded, null) { }

        public RegionStreamingService(
            int countX, int countY,
            float regionWidth, float regionHeight,
            Vector2 worldOrigin,
            int ringRadius,
            int maxLoaded,
            IRegionStreamingHost host)
        {
            _countX = Mathf.Max(0, countX);
            _countY = Mathf.Max(0, countY);
            _regionW = regionWidth > 0 ? regionWidth : 1f;
            _regionH = regionHeight > 0 ? regionHeight : 1f;
            _worldOrigin = worldOrigin;
            _ringRadius = Mathf.Max(0, ringRadius);
            _maxLoaded = Mathf.Max(1, maxLoaded);
            _host = host;
        }

        public void AttachHost(IRegionStreamingHost host) { _host = host; }

        public IReadOnlyDictionary<RegionCoord, RegionStreamState> States => _states;
        public RegionCoord ActiveRegion => _active;
        public bool HasActive => _hasActive;
        public int MaxLoaded => _maxLoaded;
        public int PlayerId { get => _playerId; set => _playerId = value; }

        public bool InBounds(RegionCoord c)
            => c.x >= 0 && c.x < _countX && c.y >= 0 && c.y < _countY;

        /// <summary>World position → region grid coordinate (may be out of bounds).</summary>
        public RegionCoord WorldToRegion(Vector2 worldPos)
        {
            int rx = Mathf.FloorToInt((worldPos.x - _worldOrigin.x) / _regionW);
            int ry = Mathf.FloorToInt((worldPos.y - _worldOrigin.y) / _regionH);
            return new RegionCoord(rx, ry);
        }

        /// <summary>
        /// AC#1/AC#5 — desired loaded set: active + neighbor ring, clamped to map
        /// bounds, sorted by Manhattan distance (then y, then x) and capped at the
        /// memory budget so the nearest regions win deterministically.
        /// </summary>
        public List<RegionCoord> ComputeDesired(RegionCoord active)
        {
            var desired = new List<RegionCoord>();
            if (!InBounds(active)) return desired;

            for (int dy = -_ringRadius; dy <= _ringRadius; dy++)
            {
                for (int dx = -_ringRadius; dx <= _ringRadius; dx++)
                {
                    var c = new RegionCoord(active.x + dx, active.y + dy);
                    if (InBounds(c)) desired.Add(c);
                }
            }

            desired.Sort((a, b) =>
            {
                int da = Mathf.Abs(a.x - active.x) + Mathf.Abs(a.y - active.y);
                int db = Mathf.Abs(b.x - active.x) + Mathf.Abs(b.y - active.y);
                if (da != db) return da.CompareTo(db);
                if (a.y != b.y) return a.y.CompareTo(b.y);
                return a.x.CompareTo(b.x);
            });

            if (desired.Count > _maxLoaded)
                desired.RemoveRange(_maxLoaded, desired.Count - _maxLoaded);
            return desired;
        }

        /// <summary>
        /// AC#1/AC#2/AC#5 — recompute the streaming set for a player/camera world
        /// position and return a deterministic load/unload plan. Newly desired
        /// regions become Loading; regions leaving the set are unloaded.
        /// </summary>
        public RegionStreamPlan Update(Vector2 worldPos)
        {
            var plan = new RegionStreamPlan();
            var active = WorldToRegion(worldPos);
            plan.active = active;
            plan.activeInBounds = InBounds(active);

            if (!plan.activeInBounds)
            {
                OnStreamingPlan?.Invoke(plan);
                return plan;
            }

            _active = active;
            _hasActive = true;

            var desired = ComputeDesired(active);
            var desiredSet = new HashSet<RegionCoord>(desired);

            // Unload regions no longer desired (deterministic order).
            var loadedNow = new List<RegionCoord>(_states.Keys);
            loadedNow.Sort((a, b) =>
            {
                if (a.y != b.y) return a.y.CompareTo(b.y);
                return a.x.CompareTo(b.x);
            });
            foreach (var c in loadedNow)
            {
                if (!desiredSet.Contains(c))
                {
                    _states.Remove(c);
                    plan.toUnload.Add(c);
                    if (_host != null)
                    {
                        _host.OnRegionUnloaded(c, active.x, active.y);
                        _host.LogRegionEvent(c, $"Unload region {c} (active={active})");
                    }
                }
            }

            // Load regions newly entering the set.
            foreach (var c in desired)
            {
                if (!_states.TryGetValue(c, out var st) || st == RegionStreamState.Unloaded)
                {
                    _states[c] = RegionStreamState.Loading;
                    plan.toLoad.Add(c);
                    if (_host != null)
                    {
                        _host.OnRegionLoadStarted(c, active.x, active.y);
                        _host.LogRegionEvent(c, $"Load region {c} (active={active})");
                    }
                }
            }

            OnStreamingPlan?.Invoke(plan);
            if (_host != null)
            {
                _host.UpdateRegionOverlay(active, LoadedCount, _maxLoaded);
                _host.SaveRegionState(active, GetState(active), LoadedCount);
            }
            return plan;
        }

        /// <summary>AC#1/AC#2 — caller reports a region finished loading.</summary>
        public void MarkLoaded(RegionCoord c)
        {
            if (_states.ContainsKey(c)) _states[c] = RegionStreamState.Loaded;
            if (_host != null)
            {
                _host.OnRegionLoaded(c, 0);
                _host.PlayRegionLoadSFX(c);
                _host.LogRegionEvent(c, $"Region {c} load complete");
            }
        }

        /// <summary>AC#4 — caller reports a region failed to load; runtime continues.</summary>
        public void MarkFailed(RegionCoord c)
        {
            _states[c] = RegionStreamState.Failed;
            SubsystemLog.Error("RegionStreaming", $"Region {c} failed to load");
            _host?.OnRegionLoadFailed(c, "Region failed to load");
            _host?.LogRegionEvent(c, $"Region {c} failed to load");
            _host?.SaveRegionState(c, RegionStreamState.Failed, LoadedCount);
        }

        public RegionStreamState GetState(RegionCoord c)
            => _states.TryGetValue(c, out var st) ? st : RegionStreamState.Unloaded;

        public int LoadedCount
        {
            get
            {
                int n = 0;
                foreach (var kv in _states)
                    if (kv.Value == RegionStreamState.Loaded || kv.Value == RegionStreamState.Loading)
                        n++;
                return n;
            }
        }

        /// <summary>AC#3 — color code for the GM region overlay.</summary>
        public Color GetStateColor(RegionCoord c)
        {
            switch (GetState(c))
            {
                case RegionStreamState.Loaded:  return Color.green;
                case RegionStreamState.Loading: return Color.yellow;
                case RegionStreamState.Failed:  return Color.red;
                default:                        return Color.gray;
            }
        }
    }
}
