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

        [Header("PC Cast Presentation")]
        [Tooltip("PC speed bonus percent. Logical action clock uses BaseValue.ini's 20 frames, not Skills.txt WaitTime.")]
        public float attackSpeedPercent;
        public float castSpeedPercent;
        public bool IsSkillActionLocked => _forcedVisualAction.HasValue && _forcedVisualRemaining > 0f;
        public float ForcedActionRemaining => Mathf.Max(0f, _forcedVisualRemaining);
        public float ForcedActionDuration => _forcedVisualDuration;
        public float ForcedActionEffectTime => _forcedVisualEffectTime;
        public int ForcedActionTotalTicks => _forcedVisualTotalTicks;
        public float ForcedActionProgress
        {
            get
            {
                if (_forcedVisualTotalTicks <= 0 || _forcedVisualDuration <= 0f)
                    return 0f;
                float elapsed = Mathf.Max(0f, _forcedVisualDuration - _forcedVisualRemaining);
                int currentTick = Mathf.Clamp(
                    Mathf.FloorToInt(elapsed * PcFramesPerSecond + 0.0001f),
                    0,
                    Mathf.Max(0, _forcedVisualTotalTicks - 1));
                return currentTick / (float)_forcedVisualTotalTicks;
            }
        }
        public event System.Action OnPcSkillActionEffect;

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

        private const int PcBaseActionFrames = 20;
        private const int PcPercentBase = 100;
        private const float PcFramesPerSecond = 18f;
        private PlayerVisualAction? _forcedVisualAction;
        private int _forcedVisualTotalTicks;
        private float _forcedVisualDuration;
        private float _forcedVisualEffectTime;
        private float _forcedVisualRemaining;
        private bool _forcedVisualEffectEmitted;
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
                if (keyboard.sqrMagnitude > 0.0001f)
                    SetMoveInput(keyboard);
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
            if (IsSkillActionLocked)
                return;
            Vector2 clamped = Vector2.ClampMagnitude(input, 1f);
            if (IsMeditating && clamped.sqrMagnitude > 0.0001f)
                StopMeditation("Dừng ngồi thiền để di chuyển");

            MoveInput = IsMeditating ? Vector2.zero : clamped;
            if (MoveInput.sqrMagnitude > 0.0001f)
                HasMoveTarget = false;
        }

        public void MoveTo(Vector2 worldTarget)
        {
            if (IsSkillActionLocked)
                return;
            if (IsMeditating)
                StopMeditation("Dừng ngồi thiền để di chuyển");

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

            if (IsMeditating)
            {
                StopMeditation("Dừng ngồi thiền");
                return;
            }

            IsMeditating = true;
            MoveInput = Vector2.zero;
            HasMoveTarget = false;
            LastMoveDelta = Vector2.zero;
            SubsystemLog.Info("HUD", "Bắt đầu ngồi thiền");
        }

        private void StopMeditation(string reason)
        {
            if (!IsMeditating)
                return;
            IsMeditating = false;
            LastMoveDelta = Vector2.zero;
            SubsystemLog.Info("HUD", reason);
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

        public void EquipWeapon(PcWeaponType weapon, int exactVariant)
        {
            _equippedWeapon = weapon;
            EnsureVisual();
            if (visual != null)
                visual.SetWeapon(weapon, exactVariant);
        }

        /// <summary>Trigger CharAnim 9/10/11. WaitTime is cooldown metadata, never pose length.</summary>
        public void PlayPcSkillAction(int charAnimId, float ignoredWaitTimeSeconds, int horseLimit = 0)
        {
            // KSkills.cpp rejects HorseLimit before assigning ClientDoing/action.
            if ((horseLimit == 1 && Mount.IsMounted) || (horseLimit == 2 && !Mount.IsMounted))
                return;

            var action = ResolveAction(charAnimId, _equippedWeapon);
            if (!action.HasValue)
                return;

            float speed = action == PlayerVisualAction.Attack || action == PlayerVisualAction.Attack1
                ? attackSpeedPercent : castSpeedPercent;
            _forcedVisualAction = action.Value;
            _forcedVisualTotalTicks = ResolvePcActionTicks(speed);
            _forcedVisualDuration = _forcedVisualTotalTicks / PcFramesPerSecond;
            _forcedVisualEffectTime = (_forcedVisualTotalTicks * 60 / 100) / PcFramesPerSecond;
            _forcedVisualRemaining = _forcedVisualDuration;
            _forcedVisualEffectEmitted = false;
            MoveInput = Vector2.zero;
            HasMoveTarget = false;
            LastMoveDelta = Vector2.zero;
            EnsureVisual();
            if (visual != null)
                visual.SetAction(action.Value);
        }

        private void AdvanceForcedVisualAction(float deltaTime)
        {
            if (!IsSkillActionLocked)
                return;

            _forcedVisualRemaining = Mathf.Max(0f, _forcedVisualRemaining - deltaTime);
            if (_forcedVisualRemaining < 0.0001f)
                _forcedVisualRemaining = 0f;
            if (!_forcedVisualEffectEmitted && _forcedVisualDuration - _forcedVisualRemaining + 0.0001f >= _forcedVisualEffectTime)
            {
                _forcedVisualEffectEmitted = true;
                OnPcSkillActionEffect?.Invoke();
            }
        }

        public static int ResolvePcActionTicks(float speedPercent)
        {
            // PC KNpc stores m_CurrentAttackSpeed/m_CurrentCastSpeed as int and
            // performs integer division. Unity-facing stats are floats, so discard
            // the fractional part toward zero before reproducing that calculation.
            int pcSpeed = speedPercent >= 0f
                ? Mathf.FloorToInt(speedPercent)
                : Mathf.CeilToInt(speedPercent);
            int denominator = Mathf.Max(1, PcPercentBase + pcSpeed);
            int totalTicks = PcBaseActionFrames * PcPercentBase / denominator;
            totalTicks -= totalTicks % 2;
            return totalTicks > 0 ? totalTicks : 1;
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
            // PC KNpc::NewJump jumps toward the current command/direction. On mobile, prefer the
            // live joystick/click-to-move direction so Khinh Công can be fired while already moving.
            Vector2 dir = Vector2.zero;
            if (MoveInput.sqrMagnitude > 0.0001f)
                dir = MoveInput;
            else if (HasMoveTarget)
                dir = MoveTarget - (Vector2)transform.position;
            else if (visual != null && visual.direction >= 0 && visual.direction < Facing8Way.Length)
                dir = Facing8Way[visual.direction];
            else
                dir = Vector2.down;
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
            // Khinh Công may be pressed while moving. Dash owns position during the leap, but keep
            // the current movement intent so holding joystick continues movement after landing.
            Vector2 preservedInput = MoveInput;
            Vector2 preservedTarget = MoveTarget;
            bool preservedHasTarget = HasMoveTarget;
            EnsureVisual();
            if (visual != null)
                visual.SetAction(PlayerVisualAction.Jump);
            BeginDash(worldTarget, duration);
            MoveInput = preservedInput;
            MoveTarget = preservedTarget;
            HasMoveTarget = preservedHasTarget;
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

            AdvanceForcedVisualAction(dt);
            bool actionLocked = IsSkillActionLocked;
            Vector2 input = actionLocked ? Vector2.zero : Vector2.ClampMagnitude(MoveInput, 1f);
            if (actionLocked)
                HasMoveTarget = false;
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
            float walkMultiplier = Mathf.Clamp(walkSpeedMultiplier, 0.05f, 1f);
            float speed = moveSpeed;
            if (Mount.IsMounted)
            {
                // PC walk/run is an action mode. Mounted walk must select the horse's
                // walk pace, not apply the walk factor after the horse run multiplier;
                // otherwise walking on a horse still moves about as fast as on-foot run.
                speed *= IsRunning ? mountedSpeedMultiplier : walkMultiplier;
            }
            else
            {
                speed *= IsRunning ? 1f : walkMultiplier;
            }

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
                if (IsSkillActionLocked)
                {
                    visual.SetAction(_forcedVisualAction.Value);
                    visual.SetLogicalActionProgress(ForcedActionProgress);
                    visual.Tick(dt);
                }
                else
                {
                    _forcedVisualAction = null;
                    visual.SetLogicalActionProgress(-1f);
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
                  visual.SetMounted(Mount.IsMounted);
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
              visual.SetMounted(Mount.IsMounted);
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
                    // Detach before deferred Destroy so EnsureVisual cannot rediscover the
                    // wrong-gender component through GetComponentInChildren in this frame.
                    visualMono.transform.SetParent(null, worldPositionStays: false);
                    if (Application.isPlaying)
                        Destroy(visualMono.gameObject);
                    else
                        DestroyImmediate(visualMono.gameObject);
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
                _equippedWeapon = eq.GetCurrentWeaponType();
                // SetWeapon picks action bank; exact equip row owns weapon SPR variant in one refresh.
                visual.SetWeapon(_equippedWeapon, eq.GetVariant(PlayerEquipSlot.Weapon));
                foreach (PlayerEquipSlot slot in System.Enum.GetValues(typeof(PlayerEquipSlot)))
                {
                    if (slot == PlayerEquipSlot.Weapon) continue;
                    visual.SetEquipVariant(slot, eq.GetVariant(slot));
                }
                return;
            }
            visual.SetWeapon(_equippedWeapon);
        }

        /// <summary>
        /// Resolve CharAnimId → PlayerVisualAction. Works cho cả male và female
        /// vì cả hai dùng chung action mapping từ PC source.
        /// </summary>
        public static PlayerVisualAction? ResolveAction(int charAnimId, PcWeaponType weapon)
        {
            return MalePlayerSpriteCatalog.ResolveAction(charAnimId, weapon);
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
                StopMeditation("Dừng ngồi thiền");

            // Preserve IsRunning across mount/dismount: PC walk/run is an action-toggle state,
            // and mounted movement also respects walk vs run speed.
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
