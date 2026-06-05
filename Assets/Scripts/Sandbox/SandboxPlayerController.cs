// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Runtime player driver cho sandbox/mobile client. Joystick input applied
    /// as continuous world movement và forwarded đến <see cref="IPlayerVisual"/>
    /// (MalePlayerVisual hoặc FemalePlayerVisual) để chạy đúng 8-way animation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SandboxPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 900f; // TEMP: 5x normal (180) for movement testing
        public bool allowKeyboardFallback = true;

        [Header("Player Gender")]
        [Tooltip("True = female (FM_*), false = male (MA_*). Set trước Awake.")]
        public bool isFemale;

        [Header("Wiring")]
        public IPlayerVisual visual;
        public MobileJoystick joystick;
        public HorseVisual horse;

        [Header("Mount")]
        [Tooltip("PC horse id (1/3/5/7/9). 0 = no horse / cannot mount. SandboxManager overrides this from PlayerProgression on boot.")]
        public int defaultHorseId = 5;
        [Tooltip("True while mounted (horse GO is active and visual.SetMounted(true) was applied).")]
        public bool startMounted = true;
        public float mountedSpeedMultiplier = 1.8f; // PC: horse RunSpeed / player RunSpeed ≈ 1.8
        public PlayerMountService Mount { get; } = new PlayerMountService();

        [Header("Camera Follow")]
        public Camera followCamera;
        public bool followCameraEnabled = true;
        public float followOrthoSize = 480f;  // wider view to see full map context
        public float followSmooth = 12f;
        public float cameraZ = -100f;

        [Header("Map Bounds Clamp")]
        public bool clampToMapBounds = true;
        // Map 79 bounds: regions 94-113 (col) x 91-103 (row)
        // world X: col*512, world Y: -(row+1)*512 to -(row*512)
        public Vector2 mapBoundsMin = new Vector2(48128f, -53248f);
        public Vector2 mapBoundsMax = new Vector2(58368f, -46592f);

        public Vector2 MoveInput { get; private set; }
        public Vector2 LastMoveDelta { get; private set; }
        public Vector2 MoveTarget { get; private set; }
        public bool HasMoveTarget { get; private set; }
        public float targetArriveDistance = 8f;

        private PlayerVisualAction? _forcedVisualAction;
        private float _forcedVisualUntil;
        private PcWeaponType _equippedWeapon;

        public PcWeaponType EquippedWeapon => _equippedWeapon;

        private void Awake()
        {
            EnsureVisual();
            EnsureHorse();
            Mount.OnMountChanged += OnMountChanged;
            if (startMounted && defaultHorseId > 0 && visual != null && !visual.IsMounted)
            {
                Mount.Mount(defaultHorseId);
            }
        }

        private void OnEnable()
        {
            if (joystick != null)
                joystick.onMove.AddListener(SetMoveInput);
        }

        private void OnDisable()
        {
            if (joystick != null)
                joystick.onMove.RemoveListener(SetMoveInput);
        }

        private void Update()
        {
            if (allowKeyboardFallback)
            {
                var keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
                if (keyboard.sqrMagnitude > 0.0001f)
                    MoveInput = Vector2.ClampMagnitude(keyboard, 1f);
            }

            SimulateMove(Time.deltaTime);
        }

        public void BindJoystick(MobileJoystick nextJoystick)
        {
            if (joystick == nextJoystick)
                return;

            if (joystick != null && isActiveAndEnabled)
                joystick.onMove.RemoveListener(SetMoveInput);

            joystick = nextJoystick;

            if (joystick != null && isActiveAndEnabled)
                joystick.onMove.AddListener(SetMoveInput);
        }

        public void SetMoveInput(Vector2 input)
        {
            MoveInput = Vector2.ClampMagnitude(input, 1f);
            if (MoveInput.sqrMagnitude > 0.0001f)
                HasMoveTarget = false;
        }

        public void MoveTo(Vector2 worldTarget)
        {
            MoveTarget = worldTarget;
            HasMoveTarget = true;
        }

        public void ClearMoveTarget()
        {
            HasMoveTarget = false;
        }

        /// <summary>Set 8-way facing direction (0-7) for combat targeting.</summary>
        public void SetFacing(int facing8Way)
        {
            if (visual != null)
                visual.SetDirection(facing8Way);
        }

        /// <summary>
        /// Equip a weapon type, switching the player visual to use the correct
        /// PC action/weapon SPR set (e.g. LongWeapon for staff).
        /// </summary>
        public void EquipWeapon(PcWeaponType weapon)
        {
            _equippedWeapon = weapon;
            EnsureVisual();
            if (visual != null)
                visual.SetWeapon(weapon);
        }

        /// <summary>
        /// Trigger the PC client action selected by Skills.txt CharAnimId.
        /// For Cái Bang Bổng Pháp (staff), this uses 长武器魔法 (MG04) action;
        /// for Chưởng Pháp (empty hand), this uses 空手魔法 (MG01) action.
        /// </summary>
        public void PlayPcSkillAction(int charAnimId, float durationSeconds)
        {
            var action = ResolveAction(charAnimId, _equippedWeapon);
            if (action == null)
                return;

            EnsureVisual();
            _forcedVisualAction = action.Value;
            _forcedVisualUntil = Time.time + Mathf.Max(0.05f, durationSeconds);
            if (visual != null)
                visual.SetAction(action.Value);
        }

        public void ResetMovementState()
        {
            MoveInput = Vector2.zero;
            LastMoveDelta = Vector2.zero;
            MoveTarget = Vector2.zero;
            HasMoveTarget = false;
            if (joystick != null)
                joystick.ResetInput(notify: false);
        }

        public void SimulateMove(float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            Vector2 input = Vector2.ClampMagnitude(MoveInput, 1f);
            if (HasMoveTarget)
            {
                var pos = (Vector2)transform.position;
                var toTarget = MoveTarget - pos;
                float arrive = Mathf.Max(0.1f, targetArriveDistance);
                if (toTarget.magnitude <= arrive)
                {
                    transform.position = new Vector3(MoveTarget.x, MoveTarget.y, transform.position.z);
                    HasMoveTarget = false;
                    input = Vector2.zero;
                }
                else
                {
                    input = toTarget.normalized;
                }
            }

            LastMoveDelta = Vector2.zero;

            EnsureVisual();
            Mount.Tick(dt);
            float speed = Mount.IsMounted ? moveSpeed * mountedSpeedMultiplier : moveSpeed;
            LastMoveDelta = input * (speed * dt);
            if (LastMoveDelta.sqrMagnitude > 0f)
            {
                var before = (Vector2)transform.position;
                var next = before + LastMoveDelta;
                if (HasMoveTarget)
                {
                    var toTarget = MoveTarget - before;
                    if (LastMoveDelta.sqrMagnitude >= toTarget.sqrMagnitude)
                    {
                        next = MoveTarget;
                        HasMoveTarget = false;
                    }
                }
                transform.position = new Vector3(next.x, next.y, transform.position.z);

                // Clamp player inside map bounds
                if (clampToMapBounds)
                {
                    var clamped = new Vector3(
                        Mathf.Clamp(transform.position.x, mapBoundsMin.x, mapBoundsMax.x),
                        Mathf.Clamp(transform.position.y, mapBoundsMin.y, mapBoundsMax.y),
                        transform.position.z);
                    transform.position = clamped;
                }
            }

            EnsureVisual();
            if (visual != null)
            {
                if (_forcedVisualAction.HasValue && Time.time < _forcedVisualUntil)
                {
                    visual.SetAction(_forcedVisualAction.Value);
                    visual.Tick(dt);
                }
                else
                {
                    _forcedVisualAction = null;
                    visual.SetMoveInput(input);
                    visual.Tick(dt);
                }
                SyncHorseDirectionAndSorting();
            }

            FollowCamera(dt, immediate: false);
        }

        public void PlaceAt(Vector2 worldPosition, bool snapCamera = true)
        {
            ResetMovementState();
            Vector2 pos = worldPosition;
            if (clampToMapBounds)
                pos = new Vector2(
                    Mathf.Clamp(pos.x, mapBoundsMin.x, mapBoundsMax.x),
                    Mathf.Clamp(pos.y, mapBoundsMin.y, mapBoundsMax.y));
            transform.position = new Vector3(pos.x, pos.y, transform.position.z);
            EnsureVisual();
            if (visual != null)
                visual.Tick(0f);
            if (snapCamera)
                FollowCamera(0f, immediate: true);
        }

        public void SnapCamera()
        {
            FollowCamera(0f, immediate: true);
        }

        private void EnsureVisual()
        {
            if (visual != null)
                return;

            // Try find existing visual in children
            var maleV = GetComponentInChildren<MalePlayerVisual>(true);
            var femaleV = GetComponentInChildren<FemalePlayerVisual>(true);

            if (isFemale && femaleV != null)
                visual = femaleV;
            else if (!isFemale && maleV != null)
                visual = maleV;
            else if (maleV != null)
                visual = maleV;
            else if (femaleV != null)
                visual = femaleV;

            if (visual != null)
            {
                visual.playAutomatically = false;
                return;
            }

            // Create new visual based on gender
            if (isFemale)
            {
                var go = new GameObject("FemalePlayerVisual");
                go.transform.SetParent(transform, false);
                var fv = go.AddComponent<FemalePlayerVisual>();
                fv.playAutomatically = false;
                visual = fv;
            }
            else
            {
                var go = new GameObject("MalePlayerVisual");
                go.transform.SetParent(transform, false);
                var mv = go.AddComponent<MalePlayerVisual>();
                mv.playAutomatically = false;
                visual = mv;
            }
        }

        /// <summary>
        /// Resolve CharAnimId → PlayerVisualAction. Works cho cả male và female
        /// vì cả hai dùng chung action mapping từ PC source.
        /// </summary>
        public static PlayerVisualAction? ResolveAction(int charAnimId, PcWeaponType weapon)
        {
            return charAnimId switch
            {
                7 or 8 => PlayerVisualAction.Attack,
                9 or 10 or 11 => PlayerVisualAction.Magic,
                14 => null,
                _ => null,
            };
        }

        private void EnsureHorse()
        {
            if (horse != null)
                return;

            horse = GetComponentInChildren<HorseVisual>(true);
            if (horse != null)
            {
                if (defaultHorseId > 0) horse.SetHorseId(defaultHorseId);
                horse.gameObject.SetActive(false);
                return;
            }

            var horseGo = new GameObject("HorseVisual");
            horseGo.transform.SetParent(transform, false);
            horse = horseGo.AddComponent<HorseVisual>();
            horse.anchorOffset = new Vector3(0f, -28f, 0f);
            if (defaultHorseId > 0) horse.SetHorseId(defaultHorseId);
            horseGo.SetActive(false);
        }

        private void OnMountChanged(MountChangeEvent evt)
        {
            bool mounted = evt.newState == MountState.Mounted;
            if (visual != null)
                visual.SetMounted(mounted);
            if (horse != null)
            {
                if (evt.horseType > 0) horse.SetHorseId(evt.horseType);
                horse.gameObject.SetActive(mounted);
                if (mounted) SyncHorseDirectionAndSorting();
            }
        }

        /// <summary>
        /// Player-facing toggle. Mount uses <see cref="defaultHorseId"/> if not yet mounted;
        /// Dismount if already mounted. PC source: press 'mount' key on PC client.
        /// </summary>
        public void ToggleMount()
        {
            if (Mount.IsMounted)
                Mount.Dismount();
            else if (defaultHorseId > 0)
                Mount.Mount(defaultHorseId);
        }

        public void SetHorseId(int newHorseId)
        {
            defaultHorseId = newHorseId;
            if (horse != null) horse.SetHorseId(newHorseId);
            if (Mount.IsMounted) Mount.Mount(newHorseId);
        }

        private int _lastSyncDirection = -1;
        private int _lastSyncSortingOrder = int.MinValue;
        private void SyncHorseDirectionAndSorting()
        {
            if (horse == null || !horse.gameObject.activeSelf) return;
            int dir = visual != null ? visual.GetCurrentDirection() : 0;
            if (dir != _lastSyncDirection)
            {
                horse.SetDirection(dir);
                _lastSyncDirection = dir;
            }
            // Mounted rider uses sortOrder 5000..5022; horse sits 100 below so it sorts behind.
            if (visual != null)
            {
                int riderOrder = visual.GetRiderSortingOrder();
                int horseOrder = riderOrder - 100;
                if (horseOrder != _lastSyncSortingOrder)
                {
                    horse.SetSortingOrder(horseOrder);
                    _lastSyncSortingOrder = horseOrder;
                }
            }
        }

        private void FollowCamera(float deltaTime, bool immediate)
        {
            if (!followCameraEnabled)
                return;

            var cam = followCamera != null ? followCamera : Camera.main;
            if (cam == null)
                return;

            cam.orthographic = true;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 5000f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
            cam.orthographicSize = Mathf.Max(1f, followOrthoSize);
            cam.transform.rotation = Quaternion.identity;

            // Clamp camera so viewport never shows beyond map bounds
            // halfW/halfH = half the visible area in world units
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float camX = transform.position.x;
            float camY = transform.position.y;

            if (clampToMapBounds)
            {
                camX = Mathf.Clamp(camX, mapBoundsMin.x + halfW, mapBoundsMax.x - halfW);
                camY = Mathf.Clamp(camY, mapBoundsMin.y + halfH, mapBoundsMax.y - halfH);
            }

            var target = new Vector3(camX, camY, cameraZ);
            if (immediate || deltaTime <= 0f || followSmooth <= 0f)
                cam.transform.position = target;
            else
            {
                float t = 1f - Mathf.Exp(-followSmooth * deltaTime);
                cam.transform.position = Vector3.Lerp(cam.transform.position, target, t);
            }
        }
    }
}
