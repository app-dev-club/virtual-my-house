using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VirtualHouse
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class HouseFirstPersonController : MonoBehaviour
    {
        private const float TouchMovementAreaRatio = 0.2f;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 2.6f;
        [SerializeField] private float sprintSpeed = 5.0f;
        [SerializeField] private float jumpHeight = 0.75f;
        [SerializeField] private float gravity = -20f;

        [Header("Look")]
        [SerializeField] private Transform view;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float gamepadSensitivity = 130f;
        [SerializeField] private float maximumLookAngle = 85f;

        [Header("Touch")]
        [SerializeField] private float touchLookSensitivity = 0.16f;
        [SerializeField, Range(0.08f, 0.25f)] private float touchStickScreenRadius = 0.14f;

        private CharacterController characterController;
        private float verticalVelocity;
        private float pitch;
        private int movementTouchId = -1;
        private int lookTouchId = -1;
        private Vector2 movementTouchOrigin;
        private Vector2 movementTouchPosition;
        private Vector2 touchMovement;
        private Vector2 touchLookDelta;
        private bool touchJumpPressed;
        private bool useTouchControls;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int VirtualHouse_HasTouch();
#endif

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            useTouchControls = DetectTouchControls();
            HideDoorModels();
            if (view == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null)
                    view = childCamera.transform;
            }
        }

        private static bool DetectTouchControls()
        {
            if (Application.isMobilePlatform)
                return true;
#if UNITY_WEBGL && !UNITY_EDITOR
            return VirtualHouse_HasTouch() != 0;
#else
            return false;
#endif
        }

        private static void HideDoorModels()
        {
            Transform[] objects = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform item in objects)
            {
                if (IsDoorModel(item.gameObject.name))
                    item.gameObject.SetActive(false);
            }
        }

        private static bool IsDoorModel(string objectName)
        {
            return objectName.Contains("扉") || objectName.Contains("襖") ||
                   objectName.Contains("入口") || objectName.Contains("勝手口") ||
                   objectName.Contains("引違い戸");
        }
        private void OnEnable()
        {
            if (useTouchControls)
                ReleaseCursor();
            else
                CaptureCursor();
        }

        private void OnDisable()
        {
            ResetTouchInput();
            ReleaseCursor();
        }

        private void ResetTouchInput()
        {
            movementTouchId = lookTouchId = -1;
            touchMovement = touchLookDelta = Vector2.zero;
            touchJumpPressed = false;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) ResetTouchInput();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) ResetTouchInput();
        }

        private void Update()
        {
            ReadTouchInput();
            HandleCursor();
            HandleLook();
            HandleMovement();
        }

        private void HandleCursor()
        {
            if (useTouchControls)
                return;

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                ReleaseCursor();
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
                CaptureCursor();
        }

        private void HandleLook()
        {
            if (view == null || (!useTouchControls && Cursor.lockState != CursorLockMode.Locked))
                return;

            Vector2 lookDelta = Vector2.zero;
            if (!useTouchControls && Mouse.current != null)
                lookDelta += Mouse.current.delta.ReadValue() * mouseSensitivity;
            if (Gamepad.current != null)
                lookDelta += Gamepad.current.rightStick.ReadValue() * gamepadSensitivity * Time.unscaledDeltaTime;
            lookDelta += touchLookDelta * touchLookSensitivity;

            transform.Rotate(Vector3.up, lookDelta.x, Space.World);
            pitch = Mathf.Clamp(pitch - lookDelta.y, -maximumLookAngle, maximumLookAngle);
            view.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            Vector2 input = ReadMovementInput();
            Vector3 movement = transform.right * input.x + transform.forward * input.y;
            movement = Vector3.ClampMagnitude(movement, 1f);

            bool sprinting = (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ||
                              (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed) ||
                              touchMovement.magnitude > 0.92f;
            float speed = sprinting ? sprintSpeed : walkSpeed;

            if (characterController.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            bool jumpPressed = (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                               (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) ||
                               touchJumpPressed;
            if (jumpPressed && characterController.isGrounded)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            verticalVelocity += gravity * Time.deltaTime;
            Vector3 velocity = movement * speed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private Vector2 ReadMovementInput()
        {
            Vector2 input = Vector2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            }

            if (Gamepad.current != null)
                input += Gamepad.current.leftStick.ReadValue();
            input += touchMovement;

            return Vector2.ClampMagnitude(input, 1f);
        }

        private void ReadTouchInput()
        {
            touchLookDelta = Vector2.zero;
            touchJumpPressed = false;

            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                ResetTouchInput();
                return;
            }

            foreach (var touch in touchscreen.touches)
            {
                int touchId = touch.touchId.ReadValue();
                UnityEngine.InputSystem.TouchPhase phase = touch.phase.ReadValue();

                if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    if (touchId == movementTouchId)
                    {
                        movementTouchId = -1;
                        touchMovement = Vector2.zero;
                    }
                    if (touchId == lookTouchId)
                        lookTouchId = -1;
                    continue;
                }

                if (!touch.press.isPressed)
                    continue;

                Vector2 position = touch.position.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    useTouchControls = true;
                    ReleaseCursor();
                    if (IsInsideJumpArea(position))
                    {
                        touchJumpPressed = true;
                        continue;
                    }

                    // Lock each finger to the region where it began, even across the divider.
                    if (position.y < Screen.height * TouchMovementAreaRatio && movementTouchId < 0)
                    {
                        movementTouchId = touchId;
                        movementTouchOrigin = position;
                        movementTouchPosition = position;
                    }
                    else if (position.y >= Screen.height * 0.3f && lookTouchId < 0)
                    {
                        lookTouchId = touchId;
                    }
                }

                if (touchId == movementTouchId)
                {
                    movementTouchPosition = position;
                    touchMovement = Vector2.ClampMagnitude(
                        (position - movementTouchOrigin) / GetTouchStickRadius(), 1f);
                }
                else if (touchId == lookTouchId)
                {
                    touchLookDelta += touch.delta.ReadValue();
                }
            }
        }

        private static bool IsInsideJumpArea(Vector2 position)
        {
            return GetJumpRect().Contains(new Vector2(position.x, Screen.height - position.y));
        }

        // GUI coordinates (top-left origin); shared by the visible button and hit testing.
        private static Rect GetJumpRect()
        {
            Rect safe = Screen.safeArea;
            float size = Mathf.Min(Screen.width, Screen.height) * 0.18f;
            float margin = Mathf.Min(Screen.width, Screen.height) * 0.035f;
            return new Rect(safe.xMax - size - margin,
                Screen.height - safe.yMin - size - margin, size, size);
        }

        private float GetTouchStickRadius()
        {
            return Mathf.Min(Screen.width, Screen.height) * touchStickScreenRadius;
        }

        private static void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnGUI()
        {
            if (useTouchControls)
            {
                DrawTouchControls();
                return;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
                return;

            const string message = "Click to look  |  WASD: Move  |  Shift: Sprint  |  Space: Jump  |  Esc: Release cursor";
            GUI.Box(new Rect(16f, 16f, 580f, 32f), message);
        }

        private void DrawTouchControls()
        {
            float radius = GetTouchStickRadius();
            Vector2 origin = movementTouchId >= 0
                ? new Vector2(movementTouchOrigin.x, Screen.height - movementTouchOrigin.y)
                : new Vector2(radius + 24f, Screen.height - radius - 24f);
            Vector2 knob = movementTouchId >= 0
                ? origin + new Vector2(touchMovement.x, -touchMovement.y) * radius
                : origin;

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);
            float dividerY = Screen.height * (1f - TouchMovementAreaRatio);
            GUI.Box(new Rect(0f, dividerY, Screen.width, 2f), "");
            GUI.Label(new Rect(24f, dividerY - 30f, 280f, 26f), "LOOK: drag upper 70%");
            GUI.Label(new Rect(24f, dividerY + 8f, 280f, 26f), "MOVE: drag lower 30%");
            GUI.color = new Color(1f, 1f, 1f, 0.38f);
            GUI.Box(new Rect(origin.x - radius, origin.y - radius, radius * 2f, radius * 2f), "MOVE");
            GUI.Box(new Rect(knob.x - 26f, knob.y - 26f, 52f, 52f), "");

            GUI.Box(GetJumpRect(), "JUMP");
            GUI.color = previousColor;
        }
    }
}
