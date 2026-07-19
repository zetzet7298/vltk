using System;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Manual GM capture bridge. Never accepts or persists a golden.</summary>
    public static class GoldenSnapshotCaptureDriver
    {
        public const string SkillFxLayerName = "SkillFx";

        public static GoldenSnapshot CaptureActive(SandboxManager manager, string caseId, int? injectedLayer = null)
        {
            if (manager == null) throw new InvalidOperationException("SandboxManager is required");
            var mapId = manager.MapManager?.ActiveMapId.ToString();
            var faction = manager.PlayerProgression?.faction.ToString();
            return CaptureActive(
                manager.SkillEffectVisual, mapId, caseId, faction,
                Time.frameCount, Mathf.FloorToInt(Time.time * 18f), injectedLayer);
        }

        /// <summary>Injection seam for tests and GM callers with a provisioned layer.</summary>
        public static GoldenSnapshot CaptureActive(
            SkillEffectVisualService effects, string mapId, string caseId, string faction,
            int frame, long tick, int? injectedLayer = null, string layerName = SkillFxLayerName)
        {
            ValidateIdentity(mapId, caseId, faction, frame, tick);
            int layer = ResolveSkillFxLayer(layerName, injectedLayer);
            if (effects == null) throw new InvalidOperationException("SkillEffectVisualService is required");

            ActiveSkillEffect selected = null;
            foreach (var effect in effects.GetActiveEffects())
            {
                if (effect == null || effect.phase == SkillEffectPhase.Finished) continue;
                if (selected != null) throw new InvalidOperationException("Capture requires exactly one active skill effect");
                selected = effect;
            }
            if (selected == null) throw new InvalidOperationException("Capture requires exactly one active skill effect");
            if (selected.skillId <= 0) throw new InvalidOperationException("Active skill effect has no skillId");

            return GoldenSnapshotCaptureService.Capture(
                mapId, caseId, layer, FocusFor(selected), selected.skillId, faction, frame, tick);
        }

        public static int ResolveSkillFxLayer(string layerName = SkillFxLayerName, int? injectedLayer = null)
        {
            if (injectedLayer.HasValue)
            {
                if (injectedLayer.Value < 0 || injectedLayer.Value > 31)
                    throw new ArgumentOutOfRangeException(nameof(injectedLayer));
                return injectedLayer.Value;
            }
            if (string.IsNullOrWhiteSpace(layerName)) throw new ArgumentException("SkillFx layer name is required", nameof(layerName));
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0) throw new InvalidOperationException($"Required layer '{layerName}' is not configured");
            return layer;
        }

        public static Vector2 FocusFor(ActiveSkillEffect effect)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));
            if (effect.phase == SkillEffectPhase.Missile)
            {
                // Match SkillEffectWorldOverlay missile placement exactly.
                Vector2 focus = effect.currentMissilePos;
                if (effect.HasPcImpactSprite && effect.missilePositions != null && effect.missilePositions.Length > 0)
                    focus = effect.missilePositions[0];
                return focus.sqrMagnitude < 0.01f ? effect.casterPos : focus;
            }
            return effect.phase == SkillEffectPhase.Impact ? effect.targetPos : effect.casterPos;
        }

        private static void ValidateIdentity(string mapId, string caseId, string faction, int frame, long tick)
        {
            if (string.IsNullOrWhiteSpace(mapId)) throw new ArgumentException("mapId is required", nameof(mapId));
            if (string.IsNullOrWhiteSpace(caseId)) throw new ArgumentException("caseId is required", nameof(caseId));
            if (string.IsNullOrWhiteSpace(faction) || faction == "None") throw new ArgumentException("faction is required", nameof(faction));
            if (frame < 0) throw new ArgumentOutOfRangeException(nameof(frame));
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        }
    }
}
