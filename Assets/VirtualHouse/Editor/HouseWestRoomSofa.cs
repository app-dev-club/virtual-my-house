using System;
using System.Collections.Generic;
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
        private const string SofaFolder = "Assets/VirtualHouse/Sofa";
        private const string SofaName = "西和室_北東L字ローソファ_195x195cm";

        // Reference: 1P x 2 (51 x 75), 2P (102 x 75), corner (93 x 93), H46, seat H16 cm.
        private static void CreateWestRoomSofa(Transform first)
        {
            Transform old = first.Find(SofaName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            if (!AssetDatabase.IsValidFolder(SofaFolder)) AssetDatabase.CreateFolder("Assets/VirtualHouse", "Sofa");
            Transform sofa = NewGroup(SofaName, first);
            Material fabric = WesternMaterial("SofaBrownWoven", new Color(.34f, .30f, .26f));
            Material baseFabric = WesternMaterial("SofaDarkBase", new Color(.15f, .12f, .105f));
            Material pillow = WesternMaterial("SofaWarmGreyPillow", new Color(.57f, .55f, .51f));
            foreach (Material mat in new[] { fabric, baseFabric, pillow })
            {
                mat.SetFloat("_Glossiness", .08f);
                mat.SetFloat("_Smoothness", .08f);
            }
            Texture2D weave = AssetDatabase.LoadAssetAtPath<Texture2D>(SofaFolder + "/Weave.asset");
            if (weave == null)
            {
                weave = new Texture2D(64, 64) { name = "Woven upholstery", wrapMode = TextureWrapMode.Repeat };
                var random = new System.Random(195);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float v = .78f + (float)random.NextDouble() * .12f + ((x % 3 == 0 || y % 3 == 0) ? .10f : 0);
                    weave.SetPixel(x, y, new Color(v, v, v));
                }
                weave.Apply();
                AssetDatabase.CreateAsset(weave, SofaFolder + "/Weave.asset");
            }
            foreach (Material mat in new[] { fabric, baseFabric, pillow })
            {
                mat.mainTexture = weave; mat.mainTextureScale = new Vector2(8, 8);
                EditorUtility.SetDirty(mat);
            }
            // Local origin is the outer NE corner; backs face north/east, seats face SW.
            void Cushion(string name, Vector3 center, Vector3 size, Material mat, float tilt = 0, float yaw = 0)
            {
                string key = size.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" +
                    size.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" +
                    size.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
                string path = SofaFolder + "/Cushion_" + key + ".asset";
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null) { mesh = CreateSofaCushionMesh(size); AssetDatabase.CreateAsset(mesh, path); }
                GameObject part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                part.transform.SetParent(sofa, false);
                part.transform.localPosition = center;
                part.transform.localRotation = Quaternion.Euler(tilt, yaw, 0);
                part.GetComponent<MeshFilter>().sharedMesh = mesh;
                part.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
            void Module(string name, float x, float z, float width, float depth, bool eastBack, bool northBack)
            {
                Cushion(name + "_台座", new Vector3(x, .035f, z), new Vector3(width, .07f, depth), baseFabric);
                float seatX = eastBack ? x - .105f : x;
                float seatZ = northBack ? z - .105f : z;
                float seatW = width - (eastBack ? .21f : 0), seatD = depth - (northBack ? .21f : 0);
                Cushion(name + "_座面", new Vector3(seatX, .11f, seatZ), new Vector3(seatW - .008f, .10f, seatD - .008f), fabric);
                if (northBack)
                {
                    Cushion(name + "_背台北", new Vector3(x, .25f, z + depth / 2 - .07f), new Vector3(width, .40f, .14f), baseFabric);
                    Cushion(name + "_腰当北", new Vector3(x, .24f, z + depth / 2 - .16f), new Vector3(width - .012f, .16f, .16f), fabric, -8);
                    Cushion(name + "_背上北", new Vector3(x, .371f, z + depth / 2 - .13f), new Vector3(width - .012f, .15f, .20f), fabric, -8);
                }
                if (eastBack)
                {
                    float backDepth = depth - (northBack ? .20f : 0);
                    float backZ = z - (northBack ? .10f : 0);
                    Cushion(name + "_背台東", new Vector3(x + width / 2 - .07f, .25f, backZ), new Vector3(.14f, .40f, backDepth), baseFabric);
                    Cushion(name + "_腰当東", new Vector3(x + width / 2 - .16f, .24f, backZ), new Vector3(backDepth - .012f, .16f, .16f), fabric, -8, 90);
                    Cushion(name + "_背上東", new Vector3(x + width / 2 - .13f, .371f, backZ), new Vector3(backDepth - .012f, .15f, .20f), fabric, -8, 90);
                }
                var collider = sofa.gameObject.AddComponent<BoxCollider>();
                collider.center = new Vector3(x, .23f, z); collider.size = new Vector3(width, .46f, depth);
            }
            Module("コーナー93cm", -.465f, -.465f, .93f, .93f, true, true);
            // The two 1P modules run along the north; the 2P runs along the east.
            Module("1P西51cm", -1.695f, -.375f, .51f, .75f, false, true);
            Module("1P中央51cm", -1.185f, -.375f, .51f, .75f, false, true);
            Module("2P南102cm_座席1", -.375f, -1.185f, .75f, .51f, true, false);
            Module("2P南102cm_座席2", -.375f, -1.695f, .75f, .51f, true, false);
            Cushion("置きクッション北", new Vector3(-1.50f, .285f, -.37f), new Vector3(.30f, .25f, .09f), pillow, -14, -8);
            Cushion("置きクッション東", new Vector3(-.37f, .285f, -1.34f), new Vector3(.30f, .25f, .09f), pillow, -14, 90);
            Vector3 corner = PlanPoint(462, 216, .036f);
            sofa.localPosition = corner + new Vector3(-.14f, 0, -.14f);
        }

        // Rounded boxes retain flat upholstered faces and softly bulging cushion edges.
        private static Mesh CreateSofaCushionMesh(Vector3 size)
        {
            const int subdivisions = 8;
            float radius = Mathf.Min(.045f, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .40f);
            Vector3 half = size / 2, inner = half - Vector3.one * radius;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            foreach (Vector3 normal in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
            {
                Vector3 a = Vector3.Cross(normal, Mathf.Abs(normal.y) > .5f ? Vector3.forward : Vector3.up);
                Vector3 b = Vector3.Cross(normal, a);
                int start = vertices.Count;
                for (int y = 0; y <= subdivisions; y++) for (int x = 0; x <= subdivisions; x++)
                {
                    Vector3 cube = normal + a * (x * 2f / subdivisions - 1) + b * (y * 2f / subdivisions - 1);
                    Vector3 p = Vector3.Scale(cube, half);
                    Vector3 closest = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    vertices.Add(closest + (p - closest).normalized * radius);
                    uv.Add(new Vector2(x / (float)subdivisions, y / (float)subdivisions));
                    if (x < subdivisions && y < subdivisions)
                    {
                        int i = start + y * (subdivisions + 1) + x;
                        triangles.AddRange(new[] { i, i + 1, i + subdivisions + 2, i, i + subdivisions + 2, i + subdivisions + 1 });
                    }
                }
            }
            Mesh mesh = new Mesh { name = "Rounded upholstered cushion" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        [MenuItem("Virtual House/Update And Validate West Room Sofa A-C")]
        public static void UpdateAndValidateWestRoomSofa()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in new[] { RenovatedScene, AlternativeScene, PlanCScene })
                {
                    Materials.Clear();
                    var scene = EditorSceneManager.OpenScene(path);
                    Transform house = scene.GetRootGameObjects().First(g => g.name == "住宅概形_図面ベース").transform;
                    CreateWestRoomSofa(house.Find("1階"));
                    Physics.SyncTransforms();
                    Transform sofa = house.Find("1階/" + SofaName);
                    var units = sofa.GetComponents<BoxCollider>();
                    Bounds bounds = units[0].bounds;
                    foreach (var unit in units) bounds.Encapsulate(unit.bounds);
                    if (Mathf.Abs(bounds.size.x - 1.95f) > .001f || Mathf.Abs(bounds.size.z - 1.95f) > .001f || Mathf.Abs(bounds.size.y - .46f) > .001f)
                        throw new InvalidOperationException("Sofa dimensions differ from reference");
                    foreach (var unit in units)
                    foreach (var hit in Physics.OverlapBox(unit.bounds.center, unit.bounds.extents - Vector3.one * .005f))
                        if (!hit.transform.IsChildOf(sofa)) throw new InvalidOperationException("Sofa collision: " + hit.name);
                    ValidateNandoOpening();
                    EditorSceneManager.SaveScene(scene);
                    foreach (TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) label.GetComponent<Renderer>().enabled = false;
                    Camera camera = Camera.main; camera.orthographic = false; camera.fieldOfView = 65;
                    camera.transform.position = sofa.position + new Vector3(-2.7f, 2.35f, -3.0f);
                    camera.transform.LookAt(sofa.position + new Vector3(-.9f, .18f, -.9f));
                    RenderCameraToPng(camera, Path.Combine(Application.dataPath, "VirtualHouse/sofa-" + Path.GetFileNameWithoutExtension(path) + ".png"));
                    Debug.Log("SOFA VALIDATED: " + path + " 195x195x46cm; seat 16cm; NE, backs N/E");
                }
                AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            }
            finally { if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
