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
        private const string UpperLargeRoomName = "二階8畳和室_写真ベース";

        // Six photos IMG_20260930_111802658 through 111816666. Detail sizes estimated.
        private static void CreateUpperLargeRoom(Transform second)
        {
            Transform old = second.Find(UpperLargeRoomName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Transform room = NewGroup(UpperLargeRoomName, second);
            Vector3 sw = PlanPoint(365,310,0,true), ne = PlanPoint(515,158,0,true);
            float l=sw.x,r=ne.x,s=sw.z,n=ne.z,y=SecondFloorY,cx=(l+r)/2,cz=(s+n)/2;
            Material sand=WesternMaterial("UpperLargeGreySand",new Color(.61f,.61f,.56f));
            Material wood=WesternMaterial("UpperLargeHoneyWood",new Color(.56f,.37f,.21f));
            Material ceiling=WesternMaterial("UpperLargeCeilingWood",new Color(.66f,.52f,.36f));
            Material dark=GetMaterial("DarkWood"),paper=GetMaterial("Paper");
            Material curtain=WesternMaterial("UpperLargeOchreCurtain",new Color(.64f,.43f,.22f));
            Material blue=WesternMaterial("UpperLargeFusumaBlue",new Color(.27f,.43f,.49f));
            void Part(string name,Vector3 pos,Vector3 size,Material mat,bool solid=false)
            {
                if(solid) CreateBox(name,pos,size,mat,room);
                else CreateDecorationBox(name,pos,size,mat,room);
            }
            Transform mats=NewGroup("八畳_畳表と畳縁",room);
            CreateEightTatamiLayout(mats,l,s,r-l,n-s); mats.position=Vector3.up*y;
            Part("壁_二階大和室_北砂壁",new Vector3(cx,y+1.30f,n-.085f),new Vector3(r-l,2.60f,.028f),sand);
            Part("壁_二階大和室_西砂壁",new Vector3(l+.085f,y+1.30f,cz),new Vector3(.028f,2.60f,n-s),sand);
            // Window openings match the existing exterior sashes, including the inward finish.
            CutWindowOpening(room,PlanPoint(440,158,0,true),100*PlanScale,y+.82f,1.25f,false);
            CutWindowOpening(room,PlanPoint(365,252,0,true),70*PlanScale,y+.80f,1.25f,true);
            foreach(float z in new[]{s,n})
            {
                float insideZ=z==s?s+.12f:n-.12f;
                Part("鴨居",new Vector3(cx,y+2.08f,insideZ),new Vector3(r-l,.10f,.10f),wood);
                Part("上部砂壁",new Vector3(cx,y+2.36f,insideZ),new Vector3(r-l,.48f,.05f),sand);
                Part("天井廻縁",new Vector3(cx,y+2.59f,insideZ),new Vector3(r-l,.07f,.065f),wood);
            }
            foreach(float x in new[]{l,r})
            {
                Part("天井廻縁",new Vector3(x,y+2.59f,cz),new Vector3(.065f,.07f,n-s),wood);
                foreach(float z in new[]{s,n}) Part("隅柱",new Vector3(x==l?l+.12f:r-.12f,y+1.3f,z==s?s+.12f:n-.12f),new Vector3(.105f,2.60f,.105f),wood);
            }
            foreach(float x in new[]{PlanPoint(390,158,0,true).x,PlanPoint(490,158,0,true).x})
                Part("北窓柱",new Vector3(x,y+1.30f,n-.12f),new Vector3(.075f,2.60f,.085f),wood);
            foreach(float z in new[]{PlanPoint(365,217,0,true).z,PlanPoint(365,287,0,true).z})
                Part("西窓柱",new Vector3(l+.12f,y+1.30f,z),new Vector3(.085f,2.60f,.075f),wood);
            Part("西長押",new Vector3(l+.12f,y+2.10f,cz),new Vector3(.07f,.07f,n-s),wood);
            void Curtains(bool west,float center,float span,float bottom)
            {
                // Gather both panels at the window edges, leaving the central view open.
                float bundle=Mathf.Min(.25f,span*.12f);
                for(int side=0;side<2;side++) for(int i=0;i<14;i++)
                {
                    float u=(side==0?-1:1)*(span/2-bundle/2)+(i-6.5f)*bundle/14;
                    float h=1.34f+Mathf.Sin(i*.9f)*.025f;
                    Vector3 pos=west?new Vector3(l+.17f+(i%2)*.025f,y+bottom+.625f,center+u):new Vector3(center+u,y+bottom+.625f,n-.17f-(i%2)*.025f);
                    Part("黄土色カーテン_左右に開いたひだ",pos,west?new Vector3(.055f,h,bundle/14+.004f):new Vector3(bundle/14+.004f,h,.055f),curtain);
                }
                Part("カーテンレール",west?new Vector3(l+.18f,y+2.13f,center):new Vector3(center,y+2.15f,n-.18f),west?new Vector3(.03f,.025f,span+.12f):new Vector3(span+.12f,.025f,.03f),GetMaterial("WindowFrame"));
                Part("木製窓台",west?new Vector3(l+.04f,y+bottom,center):new Vector3(center,y+bottom,n-.04f),west?new Vector3(.19f,.035f,span):new Vector3(span,.035f,.19f),wood);
            }
            Curtains(false,PlanPoint(440,158,0,true).x,100*PlanScale,.82f);
            Curtains(true,PlanPoint(365,252,0,true).z,70*PlanScale,.80f);
            // South shoji parked in pairs, leaving the original corridor entrance walkable.
            float a=PlanPoint(402,310,0,true).x,b=r,leaf=(b-a)/4;
            Transform shoji=NewGroup("南四枚障子_中央開放",room);
            for(int i=0;i<4;i++)
            {
                float x=i<2?a+leaf/2:b-leaf/2;
                CreateShojiPanelX(shoji,"障子_"+i,x,s+(i%2)*.045f,leaf);
                float faceZ=s+(i%2)*.045f+.028f;
                for(int j=1;j<4;j++) CreateDecorationBox("障子縦格子_室内",new Vector3(x-leaf/2+leaf*j/4,1.075f,faceZ),new Vector3(.016f,2.05f,.022f),wood,shoji);
                for(int j=1;j<8;j++) CreateDecorationBox("障子横格子_室内",new Vector3(x,.05f+2.05f*j/8,faceZ),new Vector3(leaf,.016f,.022f),wood,shoji);
            }
            shoji.position=Vector3.up*y;
            Part("南西袖壁",new Vector3((l+a)/2,y+1.03f,s+.075f),new Vector3(a-l,2.06f,.025f),sand);
            // East storage occupies the existing 515..553 / 194..275 plan footprint.
            float front=r+.035f,back=PlanPoint(553,194,0,true).x-.09f;
            float zn=PlanPoint(515,194,0,true).z,zs=PlanPoint(515,275,0,true).z,zc=(zn+zs)/2,width=zn-zs;
            Part("押入れ奥板",new Vector3(back,y+1.28f,zc),new Vector3(.035f,2.56f,width),wood,true);
            foreach(float z in new[]{zs,zn})
            {
                Part("押入れ側板",new Vector3((front+back)/2,y+1.28f,z),new Vector3(back-front,2.56f,.035f),wood,true);
                Part("押入れ縦枠",new Vector3(front,y+1.30f,z),new Vector3(.09f,2.60f,.09f),wood);
            }
            foreach(float h in new[]{.04f,.91f,2.02f,2.57f})
                Part("押入れ棚と天袋底",new Vector3((front+back)/2,y+h,zc),new Vector3(back-front,.045f,width),wood,true);
            void Fusuma(string name,float z,float height,float baseY,float w,bool checks)
            {
                float x=front-.025f;
                Part(name,new Vector3(x,y+baseY+height/2,z),new Vector3(.035f,height,w),paper);
                foreach(float zz in new[]{z-w/2,z+w/2}) Part("襖黒縁",new Vector3(x-.025f,y+baseY+height/2,zz),new Vector3(.025f,height,.025f),dark);
                foreach(float yy in new[]{baseY,baseY+height}) Part("襖黒縁",new Vector3(x-.025f,y+yy,z),new Vector3(.025f,.025f,w),dark);
                Part("襖引手",new Vector3(x-.045f,y+baseY+height*.51f,z-w*.32f),new Vector3(.018f,.055f,.05f),dark);
                if(checks) for(int row=0;row<3;row++) for(int col=0;col<6;col++)
                    if((row+col)%2==0) Part("襖裾の青い市松模様",new Vector3(x-.028f,y+baseY+.055f+row*.11f,z-w/2+(col+.5f)*w/6),new Vector3(.009f,.11f,w/6),blue);
            }
            Fusuma("押入れ襖_半開き",zc,1.94f,.055f,width*.49f,true);
            Fusuma("天袋襖_半開き",zc+width*.245f,.48f,2.075f,width*.49f,false);
            Part("南東砂壁",new Vector3(r-.075f,y+1.29f,(s+zs)/2),new Vector3(.025f,2.58f,zs-s),sand);
            // Ceiling boards, narrow battens and the distinctive square pendant.
            Part("板張り天井",new Vector3(cx,y+2.625f,cz),new Vector3(r-l,.045f,n-s),ceiling);
            for(int i=0;i<7;i++) Part("天井棹縁",new Vector3(cx,y+2.585f,s+(n-s)*i/6),new Vector3(r-l,.035f,.028f),wood);
            for(int i=1;i<4;i++) Part("天井板目地",new Vector3(l+(r-l)*i/4,y+2.598f,cz),new Vector3(.008f,.008f,n-s),wood);
            KitchenRod(room,"吊り照明コード",new Vector3(cx,y+2.60f,cz),new Vector3(cx,y+2.20f,cz),.009f,dark);
            Part("角型吊り照明_乳白シェード",new Vector3(cx,y+2.04f,cz),new Vector3(.43f,.32f,.43f),paper);
            foreach(float dx in new[]{-.23f,.23f}) foreach(float dz in new[]{-.23f,.23f})
                Part("照明黒い縦枠",new Vector3(cx+dx,y+2.04f,cz+dz),new Vector3(.025f,.36f,.025f),dark);
            foreach(float h in new[]{1.86f,2.22f}) foreach(bool alongX in new[]{false,true}) foreach(int sign in new[]{-1,1})
                Part("照明黒い横枠",new Vector3(cx+(alongX?0:sign*.23f),y+h,cz+(alongX?sign*.23f:0)),alongX?new Vector3(.48f,.025f,.025f):new Vector3(.025f,.025f,.48f),dark);
            GameObject lamp=new GameObject("角型照明");lamp.transform.SetParent(room,false);
            lamp.transform.position=new Vector3(cx,y+1.82f,cz);
            Light light=lamp.AddComponent<Light>();light.type=LightType.Point;
            light.range=4.5f;light.intensity=.85f;light.color=new Color(1f,.85f,.65f);
            Part("西壁コンセント",new Vector3(l+.105f,y+.18f,s+.30f),new Vector3(.018f,.10f,.065f),paper);
        }

        [MenuItem("Virtual House/Update And Validate Upper Large Room All Plans")]
        public static void UpdateAndValidateUpperLargeRoom()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in new[]{OutputScene,RenovatedScene,AlternativeScene,PlanCScene})
                {
                    Materials.Clear(); var scene=EditorSceneManager.OpenScene(path);
                    Transform house=scene.GetRootGameObjects().First(g=>g.name=="住宅概形_図面ベース").transform;
                    CreateUpperLargeRoom(house.Find("2階")); Physics.SyncTransforms();
                    Transform room=house.Find("2階/"+UpperLargeRoomName);
                    if(room.Find("八畳_畳表と畳縁").childCount!=8) throw new InvalidOperationException("Expected eight tatami mats");
                    // Check a player's full capsule at both entrances and along the central route.
                    foreach(var p in new[]{PlanPoint(458,310,SecondFloorY,true),PlanPoint(515,176,SecondFloorY,true),PlanPoint(458,250,SecondFloorY,true)})
                        foreach(var hit in Physics.OverlapCapsule(p+Vector3.up*.36f,p+Vector3.up*1.5f,.26f))
                            if(hit.transform.IsChildOf(room)) throw new InvalidOperationException("Upper room route blocked by "+hit.name);
                    EditorSceneManager.SaveScene(scene);
                    foreach(TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) label.GetComponent<Renderer>().enabled=false;
                    Camera camera=Camera.main;camera.orthographic=false;camera.fieldOfView=80;
                    camera.transform.position=PlanPoint(491,289,SecondFloorY+1.6f,true);
                    camera.transform.LookAt(PlanPoint(419,205,SecondFloorY+1.25f,true));
                    RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/upper-large-"+Path.GetFileNameWithoutExtension(path)+".png"));
                    camera.transform.position=PlanPoint(385,180,SecondFloorY+1.6f,true);
                    camera.transform.LookAt(PlanPoint(508,253,SecondFloorY+1.2f,true));
                    RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/upper-large-storage-"+Path.GetFileNameWithoutExtension(path)+".png"));
                    Debug.Log("UPPER LARGE ROOM VALIDATED: "+path+"; eight tatami, two clear entrances, photo interior");
                }
                AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            }
            finally { if(!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
