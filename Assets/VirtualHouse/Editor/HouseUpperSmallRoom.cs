using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        private static void CreateUpperSmallRoom(Transform second)
        {
            Transform room=NewGroup("二階4.5畳和室_写真ベース",second);
            Vector3 sw=PlanPoint(553,310,0,true),ne=PlanPoint(667,194,0,true);
            float l=sw.x,r=ne.x,s=sw.z,n=ne.z,cx=(l+r)/2,cz=(s+n)/2,y=SecondFloorY;
            Material sand=WesternMaterial("UpperSandWall",new Color(.66f,.65f,.55f));
            Material wood=GetMaterial("LightWood"),dark=GetMaterial("DarkWood");
            Transform mats=NewGroup("四畳と中央半畳",room);
            float ux=(r-l-.10f)/3,uz=(n-s-.10f)/3,x=l+.05f,z=s+.05f;
            CreateTatami(mats,"畳_南",x,z,ux*2,uz);
            CreateTatami(mats,"畳_東",x+ux*2,z,ux,uz*2);
            CreateTatami(mats,"畳_北",x+ux,z+uz*2,ux*2,uz);
            CreateTatami(mats,"畳_西",x,z+uz,ux,uz*2);
            CreateTatami(mats,"半畳_中央",x+ux,z+uz,ux,uz);
            mats.position=Vector3.up*y;
            foreach(float wallX in new[]{l+.08f,r-.08f})
            {
                CreateDecorationBox("砂壁",new Vector3(wallX,y+1.30f,cz),new Vector3(.025f,2.60f,n-s),sand,room);
                CreateDecorationBox("長押",new Vector3(wallX,y+1.92f,cz),new Vector3(.07f,.075f,n-s),wood,room);
                foreach(float wallZ in new[]{s+.06f,cz,n-.06f})
                    CreateDecorationBox("木柱",new Vector3(wallX,y+1.31f,wallZ),new Vector3(.075f,2.62f,.075f),wood,room);
            }
            foreach(float wallZ in new[]{s,n})
            {
                CreateDecorationBox("鴨居",new Vector3(cx,y+2.10f,wallZ),new Vector3(r-l,.12f,.09f),wood,room);
                CreateDecorationBox("上部砂壁",new Vector3(cx,y+2.39f,wallZ),new Vector3(r-l,.46f,.06f),sand,room);
            }
            // Four-leaf shoji, parked as two pairs to retain the corridor route and daylight.
            Transform shoji=NewGroup("南障子_中央を開放",room);
            float leaf=(r-l)/4;
            for(int i=0;i<4;i++)
            {
                CreateShojiPanelX(shoji,"障子_"+i,(i<2?l+leaf/2:r-leaf/2),s+(i%2)*.045f,leaf);
                float panelX=i<2?l+leaf/2:r-leaf/2,panelZ=s+(i%2)*.045f+.025f;
                for(int j=1;j<4;j++)CreateDecorationBox("縦格子_室内側",new Vector3(panelX-leaf/2+leaf*j/4,1.075f,panelZ),new Vector3(.018f,2.05f,.025f),dark,shoji);
                for(int j=1;j<6;j++)CreateDecorationBox("横框_室内側",new Vector3(panelX,.05f+2.05f*j/6,panelZ),new Vector3(leaf,.018f,.025f),dark,shoji);
            }
            shoji.position=new Vector3(0,y,.14f);
            float solidEnd=PlanPoint(591,310,0,true).x;
            CreateDecorationBox("南袖壁砂壁",new Vector3((l+solidEnd)/2,y+1.03f,s+.065f),new Vector3(solidEnd-l,2.06f,.025f),sand,room);
            // North entrance fusuma is parked over the existing western wall, never across the opening.
            float fx=PlanPoint(572,194,0,true).x,fw=36*PlanScale;
            CreateDecorationBox("入口襖_開いた状態",new Vector3(fx,y+1.02f,n-.025f),new Vector3(fw,2.04f,.035f),GetMaterial("Paper"),room);
            foreach(int sign in new[]{-1,1})CreateDecorationBox("襖黒縁",new Vector3(fx+sign*fw/2,y+1.02f,n-.045f),new Vector3(.035f,2.08f,.045f),dark,room);
            CreateDecorationBox("襖裾模様",new Vector3(fx,y+.42f,n-.05f),new Vector3(fw,.14f,.012f),GetMaterial("SandWall"),room);
            CreateDecorationBox("襖引手",new Vector3(fx+.28f,y+.90f,n-.055f),new Vector3(.045f,.065f,.02f),dark,room);
            // Small wooden cupboard seen beside the shoji; depth and orientation are estimated.
            float cupboardZ=s+.40f;
            for(int i=0;i<2;i++)
            {
                CreateDecorationBox("木製収納扉",new Vector3(l+.105f,y+1.18f,cupboardZ+(i==0?-.18f:.18f)),new Vector3(.045f,1.55f,.35f),wood,room);
                CreateDecorationBox("収納取手",new Vector3(l+.14f,y+1.02f,cupboardZ+(i==0?-.04f:.04f)),new Vector3(.02f,.17f,.018f),dark,room);
            }
            CreateDecorationBox("棹縁天井",new Vector3(cx,y+2.60f,cz),new Vector3(r-l,.05f,n-s),wood,room);
            for(int i=0;i<6;i++)CreateDecorationBox("天井竿縁_"+i,new Vector3(cx,y+2.56f,s+(n-s)*(i+.5f)/6),new Vector3(r-l,.045f,.035f),dark,room);
            KitchenRod(room,"吊り照明コード",new Vector3(cx,y+2.58f,cz),new Vector3(cx,y+2.23f,cz),.015f,dark);
            GameObject shade=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shade.name="丸い乳白色シェード";shade.transform.SetParent(room,false);
            shade.transform.position=new Vector3(cx,y+2.15f,cz);shade.transform.localScale=new Vector3(.43f,.11f,.43f);
            shade.GetComponent<Renderer>().sharedMaterial=GetMaterial("Paper");Object.DestroyImmediate(shade.GetComponent<Collider>());
            GameObject lamp=new("和室丸型灯");lamp.transform.SetParent(room,false);lamp.transform.position=new Vector3(cx,y+1.99f,cz);
            Light light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=3.4f;light.intensity=.7f;light.color=new Color(1f,.85f,.65f);
            CreateKeyboardFurniturePrefab();
        }

        private static void CreateKeyboardFurniturePrefab()
        {
            // Instrument identification is uncertain: reproduce the visible closed wooden case.
            GameObject root=new("VintageKeyboardClosed");
            CreateBox("木製ケース",new Vector3(0,.72f,0),new Vector3(1.02f,.17f,.43f),GetMaterial("LightWood"),root.transform);
            CreateBox("閉じた濃色天板",new Vector3(0,.825f,0),new Vector3(1.05f,.045f,.45f),GetMaterial("DarkWood"),root.transform);
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
                KitchenRod(root.transform,"金属脚",new Vector3(x*.40f,.64f,z*.15f),new Vector3(x*.47f,.04f,z*.20f),.025f,GetMaterial("WindowFrame"));
            const string path="Assets/VirtualHouse/Prefabs/VintageKeyboardClosed.prefab";
            PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
        }

        [MenuItem("Virtual House/Rebuild Upper 4.5 Tatami Room")]
        public static void RebuildUpperSmallRoom()
        {
            Generate();ValidateStairWalk();
            EditorSceneManager.OpenScene(OutputScene);
            if(GameObject.Find("VintageKeyboardClosed")!=null || AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VirtualHouse/Prefabs/VintageKeyboardClosed.prefab")==null)
                throw new System.InvalidOperationException("Instrument must exist only as a prefab.");
            CharacterController player=GameObject.Find("Player").GetComponent<CharacterController>();
            foreach(bool reverse in new[]{false,true})
            {
                player.enabled=false;player.transform.position=PlanPoint(630,reverse?330:180,SecondFloorY+.04f,true);player.enabled=true;Physics.SyncTransforms();
                Vector3 target=PlanPoint(630,reverse?180:330,SecondFloorY,true);float vy=0;
                for(int i=0;i<360;i++){if(player.isGrounded&&vy<0)vy=-2;vy-=20f/60;player.Move(new Vector3(0,vy/60,Mathf.Clamp(target.z-player.transform.position.z,-.035f,.035f)));}
                if(Mathf.Abs(player.transform.position.z-target.z)>.10f)throw new System.InvalidOperationException("Upper room passage blocked");
            }
            Debug.Log("Upper small room: corridor walk passed; instrument prefab present and unplaced.");
            EditorSceneManager.OpenScene(OutputScene);
            Camera camera=Camera.main;camera.transform.position=PlanPoint(644,207,SecondFloorY+1.55f,true);camera.transform.LookAt(PlanPoint(591,290,SecondFloorY+1.15f,true));camera.fieldOfView=78;
            foreach(TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))label.GetComponent<Renderer>().enabled=false;
            RenderTexture previous=RenderTexture.active;RenderTexture targetImage=new(1200,900,24);Texture2D image=new(1200,900,TextureFormat.RGB24,false);
            camera.targetTexture=targetImage;RenderTexture.active=targetImage;camera.Render();image.ReadPixels(new Rect(0,0,1200,900),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"VirtualHouse/upper-small-room-preview.png"),image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;
            Object.DestroyImmediate(image);Object.DestroyImmediate(targetImage);AssetDatabase.Refresh();
        }
    }
}
