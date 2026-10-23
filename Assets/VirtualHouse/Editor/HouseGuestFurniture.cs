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
        private const string GuestFurnitureName="1階_追加ソファと布団";

        private static void CreateGuestFurniture(Transform first)
        {
            Transform old=first.Find(GuestFurnitureName);
            if(old!=null)Object.DestroyImmediate(old.gameObject);
            Transform root=NewGroup(GuestFurnitureName,first);
            void PlacePrefab(string asset,string name,float x,float v,float yaw)
            {
                GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VirtualHouse/Prefabs/"+asset+".prefab");
                if(prefab==null)throw new InvalidOperationException("Missing existing furniture prefab: "+asset);
                Transform item=((GameObject)PrefabUtility.InstantiatePrefab(prefab,root)).transform;
                item.name=name;item.position=PlanPoint(x,v,.036f);item.rotation=Quaternion.Euler(0,yaw,0);
            }
            PlacePrefab("WesternSofa","洋室既存ソファ_2人掛け",537,242,0);
            PlacePrefab("WesternArmchair","洋室既存ソファ_1人掛け",591,285,90);
            PlacePrefab("WesternCoffeeTable","洋室既存テーブル",537,287,0);

            // Retain the photographed low table; move it beside the new sofa bed.
            Transform lowTable=first.Find("和室_東_写真ベース/家具/黒漆塗りのちゃぶ台");
            Bounds lowBounds=KitchenVisibleBounds(lowTable);
            Vector3 tableTarget=PlanPoint(414,344,lowBounds.center.y);
            lowTable.position+=tableTarget-lowBounds.center;

            Transform bed=NewGroup("濃いグレーのソファベッド_幅140cm_ソファ状態",root);
            Material grey=WesternMaterial("GuestSofaCharcoal",new Color(.18f,.19f,.20f));
            Material dark=GetMaterial("DarkWood");
            void Part(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool solid=true)
            {
                GameObject box=CreateBox(name,p,size,material,parent);
                if(!solid)Object.DestroyImmediate(box.GetComponent<Collider>());
            }
            // Width is specified; depth 90cm and back height 80cm are provisional.
            Part(bed,"台座",new Vector3(0,.23f,0),new Vector3(1.40f,.30f,.90f),grey);
            foreach(float x in new[]{-.35f,.35f})
                Part(bed,"座面クッション",new Vector3(x,.425f,-.07f),new Vector3(.69f,.09f,.74f),grey);
            Part(bed,"背もたれ",new Vector3(0,.62f,.36f),new Vector3(1.40f,.36f,.18f),grey);
            foreach(float x in new[]{-.59f,.59f})foreach(float z in new[]{-.32f,.32f})
                Part(bed,"脚",new Vector3(x,.04f,z),new Vector3(.07f,.08f,.07f),dark);
            bed.position=PlanPoint(349,344,.036f);bed.rotation=Quaternion.Euler(0,180,0);

            Material sheet=WesternMaterial("GuestFutonSheet",new Color(.91f,.90f,.84f));
            Material quilt=WesternMaterial("GuestFutonQuilt",new Color(.58f,.65f,.69f));
            Material seam=WesternMaterial("GuestFutonSeam",new Color(.48f,.55f,.60f));
            for(int i=0;i<3;i++)
            {
                Transform futon=NewGroup("洋室_布団_"+(i+1)+"_100x200cm",root);
                Part(futon,"敷布団",new Vector3(0,.045f,0),new Vector3(1f,.09f,2f),sheet);
                Part(futon,"掛け布団",new Vector3(0,.12f,-.20f),new Vector3(.96f,.08f,1.55f),quilt,false);
                Part(futon,"枕",new Vector3(0,.135f,.73f),new Vector3(.55f,.09f,.32f),sheet,false);
                foreach(float z in new[]{-.70f,-.30f,.10f})
                    Part(futon,"キルト縫い目",new Vector3(0,.161f,z),new Vector3(.94f,.002f,.008f),seam,false);
                // Two lengthwise, one crosswise at their feet; keep the east entrance clear.
                futon.position=i<2?PlanPoint(i==0?185:232,353,.045f):PlanPoint(232,418,.045f);
                if(i==2)futon.rotation=Quaternion.Euler(0,90,0);
            }
        }

        [MenuItem("Virtual House/Update And Validate Guest Furniture A-C")]
        public static void UpdateAndValidateGuestFurniture()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in new[]{RenovatedScene,AlternativeScene,PlanCScene})
                {
                    Materials.Clear();var scene=EditorSceneManager.OpenScene(path);
                    Transform house=GameObject.Find("住宅概形_図面ベース").transform,first=house.Find("1階");
                    Transform player=GameObject.Find("Player").transform;Vector3 saved=player.position;
                    CreateGuestFurniture(first);Physics.SyncTransforms();
                    Transform root=first.Find(GuestFurnitureName);
                    if(root.Cast<Transform>().Count(t=>t.name.StartsWith("洋室_布団_"))!=3)
                        throw new InvalidOperationException("Three futons required in western room.");
                    foreach(Transform item in root)
                    {
                        Bounds b=KitchenVisibleBounds(item);
                        if(item.name.StartsWith("洋室_布団_"))
                        {
                            if(b.min.x<PlanPoint(158,442,0).x || b.max.x>PlanPoint(310,291,0).x || b.min.z<PlanPoint(158,442,0).z || b.max.z>PlanPoint(310,291,0).z)
                                throw new InvalidOperationException("Futon outside western room: "+item.name);
                        }
                        foreach(Collider collider in item.GetComponentsInChildren<Collider>())
                            foreach(Collider hit in Physics.OverlapBox(collider.bounds.center,collider.bounds.extents-Vector3.one*.004f))
                                if(!hit.transform.IsChildOf(item))throw new InvalidOperationException("Guest furniture collision: "+item.name+" / "+hit.name);
                    }
                    Bounds sofa=KitchenVisibleBounds(root.Find("濃いグレーのソファベッド_幅140cm_ソファ状態"));
                    if(Mathf.Abs(sofa.size.x-1.40f)>.002f)throw new InvalidOperationException("Sofa bed width must be 140cm.");
                    ValidateNandoOpening();
                    ValidateAlternativeRoute(new[]{PlanPoint(330,386,0),PlanPoint(291,386,0),PlanPoint(291,425,0)});
                    ValidateAlternativeRoute(new[]{PlanPoint(325,310,0),PlanPoint(390,313,0),PlanPoint(475,313,0),PlanPoint(551,327,0),PlanPoint(590,340,0)});
                    player.GetComponent<CharacterController>().enabled=false;player.position=saved;player.GetComponent<CharacterController>().enabled=true;
                    EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                    foreach(TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))label.GetComponent<Renderer>().enabled=false;
                    Camera camera=Camera.main;camera.orthographic=true;camera.orthographicSize=4.5f;
                    GameObject.Find("外観_写真ベース")?.SetActive(false);house.Find("2階").gameObject.SetActive(false);
                    foreach(Renderer renderer in first.GetComponentsInChildren<Renderer>())
                        if(renderer.name.Contains("天井"))renderer.enabled=false;
                    camera.transform.position=PlanPoint(390,325,15);camera.transform.LookAt(PlanPoint(390,325,0),Vector3.forward);
                    RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/guest-furniture-"+Path.GetFileNameWithoutExtension(path)+".png"));
                    Debug.Log("GUEST FURNITURE VALIDATED: "+path+"; three futons, 140cm charcoal sofa bed, existing sofa/chair/table, collisions and routes.");
                }
                AssetDatabase.Refresh();
            }
            finally {if(!Application.isBatchMode)EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
    }
}
