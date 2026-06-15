using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Sandbox;
using VLTK.Sprites;

namespace VLTK.UI
{
    /// <summary>
    /// Renders active skill combat effects on screen using IMGUI overlay.
    /// Draws PreCast animation, missile projectiles, and impact effects
    /// for each active skill cast from the SkillEffectVisualService.
    /// Visual style matches PC JXWin: exact SPR frames via KMissleRes::Draw layout.
    ///
    /// PC source: KMissleRes::Draw(MS_DoFly) selects frame by:
    ///   nImageDir = round(nDir 64-dir → nSprDir)
    ///   nFramePerDir = totalFrames / nSprDir
    ///   frameIndex = nImageDir * nFramePerDir + (lifeTick / interval) % nFramePerDir
    /// </summary>
    public class SkillEffectRenderer
    {
        private readonly SkillEffectVisualService _service;
        private readonly Camera _camera;
        private readonly Dictionary<string, Sprite[]> _pcSpriteCache = new();

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
            // PC source: render exact SPR frame for each missile.
            // KMissleRes::Draw picks frame by direction + life-tick, alpha-blended at PC center.
            if (fx.HasPcMissileSprite)
            {
                DrawPcMissileSprite(fx);
                return;
            }

            // Fallback: legacy circle/dot rendering (no SPR resolved)
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

        /// <summary>
        /// Render PC missile SPR frames. Ported from SkillEffectWorldOverlay.SelectPcMissileFrame.
        /// PC source: KMissleRes::Draw iterates all live missiles and draws each at its current
        /// world position with the frame selected by travel direction + life tick.
        /// </summary>
        private void DrawPcMissileSprite(ActiveSkillEffect fx)
        {
            var sprites = LoadPcSprites(fx.pcMissileSpriteKey);
            if (sprites == null || sprites.Length == 0)
            {
                // Fallback to dot if SPR decode failed
                var screenPos = WorldToScreen(fx.currentMissilePos);
                DrawProjectile(screenPos, fx.color, fx.trailEnabled, fx.casterPos);
                return;
            }

            // Single missile uses currentMissilePos; multi-missile uses missilePositions[]
            int n = fx.missileCount > 0 && fx.missilePositions != null
                ? Mathf.Min(fx.missileCount, fx.missilePositions.Length)
                : 1;

            for (int i = 0; i < n; i++)
            {
                Vector2 missilePos = fx.missilePositions != null && i < fx.missilePositions.Length
                    ? fx.missilePositions[i]
                    : fx.currentMissilePos;
                // Homing (MoveKind=5): target tracks live target position; else cast-time targetPos
                Vector2 target = fx.getCurrentTargetPos != null
                    ? fx.getCurrentTargetPos()
                    : (fx.missileTargets != null && i < fx.missileTargets.Length
                        ? fx.missileTargets[i]
                        : fx.targetPos);

                var sprite = SelectPcMissileFrame(fx, sprites, missilePos, target);
                if (sprite == null || sprite.texture == null) continue;

                DrawSpriteScreen(sprite, missilePos);
            }
        }

        /// <summary>
        /// Select the correct SPR frame for a missile given its current position and travel direction.
        /// Ported from SkillEffectWorldOverlay.SelectPcMissileFrame (PC KMissleRes::Draw).
        /// </summary>
        private static Sprite SelectPcMissileFrame(ActiveSkillEffect fx, Sprite[] sprites, Vector2 fromPos, Vector2 targetPos)
        {
            int dir = ComputePc16Dir(fromPos, targetPos);
            int framePerDir = Mathf.Max(1, fx.pcMissileTotalFrames / Mathf.Max(1, fx.pcMissileDirections));
            int lifeTick = Mathf.Max(0, Mathf.FloorToInt((fx.elapsed - fx.phaseStart) * 18f));
            int localFrame = (lifeTick / Mathf.Max(1, fx.pcMissileIntervalTicks)) % framePerDir;
            int frameIndex = Mathf.Clamp(dir * framePerDir + localFrame, 0, sprites.Length - 1);
            return sprites[frameIndex];
        }

        /// <summary>
        /// PC 16-direction bucket. Ported from SkillEffectWorldOverlay.ComputePc16Dir.
        /// Mobile world: +X east, +Y north. PC missile SPR frames point back to caster,
        /// so +8 buckets (180°) flip the head to face travel direction.
        /// </summary>
        private static int ComputePc16Dir(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            if (d.sqrMagnitude < 0.001f) return 0;
            float angle = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg; // 0=N, +90=E
            int dir = (Mathf.RoundToInt(angle / 22.5f) + 8) & 15;
            return dir;
        }

        /// <summary>
        /// Draw a decoded SPR sprite at world position, honoring PC center pivot.
        /// IMGUI DrawTexture uses screen space; we scale the sprite to its native pixel size.
        /// </summary>
        private void DrawSpriteScreen(Sprite sprite, Vector2 worldPos)
        {
            var tex = sprite.texture;
            if (tex == null) return;
            var screenPos = WorldToScreen(worldPos);

            // PC sprite native pixel size, scaled by WorldToScreenScale for camera zoom.
            float w = tex.width * WorldToScreenScale;
            float h = tex.height * WorldToScreenScale;

            // Center the sprite at screenPos using PC pivot (pivot is 0..1 normalized).
            float px = sprite.pivot.x / Mathf.Max(1f, tex.width);
            float py = sprite.pivot.y / Mathf.Max(1f, tex.height);
            // IMGUI Y is top-down, flip pivot vertically.
            float drawX = screenPos.x - w * px;
            float drawY = screenPos.y - h * (1f - py);

            var prevColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(drawX, drawY, w, h), tex);
            GUI.color = prevColor;
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
            // PC source: KMissleRes::OnCollision draws explosion SPR frames.
            if (fx.HasPcImpactSprite)
            {
                DrawPcImpactSprite(fx);
                return;
            }

            // Fallback: expanding burst circle (no SPR resolved)
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

        /// <summary>PC source: explosion SPR frames cycle by life tick at impact target.</summary>
        private void DrawPcImpactSprite(ActiveSkillEffect fx)
        {
            var sprites = LoadPcSprites(fx.pcImpactSpriteKey);
            if (sprites == null || sprites.Length == 0)
            {
                // Fallback to circle burst
                var screenPos = WorldToScreen(fx.targetPos);
                DrawCircle(screenPos, 16f, fx.color, 2f);
                return;
            }

            int lifeTick = Mathf.Max(0, Mathf.FloorToInt((fx.elapsed - fx.phaseStart) * 18f));
            int framePerDir = Mathf.Max(1, fx.pcImpactTotalFrames / Mathf.Max(1, fx.pcImpactDirections));
            int localFrame = (lifeTick / Mathf.Max(1, fx.pcImpactIntervalTicks)) % framePerDir;
            int frameIndex = Mathf.Clamp(localFrame, 0, sprites.Length - 1);

            var sprite = sprites[frameIndex] ?? sprites[0];
            if (sprite != null) DrawSpriteScreen(sprite, fx.targetPos);
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

        /// <summary>
        /// Decode a PC SPR file from StreamingAssets/Sprites/{key}.spr and cache its frames.
        /// Ported from SkillEffectWorldOverlay.LoadPcSprites.
        /// </summary>
        private Sprite[] LoadPcSprites(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            string path = Path.Combine(Application.streamingAssetsPath, "Sprites",
                key.EndsWith(".spr") ? key : key + ".spr");
            if (!File.Exists(path))
            {
                SubsystemLog.Warn("Combat", $"PC skill SPR missing: {path}");
                _pcSpriteCache[key] = null;
                return null;
            }

            if (_pcSpriteCache.TryGetValue(key, out var cached)) return cached;

            var decoded = SprDecoder.Decode(File.ReadAllBytes(path));
            if (!decoded.success || decoded.frames == null || decoded.frames.Length == 0)
            {
                SubsystemLog.Warn("Combat", $"PC skill SPR decode failed: {key} — {decoded.error}");
                _pcSpriteCache[key] = null;
                return null;
            }

            var sprites = new Sprite[decoded.frames.Length];
            for (int i = 0; i < decoded.frames.Length; i++)
            {
                var frame = decoded.frames[i];
                if (frame == null || frame.width == 0 || frame.height == 0) continue;
                var tex = SprDecoder.CreateTexture(frame);
                if (tex == null) continue;
                tex.name = $"PCSPR_{key}_{i}";
                // KSprite::DrawAlpha draws at (x - centerX + frame.OffsetX, y - centerY + frame.OffsetY).
                float pivotX = decoded.header.width > 0
                    ? decoded.header.centerX / (float)decoded.header.width : 0.5f;
                float pivotY = decoded.header.height > 0
                    ? 1f - decoded.header.centerY / (float)decoded.header.height : 0.5f;
                sprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(pivotX, pivotY), 1f);
                sprites[i].name = $"PCSPR_{key}_{i}";
            }

            _pcSpriteCache[key] = sprites;
            return sprites;
        }
    }
}
