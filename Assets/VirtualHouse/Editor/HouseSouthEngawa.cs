using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        // User-specified depth: 0.91m, with an empty underside and half-open windows.
        private static void CreateSouthOutdoorEngawa(Transform house)
        {
            const string name = "和室南_屋外縁側と床下収納";
            Transform old = house.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Transform deck = NewGroup(name, house);
            Transform pool = house.Find("敷地_図面7/屋外計画_図面8/和室南_ビニールプール");
            if (pool != null)
                foreach (SphereCollider sphere in pool.GetComponentsInChildren<SphereCollider>())
                {
                    GameObject ring = sphere.gameObject;
                    Object.DestroyImmediate(sphere);
                    var shape = ring.AddComponent<MeshCollider>();
                    shape.sharedMesh = ring.GetComponent<MeshFilter>().sharedMesh; shape.convex = true;
                }
            float west = PlanPoint(310, 423, 0).x, east = PlanPoint(613, 423, 0).x;
            const float depth = .91f, back = -.04f, front = back - depth, top = .16f, ground = -.33f;
            Material wood = GetMaterial("LightWood"), dark = GetMaterial("DarkWood");
            float width = east - west;
            for (int i = 0; i < 10; i++)
            {
                float start = front + i * depth / 10 + (i == 0 ? 0 : .002f);
                float end = front + (i + 1) * depth / 10 - (i == 9 ? 0 : .002f);
                CreateBox("縁側床板_" + i, new Vector3((west + east) / 2, top - .025f, (start + end) / 2),
                    new Vector3(width, .05f, end - start), wood, deck);
            }
            for (int i = 0; i <= 5; i++)
            {
                float x = west + .08f + (width - .16f) * i / 5;
                CreateBox("床下根太", new Vector3(x, .07f, (back + front) / 2), new Vector3(.065f, .12f, depth), dark, deck);
                foreach (float z in new[] { back - .12f, front + .12f })
                    CreateBox("縁側束柱", new Vector3(x, (ground + .01f) / 2, z), new Vector3(.085f, .01f - ground, .085f), dark, deck);
            }
            // Leave the underside empty, retaining only the structural joists and posts.
            Material glass = GetMaterial("EngawaWindowGlass");
            glass.CopyPropertiesFromMaterial(GetClearWindowMaterial());
            glass.color = new Color(.53f, .73f, .80f, .40f);
            EditorUtility.SetDirty(glass);
            Material frame = WesternMaterial("EngawaWindowFrame", new Color(.29f, .32f, .33f));
            Transform trim = house.Find("外観_写真ベース/腰壁と窓");
            foreach (var window in new[] { ("南廊下西サッシ", 386f), ("南廊下東サッシ", 537f) })
            {
                Transform previous = trim.Find(window.Item1);
                if (previous != null) Object.DestroyImmediate(previous.gameObject);
                Vector3 p = PlanPoint(window.Item2, 428, 0);
                float w = 132 * PlanScale;
                CutWindowOpening(house, p, w, 0, 2.20f, false);
                Transform sash = NewGroup(window.Item1, trim);
                // Four original quarter-width leaves gather in pairs over the left half.
                // Visible glass and contrasting frames distinguish the retained half from the opening.
                for (int leaf = 0; leaf < 4; leaf++)
                {
                    float leafX = p.x - w / 2 + w / 4 * (leaf % 2 + .5f);
                    WindowX(sash, "左に寄せた引違い戸_" + leaf, leafX / Grid,
                        (p.z + leaf / 2 * .075f) / Grid, w / 4 / Grid, 2.02f, .17f, 1, false);
                    foreach (Renderer pane in sash.GetChild(sash.childCount - 1).GetComponentsInChildren<Renderer>())
                    {
                        pane.sharedMaterial = pane.name == "ガラス" ? glass : frame;
                        if (pane.name == "ガラス") pane.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    }
                }
                CreateBox("開口上枠", new Vector3(p.x, 2.19f, p.z), new Vector3(w, .065f, .10f), GetMaterial("WindowFrame"), sash);
                CreateBox("開口右枠", new Vector3(p.x + w / 2, 1.18f, p.z), new Vector3(.065f, 2.02f, .10f), GetMaterial("WindowFrame"), sash);
                CreateBox("出入口の敷居", new Vector3(p.x, .08f, p.z), new Vector3(w, .16f, .25f), wood, sash);
                float exit = PlanPoint(window.Item2 == 386 ? 405 : 599, 428, 0).x;
                CreateBox("庭への踏み段", new Vector3(exit, -.16f, front - .23f), new Vector3(1.10f, .32f, .46f), wood, deck);
            }
            Transform shoji = house.Find("1階/和室_西_写真ベース/障子・襖・欄間/南側障子_4枚");
            if (shoji != null)
                foreach (string panel in new[] { "障子_02", "障子_03" })
                {
                    Transform leaf = shoji.Find(panel);
                    if (leaf != null) Object.DestroyImmediate(leaf.gameObject);
                }
        }

        [MenuItem("Virtual House/Update And Validate South Engawa All Plans")]
        public static void UpdateAndValidateSouthEngawa()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in new[] { OutputScene, RenovatedScene, AlternativeScene, PlanCScene })
                {
                    Materials.Clear();
                    var scene = EditorSceneManager.OpenScene(path);
                    Transform house = scene.GetRootGameObjects().First(g => g.name == "住宅概形_図面ベース").transform;
                    CreateSouthOutdoorEngawa(house);
                    Physics.SyncTransforms();
                    Transform deck = house.Find("和室南_屋外縁側と床下収納");
                    if (deck.Cast<Transform>().Any(t => t.name.Contains("収納")))
                        throw new InvalidOperationException("Engawa underside must have no storage cases.");
                    foreach (string windowName in new[] { "南廊下西サッシ", "南廊下東サッシ" })
                    {
                        Transform sash = house.Find("外観_写真ベース/腰壁と窓/" + windowName);
                        var leaves = sash.Cast<Transform>().Where(t => t.name.StartsWith("左に寄せた引違い戸_")).ToArray();
                        if (leaves.Length != 4 || leaves.Any(t => !t.GetComponentsInChildren<Renderer>().Any(r => r.name == "ガラス" && r.enabled)))
                            throw new InvalidOperationException("Half-open windows must retain four visible sliding leaves.");
                        var bounds = leaves.SelectMany(t => t.GetComponentsInChildren<Renderer>()).Select(r => r.bounds).ToArray();
                        float retainedWidth = bounds.Max(b => b.max.x) - bounds.Min(b => b.min.x);
                        if (Mathf.Abs(retainedWidth - (66 * PlanScale + .065f)) > .001f)
                            throw new InvalidOperationException("Window leaves must cover half the opening.");
                    }
                    var boards = house.Find("和室南_屋外縁側と床下収納").Cast<Transform>()
                        .Where(t => t.name.StartsWith("縁側床板_")).Select(t => t.GetComponent<Renderer>().bounds).ToArray();
                    float actualDepth = boards.Max(b => b.max.z) - boards.Min(b => b.min.z);
                    if (Mathf.Abs(actualDepth - .91f) > .0001f)
                        throw new InvalidOperationException("Engawa depth must be 0.91m: " + actualDepth);
                    var player = GameObject.Find("Player").GetComponent<CharacterController>();
                    Vector3 saved = player.transform.position;
                    foreach (float x in new[] { 405f, 555f })
                    {
                        float exitX = x == 405 ? x : 599;
                        ValidateAlternativeRoute(new[] { PlanPoint(x, 350, 0), PlanPoint(x, 402, 0),
                            PlanPoint(x, 420, .16f), PlanPoint(x, 450, .16f), PlanPoint(exitX, 450, .16f),
                            PlanPoint(exitX, 458, .16f), PlanPoint(exitX, 472, 0), PlanPoint(exitX, 525, -.33f) });
                    }
                    player.enabled = false; player.transform.position = saved; player.enabled = true;
                    EditorSceneManager.SaveScene(scene);
                    Camera camera = Camera.main;
                    camera.transform.position = PlanPoint(595, 550, 1.65f);
                    camera.transform.LookAt(PlanPoint(455, 430, .50f)); camera.fieldOfView = 65;
                    foreach (TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                        label.GetComponent<Renderer>().enabled = false;
                    RenderCameraToPng(camera, Path.Combine(Application.dataPath, "VirtualHouse/south-engawa-" + Path.GetFileNameWithoutExtension(path) + ".png"));
                    Debug.Log("SOUTH ENGAWA: bidirectional room-to-garden walks passed: " + path);
                }
                AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            }
            finally { if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
