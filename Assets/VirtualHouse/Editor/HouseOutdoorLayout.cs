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
        // Image (8), same calibration as image (7). Dimensions are layout assumptions.
        private const string OutdoorName = "屋外計画_図面8";
        private static Material OutdoorMat(string name, float r, float g, float b)
            => WesternMaterial("Outdoor_" + name, new Color(r,g,b));
        private static Transform OutdoorGroup(Transform root,string name,float x,float v)
        {
            Transform t=NewGroup(name,root);t.position=SitePoint(x,v);return t;
        }
        private static GameObject OutdoorBox(Transform t,string name,Vector3 p,Vector3 size,Material mat)
            => CreateBox(name,t.position+p,size,mat,t);
        private static GameObject OutdoorRound(Transform t,string name,Vector3 p,Vector3 size,Material mat, bool solid=false)
        {
            GameObject o=GameObject.CreatePrimitive(PrimitiveType.Sphere);o.name=name;
            o.transform.SetParent(t,false);o.transform.localPosition=p;o.transform.localScale=size;
            o.GetComponent<Renderer>().sharedMaterial=mat;
            if(!solid) Object.DestroyImmediate(o.GetComponent<Collider>());return o;
        }
        private static void OutdoorRod(Transform t,string name,Vector3 a,Vector3 b,float d,Material mat)
            => KitchenRod(t,name,t.position+a,t.position+b,d,mat);

        private static void CreateOutdoorLayout(Transform house)
        {
            Transform site=house.Find("敷地_図面7");
            if(site==null) throw new InvalidOperationException("Outdoor layout requires the traced site.");
            Transform old=site.Find(OutdoorName);if(old!=null) Object.DestroyImmediate(old.gameObject);
            Transform garden=site.Find("庭_外観写真20260930");
            // Remove only trees occupying the new beds, parking and bench/access areas.
            foreach(string name in new[]{"南西_赤い実の木","北西_葉の密な木","玄関脇_低木","DK前_針葉低木","東側_大きな葉の低木","北東_低木"})
            {
                Transform tree=garden?.Find(name);if(tree!=null) Object.DestroyImmediate(tree.gameObject);
            }
            Transform root=NewGroup(OutdoorName,site);
            Material black=OutdoorMat("Black",.035f,.04f,.045f),steel=OutdoorMat("Steel",.60f,.63f,.65f);
            Material wood=OutdoorMat("Cedar",.53f,.31f,.15f),blue=OutdoorMat("PoolBlue",.08f,.48f,.79f);
            Material grey=OutdoorMat("SientaGrey",.40f,.43f,.45f),glass=OutdoorMat("VehicleGlass",.075f,.15f,.18f);
            Material white=OutdoorMat("Ivory",.91f,.91f,.84f);
            CreateDetailedOutdoorVehicles(root);

            Transform tap=OutdoorGroup(root,"洗車用の蛇口とホース",720,329);
            OutdoorBox(tap,"水栓柱",new Vector3(0,.45f,0),new Vector3(.12f,.9f,.12f),steel);
            OutdoorRod(tap,"蛇口",new Vector3(0,.77f,0),new Vector3(.22f,.77f,0),.04f,steel);
            OutdoorRod(tap,"吐水口",new Vector3(.22f,.77f,0),new Vector3(.22f,.69f,0),.04f,steel);
            OutdoorBox(tap,"十字ハンドル",new Vector3(.06f,.83f,0),new Vector3(.16f,.025f,.025f),steel);
            for(int j=0;j<3;j++)for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12,r=.25f+j*.035f;
                OutdoorRod(tap,"巻いたホース",new Vector3(.4f+Mathf.Cos(a)*r,.025f,Mathf.Sin(a)*r),new Vector3(.4f+Mathf.Cos(b)*r,.025f,Mathf.Sin(b)*r),.024f,blue);
            }
            Transform bench=OutdoorGroup(root,"玄関東_収納付きベンチ",663,340);
            OutdoorBox(bench,"収納箱",new Vector3(0,.22f,0),new Vector3(.55f,.43f,1.60f),wood);
            OutdoorBox(bench,"開閉式座面",new Vector3(0,.46f,0),new Vector3(.62f,.06f,1.70f),wood);
            OutdoorBox(bench,"背もたれ",new Vector3(-.27f,.70f,0),new Vector3(.065f,.45f,1.70f),wood);
            OutdoorBox(bench,"収納取手",new Vector3(.283f,.33f,0),new Vector3(.025f,.04f,.22f),black);
            Transform laundry=OutdoorGroup(root,"北出口すぐ_物干し",569,175);
            foreach(float x in new[]{-1.25f,1.25f})
            {
                OutdoorRod(laundry,"物干し支柱",new Vector3(x,0,0),new Vector3(x,1.85f,0),.055f,steel);
                OutdoorBox(laundry,"脚",new Vector3(x,.055f,0),new Vector3(.35f,.11f,.65f),white);
            }
            OutdoorRod(laundry,"物干し竿",new Vector3(-1.45f,1.85f,0),new Vector3(1.45f,1.85f,0),.032f,steel);
            for(int i=0;i<5;i++)
            {
                float x=-.96f+i*.48f;Material cloth=i%2==0?white:blue;
                OutdoorRod(laundry,"ハンガー",new Vector3(x,1.84f,0),new Vector3(x,1.65f,0),.014f,steel);
                OutdoorRod(laundry,"ハンガー肩",new Vector3(x,1.65f,0),new Vector3(x,1.56f,-.23f),.012f,steel);
                OutdoorRod(laundry,"ハンガー肩",new Vector3(x,1.65f,0),new Vector3(x,1.56f,.23f),.012f,steel);
                OutdoorBox(laundry,"干したシャツ",new Vector3(x,1.32f,0),new Vector3(.035f,.59f,.34f),cloth);
                OutdoorBox(laundry,"袖",new Vector3(x,1.55f,0),new Vector3(.035f,.17f,.46f),cloth);
            }
            Transform swing=OutdoorGroup(root,"和室南_ブランコ_共通フレーム",508.5f,377);
            foreach(float x in new[]{-1.62f,1.62f})foreach(float z in new[]{-.74f,.74f})
                OutdoorRod(swing,"A型支柱",new Vector3(x,0,z),new Vector3(x,2.15f,0),.11f,wood);
            OutdoorRod(swing,"共通の上梁",new Vector3(-1.72f,2.15f,0),new Vector3(1.72f,2.15f,0),.14f,wood);
            foreach(float seatX in new[]{-.78f,.78f})
            {
                foreach(float dx in new[]{-.28f,.28f}) OutdoorRod(swing,"吊りロープ",new Vector3(seatX+dx,.48f,0),new Vector3(seatX+dx,2.15f,0),.025f,white);
                OutdoorBox(swing,"座板",new Vector3(seatX,.46f,0),new Vector3(.72f,.07f,.35f),wood);
            }
            Transform pool=OutdoorGroup(root,"和室南_ビニールプール",575,367);
            // Original footprint, half the previous water depth: .60m.
            OutdoorBox(pool,"底",new Vector3(0,.03f,0),new Vector3(1.85f,.06f,1.45f),blue);
            for(int i=0;i<3;i++)
            {
                float h=.13f+i*.23f;
                foreach(float x in new[]{-.84f,.84f}) OutdoorRound(pool,"空気室_側面",new Vector3(x,h,0),new Vector3(.22f,.26f,1.45f),blue,true);
                foreach(float z in new[]{-.64f,.64f}) OutdoorRound(pool,"空気室_端面",new Vector3(0,h,z),new Vector3(1.70f,.26f,.22f),blue,true);
            }
            GameObject water=OutdoorBox(pool,"水面_深さ0.6m",new Vector3(0,.66f,0),new Vector3(1.59f,.015f,1.19f),OutdoorMat("Water",.18f,.69f,.79f));
            Object.DestroyImmediate(water.GetComponent<Collider>());
            Transform bbq=OutdoorGroup(root,"洋室南_バーベキュー",420,369);
            for(int table=0;table<2;table++)
            {
                float x=table==0?-1.04f:1.04f;
                Transform t=NewGroup("テーブル_"+(table+1),bbq);t.position=bbq.position+new Vector3(x,0,-.50f);
                OutdoorBox(t,"天板",new Vector3(0,.73f,0),new Vector3(.70f,.07f,1.75f),wood);
                foreach(float lx in new[]{-.29f,.29f})foreach(float z in new[]{-.70f,.70f}) OutdoorBox(t,"脚",new Vector3(lx,.35f,z),new Vector3(.07f,.70f,.07f),black);
                for(int side=-1;side<=1;side+=2)for(int j=0;j<3;j++)
                {
                    Transform chair=NewGroup("椅子_"+(table*6+(side+1)/2*3+j+1),bbq);
                    chair.position=t.position+new Vector3(side*.53f,0,(j-1)*.59f);
                    OutdoorBox(chair,"座面",new Vector3(0,.43f,0),new Vector3(.40f,.06f,.44f),wood);
                    OutdoorBox(chair,"背",new Vector3(side*.18f,.67f,0),new Vector3(.055f,.45f,.44f),wood);
                    foreach(float lx in new[]{-.15f,.15f})foreach(float z in new[]{-.16f,.16f}) OutdoorBox(chair,"脚",new Vector3(lx,.20f,z),new Vector3(.035f,.4f,.035f),black);
                }
            }
            for(int i=0;i<2;i++)
            {
                Transform grill=NewGroup("バーベキュー台_"+(i+1),bbq);grill.position=bbq.position+new Vector3(-3.15f,0,i==0?-.65f:.65f);
                OutdoorBox(grill,"火床",new Vector3(0,.71f,0),new Vector3(.85f,.25f,.46f),black);
                foreach(float x in new[]{-.32f,.32f})foreach(float z in new[]{-.16f,.16f}) OutdoorBox(grill,"脚",new Vector3(x,.30f,z),new Vector3(.04f,.60f,.04f),steel);
                for(int j=0;j<13;j++) OutdoorRod(grill,"焼き網",new Vector3(-.39f+j*.065f,.85f,-.22f),new Vector3(-.39f+j*.065f,.85f,.22f),.012f,steel);
                grill.rotation=Quaternion.Euler(0,90,0);
            }
            Transform oven=OutdoorGroup(root,"BBQ南_レンガのピザ窯",400,418);
            Material brick=OutdoorMat("Brick",.61f,.25f,.12f),mortar=OutdoorMat("Mortar",.66f,.61f,.49f);
            // Build brick proportions, then normalize the full envelope to .69 x .69 x .585m below.
            OutdoorBox(oven,"炉床",new Vector3(0,.045f,0),new Vector3(.60f,.09f,.40f),mortar);
            OutdoorBox(oven,"炉の奥壁",new Vector3(0,.34f,-.17f),new Vector3(.60f,.50f,.06f),brick);
            foreach(float x in new[]{-.26f,.26f}) OutdoorBox(oven,"炉の側壁",new Vector3(x,.30f,0),new Vector3(.08f,.42f,.40f),brick);
            OutdoorBox(oven,"平らな天井",new Vector3(0,.555f,0),new Vector3(.60f,.09f,.40f),brick);
            OutdoorBox(oven,"暗い炉内",new Vector3(0,.10f,0),new Vector3(.44f,.015f,.28f),black);
            for(int row=1;row<6;row++)
            {
                OutdoorBox(oven,"背面の目地",new Vector3(0,row*.09f,-.201f),new Vector3(.60f,.008f,.002f),mortar);
                foreach(float x in new[]{-.301f,.301f}) OutdoorBox(oven,"側面の目地",new Vector3(x,row*.09f,0),new Vector3(.002f,.008f,.40f),mortar);
            }
            for(int row=0;row<6;row++)foreach(float z in new[]{-.1f,.1f})foreach(float x in new[]{-.301f,.301f})
                OutdoorBox(oven,"レンガの縦目地",new Vector3(x,.045f+row*.09f,z+(row%2)*.04f),new Vector3(.002f,.08f,.007f),mortar);
            oven.localScale=new Vector3(.69f/.604f,.585f/.60f,.69f/.402f);
            Transform farm=OutdoorGroup(root,"北西_家庭菜園",400,211);
            Material soil=GardenSurface("OutdoorTilledSoil",new Color(.26f,.15f,.085f),new Color(.13f,.07f,.035f),false);
            OutdoorBox(farm,"一面の土",new Vector3(0,.07f,0),new Vector3(1.61f,.14f,2.20f),soil);
        }
        [MenuItem("Virtual House/Update And Validate Outdoor Layout A-C")]
        public static void UpdateAndValidateOutdoorLayout()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in new[]{RenovatedScene,AlternativeScene,PlanCScene})
                {
                    Materials.Clear();var scene=EditorSceneManager.OpenScene(path);
                    Transform house=scene.GetRootGameObjects().First(g=>g.name=="住宅概形_図面ベース").transform;
                    CreateOutdoorLayout(house);Physics.SyncTransforms();
                    Transform player=GameObject.Find("Player").transform;
                    Vector3 savedPlayerPosition=player.position;
                    Transform root=house.Find("敷地_図面7/"+OutdoorName);
                    Transform bbq=root.Find("洋室南_バーベキュー");
                    if(root.Cast<Transform>().Count(t=>t.name.StartsWith("和室南_ブランコ_"))!=1)
                        throw new InvalidOperationException("Expected one shared swing frame.");
                    if(root.Find("和室南_ブランコ_共通フレーム").Cast<Transform>().Count(t=>t.name=="座板")!=2)
                        throw new InvalidOperationException("Expected two seats on the shared frame.");
                    Transform bench=root.Find("玄関東_収納付きベンチ");
                    if(bench.Find("背もたれ").localPosition.x>=0 || bench.Find("収納取手").localPosition.x<=0)
                        throw new InvalidOperationException("Bench must face east.");
                    Bounds ovenBounds=new Bounds(root.Find("BBQ南_レンガのピザ窯").position,Vector3.zero);
                    foreach(Renderer r in root.Find("BBQ南_レンガのピザ窯").GetComponentsInChildren<Renderer>())ovenBounds.Encapsulate(r.bounds);
                    if(Vector3.Distance(ovenBounds.size,new Vector3(.69f,.585f,.69f))>.01f)
                        throw new InvalidOperationException("Pizza oven must be 69cm wide, 69cm deep, 58.5cm tall.");
                    foreach(Transform t in bbq)
                        if(t.name.StartsWith("バーベキュー台_") && t.position.x>=bbq.Find("テーブル_1").position.x)
                            throw new InvalidOperationException("Grills must be west of the tables.");
                    if(bbq.Cast<Transform>().Count(t=>t.name.StartsWith("椅子_"))!=12 || bbq.Cast<Transform>().Count(t=>t.name.StartsWith("テーブル_"))!=2 || bbq.Cast<Transform>().Count(t=>t.name.StartsWith("バーベキュー台_"))!=2)
                        throw new InvalidOperationException("Outdoor furniture counts differ from the requested layout.");
                    var ground=house.Find("敷地_図面7/庭_台形").GetComponent<Collider>();
                    foreach(Renderer r in root.GetComponentsInChildren<Renderer>())
                    {
                        Bounds b=r.bounds;
                        foreach(float x in new[]{b.min.x,b.max.x})foreach(float z in new[]{b.min.z,b.max.z})
                            if(!ground.Raycast(new Ray(new Vector3(x,10,z),Vector3.down),out _,20)) throw new InvalidOperationException("Outdoor object outside lot: "+r.name);
                    }
                    ValidateAlternativeRoute(new[]{SitePoint(633,374,0),SitePoint(681,352,0),SitePoint(715,343,0),SitePoint(744,323,0),SitePoint(825,323,0)});
                    ValidateAlternativeRoute(new[]{PlanPoint(575,197,0),PlanPoint(575,158,0)});
                    ValidateAlternativeRoute(new[]{SitePoint(420,395),SitePoint(420,348)});
                    Vector3[] gardenRoute={SitePoint(825,323),SitePoint(735,344),SitePoint(665,365),SitePoint(600,385),SitePoint(540,395),SitePoint(475,406),SitePoint(440,405),SitePoint(365,405)};
                    ValidateAlternativeRoute(gardenRoute);
                    player.GetComponent<CharacterController>().enabled=false;
                    player.position=savedPlayerPosition;
                    player.GetComponent<CharacterController>().enabled=true;
                    EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                    Debug.Log("OUTDOOR VALIDATION PASSED: "+path+"; counts, lot bounds, entrance / parking approach, north exit, BBQ aisle and south garden walk.");
                }
                Camera camera=Camera.main;
                GameObject.Find("外観_写真ベース")?.SetActive(false);GameObject.Find("2階")?.SetActive(false);
                camera.transform.position=SitePoint(582,312,35);camera.transform.LookAt(SitePoint(582,312,0),Vector3.forward);
                camera.orthographic=true;camera.orthographicSize=8.5f;
                RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/outdoor-layout-plan-preview.png"));
                foreach(TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                    label.GetComponent<Renderer>().enabled=false;
                camera.orthographic=false;camera.transform.position=SitePoint(685,570,15);camera.transform.LookAt(SitePoint(540,300,0));
                RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/outdoor-layout-perspective-preview.png"));
                camera.fieldOfView=45;
                Vector3 carPosition=SitePoint(791,292);
                camera.transform.position=carPosition+new Vector3(4.3f,2.8f,-5.4f);camera.transform.LookAt(carPosition+Vector3.up*.82f);
                RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/outdoor-car-preview.png"));
                Vector3 bikePosition=SitePoint(755,214);
                camera.transform.position=bikePosition+new Vector3(1.85f,1.48f,3.1f);camera.transform.LookAt(bikePosition+Vector3.up*.64f);
                RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/outdoor-bike-preview.png"));
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
