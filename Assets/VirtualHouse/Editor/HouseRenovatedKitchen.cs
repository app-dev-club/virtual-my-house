using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        // IKEA shared plan C95B8806-A083-417A-8C7F-7B4B93B2F75B, inspected 2026-10-07.
        // Printed plan origin: top-left inside corner. +x right, +depth down, in metres.
        // Window correspondence rotates the plan 180 degrees: top=south, left=east.
        // Preserve furniture dimensions; the drawn house has 3.548m clear width vs IKEA's 3.600m.
        private static Vector3 IkeaPoint(float x, float depth, float height)
            => PlanPoint(840,423,height) + new Vector3(-WallThickness/2-x,0,WallThickness/2+depth);

        private static void CreateRenovatedKitchen(Transform first)
        {
            RelocateOriginalSink(first);
            RemoveRequired(first,"キッチン_写真ベース");
            Transform kitchen=NewGroup("キッチン_IKEA共有プラン",first);
            Material bamboo=WesternMaterial("RenovationBamboo",new Color(.72f,.56f,.34f));
            Material white=WesternMaterial("RenovationWorktop",new Color(.94f,.94f,.91f));
            Material carcass=WesternMaterial("RenovationCabinetWhite",new Color(.84f,.84f,.80f));
            Material grain=WesternMaterial("RenovationBambooGrain",new Color(.66f,.50f,.29f));
            Material steel=GetMaterial("KitchenSteel"), basin=GetMaterial("KitchenBasin");
            GameObject Box(Transform parent,string name,float x,float depth,float height,float width,float tall,float deep,Material mat,bool collides=true)
            {
                var item=CreateBox(name,IkeaPoint(x,depth,height),new Vector3(width,tall,deep),mat,parent);
                if(!collides) Object.DestroyImmediate(item.GetComponent<Collider>());
                return item;
            }
            Transform Cabinet(string name,float left,float back,float width,bool sink=false,bool facesSouth=false)
            {
                Transform unit=NewGroup(name,kitchen);
                float center=left+width/2, front=back+(facesSouth?0:.60f);
                Box(unit,"キャビネット本体",center,back+.30f,sink?.37f:.481f,width,sink?.58f:.802f,.60f,carcass);
                Box(unit,"蹴込み板_高さ80mm",center,front+(facesSouth?.05f:-.05f),.04f,width,.08f,.03f,bamboo);
                float[] heights={.40f,.20f,.20f}; float bottom=.08f;
                foreach(float h in heights)
                {
                    float y=bottom+h/2;
                    Box(unit,"バンブー引出し前板",center,front,y,width-.004f,h-.004f,.02f,bamboo);
                    Box(unit,"掘込引手",center,front+(facesSouth?-.012f:.012f),bottom+h-.018f,.12f,.009f,.005f,grain,false);
                    // Fine vertical strips convey the bamboo grain without external textures.
                    for(int i=0;i<14;i++)
                        Box(unit,"竹目",left+(i+.5f)*width/14,front+(facesSouth?-.011f:.011f),y,.002f,h-.006f,.001f,grain,false);
                    bottom+=h;
                }
                Box(unit,"左側カバーパネル",left-.006f,back+.30f,.481f,.012f,.802f,.63f,bamboo);
                Box(unit,"右側カバーパネル",left+width+.006f,back+.30f,.481f,.012f,.802f,.63f,bamboo);
                return unit;
            }
            Cabinet("01_METOD_コンロ台600",.10f,0,.60f);
            Cabinet("02_METOD_シンク台800",1.312f,0,.80f,true);
            Cabinet("03_METOD_シンク台800",2.112f,0,.80f,true);
            // The 600mm open space between cooker and sinks is present in both plan and 3D.
            Box(kitchen,"白いワークトップ_コンロと空きスペース",.700f,.315f,.896f,1.224f,.028f,.63f,white);
            foreach(float left in new[]{1.312f,2.112f})
            {
                float center=left+.4f;
                Transform sink=NewGroup("LANGUDDEN_シンク560x530",kitchen);
                // Counter has a real hole; lower cabinet does not fill the recessed basin.
                foreach(int side in new[]{-1,1})
                {
                    Box(sink,"ワークトップ側部",center+side*.34f,.315f,.896f,.12f,.028f,.63f,white);
                    Box(sink,"ワークトップ前後部",center,.315f+side*.29f,.896f,.56f,.028f,.05f,white);
                    Box(sink,"シンク側壁",center+side*.26f,.315f,.80f,.02f,.19f,.49f,steel);
                    Box(sink,"シンク前後壁",center,.315f+side*.24f,.80f,.53f,.19f,.02f,steel);
                    Box(sink,"シンク縁側部",center+side*.265f,.315f,.914f,.03f,.014f,.53f,steel,false);
                    Box(sink,"シンク縁前後",center,.315f+side*.25f,.914f,.56f,.014f,.03f,steel,false);
                }
                Box(sink,"シンク底",center,.315f,.707f,.50f,.025f,.46f,basin);
                Box(sink,"排水口",center,.315f,.722f,.075f,.004f,.075f,steel,false);
                KitchenRod(sink,"混合栓立上り",IkeaPoint(center,.055f,.915f),IkeaPoint(center,.055f,1.18f),.032f,steel);
                KitchenRod(sink,"混合栓吐水管",IkeaPoint(center,.055f,1.18f),IkeaPoint(center,.24f,1.18f),.027f,steel);
                KitchenRod(sink,"吐水先端",IkeaPoint(center,.24f,1.18f),IkeaPoint(center,.24f,1.13f),.034f,steel);
            }
            // North-facing compact fridge door aligns with the south end of the storage opening.
            float northDoorDepth=(423f-329f)*PlanScale-WallThickness/2;
            float islandBack=northDoorDepth-.615f;
            Cabinet("07_対面収納_調整幅750",1.192f,islandBack,.75f,false,true);
            Cabinet("05_METOD_対面収納600",1.942f,islandBack,.60f,false,true);
            Box(kitchen,"対面ワークトップ_幅1374mm",1.867f,islandBack+.30f,.896f,1.374f,.028f,.63f,white);
            Box(kitchen,"対面収納背面パネル",1.867f,islandBack+.608f,.481f,1.374f,.802f,.016f,bamboo);
            // White 400 x 410 cabinet is rotated against the right wall in the source plan.
            float clearWidth=152*PlanScale-WallThickness;
            Transform small=NewGroup("04_METOD_側壁収納400x410",kitchen);
            Box(small,"白いキャビネット本体",clearWidth-.205f,.205f,.481f,.410f,.802f,.400f,carcass);
            Box(small,"白い天板",clearWidth-.205f,.205f,.896f,.410f,.028f,.400f,white);
            Box(small,"側板",clearWidth-.40f,.205f,.481f,.02f,.802f,.400f,white);
            // Existing unnumbered items shown in the shared plan, dimensions inferred from
            // the plan drawing. The white tall block has no specified product identity.
            Transform fridge=NewGroup("既存冷蔵庫_幅630_奥行550_高さ推定1800",kitchen);
            float fridgeDepth=northDoorDepth-.275f; // Large fridge back aligns with compact fridge door face.
            Box(fridge,"冷蔵庫本体",.315f,fridgeDepth,.90f,.630f,1.80f,.550f,steel);
            Box(fridge,"上段パネル_南向き",.315f,fridgeDepth-.282f,1.28f,.606f,1.00f,.018f,steel);
            Box(fridge,"下段パネル_南向き",.315f,fridgeDepth-.282f,.38f,.606f,.73f,.018f,steel);
            Box(fridge,"上段取手",.055f,fridgeDepth-.305f,1.20f,.022f,.48f,.026f,basin,false);
            Box(fridge,"下段取手",.315f,fridgeDepth-.305f,.70f,.46f,.022f,.026f,basin,false);
            float compactDepth=northDoorDepth-.291f;
            Transform compact=NewGroup("小型冷蔵庫_北向き_幅500_奥行550_高さ1200_仮寸法",kitchen);
            Box(compact,"冷蔵庫本体",.902f,compactDepth,.60f,.50f,1.20f,.55f,white);
            Box(compact,"ドア_北向き",.902f,compactDepth+.282f,.60f,.476f,1.17f,.018f,white);
            Box(compact,"取手",1.10f,compactDepth+.305f,.82f,.022f,.30f,.026f,steel,false);
            Box(kitchen,"既存白い縦長ブロック_プラン概形",clearWidth-.225f,.867f,1.20f,.450f,2.40f,.900f,white);
            Transform hood=NewGroup("08_ANPASSA_白いレンジフード600",kitchen);
            Box(hood,"フード下端",.45f,.28f,1.81f,.60f,.06f,.50f,white);
            CreateRenovationHoodSlope(hood,white);
            Box(hood,"白い煙突",.45f,.16f,2.15f,.30f,.50f,.28f,white,false);
            CreateWarmLight(kitchen,"キッチン照明",IkeaPoint(1.8f,1.25f,2.45f),5f,.8f);
            CreateWarmLight(kitchen,"対面収納照明",IkeaPoint(1.8f,2.9f,2.45f),4f,.5f);
        }

        private static void RelocateOriginalSink(Transform first)
        {
            Transform original=first.Find("キッチン_写真ベース");
            Transform reused=NewGroup("旧キッチン_再利用シンクと水切り",first);
            // Detachable unit to the right of the photographed seam, excluding the
            // separate preparation cabinet, cooker stand and wooden side table.
            reused.position=PlanPoint(745,423,0)+Vector3.forward*.40f;
            foreach(Transform part in original)
                if(part.name=="流し台" || part.name.StartsWith("シンク") ||
                   part.name.StartsWith("水切り") || part.name.StartsWith("蛇口"))
                {
                    GameObject copy=Object.Instantiate(part.gameObject,reused,true);
                    copy.name=part.name;
                }
            if(reused.Find("流し台")==null || reused.Find("シンク底")==null || reused.Find("水切り天板")==null)
                throw new InvalidOperationException("Original detachable sink unit is incomplete");
            // Under the bathroom's south wall, facing south into DK in both plans.
            reused.rotation=Quaternion.Euler(0,180,0);
            reused.position=PlanPoint(812,270,0);
            AddReusedPreparationCabinet(first);
        }

        // Photo IMG_20260930_111349922: the separate two-door preparation unit.
        // Match the existing photo-based estimate; do not shrink it to fit Plan A/C.
        private static bool AddReusedPreparationCabinet(Transform first)
        {
            const string name="旧キッチン_再利用調理台";
            if(first.Find(name)!=null) return true;
            Transform sink=first.Find("旧キッチン_再利用シンクと水切り");
            Vector3 center=sink.position+Vector3.left*(1.14f/2+.67f/2);
            // Clear the bathroom wall face; the old sink estimate overlaps it by 2mm.
            center.z=Mathf.Min(center.z,PlanPoint(812,254,0).z-WallThickness/2-.32f-.002f);
            Bounds footprint=new Bounds(center+Vector3.up*.44f,new Vector3(.67f,.88f,.64f));
            Physics.SyncTransforms();
            var blockingWalls=first.Find("壁").GetComponentsInChildren<Collider>().Where(c=>c.bounds.Intersects(footprint)).ToArray();
            if(blockingWalls.Length>0)
            {
                Debug.Log("Preparation cabinet skipped: insufficient space beside northeast sink: "+string.Join(", ",blockingWalls.Select(c=>c.name))+" at "+center);
                return false;
            }
            Transform unit=NewGroup(name,first);
            Material cream=GetMaterial("KitchenCream"),steel=GetMaterial("KitchenSteel");
            CreateBox("本体",center+Vector3.up*.41f,new Vector3(.67f,.82f,.60f),cream,unit);
            CreateBox("ステンレス天板",center+Vector3.up*.85f,new Vector3(.67f,.035f,.64f),steel,unit);
            CreateDecorationBox("蹴込み",center+new Vector3(0,.045f,-.305f),new Vector3(.67f,.09f,.025f),GetMaterial("DarkWood"),unit);
            foreach(int sign in new[]{-1,1})
            {
                CreateDecorationBox("扉",center+new Vector3(sign*.67f/4,.44f,-.315f),new Vector3(.321f,.70f,.022f),cream,unit);
                CreateDecorationBox("金属取手",center+new Vector3(sign*.67f/4,.75f,-.34f),new Vector3(.201f,.018f,.025f),steel,unit);
            }
            return true;
        }

        [MenuItem("Virtual House/Update And Validate Reused Preparation Cabinet")]
        public static void UpdateAndValidateReusedPreparationCabinet()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in new[]{RenovatedScene,AlternativeScene,PlanCScene})
                {
                    Materials.Clear();
                    var scene=EditorSceneManager.OpenScene(path);
                    Transform first=GameObject.Find("住宅概形_図面ベース").transform.Find("1階");
                    Physics.SyncTransforms();
                    bool placed=AddReusedPreparationCabinet(first);
                    if(placed!=(path==AlternativeScene)) throw new InvalidOperationException("Unexpected preparation cabinet fit: "+path);
                    if(placed) EditorSceneManager.SaveScene(scene);
                    Physics.SyncTransforms();
                    ValidateRenovatedKitchen();
                    if(placed)
                    {
                        ValidateAlternativeRoute(new[]{PlanPoint(710,310,0),PlanPoint(774,310,0),PlanPoint(774,298,0),PlanPoint(812,298,0)});
                        Camera camera=Camera.main;
                        camera.transform.position=PlanPoint(762,336,1.9f);
                        camera.transform.LookAt(PlanPoint(795,270,.65f));
                        camera.orthographic=false;camera.fieldOfView=65;
                        foreach(TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) label.GetComponent<Renderer>().enabled=false;
                        RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/reused-preparation-cabinet-preview.png"));
                        AssetDatabase.ImportAsset("Assets/VirtualHouse/reused-preparation-cabinet-preview.png");
                    }
                    Debug.Log("REUSED PREPARATION CABINET VALIDATED: "+path+" placed="+placed);
                }
                AssetDatabase.SaveAssets();
            }
            finally { if(setup.Length>0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }

        private static void CreateRenovationHoodSlope(Transform parent,Material white)
        {
            Vector3[] corners={
                IkeaPoint(.15f,.03f,1.84f),IkeaPoint(.75f,.03f,1.84f),
                IkeaPoint(.75f,.53f,1.84f),IkeaPoint(.15f,.53f,1.84f),
                IkeaPoint(.30f,.02f,2.00f),IkeaPoint(.60f,.02f,2.00f),
                IkeaPoint(.60f,.30f,2.00f),IkeaPoint(.30f,.30f,2.00f)};
            int[,] faces={{0,1,2,3},{4,5,6,7},{0,1,5,4},{1,2,6,5},{2,3,7,6},{3,0,4,7}};
            var vertices=new List<Vector3>();var triangles=new List<int>();
            Vector3 center=corners.Aggregate(Vector3.zero,(sum,p)=>sum+p)/8;
            for(int i=0;i<6;i++)
            {
                int start=vertices.Count;
                Vector3 a=corners[faces[i,0]],b=corners[faces[i,1]],c=corners[faces[i,2]],d=corners[faces[i,3]];
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),(a+b+c+d)/4-center)<0) (b,d)=(d,b);
                vertices.AddRange(new[]{a,b,c,d});
                triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            const string path="Assets/VirtualHouse/GeneratedMeshes/RenovationHood.asset";
            Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            GameObject slope=new("フード傾斜");slope.transform.SetParent(parent,false);
            slope.AddComponent<MeshFilter>().sharedMesh=mesh;
            slope.AddComponent<MeshRenderer>().sharedMaterial=white;
        }

        private static void ValidateRenovatedKitchen()
        {
            Transform kitchen=GameObject.Find("キッチン_IKEA共有プラン").transform;
            if(kitchen.Cast<Transform>().Count(t=>t.name.StartsWith("LANGUDDEN_"))!=2)
                throw new InvalidOperationException("Two independent IKEA sinks required");
            // Verify the working aisle and the route from the west hall past the island.
            CharacterController player=GameObject.Find("Player").GetComponent<CharacterController>();
            Vector3[] route={PlanPoint(670,328,0),IkeaPoint(3.08f,2.30f,0),IkeaPoint(3.08f,1.65f,0),IkeaPoint(2.80f,1.65f,0),IkeaPoint(2.80f,1.12f,0),IkeaPoint(1.8f,1.15f,0),IkeaPoint(.45f,.93f,0)};
            foreach(bool reverse in new[]{false,true})
            {
                player.enabled=false;player.transform.position=route[reverse?route.Length-1:0]+Vector3.up*.04f;player.enabled=true;
                Physics.SyncTransforms();float vy=0;
                for(int leg=1;leg<route.Length;leg++)
                {
                    Vector3 target=route[reverse?route.Length-1-leg:leg];
                    for(int frame=0;frame<240;frame++)
                    {
                        if(player.isGrounded && vy<0)vy=-2;vy-=20f/60;
                        Vector3 delta=target-player.transform.position;delta.y=0;
                        player.Move(Vector3.ClampMagnitude(delta,2.6f/60)+Vector3.up*(vy/60));
                    }
                    if(Vector3.Distance(player.transform.position,target)>.18f)
                        throw new InvalidOperationException("IKEA kitchen walk failed at leg "+leg+": "+player.transform.position);
                }
            }
            ValidateAlternativeRoute(new[] {IkeaPoint(3.08f,2.8f,0),IkeaPoint(1.8f,2.8f,0),PlanPoint(860,310,0)});
            Transform compact=kitchen.Find("小型冷蔵庫_北向き_幅500_奥行550_高さ1200_仮寸法");
            if(compact==null || Mathf.Abs(compact.Find("ドア_北向き").GetComponent<Renderer>().bounds.max.z-PlanPoint(840,329,0).z)>.002f)
                throw new InvalidOperationException("Compact fridge door must align with storage opening south end");
            Transform large=kitchen.Find("既存冷蔵庫_幅630_奥行550_高さ推定1800");
            if(Mathf.Abs(large.Find("冷蔵庫本体").GetComponent<Renderer>().bounds.max.z-compact.Find("ドア_北向き").GetComponent<Renderer>().bounds.max.z)>.002f)
                throw new InvalidOperationException("Large fridge back and compact fridge door must align");
            Transform reused=GameObject.Find("旧キッチン_再利用シンクと水切り").transform;
            if(reused.Find("流し台")==null || reused.Find("水切り天板")==null)
                throw new InvalidOperationException("Reused sink and drainer required in northeast DK");
            ValidateAlternativeRoute(new[] {PlanPoint(778,310,0),PlanPoint(812,310,0),PlanPoint(812,298,0)});
            Debug.Log("IKEA KITCHEN VALIDATION PASSED: two sinks, hall/island/working aisle walk in both directions.");
        }

        private static void RenderRenovatedKitchenPreviews()
        {
            Camera camera=Camera.main;
            foreach(TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))label.GetComponent<Renderer>().enabled=false;
            camera.orthographic=false;
            GameObject exterior=GameObject.Find("住宅概形_図面ベース").transform.Find("外観_写真ベース").gameObject;
            exterior.SetActive(true);
            camera.transform.position=IkeaPoint(1.70f,3.0f,2.25f);
            camera.transform.LookAt(IkeaPoint(1.75f,.60f,.95f));camera.fieldOfView=85;
            RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/renovated-kitchen-preview.png"));
            exterior.SetActive(false);
            camera.transform.position=IkeaPoint(1.8f,2f,12f);
            // Same orientation as the IKEA print, for direct comparison.
            camera.transform.LookAt(IkeaPoint(1.8f,2f,0),Vector3.back);
            camera.orthographic=true;camera.orthographicSize=2.4f;
            RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/renovated-kitchen-plan.png"));
        }
    }
}
