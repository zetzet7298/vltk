using System.Collections.Generic;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>
    /// Renders active skill combat effects on screen using IMGUI overlay.
    /// Draws PreCast animation, missile projectiles, and impact effects
    /// for each active skill cast from the SkillEffectVisualService.
    /// Visual style matches PC JXWin: colored sprites with trails.
    /// </summary>
    public class SkillEffectRenderer
    {
        private readonly SkillEffectVisualService _service;
        private readonly Camera _camera;

        /// <summary>World units per screen pixel for consistent effect sizing.</summary>
        public float WorldToScreenScale { get; set; } = 1f;

        public SkillEffectRenderer(SkillEffectVisualService service, Camera camera)
        {
            _service = service;
            _camera = camera;
        }

        /// <summary>Draw all active skill effects. Call from OnGUI or IMGUI overlay.</summary>
        public void Render()
        {
            if (_service == null || _camera == null) return;

            var effects = _service.GetActiveEffects();
            foreach (var fx in effects)
            {
                switch (fx.phase)
                {
                    case SkillEffectPhase.PreCast:
                        DrawPreCast(fx);
                        break;
                    case SkillEffectPhase.Missile:
                        DrawMissiles(fx);
                        break;
                    case SkillEffectPhase.Impact:
                        DrawImpact(fx);
                        break;
                }
            }
        }

        private void DrawPreCast(ActiveSkillEffect fx)
        {
            // Draw a pulsing circle at caster position (PreCast effect)
            var screenPos = WorldToScreen(fx.casterPos);
            float t = fx.elapsed / Mathf.Max(0.01f, fx.preCastDuration);
            float radius = Mathf.Lerp(8f, 24f, t);
            float alpha = Mathf.Lerp(0.9f, 0.2f, t);

            var color = fx.color;
            color.a = alpha;

            // Draw expanding ring
            DrawCircle(screenPos, radius, color, 2f);

            // Draw skill name briefly
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(fx.color.r, fx.color.g, fx.color.b, alpha) }
            };
            var rect = new Rect(screenPos.x - 40, screenPos.y - 30, 80, 16);
            GUI.Label(rect, fx.skillName, style);
        }

        private void DrawMissiles(ActiveSkillEffect fx)
        {
            if (fx.missileCount <= 1)
            {
                // Single missile: draw moving projectile
                var screenPos = WorldToScreen(fx.currentMissilePos);
                DrawProjectile(screenPos, fx.color, fx.trailEnabled, fx.casterPos);
            }
            else
            {
                // Multiple missiles (Surround/Fan)
                if (fx.missilePositions != null)
                {
                    for (int i = 0; i < fx.missilePositions.Length; i++)
                    {
                        var screenPos = WorldToScreen(fx.missilePositions[i]);
                        DrawProjectile(screenPos, fx.color, false, fx.casterPos);
                    }
                }
            }
        }

        private void DrawProjectile(Vector2 screenPos, Color color, bool trail, Vector2 casterPos)
        {
            // Draw the missile as a bright circle with glow
            DrawCircle(screenPos, 6f, color, 3f);

            // Inner bright core
            var coreColor = new Color(
                Mathf.Min(1f, color.r + 0.3f),
                Mathf.Min(1f, color.g + 0.3f),
                Mathf.Min(1f, color.b + 0.3f), 0.95f);
            DrawCircle(screenPos, 3f, coreColor, 1f);

            if (trail)
            {
                // Draw a trail line from near-caster to current pos
                var from = WorldToScreen(casterPos);
                DrawLine(from, screenPos, new Color(color.r, color.g, color.b, 0.3f), 1f);
            }
        }

        private void DrawImpact(ActiveSkillEffect fx)
        {
            // Draw expanding burst at target position
            var screenPos = WorldToScreen(fx.targetPos);
            float t = (fx.elapsed - fx.phaseStart) / Mathf.Max(0.01f, fx.impactDuration);
            float radius = Mathf.Lerp(4f, 32f, t);
            float alpha = Mathf.Lerp(0.8f, 0f, t);

            var color = fx.color;
            color.a = alpha;

            DrawCircle(screenPos, radius, color, 2f);

            // Flash at impact center
            if (t < 0.3f)
            {
                var flashColor = new Color(1f, 1f, 1f, Mathf.Lerp(0.9f, 0f, t / 0.3f));
                DrawCircle(screenPos, radius * 0.3f, flashColor, 1f);
            }
        }

        private Vector2 WorldToScreen(Vector2 worldPos)
        {
            var screenPos3 = _camera.WorldToScreenPoint(new Vector3(worldPos.x, worldPos.y, 0f));
            // IMGUI uses top-left origin, camera uses bottom-left
            return new Vector2(screenPos3.x, Screen.height - screenPos3.y);
        }

        private static void DrawCircle(Vector2 center, float radius, Color color, float thickness)
        {
            var segments = 24;
            var prevColor = GUI.color;
            GUI.color = color;

            var prevPos = center + new Vector2(radius, 0);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                var pos = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                DrawLine(prevPos, pos, color, thickness);
                prevPos = pos;
            }

            GUI.color = prevColor;
        }

        private static void DrawLine(Vector2 from, Vector2 to, Color color, float width)
        {
            var prevColor = GUI.color;
            GUI.color = color;

            var delta = to - from;
            float length = delta.magnitude;
            if (length < 0.5f) return;

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            var matrixBackup = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, from);
            var rect = new Rect(from.x, from.y - width * 0.5f, length, width);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.matrix = matrixBackup;

            GUI.color = prevColor;
        }
    }
}
