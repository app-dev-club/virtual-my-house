using System.IO;
using System.Linq;
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
            // Photo IMG_20260930_111559981: left alcove, right overhead doors and low cupboard.
            // Keep everything in the northern storage footprint, behind the tatami edge.
            float divider = PlanPoint(386f, 216f, 0f).x;
            float back = PlanPoint(310f, 179f, 0f).z;
            float storageDepth = back - north - 0.16f;
            float centerZ = (north + back) * 0.5f;
            Material wood=GetMaterial("LightWood"),dark=GetMaterial("DarkWood"),sand=GetMaterial("SandWall");
            Material paper=WesternMaterial("WestStorageOchre",new Color(.68f,.49f,.25f));
            float face=north+.02f;
            void Part(string name,float x,float y,float z,float w,float h,float d,Material mat)
                => CreateBox(name,new Vector3(x,y,z),new Vector3(w,h,d),mat,parent);
            foreach(var span in new[]{new Vector2(west,divider),new Vector2(divider,east)})
            {
                float cx=(span.x+span.y)/2,w=span.y-span.x-.14f;
                Part("床の間_床板",cx,.055f,centerZ,w,.11f,storageDepth,dark);
                Part("窓のない砂壁_奥壁",cx,1.35f,back-.30f,w,2.60f,.035f,sand);
                Part("上部砂壁",cx,2.39f,face,w,.48f,.055f,sand);
                Part("上部横木",cx,2.13f,face-.015f,w,.10f,.09f,wood);
            }
            foreach(float x in new[]{west+.08f,divider,east-.08f})
                Part("床柱",x,1.32f,face,.17f,2.64f,.25f,wood);
            Part("左床の間_奥の段",(west+divider)/2,.22f,back-.25f,divider-west-.18f,.08f,.32f,dark);
            float rightWidth=east-divider-.17f,rightX=(divider+east)/2;
            Part("天袋本体",rightX,1.91f,face+.26f,rightWidth,.45f,.52f,dark);
            for(int i=0;i<4;i++)
            {
                float x=divider+.085f+rightWidth*(i+.5f)/4;
                Part("天袋_引戸",x,1.91f,face-.018f,rightWidth/4-.032f,.36f,.025f,paper);
                Part("天袋_丸引手",x+rightWidth/4*.32f,1.91f,face-.035f,.027f,.035f,.012f,dark);
            }
            float cabinetWidth=rightWidth*.62f,cabinetX=east-.12f-cabinetWidth/2;
            Part("地袋本体",cabinetX,.40f,face+.29f,cabinetWidth,.58f,.52f,dark);
            Part("地袋天板",cabinetX,.705f,face+.29f,cabinetWidth+.04f,.04f,.55f,wood);
            for(int i=0;i<2;i++)
            {
                float x=cabinetX+(i==0?-.25f:.25f)*cabinetWidth;
                Part("地袋_引戸",x,.40f,face+.018f,cabinetWidth/2-.035f,.49f,.025f,paper);
                Part("地袋_丸引手",x+(i==0?-.15f:.15f),.39f,face,.028f,.035f,.015f,dark);
            }
        }

        [MenuItem("Virtual House/Update West Room North Storage All Plans")]
        public static void UpdateWestRoomNorthStorageAllPlans()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in new[]{OutputScene,RenovatedScene,AlternativeScene,PlanCScene})
                {
                    Materials.Clear();
                    var scene=EditorSceneManager.OpenScene(path);
                    Transform house=scene.GetRootGameObjects().First(g=>g.name=="住宅概形_図面ベース").transform;
                    Transform trim=house.Find("外観_写真ベース/腰壁と窓");
                    Transform window=trim.Find("北西腰窓");
                    if(window!=null)
                    {
                        Object.DestroyImmediate(window.gameObject);
                        foreach(Transform hood in trim.Cast<Transform>().ToArray())
                            if(hood.name=="窓庇" && Mathf.Abs(hood.position.x-PlanPoint(383,174,0).x)<.01f && Mathf.Abs(hood.position.y-2.14f)<.01f)
                                Object.DestroyImmediate(hood.gameObject);
                        CreateBox("北西旧窓_壁復旧",PlanPoint(383,179,1.45f),new Vector3(115*PlanScale,1.10f,WallThickness),GetMaterial("Wall"),house.Find("1階/壁"));
                        CreateBox("北西旧窓_腰壁復旧",PlanPoint(383,175,1.02f),new Vector3(115*PlanScale,.24f,.05f),GetMaterial("Cladding"),trim);
                    }
                    Transform room=house.Find("1階/和室_西_写真ベース");
                    Transform old=room.Find("北側_押入・床の間・地袋");
                    if(old!=null) Object.DestroyImmediate(old.gameObject);
                    Transform storage=NewGroup("北側_押入・床の間・地袋",room);
                    CreateWestRoomNorthFeatures(storage,PlanPoint(310,216,0).x,PlanPoint(462,216,0).x,PlanPoint(310,216,0).z);
                    Physics.SyncTransforms();ValidateNandoOpening();
                    if(trim.Find("北西腰窓")!=null) throw new System.InvalidOperationException("Unexpected north window");
                    foreach(float x in new[]{345f,415f})
                        if(!Physics.Raycast(PlanPoint(x,205,1.4f),Vector3.forward,out var hit,1f) || !hit.collider.name.Contains("奥壁"))
                            throw new System.InvalidOperationException("North alcove must have a solid back wall");
                    EditorSceneManager.SaveScene(scene);
                    foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) label.GetComponent<Renderer>().enabled=false;
                    Camera camera=Camera.main;camera.transform.position=PlanPoint(385,311,1.6f);
                    camera.transform.LookAt(PlanPoint(386,194,1.25f));camera.fieldOfView=70;
                    RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/west-north-storage-"+Path.GetFileNameWithoutExtension(path)+".png"));
                    Debug.Log("West north solid wall, storage and nando clearance passed: "+path);
                }
                AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            }
            finally { if(!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
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

        [MenuItem("Virtual House/Remove Shared Japanese Room Partition All Plans")]
        public static void RemoveSharedJapaneseRoomPartitionAllPlans()
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
                    Transform walls = house.Find("1階/壁");
                    foreach (string name in new[] { "壁_462_179_462_250", "壁_462_326_462_367" })
                    {
                        Transform wall = walls.Find(name);
                        if (wall != null) Object.DestroyImmediate(wall.gameObject);
                    }
                    if (walls.Find("壁_462_179_462_216") == null)
                        PlanWall(walls, 462, 179, 462, 216);
                    Transform fittings = house.Find("1階/和室_東_写真ベース/障子・襖・出窓収納");
                    foreach (string name in new[] { "西襖_南", "西襖_北" })
                    {
                        Transform leaf = fittings.Find(name);
                        if (leaf != null) Object.DestroyImmediate(leaf.gameObject);
                    }
                    Physics.SyncTransforms();
                    Vector3 a = PlanPoint(462, 224, .15f), b = PlanPoint(462, 359, 1.95f);
                    Bounds opening = new Bounds((a + b) / 2, new Vector3(.12f, b.y - a.y, a.z - b.z));
                    foreach (Collider hit in Physics.OverlapBox(opening.center, opening.extents))
                        throw new System.InvalidOperationException("Shared room opening blocked by " + hit.name);
                    foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                        if (renderer.enabled && renderer.GetComponent<TextMesh>() == null && renderer.bounds.Intersects(opening))
                            throw new System.InvalidOperationException("Shared room opening obscured by " + renderer.name);
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("Shared Japanese room opening visual and collider clearance passed: " + path);
                }
                AssetDatabase.SaveAssets();
            }
            finally { if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
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
