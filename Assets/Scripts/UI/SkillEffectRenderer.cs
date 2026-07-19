using System;
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
        // Per-key header center + per-frame offset (for PC body-aura frame-offset animation).
        private readonly Dictionary<string, (int centerX, int centerY, Vector2[] frameOffsets)> _pcSpriteFrameData = new();

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

        public static bool ShouldDrawFallbackPreCastCircle(ActiveSkillEffect fx) => fx != null && !fx.isAura;

        private void DrawPreCast(ActiveSkillEffect fx)
        {
            // (Removed pcAuraFrameStart/End sub-range; loops full frame range)
            if (fx != null && fx.isAura)
            {
                if (fx.HasPcPreCastSprite)
                    DrawPcAuraSprite(fx);
                return;
            }

            if (!ShouldDrawFallbackPreCastCircle(fx)) return;

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
            // [DEBUG 2026-07-16] KangLong visual: log render entry.
            if (fx.skillId == 128)
                SubsystemLog.Info("SkillFx", $"[KangLongDraw] skill={fx.skillId} missileCount={fx.missileCount} positionsLen={fx.missilePositions?.Length ?? -1} " +
                    $"originsLen={fx.missileOrigins?.Length ?? -1} targetsLen={fx.missileTargets?.Length ?? -1} " +
                    $"pos0={(fx.missilePositions != null && fx.missilePositions.Length > 0 ? fx.missilePositions[0].ToString() : "<none>")} " +
                    $"posLast={(fx.missilePositions != null && fx.missilePositions.Length > 1 ? fx.missilePositions[fx.missilePositions.Length - 1].ToString() : "<none>")}");
            // PC SPR missiles are rendered by SkillEffectWorldOverlay (SpriteRenderer, world-space).
            // IMGUI overlay must NOT draw them — doing so causes a blurry double-image because
            // WorldOverlay draws at PPU=1f world-scale while IMGUI draws at native pixel size.
            if (fx.HasPcMissileSprite)
                return;

            // Fallback: legacy circle/dot rendering (no PC SPR configured or resolved)
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
                Vector2 direction = fx.ResolveMissileDirection(i);

                var sprite = SelectPcMissileFrame(fx, sprites, missilePos, missilePos + direction);
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
            int pcDir64 = ComputePcDirection64(fromPos, targetPos);
            int lifeTick = Mathf.Max(0, Mathf.FloorToInt((fx.elapsed - fx.phaseStart) * 18f));
            int frameIndex = ComputePcMissileFrameIndex(pcDir64, fx.pcMissileTotalFrames,
                fx.pcMissileDirections, lifeTick, fx.pcMissileIntervalTicks);
            frameIndex = Mathf.Clamp(frameIndex, 0, sprites.Length - 1);
            return sprites[frameIndex];
        }

        // PC source path: g_GetDirIndex(...) yields nDir [0,63], then KMissleRes
        // maps that raw value to SPR directions. Keep all 64 buckets until that map.
        private static int ComputePcSpriteDirection(Vector2 from, Vector2 to, int spriteDirections)
            => MapPc64Direction(ComputePcDirection64(from, to), spriteDirections);

        // Exact PC KMath.cpp g_nSinBuffer[0..31], fixed-point 1024.
        // g_GetDirIdxForFindPath only scans this descending half of g_nSin.
        private static readonly int[] PcScanSin =
        {
            1024, 1019, 1004, 979, 946, 903, 851, 791,
            724, 649, 568, 482, 391, 297, 199, 100,
            0, -100, -199, -297, -391, -482, -568, -649,
            -724, -791, -851, -903, -946, -979, -1004, -1019,
        };

        // Exact PC KMath.h qsqrt table. CSV SHA-256: a4032c8b5461213d4053c9c451e786f143c3a76dce73545c72e01c3bead53deb.
        private static readonly int[] PcSqrtTable =
        {
            531980127, 532026288, 532072271, 532118079, 532163712, 532209174, 532254465, 532299589,
            532344546, 532389339, 532433970, 532478440, 532522750, 532566903, 532610900, 532654744,
            532698434, 532741974, 532785365, 532828607, 532871704, 532914655, 532957463, 533000129,
            533042654, 533085041, 533127289, 533169401, 533211378, 533253220, 533294931, 533336509,
            533377958, 533419278, 533460470, 533501535, 533542475, 533583291, 533623984, 533664554,
            533705004, 533745334, 533785545, 533825638, 533865615, 533905476, 533945222, 533984855,
            534024374, 534063782, 534103079, 534142267, 534181345, 534220315, 534259178, 534297934,
            534336585, 534375132, 534413574, 534451914, 534490152, 534528288, 534566324, 534604260,
            534642098, 534679837, 534717478, 534755023, 534792473, 534829827, 534867086, 534904252,
            534941325, 534978305, 535015194, 535051992, 535088699, 535125317, 535161846, 535198287,
            535234640, 535270905, 535307085, 535343178, 535379187, 535415110, 535450950, 535486706,
            535522379, 535557970, 535593480, 535628908, 535664255, 535699523, 535734711, 535769820,
            535804850, 535839803, 535874678, 535909476, 535944198, 535978844, 536013414, 536047910,
            536082331, 536116678, 536150952, 536185153, 536219281, 536253337, 536287322, 536321235,
            536355078, 536388850, 536422553, 536456186, 536489750, 536523246, 536556673, 536590033,
            536623325, 536656551, 536689709, 536722802, 536755829, 536788791, 536821688, 536854520,
            536887280, 536919921, 536952436, 536984827, 537017094, 537049241, 537081267, 537113174,
            537144963, 537176637, 537208195, 537239640, 537270972, 537302193, 537333304, 537364306,
            537395200, 537425987, 537456669, 537487246, 537517720, 537548091, 537578361, 537608530,
            537638600, 537668572, 537698446, 537728224, 537757906, 537787493, 537816986, 537846387,
            537875696, 537904913, 537934040, 537963078, 537992027, 538020888, 538049662, 538078350,
            538106952, 538135470, 538163903, 538192254, 538220521, 538248707, 538276812, 538304837,
            538332781, 538360647, 538388434, 538416144, 538443776, 538471332, 538498812, 538526217,
            538553548, 538580804, 538607987, 538635097, 538662136, 538689102, 538715997, 538742822,
            538769577, 538796263, 538822880, 538849428, 538875909, 538902322, 538928668, 538954949,
            538981163, 539007312, 539033396, 539059416, 539085373, 539111265, 539137095, 539162863,
            539188568, 539214212, 539239794, 539265316, 539290778, 539316180, 539341522, 539366806,
            539392031, 539417197, 539442306, 539467358, 539492352, 539517290, 539542171, 539566997,
            539591768, 539616483, 539641143, 539665749, 539690301, 539714800, 539739245, 539763637,
            539787976, 539812264, 539836499, 539860682, 539884815, 539908896, 539932927, 539956907,
            539980838, 540004718, 540028549, 540052332, 540076065, 540099750, 540123387, 540146976,
            540170517, 540194011, 540217458, 540240858, 540264211, 540287519, 540310780, 540333996,
        };

        private const int PcQsqrtBiasBits = ((23 + 127) << 23) + (1 << 22);
        private static readonly float PcQsqrtBias = BitConverter.Int32BitsToSingle(PcQsqrtBiasBits);

        // KMath.h g_GetDirIdxForFindPath: integer position endpoints, qsqrt distance,
        // fixed-point sine scan, then mirror for positive X. Same position is -1.
        internal static int ComputePcDirection64(Vector2 from, Vector2 to)
            => ComputePcDirection64FromInts(Mathf.RoundToInt(from.x), Mathf.RoundToInt(from.y),
                Mathf.RoundToInt(to.x), Mathf.RoundToInt(to.y));

        internal static int ComputePcDirection64FromInts(int fromX, int fromY, int toX, int toY)
        {
            if (fromX == toX && fromY == toY) return -1;

            int dx = toX - fromX;
            int dy = toY - fromY;
            int distance = ComputePcDistance(dx, dy);
            if (distance == 0) return -1;

            int sin = (dy << 10) / distance;
            sin = Mathf.Clamp(sin, -1024, 1024);
            int direction = -1;
            for (int i = 0; i < PcScanSin.Length; i++)
            {
                if (sin > PcScanSin[i]) break;
                direction = i;
            }
            return dx > 0 ? 63 - direction : direction;
        }

        private static int ComputePcDistance(int dx, int dy)
        {
            int squaredDistance = dx * dx + dy * dy;
            float root = ComputePcQsqrt(squaredDistance);
            return BitConverter.SingleToInt32Bits(root + PcQsqrtBias) - PcQsqrtBiasBits;
        }

        private static float ComputePcQsqrt(float value)
        {
            int bits = BitConverter.SingleToInt32Bits(value);
            int exponent = (bits >> 1) & 0x3f800000;
            int tableIndex = (bits >> 16) & 0xff;
            return BitConverter.Int32BitsToSingle(exponent + PcSqrtTable[tableIndex]);
        }

        // PC KMissleRes direction conversion: width=64/nSprDir; round half up; wrap.
        private static int MapPc64Direction(int pcDir64, int spriteDirections)
        {
            int directions = Mathf.Max(1, spriteDirections);
            int nDir = pcDir64 & 63;
            int width = 64 / directions;
            int imageDir = nDir / width;
            if (nDir % width >= 32 / directions) imageDir++;
            return imageDir % directions;
        }

        private static int ComputePcMissileFrameIndex(int pcDir64, int totalFrames,
            int spriteDirections, int lifeTick, int intervalTicks)
        {
            int directions = Mathf.Max(1, spriteDirections);
            int framePerDir = Mathf.Max(1, totalFrames / directions);
            int localFrame = (Mathf.Max(0, lifeTick) / Mathf.Max(1, intervalTicks)) % framePerDir;
            return MapPc64Direction(pcDir64, directions) * framePerDir + localFrame;
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
            // PC SPR impact sprites are rendered by SkillEffectWorldOverlay.
            // Skip IMGUI path to avoid double-rendering ghost.
            if (fx.HasPcImpactSprite)
                return;

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
            int localFrame;
            if (fx.pcStationaryLifetimeOverride && fx.pcMissileLifeTicks > 0)
            {
                // PC non-loop stationary missiles stretch their finite SPR sequence
                // across the missile lifetime (e.g. missile 359: 19 frames/31 ticks:
                // tick 0 -> frame 0, tick 18 -> frame 11, tick 30 -> frame 18).
                int clampedTick = Mathf.Clamp(lifeTick, 0, fx.pcMissileLifeTicks - 1);
                localFrame = Mathf.Min(framePerDir - 1,
                    Mathf.FloorToInt(clampedTick * (float)framePerDir / fx.pcMissileLifeTicks));
            }
            else
            {
                localFrame = (lifeTick / Mathf.Max(1, fx.pcImpactIntervalTicks)) % framePerDir;
            }
            int frameIndex = Mathf.Clamp(localFrame, 0, sprites.Length - 1);

            var sprite = sprites[frameIndex] ?? sprites[0];
            if (sprite != null) DrawSpriteScreen(sprite, fx.targetPos);
        }

        /// <summary>
        /// Render a looping body-aura SPR (e.g. Túy Điệp butterfly, PC StateSpecial 43).
        /// PC source: 状态与光效图形对照表 Status entry — PlayMode=Loop over sub-range
        /// (主角身后开始帧..结束帧), Type=Body. The sprite follows the live player position.
        /// Each frame applies KSprite::DrawAlpha offset so golden dots fly/swirl around player.
        /// </summary>
        private void DrawPcAuraSprite(ActiveSkillEffect fx)
        {
            var sprites = LoadPcSprites(fx.pcPreCastSpriteKey);
            if (sprites == null || sprites.Length == 0)
                return;

            int lo = fx.pcAuraFrameStart;
            int hi = fx.pcAuraFrameEnd > 0 ? fx.pcAuraFrameEnd : sprites.Length - 1;
            int interval = Mathf.Max(1, fx.pcPreCastIntervalTicks);

            int lifeTick = Mathf.Max(0, Mathf.FloorToInt(fx.elapsed * 18f));
            int entryTicks = lo * interval;
            int frameIndex;

            if (lifeTick < entryTicks)
            {
                int entryFrame = lifeTick / interval;
                frameIndex = Mathf.Clamp(entryFrame, 0, sprites.Length - 1);
            }
            else
            {
                int loopSpan = Mathf.Max(1, hi - lo + 1);
                int loopTick = lifeTick - entryTicks;
                int local = (loopTick / interval) % loopSpan;
                frameIndex = Mathf.Clamp(lo + local, 0, sprites.Length - 1);
            }

            // PC: KSprite::DrawAlpha draws SPR at native pixel size (ppu=1).
            // No extra scaling — camera zoom + screen res handle visibility.
            float auraScale = 1f;
            Vector2 basePos = ResolveLiveCasterPos(fx);

            float yOffset = 0f;
            bool isMounted = fx.hasStateSourceKey && fx.stateOwnerMounted;
            if (!fx.hasStateSourceKey)
            {
                var player = SandboxManager.Instance?.PlayerController;
                if (player != null && player.visual != null)
                    isMounted = player.visual.IsMounted;
            }

            if (fx.stateAuraPos == 1) // Head
            {
                yOffset = 10f;
                if (isMounted) yOffset += 38f;
            }
            else if (fx.stateAuraPos == 2) // Feet
            {
                yOffset = 0f;
            }
            else // Body (default)
            {
                yOffset = 0f;
                if (isMounted) yOffset += 38f;
            }

            basePos.y += yOffset;

            Vector2 auraOffset = GetPcAuraFrameWorldOffset(fx, frameIndex, 1f);
            var sprite = sprites[frameIndex] ?? sprites[0];
            if (sprite != null) DrawAuraSpriteScaled(sprite, basePos + auraOffset, auraScale);
        }

        /// <summary>
        /// Draw the body-aura SPR at native screen size matching PC.
        /// tex.width * auraScale * worldToScreenScale screen pixels.
        /// auraScale=1.0 means native size; camera zoom + screen res handle visibility.
        /// </summary>
        private void DrawAuraSpriteScaled(Sprite sprite, Vector2 worldPos, float auraScale)
        {
            if (sprite == null) return;
            var tex = sprite.texture;
            if (tex == null) return;

            var screenPos = WorldToScreen(worldPos);

            // WorldOverlay equivalent world size: texSize * auraScale
            // Screen size at this ortho: worldSize * (screenHeight / (orthoSize * 2))
            float orthoH = _camera.orthographicSize * 2f;
            float worldToScreenScale = Screen.height / orthoH;
            float w = tex.width * auraScale * worldToScreenScale;
            float h = tex.height * auraScale * worldToScreenScale;

            // Center using PC pivot (same calculation as DrawSpriteScreen)
            float px = sprite.pivot.x / Mathf.Max(1f, tex.width);
            float py = sprite.pivot.y / Mathf.Max(1f, tex.height);
            float drawX = screenPos.x - w * px;
            float drawY = screenPos.y - h * (1f - py);

            GUI.DrawTexture(new Rect(drawX, drawY, w, h), tex);
        }

        private string ResolvePcSpritePath(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            string spritesRoot = Path.Combine(Application.dataPath, "..", "SpritesRuntime");
            string keyWithExt = key.EndsWith(".spr") ? key : key + ".spr";
            string path = Path.Combine(spritesRoot, keyWithExt);
            if (File.Exists(path)) return path;

            string fileNameOnly = Path.GetFileName(keyWithExt.Replace('\\', '/'));
            string fallbackPath = Path.Combine(spritesRoot, fileNameOnly);
            if (File.Exists(fallbackPath)) return fallbackPath;

            // Fallback to signed hash file on disk
            string signedUid = VLTK.Sprites.SprRuntimeService.ComputePathUidHex(key, signedBytes: true);
            string hashPath = signedUid != null ? Path.Combine(spritesRoot, signedUid + ".spr") : null;
            if (hashPath != null && File.Exists(hashPath)) return hashPath;

            // Fallback to unsigned hash file on disk
            string unsignedUid = VLTK.Sprites.SprRuntimeService.ComputePathUidHex(key, signedBytes: false);
            string unsignedHashPath = unsignedUid != null ? Path.Combine(spritesRoot, unsignedUid + ".spr") : null;
            if (unsignedHashPath != null && File.Exists(unsignedHashPath)) return unsignedHashPath;

            return null;
        }

        private Vector2 GetPcAuraFrameWorldOffset(ActiveSkillEffect fx, int frameIndex, float scale)
        {
            // Pivot on Sprite already handles the PC frame offsets correctly by aligning center and frames.
            // Adding a manual offset shifts the sprite twice, causing misalignment. Return Vector2.zero.
            return Vector2.zero;
        }

        /// <summary>Live caster position so body-aura buffs follow the player.</summary>
        private static Vector2 ResolveLiveCasterPos(ActiveSkillEffect fx)
        {
            if (fx?.getCurrentTargetPos != null)
                return fx.getCurrentTargetPos();
            if (fx != null && fx.hasStateSourceKey)
                return fx.targetPos;
            var player = SandboxManager.Instance?.PlayerController;
            if (player != null)
                return (Vector2)player.transform.position;
            return fx?.casterPos ?? Vector2.zero;
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

            string path = ResolvePcSpritePath(key);
            if (path == null)
            {
                string signedUid = VLTK.Sprites.SprRuntimeService.ComputePathUidHex(key, signedBytes: true);
                string unsignedUid = VLTK.Sprites.SprRuntimeService.ComputePathUidHex(key, signedBytes: false);
                SubsystemLog.Warn("Combat", $"PC skill SPR missing: {key} (signedHash={signedUid}, unsignedHash={unsignedUid})");
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
                float pivotX = 0.5f;
                float pivotY = 0.5f;
                if (frame.width > 0)
                    pivotX = (decoded.header.centerX - frame.offsetX) / (float)frame.width;
                if (frame.height > 0)
                    pivotY = (frame.height - (decoded.header.centerY - frame.offsetY)) / (float)frame.height;
                sprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(pivotX, pivotY), 1f);
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
    }
}
