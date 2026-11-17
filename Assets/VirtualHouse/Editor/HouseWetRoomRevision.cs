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
        private static void ApplyWetRoomRevision(Transform house, bool northLaundry)
        {
            Transform first=house.Find("1階"), fittings=first.Find("リフォーム後_設備");
            Transform old=first.Find("水回り_写真仕上げ");
            if(old!=null) Object.DestroyImmediate(old.gameObject);
            Transform group=NewGroup("水回り_写真仕上げ",first);
            Material white=WesternMaterial("WetRoomWarmWhite",new Color(.88f,.87f,.82f));
            Material grey=WesternMaterial("BathGreyFloor",new Color(.25f,.25f,.23f));
            Material green=WesternMaterial("BathMintInterior",new Color(.62f,.73f,.65f));
            Material dark=GetMaterial("BathDark"), steel=GetMaterial("KitchenSteel");
            Material wood=WesternMaterial("LaundryCounterWood",new Color(.59f,.43f,.27f));

            // Keep both original toilet pieces but rotate their absolute offsets, so updates are repeatable.
            Vector3 toilet=PlanPoint(666,155,.24f);
            Transform bowl=fittings.Find("便器"), tank=fittings.Find("トイレタンク");
            bowl.position=toilet; bowl.rotation=Quaternion.Euler(0,90,0);
            tank.position=toilet+new Vector3(8*PlanScale,.40f,0);
            tank.rotation=Quaternion.Euler(0,90,0);

            Transform trim=house.Find("外観_写真ベース/腰壁と窓");
            Transform window=trim.Find("浴室小窓");
            if(window!=null) Object.DestroyImmediate(window.gameObject);
            foreach(Transform hood in trim.Cast<Transform>().ToArray())
                if(hood.name=="窓庇" && Mathf.Abs(hood.position.x-PlanPoint(764,174,0).x)<.01f && Mathf.Abs(hood.position.y-2.17f)<.01f)
                    Object.DestroyImmediate(hood.gameObject);
            // Restore the actual north wall and its collider inside the former opening.
            CreateBox("壁_浴室旧窓を閉鎖",PlanPoint(764,179,1.555f),new Vector3(60*PlanScale,.95f,WallThickness),GetMaterial("Wall"),group);
            CreateDecorationBox("浴室旧窓_外壁仕上げ",PlanPoint(764,175,1.555f),new Vector3(60*PlanScale,.95f,.05f),GetMaterial("Wall"),group);
            foreach(Transform item in fittings.Cast<Transform>().ToArray())
                if(item.name.StartsWith("浴槽") || item.name.StartsWith("シャワー")) Object.DestroyImmediate(item.gameObject);
            Vector3 sw=PlanPoint(746,254,0), ne=PlanPoint(840,179,0);
            float l=sw.x+.085f,r=ne.x-.085f,s=sw.z+.085f,n=ne.z-.085f;
            CreateBox("浴室_チャコール床",new Vector3((l+r)/2,.018f,(s+n)/2),new Vector3(r-l,.035f,n-s),grey,group);
            // Horizontal bands approximate the warm stone/wood-grain panels in the reference.
            for(int row=0;row<22;row++)
            {
                float y=(row+.5f)*2.64f/22;
                Material panel=WesternMaterial("BathBeigePanel"+(row%4),new Color(.60f+(row%4)*.013f,.53f+(row%4)*.012f,.44f+(row%4)*.010f));
                foreach(float z in new[]{s,n}) CreateDecorationBox("ベージュ横柄パネル",new Vector3((l+r)/2,y,z),new Vector3(r-l,2.64f/22,.022f),panel,group);
                CreateDecorationBox("ベージュ横柄パネル",new Vector3(r,y,(s+n)/2),new Vector3(.022f,2.64f/22,n-s),panel,group);
            }
            CreateDecorationBox("浴室_白い天井",new Vector3((l+r)/2,2.67f,(s+n)/2),new Vector3(r-l,.04f,n-s),white,group);
            Vector3 tub=PlanPoint(816,213,0);
            CreateBox("浴槽_ミント色の内底",tub+Vector3.up*.18f,new Vector3(.80f,.14f,1.52f),green,group);
            foreach(int sign in new[]{-1,1})
            {
                CreateBox("浴槽_白い外側面",tub+new Vector3(sign*.405f,.36f,0),new Vector3(.09f,.56f,1.64f),white,group);
                CreateBox("浴槽_ミント色の内側面",tub+new Vector3(sign*.352f,.40f,0),new Vector3(.018f,.42f,1.46f),green,group);
                CreateBox("浴槽_白い端面",tub+new Vector3(0,.36f,sign*.775f),new Vector3(.90f,.56f,.09f),white,group);
                CreateDecorationBox("浴槽_ミント色の内端面",tub+new Vector3(0,.40f,sign*.721f),new Vector3(.71f,.42f,.018f),green,group);
                CreateDecorationBox("浴槽_淡い縁",tub+new Vector3(sign*.405f,.65f,0),new Vector3(.10f,.025f,1.65f),green,group);
                CreateDecorationBox("浴槽_淡い端縁",tub+new Vector3(0,.65f,sign*.775f),new Vector3(.82f,.025f,.10f),green,group);
            }
            KitchenRod(group,"シャワー縦管",PlanPoint(782,184,.85f),PlanPoint(782,184,1.95f),.025f,steel);
            CreateDecorationBox("シャワーヘッド",PlanPoint(782,189,1.95f),new Vector3(.14f,.04f,.18f),steel,group);
            CreateDecorationBox("浴室混合水栓",PlanPoint(782,185,.80f),new Vector3(.30f,.05f,.09f),steel,group);
            CreateDecorationBox("浴室換気扇",PlanPoint(787,220,2.63f),new Vector3(.34f,.045f,.34f),white,group);
            CreateDecorationBox("床排水口",PlanPoint(785,239,.04f),new Vector3(.15f,.012f,.15f),steel,group);

            if(!northLaundry) return;
            // The drawing leaves the bathroom's western edge open below the stacked appliances.
            Transform partition=first.Find("壁/壁_746_216_746_254");
            if(partition!=null) Object.DestroyImmediate(partition.gameObject);
            foreach(string name in new[]{"洗濯機","洗濯機上蓋"})
            {
                Transform item=fittings.Find(name);
                if(item!=null) Object.DestroyImmediate(item.gameObject);
            }
            Vector3 wp=PlanPoint(728,195,0);
            Transform laundry=NewGroup("北側_洗濯機と上置き乾燥機",group);
            CreateBox("洗濯機",wp+Vector3.up*.47f,new Vector3(.64f,.90f,.64f),white,laundry);
            CreateDecorationBox("洗濯槽の上蓋",wp+new Vector3(0,.932f,0),new Vector3(.51f,.024f,.42f),dark,laundry);
            CreateDecorationBox("洗濯機操作パネル",wp+new Vector3(0,.96f,.245f),new Vector3(.55f,.09f,.11f),dark,laundry);
            for(int i=0;i<4;i++) CreateDecorationBox("洗濯機ボタン",wp+new Vector3(-.16f+i*.10f,1.008f,.245f),new Vector3(.045f,.008f,.025f),white,laundry);
            foreach(int sign in new[]{-1,1})
                CreateBox("乾燥機ラック支柱",wp+new Vector3(sign*.35f,.75f,.23f),new Vector3(.035f,1.50f,.035f),steel,laundry);
            CreateBox("乾燥機ラック棚",wp+new Vector3(0,1.47f,0),new Vector3(.75f,.045f,.65f),steel,laundry);
            CreateBox("上置き乾燥機",wp+new Vector3(0,1.79f,0),new Vector3(.64f,.61f,.57f),white,laundry);
            KitchenRod(laundry,"乾燥機_丸い扉枠",wp+new Vector3(0,1.85f,-.29f),wp+new Vector3(0,1.85f,-.32f),.43f,steel);
            KitchenRod(laundry,"乾燥機_暗いガラス扉",wp+new Vector3(0,1.85f,-.325f),wp+new Vector3(0,1.85f,-.33f),.34f,dark);
            CreateDecorationBox("乾燥機操作パネル",wp+new Vector3(0,1.55f,-.292f),new Vector3(.49f,.08f,.015f),white,laundry);
            KitchenRod(laundry,"乾燥機ダイヤル",wp+new Vector3(.17f,1.55f,-.31f),wp+new Vector3(.17f,1.55f,-.33f),.045f,steel);

            // Front aligns with the bathroom doorway x=746. Keep the rear and sides
            // within the 18 x 37 plan-pixel recess, allowing for the surrounding walls.
            Vector3 cp=PlanPoint(746,272.5f,0)+Vector3.right*.18f;
            Transform cabinet=NewGroup("旧洗濯機位置_木天板と引き出し収納",group);
            CreateBox("収納本体",cp+Vector3.up*.43f,new Vector3(.68f,.84f,.33f),white,cabinet);
            CreateBox("木製カウンター天板",cp+Vector3.up*.88f,new Vector3(.72f,.045f,.36f),wood,cabinet);
            for(int col=0;col<2;col++) for(int row=0;row<4;row++)
            {
                Vector3 p=cp+new Vector3((col-.5f)*.335f,.12f+row*.205f,.163f);
                CreateDecorationBox("白い引き出し",p,new Vector3(.327f,.195f,.016f),white,cabinet);
                CreateDecorationBox("引き出し上縁の取手",p+new Vector3(0,.078f,.006f),new Vector3(.28f,.018f,.008f),white,cabinet);
            }
            Vector3 outlet=cp+new Vector3(0,1.16f,-.165f);
            CreateDecorationBox("カウンター上コンセント",outlet,new Vector3(.13f,.075f,.018f),white,cabinet);
            foreach(float x in new[]{-.035f,.035f}) foreach(float dx in new[]{-.009f,.009f})
                CreateDecorationBox("コンセント差込口",outlet+new Vector3(x+dx,0,.011f),new Vector3(.004f,.023f,.004f),dark,cabinet);
            // Access the drawers from the dressing-room aisle to the west, with the outlet on the east wall.
            Quaternion facing=Quaternion.Euler(0,-90,0);
            foreach(Transform part in cabinet)
            {
                part.position=cp+facing*(part.position-cp);
                part.rotation=facing*part.rotation;
            }
        }

        [MenuItem("Virtual House/Update Wet Rooms A-C And Build")]
        public static void UpdateWetRoomsAndBuild()
        {
            foreach(string path in new[]{RenovatedScene,AlternativeScene,PlanCScene})
            {
                Materials.Clear();
                var scene=EditorSceneManager.OpenScene(path);
                Transform house=scene.GetRootGameObjects().First(g=>g.name=="住宅概形_図面ベース").transform;
                bool north=path!=RenovatedScene;
                Transform player=GameObject.Find("Player").transform;
                Vector3 savedPlayerPosition=player.position;
                Quaternion savedPlayerRotation=player.rotation;
                ApplyWetRoomRevision(house,north);
                Physics.SyncTransforms();
                if(house.Find("外観_写真ベース/腰壁と窓/浴室小窓")!=null) throw new InvalidOperationException("Bath window remains");
                if(!Physics.Raycast(PlanPoint(764,193,1.5f),Vector3.forward,.5f)) throw new InvalidOperationException("Bath window wall not closed");
                Transform tank=house.Find("1階/リフォーム後_設備/トイレタンク");
                if(tank.position.x<=PlanPoint(666,155,0).x) throw new InvalidOperationException("Toilet must face west");
                if(north)
                {
                    ValidateAlternativeRoute(new[]{PlanPoint(670,235,0),PlanPoint(710,235,0),PlanPoint(770,222,0)});
                    ValidateAlternativeRoute(new[]{PlanPoint(710,235,0),PlanPoint(710,274,0)});
                }
                // Validation moves the player; reload the saved pose before saving the new fixtures.
                player.position=savedPlayerPosition;
                player.rotation=savedPlayerRotation;
                EditorSceneManager.SaveScene(scene);
                foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) label.GetComponent<Renderer>().enabled=false;
                Camera camera=Camera.main;
                camera.transform.position=PlanPoint(754,248,1.65f); camera.transform.LookAt(PlanPoint(813,195,1.25f)); camera.fieldOfView=85;
                RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/bath-finish-"+Path.GetFileNameWithoutExtension(path)+".png"));
                if(north)
                {
                    camera.transform.position=PlanPoint(728,252,1.4f); camera.transform.LookAt(PlanPoint(728,194,1.08f)); camera.fieldOfView=80;
                    RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/north-laundry-"+Path.GetFileNameWithoutExtension(path)+".png"));
                    camera.transform.position=PlanPoint(710,274,1.4f); camera.transform.LookAt(PlanPoint(746,274,.65f)); camera.fieldOfView=75;
                    RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/laundry-cabinet-"+Path.GetFileNameWithoutExtension(path)+".png"));
                }
                Debug.Log("WET ROOM REVISION VALIDATED: "+path);
            }
            // Exercise the runtime tennis geometry in the editor without saving it to the scene.
            var tennisType=typeof(VirtualHouse.HouseTennisGuide);
            tennisType.GetMethod("OnSceneLoaded",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)
                .Invoke(null,new object[]{UnityEngine.SceneManagement.SceneManager.GetActiveScene(),UnityEngine.SceneManagement.LoadSceneMode.Single});
            Transform court=GameObject.Find("北西_テニスコート案内").transform;
            if(court.Cast<Transform>().Count(t=>t.name.Contains("ライン"))!=9) throw new InvalidOperationException("Tennis court lines missing");
            Vector3 center=court.Find("テニス案内エリア").position;
            Camera.main.transform.position=center+new Vector3(2.7f,4.5f,-3.7f);
            Camera.main.transform.LookAt(center); Camera.main.fieldOfView=50;
            RenderCameraToPng(Camera.main,Path.Combine(Application.dataPath,"VirtualHouse/tennis-court-lines-preview.png"));
            Object.DestroyImmediate(court.gameObject);
            Debug.Log("TENNIS COURT LINES VALIDATED: doubles, singles, baselines, service boxes and net reference");
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            WebBuild.BuildWebGL();
        }
    }
}
