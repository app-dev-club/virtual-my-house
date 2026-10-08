using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace VirtualHouse
{
    public sealed class HouseSceneSwitcher : MonoBehaviour
    {
        public const string Before = "HouseBlockout";
        public const string After = "HouseRenovated";
        public const string Alternative = "HouseRenovatedAlternative";
        public const string PlanC = "HouseRenovatedPlanC";
        private bool loading;
        private float ignoreMouseUntil;
        private GUIStyle buttonStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<HouseSceneSwitcher>() != null) return;
            var host = new GameObject("Renovation scene switcher");
            DontDestroyOnLoad(host);
            host.AddComponent<HouseSceneSwitcher>();
        }

        public static bool IsSwitchArea(Vector2 screenPosition)
            => ButtonRect.Contains(new Vector2(screenPosition.x, Screen.height - screenPosition.y));

        private static Rect ButtonRect
        {
            get
            {
                Rect safe = Screen.safeArea;
                if (safe.width <= 0 || safe.height <= 0)
                    safe = new Rect(0, 0, Mathf.Max(Screen.width, 360), Mathf.Max(Screen.height, 600));
                float scale = Mathf.Clamp(Mathf.Min(safe.width, safe.height) / 600f, 1f, 2f);
                float width = Mathf.Min(360 * scale, safe.width - 24);
                return new Rect(safe.xMax - width - 12, Screen.height - safe.yMax + 12, width, 88 * scale);
            }
        }

        private bool IsHouse => SceneManager.GetActiveScene().name == Before || SceneManager.GetActiveScene().name == After || SceneManager.GetActiveScene().name == Alternative || SceneManager.GetActiveScene().name == PlanC;

        private void Update()
        {
            // Touch and browser-synthesized mouse events must not both advance the plan.
            // Track touches even during a scene load, including a finger held across it.
            bool touchSwitch = false;
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.isPressed || touch.press.wasReleasedThisFrame)
                        ignoreMouseUntil = Time.unscaledTime + 1f;
                    if (touch.press.wasPressedThisFrame && IsSwitchArea(touch.position.ReadValue()))
                        touchSwitch = true;
                }
            if (!IsHouse || loading) return;
            bool mouseSwitch = Time.unscaledTime >= ignoreMouseUntil && Mouse.current != null
                && Mouse.current.leftButton.wasPressedThisFrame && IsSwitchArea(Mouse.current.position.ReadValue());
            if (touchSwitch || mouseSwitch || (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)) Switch();
        }

        private void OnGUI()
        {
            if (!IsHouse) return;
            GUI.enabled = !loading;
            string name = SceneManager.GetActiveScene().name;
            string current = name == Before ? "BEFORE (1/4) > PLAN A" : name == After ? "PLAN A (2/4) > PLAN B" : name == Alternative ? "PLAN B (3/4) > PLAN C" : "PLAN C (4/4) > BEFORE";
            if (buttonStyle == null) buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = Mathf.RoundToInt(ButtonRect.height * .25f);
            // Rendering only: all activation goes through Update, never IMGUI mouse-up.
            GUI.Box(ButtonRect, loading ? "Loading..." : current + "\n[R] / TAP", buttonStyle);
            GUI.enabled = true;
        }

        private void Switch()
        {
            if (loading) return;
            string name = SceneManager.GetActiveScene().name;
            string target = name == Before ? After : name == After ? Alternative : name == Alternative ? PlanC : Before;
            if (!Application.CanStreamedLevelBeLoaded(target))
            {
                Debug.LogError("Scene is missing from Build Settings: " + target);
                return;
            }
            loading = true;
            var player = FindFirstObjectByType<HouseFirstPersonController>();
            bool restore = player != null;
            Vector3 position = restore ? player.transform.position : Vector3.zero;
            Quaternion rotation = restore ? player.transform.rotation : Quaternion.identity;
            float pitch = restore ? player.ViewPitch : 0;
            var operation = SceneManager.LoadSceneAsync(target);
            operation.completed += _ =>
            {
                var nextPlayer = FindFirstObjectByType<HouseFirstPersonController>();
                if (restore && nextPlayer != null) nextPlayer.RestoreScenePose(position, rotation, pitch);
                loading = false;
            };
        }
    }
}
