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
        private static void CreateInstagramGlobe(Transform storage)
        {
            Transform old = storage.Find("地球儀_Instagram");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            if (storage.gameObject.scene.path == OutputScene) return;
            Physics.SyncTransforms();
            Bounds shelf = storage.Find("地袋天板").GetComponent<Collider>().bounds;
            Transform globe = NewGroup("地球儀_Instagram", storage);
            globe.position = new Vector3(shelf.center.x, shelf.max.y, shelf.center.z);
            globe.gameObject.AddComponent<HouseInstagramGlobe>();
            Material ocean = WesternMaterial("GlobeOcean", new Color(.12f, .39f, .65f));
            Material land = WesternMaterial("GlobeLand", new Color(.64f, .72f, .40f));
            Material brass = WesternMaterial("GlobeBrass", new Color(.58f, .40f, .17f));
            Material baseMat = WesternMaterial("GlobeBase", new Color(.13f, .09f, .055f));
            brass.SetFloat("_Metallic", .65f); brass.SetFloat("_Smoothness", .5f);

            GameObject Cylinder(string name, Vector3 position, Vector3 scale, Material mat)
            {
                var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                obj.name = name; obj.transform.SetParent(globe, false);
                obj.transform.localPosition = position; obj.transform.localScale = scale;
                obj.GetComponent<Renderer>().sharedMaterial = mat;
                Object.DestroyImmediate(obj.GetComponent<Collider>());
                return obj;
            }
            Cylinder("円形台座", new Vector3(0, .018f, 0), new Vector3(.25f, .018f, .25f), baseMat);
            Cylinder("真鍮支柱", new Vector3(0, .075f, 0), new Vector3(.028f, .04f, .028f), brass);
            Transform axis = NewGroup("地軸_23度", globe);
            axis.localPosition = new Vector3(0, .31f, 0);
            axis.localRotation = Quaternion.Euler(0, 0, -23.4f);
            var earth = new GameObject("地球_海と大陸");
            earth.transform.SetParent(axis, false);
            earth.transform.localRotation = Quaternion.Euler(0, -115, 0);
            earth.AddComponent<MeshFilter>().sharedMesh = CreateGlobeMesh();
            earth.AddComponent<MeshRenderer>().sharedMaterials = new[] { ocean, land };
            var ring = new GameObject("真鍮子午環").AddComponent<LineRenderer>();
            ring.transform.SetParent(axis, false);
            ring.useWorldSpace = false; ring.loop = true; ring.positionCount = 96;
            ring.widthMultiplier = .009f; ring.sharedMaterial = brass;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / ring.positionCount;
                ring.SetPosition(i, new Vector3(0, Mathf.Cos(angle) * .205f, Mathf.Sin(angle) * .205f));
            }
            var plaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plaque.name = "Instagram案内札_背板";
            plaque.transform.SetParent(globe, false);
            plaque.transform.localPosition = new Vector3(.175f, .065f, -.015f);
            plaque.transform.localScale = new Vector3(.40f, .10f, .012f);
            plaque.GetComponent<Renderer>().sharedMaterial = baseMat;
            Object.DestroyImmediate(plaque.GetComponent<Collider>());
            var label = new GameObject("Instagram案内札").AddComponent<TextMesh>();
            label.transform.SetParent(globe, false);
            label.transform.localPosition = new Vector3(.175f, .065f, -.022f);
            label.transform.localRotation = Quaternion.identity;
            label.text = "Instagram\n@yuka_mama_triplife";
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.fontSize = 48; label.characterSize = .007f; label.color = new Color(.94f, .87f, .72f);
        }

        private static Mesh CreateGlobeMesh()
        {
            const string path = "Assets/VirtualHouse/GeneratedMeshes/InstagramGlobe.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            // Simplified geographic silhouettes in longitude/latitude, drawn onto a sphere.
            Vector2[][] continents = {
                new[]{new Vector2(-168,66),new Vector2(-130,72),new Vector2(-105,80),new Vector2(-53,52),new Vector2(-65,45),new Vector2(-82,25),new Vector2(-97,16),new Vector2(-116,31),new Vector2(-130,52)},
                new[]{new Vector2(-82,12),new Vector2(-63,10),new Vector2(-35,-5),new Vector2(-44,-24),new Vector2(-69,-55),new Vector2(-76,-25)},
                new[]{new Vector2(-17,36),new Vector2(10,37),new Vector2(35,30),new Vector2(51,11),new Vector2(39,-10),new Vector2(19,-35),new Vector2(11,-20),new Vector2(9,3),new Vector2(-17,15)},
                new[]{new Vector2(-10,36),new Vector2(-10,59),new Vector2(28,71),new Vector2(80,74),new Vector2(137,70),new Vector2(180,61),new Vector2(145,45),new Vector2(121,24),new Vector2(106,8),new Vector2(92,22),new Vector2(78,7),new Vector2(65,25),new Vector2(40,30),new Vector2(29,41)},
                new[]{new Vector2(113,-22),new Vector2(130,-12),new Vector2(143,-12),new Vector2(153,-28),new Vector2(146,-39),new Vector2(116,-34)},
                new[]{new Vector2(-53,59),new Vector2(-25,67),new Vector2(-40,83),new Vector2(-61,80)},
                new[]{new Vector2(130,31),new Vector2(142,45),new Vector2(145,43),new Vector2(135,32)},
                new[]{new Vector2(46,-13),new Vector2(50,-17),new Vector2(47,-26),new Vector2(44,-23)}
            };
            bool Inside(Vector2 p, Vector2[] polygon)
            {
                bool inside = false;
                for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                    if ((polygon[i].y > p.y) != (polygon[j].y > p.y)
                        && p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                        inside = !inside;
                return inside;
            }
            const int rows = 72, columns = 144;
            var vertices = new Vector3[(rows + 1) * (columns + 1)];
            var triangles = new[] { new List<int>(), new List<int>() };
            for (int y = 0; y <= rows; y++) for (int x = 0; x <= columns; x++)
            {
                float latitude = (-90f + y * 180f / rows) * Mathf.Deg2Rad;
                float longitude = (-180f + x * 360f / columns) * Mathf.Deg2Rad;
                vertices[y * (columns + 1) + x] = new Vector3(Mathf.Cos(latitude) * Mathf.Sin(longitude),
                    Mathf.Sin(latitude), Mathf.Cos(latitude) * Mathf.Cos(longitude)) * .18f;
                if (y == rows || x == columns) continue;
                Vector2 p = new Vector2(-180f + (x + .5f) * 360f / columns, -90f + (y + .5f) * 180f / rows);
                bool land = p.y < -72 || continents.Any(polygon => Inside(p, polygon));
                int a = y * (columns + 1) + x, b = a + columns + 1;
                triangles[land ? 1 : 0].AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
            }
            mesh = new Mesh { name = "地球儀_簡略大陸", vertices = vertices, subMeshCount = 2 };
            mesh.SetTriangles(triangles[0], 0); mesh.SetTriangles(triangles[1], 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        [MenuItem("Virtual House/Update Instagram Globe All Plans")]
        public static void UpdateInstagramGlobeAllPlans()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in new[] { OutputScene, RenovatedScene, AlternativeScene, PlanCScene })
                {
                    Materials.Clear();
                    var scene = EditorSceneManager.OpenScene(path);
                    Transform legacy = GameObject.Find("住宅概形_図面ベース/1階/納戸_棚/地球儀_Instagram")?.transform;
                    if (legacy != null) Object.DestroyImmediate(legacy.gameObject);
                    Transform storage = GameObject.Find("住宅概形_図面ベース/1階/和室_西_写真ベース/北側_押入・床の間・地袋").transform;
                    CreateInstagramGlobe(storage);
                    if (path == OutputScene)
                    {
                        EditorSceneManager.SaveScene(scene);
                        Debug.Log("BEFORE GLOBE ABSENCE VALIDATION PASSED: " + path);
                        continue;
                    }
                    Bounds shelf = storage.Find("地袋天板").GetComponent<Collider>().bounds;
                    Transform globe = storage.Find("地球儀_Instagram");
                    Physics.SyncTransforms();
                    // Renderer.bounds rotates the enclosing cube and overestimates a tilted sphere.
                    var earthMesh = globe.Find("地軸_23度/地球_海と大陸").GetComponent<MeshFilter>();
                    Bounds earth = new Bounds(earthMesh.transform.position, Vector3.zero);
                    foreach (Vector3 vertex in earthMesh.sharedMesh.vertices)
                        earth.Encapsulate(earthMesh.transform.TransformPoint(vertex));
                    if (earth.min.y < shelf.max.y || earth.min.x < shelf.min.x || earth.max.x > shelf.max.x
                        || earth.min.z < shelf.min.z || earth.max.z > shelf.max.z)
                        throw new InvalidOperationException("Globe is not supported by west shelf: earth=" + earth + "; shelf=" + shelf);
                    var guide = globe.GetComponent<HouseInstagramGlobe>();
                    Vector3 approach = new Vector3(shelf.center.x, .1f, shelf.min.z - .6f);
                    if (!guide.IsInRange(approach) || guide.IsInRange(approach + Vector3.up * 3f)
                        || guide.IsInRange(approach + Vector3.right * 3f))
                        throw new InvalidOperationException("Globe proximity link range is incorrect");
                    ValidateNandoOpening();
                    EditorSceneManager.SaveScene(scene);
                    Camera camera = Camera.main;
                    camera.transform.position = globe.position + new Vector3(.35f, .65f, -1.9f);
                    camera.transform.LookAt(globe.position + Vector3.up * .27f); camera.fieldOfView = 48;
                    foreach (var text in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                        if (text.transform.parent != globe) text.GetComponent<Renderer>().enabled = false;
                    RenderCameraToPng(camera, Path.Combine(Application.dataPath, "VirtualHouse/instagram-globe-" + Path.GetFileNameWithoutExtension(path) + ".png"));
                    Debug.Log("INSTAGRAM GLOBE VALIDATION PASSED: " + path);
                }
                AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            }
            finally { if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        public static void UpdateInstagramGlobeAndBuildWebGL()
        {
            UpdateInstagramGlobeAllPlans();
            WebBuild.BuildWebGL();
        }
    }
}
