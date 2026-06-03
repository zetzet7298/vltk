// -----------------------------------------------------------------------------
// VLTK Mobile — ST-02.1 Female Player Visual
// Mirror of MalePlayerVisual for WO_* SPR parts.
// Same layered SPR system, 8-direction animation, sorting.
// Source: PC npcres/woman SPR set.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Sprites;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Runtime renderer cho female player SPR set.
    /// Mirror of MalePlayerVisual với WO_* body parts.
    /// Tầng layer: shadow, body, head, hair, hands, weapon — giống hệt male.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FemalePlayerVisual : MonoBehaviour
    {
        [Header("Playback")]
        public PlayerVisualAction currentAction = PlayerVisualAction.Idle;
        public PcWeaponType currentWeapon = PcWeaponType.EmptyHand;
        [Range(0, FemalePlayerSpriteCatalog.DirectionCount - 1)]
        public int direction;
        public float idleFrameRate = 6f;
        public float moveFrameRate = 12f;
        public float magicFrameRate = 14.4f;
        public float attackFrameRate = 14.4f;
        public bool playAutomatically = true;

        [Header("SPR Placement")]
        public Vector2 referencePixel = new Vector2(160f, 200f);
        public float pixelsPerUnit = 1f;
        public string spritesRootOverride;

        [Header("Diagnostics")]
        public bool logMissingParts = true;

        private readonly Dictionary<PlayerSpritePartKind, PartRuntime> _parts = new();
        private PlayerVisualAction _loadedAction = (PlayerVisualAction)(-1);
        private PcWeaponType _loadedWeapon = (PcWeaponType)(-1);
        private float _time;

        public int LoadedPartCount { get; private set; }
        public int CurrentFrameInDirection { get; private set; }
        public bool HasAllRequiredParts { get; private set; }
        public Vector2 LastMoveInput { get; private set; }

        private sealed class PartRuntime
        {
            public PlayerSpritePartSpec spec;
            public SpriteRenderer renderer;
            public ClipRuntime clip;
        }

        private sealed class ClipRuntime
        {
            public string sourcePath;
            public int totalFrames;
            public int directionCount;
            public int framesPerDirection;
            public Sprite[] sprites;
            public Vector2[] offsets;
        }

        private static readonly Dictionary<string, ClipRuntime> ClipCache = new();
        private static readonly HashSet<string> MissingLogCache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticCaches()
        {
            ClipCache.Clear();
            MissingLogCache.Clear();
        }

        private static bool IsClipAlive(ClipRuntime clip)
        {
            if (clip == null || clip.sprites == null) return false;
            for (int i = 0; i < clip.sprites.Length; i++)
                if (clip.sprites[i] != null) return true;
            return false;
        }

        private void Awake()
        {
            RefreshActionParts(force: true);
            ApplyFrame(0f);
        }

        private void OnValidate()
        {
            idleFrameRate = Mathf.Max(0.1f, idleFrameRate);
            moveFrameRate = Mathf.Max(0.1f, moveFrameRate);
            pixelsPerUnit = Mathf.Max(0.01f, pixelsPerUnit);
            direction = Mathf.Clamp(direction, 0, FemalePlayerSpriteCatalog.DirectionCount - 1);
        }

        private void Update()
        {
            if (playAutomatically) Tick(Time.deltaTime);
        }

        public void SetMoveInput(Vector2 input)
        {
            LastMoveInput = Vector2.ClampMagnitude(input, 1f);
            int nextDirection = FemalePlayerSpriteCatalog.DirectionFromMove(LastMoveInput);
            if (nextDirection >= 0)
            {
                direction = nextDirection;
                SetAction(PlayerVisualAction.Move);
            }
            else
            {
                SetAction(PlayerVisualAction.Idle);
            }
        }

        public void SetAction(PlayerVisualAction action)
        {
            if (currentAction == action && _loadedAction == action && _loadedWeapon == currentWeapon)
                return;
            currentAction = action;
            _time = 0f;
            RefreshActionParts(force: true);
            ApplyFrame(0f);
        }

        public void SetWeapon(PcWeaponType weapon)
        {
            if (currentWeapon == weapon) return;
            currentWeapon = weapon;
            _loadedAction = (PlayerVisualAction)(-1);
            RefreshActionParts(force: true);
            ApplyFrame(0f);
        }

        public void SetDirection(int nextDirection)
        {
            direction = ((nextDirection % FemalePlayerSpriteCatalog.DirectionCount) + FemalePlayerSpriteCatalog.DirectionCount)
                        % FemalePlayerSpriteCatalog.DirectionCount;
            ApplySorting();
        }

        public void Tick(float deltaTime)
        {
            RefreshActionParts(force: false);
            _time += Mathf.Max(0f, deltaTime);
            ApplyFrame(_time);
        }

        public void RefreshActionParts(bool force = false)
        {
            if (!force && _loadedAction == currentAction && _loadedWeapon == currentWeapon)
                return;

            for (int i = transform.childCount - 1; i >= 0; i--)
                transform.GetChild(i).gameObject.SetActive(false);

            foreach (var part in _parts.Values)
                part.renderer.enabled = false;

            LoadedPartCount = 0;
            HasAllRequiredParts = true;
            var specs = FemalePlayerSpriteCatalog.BuildParts(currentAction, currentWeapon);
            foreach (var spec in specs)
            {
                var runtime = GetOrCreatePart(spec);
                runtime.spec = spec;
                runtime.clip = LoadClip(spec.sourcePath);
                bool ok = runtime.clip != null && runtime.clip.sprites != null && runtime.clip.sprites.Length > 0;
                runtime.renderer.enabled = ok;
                runtime.renderer.gameObject.SetActive(ok);
                if (ok) LoadedPartCount++;
                else if (spec.required) HasAllRequiredParts = false;
            }

            var tracked = new HashSet<GameObject>();
            foreach (var part in _parts.Values)
                if (part.renderer != null) tracked.Add(part.renderer.gameObject);
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var go = transform.GetChild(i).gameObject;
                if (!tracked.Contains(go)) Destroy(go);
            }

            _loadedAction = currentAction;
            _loadedWeapon = currentWeapon;
            ApplySorting();
        }

        private PartRuntime GetOrCreatePart(PlayerSpritePartSpec spec)
        {
            if (_parts.TryGetValue(spec.kind, out var runtime)) return runtime;
            var child = new GameObject($"Part_{(int)spec.kind}_{spec.name}");
            child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Default";
            runtime = new PartRuntime { spec = spec, renderer = renderer };
            _parts[spec.kind] = runtime;
            return runtime;
        }

        private void ApplyFrame(float time)
        {
            float rate = currentAction switch
            {
                PlayerVisualAction.Move => moveFrameRate,
                PlayerVisualAction.Magic => magicFrameRate,
                PlayerVisualAction.Attack => attackFrameRate,
                _ => idleFrameRate,
            };
            int baseOrder = MapRenderer.PlayerSortingOrder;

            foreach (var runtime in _parts.Values)
            {
                var clip = runtime.clip;
                if (clip == null || clip.framesPerDirection <= 0 || clip.sprites == null || clip.sprites.Length == 0)
                    continue;

                int frameInDirection = Mathf.FloorToInt(time * rate) % clip.framesPerDirection;
                if (frameInDirection < 0) frameInDirection += clip.framesPerDirection;
                CurrentFrameInDirection = frameInDirection;

                int dir = clip.directionCount > 1 ? direction % clip.directionCount : 0;
                int frameIndex = dir * clip.framesPerDirection + frameInDirection;
                frameIndex = Mathf.Clamp(frameIndex, 0, clip.sprites.Length - 1);

                runtime.renderer.sprite = clip.sprites[frameIndex];
                runtime.renderer.transform.localPosition = clip.offsets[frameIndex];
                runtime.renderer.sortingOrder = baseOrder + FemalePlayerSpriteCatalog.SortingOffset(runtime.spec.kind, direction);
            }
        }

        private void ApplySorting()
        {
            int baseOrder = MapRenderer.PlayerSortingOrder;
            foreach (var runtime in _parts.Values)
                runtime.renderer.sortingOrder = baseOrder + FemalePlayerSpriteCatalog.SortingOffset(runtime.spec.kind, direction);
        }

        private ClipRuntime LoadClip(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath)) return null;

            string root = string.IsNullOrEmpty(spritesRootOverride)
                ? Path.Combine(Application.streamingAssetsPath, "Sprites")
                : spritesRootOverride;
            string cacheKey = $"{root}|{sourcePath}|ppu={pixelsPerUnit:F3}|ref={referencePixel.x:F1},{referencePixel.y:F1}";
            if (ClipCache.TryGetValue(cacheKey, out var cached))
            {
                if (IsClipAlive(cached)) return cached;
                ClipCache.Remove(cacheKey);
            }

            byte[] data = ReadSprData(root, sourcePath);
            if (data == null)
            {
                LogMissing(sourcePath, "SPR file not staged");
                return null;
            }

            var decoded = SprDecoder.Decode(data);
            if (!decoded.success || decoded.header == null || decoded.frames == null || decoded.frames.Length == 0)
            {
                LogMissing(sourcePath, decoded.error ?? "SPR decode failed");
                return null;
            }

            int directions = Mathf.Max(1, decoded.header.directions);
            int totalFrames = decoded.frames.Length;
            int framesPerDirection = Mathf.Max(1, totalFrames / directions);
            if (totalFrames % directions != 0) directions = 1;

            var clip = new ClipRuntime
            {
                sourcePath = sourcePath,
                totalFrames = totalFrames,
                directionCount = directions,
                framesPerDirection = framesPerDirection,
                sprites = new Sprite[totalFrames],
                offsets = new Vector2[totalFrames],
            };

            for (int i = 0; i < totalFrames; i++)
            {
                var frame = decoded.frames[i];
                if (frame == null || frame.width == 0 || frame.height == 0) continue;
                var tex = SprDecoder.CreateTexture(frame);
                if (tex == null) continue;
                tex.name = $"FemalePlayer_{Path.GetFileNameWithoutExtension(sourcePath)}_{i:000}";
                clip.sprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0f, 1f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
                clip.sprites[i].name = tex.name;
                clip.offsets[i] = new Vector2(
                    (frame.offsetX - referencePixel.x) / pixelsPerUnit,
                    (referencePixel.y - frame.offsetY) / pixelsPerUnit);
            }

            ClipCache[cacheKey] = clip;
            SubsystemLog.Info("FemalePlayer", $"Loaded {sourcePath}: {totalFrames} frames, {directions} dirs");
            return clip;
        }

        private static byte[] ReadSprData(string spritesRoot, string sourcePath)
        {
            if (string.IsNullOrEmpty(spritesRoot) || string.IsNullOrEmpty(sourcePath)) return null;
            string uid = SprRuntimeService.ComputePathUidHex(sourcePath);
            if (!string.IsNullOrEmpty(uid))
            {
                string hashedPath = Path.Combine(spritesRoot, uid + ".spr");
                if (File.Exists(hashedPath)) return File.ReadAllBytes(hashedPath);
            }
            string fileName = Path.GetFileName(sourcePath.Replace('\\', Path.DirectorySeparatorChar));
            if (!string.IsNullOrEmpty(fileName))
            {
                string directPath = Path.Combine(spritesRoot, fileName);
                if (File.Exists(directPath)) return File.ReadAllBytes(directPath);
                string lowerPath = Path.Combine(spritesRoot, fileName.ToLowerInvariant());
                if (File.Exists(lowerPath)) return File.ReadAllBytes(lowerPath);
            }
            return null;
        }

        private void LogMissing(string sourcePath, string reason)
        {
            if (!logMissingParts) return;
            string key = sourcePath + "|" + reason;
            if (!MissingLogCache.Add(key)) return;
            SubsystemLog.Warn("FemalePlayer", $"{reason}: {sourcePath}");
        }
    }
}
