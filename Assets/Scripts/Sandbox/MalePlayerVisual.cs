// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
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
    /// Runtime renderer for the PC male player SPR set. It keeps every visible body
    /// part as its own SpriteRenderer so the avatar matches the original layered
    /// JX client: shadow, body, head, hair, hands, and empty-hand weapon layers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MalePlayerVisual : MonoBehaviour, IPlayerVisual
    {
        [Header("Playback")]
        public PlayerVisualAction currentAction { get; set; } = PlayerVisualAction.Idle;
        public bool walkMode { get; set; }   // PC walk mode (WK01) vs run (RN01).
        public bool isMeditating { get; set; } // PC 打坐 (ZZ01) forces Sit and blocks move-driven action changes.
        public PcWeaponType currentWeapon { get; set; } = PcWeaponType.EmptyHand;
        public bool isMounted { get; set; }
        public int direction { get; set; }
        public float idleFrameRate = 6f;
        public float moveFrameRate = 12f;
        public float magicFrameRate = 14.4f; // PC cdo_magic: ~16 frames/dir over 20 ticks at 18 ticks/sec.
        public float attackFrameRate = 14.4f;
        public bool playAutomatically { get; set; } = true;

        [Header("SPR Placement")]
        [Tooltip("SPR canvas reference point that should sit on the player's feet/world position.")]
        public Vector2 referencePixel = new Vector2(160f, 200f);
        public float pixelsPerUnit = 1f;
        public string spritesRootOverride;

        [Header("Diagnostics")]
        public bool logMissingParts = true;

        [Header("Equipment Variants")]
        public int armorVariant = 19;
        public int headVariant = 19;
        public int hairVariant = 19;
        public int weaponVariant = 0;
        public int mountHorseVariant = 19;

        private int _loadedArmorVariant = -1;
        private int _loadedHeadVariant = -1;
        private int _loadedHairVariant = -1;
        private int _loadedWeaponVariant = -1;
        private int _loadedHorseVariant = -1;

        private readonly Dictionary<PlayerSpritePartKind, PartRuntime> _parts = new();
        private PlayerVisualAction _loadedAction = (PlayerVisualAction)(-1);
        private PcWeaponType _loadedWeapon = (PcWeaponType)(-1);
        private readonly List<string> _lastMissingRequiredParts = new();
        private float _time;

        public int LoadedPartCount { get; private set; }
        public int CurrentFrameInDirection { get; private set; }
        public float CurrentPlaybackRate => ResolvePlaybackRate(currentAction);
        public bool HasAllRequiredParts { get; private set; }
        public int MissingRequiredPartCount => LastMissingRequiredParts.Count;
        public IReadOnlyList<string> LastMissingRequiredParts => _lastMissingRequiredParts;
        public Vector2 LastMoveInput { get; private set; }
        public bool IsMounted => isMounted;

        public int GetCurrentDirection() => direction;
        public int GetRiderSortingOrder() => MapRenderer.PlayerSortingOrder + MalePlayerSpriteCatalog.SortingOffset(PlayerSpritePartKind.Body, direction);

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

        // Runtime Sprites/Textures created in LoadClip are destroyed when play mode stops.
        // With Domain Reload disabled (fast enter play mode) the static caches would survive
        // and hand out destroyed (fake-null) sprites on the next play, making the player
        // invisible. SubsystemRegistration runs on every play start regardless of the
        // domain-reload setting, so clearing here guarantees a fresh decode each session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticCaches()
        {
            ClipCache.Clear();
            MissingLogCache.Clear();
        }

        private static bool IsClipAlive(ClipRuntime clip)
        {
            if (clip == null || clip.sprites == null)
                return false;
            for (int i = 0; i < clip.sprites.Length; i++)
            {
                if (clip.sprites[i] != null)
                    return true;
            }
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
            magicFrameRate = Mathf.Max(0.1f, magicFrameRate);
            pixelsPerUnit = Mathf.Max(0.01f, pixelsPerUnit);
            direction = Mathf.Clamp(direction, 0, MalePlayerSpriteCatalog.DirectionCount - 1);
        }

        private void Update()
        {
            if (playAutomatically)
                Tick(Time.deltaTime);
        }

        public void SetMoveInput(Vector2 input)
        {
            LastMoveInput = Vector2.ClampMagnitude(input, 1f);
            // PC 打坐: meditation locks the visual to Sit (ZZ01) regardless of input.
            if (isMeditating)
            {
                SetAction(PlayerVisualAction.Sit);
                return;
            }
            int nextDirection = MalePlayerSpriteCatalog.DirectionFromMove(LastMoveInput);
            if (nextDirection >= 0)
            {
                // PC 走路/跑步: walk mode plays Walk (WK01), otherwise run (RN01 Move).
                SetAction(walkMode ? PlayerVisualAction.Walk : PlayerVisualAction.Move);
                direction = nextDirection;
            }
            else
            {
                SetAction(PlayerVisualAction.Idle);
            }
        }

        public void SetAction(PlayerVisualAction action)
        {
            // PC 打坐 (meditate) is sticky: once meditating, force Sit (ZZ01) until meditation ends.
            if (isMeditating)
                action = PlayerVisualAction.Sit;
            // When mounted, map on-foot actions to PC mounted equivalents:
            // Walk -> RideWalk (HW01), Move -> RideMove (HR01), idle -> Ride (RD01).
            if (isMounted)
                action = action == PlayerVisualAction.Walk ? PlayerVisualAction.RideWalk
                    : action == PlayerVisualAction.Move ? PlayerVisualAction.RideMove
                    : PlayerVisualAction.Ride;
            if (currentAction == action && _loadedAction == action && _loadedWeapon == currentWeapon)
                return;

            currentAction = action;
            _time = 0f;
            RefreshActionParts(force: true);
            ApplyFrame(0f);
        }

        /// <summary>
        /// Toggle mounted state. When mounted, the rider renders the HM01 layered
        /// set (BD/HD/HB) and a separate HorseVisual drives the horse body sprite.
        /// On dismount the visual returns to the on-foot action previously used.
        /// </summary>
        public void SetMounted(bool mounted)
        {
            if (isMounted == mounted) return;
            isMounted = mounted;
            _loadedAction = (PlayerVisualAction)(-1);
            if (isMounted)
                currentAction = (LastMoveInput.sqrMagnitude > 0.0001f)
                    ? (walkMode ? PlayerVisualAction.RideWalk : PlayerVisualAction.RideMove)
                    : PlayerVisualAction.Ride;
            else
                currentAction = Vector2.zero == LastMoveInput ? PlayerVisualAction.Idle : PlayerVisualAction.Move;
            _time = 0f;
            RefreshActionParts(force: true);
            ApplyFrame(0f);
        }

        public void SetWeapon(PcWeaponType weapon)
        {
            if (currentWeapon == weapon)
                return;
            currentWeapon = weapon;
            weaponVariant = MalePlayerSpriteCatalog.GetWeaponSprVariant(weapon);
            _loadedAction = (PlayerVisualAction)(-1);
            RefreshActionParts(force: true);
            ApplyFrame(0f);
        }

        public void SetDirection(int nextDirection)
        {
            direction = ((nextDirection % MalePlayerSpriteCatalog.DirectionCount) + MalePlayerSpriteCatalog.DirectionCount)
                        % MalePlayerSpriteCatalog.DirectionCount;
            ApplySorting();
        }

        public void SetEquipVariant(PlayerEquipSlot slot, int variant)
        {
            switch (slot)
            {
                case PlayerEquipSlot.Body:
                    armorVariant = variant;
                    break;
                case PlayerEquipSlot.Head:
                    headVariant = variant;
                    break;
                case PlayerEquipSlot.Hair:
                    hairVariant = variant;
                    break;
                case PlayerEquipSlot.Weapon:
                    weaponVariant = variant;
                    break;
                case PlayerEquipSlot.Mount:
                    mountHorseVariant = variant;
                    break;
            }
            RefreshActionParts(force: true);
            ApplyFrame(0f);
        }

        public void Tick(float deltaTime)
        {
            RefreshActionParts(force: false);
            _time += Mathf.Max(0f, deltaTime);
            ApplyFrame(_time);
        }

        public void RefreshActionParts(bool force = false)
        {
            if (!force && _loadedAction == currentAction && _loadedWeapon == currentWeapon &&
                _loadedArmorVariant == armorVariant && _loadedHeadVariant == headVariant &&
                _loadedHairVariant == hairVariant && _loadedWeaponVariant == weaponVariant &&
                _loadedHorseVariant == mountHorseVariant)
                return;

            // Disable ALL existing part children (including orphans from prior actions
            // that are no longer tracked in _parts) to prevent ghost duplicates.
            for (int i = transform.childCount - 1; i >= 0; i--)
                transform.GetChild(i).gameObject.SetActive(false);

            foreach (var part in _parts.Values)
                part.renderer.enabled = false;

            LoadedPartCount = 0;
            HasAllRequiredParts = true;
            _lastMissingRequiredParts.Clear();
            var specs = MalePlayerSpriteCatalog.BuildParts(currentAction, currentWeapon, armorVariant, headVariant, weaponVariant, hairVariant, mountHorseVariant);
            foreach (var spec in specs)
            {
                var runtime = GetOrCreatePart(spec);
                runtime.spec = spec;
                runtime.clip = LoadClip(spec.sourcePath, spec.expectedDirections);
                bool loaded = runtime.clip != null && runtime.clip.sprites != null && runtime.clip.sprites.Length > 0;
                // Part-count model (NOT staging): a slot only counts toward
                // LoadedPartCount and renders when the PC art genuinely exists for
                // this gender/config, which the catalog encodes via spec.required.
                // A non-required slot whose {uid}.spr happens to resolve (e.g. an
                // orphan staged from an out-of-scope tree) must NOT inflate the
                // count nor paint a phantom layer. This keeps the invariant
                // LoadedPartCount == number of visible part renderers, independent
                // of which SPR files are present on disk.
                bool show = loaded && spec.required;
                runtime.renderer.enabled = show;
                runtime.renderer.gameObject.SetActive(show);
                if (show)
                    LoadedPartCount++;
                else if (spec.required)
                {
                    HasAllRequiredParts = false;
                    _lastMissingRequiredParts.Add(spec.sourcePath);
                }
            }

            // Destroy orphan children that are no longer tracked.
            var tracked = new HashSet<GameObject>();
            foreach (var part in _parts.Values)
                if (part.renderer != null) tracked.Add(part.renderer.gameObject);
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var go = transform.GetChild(i).gameObject;
                if (!tracked.Contains(go))
                    Destroy(go);
            }

            _loadedAction = currentAction;
            _loadedWeapon = currentWeapon;
            _loadedArmorVariant = armorVariant;
            _loadedHeadVariant = headVariant;
            _loadedHairVariant = hairVariant;
            _loadedWeaponVariant = weaponVariant;
            _loadedHorseVariant = mountHorseVariant;
            ApplySorting();
        }

        private PartRuntime GetOrCreatePart(PlayerSpritePartSpec spec)
        {
            if (_parts.TryGetValue(spec.kind, out var runtime))
                return runtime;

            var child = new GameObject($"Part_{(int)spec.kind}_{spec.name}");
            child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Default";
            runtime = new PartRuntime { spec = spec, renderer = renderer };
            _parts[spec.kind] = runtime;
            return runtime;
        }

        private float ResolvePlaybackRate(PlayerVisualAction action)
        {
            return action switch
            {
                PlayerVisualAction.Move => moveFrameRate,
                PlayerVisualAction.Walk => moveFrameRate * 0.55f, // PC walk mode: slower cadence than run.
                PlayerVisualAction.RideWalk => moveFrameRate * 0.55f,
                PlayerVisualAction.RideMove => moveFrameRate,
                PlayerVisualAction.Magic => magicFrameRate,
                PlayerVisualAction.Attack => attackFrameRate,
                PlayerVisualAction.Jump => magicFrameRate, // PC 跳跃 leap — single burst cycle.
                _ => idleFrameRate, // Idle, Sit (打坐), Ride use idle cadence.
            };
        }

        private void ApplyFrame(float time)
        {
            float rate = ResolvePlaybackRate(currentAction);
            int baseOrder = PlayerBaseSortingOrder();

            foreach (var runtime in _parts.Values)
            {
                var clip = runtime.clip;
                if (clip == null || clip.framesPerDirection <= 0 || clip.sprites == null || clip.sprites.Length == 0)
                    continue;

                // PC 打坐 (Sit): the meditation cycle is a one-shot sit-down that HOLDS on the final
                // seated frame once the character is on the ground — it does not loop back to standing.
                // PC 跳跃 (Jump): the leap is a single forward burst; hold the last frame until the dash ends.
                int frameInDirection;
                if ((currentAction == PlayerVisualAction.Sit || currentAction == PlayerVisualAction.Jump) && clip.framesPerDirection > 0)
                {
                    int lastFrame = clip.framesPerDirection - 1;
                    int computed = Mathf.FloorToInt(time * rate);
                    frameInDirection = (computed < lastFrame) ? computed : lastFrame;
                }
                else
                {
                    frameInDirection = Mathf.FloorToInt(time * rate) % clip.framesPerDirection;
                    if (frameInDirection < 0) frameInDirection += clip.framesPerDirection;
                }
                CurrentFrameInDirection = frameInDirection;

                int dir = clip.directionCount > 1 ? direction % clip.directionCount : 0;
                int frameIndex = dir * clip.framesPerDirection + frameInDirection;
                frameIndex = Mathf.Clamp(frameIndex, 0, clip.sprites.Length - 1);

                runtime.renderer.sprite = clip.sprites[frameIndex];
                runtime.renderer.transform.localPosition = clip.offsets[frameIndex];
                runtime.renderer.sortingOrder = baseOrder + MalePlayerSpriteCatalog.SortingOffset(runtime.spec.kind, direction);
            }
        }

        // Actors sit just above the static map-art layer (MapRenderer.ObjectSortingOrder)
        // so the player stays visible in dense town centers. Depth among nearby sprites is
        // handled by the camera's CustomAxis world-Y sort, so no screen-Y encoding into the
        // (int16) sortingOrder is needed -- that overflow is exactly what broke the map art.
        private int PlayerBaseSortingOrder()
        {
            return MapRenderer.PlayerSortingOrder;
        }

        private void ApplySorting()
        {
            int baseOrder = PlayerBaseSortingOrder();
            foreach (var runtime in _parts.Values)
            {
                runtime.renderer.sortingOrder = baseOrder + MalePlayerSpriteCatalog.SortingOffset(runtime.spec.kind, direction);
            }
        }

        private ClipRuntime LoadClip(string sourcePath, int expectedDirections = 0)
        {
            if (string.IsNullOrEmpty(sourcePath))
                return null;

            string root = string.IsNullOrEmpty(spritesRootOverride)
                ? Path.Combine(Application.streamingAssetsPath, "Sprites")
                : spritesRootOverride;
            string cacheKey = $"{root}|{sourcePath}|ppu={pixelsPerUnit:F3}|ref={referencePixel.x:F1},{referencePixel.y:F1}|dir={expectedDirections}";
            if (ClipCache.TryGetValue(cacheKey, out var cached))
            {
                if (IsClipAlive(cached))
                    return cached;
                // Cached clip's sprites were destroyed on a previous stop -> re-decode.
                ClipCache.Remove(cacheKey);
            }

            byte[] data = ReadSprData(root, sourcePath);
            if (data == null)
            {
                LogMissing(sourcePath, "SPR file not staged in StreamingAssets/Sprites");
                return null;
            }

            var decoded = SprDecoder.Decode(data);
            if (!decoded.success || decoded.header == null || decoded.frames == null || decoded.frames.Length == 0)
            {
                LogMissing(sourcePath, decoded.error ?? "SPR decode failed");
                return null;
            }

            int totalFrames = decoded.frames.Length;
            // SPR header directions không đáng tin với vài file (HH/HT báo dirs=1 dù
            // thực có 8). Ưu tiên hint expectedDirections nếu chia hết totalFrames.
            int directions = Mathf.Max(1, decoded.header.directions);
            if (expectedDirections > 1 && totalFrames % expectedDirections == 0)
                directions = expectedDirections;
            int framesPerDirection = Mathf.Max(1, totalFrames / directions);
            if (totalFrames % directions != 0)
                directions = 1;

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
                if (frame == null || frame.width == 0 || frame.height == 0)
                    continue;

                var tex = SprDecoder.CreateTexture(frame);
                if (tex == null)
                    continue;

                tex.name = $"MalePlayer_{Path.GetFileNameWithoutExtension(sourcePath)}_{i:000}";
                clip.sprites[i] = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0f, 1f),
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                clip.sprites[i].name = tex.name;

                clip.offsets[i] = new Vector2(
                    (frame.offsetX - referencePixel.x) / pixelsPerUnit,
                    (referencePixel.y - frame.offsetY) / pixelsPerUnit);
            }

            ClipCache[cacheKey] = clip;
            SubsystemLog.Info("MalePlayer", $"Loaded {sourcePath}: {totalFrames} frames, {directions} dirs");
            return clip;
        }

        private static byte[] ReadSprData(string spritesRoot, string sourcePath)
        {
            if (string.IsNullOrEmpty(spritesRoot) || string.IsNullOrEmpty(sourcePath))
                return null;

            string uid = SprRuntimeService.ComputePathUidHex(sourcePath);
            if (!string.IsNullOrEmpty(uid))
            {
                string hashedPath = Path.Combine(spritesRoot, uid + ".spr");
                if (File.Exists(hashedPath))
                    return File.ReadAllBytes(hashedPath);
            }

            string fileName = Path.GetFileName(sourcePath.Replace('\\', Path.DirectorySeparatorChar));
            if (!string.IsNullOrEmpty(fileName))
            {
                string directPath = Path.Combine(spritesRoot, fileName);
                if (File.Exists(directPath))
                    return File.ReadAllBytes(directPath);

                string lowerPath = Path.Combine(spritesRoot, fileName.ToLowerInvariant());
                if (File.Exists(lowerPath))
                    return File.ReadAllBytes(lowerPath);
            }

            return null;
        }

        private void LogMissing(string sourcePath, string reason)
        {
            if (!logMissingParts)
                return;
            string key = sourcePath + "|" + reason;
            if (!MissingLogCache.Add(key))
                return;
            SubsystemLog.Warn("MalePlayer", $"{reason}: {sourcePath}");
        }
    }
}
