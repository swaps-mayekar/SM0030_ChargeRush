using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChargeRush.Input
{
    /// <summary>Unified pointer abstraction for Editor mouse and mobile touch.</summary>
    public sealed class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        public bool IsPointerDown { get; private set; }
        public bool IsPointerPressedThisFrame { get; private set; }
        public bool IsPointerReleasedThisFrame { get; private set; }
        public Vector2 PointerScreenPosition { get; private set; }

        public event Action<Vector2> PointerPressed;
        public event Action<Vector2> PointerMoved;
        public event Action<Vector2> PointerReleased;

        private bool wasDown;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            IsPointerPressedThisFrame = false;
            IsPointerReleasedThisFrame = false;

            var down = false;
            var position = PointerScreenPosition;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                down = true;
                position = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else if (Mouse.current != null)
            {
                down = Mouse.current.leftButton.isPressed;
                position = Mouse.current.position.ReadValue();
            }

            PointerScreenPosition = position;

            if (down && !wasDown)
            {
                IsPointerPressedThisFrame = true;
                IsPointerDown = true;
                PointerPressed?.Invoke(position);
            }
            else if (down && wasDown)
            {
                IsPointerDown = true;
                PointerMoved?.Invoke(position);
            }
            else if (!down && wasDown)
            {
                IsPointerReleasedThisFrame = true;
                IsPointerDown = false;
                PointerReleased?.Invoke(position);
            }
            else
            {
                IsPointerDown = false;
            }

            wasDown = down;
        }

        public Vector3 ScreenToWorld(Camera camera, float z = 0f)
        {
            if (camera == null)
            {
                return Vector3.zero;
            }

            var screen = new Vector3(PointerScreenPosition.x, PointerScreenPosition.y, Mathf.Abs(camera.transform.position.z - z));
            return camera.ScreenToWorldPoint(screen);
        }
    }
}
