using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace VirtualHouse.Editor
{
    public static class HouseTouchValidation
    {
        [MenuItem("Virtual House/Validate Touch Controls")]
        public static void Validate()
        {
            var device = InputSystem.AddDevice<Touchscreen>();
            var root = new GameObject("Touch validation temporary player");
            var controller = root.AddComponent<HouseFirstPersonController>();
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var type = typeof(HouseFirstPersonController);
            T Read<T>(string field) => (T)type.GetField(field, flags).GetValue(controller);
            void Sample() => type.GetMethod("ReadTouchInput", flags).Invoke(controller, null);
            void Touch(int id, TouchPhase phase, Vector2 position)
            {
                InputSystem.QueueStateEvent(device, new TouchState { touchId = id, phase = phase, position = position });
                InputSystem.Update();
            }
            void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
            Vector2 lower = new Vector2(Screen.width * .30f, Screen.height * .25f);
            Vector2 upper = new Vector2(Screen.width * .30f, Screen.height * .35f);
            try
            {
                Touch(1, TouchPhase.Began, lower); Sample();
                Check(Read<int>("movementTouchId") == 1 && Read<int>("lookTouchId") == -1, "Lower half must move only");
                Touch(1, TouchPhase.Moved, lower + Vector2.right * 40); Sample();
                Check(Read<Vector2>("touchMovement").x > 0, "Movement drag missing");
                Touch(2, TouchPhase.Began, upper); Sample();
                Check(Read<int>("lookTouchId") == 2 && Read<int>("movementTouchId") == 1, "Simultaneous look and movement failed");
                Touch(2, TouchPhase.Moved, upper + Vector2.right * 30); Sample();
                Check(Read<Vector2>("touchLookDelta").x > 0, "Look drag missing");
                Touch(1, TouchPhase.Moved, upper); Sample();
                Check(Read<int>("movementTouchId") == 1 && Read<int>("lookTouchId") == 2, "Crossing divider changed ownership");
                Rect jump = (Rect)type.GetMethod("GetJumpRect", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
                Touch(3, TouchPhase.Began, new Vector2(jump.center.x, Screen.height - jump.center.y)); Sample();
                Check(Read<bool>("touchJumpPressed") && Read<int>("lookTouchId") == 2, "Jump with two fingers active failed");
                Touch(3, TouchPhase.Stationary, new Vector2(jump.center.x, Screen.height - jump.center.y)); Sample();
                Check(!Read<bool>("touchJumpPressed"), "Held jump retriggered");
                Touch(1, TouchPhase.Canceled, upper); Sample();
                Check(Read<int>("movementTouchId") == -1 && Read<Vector2>("touchMovement") == Vector2.zero, "Canceled movement stuck");
                type.GetMethod("OnApplicationFocus", flags).Invoke(controller, new object[] { false });
                Check(Read<int>("lookTouchId") == -1, "Focus loss did not clear look");
                Debug.Log("Touch validation passed: lower move, upper look, boundary lock, three-finger jump, cancellation and focus reset.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                InputSystem.RemoveDevice(device);
            }
        }
    }
}
