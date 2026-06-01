// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Runtime player driver for the sandbox/mobile client. Joystick input is applied
    /// as continuous world movement and forwarded to <see cref="MalePlayerVisual"/>
    /// so the correct 8-way run animation plays while moving.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SandboxPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 900f; // TEMP: 5x normal (180) for movement testing
        public bool allowKeyboardFallback = true;

        [Header("Wiring")]
        public MalePlayerVisual visual;
        public MobileJoystick joystick;

        [Header("Camera Follow")]
        public Camera followCamera;
        public bool followCameraEnabled = true;
        public float followOrthoSize = 480f;  // wider view to see full map context
        public float followSmooth = 12f;
        public float cameraZ = -100f;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LastMoveDelta { get; private set; }
        public Vector2 MoveTarget { get; private set; }
        public bool HasMoveTarget { get; private set; }
        public float targetArriveDistance = 8f;

        private void Awake()
        {
            EnsureVisual();
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

            LastMoveDelta = input * (moveSpeed * dt);
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
            }

            EnsureVisual();
            if (visual != null)
            {
                visual.SetMoveInput(input);
                visual.Tick(dt);
            }

            FollowCamera(dt, immediate: false);
        }

        public void PlaceAt(Vector2 worldPosition, bool snapCamera = true)
        {
            ResetMovementState();
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
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

            visual = GetComponentInChildren<MalePlayerVisual>(true);
            if (visual != null)
            {
                visual.playAutomatically = false;
                return;
            }

            var visualGo = new GameObject("MalePlayerVisual");
            visualGo.transform.SetParent(transform, false);
            visual = visualGo.AddComponent<MalePlayerVisual>();
            visual.playAutomatically = false;
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

            var target = new Vector3(transform.position.x, transform.position.y, cameraZ);
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
