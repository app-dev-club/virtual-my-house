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
        // Desk dimensions supplied by the user; device dimensions estimated from the photo.
        private const float OfficeWidth = 1.20f, OfficeDepth = .60f, OfficeHeight = .86f;
        private static void CreateHomeOffice(Transform first)
        {
            Transform old = first.Find("仕事部屋_東物入");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Transform office = NewGroup("仕事部屋_東物入", first);
            Transform desk = NewGroup("昇降デスク_120x60x86cm_東向き", office);
            Material wood = WesternMaterial("OfficeWarmWood",new Color(.49f,.27f,.09f));
            Material black = WesternMaterial("OfficeCharcoal",new Color(.035f,.04f,.05f));
            Material screen = WesternMaterial("OfficeScreen",new Color(.015f,.024f,.038f));
            Material blue = WesternMaterial("OfficeBlueScreen",new Color(.02f,.30f,.72f));
            Material silver = GetMaterial("KitchenSteel");
            void Part(string name,Vector3 p,Vector3 size,Material mat,bool solid=false)
            {
                p.x *= OfficeWidth / 1.4f;
                if(name != "木目天板") { size.x *= OfficeWidth / 1.4f; if(p.y > .60f) p.y += .14f; }
                if(name == "昇降脚_上段") { p.y += .07f; size.y += .14f; }
                var box=CreateBox(name,p,size,mat,desk);
                if(!solid) Object.DestroyImmediate(box.GetComponent<Collider>());
            }
            Part("木目天板",new Vector3(0,OfficeHeight-.015f,0),new Vector3(OfficeWidth,.03f,OfficeDepth),wood,true);
            foreach(int side in new[]{-1,1})
            {
                float x=side*.53f;
                Part("T字脚_床",new Vector3(x,.035f,0),new Vector3(.075f,.07f,.55f),black,true);
                Part("昇降脚_下段",new Vector3(x,.25f,.04f),new Vector3(.075f,.43f,.075f),black,true);
                Part("昇降脚_上段",new Vector3(x,.55f,.04f),new Vector3(.06f,.29f,.06f),black,true);
                Part("天板支持",new Vector3(x,.675f,0),new Vector3(.07f,.06f,.49f),black);
            }
            Part("脚間フレーム",new Vector3(0,.64f,.08f),new Vector3(1.1f,.08f,.06f),black);
            Part("昇降操作パネル",new Vector3(.51f,.665f,-.285f),new Vector3(.19f,.045f,.045f),black);
            for(int i=0;i<4;i++) Part("メモリーボタン",new Vector3(.455f+i*.033f,.665f,-.31f),new Vector3(.016f,.012f,.003f),silver);
            // Local +Z is east/the wall, local -X is the user's left (north).
            Part("大型モニター_背面",new Vector3(-.40f,1.17f,.19f),new Vector3(.62f,.365f,.04f),black);
            Part("大型モニター_画面",new Vector3(-.40f,1.17f,.167f),new Vector3(.585f,.327f,.006f),screen);
            Part("モニターアーム支柱",new Vector3(-.30f,.92f,.245f),new Vector3(.035f,.4f,.035f),black);
            Part("モニターアーム",new Vector3(-.36f,1.08f,.225f),new Vector3(.18f,.035f,.035f),black);
            Part("ノートPCスタンド",new Vector3(.03f,.83f,.09f),new Vector3(.035f,.22f,.04f),silver);
            var laptop=NewGroup("ノートPC_スタンド上",desk);
            var basePc=CreateDecorationBox("キーボード面",new Vector3(.03f*OfficeWidth/1.4f,1.065f,.055f),new Vector3(.30f*OfficeWidth/1.4f,.015f,.21f),silver,laptop);
            basePc.transform.rotation=Quaternion.Euler(-18,0,0);
            Part("ノートPC_液晶背面",new Vector3(.03f,1.09f,.17f),new Vector3(.305f,.205f,.015f),silver);
            Part("ノートPC_画面",new Vector3(.03f,1.09f,.16f),new Vector3(.28f,.18f,.004f),screen);
            Part("ノートPC_キー",new Vector3(.03f,.947f,.09f),new Vector3(.26f,.008f,.095f),black);
            Part("小型モニター_背面",new Vector3(.40f,.92f,.10f),new Vector3(.36f,.225f,.018f),black);
            Part("小型モニター_画面",new Vector3(.40f,.92f,.088f),new Vector3(.337f,.198f,.006f),blue);
            Part("小型モニター_折り畳み台",new Vector3(.40f,.735f,.06f),new Vector3(.36f,.02f,.24f),black);
            Part("外付けキーボード",new Vector3(-.02f,.737f,-.19f),new Vector3(.43f,.027f,.135f),black);
            for(int row=0;row<4;row++) for(int col=0;col<15;col++)
                Part("キー",new Vector3(-.215f+col*.027f,.754f,-.237f+row*.029f),new Vector3(.021f,.006f,.021f),silver);
            Part("マウス",new Vector3(.31f,.749f,-.205f),new Vector3(.058f,.04f,.10f),black);
            Part("USBハブ",new Vector3(-.49f,.74f,-.12f),new Vector3(.08f,.025f,.04f),black);
            Part("スマートフォン",new Vector3(.62f,.795f,-.025f),new Vector3(.068f,.145f,.013f),black);
            // Put the large display centre on the left edge: half its width overhangs.
            float screenShift = -OfficeWidth/2 - (-.40f*OfficeWidth/1.4f);
            foreach(Transform item in desk)
                if(item.name.StartsWith("大型モニター") || item.name.StartsWith("モニターアーム")
                    || item.name.StartsWith("ノートPC") || item.name.StartsWith("小型モニター"))
                    item.position += Vector3.right*screenShift;
            Transform chair = NewGroup("黒いオフィスチェア",desk);
            void ChairPart(string name,Vector3 p,Vector3 size)
                => CreateBox(name,p,size,black,chair);
            ChairPart("座面クッション",new Vector3(0,.49f,0),new Vector3(.48f,.10f,.46f));
            ChairPart("背もたれ",new Vector3(0,.83f,-.225f),new Vector3(.45f,.60f,.07f));
            ChairPart("ガスシリンダー",new Vector3(0,.27f,0),new Vector3(.065f,.36f,.065f));
            foreach(int side in new[]{-1,1})
            {
                ChairPart("肘掛け支柱",new Vector3(side*.265f,.59f,.015f),new Vector3(.035f,.25f,.035f));
                ChairPart("肘掛け",new Vector3(side*.265f,.715f,.015f),new Vector3(.065f,.045f,.30f));
            }
            for(int i=0;i<5;i++)
            {
                float angle=i*Mathf.PI*2/5;
                Vector3 tip=new Vector3(Mathf.Sin(angle)*.28f,.095f,Mathf.Cos(angle)*.28f);
                KitchenRod(chair,"五本脚",new Vector3(0,.14f,0),tip,.04f,black);
                ChairPart("キャスター",new Vector3(tip.x,.045f,tip.z),new Vector3(.065f,.075f,.065f));
            }
            chair.localPosition=new Vector3(-.10f,0,-.48f);
            // All parts were created around the origin; rotate the complete arrangement east.
            desk.rotation=Quaternion.Euler(0,90,0);
            Vector3 corner=PlanPoint(915,329,0);
            desk.position=corner+new Vector3(-WallThickness/2-.035f-OfficeDepth/2,0,WallThickness/2+.035f+OfficeWidth/2);
            foreach(var label in first.Find("床_部屋別").GetComponentsInChildren<TextMesh>())
                if(label.text=="東物入") label.text="仕事部屋";
        }

        [MenuItem("Virtual House/Update And Validate Home Office A-C")]
        public static void UpdateHomeOfficeAllPlans()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in new[]{RenovatedScene,AlternativeScene,PlanCScene})
                {
                    Materials.Clear();
                    var scene=EditorSceneManager.OpenScene(path);
                    Transform house=scene.GetRootGameObjects().First(g=>g.name=="住宅概形_図面ベース").transform;
                    CreateHomeOffice(house.Find("1階"));
                    Physics.SyncTransforms();
                    Transform office=house.Find("1階/仕事部屋_東物入");
                    Vector3 sw=PlanPoint(840,329,0),ne=PlanPoint(915,254,0);
                    foreach(var r in office.GetComponentsInChildren<Renderer>())
                        if(r.bounds.min.x<sw.x+WallThickness/2 || r.bounds.max.x>ne.x-WallThickness/2 || r.bounds.min.z<sw.z+WallThickness/2 || r.bounds.max.z>ne.z-WallThickness/2)
                            throw new InvalidOperationException("Office furniture outside room: "+r.name);
                    var player=GameObject.Find("Player").GetComponent<CharacterController>();
                    Vector3[] route={PlanPoint(826,310,.04f),PlanPoint(855,310,.04f),PlanPoint(855,268,.04f),PlanPoint(870,268,.04f)};
                    Vector3 start=route[0],end=route[route.Length-1];
                    foreach(bool reverse in new[]{false,true})
                    {
                        player.enabled=false;player.transform.position=reverse?end:start;player.enabled=true;
                        Physics.SyncTransforms();
                        for(int step=1;step<route.Length;step++)
                        {
                            Vector3 target=route[reverse?route.Length-1-step:step];
                            for(int i=0;i<240;i++) player.Move(new Vector3(Mathf.Clamp(target.x-player.transform.position.x,-.025f,.025f),-.02f,Mathf.Clamp(target.z-player.transform.position.z,-.025f,.025f)));
                            Vector3 delta=player.transform.position-target;delta.y=0;
                            if(delta.magnitude>.10f) throw new InvalidOperationException("Office chair approach obstructed: "+path+" step "+step);
                        }
                    }
                    // Reloading the saved scene below keeps preview camera/player changes out of the asset.
                    player.enabled=false;player.transform.position=start;player.enabled=true;
                    EditorSceneManager.OpenScene(path);
                    house=GameObject.Find("住宅概形_図面ベース").transform;
                    CreateHomeOffice(house.Find("1階"));
                    EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                    foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) label.GetComponent<Renderer>().enabled=false;
                    Camera camera=Camera.main;
                    camera.transform.position=PlanPoint(848,271,1.65f);
                    camera.transform.LookAt(PlanPoint(895,304,.94f));camera.fieldOfView=75;
                    RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/home-office-"+Path.GetFileNameWithoutExtension(path)+".png"));
                    Debug.Log("Home office bounds and bidirectional entrance walk passed: "+path);
                }
                AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            }
            finally { if(!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
