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
        private static void CreateKitchenPower(Transform first)
        {
            Transform old=first.Find("キッチン_コンセントと電源タップ");
            if(old!=null) Object.DestroyImmediate(old.gameObject);
            if(first.Find("キッチン_IKEA共有プラン")==null) return;
            Transform group=NewGroup("キッチン_コンセントと電源タップ",first);
            Material white=WesternMaterial("KitchenPowerIvory",new Color(.92f,.91f,.86f));
            Material dark=GetMaterial("OfficeCharcoal");
            Transform Outlet(string name,Vector3 p,Quaternion rotation)
            {
                Transform plate=NewGroup(name,group);
                CreateDecorationBox("2口コンセントプレート",Vector3.zero,new Vector3(.075f,.12f,.012f),white,plate);
                foreach(float y in new[]{-.027f,.027f})
                {
                    CreateDecorationBox("差込口",new Vector3(0,y,.007f),new Vector3(.043f,.033f,.004f),white,plate);
                    foreach(float x in new[]{-.008f,.008f})
                        CreateDecorationBox("差込スリット",new Vector3(x,y,.010f),new Vector3(.0035f,.016f,.002f),dark,plate);
                }
                plate.rotation=rotation;plate.position=p;
                return plate;
            }
            Bounds sill=first.Find("キッチン窓_くぼみと窓台/東窓/木製窓台_奥行300mm").GetComponent<Renderer>().bounds;
            Transform appliances=first.Find("キッチン窓_家電");
            Transform cooker=appliances.Find("SERIE_EPC19_260x285x283mm");
            Transform kettle=appliances.Find("EGL-C1280_285x180x225mm");
            // Reserve the south end of the sill for the strip; keep the 80mm appliance gap.
            cooker.position=new Vector3(sill.center.x,sill.max.y+.006f,sill.center.z-.16f);
            appliances.Find("圧力なべ敷板_280x290x6mm").position=new Vector3(sill.center.x,sill.max.y+.003f,cooker.position.z);
            kettle.position=new Vector3(sill.center.x,sill.max.y,cooker.position.z+.26f/2+.285f/2+.08f);
            Vector3 wall=PlanPoint(840,414,1.12f)+Vector3.left*(WallThickness/2+.008f);
            Transform outlet=Outlet("東出窓の南側_2口コンセント",wall,Quaternion.Euler(0,-90,0));
            Vector3 plug=outlet.TransformPoint(new Vector3(0,-.027f,.025f));
            CreateDecorationBox("電源タップの差込プラグ",plug,new Vector3(.035f,.035f,.04f),white,group);
            Transform strip=NewGroup("出窓南端_4口電源タップ",group);
            Vector3 sp=new Vector3(sill.center.x,sill.max.y+.022f,sill.min.z+.19f);
            CreateBox("電源タップ本体",sp,new Vector3(.065f,.036f,.28f),white,strip);
            foreach(float z in new[]{-.09f,-.03f,.03f,.09f})
            {
                CreateDecorationBox("タップ差込口",sp+new Vector3(0,.019f,z),new Vector3(.043f,.003f,.04f),white,strip);
                foreach(float x in new[]{-.008f,.008f})
                    CreateDecorationBox("タップ差込スリット",sp+new Vector3(x,.021f,z),new Vector3(.0035f,.002f,.017f),dark,strip);
            }
            Vector3[] cord={plug+Vector3.left*.02f,plug+new Vector3(-.06f,-.08f,0),
                new Vector3(wall.x-.07f,sill.max.y+.045f,wall.z+.06f),
                new Vector3(wall.x-.07f,sill.max.y+.045f,sill.min.z+.035f),
                new Vector3(sp.x-.04f,sp.y,sill.min.z+.035f),sp+new Vector3(0,0,-.145f)};
            for(int i=1;i<cord.Length;i++) KitchenRod(group,"コンセントからタップへの電源コード",cord[i-1],cord[i],.008f,white);

            Transform kitchen=first.Find("キッチン_IKEA共有プラン");
            Bounds compact=kitchen.Find(RelocatedCompactName+"/冷蔵庫本体").GetComponent<Renderer>().bounds;
            Outlet("北側小型冷蔵庫の上_2口コンセント",new Vector3(compact.center.x,compact.max.y+.22f,PlanPoint(812,254,0).z-WallThickness/2-.008f),Quaternion.Euler(0,180,0));
            Bounds large=kitchen.Find("既存冷蔵庫_幅630_奥行550_高さ推定1800/冷蔵庫本体").GetComponent<Renderer>().bounds;
            Outlet("大型冷蔵庫の上_東壁2口コンセント",new Vector3(PlanPoint(840,340,0).x-WallThickness/2-.008f,large.max.y+.22f,large.center.z),Quaternion.Euler(0,-90,0));
        }

        [MenuItem("Virtual House/Update Kitchen Power A-C And Build")]
        public static void UpdateKitchenPowerAndBuild()
        {
            foreach(string path in new[]{RenovatedScene,AlternativeScene,PlanCScene})
            {
                Materials.Clear();var scene=EditorSceneManager.OpenScene(path);
                Transform first=GameObject.Find("住宅概形_図面ベース").transform.Find("1階");
                CreateKitchenPower(first);Physics.SyncTransforms();
                if(path!=RenovatedScene)
                {
                    CreateLaundryWallOutlet(first);
                    CreateLaundryShelfMirror(first);
                    ShiftLaundryCounterOutletSouth(first);
                    Transform socket=first.Find("水回り_写真仕上げ/北側_洗濯機と上置き乾燥機/洗濯機と乾燥機の間_2口コンセント/2口プレート");
                    Bounds b=socket.GetComponent<Renderer>().bounds;
                    if(b.min.y<1.01f || b.max.y>1.44f) throw new InvalidOperationException("Laundry outlet must be between washer and dryer rack");
                    Debug.Log("LAUNDRY OUTLET AND SHELF MIRROR VALIDATED: "+path);
                }
                Transform power=first.Find("キッチン_コンセントと電源タップ");
                if(power.Cast<Transform>().Count(t=>t.name.Contains("2口コンセント"))!=3)
                    throw new InvalidOperationException("Expected three kitchen duplex outlets");
                Bounds strip=power.Find("出窓南端_4口電源タップ/電源タップ本体").GetComponent<Collider>().bounds;
                Bounds sill=first.Find("キッチン窓_くぼみと窓台/東窓/木製窓台_奥行300mm").GetComponent<Collider>().bounds;
                if(strip.min.x<sill.min.x || strip.max.x>sill.max.x || strip.min.z<sill.min.z || strip.max.z>sill.max.z || strip.min.y<sill.max.y)
                    throw new InvalidOperationException("Power strip must fit on the window sill");
                Transform appliances=first.Find("キッチン窓_家電");
                foreach(string name in new[]{"SERIE_EPC19_260x285x283mm","EGL-C1280_285x180x225mm"})
                {
                    Bounds body=appliances.Find(name).GetComponent<Collider>().bounds;
                    if(strip.Intersects(body) || body.min.x<sill.min.x || body.max.x>sill.max.x || body.min.z<sill.min.z || body.max.z>sill.max.z)
                        throw new InvalidOperationException("Appliance/strip overlap or unsupported appliance: "+name);
                }
                EditorSceneManager.SaveScene(scene);
                foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))label.GetComponent<Renderer>().enabled=false;
                Camera camera=Camera.main;
                camera.transform.position=PlanPoint(797,397,1.65f);camera.transform.LookAt(PlanPoint(844,392,1.02f));camera.fieldOfView=70;
                RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/kitchen-power-window-"+Path.GetFileNameWithoutExtension(path)+".png"));
                camera.transform.position=PlanPoint(785,365,2.05f);camera.transform.LookAt(PlanPoint(835,340,1.90f));camera.fieldOfView=65;
                RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/kitchen-power-fridge-"+Path.GetFileNameWithoutExtension(path)+".png"));
                camera.transform.position=PlanPoint(770,311,1.7f);camera.transform.LookAt(PlanPoint(778,255,1.35f));camera.fieldOfView=65;
                RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/kitchen-power-north-"+Path.GetFileNameWithoutExtension(path)+".png"));
                if(path!=RenovatedScene)
                {
                    camera.transform.position=PlanPoint(728,238,1.26f);camera.transform.LookAt(PlanPoint(728,179,1.22f));camera.fieldOfView=65;
                    RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/laundry-power-"+Path.GetFileNameWithoutExtension(path)+".png"));
                    camera.transform.position=PlanPoint(710,272.5f,1.4f);camera.transform.LookAt(PlanPoint(761,272.5f,1.25f));camera.fieldOfView=75;
                    RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/laundry-mirror-"+Path.GetFileNameWithoutExtension(path)+".png"));
                }
                Debug.Log("KITCHEN POWER VALIDATED: three duplex wall outlets, connected four-socket strip, appliances supported and clear: "+path);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            WebBuild.BuildWebGL();
        }
    }
}
