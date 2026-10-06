using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        // Layout follows the DK footprint. Heights and furniture sizes are photo estimates.
        private static void CreateKitchenInterior(Transform first)
        {
            Transform room = NewGroup("キッチン_写真ベース", first);
            Material cream = WesternMaterial("KitchenCream", new Color(.80f,.79f,.68f));
            Material steel = WesternMaterial("KitchenSteel", new Color(.57f,.63f,.65f));
            steel.SetFloat("_Metallic", .7f); steel.SetFloat("_Smoothness", .65f);
            Material basin = WesternMaterial("KitchenBasin", new Color(.31f,.38f,.40f));
            Material wood = GetMaterial("LightWood");
            Vector3 south = PlanPoint(764,423,0);
            float z = south.z + .40f;
            // Facing the south window: low cooker stand on the left, drainer and sink to its right.
            void Cabinet(string name, float px, float width, float height)
            {
                float x = PlanPoint(px,423,0).x;
                Transform unit = NewGroup(name,room);
                CreateBox("本体",new Vector3(x,height/2,z),new Vector3(width,height,.60f),cream,unit);
                CreateDecorationBox("蹴込み",new Vector3(x,.045f,z+.305f),new Vector3(width,.09f,.025f),GetMaterial("DarkWood"),unit);
                for(int i=0;i<2;i++)
                {
                    float dx=(i==0?-1:1)*width/4;
                    CreateDecorationBox("扉",new Vector3(x+dx,height/2+.03f,z+.315f),new Vector3(width/2-.014f,height-.12f,.022f),cream,unit);
                    CreateDecorationBox("金属取手",new Vector3(x+dx,height-.07f,z+.34f),new Vector3(width*.30f,.018f,.025f),steel,unit);
                }
            }
            Cabinet("低いコンロ台_器具なし",810,.60f,.65f);
            Cabinet("調理台",783,.67f,.82f);
            Cabinet("流し台",745,1.14f,.82f);
            foreach(var part in new[]{new Vector3(810,.68f,.60f),new Vector3(783,.85f,.67f)})
                CreateBox("ステンレス天板",new Vector3(PlanPoint(part.x,423,0).x,part.y,z),new Vector3(part.z,.035f,.64f),steel,room);
            float sx=PlanPoint(734,423,0).x;
            // A real recessed basin: bottom and four thin sides, no solid slab across the opening.
            CreateBox("シンク底",new Vector3(sx,.66f,z),new Vector3(.61f,.025f,.43f),basin,room);
            foreach(int sign in new[]{-1,1})
            {
                CreateBox("シンク側面",new Vector3(sx+sign*.315f,.76f,z),new Vector3(.025f,.20f,.47f),steel,room);
                CreateBox("シンク縁",new Vector3(sx+sign*.32f,.855f,z),new Vector3(.05f,.025f,.64f),steel,room);
                CreateBox("シンク前後面",new Vector3(sx,.76f,z+sign*.23f),new Vector3(.64f,.20f,.025f),steel,room);
                CreateBox("シンク前後縁",new Vector3(sx,.855f,z+sign*.275f),new Vector3(.64f,.025f,.09f),steel,room);
            }
            // The cabinet top is below the basin; its front remains full height.
            Transform sink=room.Find("流し台/本体");
            sink.localScale=new Vector3(1.14f,.60f,.60f); sink.position=new Vector3(PlanPoint(745,423,0).x,.30f,z);
            float drainX=PlanPoint(758,423,0).x;
            CreateBox("水切り天板",new Vector3(drainX,.85f,z),new Vector3(.47f,.035f,.64f),steel,room);
            for(int i=0;i<7;i++) CreateDecorationBox("水切り溝",new Vector3(drainX,.871f,z-.22f+i*.07f),new Vector3(.39f,.006f,.008f),basin,room);
            foreach(float x in new[]{sx-.16f,sx+.16f})
            {
                KitchenRod(room,"蛇口立上り",new Vector3(x,.90f,z-.27f),new Vector3(x,1.15f,z-.27f),.025f,steel);
                KitchenRod(room,"蛇口吐水管",new Vector3(x,1.15f,z-.27f),new Vector3(x,1.15f,z-.06f),.023f,steel);
                KitchenRod(room,"蛇口先端",new Vector3(x,1.15f,z-.06f),new Vector3(x,1.07f,z-.06f),.023f,steel);
                CreateDecorationBox("蛇口ハンドル",new Vector3(x,1.18f,z-.27f),new Vector3(.09f,.02f,.025f),steel,room);
            }
            foreach(float px in new[]{709f,830f})
            {
                float x=PlanPoint(px,423,0).x;
                CreateBox("木製補助台",new Vector3(x,.79f,z),new Vector3(.35f,.045f,.60f),wood,room);
                foreach(int sign in new[]{-1,1}) CreateBox("補助台脚",new Vector3(x+sign*.14f,.38f,z+.24f),new Vector3(.04f,.76f,.04f),wood,room);
            }
            // Window reveals are thin trim only: keep the transparent opening unobstructed.
            foreach(float px in new[]{708f,820f,837f}) CreateDecorationBox("南壁木柱",PlanPoint(px,420,1.34f),new Vector3(.065f,2.68f,.07f),wood,room);
            CreateDecorationBox("南窓敷居",PlanPoint(764,419,.91f),new Vector3(108*PlanScale,.055f,.22f),wood,room);
            CreateDecorationBox("南窓上枠",PlanPoint(764,419,2.02f),new Vector3(112*PlanScale,.075f,.12f),wood,room);
            CreateDecorationBox("東窓敷居",PlanPoint(837,376,.89f),new Vector3(.22f,.055f,65*PlanScale),wood,room);
            CreateDecorationBox("流し元灯",PlanPoint(764,416,2.12f),new Vector3(.58f,.075f,.10f),cream,room);
            CreateWarmLight(room,"キッチン照明",PlanPoint(764,385,2.4f),5f,.7f);
            CreateDecorationBox("換気扇枠",PlanPoint(813,419,2.36f),new Vector3(.36f,.38f,.085f),cream,room);
            CreateDecorationBox("換気扇奥",PlanPoint(813,416,2.36f),new Vector3(.30f,.31f,.025f),basin,room);
            for(int i=0;i<4;i++)
            {
                GameObject blade=CreateDecorationBox("換気扇羽根",PlanPoint(813,415,2.36f),new Vector3(.25f,.06f,.02f),GetMaterial("WindowFrame"),room);
                blade.transform.rotation=Quaternion.Euler(0,0,45*i);
            }
            // No loose boxes, discarded doors or hanging curtain across the service route.
        }

        private static void KitchenRod(Transform parent,string name,Vector3 a,Vector3 b,float diameter,Material material)
        {
            GameObject rod=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rod.name=name; rod.transform.SetParent(parent,false);
            rod.transform.position=(a+b)/2; rod.transform.up=(b-a).normalized;
            rod.transform.localScale=new Vector3(diameter,Vector3.Distance(a,b)/2,diameter);
            rod.GetComponent<Renderer>().sharedMaterial=material;
            Object.DestroyImmediate(rod.GetComponent<Collider>());
        }

        public static void RebuildKitchen()
        {
            Generate();
            ValidateServiceWalk();
            RenderKitchenPreview();
        }

        [MenuItem("Virtual House/Render Kitchen Preview")]
        public static void RenderKitchenPreview()
        {
            EditorSceneManager.OpenScene(OutputScene);
            Camera camera=Camera.main;
            camera.transform.position=PlanPoint(720,335,1.65f);
            camera.transform.LookAt(PlanPoint(780,418,1.15f)); camera.fieldOfView=70;
            foreach(TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) label.GetComponent<Renderer>().enabled=false;
            RenderTexture previous=RenderTexture.active;
            RenderTexture target=new(1280,900,24); Texture2D image=new(1280,900,TextureFormat.RGB24,false);
            camera.targetTexture=target; RenderTexture.active=target; camera.Render();
            image.ReadPixels(new Rect(0,0,1280,900),0,0); image.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"VirtualHouse/kitchen-preview.png"),image.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=previous;
            Object.DestroyImmediate(image); Object.DestroyImmediate(target); AssetDatabase.Refresh();
            Debug.Log("Kitchen generated and service passage walk validated.");
        }
    }
}
