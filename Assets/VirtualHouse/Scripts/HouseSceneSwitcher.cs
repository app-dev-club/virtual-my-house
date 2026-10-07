using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace VirtualHouse
{
    public sealed class HouseSceneSwitcher : MonoBehaviour
    {
        public const string Before = "HouseBlockout";
        public const string After = "HouseRenovated";
        private bool loading;

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
                float width = Mathf.Min(280, safe.width - 24);
                return new Rect(safe.xMax - width - 12, Screen.height - safe.yMax + 12, width, 54);
            }
        }

        private bool IsHouse => SceneManager.GetActiveScene().name == Before || SceneManager.GetActiveScene().name == After;

        private void Update()
        {
            if (!IsHouse || loading) return;
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Switch();
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                    if (touch.press.wasPressedThisFrame && IsSwitchArea(touch.position.ReadValue())) Switch();
        }

        private void OnGUI()
        {
            if (!IsHouse) return;
            GUI.enabled = !loading;
            string current = SceneManager.GetActiveScene().name == Before ? "BEFORE > AFTER" : "AFTER > BEFORE";
            if (GUI.Button(ButtonRect, loading ? "Loading..." : current + "  [R]")) Switch();
            GUI.enabled = true;
        }

        private void Switch()
        {
            if (loading) return;
            string target = SceneManager.GetActiveScene().name == Before ? After : Before;
            if (!Application.CanStreamedLevelBeLoaded(target))
            {
                Debug.LogError("Scene is missing from Build Settings: " + target);
                return;
            }
            loading = true;
            var operation = SceneManager.LoadSceneAsync(target);
            operation.completed += _ => loading = false;
        }
    }
}
