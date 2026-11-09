using System.Runtime.InteropServices;
using UnityEngine;

namespace VirtualHouse
{
    public sealed class HouseInstagramGlobe : MonoBehaviour
    {
        public const string ProfileUrl = "https://www.instagram.com/yuka_mama_triplife/";
        public static bool IsNearby { get; private set; }
        private HouseFirstPersonController player;
        private bool nearby;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void VirtualHouse_InstagramGlobe(int visible);
#endif

        private void Start() => player = FindFirstObjectByType<HouseFirstPersonController>();

        public bool IsInRange(Vector3 position)
        {
            Vector3 offset = position - transform.position;
            return new Vector2(offset.x, offset.z).sqrMagnitude < 1.8f * 1.8f
                && offset.y > -1.6f && offset.y < .5f;
        }

        private void Update()
        {
            bool next = player != null && IsInRange(player.transform.position);
            if (nearby == next) return;
            nearby = IsNearby = next;
            if (nearby) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
#if UNITY_WEBGL && !UNITY_EDITOR
            VirtualHouse_InstagramGlobe(nearby ? 1 : 0);
#endif
        }

        private void OnDisable()
        {
            nearby = IsNearby = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            VirtualHouse_InstagramGlobe(0);
#endif
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private void OnGUI()
        {
            if (nearby && GUI.Button(new Rect((Screen.width - 340) / 2f, 110, 340, 90),
                "地球儀 · Instagram\n@yuka_mama_triplife を見る ↗")) Application.OpenURL(ProfileUrl);
        }
#endif
    }
}
