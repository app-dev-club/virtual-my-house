using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VirtualHouse;

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
            bool restorePreviousScene = !Application.isBatchMode && previousScene.IsValid() && previousScene.isLoaded;
            NewSceneMode sceneMode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, sceneMode);
            scene.name = "HouseBlockout";
            SceneManager.SetActiveScene(scene);

            GameObject house = new("住宅概形_図面ベース");
            CreateReferenceGrid(house.transform);
            CreateGround(house.transform);
            CreateFirstFloor(house.transform);
            CreateSecondFloor(house.transform);
            CreateExterior(house.transform);
            CreateLightingAndPlayer();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, OutputScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = house;
            Debug.Log($"House blockout generated: {OutputScene}");

            if (restorePreviousScene)
            {
                SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void GenerateAndRenderPreviews()
        {
            Generate();
            RenderFloorPlanPreview();
            RenderPreview();
            RenderEntrancePreview();
        }

        [MenuItem("Virtual House/Render Exterior Preview")]
        public static void RenderPreview()
        {
            Scene scene = EditorSceneManager.OpenScene(OutputScene, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
                throw new System.InvalidOperationException("HouseBlockout scene has no Main Camera.");

            camera.transform.position = new Vector3(18.5f, 5.8f, -16.5f);
            camera.transform.LookAt(new Vector3(8.2f, 2.7f, 2.2f));
            camera.fieldOfView = 44f;
            foreach (TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                label.GetComponent<MeshRenderer>().enabled = false;

            const int width = 1280;
            const int height = 900;
            RenderTexture target = new(width, height, 24);
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();
            camera.targetTexture = null;
            RenderTexture.active = previous;

            string output = Path.Combine(Application.dataPath, "VirtualHouse", "house-exterior-preview.png");
            File.WriteAllBytes(output, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log($"Exterior preview rendered: {output} ({scene.name})");
        }

        [MenuItem("Virtual House/Render Entrance Preview")]
        public static void RenderEntrancePreview()
        {
            Scene scene = EditorSceneManager.OpenScene(OutputScene, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
                throw new System.InvalidOperationException("HouseBlockout scene has no Main Camera.");

            camera.transform.position = new Vector3(13f * Grid, 1.48f, -0.72f * Grid);
            camera.transform.LookAt(new Vector3(13.48f * Grid, 1.28f, 4.85f * Grid));
            camera.fieldOfView = 72f;
            foreach (TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                label.GetComponent<MeshRenderer>().enabled = false;

            const int width = 1280;
            const int height = 900;
            RenderTexture target = new(width, height, 24);
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();
            camera.targetTexture = null;
            RenderTexture.active = previous;

            string output = Path.Combine(Application.dataPath, "VirtualHouse", "entrance-interior-preview.png");
            File.WriteAllBytes(output, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log($"Entrance preview rendered: {output} ({scene.name})");
        }

        [MenuItem("Virtual House/Render Floor Plan Preview")]
        public static void RenderFloorPlanPreview()
        {
            Scene scene = EditorSceneManager.OpenScene(OutputScene, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
                throw new System.InvalidOperationException("HouseBlockout scene has no Main Camera.");

            GameObject exterior = GameObject.Find("外観_写真ベース");
            if (exterior != null) exterior.SetActive(false);
            GameObject firstFloor = GameObject.Find("1階");
            GameObject secondFloor = GameObject.Find("2階");

            camera.transform.position = new Vector3(9f * Grid, 20f, 2.5f * Grid);
            camera.transform.LookAt(new Vector3(9f * Grid, 0f, 2.5f * Grid), Vector3.forward);
            camera.orthographic = true;
            camera.orthographicSize = 6.2f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;

            if (secondFloor != null) secondFloor.SetActive(false);
            RenderCameraToPng(camera, Path.Combine(Application.dataPath, "VirtualHouse", "floor-plan-preview.png"));

            if (firstFloor != null) firstFloor.SetActive(false);
            if (secondFloor != null) secondFloor.SetActive(true);
            camera.transform.position = new Vector3(8.5f * Grid, 20f, 5.5f * Grid);
            camera.transform.LookAt(new Vector3(8.5f * Grid, SecondFloorY, 5.5f * Grid), Vector3.forward);
            camera.orthographicSize = 3.6f;
            RenderCameraToPng(camera, Path.Combine(Application.dataPath, "VirtualHouse", "second-floor-plan-preview.png"));

            AssetDatabase.Refresh();
            Debug.Log($"Floor-plan previews rendered ({scene.name})");
        }

        private static void RenderCameraToPng(Camera camera, string output)
        {
            const int width = 1600;
            const int height = 900;
            RenderTexture target = new(width, height, 24);
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();
            camera.targetTexture = null;
            RenderTexture.active = previous;

            File.WriteAllBytes(output, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
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

            // Redrawn plan: 1.82 m in the legend equals two grid cells.
            // Thick strokes become walls; dotted strokes become full-height openings.
            Room(rooms, "8帖洋室", 0f, 0f, 4f, 4f, 0f, "FloorWood");
            Room(rooms, "8帖和室_西", 4f, 1f, 4f, 4f, 0f, "Tatami");
            Room(rooms, "8帖和室_東", 8f, 1f, 4f, 4f, 0f, "Tatami");
            Room(rooms, "8帖DK", 14f, 0f, 4f, 4f, 0f, "FloorWood");
            Room(rooms, "納戸", 2f, 4f, 2f, 2f, 0f, "Storage");
            Room(rooms, "南側廊下", 4f, 0f, 8f, 1f, 0f, "Hall");
            Room(rooms, "北側物入_西", 4f, 5f, 2f, 1f, 0f, "Storage");
            Room(rooms, "北側物入_中央", 6f, 5f, 2f, 1f, 0f, "Storage");
            Room(rooms, "北側物入_東", 8f, 5f, 2f, 1f, 0f, "Storage");
            Room(rooms, "北側板間", 10f, 5f, 2f, 1f, 0f, "Hall");
            Room(rooms, "縦廊下", 13f, 0f, 1f, 6f, 0f, "Hall");
            Room(rooms, "WC_西", 12f, 6f, 1f, 1f, 0f, "Wet");
            Room(rooms, "WC_東", 13f, 6f, 1f, 1f, 0f, "Wet");
            Room(rooms, "脱衣室", 14f, 3f, 2f, 1f, 0f, "Wet");
            Room(rooms, "洗面", 14f, 5f, 1f, 1f, 0f, "Wet");
            Room(rooms, "水回り物入", 14f, 4f, 1f, 1f, 0f, "Storage");
            Room(rooms, "浴室", 15f, 4f, 2f, 2f, 0f, "Bath");
            Room(rooms, "給湯", 17f, 4f, 1f, 2f, 0f, "Utility");
            Room(rooms, "玄関", 12f, -2f, 2f, 2f, -0.08f, "Entry");

            // Exterior outline. The entrance, storage and wet-area projections follow the drawing.
            WallXWithWindowOpening(walls, "外壁_南西窓", 0f, 4.2f, 0f, 0f, 2f, 3.55f, 0.18f, 2.05f);
            WallXWithWindowOpening(walls, "外壁_南中央窓", 4.2f, 12f, 0f, 0f, 7.5f, 5.8f, 0.18f, 2.05f);
            WallZ(walls, "外壁_玄関西", 12f, -2f, 0f, 0f);
            WallXWithOpening(walls, fittings, "外壁_玄関正面", 12f, 14f, -2f, 0f, 13f, 1.05f, "玄関扉");
            WallZ(walls, "外壁_玄関東", 14f, -2f, 0f, 0f);
            WallXWithWindowOpening(walls, "外壁_南東窓", 14f, 18f, 0f, 0f, 16f, 2.25f, 0.82f, 1.25f);
            WallZWithWindowOpening(walls, "外壁_東南窓", 18f, 0f, 3.2f, 0f, 1.4f, 1.7f, 0.8f, 1.25f);
            WallZWithOpening(walls, fittings, "外壁_東勝手口", 18f, 3.2f, 5.2f, 0f, 4.75f, 0.9f, "勝手口");
            WallZWithWindowOpening(walls, "外壁_東北窓", 18f, 5.2f, 6f, 0f, 5.55f, 0.7f, 0.95f, 1.05f);
            WallXWithWindowOpening(walls, "外壁_水回り北", 14f, 18f, 6f, 0f, 16f, 1.65f, 0.85f, 1.15f);
            WallX(walls, "外壁_WC北", 12f, 14f, 7f, 0f);
            WallZ(walls, "外壁_WC西", 12f, 6f, 7f, 0f);
            WallZ(walls, "外壁_WC東", 14f, 6f, 7f, 0f);
            WallXWithWindowOpening(walls, "外壁_北中央窓", 4f, 12f, 6f, 0f, 8f, 2.2f, 0.72f, 1.35f);
            WallZ(walls, "外壁_北西", 4f, 5f, 6f, 0f);
            WallXWithWindowOpening(walls, "外壁_納戸北窓", 2f, 4f, 6f, 0f, 3f, 1.35f, 0.9f, 1.15f);
            WallZ(walls, "外壁_納戸西", 2f, 4f, 6f, 0f);
            WallX(walls, "外壁_洋室北", 0f, 2f, 4f, 0f);
            WallZWithWindowOpening(walls, "外壁_西窓", 0f, 0f, 4f, 0f, 1.75f, 3f, 0.18f, 2.05f);

            // Principal partitions. Opening widths follow the dotted portions of the drawing.
            WallZWithOpening(walls, fittings, "間仕切_洋室", 4f, 0f, 5f, 0f, 1.1f, 0.9f, "洋室扉");
            WallZWithOpening(walls, fittings, "間仕切_和室間", 8f, 1f, 5f, 0f, 3f, 2.1f, "襖_和室間");
            WallZ(walls, "間仕切_和室階段", 12f, 1f, 5f, 0f);
            WallZWithOpening(walls, fittings, "間仕切_廊下DK", 14f, 0f, 4f, 0f, 2f, 3.1f, "DK入口");
            WallXWithOpening(walls, fittings, "間仕切_和室西南", 4f, 8f, 1f, 0f, 6f, 3.1f, "襖_和室西");
            WallXWithOpening(walls, fittings, "間仕切_和室東南", 8f, 12f, 1f, 0f, 10f, 3.1f, "襖_和室東");
            WallXWithOpening(walls, fittings, "間仕切_和室西北", 4f, 8f, 5f, 0f, 6f, 3.5f, "襖_北西");
            WallXWithOpening(walls, fittings, "間仕切_和室東北", 8f, 12f, 5f, 0f, 10f, 3.5f, "襖_北東");
            WallXWithOpening(walls, fittings, "間仕切_DK北", 14f, 18f, 4f, 0f, 16f, 2.2f, "水回り入口");
            WallZWithOpening(walls, fittings, "間仕切_水回り西", 14f, 3f, 6f, 0f, 4.85f, 2.1f, "水回り引戸");
            WallXWithOpening(walls, fittings, "間仕切_脱衣南", 14f, 16f, 3f, 0f, 14.5f, 0.8f, "脱衣入口");
            WallZWithOpening(walls, fittings, "間仕切_浴室西", 15f, 4f, 6f, 0f, 4.55f, 0.75f, "浴室扉");
            WallZ(walls, "間仕切_浴室給湯", 17f, 4f, 6f, 0f);
            WallXWithOpening(walls, fittings, "間仕切_洗面南", 14f, 15f, 5f, 0f, 14.5f, 0.85f, "洗面入口");
            WallXWithOpening(walls, fittings, "間仕切_WC南", 12f, 14f, 6f, 0f, 13f, 1.8f, "WC入口");
            WallZ(walls, "間仕切_WC間", 13f, 6f, 7f, 0f);
            WallXWithOpening(walls, fittings, "間仕切_納戸南", 2f, 4f, 4f, 0f, 3.2f, 0.8f, "納戸扉");

            CreateStairs(floor, 12f, 1f, 1f, 4f, 0f);
            CreateEntranceInterior(floor);
        }

        private static void CreateSecondFloor(Transform root)
        {
            Transform floor = NewGroup("2階", root);
            Transform rooms = NewGroup("床_部屋別", floor);
            Transform walls = NewGroup("壁", floor);
            Transform fittings = NewGroup("建具_目安", floor);

            // The redrawn second floor has corridors along both eaves and the stair on the east side.
            // Dotted runs are represented as wide wall openings.
            Room(rooms, "8帖和室", 4.5f, 4f, 4f, 3f, SecondFloorY, "Tatami");
            Room(rooms, "4.5帖和室", 9f, 4f, 3f, 3f, SecondFloorY, "Tatami");
            Room(rooms, "北側廊下", 4.5f, 7f, 7.5f, 1f, SecondFloorY, "Hall");
            Room(rooms, "南側廊下", 4f, 3f, 8f, 1f, SecondFloorY, "Hall");
            Room(rooms, "物入_南西", 4f, 3f, 0.5f, 1f, SecondFloorY, "Storage");
            Room(rooms, "物入_中央北", 8.5f, 5.8f, 0.5f, 1.2f, SecondFloorY, "Storage");
            Room(rooms, "物入_中央南", 8.5f, 4f, 0.5f, 1.2f, SecondFloorY, "Storage");
            Room(rooms, "物入_南東", 12f, 3f, 1f, 1f, SecondFloorY, "Storage");
            FloorBox(rooms, "階段上踊り場", 12f, 7f, 1f, 1f, SecondFloorY, "Hall");

            WallXWithWindowOpening(walls, "外壁_2階南西窓", 4f, 8.3f, 3f, SecondFloorY, 6.1f, 2.25f, 0.72f, 1.5f);
            WallXWithWindowOpening(walls, "外壁_2階南東窓", 8.3f, 13f, 3f, SecondFloorY, 10.6f, 2.25f, 0.72f, 1.5f);
            WallZWithWindowOpening(walls, "外壁_2階東窓", 13f, 3f, 8f, SecondFloorY, 5.5f, 1.7f, 0.75f, 1.4f);
            WallXWithWindowOpening(walls, "外壁_2階北西窓", 4f, 8.4f, 8f, SecondFloorY, 6.15f, 1.8f, 0.78f, 1.35f);
            WallXWithWindowOpening(walls, "外壁_2階北東窓", 8.4f, 13f, 8f, SecondFloorY, 10.75f, 1.8f, 0.78f, 1.35f);
            WallZWithWindowOpening(walls, "外壁_2階西窓", 4f, 3f, 8f, SecondFloorY, 5.5f, 1.7f, 0.75f, 1.4f);

            WallXWithOpening(walls, fittings, "間仕切_北側廊下", 4.5f, 12f, 7f, SecondFloorY, 10f, 3.8f, "北廊下開口");
            WallXWithOpening(walls, fittings, "間仕切_南側廊下", 4f, 12f, 4f, SecondFloorY, 8.2f, 7.0f, "南廊下開口");
            WallZWithOpening(walls, fittings, "間仕切_8帖東", 8.5f, 4f, 7f, SecondFloorY, 5.5f, 2.6f, "8帖側開口");
            WallZWithOpening(walls, fittings, "間仕切_4.5帖西", 9f, 4f, 7f, SecondFloorY, 5.5f, 2.2f, "4.5帖側開口");
            WallZ(walls, "間仕切_階段西", 12f, 4f, 7f, SecondFloorY);
        }

        /// <summary>
        /// Photo-derived exterior. Absolute dimensions remain estimates, but the placement is tied to
        /// the plan grid so every part can be adjusted independently after generation.
        /// </summary>
        private static void CreateExterior(Transform root)
        {
            Transform exterior = NewGroup("外観_写真ベース", root);
            Transform cladding = NewGroup("外壁仕上げ", exterior);
            Transform openings = NewGroup("窓と玄関", exterior);
            Transform roofs = NewGroup("屋根と庇", exterior);
            Transform drainage = NewGroup("雨樋", exterior);

            // The photographs show a rough, dark pebble-dash skirt wrapping the ground floor.
            CladdingX(cladding, "南面腰壁_西", 0f, 12f, -0.09f);
            CladdingX(cladding, "南面腰壁_東", 14f, 18f, -0.09f);
            // The entrance is deliberately left as an unobstructed opening for the walkthrough.
            CladdingX(cladding, "玄関正面腰壁_左袖", 12f, 12.18f, -2.09f);
            CladdingX(cladding, "玄関正面腰壁_右袖", 13.82f, 14f, -2.09f);
            CladdingX(cladding, "北面腰壁", 2f, 18f, 6.09f);
            CladdingZ(cladding, "西面腰壁", -0.09f, 0f, 4f);
            CladdingZ(cladding, "東面腰壁", 18.09f, 0f, 6f);
            CladdingZ(cladding, "北西張出し腰壁", 1.91f, 4f, 6f);

            // South/front elevation: two broad sets of sliding doors and the recessed entrance.
            WindowX(openings, "南西掃出し窓", 2.0f, -0.085f, 3.55f, 2.05f, 0.18f, 4, true);
            WindowX(openings, "南中央掃出し窓", 7.5f, -0.085f, 5.8f, 2.05f, 0.18f, 6, true);
            WindowX(openings, "南東腰窓", 16f, -0.085f, 2.25f, 1.25f, 0.82f, 2, true);
            DoorX(openings, "玄関引違い戸", 13f, -2.085f, 1.65f, 0f);
            AddPorch(openings, 13f * Grid, -2.45f * Grid);

            // The long first-floor side visible in photos 1, 9 and 16-18.
            WindowZ(openings, "西南掃出し窓", -0.085f, 1.75f, 3.0f, 2.05f, 0.18f, 4, true);
            WindowZ(openings, "西北掃出し窓", 1.915f, 5.0f, 1.5f, 1.55f, 0.55f, 2, true);

            // Rear/service elevations use smaller frosted windows, matching the photo sequence.
            WindowX(openings, "北西腰窓", 3.0f, 6.085f, 1.35f, 1.15f, 0.9f, 2, true);
            WindowX(openings, "北中央窓", 8f, 6.085f, 2.2f, 1.35f, 0.72f, 3, true);
            WindowX(openings, "北東窓", 16f, 6.085f, 1.65f, 1.15f, 0.85f, 2, true);
            WindowZ(openings, "東南腰窓", 18.085f, 1.4f, 1.7f, 1.25f, 0.8f, 2, true);
            DoorZ(openings, "東勝手口", 18.085f, 4.75f, 0.9f, 0f);
            WindowZ(openings, "東北窓", 18.085f, 5.55f, 0.8f, 1.05f, 0.95f, 1, true);

            // Second storey: two windows along each eave elevation and one on each gable end.
            WindowX(openings, "2階南西窓", 6.1f, 2.915f, 2.25f, 1.5f, 3.72f, 3, true);
            WindowX(openings, "2階南東窓", 10.6f, 2.915f, 2.25f, 1.5f, 3.72f, 3, true);
            WindowX(openings, "2階北西窓", 6.15f, 8.085f, 1.8f, 1.35f, 3.78f, 2, true);
            WindowX(openings, "2階北東窓", 10.75f, 8.085f, 1.8f, 1.35f, 3.78f, 2, true);
            WindowZ(openings, "2階西妻窓", 3.915f, 5.5f, 1.7f, 1.4f, 3.75f, 2, true);
            WindowZ(openings, "2階東妻窓", 13.085f, 5.5f, 1.7f, 1.4f, 3.75f, 2, true);

            // Main tiled gable. Ridge runs along the long axis of the second floor.
            const float mainRoofAngle = 27f;
            float mainHalfRun = 2.75f;
            float mainSlopeLength = mainHalfRun / Mathf.Cos(mainRoofAngle * Mathf.Deg2Rad);
            float mainRise = mainHalfRun * Mathf.Tan(mainRoofAngle * Mathf.Deg2Rad);
            float mainEaveY = 5.72f;
            float mainCenterY = mainEaveY + mainRise * 0.5f;
            float mainLength = 9.8f * Grid;
            float mainCenterX = 8.5f * Grid;
            float ridgeZ = 5.5f * Grid;
            CreateGableWall(roofs, "西妻壁", 4f * Grid - 0.02f, ridgeZ, 4.55f, 5.68f, mainRoofAngle);
            CreateGableWall(roofs, "東妻壁", 13f * Grid + 0.02f, ridgeZ, 4.55f, 5.68f, mainRoofAngle);
            CreateRotatedBox("主屋根_南面", new Vector3(mainCenterX, mainCenterY, ridgeZ - mainHalfRun * 0.5f),
                new Vector3(mainLength, 0.13f, mainSlopeLength), new Vector3(-mainRoofAngle, 0f, 0f), GetMaterial("UpperRoof"), roofs);
            CreateRotatedBox("主屋根_北面", new Vector3(mainCenterX, mainCenterY, ridgeZ + mainHalfRun * 0.5f),
                new Vector3(mainLength, 0.13f, mainSlopeLength), new Vector3(mainRoofAngle, 0f, 0f), GetMaterial("UpperRoof"), roofs);
            CreateBox("主屋根_棟", new Vector3(mainCenterX, mainEaveY + mainRise + 0.06f, ridgeZ),
                new Vector3(mainLength + 0.12f, 0.18f, 0.20f), GetMaterial("UpperRoofRidge"), roofs);

            // One-storey hipped/shed roofs reconstructed as editable overlapping roof planes.
            ShedRoofX(roofs, "南側下屋", 0f, 18.5f, -2.55f, 3.05f, 3.26f, 2.72f, true);
            ShedRoofX(roofs, "北側下屋", 1.7f, 18.4f, 5.75f, 6.75f, 3.18f, 2.82f, false);
            ShedRoofZ(roofs, "西側下屋", -0.65f, 4.15f, -0.2f, 6.3f, 2.76f, 3.18f, true);
            ShedRoofZ(roofs, "東側下屋", 12.85f, 18.65f, -0.2f, 6.25f, 3.20f, 2.75f, false);
            CanopyX(roofs, "玄関庇", 13f * Grid, -2.55f * Grid, 2.75f, 2.45f, 1.25f);

            // Characteristic dark-red metal gutters and downpipes.
            GutterX(drainage, "南雨樋", -0.35f, 18.4f, -2.58f, 2.68f);
            GutterX(drainage, "北雨樋", 1.5f, 18.6f, 6.78f, 2.78f);
            Downpipe(drainage, "南西縦樋", 0.15f, -0.18f, 2.65f);
            Downpipe(drainage, "玄関縦樋", 14.05f, -2.1f, 2.8f);
            Downpipe(drainage, "北西縦樋", 2.0f, 6.05f, 2.8f);
            Downpipe(drainage, "北東縦樋", 18.0f, 5.9f, 2.8f);
        }

        private static void CreateStairs(Transform parent, float x, float z, float widthCells, float lengthCells, float baseY)
        {
            Transform stairs = NewGroup("木階段_写真ベース", parent);
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

            float xCenter = (x + widthCells * 0.5f) * Grid;
            float zStart = z * Grid;
            float run = lengthCells * Grid;
            float railAngle = -Mathf.Atan2(SecondFloorY, run) * Mathf.Rad2Deg;
            float railLength = Mathf.Sqrt(run * run + SecondFloorY * SecondFloorY);
            float railY = baseY + SecondFloorY * 0.5f + 0.79f;
            float railZ = zStart + run * 0.5f;

            // Leave a character-width gap in the left rail at the upper landing.
            const float landingExitGap = 0.72f;
            float leftRun = run - landingExitGap;
            float leftRise = SecondFloorY * leftRun / run;
            float leftRailLength = Mathf.Sqrt(leftRun * leftRun + leftRise * leftRise);

            CreateRotatedBox("手すり_左", new Vector3(xCenter - width * 0.5f + 0.045f,
                    baseY + leftRise * 0.5f + 0.79f, zStart + leftRun * 0.5f),
                new Vector3(0.07f, 0.08f, leftRailLength), new Vector3(railAngle, 0f, 0f),
                GetMaterial("DarkWood"), stairs);
            CreateRotatedBox("手すり_右", new Vector3(xCenter + width * 0.5f - 0.045f, railY, railZ),
                new Vector3(0.07f, 0.08f, railLength), new Vector3(railAngle, 0f, 0f),
                GetMaterial("DarkWood"), stairs);

            for (int i = 0; i < steps; i += 2)
            {
                float treadY = baseY + rise * (i + 1);
                float balusterZ = zStart + depth * (i + 0.5f);
                if (balusterZ < zStart + leftRun)
                    CreateBox($"親柱_左_{i + 1:00}", new Vector3(xCenter - width * 0.5f + 0.045f, treadY + 0.39f, balusterZ),
                        new Vector3(0.055f, 0.78f, 0.055f), GetMaterial("DarkWood"), stairs);
                CreateBox($"親柱_右_{i + 1:00}", new Vector3(xCenter + width * 0.5f - 0.045f, treadY + 0.39f, balusterZ),
                    new Vector3(0.055f, 0.78f, 0.055f), GetMaterial("DarkWood"), stairs);
            }
        }

        private static void CreateEntranceInterior(Transform parent)
        {
            Transform interior = NewGroup("玄関周り_写真ベース", parent);
            Transform surfaces = NewGroup("土間と廊下", interior);
            Transform timber = NewGroup("柱梁と天井", interior);
            Transform fittings = NewGroup("格子建具と下駄箱", interior);
            Transform lights = NewGroup("照明", interior);

            float entryCenterX = 13f * Grid;
            float entryCenterZ = -1f * Grid;
            float entryWidth = 2f * Grid;
            float entryDepth = 2f * Grid;

            // The photographed green concrete doma sits below the raised timber floor.
            CreateBox("緑色モルタル土間", new Vector3(entryCenterX, -0.052f, entryCenterZ),
                new Vector3(entryWidth - 0.10f, 0.055f, entryDepth - 0.06f), GetMaterial("GenkanGreen"), surfaces);
            CreateBox("上がり框", new Vector3(entryCenterX, 0.045f, 0.035f),
                new Vector3(entryWidth - 0.04f, 0.09f, 0.18f), GetMaterial("DarkWood"), surfaces);

            // Parquet-like boards reproduce the warm, small-square hallway floor visible in the photos.
            const int boardRows = 12;
            for (int i = 0; i < boardRows; i++)
            {
                float zCenter = (0.16f + i * 0.31f) * Grid;
                Material boardMaterial = GetMaterial(i % 2 == 0 ? "HallLight" : "HallDark");
                CreateBox($"廊下板_{i + 1:00}_西", new Vector3(13.28f * Grid, 0.012f, zCenter),
                    new Vector3(0.52f * Grid, 0.024f, 0.29f * Grid), boardMaterial, surfaces);
                CreateBox($"廊下板_{i + 1:00}_東", new Vector3(13.74f * Grid, 0.013f, zCenter),
                    new Vector3(0.38f * Grid, 0.026f, 0.29f * Grid),
                    GetMaterial(i % 2 == 0 ? "HallDark" : "HallLight"), surfaces);
            }

            // Rough pale wall panels framed with exposed light timber (traditional shinkabe construction).
            CreateBox("砂壁_玄関西", new Vector3(12.075f * Grid, 1.30f, entryCenterZ),
                new Vector3(0.035f, 2.52f, entryDepth - 0.12f), GetMaterial("Plaster"), timber);
            CreateBox("砂壁_玄関東", new Vector3(13.925f * Grid, 1.30f, entryCenterZ),
                new Vector3(0.035f, 2.52f, entryDepth - 0.12f), GetMaterial("Plaster"), timber);
            foreach (float postX in new[] { 12.10f, 13.90f })
            {
                foreach (float postZ in new[] { -1.93f, -0.05f })
                    CreateBox($"玄関柱_{postX:0.00}_{postZ:0.00}", new Vector3(postX * Grid, 1.31f, postZ * Grid),
                        new Vector3(0.105f, 2.62f, 0.105f), GetMaterial("LightWood"), timber);
            }
            CreateBox("玄関鴨居", new Vector3(entryCenterX, 2.36f, -1.94f * Grid),
                new Vector3(entryWidth, 0.13f, 0.12f), GetMaterial("DarkWood"), timber);
            CreateBox("玄関天井", new Vector3(entryCenterX, 2.62f, entryCenterZ),
                new Vector3(entryWidth, 0.10f, entryDepth), GetMaterial("Ceiling"), timber);
            CreateBox("廊下天井", new Vector3(13.52f * Grid, 2.62f, 1.90f * Grid),
                new Vector3(1.12f * Grid, 0.10f, 3.80f * Grid), GetMaterial("Ceiling"), timber);
            for (int i = 0; i <= 5; i++)
            {
                float beamZ = (0.15f + i * 0.70f) * Grid;
                CreateBox($"廊下天井桟_{i + 1:00}", new Vector3(13.52f * Grid, 2.555f, beamZ),
                    new Vector3(1.12f * Grid, 0.045f, 0.055f), GetMaterial("DarkWood"), timber);
            }

            CreateLatticePanelZ(fittings, "縦格子引戸", 13.94f, 2.46f, 1.62f);
            CreateShoeCabinet(fittings, 13.66f * Grid, -0.54f * Grid);

            CreateWarmLight(lights, "玄関灯", new Vector3(entryCenterX, 2.38f, entryCenterZ), 4.0f, 0.72f);
            CreateWarmLight(lights, "廊下灯", new Vector3(13.52f * Grid, 2.40f, 2.25f * Grid), 3.6f, 0.62f);
        }

        private static void CreateLatticePanelZ(Transform parent, string name, float xCells, float zCells, float lengthCells)
        {
            Transform panel = NewGroup(name, parent);
            float x = xCells * Grid;
            float z = zCells * Grid;
            float length = lengthCells * Grid;
            const float bottom = 0.08f;
            const float height = 2.12f;

            CreateBox("和紙面", new Vector3(x, bottom + height * 0.5f, z),
                new Vector3(0.028f, height - 0.16f, length - 0.08f), GetMaterial("Paper"), panel);
            CreateBox("枠_上", new Vector3(x - 0.018f, bottom + height, z),
                new Vector3(0.075f, 0.075f, length), GetMaterial("DarkWood"), panel);
            CreateBox("枠_下", new Vector3(x - 0.018f, bottom, z),
                new Vector3(0.075f, 0.075f, length), GetMaterial("DarkWood"), panel);
            CreateBox("枠_前", new Vector3(x - 0.018f, bottom + height * 0.5f, z - length * 0.5f),
                new Vector3(0.075f, height, 0.075f), GetMaterial("DarkWood"), panel);
            CreateBox("枠_後", new Vector3(x - 0.018f, bottom + height * 0.5f, z + length * 0.5f),
                new Vector3(0.075f, height, 0.075f), GetMaterial("DarkWood"), panel);
            for (int i = 1; i < 9; i++)
            {
                float barZ = z - length * 0.5f + length * i / 9f;
                CreateBox($"縦格子_{i:00}", new Vector3(x - 0.035f, bottom + height * 0.5f, barZ),
                    new Vector3(0.055f, height, 0.028f), GetMaterial("LightWood"), panel);
            }
            CreateBox("中桟", new Vector3(x - 0.04f, bottom + 0.82f, z),
                new Vector3(0.06f, 0.075f, length), GetMaterial("LightWood"), panel);
        }

        private static void CreateShoeCabinet(Transform parent, float x, float z)
        {
            Transform cabinet = NewGroup("造付け下駄箱", parent);
            const float width = 0.58f;
            const float depth = 0.72f;
            const float height = 1.02f;
            CreateBox("側板_左", new Vector3(x - width * 0.5f, height * 0.5f, z),
                new Vector3(0.035f, height, depth), GetMaterial("Cabinet"), cabinet);
            CreateBox("側板_右", new Vector3(x + width * 0.5f, height * 0.5f, z),
                new Vector3(0.035f, height, depth), GetMaterial("Cabinet"), cabinet);
            CreateBox("天板", new Vector3(x, height, z),
                new Vector3(width + 0.08f, 0.055f, depth + 0.06f), GetMaterial("DarkWood"), cabinet);
            CreateBox("背板", new Vector3(x, height * 0.5f, z + depth * 0.5f),
                new Vector3(width, height, 0.035f), GetMaterial("Cabinet"), cabinet);
            for (int i = 0; i < 5; i++)
            {
                float shelfY = 0.08f + i * 0.19f;
                CreateBox($"棚_{i + 1:00}", new Vector3(x, shelfY, z),
                    new Vector3(width, 0.028f, depth), GetMaterial("Cabinet"), cabinet);
            }
        }

        private static void CreateWarmLight(Transform parent, string name, Vector3 position, float range, float intensity)
        {
            GameObject lightObject = new(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = range;
            light.intensity = intensity;
            light.color = new Color(1f, 0.67f, 0.42f);
            CreateBox("乳白ガラスカバー", position - new Vector3(0f, 0.05f, 0f),
                new Vector3(0.24f, 0.10f, 0.24f), GetMaterial("Paper"), parent);
        }

        private static void CladdingX(Transform parent, string name, float x1, float x2, float z)
        {
            CreateBox(name, new Vector3((x1 + x2) * 0.5f * Grid, 0.72f, z * Grid),
                new Vector3(Mathf.Abs(x2 - x1) * Grid, 1.12f, 0.055f), GetMaterial("Cladding"), parent);
        }

        private static void CladdingZ(Transform parent, string name, float x, float z1, float z2)
        {
            CreateBox(name, new Vector3(x * Grid, 0.72f, (z1 + z2) * 0.5f * Grid),
                new Vector3(0.055f, 1.12f, Mathf.Abs(z2 - z1) * Grid), GetMaterial("Cladding"), parent);
        }

        private static void WindowX(Transform parent, string name, float x, float z, float width, float height,
            float bottom, int columns, bool awning)
        {
            Transform window = NewGroup(name, parent);
            float widthM = width * Grid;
            float xM = x * Grid;
            float zM = z * Grid;
            float frame = 0.065f;
            CreateBox("ガラス", new Vector3(xM, bottom + height * 0.5f, zM),
                new Vector3(widthM - frame * 2f, height - frame * 2f, 0.045f), GetMaterial("Glass"), window);
            CreateBox("枠_上", new Vector3(xM, bottom + height, zM - 0.008f),
                new Vector3(widthM, frame, 0.075f), GetMaterial("WindowFrame"), window);
            CreateBox("枠_下", new Vector3(xM, bottom, zM - 0.008f),
                new Vector3(widthM, frame, 0.075f), GetMaterial("WindowFrame"), window);
            CreateBox("枠_左", new Vector3(xM - widthM * 0.5f, bottom + height * 0.5f, zM - 0.008f),
                new Vector3(frame, height, 0.075f), GetMaterial("WindowFrame"), window);
            CreateBox("枠_右", new Vector3(xM + widthM * 0.5f, bottom + height * 0.5f, zM - 0.008f),
                new Vector3(frame, height, 0.075f), GetMaterial("WindowFrame"), window);
            for (int i = 1; i < columns; i++)
            {
                float mullionX = xM - widthM * 0.5f + widthM * i / columns;
                CreateBox($"縦桟_{i}", new Vector3(mullionX, bottom + height * 0.5f, zM - 0.012f),
                    new Vector3(frame * 0.72f, height, 0.08f), GetMaterial("WindowFrame"), window);
            }
            if (height > 1.7f)
                CreateBox("中桟", new Vector3(xM, bottom + height * 0.46f, zM - 0.014f),
                    new Vector3(widthM, frame * 0.8f, 0.08f), GetMaterial("WindowFrame"), window);
            if (awning)
                CreateRotatedBox("庇", new Vector3(xM, bottom + height + 0.17f, zM - 0.25f),
                    new Vector3(widthM + 0.34f, 0.09f, 0.55f), new Vector3(-6f, 0f, 0f), GetMaterial("Awning"), window);
        }

        private static void WindowZ(Transform parent, string name, float x, float z, float width, float height,
            float bottom, int columns, bool awning)
        {
            Transform window = NewGroup(name, parent);
            float widthM = width * Grid;
            float xM = x * Grid;
            float zM = z * Grid;
            float frame = 0.065f;
            CreateBox("ガラス", new Vector3(xM, bottom + height * 0.5f, zM),
                new Vector3(0.045f, height - frame * 2f, widthM - frame * 2f), GetMaterial("Glass"), window);
            CreateBox("枠_上", new Vector3(xM, bottom + height, zM),
                new Vector3(0.075f, frame, widthM), GetMaterial("WindowFrame"), window);
            CreateBox("枠_下", new Vector3(xM, bottom, zM),
                new Vector3(0.075f, frame, widthM), GetMaterial("WindowFrame"), window);
            CreateBox("枠_左", new Vector3(xM, bottom + height * 0.5f, zM - widthM * 0.5f),
                new Vector3(0.075f, height, frame), GetMaterial("WindowFrame"), window);
            CreateBox("枠_右", new Vector3(xM, bottom + height * 0.5f, zM + widthM * 0.5f),
                new Vector3(0.075f, height, frame), GetMaterial("WindowFrame"), window);
            for (int i = 1; i < columns; i++)
            {
                float mullionZ = zM - widthM * 0.5f + widthM * i / columns;
                CreateBox($"縦桟_{i}", new Vector3(xM, bottom + height * 0.5f, mullionZ),
                    new Vector3(0.08f, height, frame * 0.72f), GetMaterial("WindowFrame"), window);
            }
            if (height > 1.7f)
                CreateBox("中桟", new Vector3(xM, bottom + height * 0.46f, zM),
                    new Vector3(0.08f, frame * 0.8f, widthM), GetMaterial("WindowFrame"), window);
            if (awning)
                CreateRotatedBox("庇", new Vector3(xM - 0.25f, bottom + height + 0.17f, zM),
                    new Vector3(0.55f, 0.09f, widthM + 0.34f), new Vector3(0f, 0f, 6f), GetMaterial("Awning"), window);
        }

        private static void AddPorch(Transform parent, float x, float z)
        {
            Transform porch = NewGroup("玄関ポーチ", parent);
            CreateBox("土間", new Vector3(x, 0.02f, z), new Vector3(2.15f, 0.16f, 1.45f), GetMaterial("Concrete"), porch);
            CreateBox("左柱", new Vector3(x - 0.86f, 1.25f, z - 0.48f), new Vector3(0.10f, 2.5f, 0.10f), GetMaterial("Post"), porch);
            CreateBox("右柱", new Vector3(x + 0.86f, 1.25f, z - 0.48f), new Vector3(0.10f, 2.5f, 0.10f), GetMaterial("Post"), porch);
        }

        private static void ShedRoofX(Transform parent, string name, float x1, float x2, float z1, float z2,
            float innerY, float outerY, bool highAtPositiveZ)
        {
            float z1M = z1 * Grid;
            float z2M = z2 * Grid;
            float y1 = highAtPositiveZ ? outerY : innerY;
            float y2 = highAtPositiveZ ? innerY : outerY;
            float run = Mathf.Abs(z2M - z1M);
            float angle = -Mathf.Atan2(y2 - y1, z2M - z1M) * Mathf.Rad2Deg;
            float slopeLength = Mathf.Sqrt(run * run + (y2 - y1) * (y2 - y1));
            CreateRotatedBox(name, new Vector3((x1 + x2) * 0.5f * Grid, (y1 + y2) * 0.5f, (z1M + z2M) * 0.5f),
                new Vector3(Mathf.Abs(x2 - x1) * Grid, 0.11f, slopeLength), new Vector3(angle, 0f, 0f), GetMaterial("LowerRoof"), parent);
        }

        private static void ShedRoofZ(Transform parent, string name, float x1, float x2, float z1, float z2,
            float innerY, float outerY, bool highAtPositiveX)
        {
            float x1M = x1 * Grid;
            float x2M = x2 * Grid;
            float y1 = highAtPositiveX ? outerY : innerY;
            float y2 = highAtPositiveX ? innerY : outerY;
            float run = Mathf.Abs(x2M - x1M);
            float angle = Mathf.Atan2(y2 - y1, x2M - x1M) * Mathf.Rad2Deg;
            float slopeLength = Mathf.Sqrt(run * run + (y2 - y1) * (y2 - y1));
            CreateRotatedBox(name, new Vector3((x1M + x2M) * 0.5f, (y1 + y2) * 0.5f, (z1 + z2) * 0.5f * Grid),
                new Vector3(slopeLength, 0.11f, Mathf.Abs(z2 - z1) * Grid), new Vector3(0f, 0f, angle), GetMaterial("LowerRoof"), parent);
        }

        private static void CanopyX(Transform parent, string name, float x, float z, float y, float width, float depth)
        {
            CreateRotatedBox(name, new Vector3(x, y, z), new Vector3(width, 0.12f, depth),
                new Vector3(5f, 0f, 0f), GetMaterial("LowerRoof"), parent);
        }

        private static void CreateGableWall(Transform parent, string name, float x, float centerZ, float width,
            float baseY, float roofAngle)
        {
            Transform gable = NewGroup(name, parent);
            const int segments = 12;
            float segmentWidth = width / segments;
            float halfWidth = width * 0.5f;
            for (int i = 0; i < segments; i++)
            {
                float localZ = -halfWidth + segmentWidth * (i + 0.5f);
                float height = (halfWidth - Mathf.Abs(localZ)) * Mathf.Tan(roofAngle * Mathf.Deg2Rad);
                CreateBox($"妻壁_{i + 1:00}", new Vector3(x, baseY + height * 0.5f, centerZ + localZ),
                    new Vector3(0.16f, height, segmentWidth + 0.015f), GetMaterial("Wall"), gable);
            }
        }

        private static void GutterX(Transform parent, string name, float x1, float x2, float z, float y)
        {
            CreateBox(name, new Vector3((x1 + x2) * 0.5f * Grid, y, z * Grid),
                new Vector3(Mathf.Abs(x2 - x1) * Grid, 0.12f, 0.12f), GetMaterial("Gutter"), parent);
        }

        private static void Downpipe(Transform parent, string name, float x, float z, float height)
        {
            CreateBox(name, new Vector3(x * Grid, height * 0.5f, z * Grid),
                new Vector3(0.105f, height, 0.105f), GetMaterial("Gutter"), parent);
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

        private static void WallXWithWindowOpening(Transform walls, string name, float x1, float x2, float z,
            float floorY, float openingCenter, float openingWidth, float openingBottom, float openingHeight)
        {
            float leftEnd = openingCenter - openingWidth * 0.5f;
            float rightStart = openingCenter + openingWidth * 0.5f;
            if (leftEnd > x1) WallX(walls, name + "_左", x1, leftEnd, z, floorY);
            if (rightStart < x2) WallX(walls, name + "_右", rightStart, x2, z, floorY);

            float width = openingWidth * Grid;
            float centerX = openingCenter * Grid;
            if (openingBottom > 0f)
                CreateBox(name + "_腰壁", new Vector3(centerX, floorY + openingBottom * 0.5f, z * Grid),
                    new Vector3(width, openingBottom, WallThickness), GetMaterial("Wall"), walls);
            float headerHeight = WallHeight - openingBottom - openingHeight;
            if (headerHeight > 0f)
                CreateBox(name + "_垂壁", new Vector3(centerX, floorY + openingBottom + openingHeight + headerHeight * 0.5f, z * Grid),
                    new Vector3(width, headerHeight, WallThickness), GetMaterial("Wall"), walls);
        }

        private static void WallZWithWindowOpening(Transform walls, string name, float x, float z1, float z2,
            float floorY, float openingCenter, float openingWidth, float openingBottom, float openingHeight)
        {
            float lowerEnd = openingCenter - openingWidth * 0.5f;
            float upperStart = openingCenter + openingWidth * 0.5f;
            if (lowerEnd > z1) WallZ(walls, name + "_南", x, z1, lowerEnd, floorY);
            if (upperStart < z2) WallZ(walls, name + "_北", x, upperStart, z2, floorY);

            float width = openingWidth * Grid;
            float centerZ = openingCenter * Grid;
            if (openingBottom > 0f)
                CreateBox(name + "_腰壁", new Vector3(x * Grid, floorY + openingBottom * 0.5f, centerZ),
                    new Vector3(WallThickness, openingBottom, width), GetMaterial("Wall"), walls);
            float headerHeight = WallHeight - openingBottom - openingHeight;
            if (headerHeight > 0f)
                CreateBox(name + "_垂壁", new Vector3(x * Grid, floorY + openingBottom + openingHeight + headerHeight * 0.5f, centerZ),
                    new Vector3(WallThickness, headerHeight, width), GetMaterial("Wall"), walls);
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
            // Door models are intentionally omitted so walkthrough routes remain open.
        }

        private static void DoorZ(Transform parent, string name, float x, float z, float width, float floorY)
        {
            // Door models are intentionally omitted so walkthrough routes remain open.
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
            mesh.characterSize = 0.085f;
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

        private static void CreateLightingAndPlayer()
        {
            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.95f, 0.86f);
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            GameObject playerObject = new("Player");
            // Start in the genkan, facing the hallway and stair, with the open exterior behind the player.
            playerObject.transform.position = new Vector3(13f * Grid, 0.03f, -1.15f * Grid);
            playerObject.transform.rotation = Quaternion.identity;

            CharacterController controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.28f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 50f;

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerObject.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 65f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;

            HouseFirstPersonController player = playerObject.AddComponent<HouseFirstPersonController>();
            SerializedObject serializedPlayer = new(player);
            serializedPlayer.FindProperty("view").objectReferenceValue = cameraObject.transform;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
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

        private static GameObject CreateRotatedBox(string name, Vector3 position, Vector3 size, Vector3 euler,
            Material material, Transform parent)
        {
            GameObject box = CreateBox(name, position, size, material, parent);
            box.transform.rotation = Quaternion.Euler(euler);
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
                    "Cladding" => new Color(0.25f, 0.23f, 0.20f),
                    "UpperRoof" => new Color(0.16f, 0.30f, 0.42f),
                    "UpperRoofRidge" => new Color(0.10f, 0.21f, 0.31f),
                    "LowerRoof" => new Color(0.48f, 0.08f, 0.07f),
                    "Awning" => new Color(0.82f, 0.80f, 0.74f),
                    "WindowFrame" => new Color(0.73f, 0.74f, 0.72f),
                    "Glass" => new Color(0.40f, 0.53f, 0.58f),
                    "Gutter" => new Color(0.34f, 0.08f, 0.07f),
                    "Concrete" => new Color(0.48f, 0.47f, 0.44f),
                    "Post" => new Color(0.16f, 0.14f, 0.12f),
                    "GenkanGreen" => new Color(0.24f, 0.40f, 0.34f),
                    "Plaster" => new Color(0.76f, 0.75f, 0.68f),
                    "LightWood" => new Color(0.64f, 0.46f, 0.28f),
                    "DarkWood" => new Color(0.30f, 0.17f, 0.085f),
                    "HallLight" => new Color(0.66f, 0.43f, 0.23f),
                    "HallDark" => new Color(0.48f, 0.27f, 0.13f),
                    "Paper" => new Color(0.90f, 0.88f, 0.80f),
                    "Cabinet" => new Color(0.43f, 0.28f, 0.16f),
                    "Ceiling" => new Color(0.73f, 0.62f, 0.48f),
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
