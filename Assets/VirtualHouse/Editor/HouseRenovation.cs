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
            CreateBox("洗面台",PlanPoint(707,194,.40f),new Vector3(.64f,.80f,.45f),white,fittings);
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
            ValidateRenovatedKitchen();
            Camera camera=Camera.main;
            GameObject.Find("外観_写真ベース").SetActive(false);
            GameObject.Find("2階").SetActive(false);
            camera.transform.position=PlanPoint(760,230,15);
            camera.transform.LookAt(PlanPoint(760,230,0),Vector3.forward);
            camera.orthographic=true; camera.orthographicSize=3.3f;
            RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/renovation-preview.png"));
            RenderRenovatedKitchenPreviews();
            // Preview and walking checks must not change the saved scene or its spawn.
            EditorSceneManager.OpenScene(RenovatedScene);
            Debug.Log("RENOVATION VALIDATION PASSED: openings, closed walls, retained pillar, walking and preview.");
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
