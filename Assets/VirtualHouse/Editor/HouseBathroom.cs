using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        private static void CreateBathroomInterior(Transform first)
        {
            Transform room=NewGroup("浴室と脱衣所_写真ベース",first);
            Material tile=WesternMaterial("BathBlueTile",new Color(.20f,.29f,.33f));
            Material grout=WesternMaterial("BathGrout",new Color(.64f,.63f,.56f));
            Material tub=WesternMaterial("BathPaleGreen",new Color(.57f,.64f,.51f));
            Material white=WesternMaterial("WasherIvory",new Color(.82f,.81f,.72f));
            Material dark=WesternMaterial("BathDark",new Color(.17f,.20f,.20f));
            Material metal=GetMaterial("KitchenSteel");
            Vector3 sw=PlanPoint(726,254,0), ne=PlanPoint(802,179,0);
            float l=sw.x+.085f,r=ne.x-.085f,s=sw.z,n=ne.z-.085f;
            void TiledWall(Vector3 a,Vector3 b)
            {
                bool side=Mathf.Abs(a.x-b.x)<.01f;
                float length=Vector3.Distance(a,b);
                CreateDecorationBox("腰壁目地",(a+b)/2+Vector3.up*.52f,side?new Vector3(.025f,1.04f,length):new Vector3(length,1.04f,.025f),grout,room);
                int cols=Mathf.CeilToInt(length/.065f);
                for(int col=0;col<cols;col++) for(int row=0;row<12;row++)
                {
                    Vector3 p=Vector3.Lerp(a,b,(col+.5f)/cols)+Vector3.up*((row+.5f)*1.04f/12);
                    CreateDecorationBox("青灰タイル",p,side?new Vector3(.032f,1.04f/12-.004f,length/cols-.004f):new Vector3(length/cols-.004f,1.04f/12-.004f,.032f),tile,room);
                }
            }
            TiledWall(new Vector3(l,0,s),new Vector3(l,0,n));
            TiledWall(new Vector3(r,0,s),new Vector3(r,0,n));
            TiledWall(new Vector3(l,0,n),new Vector3(r,0,n));
            TiledWall(PlanPoint(764,251,0),new Vector3(r,0,s+.073f));
            CreateDecorationBox("浴室床目地",new Vector3((l+r)/2,.012f,(s+n)/2),new Vector3(r-l,.022f,n-s),grout,room);
            CreatePebbleFloor(room,l,r,s,n,dark);
            // Compact deep bathtub against the northeast corner, with an open interior.
            float tx=r-.45f,tz=n-.56f;
            Transform bath=NewGroup("小さな浴槽",room);
            CreateBox("浴槽底",new Vector3(tx,.13f,tz),new Vector3(.85f,.15f,1.02f),tub,bath);
            foreach(int sign in new[]{-1,1})
            {
                CreateBox("浴槽側壁",new Vector3(tx+sign*.385f,.38f,tz),new Vector3(.08f,.56f,1.02f),tub,bath);
                CreateBox("浴槽端壁",new Vector3(tx,.38f,tz+sign*.47f),new Vector3(.77f,.56f,.08f),tub,bath);
                CreateDecorationBox("浴槽縁",new Vector3(tx+sign*.385f,.67f,tz),new Vector3(.10f,.04f,1.04f),tub,bath);
                CreateDecorationBox("浴槽端縁",new Vector3(tx,.67f,tz+sign*.47f),new Vector3(.77f,.04f,.10f),tub,bath);
            }
            CreateDecorationBox("浴槽排水口",new Vector3(tx,.208f,tz-.30f),new Vector3(.065f,.005f,.065f),dark,bath);
            CreateDecorationBox("洗い場排水口",new Vector3(tx-.55f,.03f,tz-.59f),new Vector3(.13f,.01f,.13f),metal,room);
            for(int i=0;i<5;i++) CreateDecorationBox("排水溝",new Vector3(tx-.55f,.037f,tz-.63f+i*.02f),new Vector3(.10f,.004f,.008f),dark,room);
            KitchenRod(room,"混合水栓",new Vector3(r-.065f,.83f,tz-.30f),new Vector3(r-.065f,.83f,tz+.01f),.05f,metal);
            KitchenRod(room,"吐水口",new Vector3(r-.065f,.83f,tz-.17f),new Vector3(r-.24f,.78f,tz-.17f),.03f,metal);
            for(int i=0;i<16;i++)
            {
                float t=i/16f,u=(i+1)/16f;
                Vector3 Hose(float v)=>new Vector3(r-.07f,.85f-.67f*Mathf.Sin(Mathf.PI*v),tz-.30f-.54f*v);
                KitchenRod(room,"シャワーホース",Hose(t),Hose(u),.018f,dark);
            }
            KitchenRod(room,"シャワーヘッド",new Vector3(r-.07f,.84f,tz-.84f),new Vector3(r-.12f,1.02f,tz-.84f),.055f,white);
            CreateDecorationBox("窓台",PlanPoint(764,181,1.055f),new Vector3(60*PlanScale,.05f,.25f),grout,room);
            CreateOpenCurtains(room,PlanPoint(764,184,1.56f),60*PlanScale,.94f,false);
            // This bathroom has cream curtains rather than the orange living-room fabric.
            Transform curtains=room.Find("カーテン_左右に開いた状態");
            foreach(Renderer renderer in curtains.GetComponentsInChildren<Renderer>())
                if(renderer.name.StartsWith("カーテンひだ")) renderer.sharedMaterial=white;
            CreateWarmLight(room,"浴室照明",PlanPoint(757,228,2.4f),3f,.6f);
            CreateDecorationBox("浴室天井",new Vector3((l+r)/2,2.65f,(s+n)/2),new Vector3(r-l,.04f,n-s),GetMaterial("Wall"),room);
            CreateVintageWasherPrefab();
            CreateDecorationBox("脱衣所木床",PlanPoint(726,272.5f,.014f),new Vector3(76*PlanScale,.025f,37*PlanScale),GetMaterial("FloorWood"),room);
        }

        private const string VintageWasherPath = "Assets/VirtualHouse/Prefabs/VintageWasher.prefab";

        private static void CreateVintageWasherPrefab()
        {
            Material white=WesternMaterial("WasherIvory",new Color(.82f,.81f,.72f));
            Material dark=WesternMaterial("BathDark",new Color(.17f,.20f,.20f));
            Material metal=GetMaterial("KitchenSteel");
            Vector3 wp=Vector3.zero;
            Transform washer=new GameObject("古い洗濯機").transform;
            CreateBox("洗濯機本体",wp+Vector3.up*.43f,new Vector3(.64f,.80f,.53f),white,washer);
            CreateBox("操作パネル",wp+new Vector3(-.265f,.91f,0),new Vector3(.075f,.18f,.53f),dark,washer);
            for(int i=0;i<3;i++)
                KitchenRod(washer,"操作ダイヤル",wp+new Vector3(-.215f,.92f,-.17f+i*.17f),wp+new Vector3(-.195f,.92f,-.17f+i*.17f),.072f,white);
            foreach(int sign in new[]{-1,1})
                CreateDecorationBox("上蓋",wp+new Vector3(.035f,.845f,sign*.125f),new Vector3(.48f,.025f,.24f),white,washer);
            KitchenRod(washer,"前面取手",wp+new Vector3(.329f,.73f,-.18f),wp+new Vector3(.329f,.73f,.18f),.017f,metal);
            foreach(int x in new[]{-1,1}) foreach(int z in new[]{-1,1}) CreateBox("洗濯機脚",wp+new Vector3(x*.25f,.035f,z*.20f),new Vector3(.065f,.07f,.065f),white,washer);
            KitchenRod(washer,"排水ホース",wp+new Vector3(-.15f,.7f,.29f),wp+new Vector3(-.10f,.08f,.29f),.03f,dark);
            Directory.CreateDirectory("Assets/VirtualHouse/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(washer.gameObject,VintageWasherPath);
            Object.DestroyImmediate(washer.gameObject);
        }

        [MenuItem("Virtual House/Make Washer Prefab Only")]
        public static void MakeWasherPrefabOnly()
        {
            EditorSceneManager.OpenScene(OutputScene);
            CreateVintageWasherPrefab();
            foreach(Transform item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(item!=null && item.name=="古い洗濯機") Object.DestroyImmediate(item.gameObject);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(OutputScene);
            foreach(Transform item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(item.name=="古い洗濯機" || item.name=="洗濯機本体")
                    throw new System.InvalidOperationException("Washer remains in scene");
            GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(VintageWasherPath);
            if(prefab==null || prefab.transform.Find("洗濯機本体")==null || prefab.transform.position!=Vector3.zero)
                throw new System.InvalidOperationException("Washer prefab missing or not centered");
            Debug.Log("Washer prefab saved at origin; no washer remains in saved scene.");
            RenderBathroomPreview();
        }

        private static void CreatePebbleFloor(Transform parent,float l,float r,float s,float n,Material material)
        {
            // One persistent mesh for hundreds of flattened, irregular stones (no individual colliders).
            var verts=new List<Vector3>(); var tris=new List<int>();
            var random=new System.Random(442);
            for(float x=l+.035f;x<r-.03f;x+=.075f) for(float z=s+.035f;z<n-.03f;z+=.075f)
            {
                float cx=x+(float)(random.NextDouble()-.5)*.016f,cz=z+(float)(random.NextDouble()-.5)*.016f;
                float rx=.019f+(float)random.NextDouble()*.016f,rz=.019f+(float)random.NextDouble()*.016f;
                int start=verts.Count; verts.Add(new Vector3(cx,.026f,cz));
                for(int i=0;i<9;i++) {float a=i*2*Mathf.PI/8; verts.Add(new Vector3(cx+Mathf.Cos(a)*rx,.026f,cz+Mathf.Sin(a)*rz));}
                for(int i=0;i<8;i++) tris.AddRange(new[]{start,start+i+2,start+i+1});
            }
            const string path="Assets/VirtualHouse/GeneratedMeshes/BathPebbles.asset";
            Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
            mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            GameObject stones=new("石模様の床");stones.transform.SetParent(parent,false);
            stones.AddComponent<MeshFilter>().sharedMesh=mesh;stones.AddComponent<MeshRenderer>().sharedMaterial=material;
        }

        public static void RebuildBathroom()
        {
            Generate();
            ValidateServiceWalk();
            EditorSceneManager.OpenScene(OutputScene);
            CharacterController player=GameObject.Find("Player").GetComponent<CharacterController>();
            foreach(bool reverse in new[]{false,true})
            {
                player.enabled=false;player.transform.position=PlanPoint(745,reverse?226:274,.04f);player.enabled=true;Physics.SyncTransforms();
                Vector3 target=PlanPoint(745,reverse?274:226,0);float vy=0;
                for(int i=0;i<240;i++){if(player.isGrounded&&vy<0)vy=-2;vy-=20f/60;float dz=Mathf.Clamp(target.z-player.transform.position.z,-.035f,.035f);player.Move(new Vector3(0,vy/60,dz));}
                if(Mathf.Abs(player.transform.position.z-target.z)>.10f)throw new System.InvalidOperationException("Bathroom doorway walk blocked");
            }
            Debug.Log("Bathroom doorway walk passed in both directions.");
            RenderBathroomPreview();
        }

        [MenuItem("Virtual House/Render Bathroom Preview")]
        public static void RenderBathroomPreview()
        {
            EditorSceneManager.OpenScene(OutputScene);
            Camera camera=Camera.main;
            foreach(TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))label.GetComponent<Renderer>().enabled=false;
            for(int i=0;i<2;i++)
            {
                camera.transform.position=i==0?PlanPoint(738,249,1.65f):PlanPoint(754,257,1.40f);
                camera.transform.LookAt(i==0?PlanPoint(775,197,.65f):PlanPoint(705,274,.55f));camera.fieldOfView=75;
                RenderTexture previous=RenderTexture.active;RenderTexture target=new(1100,900,24);Texture2D image=new(1100,900,TextureFormat.RGB24,false);
                camera.targetTexture=target;RenderTexture.active=target;camera.Render();image.ReadPixels(new Rect(0,0,1100,900),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"VirtualHouse/"+(i==0?"bathroom":"washer")+"-preview.png"),image.EncodeToPNG());
                camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(image);Object.DestroyImmediate(target);
            }
            AssetDatabase.Refresh();
        }
    }
}
