using System;
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
        // Photo estimate. Keep the established interior datum at zero; lower the site and doma.
        private const float EntryRise = .24f;

        private static void ApplyRaisedEntry(Transform house)
        {
            Transform floors=house.Find("1階/床_部屋別");
            Transform entry=floors.Find("玄関");
            Vector3 p=entry.position;p.y=-EntryRise-FloorThickness/2;entry.position=p;
            foreach(TextMesh label in floors.GetComponentsInChildren<TextMesh>())
                if(label.text=="玄関") { p=label.transform.position;p.y=-EntryRise+.08f;label.transform.position=p; }
            Transform old=house.Find("床下基礎と上がり框");
            if(old!=null) Object.DestroyImmediate(old.gameObject);
            Transform foundation=NewGroup("床下基礎と上がり框",house);
            foreach(Transform floor in floors)
            {
                var collider=floor.GetComponent<BoxCollider>();
                if(collider==null || floor==entry) continue;
                Bounds b=collider.bounds;
                float bottom=-EntryRise-.09f,top=b.min.y;
                if(top<=bottom) continue;
                CreateBox("基礎_"+floor.name,new Vector3(b.center.x,(bottom+top)/2,b.center.z),
                    new Vector3(b.size.x,top-bottom,b.size.z),GetMaterial("Concrete"),foundation);
            }
            CreateBox("玄関_木の上がり框",PlanPoint(650.5f,429,-EntryRise/2),
                new Vector3(75*PlanScale-.12f,EntryRise,12*PlanScale),GetMaterial("FloorWood"),foundation);
            foreach(float x in new[]{613f,688f})
                CreateBox("玄関_基礎側面",PlanPoint(x,451.5f,-EntryRise/2),
                    new Vector3(WallThickness,EntryRise,57*PlanScale),GetMaterial("Concrete"),foundation);
            Transform porch=house.Find("外観_写真ベース/玄関ポーチ");
            if(porch!=null)
            {
                Transform slab=porch.Find("土間");p=slab.position;p.y=-EntryRise-.08f;slab.position=p;
                foreach(string name in new[]{"左柱","右柱"})
                {
                    Transform post=porch.Find(name);p=post.position;p.y=(2.5f-EntryRise)/2;post.position=p;
                    Vector3 scale=post.localScale;scale.y=2.5f+EntryRise;post.localScale=scale;
                }
            }
            // One low tread at the elevated outdoor landings keeps both exits walkable.
            CreateBox("北出口_下段",PlanPoint(575,144,-.22f),new Vector3(76*PlanScale,.16f,10*PlanScale),GetMaterial("Concrete"),foundation);
            if(house.Find("外観_写真ベース/屋外サービス踏み場")!=null)
                CreateBox("東出口_下段",PlanPoint(897,235,-.22f),new Vector3(10*PlanScale,.16f,38*PlanScale),GetMaterial("Concrete"),foundation);
        }

        [MenuItem("Virtual House/Update And Validate Entry Level All Plans")]
        public static void UpdateAndValidateEntryLevel()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in new[]{OutputScene,RenovatedScene,AlternativeScene,PlanCScene})
                {
                    Materials.Clear();var scene=EditorSceneManager.OpenScene(path);
                    Transform house=scene.GetRootGameObjects().First(g=>g.name=="住宅概形_図面ベース").transform;
                    CreateTracedSite(house);ApplyRaisedEntry(house);Physics.SyncTransforms();
                    var player=GameObject.Find("Player").GetComponent<CharacterController>();
                    Vector3 saved=player.transform.position;
                    ValidateAlternativeRoute(new[]{PlanPoint(650,510,-EntryRise-.038f),PlanPoint(650,465,-EntryRise),PlanPoint(650,452,-EntryRise),PlanPoint(650,420,0),PlanPoint(635,405,0)});
                    ValidateAlternativeRoute(new[]{PlanPoint(575,197,0),PlanPoint(575,160,0),PlanPoint(575,115,-EntryRise-.09f)});
                    float entryTop=house.Find("1階/床_部屋別/玄関").GetComponent<Collider>().bounds.max.y;
                    if(Mathf.Abs(entryTop+EntryRise)>.001f) throw new InvalidOperationException("Entry rise differs from 24cm.");
                    player.enabled=false;player.transform.position=saved;player.enabled=true;
                    EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                    Debug.Log("ENTRY LEVEL VALIDATION PASSED: "+path+"; 24cm rise, bidirectional entry and north exit walks.");
                }
                Camera camera=Camera.main;camera.orthographic=false;camera.fieldOfView=70;
                camera.transform.position=PlanPoint(656,474,1.32f);camera.transform.LookAt(PlanPoint(630,413,.25f));
                RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/entry-step-preview.png"));
                camera.transform.position=PlanPoint(500,250,1.6f);camera.transform.LookAt(PlanPoint(499.5f,174,1.55f));
                RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/laundry-east-adjusted-preview.png"));
                AssetDatabase.Refresh();
            }
            finally
            {
                if(!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.OpenScene(PlanCScene);
            }
        }
    }
}
