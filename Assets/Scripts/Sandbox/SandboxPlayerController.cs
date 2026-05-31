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
        public float followOrthoSize = 240f;
        public float followSmooth = 12f;
        public float cameraZ = -100f;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LastMoveDelta { get; private set; }

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
        }

        public void SimulateMove(float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            Vector2 input = Vector2.ClampMagnitude(MoveInput, 1f);
            LastMoveDelta = input * (moveSpeed * dt);
            if (LastMoveDelta.sqrMagnitude > 0f)
                transform.position += new Vector3(LastMoveDelta.x, LastMoveDelta.y, 0f);

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
