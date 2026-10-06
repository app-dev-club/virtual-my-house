using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        /// <summary>
        /// Reconstructs the eastern first-floor eight-tatami room from the 2026-09-30 photo set.
        /// Dimensions which cannot be read directly from the photographs are fitted to the traced plan.
        /// </summary>
        private static void CreateEastJapaneseRoomInterior(Transform firstFloor)
        {
            Transform room = NewGroup("和室_東_写真ベース", firstFloor);
            Transform tatami = NewGroup("畳と畳縁", room);
            Transform walls = NewGroup("砂壁と柱梁", room);
            Transform fittings = NewGroup("障子・襖・出窓収納", room);
            Transform furniture = NewGroup("家具", room);
            Transform lighting = NewGroup("天井と照明", room);
            Transform veranda = NewGroup("南側縁側_和室2室分", room);

            Vector3 southWest = PlanPoint(462f, 367f, 0f);
            Vector3 northEast = PlanPoint(613f, 216f, 0f);
            float west = southWest.x;
            float east = northEast.x;
            float south = southWest.z;
            float north = northEast.z;
            float width = east - west;
            float depth = north - south;
            float centerX = (west + east) * 0.5f;
            float centerZ = (south + north) * 0.5f;

            CreateEightTatamiLayout(tatami, west, south, width, depth);

            // The photographs show olive-gold sand walls framed by pale exposed timber.
            CreateDecorationBox("東砂壁", new Vector3(east - 0.075f, 1.28f, centerZ),
                new Vector3(0.035f, 2.40f, depth - 0.18f), GetMaterial("SandWall"), walls);
            foreach (float z in new[] { south + 0.06f, centerZ, north - 0.06f })
                CreateDecorationBox($"東柱_{z:0.00}", new Vector3(east - 0.105f, 1.31f, z),
                    new Vector3(0.105f, 2.62f, 0.105f), GetMaterial("LightWood"), walls);
            CreateDecorationBox("東長押", new Vector3(east - 0.11f, 1.93f, centerZ),
                new Vector3(0.11f, 0.13f, depth), GetMaterial("LightWood"), walls);
            CreateDecorationBox("南鴨居", new Vector3(centerX, 2.16f, south + 0.05f),
                new Vector3(width, 0.16f, 0.12f), GetMaterial("LightWood"), walls);
            CreateDecorationBox("北鴨居", new Vector3(centerX, 2.16f, north - 0.05f),
                new Vector3(width, 0.16f, 0.12f), GetMaterial("LightWood"), walls);

            CreateSouthShoji(fittings, west, east, south);
            CreateWestFusuma(fittings, west, south, north);
            CreateNorthBayStorage(fittings, west, east, north);
            CreateChabudai(furniture, centerX, centerZ);
            CreateJapaneseCeiling(lighting, centerX, centerZ, width, depth);
            // The photographed engawa and curtain rail continue across both adjacent eight-tatami rooms.
            float westRoomWest = PlanPoint(310f, 367f, 0f).x;
            CreateSouthVeranda(veranda, westRoomWest, east, south);
        }

        private static void CreateEightTatamiLayout(Transform parent, float west, float south, float width, float depth)
        {
            float margin = 0.055f;
            float unitX = (width - margin * 2f) / 4f;
            float unitZ = (depth - margin * 2f) / 4f;
            float x0 = west + margin;
            float z0 = south + margin;

            // A non-crossing eight-mat arrangement, matching the alternating direction visible in the photos.
            CreateTatami(parent, "畳_01_南西", x0, z0, unitX * 2f, unitZ);
            CreateTatami(parent, "畳_02_南東", x0 + unitX * 2f, z0, unitX * 2f, unitZ);
            CreateTatami(parent, "畳_03_中西", x0, z0 + unitZ, unitX, unitZ * 2f);
            CreateTatami(parent, "畳_04_中南", x0 + unitX, z0 + unitZ, unitX * 2f, unitZ);
            CreateTatami(parent, "畳_05_中北", x0 + unitX, z0 + unitZ * 2f, unitX * 2f, unitZ);
            CreateTatami(parent, "畳_06_中東", x0 + unitX * 3f, z0 + unitZ, unitX, unitZ * 2f);
            CreateTatami(parent, "畳_07_北西", x0, z0 + unitZ * 3f, unitX * 2f, unitZ);
            CreateTatami(parent, "畳_08_北東", x0 + unitX * 2f, z0 + unitZ * 3f, unitX * 2f, unitZ);
        }

        private static void CreateTatami(Transform parent, string name, float x, float z, float width, float depth)
        {
            Transform mat = NewGroup(name, parent);
            const float top = 0.018f;
            CreateDecorationBox("畳表", new Vector3(x + width * 0.5f, top, z + depth * 0.5f),
                new Vector3(width - 0.022f, 0.022f, depth - 0.022f), GetMaterial("TatamiPhoto"), mat);
            const float edge = 0.028f;
            CreateDecorationBox("畳縁_南", new Vector3(x + width * 0.5f, top + 0.012f, z + edge * 0.5f),
                new Vector3(width, 0.012f, edge), GetMaterial("TatamiEdge"), mat);
            CreateDecorationBox("畳縁_北", new Vector3(x + width * 0.5f, top + 0.012f, z + depth - edge * 0.5f),
                new Vector3(width, 0.012f, edge), GetMaterial("TatamiEdge"), mat);
        }

        private static void CreateSouthShoji(Transform parent, float west, float east, float south)
        {
            float panelWidth = (east - west) * 0.245f;
            CreateShojiPanelX(parent, "南障子_西", west + panelWidth * 0.5f, south + 0.035f, panelWidth);
            CreateShojiPanelX(parent, "南障子_東", east - panelWidth * 0.5f, south + 0.028f, panelWidth);
            CreateDecorationBox("南無双窓下板", new Vector3(west + panelWidth * 0.5f, 0.22f, south + 0.012f),
                new Vector3(panelWidth - 0.06f, 0.34f, 0.045f), GetMaterial("LightWood"), parent);
        }

        private static void CreateShojiPanelX(Transform parent, string name, float x, float z, float width)
        {
            Transform panel = NewGroup(name, parent);
            const float height = 2.05f;
            CreateDecorationBox("和紙", new Vector3(x, height * 0.5f + 0.05f, z),
                new Vector3(width - 0.08f, height - 0.10f, 0.025f), GetMaterial("Paper"), panel);
            foreach (float y in new[] { 0.05f, 0.75f, 1.40f, 2.10f })
                CreateDecorationBox($"横框_{y:0.00}", new Vector3(x, y, z - 0.02f),
                    new Vector3(width, 0.055f, 0.055f), GetMaterial("DarkWood"), panel);
            for (int i = 0; i <= 4; i++)
            {
                float barX = x - width * 0.5f + width * i / 4f;
                CreateDecorationBox($"縦框_{i:00}", new Vector3(barX, 1.075f, z - 0.021f),
                    new Vector3(0.045f, height + 0.05f, 0.055f), GetMaterial("DarkWood"), panel);
            }
        }

        private static void CreateWestFusuma(Transform parent, float west, float south, float north)
        {
            float leafDepth = (north - south) * 0.24f;
            foreach ((string name, float z) in new[]
                     { ("西襖_南", south + leafDepth * 0.5f), ("西襖_北", north - leafDepth * 0.5f) })
            {
                Transform leaf = NewGroup(name, parent);
                CreateDecorationBox("白地", new Vector3(west + 0.025f, 1.05f, z),
                    new Vector3(0.035f, 2.05f, leafDepth - 0.04f), GetMaterial("Paper"), leaf);
                CreateDecorationBox("黒縁_上", new Vector3(west + 0.006f, 2.08f, z),
                    new Vector3(0.055f, 0.055f, leafDepth), GetMaterial("DarkWood"), leaf);
                CreateDecorationBox("黒縁_下", new Vector3(west + 0.006f, 0.03f, z),
                    new Vector3(0.055f, 0.055f, leafDepth), GetMaterial("DarkWood"), leaf);
                CreateDecorationBox("黒縁_端", new Vector3(west + 0.006f, 1.05f, z),
                    new Vector3(0.055f, 2.10f, 0.055f), GetMaterial("DarkWood"), leaf);
                // The blue-grey landscape band is simplified as two horizontal painted strips.
                CreateDecorationBox("水墨画_水", new Vector3(west - 0.017f, 0.44f, z),
                    new Vector3(0.018f, 0.16f, leafDepth - 0.11f), GetMaterial("Glass"), leaf);
                CreateDecorationBox("水墨画_岩", new Vector3(west - 0.026f, 0.49f, z - leafDepth * 0.14f),
                    new Vector3(0.018f, 0.14f, leafDepth * 0.18f), GetMaterial("Cladding"), leaf);
            }
        }

        private static void CreateNorthBayStorage(Transform parent, float west, float east, float north)
        {
            float left = west + 0.35f;
            float right = east - 0.82f;
            float width = right - left;
            float center = (left + right) * 0.5f;
            Transform bay = NewGroup("北出窓収納", parent);
            CreateBox("収納天板", new Vector3(center, 0.82f, north - 0.25f),
                new Vector3(width, 0.075f, 0.52f), GetMaterial("LightWood"), bay);
            CreateBox("収納背板", new Vector3(center, 0.40f, north - 0.48f),
                new Vector3(width, 0.80f, 0.045f), GetMaterial("Cabinet"), bay);
            CreateDecorationBox("引戸_中央", new Vector3(center, 0.40f, north - 0.515f),
                new Vector3(width * 0.47f, 0.72f, 0.035f), GetMaterial("LightWood"), bay);
            CreateDecorationBox("窓ガラス", new Vector3(center, 1.45f, north - 0.49f),
                new Vector3(width - 0.12f, 1.03f, 0.035f), GetMaterial("Glass"), bay);
            foreach (float y in new[] { 0.91f, 1.45f, 1.99f })
                CreateDecorationBox($"窓横框_{y:0.00}", new Vector3(center, y, north - 0.52f),
                    new Vector3(width, 0.055f, 0.065f), GetMaterial("DarkWood"), bay);
            for (int i = 0; i <= 6; i++)
            {
                float x = left + width * i / 6f;
                CreateDecorationBox($"窓縦框_{i:00}", new Vector3(x, 1.45f, north - 0.52f),
                    new Vector3(0.045f, 1.10f, 0.065f), GetMaterial("DarkWood"), bay);
            }
            CreateShojiPanelX(parent, "北側引戸", east - 0.38f, north - 0.04f, 0.68f);
        }

        private static void CreateChabudai(Transform parent, float x, float z)
        {
            Transform table = NewGroup("黒漆塗りのちゃぶ台", parent);
            const float width = 1.48f;
            const float depth = 0.90f;
            const float topY = 0.33f;
            CreateBox("天板", new Vector3(x, topY, z), new Vector3(width, 0.075f, depth),
                GetMaterial("Chabudai"), table);
            const float inset = 0.075f;
            CreateDecorationBox("金蒴縁_南", new Vector3(x, topY + 0.041f, z - depth * 0.5f + inset),
                new Vector3(width - inset * 2f, 0.012f, 0.025f), GetMaterial("ChabudaiGold"), table);
            CreateDecorationBox("金蒴縁_北", new Vector3(x, topY + 0.041f, z + depth * 0.5f - inset),
                new Vector3(width - inset * 2f, 0.012f, 0.025f), GetMaterial("ChabudaiGold"), table);
            CreateDecorationBox("金蒴縁_西", new Vector3(x - width * 0.5f + inset, topY + 0.041f, z),
                new Vector3(0.025f, 0.012f, depth - inset * 2f), GetMaterial("ChabudaiGold"), table);
            CreateDecorationBox("金蒴縁_東", new Vector3(x + width * 0.5f - inset, topY + 0.041f, z),
                new Vector3(0.025f, 0.012f, depth - inset * 2f), GetMaterial("ChabudaiGold"), table);
            foreach (float dx in new[] { -0.58f, 0.58f })
                foreach (float dz in new[] { -0.29f, 0.29f })
                    CreateBox($"折脚_{dx:0.00}_{dz:0.00}", new Vector3(x + dx, 0.16f, z + dz),
                        new Vector3(0.075f, 0.29f, 0.075f), GetMaterial("Chabudai"), table);
        }

        private static void CreateJapaneseCeiling(Transform parent, float x, float z, float width, float depth)
        {
            CreateDecorationBox("棹縁天井", new Vector3(x, 2.57f, z),
                new Vector3(width - 0.12f, 0.055f, depth - 0.12f), GetMaterial("Ceiling"), parent);
            for (int i = 0; i <= 6; i++)
            {
                float beamX = x - width * 0.5f + 0.12f + (width - 0.24f) * i / 6f;
                CreateDecorationBox($"天井竿縁_{i:00}", new Vector3(beamX, 2.535f, z),
                    new Vector3(0.045f, 0.045f, depth - 0.13f), GetMaterial("DarkWood"), parent);
            }
            CreateWarmLight(parent, "和室ペンダント灯", new Vector3(x, 2.29f, z), 4.4f, 0.78f);
            CreateDecorationBox("照明木枠", new Vector3(x, 2.25f, z),
                new Vector3(0.48f, 0.08f, 0.48f), GetMaterial("DarkWood"), parent);
        }

        private static void CreateSouthVeranda(Transform parent, float west, float east, float south)
        {
            float center = (west + east) * 0.5f;
            float width = east - west;
            for (int i = 0; i < 12; i++)
            {
                float z = 0.08f + i * (south - 0.14f) / 12f;
                CreateDecorationBox($"縁側板_{i:00}", new Vector3(center, 0.014f, z),
                    new Vector3(width - 0.08f, 0.025f, 0.085f), GetMaterial(i % 2 == 0 ? "HallLight" : "HallDark"), parent);
            }
            CreateOpenCurtains(parent, new Vector3(center, 1.15f, 0.07f), width, 2.18f, false);
        }

        private static GameObject CreateDecorationBox(string name, Vector3 position, Vector3 size,
            Material material, Transform parent)
        {
            if (name.Contains("ガラス")) material = GetClearWindowMaterial();
            GameObject box = CreateBox(name, position, size, material, parent);
            if (name.Contains("ガラス"))
                box.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Collider collider = box.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
            return box;
        }

        [MenuItem("Virtual House/Render East Japanese Room Preview")]
        public static void RenderEastJapaneseRoomPreview()
        {
            EditorSceneManager.OpenScene(OutputScene, OpenSceneMode.Single);
            Camera camera = Camera.main;
            if (camera == null)
                throw new System.InvalidOperationException("HouseBlockout scene has no Main Camera.");

            Vector3 southWest = PlanPoint(462f, 367f, 0f);
            Vector3 northEast = PlanPoint(613f, 216f, 0f);
            float centerX = (southWest.x + northEast.x) * 0.5f;
            float centerZ = (southWest.z + northEast.z) * 0.5f;
            camera.transform.position = new Vector3(southWest.x + 0.48f, 1.52f, southWest.z + 0.48f);
            camera.transform.LookAt(new Vector3(centerX + 0.18f, 1.12f, centerZ + 0.55f));
            camera.fieldOfView = 67f;
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

            string output = Path.Combine(Application.dataPath, "VirtualHouse", "east-japanese-room-preview.png");
            File.WriteAllBytes(output, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log($"East Japanese room preview rendered: {output}");
        }

        public static void GenerateAndRenderEastJapaneseRoom()
        {
            Generate();
            RenderEastJapaneseRoomPreview();
        }
    }
}
