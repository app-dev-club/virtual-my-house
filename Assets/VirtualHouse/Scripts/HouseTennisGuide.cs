using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualHouse
{
    // Installed per scene so every renovation plan shares the same nearby guide.
    public sealed class HouseTennisGuide : MonoBehaviour
    {
        public const string CircleUrl = "https://www.tennisbear.net/circle/119692/info";
        private HouseFirstPersonController player;
        private Vector3 center;
        private bool nearby;
        public static bool IsNearby { get; private set; }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void VirtualHouse_TennisGuide(int visible);
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install() => SceneManager.sceneLoaded += OnSceneLoaded;

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var ground = GameObject.Find("住宅概形_図面ベース/敷地_図面7/庭_台形");
            if (ground == null || FindFirstObjectByType<HouseTennisGuide>() != null) return;
            var guide = new GameObject("北西_テニスコート案内").AddComponent<HouseTennisGuide>();
            var bounds = ground.GetComponent<Collider>().bounds;
            guide.center = new Vector3(bounds.min.x + 2f, bounds.max.y, bounds.max.z - 2f);
            guide.CreateArea();
        }

        private void CreateArea()
        {
            player = FindFirstObjectByType<HouseFirstPersonController>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = new Color(.12f, .48f, .30f);
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "テニス案内エリア";
            pad.transform.SetParent(transform);
            pad.transform.position = center + Vector3.up * .015f;
            pad.transform.localScale = new Vector3(3f, .03f, 3f);
            pad.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(pad.GetComponent<Collider>());
            var sign = new GameObject("テニスコート案内看板");
            sign.transform.SetParent(transform);
            sign.transform.position = center + Vector3.up * 1.8f;
            // Readable from the house to the southeast.
            sign.transform.rotation = Quaternion.LookRotation(new Vector3(-1, 0, 1));
            var text = sign.AddComponent<TextMesh>();
            text.text = "TENNIS COURT\nCircle info\nWalk onto the green area";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 64;
            text.characterSize = .055f;
            text.color = new Color(.08f, .25f, .16f);
        }

        private void Update()
        {
            if (player == null) return;
            Vector3 offset = player.transform.position - center;
            bool next = Mathf.Abs(offset.x) <= 2f && Mathf.Abs(offset.z) <= 2f
                && offset.y > -.3f && offset.y < 2f;
            if (nearby == next) return;
            nearby = IsNearby = next;
            if (nearby) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
#if UNITY_WEBGL && !UNITY_EDITOR
            VirtualHouse_TennisGuide(nearby ? 1 : 0);
#endif
        }

        private void OnDestroy()
        {
            IsNearby = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            VirtualHouse_TennisGuide(0);
#endif
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        private void OnGUI()
        {
            if (!nearby) return;
            var rect = new Rect((Screen.width - 320) / 2f, 110, 320, 90);
            if (GUI.Button(rect, "家の北西・テニスコート\nテニスサークルを見る ↗")) Application.OpenURL(CircleUrl);
        }
#endif
    }
}
