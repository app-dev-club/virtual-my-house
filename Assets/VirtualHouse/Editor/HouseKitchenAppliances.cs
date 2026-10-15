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
        // User-supplied overall sizes; appearance approximated from the three reference photos.
        private static void CreateKitchenAppliances(Transform house)
        {
            Transform first=house.Find("1階");
            Transform old=first.Find("キッチン窓_家電");
            if(old!=null) Object.DestroyImmediate(old.gameObject);
            Transform group=NewGroup("キッチン窓_家電",first);
            Material black=WesternMaterial("ApplianceCharcoal",new Color(.025f,.028f,.03f));
            Material glass=WesternMaterial("ApplianceDarkGlass",new Color(.012f,.022f,.024f));
            Material white=WesternMaterial("ApplianceWhite",new Color(.91f,.91f,.88f));
            Material silver=GetMaterial("KitchenSteel");
            Material display=WesternMaterial("ApplianceDisplay",new Color(.42f,.52f,.47f));
            GameObject Part(Transform t,string name,Vector3 p,Vector3 size,Material mat)
                => CreateDecorationBox(name,p,size,mat,t);
            void Round(Transform t,string name,Vector3 p,Vector3 size,Material mat)
            {
                GameObject go=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name=name;go.transform.SetParent(t,false);go.transform.localPosition=p;go.transform.localScale=size;
                go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            Transform Unit(string name,Vector3 size)
            {
                Transform t=NewGroup(name,group);
                var c=t.gameObject.AddComponent<BoxCollider>();c.center=Vector3.up*(size.y/2);c.size=size;
                return t;
            }
            // Local +Z faces the room. Keep the entire oven west of the hood/IH strip.
            Bounds south=first.Find("キッチン窓_くぼみと窓台/南窓/木製窓台_奥行300mm").GetComponent<Renderer>().bounds;
            // East rim of the eastern LANGUDDEN sink (center 1.712m, half-width .28m)
            // aligns with the oven's west edge. Share this position across all plans.
            float ovenX=IkeaPoint(1.712f-.28f,0,0).x+.48f/2;
            CreateBox("レンジ支持板_幅540_奥行420_室内へ120mm",new Vector3(ovenX,south.max.y+.012f,south.min.z+.21f),new Vector3(.54f,.024f,.42f),GetMaterial("LightWood"),group);
            Transform oven=Unit("ER-XD70_480x390x350mm",new Vector3(.48f,.35f,.39f));
            Part(oven,"黒い筐体",new Vector3(0,.183f,-.008f),new Vector3(.48f,.334f,.374f),black);
            Part(oven,"ガラス扉",new Vector3(0,.211f,.181f),new Vector3(.448f,.226f,.012f),glass);
            Part(oven,"横長ハンドル",new Vector3(0,.314f,.185f),new Vector3(.434f,.022f,.02f),black);
            Part(oven,"操作部",new Vector3(0,.061f,.184f),new Vector3(.45f,.063f,.012f),black);
            Part(oven,"液晶表示",new Vector3(.045f,.059f,.192f),new Vector3(.095f,.029f,.004f),display);
            for(int i=0;i<7;i++) Part(oven,"操作キー",new Vector3(-.19f+i*.06f,.033f,.192f),new Vector3(.024f,.007f,.004f),silver);
            foreach(int x in new[]{-1,1}) foreach(int z in new[]{-1,1})
                Part(oven,"脚",new Vector3(x*.19f,.008f,z*.145f),new Vector3(.035f,.016f,.035f),black);
            oven.position=new Vector3(ovenX,south.max.y+.024f,south.min.z+.21f);

            Bounds east=first.Find("キッチン窓_くぼみと窓台/東窓/木製窓台_奥行300mm").GetComponent<Renderer>().bounds;
            Transform cooker=Unit("SERIE_EPC19_260x285x283mm",new Vector3(.26f,.283f,.285f));
            Round(cooker,"白い丸胴",new Vector3(0,.132f,-.005f),new Vector3(.26f,.248f,.265f),white);
            Round(cooker,"白い蓋",new Vector3(0,.224f,-.005f),new Vector3(.245f,.075f,.265f),white);
            Part(cooker,"底座",new Vector3(0,.019f,0),new Vector3(.19f,.038f,.21f),white);
            Part(cooker,"黒い操作パネル",new Vector3(0,.134f,.133f),new Vector3(.155f,.14f,.012f),black);
            Part(cooker,"蓋の取手",new Vector3(0,.269f,0),new Vector3(.075f,.028f,.055f),silver);
            for(int r=0;r<3;r++) for(int c=0;c<3;c++)
                Round(cooker,"操作ボタン",new Vector3((c-1)*.042f,.09f+r*.035f,.138f),new Vector3(.012f,.012f,.003f),silver);
            cooker.rotation=Quaternion.Euler(0,-90,0);
            CreateBox("圧力なべ敷板_280x290x6mm",new Vector3(east.center.x,east.max.y+.003f,east.center.z-.32f),new Vector3(.29f,.006f,.28f),GetMaterial("LightWood"),group);
            cooker.position=new Vector3(east.center.x,east.max.y+.006f,east.center.z-.32f);

            Transform kettle=Unit("EGL-C1280_285x180x225mm",new Vector3(.285f,.225f,.18f));
            Round(kettle,"電源ベース",new Vector3(0,.012f,0),new Vector3(.175f,.024f,.18f),black);
            Round(kettle,"黒いポット",new Vector3(0,.106f,0),new Vector3(.15f,.185f,.15f),black);
            Round(kettle,"蓋",new Vector3(0,.195f,0),new Vector3(.105f,.012f,.105f),black);
            Part(kettle,"蓋つまみ",new Vector3(0,.213f,0),new Vector3(.025f,.024f,.025f),black);
            Vector3[] spout={new Vector3(-.055f,.06f,0),new Vector3(-.095f,.08f,0),new Vector3(-.116f,.115f,0),new Vector3(-.126f,.166f,0),new Vector3(-.1375f,.185f,0)};
            for(int i=1;i<spout.Length;i++) KitchenRod(kettle,"細口注ぎ口",spout[i-1],spout[i],.01f,black);
            Vector3[] handle={new Vector3(.05f,.18f,0),new Vector3(.105f,.183f,0),new Vector3(.135f,.085f,0),new Vector3(.075f,.065f,0)};
            for(int i=1;i<handle.Length;i++) KitchenRod(kettle,"角形ハンドル",handle[i-1],handle[i],.015f,black);
            kettle.rotation=Quaternion.Euler(0,-90,0);
            // 80mm between the overall silhouettes, including spout and handle.
            kettle.position=new Vector3(east.center.x,east.max.y,cooker.position.z+.26f/2+.285f/2+.08f);
        }

        [MenuItem("Virtual House/Update And Validate Kitchen Appliances All Plans")]
        public static void UpdateAndValidateKitchenAppliances()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in new[]{OutputScene,RenovatedScene,AlternativeScene,PlanCScene})
                {
                    Materials.Clear();
                    var scene=EditorSceneManager.OpenScene(path);
                    Transform house=GameObject.Find("住宅概形_図面ベース").transform;
                    CreateKitchenAppliances(house);
                    Physics.SyncTransforms();
                    Transform first=house.Find("1階"),group=first.Find("キッチン窓_家電");
                    var units=group.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("ER-") || c.name.StartsWith("SERIE_") || c.name.StartsWith("EGL-")).ToArray();
                    if(units.Length!=3) throw new InvalidOperationException("Expected three appliances");
                    foreach(var unit in units)
                    {
                        bool oven=unit.name.StartsWith("ER-");
                        Bounds support=(oven?group.Find("レンジ支持板_幅540_奥行420_室内へ120mm"):first.Find("キッチン窓_くぼみと窓台/東窓/木製窓台_奥行300mm")).GetComponent<Collider>().bounds;
                        if(unit.name.StartsWith("SERIE_")) support=group.Find("圧力なべ敷板_280x290x6mm").GetComponent<Collider>().bounds;
                        Bounds b=unit.bounds;
                        if(b.min.x<support.min.x-.001f || b.max.x>support.max.x+.001f || b.min.z<support.min.z-.001f || b.max.z>support.max.z+.001f || Mathf.Abs(b.min.y-support.max.y)>.001f)
                            throw new InvalidOperationException("Appliance is not supported: "+unit.name);
                        foreach(var other in units) if(other!=unit && b.Intersects(other.bounds)) throw new InvalidOperationException("Appliances overlap");
                        foreach(var obstacle in house.GetComponentsInChildren<Collider>())
                            if(!obstacle.transform.IsChildOf(group) && b.Intersects(obstacle.bounds) && obstacle.bounds.max.y>b.min.y+.002f)
                                throw new InvalidOperationException("Appliance collision: "+unit.name+" / "+obstacle.name);
                        if(oven && b.max.x>IkeaPoint(.75f,0,0).x-.10f) throw new InvalidOperationException("Oven overlaps reserved IH/hood strip");
                        // Faucets are decorative meshes without colliders; check their visible bounds too.
                        if(oven)
                        {
                            foreach(var faucet in first.GetComponentsInChildren<Renderer>().Where(r=>r.name.StartsWith("混合栓") || r.name.StartsWith("吐水") || r.name.StartsWith("蛇口")))
                                if(b.Intersects(faucet.bounds)) throw new InvalidOperationException("Oven overlaps faucet: "+faucet.name);
                            if(Mathf.Abs(b.min.x-IkeaPoint(1.432f,0,0).x)>.001f)
                                throw new InvalidOperationException("Oven west edge must align with sink east rim");
                        }
                    }
                    Transform player=GameObject.Find("Player").transform;
                    Vector3 playerPosition=player.position;
                    if(path!=OutputScene) ValidateRenovatedKitchen();
                    player.GetComponent<CharacterController>().enabled=false;
                    player.position=playerPosition;
                    player.GetComponent<CharacterController>().enabled=true;
                    EditorSceneManager.SaveScene(scene);
                    foreach(TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) label.GetComponent<Renderer>().enabled=false;
                    Camera camera=Camera.main;camera.orthographic=false;camera.fieldOfView=72;
                    camera.transform.position=PlanPoint(760,365,1.65f);camera.transform.LookAt(PlanPoint(805,413,1.1f));
                    RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/kitchen-appliances-"+Path.GetFileNameWithoutExtension(path)+".png"));
                    Debug.Log("KITCHEN APPLIANCES VALIDATED: "+path);
                }
                AssetDatabase.SaveAssets();
            }
            finally { if(!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
