using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        // Positions trace the AC rectangles in the supplied first/second floor plans.
        // Local +Z faces into the room; dimensions are provisional, in metres.
        private static void CreateAirConditioners(Transform house)
        {
            Transform first = house.Find("1階"), second = house.Find("2階");
            foreach (Transform floor in new[] { first, second })
            {
                Transform old = floor.Find("エアコン_天井近くの壁付け");
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            Transform lower = NewGroup("エアコン_天井近くの壁付け", first);
            Transform upper = NewGroup("エアコン_天井近くの壁付け", second);
            Material shell = WesternMaterial("AirConditionerWhite", new Color(.94f,.94f,.92f));
            Material vent = WesternMaterial("AirConditionerVent", new Color(.16f,.18f,.18f));
            Material led = WesternMaterial("AirConditionerIndicator", new Color(.25f,.65f,.35f));
            void Unit(Transform parent, string name, float x, float v, float yaw, bool upstairs = false)
            {
                Transform unit = NewGroup(name, parent);
                CreateBox("室内機_幅800高さ280奥行220mm", new Vector3(0,0,.11f), new Vector3(.80f,.28f,.22f), shell, unit);
                CreateDecorationBox("前面パネル",new Vector3(0,.025f,.224f),new Vector3(.76f,.19f,.016f),shell,unit);
                CreateDecorationBox("吹出口",new Vector3(0,-.104f,.224f),new Vector3(.68f,.047f,.012f),vent,unit);
                CreateDecorationBox("風向ルーバー",new Vector3(0,-.119f,.238f),new Vector3(.67f,.012f,.045f),shell,unit);
                CreateDecorationBox("運転ランプ",new Vector3(.32f,-.062f,.235f),new Vector3(.018f,.008f,.004f),led,unit);
                unit.rotation = Quaternion.Euler(0,yaw,0);
                // Top at 2.45m, below the lowest room ceiling/rafters (~2.51m).
                unit.position = PlanPoint(x,v,(upstairs ? SecondFloorY : 0)+2.31f,upstairs);
            }
            Unit(lower,"洋室_北壁",227,291,180);
            Unit(lower,"和室西_西壁",310,287,90);
            Unit(lower,"東物入_東壁",915,308,-90);
            Unit(upper,"二階8畳_北壁",392,158,180,true);
            Unit(upper,"二階4.5畳_西壁",553,217,90,true);
            // Stand clear of plaster/wood wall finishes, not merely the wall centreline.
            foreach (Transform floorGroup in new[] { lower, upper })
                foreach (Transform unit in floorGroup)
                    unit.position += unit.forward * .11f;
        }

        [MenuItem("Virtual House/Update Air Conditioners A-C And Build")]
        public static void UpdateAirConditionersAndBuild()
        {
            foreach (string path in new[] { RenovatedScene, AlternativeScene, PlanCScene })
            {
                Materials.Clear();
                var scene = EditorSceneManager.OpenScene(path);
                Transform house = GameObject.Find("住宅概形_図面ベース").transform;
                CreateAirConditioners(house);
                Physics.SyncTransforms();
                int count = 0;
                foreach (string floorName in new[] { "1階", "2階" })
                {
                    Transform group = house.Find(floorName+"/エアコン_天井近くの壁付け");
                    float floorY = floorName == "2階" ? SecondFloorY : 0;
                    foreach (Transform unit in group)
                    {
                        Bounds b = unit.GetChild(0).GetComponent<Renderer>().bounds;
                        if (Mathf.Abs(b.min.y-floorY-2.17f)>.005f || Mathf.Abs(b.max.y-floorY-2.45f)>.005f)
                            throw new InvalidOperationException("AC ceiling clearance invalid: "+unit.name);
                        count++;
                    }
                }
                if(count != 5) throw new InvalidOperationException("Expected five air conditioners");
                EditorSceneManager.SaveScene(scene);
                foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) label.GetComponent<Renderer>().enabled=false;
                Camera camera = Camera.main;
                foreach (string floorName in new[] { "1階", "2階" })
                    foreach (Transform unit in house.Find(floorName+"/エアコン_天井近くの壁付け"))
                    {
                        camera.transform.position = unit.position + unit.forward*2f + Vector3.down*.55f;
                        camera.transform.LookAt(unit.position+unit.forward*.12f);
                        camera.fieldOfView=55;
                        RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/ac-"+Path.GetFileNameWithoutExtension(path)+"-"+unit.name+".png"));
                    }
                Debug.Log("AIR CONDITIONERS VALIDATED: five wall units, top 2.45m: "+path);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WebBuild.BuildWebGL();
        }
    }
}
