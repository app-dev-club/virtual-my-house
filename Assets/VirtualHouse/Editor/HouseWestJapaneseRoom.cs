using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        /// <summary>
        /// Reconstructs the western first-floor eight-tatami room from the 2026-09-30 photo set.
        /// The continuous southern engawa and curtains are created by the eastern-room component.
        /// </summary>
        private static void CreateWestJapaneseRoomInterior(Transform firstFloor)
        {
            Transform room = NewGroup("和室_西_写真ベース", firstFloor);
            Transform tatami = NewGroup("畳と畳縁", room);
            Transform walls = NewGroup("砂壁と柱梁", room);
            Transform fittings = NewGroup("障子・襖・欄間", room);
            Transform northStorage = NewGroup("北側_押入・床の間・地袋", room);
            Transform ceiling = NewGroup("天井", room);

            Vector3 southWest = PlanPoint(310f, 367f, 0f);
            Vector3 northEast = PlanPoint(462f, 216f, 0f);
            float west = southWest.x;
            float east = northEast.x;
            float south = southWest.z;
            float north = northEast.z;
            float width = east - west;
            float depth = north - south;
            float centerX = (west + east) * 0.5f;
            float centerZ = (south + north) * 0.5f;

            CreateEightTatamiLayout(tatami, west, south, width, depth);
            CreateWestRoomWalls(walls, west, east, south, north);
            CreateWestRoomSouthShoji(fittings, west, east, south);
            CreateSharedRanma(fittings, east, south, north);
            CreateWestRoomNorthFeatures(northStorage, west, east, north);
            CreateNandoShelf(firstFloor);
            CreateWestRoomCeiling(ceiling, centerX, centerZ, width, depth);
        }

        private static void CreateWestRoomWalls(Transform parent, float west, float east, float south, float north)
        {
            // Only the southern half is a wall; the northern half opens into the nando.
            float wallNorth = PlanPoint(310f, 291f, 0f).z;
            float centerZ = (south + wallNorth) * 0.5f;
            CreateDecorationBox("西砂壁", new Vector3(west + 0.075f, 1.28f, centerZ),
                new Vector3(0.035f, 2.40f, wallNorth - south - 0.12f), GetMaterial("SandWall"), parent);
            foreach (float z in new[] { south + 0.06f, wallNorth - 0.06f })
                CreateDecorationBox($"西柱_{z:0.00}", new Vector3(west + 0.105f, 1.31f, z),
                    new Vector3(0.105f, 2.62f, 0.105f), GetMaterial("LightWood"), parent);
            CreateDecorationBox("西長押", new Vector3(west + 0.11f, 1.93f, centerZ),
                new Vector3(0.11f, 0.13f, wallNorth - south), GetMaterial("LightWood"), parent);
            CreateDecorationBox("南鴨居", new Vector3((west + east) * 0.5f, 2.16f, south + 0.05f),
                new Vector3(east - west, 0.16f, 0.12f), GetMaterial("LightWood"), parent);
            CreateDecorationBox("北鴨居", new Vector3((west + east) * 0.5f, 2.16f, north - 0.05f),
                new Vector3(east - west, 0.16f, 0.12f), GetMaterial("DarkWood"), parent);
        }

        private static void CreateWestRoomSouthShoji(Transform parent, float west, float east, float south)
        {
            Transform wall = NewGroup("南側障子_4枚", parent);
            float fullWidth = east - west;
            float panelWidth = fullWidth / 4f;
            for (int i = 0; i < 4; i++)
            {
                float x = west + panelWidth * (i + 0.5f);
                Transform panel = NewGroup($"障子_{i + 1:00}", wall);
                CreateDecorationBox("和紙", new Vector3(x, 1.38f, south + 0.026f),
                    new Vector3(panelWidth - 0.07f, 1.36f, 0.025f), GetMaterial("Paper"), panel);
                CreateDecorationBox("型ガラス", new Vector3(x, 0.53f, south + 0.024f),
                    new Vector3(panelWidth - 0.07f, 0.40f, 0.028f), GetMaterial("Glass"), panel);
                CreateDecorationBox("腰板", new Vector3(x, 0.18f, south + 0.025f),
                    new Vector3(panelWidth - 0.07f, 0.28f, 0.032f), GetMaterial("LightWood"), panel);
                foreach (float y in new[] { 0.04f, 0.34f, 0.74f, 1.18f, 1.62f, 2.06f })
                    CreateDecorationBox($"横框_{y:0.00}", new Vector3(x, y, south - 0.008f),
                        new Vector3(panelWidth, 0.052f, 0.055f), GetMaterial("DarkWood"), panel);
                for (int bar = 0; bar <= 4; bar++)
                {
                    float barX = x - panelWidth * 0.5f + panelWidth * bar / 4f;
                    CreateDecorationBox($"縦框_{bar:00}", new Vector3(barX, 1.05f, south - 0.009f),
                        new Vector3(0.042f, 2.05f, 0.055f), GetMaterial("DarkWood"), panel);
                }
            }

            // Four shallow transom lights continue the same grid above the sliding panels.
            for (int i = 0; i < 4; i++)
            {
                float x = west + panelWidth * (i + 0.5f);
                CreateDecorationBox($"南欄間和紙_{i + 1:00}", new Vector3(x, 2.37f, south + 0.024f),
                    new Vector3(panelWidth - 0.07f, 0.34f, 0.025f), GetMaterial("Paper"), wall);
                for (int bar = 0; bar <= 4; bar++)
                {
                    float barX = x - panelWidth * 0.5f + panelWidth * bar / 4f;
                    CreateDecorationBox($"南欄間縦框_{i:00}_{bar:00}", new Vector3(barX, 2.37f, south - 0.009f),
                        new Vector3(0.035f, 0.40f, 0.05f), GetMaterial("DarkWood"), wall);
                }
                CreateDecorationBox($"南欄間横框_{i + 1:00}", new Vector3(x, 2.37f, south - 0.01f),
                    new Vector3(panelWidth, 0.04f, 0.05f), GetMaterial("DarkWood"), wall);
            }
        }

        private static void CreateSharedRanma(Transform parent, float east, float south, float north)
        {
            Transform ranma = NewGroup("東側透かし欄間_山水", parent);
            float openingSouth = PlanPoint(462f, 326f, 0f).z;
            float openingNorth = PlanPoint(462f, 250f, 0f).z;
            float length = openingNorth - openingSouth;
            float centerZ = (openingSouth + openingNorth) * 0.5f;
            CreateDecorationBox("欄間背板", new Vector3(east - 0.025f, 2.36f, centerZ),
                new Vector3(0.022f, 0.39f, length - 0.06f), GetMaterial("Paper"), ranma);
            for (int i = 0; i <= 28; i++)
            {
                float z = openingSouth + length * i / 28f;
                CreateDecorationBox($"縦格子_{i:00}", new Vector3(east - 0.05f, 2.36f, z),
                    new Vector3(0.04f, 0.39f, 0.022f), GetMaterial("DarkWood"), ranma);
            }
            foreach (float y in new[] { 2.17f, 2.36f, 2.55f })
                CreateDecorationBox($"横框_{y:0.00}", new Vector3(east - 0.052f, y, centerZ),
                    new Vector3(0.045f, 0.04f, length), GetMaterial("DarkWood"), ranma);

            // Layered sloping strips suggest the mountain-and-cloud carving visible in the photographs.
            CreateRanmaStroke(ranma, east - 0.078f, 2.31f, centerZ - 0.50f, 0.58f, 21f);
            CreateRanmaStroke(ranma, east - 0.079f, 2.40f, centerZ - 0.03f, 0.55f, -15f);
            CreateRanmaStroke(ranma, east - 0.080f, 2.30f, centerZ + 0.47f, 0.52f, 18f);
        }

        private static void CreateRanmaStroke(Transform parent, float x, float y, float z, float length, float angle)
        {
            GameObject stroke = CreateDecorationBox("山並み", new Vector3(x, y, z),
                new Vector3(0.035f, 0.035f, length), GetMaterial("DarkWood"), parent);
            stroke.transform.rotation = Quaternion.Euler(angle, 0f, 0f);
        }

        private static void CreateWestRoomNorthFeatures(Transform parent, float west, float east, float north)
        {
            // Keep simple open recesses inside the traced northern storage footprint.
            // No decorative doors or false back walls project into the tatami room.
            float divider = PlanPoint(386f, 216f, 0f).x;
            float back = PlanPoint(310f, 179f, 0f).z;
            float storageDepth = back - north - 0.16f;
            float centerZ = (north + back) * 0.5f;
            CreateBox("床の間_床板", new Vector3((divider + east) * 0.5f, 0.04f, centerZ),
                new Vector3(east - divider - 0.18f, 0.08f, storageDepth),
                GetMaterial("DarkWood"), parent);
        }

        private static void CreateNandoShelf(Transform firstFloor)
        {
            Transform storage = NewGroup("納戸_棚", firstFloor);
            Vector3 backSouth = PlanPoint(235f, 291f, 0f);
            Vector3 frontNorth = PlanPoint(310f, 216f, 0f);
            // Shelf against the western back wall, clear of the eastern entrance.
            const float shelfDepth = 0.56f;
            CreateBox("納戸_中棚",
                new Vector3(backSouth.x + 0.08f + shelfDepth * 0.5f, 1.0f,
                    (backSouth.z + frontNorth.z) * 0.5f),
                new Vector3(shelfDepth, 0.05f, frontNorth.z - backSouth.z - 0.18f),
                GetMaterial("LightWood"), storage);
        }

        private static void ValidateNandoOpening()
        {
            Vector3 a = PlanPoint(310f, 216f, 0f);
            Vector3 b = PlanPoint(310f, 291f, 0f);
            Bounds opening = new Bounds((a + b) * 0.5f + Vector3.up,
                new Vector3(0.38f, 1.80f, a.z - b.z - 0.24f));
            // Decorative meshes can hide an opening even without a collider.
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (renderer.enabled && renderer.bounds.Intersects(opening))
                    throw new System.InvalidOperationException($"Nando opening obscured by {renderer.name}");
            foreach (Collider collider in Physics.OverlapBox(opening.center, opening.extents))
                throw new System.InvalidOperationException($"Nando opening blocked by {collider.name}");
            Debug.Log("Nando opening validation passed: visible opening and collider clearance.");
        }

        private static void CreateWestRoomCeiling(Transform parent, float x, float z, float width, float depth)
        {
            CreateDecorationBox("棹縁天井", new Vector3(x, 2.57f, z),
                new Vector3(width - 0.12f, 0.055f, depth - 0.12f), GetMaterial("Ceiling"), parent);
            for (int i = 0; i <= 5; i++)
            {
                float beamZ = z - depth * 0.5f + 0.12f + (depth - 0.24f) * i / 5f;
                CreateDecorationBox($"天井竿縁_{i:00}", new Vector3(x, 2.535f, beamZ),
                    new Vector3(width - 0.13f, 0.045f, 0.045f), GetMaterial("DarkWood"), parent);
            }
        }

        [MenuItem("Virtual House/Render West Japanese Room Preview")]
        public static void RenderWestJapaneseRoomPreview()
        {
            EditorSceneManager.OpenScene(OutputScene, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
                throw new System.InvalidOperationException("HouseBlockout scene has no Main Camera.");

            Vector3 southWest = PlanPoint(310f, 367f, 0f);
            Vector3 northEast = PlanPoint(462f, 216f, 0f);
            float centerX = (southWest.x + northEast.x) * 0.5f;
            float centerZ = (southWest.z + northEast.z) * 0.5f;
            camera.transform.position = new Vector3(southWest.x + 0.52f, 1.48f, southWest.z + 0.55f);
            camera.transform.LookAt(new Vector3(centerX + 0.12f, 1.10f, centerZ + 0.72f));
            camera.fieldOfView = 68f;
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

            string output = Path.Combine(Application.dataPath, "VirtualHouse", "west-japanese-room-preview.png");
            File.WriteAllBytes(output, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log($"West Japanese room preview rendered: {output}");
        }

        public static void GenerateAndRenderWestJapaneseRoom()
        {
            Generate();
            ValidateNandoOpening();
            RenderWestJapaneseRoomPreview();
        }
    }
}
