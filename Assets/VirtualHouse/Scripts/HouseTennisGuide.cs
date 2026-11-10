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
            // Keep the entire 4m interaction area outside the western site boundary.
            guide.center = new Vector3(bounds.min.x - 2.5f, bounds.max.y, bounds.max.z - 2f);
            var approach = GameObject.CreatePrimitive(PrimitiveType.Cube);
            approach.name = "テニス案内_敷地外の足場";
            approach.transform.SetParent(guide.transform);
            approach.transform.position = new Vector3(bounds.min.x - 2.25f, bounds.max.y - .09f, guide.center.z);
            approach.transform.localScale = new Vector3(4.5f, .18f, 4f);
            approach.GetComponent<Renderer>().sharedMaterial = ground.GetComponent<Renderer>().sharedMaterial;
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
            CreateCourtLines();
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

        private void CreateCourtLines()
        {
            var white = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            white.color = Color.white;
            // A miniature court fits within the existing green guide area.
            const float halfWidth = .62f, singlesWidth = .465f, halfLength = 1.35f;
            const float serviceDistance = .73f, lineWidth = .025f;
            void Line(string name, float x, float z, float width, float length)
            {
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = name;
                line.transform.SetParent(transform);
                line.transform.position = center + new Vector3(x, .033f, z);
                line.transform.localScale = new Vector3(width, .004f, length);
                line.GetComponent<Renderer>().sharedMaterial = white;
                Destroy(line.GetComponent<Collider>());
            }
            foreach (float x in new[] { -halfWidth, halfWidth })
                Line("ダブルスサイドライン", x, 0, lineWidth, halfLength * 2);
            foreach (float x in new[] { -singlesWidth, singlesWidth })
                Line("シングルスサイドライン", x, 0, lineWidth, halfLength * 2);
            foreach (float z in new[] { -halfLength, halfLength })
            {
                Line("ベースライン", 0, z, halfWidth * 2 + lineWidth, lineWidth);
                Line("センターマーク", 0, z - Mathf.Sign(z) * .035f, lineWidth, .07f);
            }
            foreach (float z in new[] { -serviceDistance, serviceDistance })
                Line("サービスライン", 0, z, singlesWidth * 2, lineWidth);
            Line("センターサービスライン", 0, 0, lineWidth, serviceDistance * 2);
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
