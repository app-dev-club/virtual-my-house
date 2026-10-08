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
        private const string PlanCScene = "Assets/Scenes/HouseRenovatedPlanC.unity";

        // The north hall window west of the WC is shown open for walk-through comparison.
        private static void CreateNorthHallOpening(Transform house)
        {
            if (house.Find("トイレ西隣_開放窓") != null) return;
            CutWindowOpening(house,PlanPoint(575,179,0),76*PlanScale,-.01f,2.21f,false);
            Transform opening=NewGroup("トイレ西隣_開放窓",house);
            foreach(float x in new[]{537f,613f})
                CreateBox("窓枠_側",PlanPoint(x,179,1.1f),new Vector3(.045f,2.2f,.12f),GetMaterial("WindowFrame"),opening);
            CreateBox("窓枠_上",PlanPoint(575,179,2.2f),new Vector3(76*PlanScale,.045f,.12f),GetMaterial("WindowFrame"),opening);
            PlanFloor(opening,"北側踏み場",537,149,613,179,"Concrete");
        }

        [MenuItem("Virtual House/Create Renovation Plan C")]
        public static void CreateRenovationPlanC()
        {
            Materials.Clear();
            Scene previous=SceneManager.GetActiveScene();
            Scene source=SceneManager.GetSceneByPath(RenovatedScene);
            bool loaded=source.IsValid() && source.isLoaded;
            if(!loaded) source=EditorSceneManager.OpenScene(RenovatedScene,Application.isBatchMode?OpenSceneMode.Single:OpenSceneMode.Additive);
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            foreach(GameObject root in source.GetRootGameObjects())
            {
                GameObject copy=Object.Instantiate(root);copy.name=root.name;
                SceneManager.MoveGameObjectToScene(copy,scene);
            }
            if(!loaded) EditorSceneManager.CloseScene(source,true);
            Transform house=scene.GetRootGameObjects().First(g=>g.name=="住宅概形_図面ベース").transform;
            Transform first=house.Find("1階"),walls=first.Find("壁"),floors=first.Find("床_部屋別");
            foreach(string name in new[]{"壁_688_216_726_216","壁_726_179_726_216","壁_651_216_651_367","壁_613_216_651_216","壁_746_179_746_216","壁_688_216_688_254"}) RemoveRequired(walls,name);
            PlanWall(walls,651,255,651,367);
            PlanWall(walls,613,216,613,255);
            PlanWall(walls,746,216,746,254);
            // Plan C (6): only the middle west section is open; retain the wall facing the washer.
            PlanWall(walls,688,179,688,216);
            PlanWall(walls,688,254,688,291);
            CreateBox("洗面_北東角柱",PlanPoint(651,216,WallHeight/2),new Vector3(.12f,WallHeight,.12f),GetMaterial("DarkWood"),walls);
            foreach(string name in new[]{"洗面","洗面横通路","階段北物入"})
            {
                RemoveRequired(floors,name);
                foreach(TextMesh label in floors.GetComponentsInChildren<TextMesh>()) if(label.text==name) Object.DestroyImmediate(label.gameObject);
            }
            PlanFloor(floors,"洗面_階段北",613,216,651,255,"Wet");
            PlanFloor(floors,"脱衣所_旧洗面部分",688,179,746,216,"FloorWood");
            Transform fittings=first.Find("リフォーム後_設備");
            fittings.Find("洗面台").position=PlanPoint(625,239,.40f);
            fittings.Find("洗面台").rotation=Quaternion.Euler(0,-90,0);
            CreateNorthHallOpening(house);
            EditorSceneManager.SaveScene(scene,PlanCScene);
            string[] paths={OutputScene,RenovatedScene,AlternativeScene,PlanCScene};
            EditorBuildSettings.scenes=paths.Select(p=>new EditorBuildSettingsScene(p,true)).Concat(EditorBuildSettings.scenes.Where(s=>!paths.Contains(s.path))).ToArray();
            AssetDatabase.SaveAssets();
            if(previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene,true);
            }
        }

        public static void BuildAndValidatePlanC()
        {
            // Patch saved scenes without regenerating unrelated interiors.
            foreach(string path in new[]{OutputScene,RenovatedScene,AlternativeScene})
            {
                Scene scene=EditorSceneManager.OpenScene(path);
                CreateNorthHallOpening(scene.GetRootGameObjects().First(g=>g.name=="住宅概形_図面ベース").transform);
                EditorSceneManager.SaveScene(scene);
                ValidateAlternativeRoute(new[]{PlanPoint(575,197,0),PlanPoint(575,158,0)});
            }
            CreateRenovationPlanC();
            EditorSceneManager.OpenScene(PlanCScene);
            Physics.SyncTransforms();
            ValidateTracedOpenings(false,new float[,]{{613,216,651,216},{651,216,651,255},{688,216,688,254},{746,179,746,216},{537,179,613,179}});
            foreach(Vector3 point in new[]{PlanPoint(688,197,1),PlanPoint(688,274,1),PlanPoint(651,216,1),PlanPoint(726,254,1),PlanPoint(726,291,1),PlanPoint(764,274,1)})
                if(!Physics.OverlapSphere(point,.025f).Any(c=>c.name.StartsWith("壁_") || c.name.Contains("柱"))) throw new InvalidOperationException("Plan C wall/post missing: "+point);
            // Approach each side separately: the compact vanity is not a through-corridor.
            ValidateAlternativeRoute(new[]{PlanPoint(632,197,0),PlanPoint(632,214,0)});
            ValidateAlternativeRoute(new[]{PlanPoint(670,274,0),PlanPoint(670,240,0),PlanPoint(646,239,0)});
            ValidateAlternativeRoute(new[]{PlanPoint(670,328,0),PlanPoint(670,235,0),PlanPoint(710,235,0),PlanPoint(714,197,0),PlanPoint(770,197,0)});
            ValidateAlternativeRoute(new[]{PlanPoint(670,274,0),PlanPoint(670,235,0),PlanPoint(710,235,0),PlanPoint(710,274,0)});
            ValidateAlternativeRoute(new[]{PlanPoint(714,197,0),PlanPoint(710,270,0)});
            ValidateAlternativeRoute(new[]{PlanPoint(575,197,0),PlanPoint(575,158,0)});
            ValidateRenovatedKitchen();
            GameObject.Find("外観_写真ベース").SetActive(false);
            GameObject.Find("2階").SetActive(false);
            Camera camera=Camera.main;
            camera.transform.position=PlanPoint(706,284,15);
            camera.transform.LookAt(PlanPoint(706,284,0),Vector3.forward);
            camera.orthographic=true;camera.orthographicSize=4.3f;
            RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/renovation-plan-c-preview.png"));
            EditorSceneManager.OpenScene(PlanCScene);
            Debug.Log("PLAN C VALIDATION PASSED: two washbasin entrances, retained projection/posts, dressing/bath, kitchen, all four north window walk-throughs.");
        }
    }
}
