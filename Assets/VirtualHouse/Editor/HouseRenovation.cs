using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        private const string RenovatedScene = "Assets/Scenes/HouseRenovated.unity";

        [MenuItem("Virtual House/Create Renovated Scene")]
        public static void CreateRenovatedScene()
        {
            // Work on a separate loaded copy; preserve the saved original and open scenes.
            Materials.Clear();
            Scene previous = SceneManager.GetActiveScene();
            Scene source = SceneManager.GetSceneByPath(OutputScene);
            bool sourceAlreadyLoaded = source.IsValid() && source.isLoaded;
            if (!sourceAlreadyLoaded) source = EditorSceneManager.OpenScene(OutputScene,
                Application.isBatchMode ? OpenSceneMode.Single : OpenSceneMode.Additive);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            foreach (GameObject root in source.GetRootGameObjects())
            {
                GameObject copy = Object.Instantiate(root);
                copy.name = root.name;
                SceneManager.MoveGameObjectToScene(copy, scene);
            }
            if (!sourceAlreadyLoaded) EditorSceneManager.CloseScene(source, true);
            SceneManager.SetActiveScene(scene);
            Transform house = scene.GetRootGameObjects().First(g => g.name == "住宅概形_図面ベース").transform;
            Transform first = house.Find("1階");
            Transform walls = first.Find("壁"), floors = first.Find("床_部屋別");
            foreach (string name in new[] { "壁_651_141_651_179", "壁_688_254_726_254", "壁_726_179_726_254", "壁_802_179_802_254", "壁_764_254_802_254" })
                RemoveRequired(walls, name);
            foreach (string name in new[] { "WC西", "WC東", "水回り物入", "脱衣", "浴室", "給湯" })
            {
                RemoveRequired(floors, name);
                foreach (TextMesh label in floors.GetComponentsInChildren<TextMesh>())
                    if (label.text == name) Object.DestroyImmediate(label.gameObject);
            }
            RemoveRequired(first, "浴室と脱衣所_写真ベース");
            // Trace the new plan: bathroom west edge x=746; post at (726,254).
            PlanFloor(floors, "WC_統合", 613,141,688,179,"Wet");
            PlanFloor(floors, "脱衣所_拡張北",688,216,746,254,"FloorWood");
            PlanFloor(floors, "脱衣所_拡張南",688,254,764,291,"FloorWood");
            PlanFloor(floors, "浴室_2.5畳",746,179,840,254,"Bath");
            PlanFloor(floors, "洗面横通路",726,179,746,216,"Wet");
            PlanWall(walls,726,179,726,216);
            PlanWall(walls,746,179,746,216);
            PlanWall(walls,746,254,840,254); // Closed on kitchen side.
            PlanWall(walls,840,216,840,254); // Closed on outdoor side.
            PlanWall(walls,651,179,688,179); // WC entry is only the western dotted half.
            PlanWall(walls,688,216,688,254); // Dressing entry is only the southern dotted half.
            CreateBox("脱衣所_残す柱",PlanPoint(726,254,WallHeight/2),new Vector3(.16f,WallHeight,.16f),GetMaterial("DarkWood"),walls);
            Transform fittings = NewGroup("リフォーム後_設備",first);
            Material white = GetMaterial("Wall"), steel = GetMaterial("KitchenSteel");
            // Schematic fixtures: final product dimensions and finishes are not specified.
            Vector3 tub = PlanPoint(816,213,.12f);
            CreateBox("浴槽底",tub,new Vector3(.83f,.16f,1.55f),white,fittings);
            foreach (int sign in new[] {-1,1})
            {
                CreateBox("浴槽側面",tub+new Vector3(sign*.405f,.27f,0),new Vector3(.09f,.54f,1.64f),white,fittings);
                CreateBox("浴槽端面",tub+new Vector3(0,.27f,sign*.775f),new Vector3(.90f,.54f,.09f),white,fittings);
            }
            KitchenRod(fittings,"シャワー縦管",PlanPoint(782,183,.85f),PlanPoint(782,183,1.9f),.035f,steel);
            CreateBox("シャワーヘッド",PlanPoint(782,187,1.9f),new Vector3(.16f,.05f,.20f),steel,fittings);
            CreateBox("洗濯機",PlanPoint(746,274,.46f),new Vector3(.62f,.90f,.62f),white,fittings);
            CreateDecorationBox("洗濯機上蓋",PlanPoint(746,274,.92f),new Vector3(.49f,.025f,.46f),GetMaterial("KitchenSteel"),fittings);
            CreatePhotoWashbasin(fittings,PlanPoint(714,197,.40f));
            fittings.Find("洗面台").rotation=Quaternion.Euler(0,90,0); // Face the western hall opening.
            CreateBox("便器",PlanPoint(666,155,.24f),new Vector3(.38f,.46f,.56f),white,fittings);
            CreateBox("トイレタンク",PlanPoint(666,147,.64f),new Vector3(.43f,.70f,.20f),white,fittings);
            CreateWarmLight(fittings,"浴室照明",PlanPoint(779,216,2.4f),3f,.7f);
            CreateWarmLight(fittings,"脱衣所照明",PlanPoint(710,246,2.4f),2f,.6f);
            CreateRenovatedKitchen(first);
            EditorSceneManager.SaveScene(scene,RenovatedScene);
            var builds = EditorBuildSettings.scenes.Where(s => s.path != OutputScene && s.path != RenovatedScene).ToList();
            builds.Insert(0,new EditorBuildSettingsScene(RenovatedScene,true));
            builds.Insert(0,new EditorBuildSettingsScene(OutputScene,true));
            EditorBuildSettings.scenes = builds.ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            Debug.Log("Renovated scene saved; original scene preserved.");
        }

        // Photo reference: ivory recessed basin, ochre double doors and two separate taps.
        // Keep the existing 640 x 450mm footprint; product dimensions are inferred.
        // Local -Z is the user side. Root stays at y=.40 to support Plan B relocation.
        private static void CreatePhotoWashbasin(Transform fittings,Vector3 position)
        {
            Transform vanity=NewGroup("洗面台",fittings);
            Material ceramic=WesternMaterial("WashbasinIvory",new Color(.88f,.87f,.78f));
            Material ochre=WesternMaterial("WashbasinOchre",new Color(.61f,.40f,.13f));
            Material chrome=GetMaterial("KitchenSteel"), dark=GetMaterial("KitchenBasin");
            void Part(string name,Vector3 center,Vector3 size,Material material,bool collides=false)
            {
                GameObject part=CreateBox(name,center-Vector3.up*.40f,size,material,vanity);
                if(!collides) Object.DestroyImmediate(part.GetComponent<Collider>());
            }
            Part("収納本体",new Vector3(0,.35f,0),new Vector3(.63f,.62f,.43f),ceramic,true);
            Part("台輪",new Vector3(0,.045f,.015f),new Vector3(.60f,.09f,.39f),ochre);
            foreach(int side in new[]{-1,1})
            {
                Part("黄土色の開き戸",new Vector3(side*.157f,.355f,-.216f),new Vector3(.309f,.58f,.018f),ochre);
                Part("横長の金属取手",new Vector3(side*.068f,.605f,-.233f),new Vector3(.102f,.014f,.018f),chrome);
            }
            Part("陶器の前縁",new Vector3(0,.742f,-.21f),new Vector3(.64f,.116f,.03f),ceramic);
            Part("立上り",new Vector3(0,.82f,.213f),new Vector3(.64f,.09f,.024f),ceramic);
            // Concentric rings form an actual recessed bowl, leaving the opening unobstructed.
            const int count=64;
            var vertices=new System.Collections.Generic.List<Vector3>();
            var triangles=new System.Collections.Generic.List<int>();
            for(int ring=0;ring<4;ring++)
                for(int k=0;k<count;k++)
                {
                    float angle=k*Mathf.PI*2/count,c=Mathf.Cos(angle),z=Mathf.Sin(angle);
                    float x,depth,y;
                    if(ring==0) {float r=Mathf.Min(.32f/Mathf.Max(Mathf.Abs(c),.0001f),.225f/Mathf.Max(Mathf.Abs(z),.0001f));x=c*r;depth=z*r;y=.80f;}
                    else {float scale=ring==1?1f:ring==2?.76f:0f;x=c*.235f*scale;depth=z*.155f*scale-.025f;y=ring==1?.797f:.675f;}
                    vertices.Add(new Vector3(x,y-.40f,depth));
                }
            for(int ring=0;ring<3;ring++)
                for(int k=0;k<count;k++)
                {
                    int a=ring*count+k,b=ring*count+(k+1)%count,c=a+count,d=b+count;
                    triangles.AddRange(new[]{a,c,b,b,c,d});
                }
            const string meshPath="Assets/VirtualHouse/GeneratedMeshes/PhotoWashbasin.asset";
            Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}else mesh.Clear();
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            GameObject bowl=new GameObject("くぼみのある陶器ボウル");bowl.transform.SetParent(vanity,false);
            bowl.AddComponent<MeshFilter>().sharedMesh=mesh;bowl.AddComponent<MeshRenderer>().sharedMaterial=ceramic;
            Part("排水口",new Vector3(0,.678f,-.025f),new Vector3(.040f,.005f,.040f),chrome);
            Part("排水口内側",new Vector3(0,.681f,-.025f),new Vector3(.025f,.002f,.025f),dark);
            Part("オーバーフロー",new Vector3(0,.766f,.116f),new Vector3(.045f,.013f,.004f),dark);
            foreach(float x in new[]{-.09f,.09f})
            {
                Vector3 P(float y,float z)=>new Vector3(x,y-.40f,z);
                KitchenRod(vanity,"独立蛇口_立上り",P(.80f,.167f),P(.89f,.167f),.025f,chrome);
                KitchenRod(vanity,"独立蛇口_吐水管",P(.855f,.167f),P(.855f,.105f),.021f,chrome);
                KitchenRod(vanity,"独立蛇口_吐水口",P(.855f,.105f),P(.835f,.105f),.023f,chrome);
                Part("十字ハンドル横",new Vector3(x,.897f,.167f),new Vector3(.060f,.012f,.015f),chrome);
                Part("十字ハンドル縦",new Vector3(x,.897f,.167f),new Vector3(.015f,.012f,.050f),chrome);
            }
            vanity.position=position;
        }

        private static void RemoveRequired(Transform parent,string name)
        {
            Transform item = parent.Find(name);
            if(item == null) throw new InvalidOperationException("Expected original object missing: " + name);
            Object.DestroyImmediate(item.gameObject);
        }

        public static void BuildAndValidateRenovation()
        {
            CreateRenovatedScene();
            EditorSceneManager.OpenScene(RenovatedScene);
            Physics.SyncTransforms();
            ValidateTracedOpenings(false,new float[,] { {688,254,688,291},{746,216,746,254},{688,254,726,254},{613,179,651,179} });
            foreach (Vector3 point in new[] {PlanPoint(820,254,1),PlanPoint(840,235,1),PlanPoint(726,254,1),PlanPoint(669,179,1),PlanPoint(688,235,1)})
                if (!Physics.OverlapSphere(point,.025f).Any(c=>c.name.StartsWith("壁_") || c.name=="脱衣所_残す柱"))
                    throw new InvalidOperationException("Required closed wall or pillar missing: " + point);
            ValidateRenovationWalk();
            ValidateAlternativeRoute(new[] {PlanPoint(670,197,0),PlanPoint(693,197,0)});
            ValidateRenovatedKitchen();
            Camera camera=Camera.main;
            GameObject.Find("外観_写真ベース").SetActive(false);
            GameObject.Find("2階").SetActive(false);
            camera.transform.position=PlanPoint(760,230,15);
            camera.transform.LookAt(PlanPoint(760,230,0),Vector3.forward);
            camera.orthographic=true; camera.orthographicSize=3.3f;
            RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/renovation-preview.png"));
            RenderRenovatedKitchenPreviews();
            RenderPhotoWashbasinPreview();
            // Preview and walking checks must not change the saved scene or its spawn.
            EditorSceneManager.OpenScene(RenovatedScene);
            Debug.Log("RENOVATION VALIDATION PASSED: openings, closed walls, retained pillar, walking and preview.");
        }

        private static void RenderPhotoWashbasinPreview()
        {
            Transform vanity=GameObject.Find("洗面台").transform;
            Camera camera=Camera.main;
            foreach(Transform part in vanity.GetComponentsInChildren<Transform>()) part.gameObject.layer=31;
            camera.cullingMask=1<<31;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.24f,.26f,.28f);
            camera.orthographic=false;camera.fieldOfView=48;
            camera.transform.position=vanity.TransformPoint(new Vector3(0,.90f,-1.05f));
            camera.transform.LookAt(vanity.TransformPoint(new Vector3(0,.12f,0)),Vector3.up);
            RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/washbasin-photo-preview.png"));
        }

        private static void ValidateRenovationWalk()
        {
            CharacterController player=GameObject.Find("Player").GetComponent<CharacterController>();
            Vector3[] route={PlanPoint(632,159,0),PlanPoint(632,196,0),PlanPoint(670,196,0),PlanPoint(670,274,0),PlanPoint(706,274,0),PlanPoint(706,235,0),PlanPoint(764,235,0),PlanPoint(776,206,0)};
            foreach(bool reverse in new[]{false,true})
            {
                player.enabled=false;player.transform.position=route[reverse?route.Length-1:0]+Vector3.up*.04f;player.enabled=true;
                Physics.SyncTransforms();float vy=0;
                for(int leg=1;leg<route.Length;leg++)
                {
                    Vector3 target=route[reverse?route.Length-1-leg:leg];
                    for(int frame=0;frame<240;frame++)
                    {
                        if(player.isGrounded && vy<0)vy=-2;vy-=20f/60;
                        Vector3 delta=target-player.transform.position;delta.y=0;
                        player.Move(Vector3.ClampMagnitude(delta,2.6f/60)+Vector3.up*(vy/60));
                    }
                    if(Vector3.Distance(player.transform.position,target)>.18f)throw new InvalidOperationException("Renovation walk failed: " + player.transform.position);
                }
            }
        }
    }
}
