// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

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
        public float moveSpeed = 360f; // 200% tốc độ hiện tại (180f) theo yêu cầu test map rộng
        [Tooltip("Walk multiplier relative to the normal run speed. PC walk/run is a movement mode toggle; exact PC value can be refined when source is recovered.")]
        public float walkSpeedMultiplier = 0.55f;
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
        public float followOrthoSize = 300f;  // zoom cân bằng: player rõ nhưng vẫn thấy context (160 quá gần, 480 quá xa)
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
        public bool IsRunning { get; private set; } = true;
        public bool IsMeditating { get; private set; }
        public float targetArriveDistance = 8f;

        // [SECT-ALL] Dash state machine cho melee skill THẬT SỰ (Cái Bang Bổng Pháp, etc.).
        // PC source: KNpc::DoRunAttack (0x0809b9c0) sets m_214=0x12 (LUNGE_STATE) cho close-range.
        // KNpc::NewJump (0x08099fd0) dùng TestMovePos + stores distance ở m_1834 cho long-range.
        // [SECT-ALL fix 2026-06-15]: Phi Long (357) KHÔNG dùng dash — PC IsMelee=0, ByMissle=1.
        //   Comment cũ ghi "cho Phi Long" là sai (commit e194a242a đọc sai gaibang.lua). Đã sửa.
        // Client engine reads state + distance để chạy sprite animation. Mobile port equivalent:
        // lerp position từ dashStartPos → dashTargetPos trong dashDuration, KHÔNG teleport.
        // [SECT-ALL] TODO(PC-runtime): dashDuration không có trong PC source. Server chỉ set state,
        // duration thuộc client engine animation. Cần PC runtime video để verify duration chính xác.
        // Caller (CombatSkillSlotController) phải truyền duration; hiện đang dùng placeholder.
        private Vector2 dashStartPos;
        private Vector2 dashTargetPos;
        private float dashStartTime = -1f;
        private float dashDuration = 0f;
        public bool IsDashing => dashStartTime >= 0f;
        public Vector2 DashStartPos => dashStartPos;
        public Vector2 DashTargetPos => dashTargetPos;
        public float DashProgress
        {
            get
            {
                if (dashStartTime < 0f || dashDuration <= 0f) return 1f;
                return Mathf.Clamp01((Time.time - dashStartTime) / dashDuration);
            }
        }

        private const float TrapContactRadius = 16f;

        private PlayerVisualAction? _forcedVisualAction;
        private float _forcedVisualUntil;
        private PcWeaponType _equippedWeapon;

        public PcWeaponType EquippedWeapon => _equippedWeapon;

        private void Awake()
        {
            EnsureVisual();
            EnsureHorse();
            EnsureTrapContactBody();
            Mount.OnMountChanged += OnMountChanged;
            if (startMounted && defaultHorseId > 0 && visual != null && !visual.IsMounted)
            {
                Mount.Mount(defaultHorseId);
            }
        }

        private void Reset()
        {
            EnsureTrapContactBody();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                EnsureTrapContactBody();
        }
#endif

        private void OnEnable()
        {
            if (joystick != null)
                joystick.onMove.AddListener(SetMoveInput);
        }

        private void EnsureTrapContactBody()
        {
            var body = GetComponent<Rigidbody2D>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;

            var collider = GetComponent<CircleCollider2D>();
            if (collider == null)
                collider = gameObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = TrapContactRadius;
            collider.offset = Vector2.zero;
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
                if (keyboard.sqrMagnitude > 0.0001f && !IsMeditating)
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
            if (IsMeditating)
            {
                MoveInput = Vector2.zero;
                HasMoveTarget = false;
                return;
            }

            MoveInput = Vector2.ClampMagnitude(input, 1f);
            if (MoveInput.sqrMagnitude > 0.0001f)
                HasMoveTarget = false;
        }

        public void MoveTo(Vector2 worldTarget)
        {
            if (IsMeditating)
            {
                MoveInput = Vector2.zero;
                HasMoveTarget = false;
                return;
            }

            MoveTarget = ClampToActiveMapBounds(worldTarget);
            HasMoveTarget = true;
        }

        /// <summary>Apply active PC map source bounds so movement/camera clamp matches minimap/world extent.</summary>
        public void SetMapBounds(RectDef sourceBounds)
        {
            if (sourceBounds == null || sourceBounds.width <= 0f || sourceBounds.height <= 0f)
                return;

            mapBoundsMin = new Vector2(sourceBounds.x, sourceBounds.y);
            mapBoundsMax = new Vector2(sourceBounds.x + sourceBounds.width, sourceBounds.y + sourceBounds.height);
            clampToMapBounds = true;
            if (HasMoveTarget)
                MoveTarget = ClampToActiveMapBounds(MoveTarget);
        }

        private Vector2 ClampToActiveMapBounds(Vector2 worldPosition)
        {
            if (!clampToMapBounds)
                return worldPosition;

            return new Vector2(
                Mathf.Clamp(worldPosition.x, mapBoundsMin.x, mapBoundsMax.x),
                Mathf.Clamp(worldPosition.y, mapBoundsMin.y, mapBoundsMax.y));
        }

        public void ClearMoveTarget()
        {
            HasMoveTarget = false;
        }

        public void ToggleWalkRun()
        {
            IsRunning = !IsRunning;
            SubsystemLog.Info("HUD", IsRunning ? "Chạy" : "Đi bộ");
        }

        public void ToggleMeditation()
        {
            if (!IsMeditating && Mount.IsMounted)
            {
                SubsystemLog.Info("HUD", "Không thể ngồi thiền khi đang cưỡi ngựa");
                return;
            }

            IsMeditating = !IsMeditating;
            if (IsMeditating)
            {
                MoveInput = Vector2.zero;
                HasMoveTarget = false;
                LastMoveDelta = Vector2.zero;
            }

            SubsystemLog.Info("HUD", IsMeditating ? "Bắt đầu ngồi thiền" : "Dừng ngồi thiền");
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

        /// <summary>
        /// Bắt đầu dash từ vị trí hiện tại tới <paramref name="worldTarget"/> trong <paramref name="duration"/> giây.
        /// PC source: KNpc::DoRunAttack (close-range, state 0x12) / KNpc::NewJump (long-range, TestMovePos).
        /// Dash chiếm quyền điều khiển — joystick + click-to-move bị bơ qua trong khi dash.
        /// </summary>
        public void BeginDash(Vector2 worldTarget, float duration)
        {
            if (clampToMapBounds)
                worldTarget = new Vector2(
                    Mathf.Clamp(worldTarget.x, mapBoundsMin.x, mapBoundsMax.x),
                    Mathf.Clamp(worldTarget.y, mapBoundsMin.y, mapBoundsMax.y));
            dashStartPos = (Vector2)transform.position;
            dashTargetPos = worldTarget;
            dashStartTime = Time.time;
            dashDuration = Mathf.Max(0.01f, duration);
            // Clear normal movement để dash có toàn quyền
            MoveInput = Vector2.zero;
            LastMoveDelta = Vector2.zero;
            HasMoveTarget = false;
            Mount.Tick(0f);
        }

        /// <summary>Cancel dash hiện tại (dùng khi bị stun, knockback, etc.).</summary>
        public void CancelDash()
        {
            dashStartTime = -1f;
            dashDuration = 0f;
        }

        // PC 8-way direction → unit vector. Mirrors PcDirection8Way but kept gender-agnostic
        // (works for both Male/Female visuals since `direction` is a shared 0..7 index).
        // PC convention: 0=S, 1=SW, 2=W, 3=NW, 4=N, 5=NE, 6=E, 7=SE.
        private static readonly Vector2[] Facing8Way =
        {
            new Vector2(0, -1), new Vector2(-1, -1), new Vector2(-1, 0), new Vector2(-1, 1),
            new Vector2(0, 1),  new Vector2(1, 1),    new Vector2(1, 0),  new Vector2(1, -1),
        };

        /// <summary>
        /// PC 轻功 (Khinh Công / JumpFly, skill 210): world position reached by leaping
        /// <paramref name="distance"/> units in the current facing direction, clamped to map bounds.
        /// </summary>
        public Vector2 GetLeapTarget(float distance)
        {
            Vector2 dir = (visual != null && visual.direction >= 0 && visual.direction < Facing8Way.Length)
                ? Facing8Way[visual.direction]
                : Vector2.down;
            Vector2 target = (Vector2)transform.position + dir.normalized * Mathf.Max(0f, distance);
            if (clampToMapBounds)
                target = new Vector2(
                    Mathf.Clamp(target.x, mapBoundsMin.x, mapBoundsMax.x),
                    Mathf.Clamp(target.y, mapBoundsMin.y, mapBoundsMax.y));
            return target;
        }

        /// <summary>
        /// PC 轻功 leap: dash from the current position toward <paramref name="worldTarget"/> while
        /// playing the Jump (JP01) animation. PC source: KNpc::NewJump (TestMovePos + m_1834 distance).
        /// The exact leap distance/duration is owned by the PC client engine animation and is not in
        /// the server source, so the caller passes a faithful observed value.
        /// </summary>
        public void BeginLeap(Vector2 worldTarget, float duration)
        {
            EnsureVisual();
            _forcedVisualAction = PlayerVisualAction.Jump;
            _forcedVisualUntil = Time.time + Mathf.Max(0.05f, duration);
            if (visual != null)
                visual.SetAction(PlayerVisualAction.Jump);
            BeginDash(worldTarget, duration);
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

            // [SECT-ALL] Dash state machine (PC source: DoRunAttack/NewJump).
            // Nếu đang dash, lerp position từ dashStartPos → dashTargetPos theo dashDuration.
            // Dash chiếm toàn quyền — bỏ qua joystick + click-to-move cho tới khi xong.
            if (dashStartTime >= 0f)
            {
                float t = (Time.time - dashStartTime) / dashDuration;
                if (t >= 1f)
                {
                    transform.position = new Vector3(dashTargetPos.x, dashTargetPos.y, transform.position.z);
                    dashStartTime = -1f;  // dash xong
                    EnsureVisual();
                    if (visual != null) visual.Tick(0f);
                }
                else
                {
                    var lerped = Vector2.Lerp(dashStartPos, dashTargetPos, t);
                    transform.position = new Vector3(lerped.x, lerped.y, transform.position.z);
                    EnsureVisual();
                    if (visual != null) visual.Tick(dt);
                    FollowCamera(dt, immediate: false);
                }
                return;  // skip normal movement logic khi đang dash
            }

            Vector2 input = Vector2.ClampMagnitude(MoveInput, 1f);
            if (HasMoveTarget)
            {
                var pos = (Vector2)transform.position;
                var toTarget = MoveTarget - pos;
                float arrive = Mathf.Max(0.1f, targetArriveDistance);
                if (toTarget.magnitude <= arrive)
                {
                    var clampedTarget = ClampToActiveMapBounds(MoveTarget);
                    transform.position = new Vector3(clampedTarget.x, clampedTarget.y, transform.position.z);
                    MoveTarget = clampedTarget;
                    HasMoveTarget = false;
                    input = Vector2.zero;
                }
                else
                {
                    input = toTarget.normalized;
                }
            }

            if (IsMeditating)
            {
                input = Vector2.zero;
                HasMoveTarget = false;
                MoveInput = Vector2.zero;
            }

            LastMoveDelta = Vector2.zero;

            EnsureVisual();
            Mount.Tick(dt);
            float speed = moveSpeed * (IsRunning ? 1f : Mathf.Clamp(walkSpeedMultiplier, 0.05f, 1f));
            if (Mount.IsMounted)
                speed *= mountedSpeedMultiplier;

            // Apply FastWalkRunP state buff/debuff speed multiplier from active player combat states
            var manager = SandboxManager.Instance;
            var playerActor = manager?.GameplayLoop?.Player;
            if (playerActor != null && playerActor.combat != null && playerActor.combat.states != null)
            {
                if (playerActor.combat.states.TryGetValue(MagicAttributeKind.FastWalkRunP, out var attr))
                {
                    float walkRunMultiplier = 1f + (attr.value1 / 100f);
                    speed *= Mathf.Max(0.1f, walkRunMultiplier);
                }
            }

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
                var boundedNext = ClampToActiveMapBounds(next);
                transform.position = new Vector3(boundedNext.x, boundedNext.y, transform.position.z);
            }

            EnsureVisual();
            if (visual != null)
            {
                // Drive PC walk/run + 打坐 (meditate) modes into the visual every frame.
                visual.walkMode = !IsRunning;
                visual.isMeditating = IsMeditating;
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
                ApplyActiveEquipmentVariants();
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
            ApplyActiveEquipmentVariants();
        }

        public void SetGender(bool female)
        {
            if (isFemale == female && visual != null)
                return;

            isFemale = female;

            // Sync with PlayerEquipmentService and re-equip to map to new gender variants
            if (SandboxManager.Instance != null)
            {
                if (SandboxManager.Instance.EquipmentService != null)
                {
                    SandboxManager.Instance.EquipmentService.IsFemale = female;
                }
                var inv = SandboxManager.Instance.InventoryService;
                if (inv != null)
                {
                    var currentEquipped = new Dictionary<EquipSlot, int>();
                    foreach (var pair in inv.Equipped)
                    {
                        if (pair.Value != null)
                            currentEquipped[pair.Key] = pair.Value.itemId;
                    }
                    foreach (var pair in currentEquipped)
                    {
                        inv.Equip(pair.Key, pair.Value);
                    }
                }
            }

            if (visual != null)
            {
                var visualMono = visual as MonoBehaviour;
                if (visualMono != null)
                {
                    Destroy(visualMono.gameObject);
                }
                visual = null;
            }

            EnsureVisual();

            if (visual != null)
            {
                visual.SetMounted(Mount.IsMounted);
            }
        }

        private void ApplyActiveEquipmentVariants()
        {
            if (visual == null) return;
            if (SandboxManager.Instance != null && SandboxManager.Instance.EquipmentService != null)
            {
                var eq = SandboxManager.Instance.EquipmentService;
                foreach (PlayerEquipSlot slot in System.Enum.GetValues(typeof(PlayerEquipSlot)))
                {
                    visual.SetEquipVariant(slot, eq.GetVariant(slot));
                }
                var weaponVariant = eq.GetVariant(PlayerEquipSlot.Weapon);
                _equippedWeapon = eq.GetCurrentWeaponType();
            }
            visual.SetWeapon(_equippedWeapon);
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
            bool mounted = evt.newState == MountState.Mounted || evt.newState == MountState.Mounting;
            if (visual != null)
                visual.SetMounted(mounted);
            // Horse body now renders as layered HH/HB/HT parts inside the player visual
            // (full 320x320 8-dir, frame-synced with rider). The legacy 50x76 single-frame
            // HorseVisual is kept disabled to avoid a duplicate mismatched horse.
            if (horse != null)
                horse.gameObject.SetActive(false);
        }

        /// <summary>
        /// Player-facing toggle. Mount uses <see cref="defaultHorseId"/> if not yet mounted;
        /// Dismount if already mounted. PC source: press 'mount' key on PC client.
        /// </summary>
        public void ToggleMount()
        {
            if (IsMeditating)
                ToggleMeditation();

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
            cam.clearFlags = CameraClearFlags.Skybox;
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
                float minCamX = mapBoundsMin.x + halfW;
                float maxCamX = mapBoundsMax.x - halfW;
                float minCamY = mapBoundsMin.y + halfH;
                float maxCamY = mapBoundsMax.y - halfH;
                camX = minCamX <= maxCamX
                    ? Mathf.Clamp(camX, minCamX, maxCamX)
                    : (mapBoundsMin.x + mapBoundsMax.x) * 0.5f;
                camY = minCamY <= maxCamY
                    ? Mathf.Clamp(camY, minCamY, maxCamY)
                    : (mapBoundsMin.y + mapBoundsMax.y) * 0.5f;
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
