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
        private const string AlternativeScene = "Assets/Scenes/HouseRenovatedAlternative.unity";

        [MenuItem("Virtual House/Create Renovation Plan B")]
        public static void CreateRenovationAlternative()
        {
            Materials.Clear();
            Scene previous = SceneManager.GetActiveScene();
            Scene source = SceneManager.GetSceneByPath(RenovatedScene);
            bool loaded = source.IsValid() && source.isLoaded;
            if (!loaded) source = EditorSceneManager.OpenScene(RenovatedScene,
                Application.isBatchMode ? OpenSceneMode.Single : OpenSceneMode.Additive);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            foreach (GameObject root in source.GetRootGameObjects())
            {
                GameObject copy = Object.Instantiate(root);
                copy.name = root.name;
                SceneManager.MoveGameObjectToScene(copy, scene);
            }
            if (!loaded) EditorSceneManager.CloseScene(source, true);
            Transform first = scene.GetRootGameObjects().First(g => g.name == "住宅概形_図面ベース").transform.Find("1階");
            Transform walls = first.Find("壁"), floors = first.Find("床_部屋別");
            foreach (string name in new[] { "壁_764_254_764_291", "壁_688_291_764_291", "壁_688_216_726_216", "壁_726_179_726_216", "壁_651_216_651_367", "壁_746_179_746_216" })
                RemoveRequired(walls, name);
            PlanWall(walls,651,255,651,367); // Washbasin entry faces the hall.
            PlanWall(walls,613,216,613,255);
            PlanWall(walls,746,216,746,254); // Bathroom entry moves north.
            PlanWall(walls,688,254,746,254);
            PlanWall(walls,688,254,688,291);
            // The previously retained post now falls within the new southern partition.
            foreach (string name in new[] { "洗面", "洗面横通路", "脱衣所_拡張北", "脱衣所_拡張南", "DK北接続", "階段北物入" })
            {
                RemoveRequired(floors,name);
                foreach (TextMesh label in floors.GetComponentsInChildren<TextMesh>())
                    if (label.text == name) Object.DestroyImmediate(label.gameObject);
            }
            PlanFloor(floors,"洗面_階段北",613,216,651,255,"Wet");
            PlanFloor(floors,"脱衣所_1.5畳",688,179,746,254,"FloorWood");
            PlanFloor(floors,"DK_張出し撤去部分",688,254,840,291,"FloorWood");
            Transform fittings = first.Find("リフォーム後_設備");
            fittings.Find("洗濯機").position = PlanPoint(705,236,.46f);
            fittings.Find("洗濯機上蓋").position = PlanPoint(705,236,.92f);
            fittings.Find("洗面台").position = PlanPoint(625,235,.40f);
            fittings.Find("洗面台").rotation = Quaternion.Euler(0,-90,0);
            fittings.Find("脱衣所照明").position = PlanPoint(712,216,2.4f);
            Physics.SyncTransforms();
            AddReusedPreparationCabinet(first);
            EditorSceneManager.SaveScene(scene,AlternativeScene);
            var builds = EditorBuildSettings.scenes.Where(s=>s.path != OutputScene && s.path != RenovatedScene && s.path != AlternativeScene).ToList();
            builds.InsertRange(0,new[] { new EditorBuildSettingsScene(OutputScene,true),new EditorBuildSettingsScene(RenovatedScene,true),new EditorBuildSettingsScene(AlternativeScene,true) });
            EditorBuildSettings.scenes = builds.ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.CloseScene(scene,true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }

        public static void BuildAndValidateBothRenovations()
        {
            BuildAndValidateRenovation();
            BuildAndValidateAlternative();
        }

        public static void BuildAndValidateAlternative()
        {
            CreateRenovationAlternative();
            EditorSceneManager.OpenScene(AlternativeScene);
            Physics.SyncTransforms();
            ValidateTracedOpenings(false,new float[,] { {688,179,688,216},{746,179,746,216},{651,216,651,255},{688,291,688,367},{613,179,651,179} });
            foreach (Vector3 point in new[] {PlanPoint(709,254,1),PlanPoint(688,274,1),PlanPoint(726,254,1),PlanPoint(613,235,1),PlanPoint(746,235,1)})
                if (!Physics.OverlapSphere(point,.025f).Any(c=>c.name.StartsWith("壁_") || c.name=="脱衣所_残す柱"))
                    throw new InvalidOperationException("Plan B partition or post missing: " + point);
            ValidateAlternativeRoute(new[] {PlanPoint(592,197,0),PlanPoint(670,197,0),PlanPoint(723,197,0),PlanPoint(765,197,0),PlanPoint(776,206,0)});
            ValidateAlternativeRoute(new[] {PlanPoint(670,328,0),PlanPoint(705,328,0),PlanPoint(710,298,0),PlanPoint(778,298,0)});
            ValidateAlternativeRoute(new[] {PlanPoint(670,274,0),PlanPoint(670,240,0),PlanPoint(646,235,0)});
            ValidateRenovatedKitchen();
            Camera camera = Camera.main;
            GameObject.Find("外観_写真ベース").SetActive(false);
            GameObject.Find("2階").SetActive(false);
            camera.transform.position=PlanPoint(706,284,15);
            camera.transform.LookAt(PlanPoint(706,284,0),Vector3.forward);
            camera.orthographic=true;
            camera.orthographicSize=4.3f;
            RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/renovation-plan-b-preview.png"));
            EditorSceneManager.OpenScene(AlternativeScene);
            Debug.Log("PLAN B VALIDATION PASSED: dressing/bath access, reclaimed DK walk, kitchen aisle, walls and retained post.");
        }

        private static void ValidateAlternativeRoute(Vector3[] route)
        {
            CharacterController player=GameObject.Find("Player").GetComponent<CharacterController>();
            foreach (bool reverse in new[] {false,true})
            {
                player.enabled=false;
                player.transform.position=route[reverse?route.Length-1:0]+Vector3.up*.04f;
                player.enabled=true;
                Physics.SyncTransforms();
                float vy=0;
                for(int leg=1;leg<route.Length;leg++)
                {
                    Vector3 target=route[reverse?route.Length-1-leg:leg];
                    for(int frame=0;frame<240;frame++)
                    {
                        if(player.isGrounded && vy<0)vy=-2;
                        vy-=20f/60;
                        Vector3 delta=target-player.transform.position;delta.y=0;
                        player.Move(Vector3.ClampMagnitude(delta,2.6f/60)+Vector3.up*(vy/60));
                    }
                    if(Vector3.Distance(player.transform.position,target)>.18f)
                        throw new InvalidOperationException("Plan B walk failed: " + player.transform.position + " target " + target);
                }
            }
        }
    }
}
