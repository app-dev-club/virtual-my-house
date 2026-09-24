using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualHouse.Editor
{
    /// <summary>
    /// Generates an editable blockout from the supplied hand-drawn floor plan.
    /// One grid cell is half of one tsubo side: 1.81818 m / 2 = 0.90909 m.
    /// </summary>
    [InitializeOnLoad]
    public static class HouseBlockoutGenerator
    {
        private const float Grid = 0.90909f;
        private const float WallHeight = 2.7f;
        private const float WallThickness = 0.14f;
        private const float FloorThickness = 0.12f;
        private const float SecondFloorY = 3.0f;
        private const string OutputScene = "Assets/Scenes/HouseBlockout.unity";
        private const string MaterialFolder = "Assets/VirtualHouse/Materials";

        private static readonly Dictionary<string, Material> Materials = new();

        static HouseBlockoutGenerator()
        {
            // The first import creates the deliverable without disturbing the user's open scene.
            // Subsequent imports never overwrite an existing blockout automatically.
            if (!Application.isBatchMode)
                EditorApplication.delayCall += GenerateIfMissing;
        }

        [MenuItem("Virtual House/Regenerate Floor Plan Blockout")]
        public static void Generate()
        {
            EnsureFolders();
            Materials.Clear();

            Scene previousScene = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "HouseBlockout";
            SceneManager.SetActiveScene(scene);

            GameObject house = new("住宅概形_図面ベース");
            CreateReferenceGrid(house.transform);
            CreateGround(house.transform);
            CreateFirstFloor(house.transform);
            CreateSecondFloor(house.transform);
            CreateLightingAndCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, OutputScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = house;
            Debug.Log($"House blockout generated: {OutputScene}");

            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void GenerateIfMissing()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += GenerateIfMissing;
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OutputScene) != null)
                return;

            try
            {
                Generate();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void CreateFirstFloor(Transform root)
        {
            Transform floor = NewGroup("1階", root);
            Transform rooms = NewGroup("床_部屋別", floor);
            Transform walls = NewGroup("壁", floor);
            Transform fittings = NewGroup("建具_目安", floor);

            // Major spaces reconstructed from the plan. Dimensions are in plan-grid cells.
            Room(rooms, "8帖洋室", 0f, 0f, 4f, 4f, 0f, "FloorWood");
            Room(rooms, "8帖和室_西", 4f, 1f, 4f, 4f, 0f, "Tatami");
            Room(rooms, "8帖和室_東", 8f, 1f, 4f, 4f, 0f, "Tatami");
            Room(rooms, "8帖DK", 12f, 0f, 4f, 4f, 0f, "FloorWood");
            Room(rooms, "納戸", 2f, 4f, 2f, 2f, 0f, "Storage");
            Room(rooms, "南側廊下", 4f, 0f, 8f, 1f, 0f, "Hall");
            Room(rooms, "北側廊下", 4f, 5f, 8f, 2f, 0f, "Hall");
            Room(rooms, "階段ホール", 11f, 4f, 2f, 2f, 0f, "Hall");
            Room(rooms, "脱衣室", 13f, 4f, 2f, 2f, 0f, "Wet");
            Room(rooms, "浴室", 15f, 5f, 2f, 2f, 0f, "Bath");
            Room(rooms, "洗面", 13f, 6f, 2f, 2f, 0f, "Wet");
            Room(rooms, "WC", 12f, 7f, 1f, 1f, 0f, "Wet");
            Room(rooms, "給湯", 15f, 7f, 2f, 1f, 0f, "Utility");
            Room(rooms, "物入", 16f, 2f, 2f, 2f, 0f, "Storage");
            Room(rooms, "玄関", 11f, -2f, 2f, 2f, -0.08f, "Entry");

            // Fill the circulation/utility footprint between named rooms.
            FloorBox(rooms, "北側床補完", 4f, 7f, 8f, 1f, 0f, "Hall");
            FloorBox(rooms, "水回り通路", 12f, 6f, 1f, 1f, 0f, "Hall");
            FloorBox(rooms, "浴室南側床", 15f, 4f, 2f, 1f, 0f, "Wet");
            FloorBox(rooms, "東側接続床", 17f, 4f, 1f, 2f, 0f, "Storage");

            // Exterior outline. The entrance, storage and wet-area projections follow the drawing.
            WallX(walls, "外壁_南西", 0f, 11f, 0f, 0f);
            WallZ(walls, "外壁_玄関西", 11f, -2f, 0f, 0f);
            WallXWithOpening(walls, fittings, "外壁_玄関正面", 11f, 13f, -2f, 0f, 12f, 1.05f, "玄関扉");
            WallZ(walls, "外壁_玄関東", 13f, -2f, 0f, 0f);
            WallX(walls, "外壁_南東", 13f, 16f, 0f, 0f);
            WallZ(walls, "外壁_DK東", 16f, 0f, 2f, 0f);
            WallX(walls, "外壁_物入南", 16f, 18f, 2f, 0f);
            WallZ(walls, "外壁_東", 18f, 2f, 6f, 0f);
            WallX(walls, "外壁_東北段差", 17f, 18f, 6f, 0f);
            WallZ(walls, "外壁_北東", 17f, 6f, 8f, 0f);
            WallX(walls, "外壁_北", 4f, 17f, 8f, 0f);
            WallZ(walls, "外壁_北西", 4f, 6f, 8f, 0f);
            WallX(walls, "外壁_納戸北", 2f, 4f, 6f, 0f);
            WallZ(walls, "外壁_納戸西", 2f, 4f, 6f, 0f);
            WallX(walls, "外壁_洋室北", 0f, 2f, 4f, 0f);
            WallZ(walls, "外壁_西", 0f, 0f, 4f, 0f);

            // Principal partitions, with representative door/fusuma openings.
            WallZWithOpening(walls, fittings, "間仕切_洋室", 4f, 0f, 5f, 0f, 1.1f, 0.9f, "洋室扉");
            WallZWithOpening(walls, fittings, "間仕切_和室間", 8f, 1f, 5f, 0f, 1.8f, 1.4f, "襖_和室間");
            WallZWithOpening(walls, fittings, "間仕切_和室DK", 12f, 0f, 5f, 0f, 1.2f, 0.9f, "DK扉");
            WallXWithOpening(walls, fittings, "間仕切_和室西南", 4f, 8f, 1f, 0f, 6.2f, 1.6f, "襖_和室西");
            WallXWithOpening(walls, fittings, "間仕切_和室東南", 8f, 12f, 1f, 0f, 10.1f, 1.6f, "襖_和室東");
            WallXWithOpening(walls, fittings, "間仕切_和室西北", 4f, 8f, 5f, 0f, 6.2f, 1.4f, "襖_北西");
            WallXWithOpening(walls, fittings, "間仕切_和室東北", 8f, 12f, 5f, 0f, 10.3f, 1.2f, "襖_北東");
            WallXWithOpening(walls, fittings, "間仕切_DK北", 12f, 16f, 4f, 0f, 12.7f, 0.9f, "DK北扉");
            WallZWithOpening(walls, fittings, "間仕切_脱衣西", 13f, 4f, 6f, 0f, 5f, 0.8f, "脱衣扉");
            WallZWithOpening(walls, fittings, "間仕切_浴室西", 15f, 4f, 8f, 0f, 5.5f, 0.8f, "浴室扉");
            WallXWithOpening(walls, fittings, "間仕切_洗面南", 13f, 15f, 6f, 0f, 14f, 0.8f, "洗面入口");
            WallXWithOpening(walls, fittings, "間仕切_WC南", 12f, 13f, 7f, 0f, 12.5f, 0.65f, "WC扉");
            WallZWithOpening(walls, fittings, "間仕切_物入西", 16f, 2f, 4f, 0f, 3f, 1.1f, "物入扉");
            WallXWithOpening(walls, fittings, "間仕切_納戸南", 2f, 4f, 4f, 0f, 3.2f, 0.8f, "納戸扉");

            CreateStairs(floor, 11.25f, 4.1f, 1.35f, 3.4f, 0f);
        }

        private static void CreateSecondFloor(Transform root)
        {
            Transform floor = NewGroup("2階", root);
            Transform rooms = NewGroup("床_部屋別", floor);
            Transform walls = NewGroup("壁", floor);
            Transform fittings = NewGroup("建具_目安", floor);

            // 9 x 5 cells = about 37.2 square metres, matching the noted 37.31 m2 closely.
            Room(rooms, "8帖和室", 4f, 4f, 4f, 4f, SecondFloorY, "Tatami");
            Room(rooms, "4.5帖和室", 9f, 5f, 3f, 3f, SecondFloorY, "Tatami");
            Room(rooms, "2階ホール", 8f, 3f, 5f, 2f, SecondFloorY, "Hall");
            Room(rooms, "2階廊下", 8f, 5f, 1f, 3f, SecondFloorY, "Hall");
            Room(rooms, "2階収納", 12f, 5f, 1f, 3f, SecondFloorY, "Storage");
            FloorBox(rooms, "2階南西床補完", 4f, 3f, 4f, 1f, SecondFloorY, "Hall");

            WallX(walls, "外壁_2階南", 4f, 13f, 3f, SecondFloorY);
            WallZ(walls, "外壁_2階東", 13f, 3f, 8f, SecondFloorY);
            WallX(walls, "外壁_2階北", 4f, 13f, 8f, SecondFloorY);
            WallZ(walls, "外壁_2階西", 4f, 3f, 8f, SecondFloorY);

            WallZWithOpening(walls, fittings, "間仕切_8帖東", 8f, 3f, 8f, SecondFloorY, 4.6f, 1.2f, "襖_8帖");
            WallZWithOpening(walls, fittings, "間仕切_4.5帖西", 9f, 5f, 8f, SecondFloorY, 5.8f, 0.9f, "襖_4.5帖");
            WallXWithOpening(walls, fittings, "間仕切_4.5帖南", 9f, 12f, 5f, SecondFloorY, 10f, 0.9f, "4.5帖入口");
            WallZWithOpening(walls, fittings, "間仕切_収納西", 12f, 5f, 8f, SecondFloorY, 6.2f, 1.0f, "収納扉");
        }

        private static void CreateStairs(Transform parent, float x, float z, float widthCells, float lengthCells, float baseY)
        {
            Transform stairs = NewGroup("階段_概形", parent);
            const int steps = 14;
            float width = widthCells * Grid;
            float depth = lengthCells * Grid / steps;
            float rise = SecondFloorY / steps;

            for (int i = 0; i < steps; i++)
            {
                float height = rise * (i + 1);
                Vector3 center = new((x + widthCells * 0.5f) * Grid, baseY + height * 0.5f,
                    (z + (i + 0.5f) * lengthCells / steps) * Grid);
                CreateBox($"段_{i + 1:00}", center, new Vector3(width, height, depth),
                    GetMaterial("Stair"), stairs);
            }
        }

        private static void Room(Transform parent, string name, float x, float z, float width, float depth, float y, string material)
        {
            Transform room = NewGroup(name, parent);
            FloorBox(room, "床", x, z, width, depth, y, material);
            CreateLabel(room, name, x + width * 0.5f, z + depth * 0.5f, y + 0.08f);
        }

        private static void FloorBox(Transform parent, string name, float x, float z, float width, float depth, float y, string material)
        {
            Vector3 center = new((x + width * 0.5f) * Grid, y - FloorThickness * 0.5f, (z + depth * 0.5f) * Grid);
            Vector3 size = new(width * Grid, FloorThickness, depth * Grid);
            CreateBox(name, center, size, GetMaterial(material), parent);
        }

        private static void WallX(Transform parent, string name, float x1, float x2, float z, float floorY)
        {
            float length = Mathf.Abs(x2 - x1) * Grid;
            Vector3 center = new((x1 + x2) * 0.5f * Grid, floorY + WallHeight * 0.5f, z * Grid);
            CreateBox(name, center, new Vector3(length, WallHeight, WallThickness), GetMaterial("Wall"), parent);
        }

        private static void WallZ(Transform parent, string name, float x, float z1, float z2, float floorY)
        {
            float length = Mathf.Abs(z2 - z1) * Grid;
            Vector3 center = new(x * Grid, floorY + WallHeight * 0.5f, (z1 + z2) * 0.5f * Grid);
            CreateBox(name, center, new Vector3(WallThickness, WallHeight, length), GetMaterial("Wall"), parent);
        }

        private static void WallXWithOpening(Transform walls, Transform fittings, string name, float x1, float x2,
            float z, float floorY, float openingCenter, float openingWidth, string doorName)
        {
            float leftEnd = openingCenter - openingWidth * 0.5f;
            float rightStart = openingCenter + openingWidth * 0.5f;
            if (leftEnd > x1) WallX(walls, name + "_左", x1, leftEnd, z, floorY);
            if (rightStart < x2) WallX(walls, name + "_右", rightStart, x2, z, floorY);
            DoorX(fittings, doorName, openingCenter, z, openingWidth, floorY);
        }

        private static void WallZWithOpening(Transform walls, Transform fittings, string name, float x, float z1,
            float z2, float floorY, float openingCenter, float openingWidth, string doorName)
        {
            float lowerEnd = openingCenter - openingWidth * 0.5f;
            float upperStart = openingCenter + openingWidth * 0.5f;
            if (lowerEnd > z1) WallZ(walls, name + "_南", x, z1, lowerEnd, floorY);
            if (upperStart < z2) WallZ(walls, name + "_北", x, upperStart, z2, floorY);
            DoorZ(fittings, doorName, x, openingCenter, openingWidth, floorY);
        }

        private static void DoorX(Transform parent, string name, float x, float z, float width, float floorY)
        {
            Vector3 center = new(x * Grid, floorY + 1.0f, z * Grid);
            CreateBox(name, center, new Vector3(width * Grid, 2.0f, 0.045f), GetMaterial("Door"), parent);
        }

        private static void DoorZ(Transform parent, string name, float x, float z, float width, float floorY)
        {
            Vector3 center = new(x * Grid, floorY + 1.0f, z * Grid);
            CreateBox(name, center, new Vector3(0.045f, 2.0f, width * Grid), GetMaterial("Door"), parent);
        }

        private static void CreateLabel(Transform parent, string text, float x, float z, float y)
        {
            GameObject label = new("ラベル_" + text);
            label.transform.SetParent(parent, false);
            label.transform.position = new Vector3(x * Grid, y, z * Grid);
            label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = 0.16f;
            mesh.fontSize = 42;
            mesh.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        }

        private static void CreateReferenceGrid(Transform root)
        {
            Transform grid = NewGroup("基準グリッド_1マス0.909m", root);
            Material material = GetMaterial("Grid");
            const float lineWidth = 0.012f;
            float y = -0.085f;

            for (int x = -3; x <= 20; x++)
            {
                Vector3 center = new(x * Grid, y, 3.5f * Grid);
                CreateBox($"縦_{x}", center, new Vector3(lineWidth, 0.01f, 13f * Grid), material, grid);
            }

            for (int z = -3; z <= 10; z++)
            {
                Vector3 center = new(8.5f * Grid, y, z * Grid);
                CreateBox($"横_{z}", center, new Vector3(23f * Grid, 0.01f, lineWidth), material, grid);
            }
        }

        private static void CreateGround(Transform root)
        {
            CreateBox("敷地_仮", new Vector3(7.5f, -0.18f, 2.8f), new Vector3(24f, 0.18f, 16f),
                GetMaterial("Ground"), root);
        }

        private static void CreateLightingAndCamera()
        {
            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.95f, 0.86f);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            cameraObject.transform.position = new Vector3(17f, 19f, -17f);
            cameraObject.transform.LookAt(new Vector3(7.5f, 1.3f, 3.2f));
        }

        private static GameObject CreateBox(string name, Vector3 position, Vector3 size, Material material, Transform parent)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, true);
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private static Transform NewGroup(string name, Transform parent)
        {
            GameObject group = new(name);
            group.transform.SetParent(parent, false);
            return group.transform;
        }

        private static Material GetMaterial(string key)
        {
            if (Materials.TryGetValue(key, out Material cached)) return cached;

            string path = $"{MaterialFolder}/{key}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = key };
                material.color = key switch
                {
                    "Wall" => new Color(0.91f, 0.89f, 0.84f),
                    "FloorWood" => new Color(0.62f, 0.40f, 0.22f),
                    "Tatami" => new Color(0.65f, 0.70f, 0.40f),
                    "Hall" => new Color(0.73f, 0.55f, 0.34f),
                    "Wet" => new Color(0.65f, 0.78f, 0.82f),
                    "Bath" => new Color(0.50f, 0.68f, 0.77f),
                    "Storage" => new Color(0.66f, 0.58f, 0.48f),
                    "Entry" => new Color(0.35f, 0.36f, 0.37f),
                    "Utility" => new Color(0.54f, 0.60f, 0.62f),
                    "Door" => new Color(0.35f, 0.19f, 0.09f),
                    "Stair" => new Color(0.55f, 0.34f, 0.17f),
                    "Ground" => new Color(0.30f, 0.45f, 0.25f),
                    "Grid" => new Color(0.20f, 0.55f, 0.75f),
                    _ => Color.white
                };
                AssetDatabase.CreateAsset(material, path);
            }

            Materials[key] = material;
            return material;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/VirtualHouse"))
                AssetDatabase.CreateFolder("Assets", "VirtualHouse");
            if (!AssetDatabase.IsValidFolder("Assets/VirtualHouse/Materials"))
                AssetDatabase.CreateFolder("Assets/VirtualHouse", "Materials");
        }
    }
}
