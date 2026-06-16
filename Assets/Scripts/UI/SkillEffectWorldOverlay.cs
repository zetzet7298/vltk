using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Sandbox;
using VLTK.Sprites;

namespace VLTK.UI
{
    /// <summary>
    /// World-space renderer for active skill effects.
    /// Renders cast rings, missile projectiles with trails, and impact bursts
    /// using LineRenderer + SpriteRenderer at the correct world positions.
    ///
    /// Scales automatically with the scene camera orthographicSize so VFX remains
    /// proportional regardless of zoom level.
    ///
    /// This is a visible fallback when no PC SPR/prefab has been configured yet.
    /// Future exact PC VFX: configure via SkillEffectVisualService preCastSprite / missileSprite.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillEffectWorldOverlay : MonoBehaviour
    {
        [Header("Sizing (fraction of visible screen height)")]
        [Tooltip("PreCast ring min radius as fraction of visible world height (0..1).")]
        public float preCastRadiusMinFrac = 0.06f;
        [Tooltip("PreCast ring max radius as fraction of visible world height.")]
        public float preCastRadiusMaxFrac = 0.18f;
        [Tooltip("Impact burst max radius as fraction of visible world height.")]
        public float impactRadiusMaxFrac = 0.22f;
        [Tooltip("Missile dot diameter as fraction of visible world height.")]
        public float missileDotFrac = 0.05f;
        [Tooltip("Line width as fraction of visible world height.")]
        public float lineWidthFrac = 0.018f;

        [Header("Duration overrides (seconds)")]
        public float minPreCastDuration = 0.3f;
        public float minImpactDuration = 0.5f;

        public int sortingOrder = 32000;

        private readonly Dictionary<ActiveSkillEffect, RuntimeEffectVisual> _visuals = new();
        private readonly Dictionary<string, Sprite[]> _pcSpriteCache = new();
        // Per-key header center + per-frame offset (for PC body-aura frame-offset animation).
        // Key same as LoadPcSprites cacheKey.
        private readonly Dictionary<string, (int centerX, int centerY, Vector2[] frameOffsets)> _pcSpriteFrameData = new();
        private Material _lineMaterial;
        
        private Sprite _dotSprite;
        private Camera _cam;
        private float _cachedOrthoSize;
        private float _worldHeight;
        private float _scale; // world units per 1 fraction unit

        private void EnsureResources()
        {
            FindCamera();
            if (_cam == null) return;

            // Recompute scale when camera zoom changes
            if (Mathf.Abs(_cam.orthographicSize - _cachedOrthoSize) > 0.1f)
            {
                _cachedOrthoSize = _cam.orthographicSize;
                _worldHeight = _cachedOrthoSize * 2f;
                _scale = _worldHeight;
            }

            if (_lineMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default")
                    ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default")
                    ?? Shader.Find("Unlit/Color");
                _lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };


            }
            if (_dotSprite == null)
                _dotSprite = CreateDotSprite();
        }

        private void FindCamera()
        {
            if (_cam != null && _cam.isActiveAndEnabled) return;
            // Tagged "MainCamera" is unreliable; find by Camera component instead.
            _cam = null;
            var cams = FindObjectsOfType<Camera>();
            foreach (var c in cams)
            {
                if (c.orthographic && c.enabled)
                {
                    _cam = c;
                    return;
                }
            }
        }

        private void LateUpdate()
        {
            EnsureResources();
            if (_cam == null) return;

            var service = SandboxManager.Instance?.SkillEffectVisual;
            if (service == null) return;

            var active = service.GetActiveEffects();
            var stillActive = new HashSet<ActiveSkillEffect>(active);

            foreach (var fx in active)
            {
                if (!_visuals.TryGetValue(fx, out var visual))
                {
                    visual = CreateVisual(fx);
                    _visuals[fx] = visual;
                }
                UpdateVisual(fx, visual);
            }

            var toRemove = new List<ActiveSkillEffect>();
            foreach (var kv in _visuals)
                if (!stillActive.Contains(kv.Key) || kv.Key.phase == SkillEffectPhase.Finished)
                    toRemove.Add(kv.Key);

            foreach (var fx in toRemove)
            {
                if (_visuals.TryGetValue(fx, out var visual) && visual.root != null)
                    Destroy(visual.root);
                _visuals.Remove(fx);
            }
        }

        // ── Factory ──────────────────────────────────────────────────────────

        private RuntimeEffectVisual CreateVisual(ActiveSkillEffect fx)
        {
            var root = new GameObject($"SkillVFX_{fx.skillId}_{fx.skillName}");
            root.transform.SetParent(SandboxManager.Instance?.worldRoot, false);

            var ring = CreateLine(root.transform, "PreCastRing", loop: true);
            var impact = CreateLine(root.transform, "ImpactRing", loop: true);
            var trail = CreateLine(root.transform, "Trail", loop: false);

            var missileGo = new GameObject("Missile");
            missileGo.transform.SetParent(root.transform, false);
            var sr = missileGo.AddComponent<SpriteRenderer>();
            sr.sprite = fx.HasPcMissileSprite ? FirstValidPcSprite(fx.pcMissileSpriteKey) : _dotSprite;
            sr.sortingOrder = sortingOrder + 2;
            sr.color = Color.white;

            var pcMissiles = new List<SpriteRenderer> { sr };
            if (fx.HasPcMissileSprite && fx.missileCount > 1)
            {
                for (int i = 1; i < fx.missileCount; i++)
                {
                    var extra = new GameObject($"Missile_{i}");
                    extra.transform.SetParent(root.transform, false);
                    var extraSr = extra.AddComponent<SpriteRenderer>();
                    extraSr.sprite = FirstValidPcSprite(fx.pcMissileSpriteKey);
                    extraSr.sortingOrder = sortingOrder + 2;
                    extraSr.color = Color.white;
                    extraSr.enabled = false;
                    pcMissiles.Add(extraSr);
                }
            }

            var impactGo = new GameObject("PcImpact");
            impactGo.transform.SetParent(root.transform, false);
            var impactSr = impactGo.AddComponent<SpriteRenderer>();
            impactSr.sprite = fx.HasPcImpactSprite ? FirstValidPcSprite(fx.pcImpactSpriteKey) : null;
            impactSr.sortingOrder = sortingOrder + 3;
            impactSr.color = Color.white;
            impactSr.enabled = false;

            var preCastGo = new GameObject("PcPreCast");
            preCastGo.transform.SetParent(root.transform, false);
            var preCastSr = preCastGo.AddComponent<SpriteRenderer>();
            preCastSr.sprite = fx.HasPcPreCastSprite ? FirstValidPcSprite(fx.pcPreCastSpriteKey) : null;
            preCastSr.sortingOrder = sortingOrder + 3;
            preCastSr.color = Color.white;
            preCastSr.enabled = false;

            return new RuntimeEffectVisual
            {
                root = root,
                preCastRing = ring,
                impactRing = impact,
                trail = trail,
                missileDot = sr,
                pcMissiles = pcMissiles,
                pcImpact = impactSr,
                pcPreCast = preCastSr,
            };
        }

        private LineRenderer CreateLine(Transform parent, string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.material = new Material(_lineMaterial); // instance per-line to avoid shared state
            line.useWorldSpace = true;
            line.loop = loop;
            line.widthMultiplier = _scale * lineWidthFrac;
            line.numCapVertices = 6;
            line.numCornerVertices = 6;
            line.positionCount = 0;
            line.sortingOrder = sortingOrder + 1;
            line.startColor = Color.white;
            line.endColor = Color.white;
            // LineRenderer uses its own generated mesh; ensure the material has a white texture.
            line.material.mainTexture = Texture2D.whiteTexture;
            return line;
        }

        // ── Per-frame update ─────────────────────────────────────────────────

        private void UpdateVisual(ActiveSkillEffect fx, RuntimeEffectVisual v)
        {
            if (v.root == null) return;

            // Keep line width synced with camera zoom
            float lineW = _scale * lineWidthFrac;
            float preCastRMin = _scale * preCastRadiusMinFrac;
            float preCastRMax = _scale * preCastRadiusMaxFrac;
            float impactRMax = _scale * impactRadiusMaxFrac;

            switch (fx.phase)
            {
                case SkillEffectPhase.PreCast:
                {
                    if (fx.isAura)
                    {
                        // PC body-aura buff (e.g. Túy Điệp StateSpecial 43 butterfly):
                        // render the SPR on the player body, following the live player position,
                        // looping the PC sub-range (pcAuraFrameStart..pcAuraFrameEnd).
                        if (fx.HasPcPreCastSprite)
                        {
                            Hide(v.preCastRing);
                            Vector2 auraPos = ResolveLiveCasterPos(fx);
                            v.pcPreCast.enabled = true;
                            // PC: KSprite::DrawAlpha draws SPR at native pixel size (ppu=1).
                            // No extra scaling — the camera zoom + screen res handles visibility.
                            v.pcPreCast.transform.localScale = Vector3.one;
                            // PC KSprite::DrawAlpha per-frame offset: (offsetX - centerX, centerY - offsetY).
                            // At ppu=1, the offset is naturally in world units (no extra scale multiplication).
                            int frameIdx = SelectPcAuraFrameIndex(fx);
                            v.pcPreCast.sprite = SelectPcAuraFrame(fx, frameIdx);
                            Vector2 offset = GetPcAuraFrameWorldOffset(fx, frameIdx, 1f);
                            v.pcPreCast.transform.position = new Vector3(auraPos.x + offset.x, auraPos.y + offset.y, 0f);
                            Hide(v.impactRing);
                            Hide(v.trail);
                            SetMissileVisible(v, false);
                            SetImpactVisible(v, false);
                            break;
                        }

                        float dur = Mathf.Max(0.05f, fx.auraDuration);
                        float t = Mathf.Clamp01(fx.elapsed / dur);
                        float pulse = 0.5f + 0.5f * Mathf.Sin(fx.elapsed * fx.auraPulseRate * Mathf.PI * 2f);
                        var c = fx.color;
                        c.a = Mathf.Lerp(0.85f, 0.15f, t) * Mathf.Lerp(0.55f, 1f, pulse);
                        float r = Mathf.Max(1f, fx.auraRadius) * Mathf.Lerp(0.85f, 1.15f, pulse);
                        DrawRing(v.preCastRing, fx.casterPos, r, c, lineW * 1.25f);
                        Hide(v.impactRing);
                        Hide(v.trail);
                        SetMissileVisible(v, false);
                        SetImpactVisible(v, false);
                        SetPreCastVisible(v, false);
                        break;
                    }

                    if (fx.HasPcPreCastSprite)
                    {
                        Hide(v.preCastRing);
                        v.pcPreCast.enabled = true;
                        v.pcPreCast.transform.position = new Vector3(fx.casterPos.x, fx.casterPos.y, 0f);
                        v.pcPreCast.transform.localScale = Vector3.one;
                        v.pcPreCast.sprite = SelectPcPreCastFrame(fx);
                    }
                    else if (fx.HasPcMissileSprite)
                    {
                        // PC skill 128 has PreCastSpr mag_tr_16_施魔法.spr; until that SPR is staged,
                        // do not draw fake geometry. The visible PC missile starts at MS_DoFly.
                        Hide(v.preCastRing);
                    }
                    else
                    {
                        float dur = Mathf.Max(fx.preCastDuration, minPreCastDuration);
                        float t = Mathf.Clamp01(fx.elapsed / dur);
                        var c = fx.color;
                        c.a = Mathf.Lerp(1f, 0.3f, t);
                        float r = Mathf.Lerp(preCastRMin, preCastRMax, t);
                        DrawRing(v.preCastRing, fx.casterPos, r, c, lineW);
                    }
                    Hide(v.impactRing);
                    Hide(v.trail);
                    SetMissileVisible(v, false);
                    SetImpactVisible(v, false);
                    break;
                }
                case SkillEffectPhase.Missile:
                {
                    Hide(v.preCastRing);
                    SetPreCastVisible(v, false);
                    Hide(v.impactRing);
                    SetImpactVisible(v, false);

                    Vector2 p = fx.currentMissilePos;
                    if (fx.missilePositions != null && fx.missilePositions.Length > 0)
                        p = fx.missilePositions[0];
                    if (p.sqrMagnitude < 0.01f)
                        p = fx.casterPos;

                    var c = fx.color;
                    c.a = 0.9f;
                    if (fx.HasPcMissileSprite)
                        Hide(v.trail);
                    else
                        DrawLineSegment(v.trail, fx.casterPos, p, new Color(c.r, c.g, c.b, 0.5f), lineW);

                    if (fx.HasPcMissileSprite && v.pcMissiles != null)
                    {
                        Vector2 liveTarget = fx.getCurrentTargetPos != null ? fx.getCurrentTargetPos() : fx.targetPos;
                        for (int i = 0; i < v.pcMissiles.Count; i++)
                        {
                            var renderer = v.pcMissiles[i];
                            Vector2 mp = fx.missilePositions != null && i < fx.missilePositions.Length ? fx.missilePositions[i] : p;
                            // Homing: face the live target from the missile's current position
                            // so the dragon SPR rotates as it curves toward a moving enemy.
                            renderer.sprite = SelectPcMissileFrame(fx, mp, liveTarget);
                            renderer.transform.position = new Vector3(mp.x, mp.y, 0f);
                            renderer.transform.localScale = Vector3.one; // SPR decoder already outputs PC-correct orientation.
                            renderer.color = Color.white;
                            renderer.enabled = true;
                        }
                    }
                    else
                    {
                        v.missileDot.transform.position = new Vector3(p.x, p.y, 0f);
                        float dotSize = _scale * missileDotFrac;
                        v.missileDot.transform.localScale = Vector3.one * dotSize;
                        v.missileDot.color = c;
                        SetMissileVisible(v, true);
                    }

                    // Sâu xé: render proximity rend flashes per-missile
                    RenderRendFlashes(fx, v);

                    break;
                }
                case SkillEffectPhase.Impact:
                {
                    Hide(v.preCastRing);
                    SetPreCastVisible(v, false);
                    Hide(v.trail);
                    SetMissileVisible(v, false);

                    if (fx.HasPcImpactSprite)
                    {
                        Hide(v.impactRing);
                        v.pcImpact.enabled = true;
                        v.pcImpact.transform.position = new Vector3(fx.targetPos.x, fx.targetPos.y, 0f);
                        v.pcImpact.transform.localScale = Vector3.one;
                        v.pcImpact.sprite = SelectPcImpactFrame(fx);
                        break;
                    }
                    if (fx.HasPcMissileSprite)
                    {
                        Hide(v.impactRing);
                        break;
                    }

                    float dur = Mathf.Max(fx.impactDuration, minImpactDuration);
                    float t = Mathf.Clamp01((fx.elapsed - fx.phaseStart) / dur);
                    var c = fx.color;
                    c.a = Mathf.Lerp(1f, 0f, t);
                    float r = Mathf.Lerp(preCastRMin, impactRMax, t);
                    DrawRing(v.impactRing, fx.targetPos, r, c, lineW * (1f + t));

                    // Inner white flash / target hit feedback
                    float flashWindow = fx.isHitFlash ? 1f : 0.3f;
                    if (t < flashWindow && v.impactFlash == null)
                    {
                        var flashGo = new GameObject("Flash");
                        flashGo.transform.SetParent(v.root.transform, false);
                        var fsr = flashGo.AddComponent<SpriteRenderer>();
                        fsr.sprite = _dotSprite;
                        fsr.sortingOrder = sortingOrder + 3;
                        fsr.color = new Color(1f, 1f, 1f, 0.9f);
                        flashGo.transform.position = new Vector3(fx.targetPos.x, fx.targetPos.y, 0f);
                        v.impactFlash = fsr;
                    }
                    if (v.impactFlash != null)
                    {
                        float flashT = Mathf.Clamp01(t / flashWindow);
                        float baseSize = fx.isHitFlash ? 0.09f : 0.06f;
                        float flashSize = _scale * baseSize * (1f + flashT * (fx.isHitFlash ? 3f : 2f));
                        v.impactFlash.transform.localScale = Vector3.one * flashSize;
                        var fc = fx.isHitFlash ? fx.color : v.impactFlash.color;
                        fc.a = t < flashWindow ? Mathf.Lerp(0.95f, 0f, flashT) : 0f;
                        v.impactFlash.color = fc;
                    }
                    break;
                }
            }
        }

        // ── PC SPR playback ─────────────────────────────────────────────────

        private Sprite FirstValidPcSprite(string key)
        {
            var sprites = LoadPcSprites(key);
            if (sprites == null) return _dotSprite;
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null) return sprites[i];
            return _dotSprite;
        }

        private Sprite SelectPcMissileFrame(ActiveSkillEffect fx)
        {
            return SelectPcMissileFrame(fx, fx.targetPos);
        }

        private Sprite SelectPcMissileFrame(ActiveSkillEffect fx, Vector2 targetPos)
        {
            return SelectPcMissileFrame(fx, fx.casterPos, targetPos);
        }

        private Sprite SelectPcMissileFrame(ActiveSkillEffect fx, Vector2 fromPos, Vector2 targetPos)
        {
            var sprites = LoadPcSprites(fx.pcMissileSpriteKey);
            if (sprites == null || sprites.Length == 0) return _dotSprite;

            // PC KMissleRes::Draw(MS_DoFly):
            // nImageDir = rounded nDir from 64-dir space into nSprDir; nFramePerDir = totalFrames / nSprDir;
            // For homing missiles (MoveKind=5 in missles.txt), the dir must update each tick
            // to face the current target — dragon SPR rotates as it chases a moving enemy.
            int dir = ComputePc16Dir(fromPos, targetPos);
            int framePerDir = Mathf.Max(1, fx.pcMissileTotalFrames / Mathf.Max(1, fx.pcMissileDirections));
            int lifeTick = Mathf.Max(0, Mathf.FloorToInt((fx.elapsed - fx.phaseStart) * 18f));
            int localFrame = (lifeTick / Mathf.Max(1, fx.pcMissileIntervalTicks)) % framePerDir;
            int frameIndex = Mathf.Clamp(dir * framePerDir + localFrame, 0, sprites.Length - 1);
            return sprites[frameIndex] ?? FirstValidPcSprite(fx.pcMissileSpriteKey);
        }

        private Sprite SelectPcImpactFrame(ActiveSkillEffect fx)
        {
            var sprites = LoadPcSprites(fx.pcImpactSpriteKey);
            if (sprites == null || sprites.Length == 0) return null;
            int lifeTick = Mathf.Max(0, Mathf.FloorToInt((fx.elapsed - fx.phaseStart) * 18f));
            int frameIndex = Mathf.Clamp(lifeTick / Mathf.Max(1, fx.pcImpactIntervalTicks), 0, sprites.Length - 1);
            return sprites[frameIndex] ?? FirstValidPcSprite(fx.pcImpactSpriteKey);
        }

        private Sprite SelectPcPreCastFrame(ActiveSkillEffect fx)
        {
            var sprites = LoadPcSprites(fx.pcPreCastSpriteKey);
            if (sprites == null || sprites.Length == 0) return null;
            int lifeTick = Mathf.Max(0, Mathf.FloorToInt(fx.elapsed * 18f));
            int frameIndex = Mathf.Clamp(lifeTick / Mathf.Max(1, fx.pcPreCastIntervalTicks), 0, sprites.Length - 1);
            return sprites[frameIndex] ?? FirstValidPcSprite(fx.pcPreCastSpriteKey);
        }

        /// <summary>
        /// Select a looping body-aura SPR frame for self-buff visuals (e.g. Túy Điệp butterfly).
        /// PC source: 状态与光效图形对照表 Status entry — PlayMode=Loop over a sub-range
        /// (主角身后开始帧..结束帧). When pcAuraFrameEnd&gt;pcAuraFrameStart, loop inside that
        /// range; otherwise loop the full frame set.
        /// </summary>
        private Sprite SelectPcAuraFrame(ActiveSkillEffect fx)
        {
            int idx = SelectPcAuraFrameIndex(fx);
            return SelectPcAuraFrame(fx, idx);
        }

        private int SelectPcAuraFrameIndex(ActiveSkillEffect fx)
        {
            var sprites = LoadPcSprites(fx.pcPreCastSpriteKey);
            if (sprites == null || sprites.Length == 0) return 0;

            int lo = Mathf.Clamp(fx.pcAuraFrameStart, 0, sprites.Length - 1);
            int hi = fx.pcAuraFrameEnd > fx.pcAuraFrameStart
                ? Mathf.Clamp(fx.pcAuraFrameEnd, 0, sprites.Length - 1)
                : sprites.Length - 1;
            int span = Mathf.Max(1, hi - lo + 1);

            int lifeTick = Mathf.Max(0, Mathf.FloorToInt(fx.elapsed * 18f));
            int local = (lifeTick / Mathf.Max(1, fx.pcPreCastIntervalTicks)) % span;
            return Mathf.Clamp(lo + local, 0, sprites.Length - 1);
        }

        private Sprite SelectPcAuraFrame(ActiveSkillEffect fx, int frameIndex)
        {
            var sprites = LoadPcSprites(fx.pcPreCastSpriteKey);
            if (sprites == null || sprites.Length == 0) return null;
            return sprites[frameIndex] ?? FirstValidPcSprite(fx.pcPreCastSpriteKey);
        }

        /// <summary>
        /// Compute the world-space offset for a PC body-aura frame.
        /// PC KSprite::DrawAlpha: (x - centerX + frame.OffsetX, y - centerY + frame.OffsetY).
        /// Adapted for Unity Y+ up: offsetX = (frame.offsetX - centerX), offsetY = (centerY - frame.offsetY).
        /// Scaled by auraScale (pixelsPerUnit=1 so 1 px = 1 unit * scale).
        /// </summary>
        private Vector2 GetPcAuraFrameWorldOffset(ActiveSkillEffect fx, int frameIndex, float scale)
        {
            var sprites = LoadPcSprites(fx.pcPreCastSpriteKey);
            if (sprites == null || frameIndex < 0) return Vector2.zero;

            string path = Path.Combine(Application.streamingAssetsPath, "Sprites", fx.pcPreCastSpriteKey.EndsWith(".spr") ? fx.pcPreCastSpriteKey : fx.pcPreCastSpriteKey + ".spr");
            if (!System.IO.File.Exists(path)) return Vector2.zero;
            var fileInfo = new System.IO.FileInfo(path);
            string cacheKey = $"{fx.pcPreCastSpriteKey}|{fileInfo.Length}|{fileInfo.LastWriteTimeUtc.Ticks}";

            if (!_pcSpriteFrameData.TryGetValue(cacheKey, out var data)) return Vector2.zero;
            if (frameIndex >= data.frameOffsets.Length) return Vector2.zero;

            Vector2 foff = data.frameOffsets[frameIndex];
            // (offsetX - centerX, centerY - offsetY) scaled by aura scale
            return new Vector2((foff.x - data.centerX) * scale, (data.centerY - foff.y) * scale);
        }

        /// <summary>
        /// Resolve the live caster position so body-aura buffs follow the player as it moves.
        /// Falls back to the cast-time casterPos when the player is unavailable.
        /// </summary>
        private static Vector2 ResolveLiveCasterPos(ActiveSkillEffect fx)
        {
            var player = SandboxManager.Instance?.PlayerController;
            if (player != null)
                return (Vector2)player.transform.position;
            return fx.casterPos;
        }

        private Sprite[] LoadPcSprites(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            string path = Path.Combine(Application.streamingAssetsPath, "Sprites", key.EndsWith(".spr") ? key : key + ".spr");
            if (!File.Exists(path))
            {
                SubsystemLog.Warn("Combat", $"PC skill SPR missing: {path}");
                _pcSpriteCache[key] = null;
                return null;
            }

            var fileInfo = new FileInfo(path);
            string cacheKey = $"{key}|{fileInfo.Length}|{fileInfo.LastWriteTimeUtc.Ticks}";
            if (_pcSpriteCache.TryGetValue(cacheKey, out var cached)) return cached;

            var decoded = SprDecoder.Decode(File.ReadAllBytes(path));
            if (!decoded.success || decoded.frames == null || decoded.frames.Length == 0)
            {
                SubsystemLog.Warn("Combat", $"PC skill SPR decode failed: {key} — {decoded.error}");
                _pcSpriteCache[cacheKey] = null;
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
                // Preserve PC center as sprite pivot.
                float pivotX = decoded.header.width > 0 ? decoded.header.centerX / (float)decoded.header.width : 0.5f;
                float pivotY = decoded.header.height > 0 ? 1f - decoded.header.centerY / (float)decoded.header.height : 0.5f;
                sprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(pivotX, pivotY), 1f);
                sprites[i].name = $"PCSPR_{key}_{i}";
            }

            // Cache frame offset data for PC body-aura animation.
            int cx = decoded.header.centerX;
            int cy = decoded.header.centerY;
            var offsets = new Vector2[decoded.frames.Length];
            for (int i = 0; i < decoded.frames.Length; i++)
            {
                var f = decoded.frames[i];
                offsets[i] = new Vector2(f != null ? f.offsetX : 0, f != null ? f.offsetY : 0);
            }
            _pcSpriteFrameData[cacheKey] = (cx, cy, offsets);
            _pcSpriteCache[cacheKey] = sprites;
            return sprites;
        }

        private static int ComputePc16Dir(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            if (d.sqrMagnitude < 0.001f) return 0;
            // Mobile world uses +X east, +Y north. PC missile SPR frames are stored with image direction
            // opposite to the movement vector bucket (observed mag_gb_05 dragon heads point back to caster
            // without this PC 16-dir half-turn). Offset by 8 buckets = 180°.
            float angle = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg; // 0=N, +90=E
            int dir = (Mathf.RoundToInt(angle / 22.5f) + 8) & 15;
            return dir;
        }

        private void RenderRendFlashes(ActiveSkillEffect fx, RuntimeEffectVisual v)
        {
            if (fx.rendPositions == null) return;
            int flashIdx = 0;
            foreach (var rp in fx.rendPositions)
            {
                SpriteRenderer sr;
                if (flashIdx < v.rendFlash.Count && v.rendFlash[flashIdx] != null)
                {
                    sr = v.rendFlash[flashIdx];
                }
                else
                {
                    var rendGo = new GameObject("RendFlash");
                    rendGo.transform.SetParent(v.root.transform, false);
                    sr = rendGo.AddComponent<SpriteRenderer>();
                    sr.sprite = _dotSprite;
                    sr.sortingOrder = sortingOrder + 4;
                    v.rendFlash.Add(sr);
                }
                sr.transform.position = new Vector3(rp.x, rp.y, 0f);
                sr.enabled = true;

                // PC sâu xé: each dragon bite is a brief, violent flash that expands fast then fades.
                // Use a per-rend age (newest first) to drive scale and alpha.
                float ageIdx = v.rendFlashAges.Count > flashIdx ? v.rendFlashAges[flashIdx] : 0f;
                if (ageIdx < 0f) ageIdx = 0f;
                ageIdx += Time.deltaTime;
                if (v.rendFlashAges.Count <= flashIdx)
                    v.rendFlashAges.Add(ageIdx);
                else
                    v.rendFlashAges[flashIdx] = ageIdx;

                float life = Mathf.Clamp01(ageIdx / 0.35f);
                float scale = _scale * 0.07f * (1f + life * 3f);
                sr.transform.localScale = Vector3.one * scale;

                // Sâu xé color: blend from hot white-yellow to fiery red as it dies out.
                var hot = new Color(1f, 0.95f, 0.7f);
                var fire = new Color(1f, 0.35f, 0.1f);
                var c = Color.Lerp(hot, fire, life);
                c.a = Mathf.Lerp(1f, 0f, life);
                sr.color = c;

                flashIdx++;
            }
            // Hide unused flashes
            for (int i = flashIdx; i < v.rendFlash.Count; i++)
            {
                if (v.rendFlash[i] != null)
                    v.rendFlash[i].enabled = false;
            }
        }

        // ── Drawing primitives ───────────────────────────────────────────────

        private static void SetMissileVisible(RuntimeEffectVisual v, bool visible)
        {
            if (v.missileDot != null) v.missileDot.enabled = visible;
            if (v.pcMissiles != null)
                for (int i = 0; i < v.pcMissiles.Count; i++)
                    if (v.pcMissiles[i] != null) v.pcMissiles[i].enabled = visible;
        }

        private static void SetImpactVisible(RuntimeEffectVisual v, bool visible)
        {
            if (v.pcImpact != null) v.pcImpact.enabled = visible;
        }

        private static void SetPreCastVisible(RuntimeEffectVisual v, bool visible)
        {
            if (v.pcPreCast != null) v.pcPreCast.enabled = visible;
        }

        private static void Hide(LineRenderer line)
        {
            if (line != null) line.positionCount = 0;
        }

        private static void DrawLineSegment(LineRenderer line, Vector2 from, Vector2 to, Color color, float width)
        {
            if (line == null) return;
            line.widthMultiplier = width;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.1f);
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(from.x, from.y, 0f));
            line.SetPosition(1, new Vector3(to.x, to.y, 0f));
        }

        private static void DrawRing(LineRenderer line, Vector2 center, float radius, Color color, float width)
        {
            if (line == null) return;
            const int segments = 48;
            line.widthMultiplier = width;
            line.startColor = color;
            line.endColor = color;
            line.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, 0f));
            }
        }

        private static Sprite CreateDotSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float r = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / r;
                    float a = d <= 1f ? Mathf.Clamp01(1f - d * d) : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        }

        // ── Inner types ──────────────────────────────────────────────────────

        private sealed class RuntimeEffectVisual
        {
            public GameObject root;
            public LineRenderer preCastRing;
            public LineRenderer impactRing;
            public LineRenderer trail;
            public SpriteRenderer missileDot;
            public List<SpriteRenderer> pcMissiles;
            public SpriteRenderer pcImpact;
            public SpriteRenderer pcPreCast;
            public SpriteRenderer impactFlash;
            public List<SpriteRenderer> rendFlash = new();
            public List<float> rendFlashAges = new();
        }
    }
}
