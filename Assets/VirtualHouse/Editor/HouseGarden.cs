using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        // Positions and sizes inferred from the September 30 exterior photo sequence.
        // Site image coordinates retain the measured lot/house alignment.
        private static void CreatePhotoGarden(Transform site)
        {
            Transform garden=NewGroup("庭_外観写真20260930",site);
            Material paving=GardenSurface("GardenConcrete",new Color(.49f,.48f,.43f),new Color(.28f,.30f,.24f),false);
            Material rust=GardenSurface("GardenShedRust",new Color(.52f,.30f,.21f),new Color(.24f,.12f,.075f),true);
            Transform concrete=NewGroup("コンクリート_犬走りと東側通路",garden);
            void Slab(string name,float l,float t,float r,float b)
                => CreateBox(name,SitePoint((l+r)/2,(t+b)/2,-.10f),new Vector3((r-l)*SiteScale,.12f,(b-t)*SiteScale),paving,concrete);
            // Southern window-front apron steps with the western room's projecting facade.
            Slab("洋室南_犬走り",383,326,457,347);
            Slab("縁側南_犬走り",457,317,612,339);
            Slab("玄関西_接続",603,338,612,360);
            Slab("玄関前_アプローチ",612,360,650,377);
            Slab("東側_コンクリート通路",790,177,850,326);
            Slab("北東_細い通路",741,166,850,182);
            GardenPolygon(concrete,"玄関から東側舗装への接続",new[]{new Vector2(650,357),new Vector2(790,310),new Vector2(850,326),new Vector2(650,377)},-.038f,paving,true);
            // Thin joints and modest staining leave the photo's worn surface readable.
            Material joint=WesternMaterial("GardenJoint",new Color(.29f,.29f,.25f));
            for(float x=467;x<605;x+=27)
                CreateDecorationBox("犬走り目地",SitePoint(x,328,-.037f),new Vector3(.009f,.002f,21*SiteScale),joint,concrete);
            for(float v=192;v<324;v+=26)
                CreateDecorationBox("東舗装目地",SitePoint(820,v,-.037f),new Vector3(60*SiteScale,.002f,.012f),joint,concrete);
            CreateGardenShed(garden,rust);
            CreateGardenTree(garden,"西側_針葉樹",357,291,5.8f,2.0f,11,true,false);
            CreateGardenTree(garden,"南西_赤い実の木",368,337,4.1f,1.75f,22,false,true);
            CreateGardenTree(garden,"北西_葉の密な木",403,191,3.2f,1.25f,33,false,false);
            CreateGardenTree(garden,"北側_カエデ状の木",467,171,3.0f,1.1f,44,false,false);
            CreateGardenTree(garden,"玄関脇_低木",666,351,1.35f,.68f,55,false,false);
            CreateGardenTree(garden,"DK前_針葉低木",704,340,1.1f,.83f,66,true,false);
            CreateGardenTree(garden,"東側_大きな葉の低木",771,202,1.65f,.90f,77,false,false);
            CreateGardenTree(garden,"北東_低木",752,188,1.05f,.62f,88,false,false);
            CreateGardenGroundcover(garden);
            // The concrete washing basin below the southern windows (photo 111045408).
            Transform basin=NewGroup("南窓下_屋外流し",garden);
            Vector3 p=SitePoint(460,326,0);
            CreateBox("流し底",p+Vector3.up*.035f,new Vector3(.86f,.10f,.53f),paving,basin);
            foreach(int sign in new[]{-1,1})
            {
                CreateBox("流し側",p+new Vector3(sign*.42f,.16f,0),new Vector3(.045f,.25f,.55f),paving,basin);
                CreateBox("流し縁",p+new Vector3(0,.16f,sign*.26f),new Vector3(.86f,.25f,.045f),paving,basin);
            }
            KitchenRod(basin,"外水栓",p+new Vector3(0,.22f,.29f),p+new Vector3(0,.84f,.29f),.025f,GetMaterial("KitchenSteel"));
            KitchenRod(basin,"蛇口",p+new Vector3(0,.80f,.29f),p+new Vector3(0,.80f,.12f),.024f,GetMaterial("KitchenSteel"));
        }

        private static Material GardenSurface(string name,Color baseColor,Color patchColor,bool rusty)
        {
            const string folder="Assets/VirtualHouse/GeneratedMeshes";
            string path=folder+"/"+name+".png";
            if(!File.Exists(path))
            {
                var texture=new Texture2D(256,256,TextureFormat.RGB24,false);
                var pixels=new Color[256*256];
                for(int y=0;y<256;y++)for(int x=0;x<256;x++)
                {
                    float noise=Mathf.PerlinNoise(x*.037f+12,y*(rusty?.008f:.037f)+31);
                    float speck=Mathf.PerlinNoise(x*.81f,y*.79f);
                    float stain=Mathf.SmoothStep(.30f,.80f,noise)*.64f;
                    pixels[y*256+x]=Color.Lerp(baseColor,patchColor,stain)*( .84f+.30f*speck);
                }
                texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            Material material=WesternMaterial(name,Color.white);
            material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            material.SetFloat("_Smoothness",.07f);EditorUtility.SetDirty(material);
            return material;
        }

        private static void GardenPolygon(Transform parent,string name,Vector2[] points,float y,Material material,bool collider)
        {
            var data=new GardenGeometry();
            for(int i=1;i<points.Length-1;i++)
                data.Triangle(SitePoint(points[0].x,points[0].y,y),SitePoint(points[i].x,points[i].y,y),SitePoint(points[i+1].x,points[i+1].y,y));
            GameObject obj=data.Save(parent,name,material);
            if(collider)obj.AddComponent<MeshCollider>().sharedMesh=obj.GetComponent<MeshFilter>().sharedMesh;
        }

        private static void CreateGardenShed(Transform garden,Material rust)
        {
            Transform shed=NewGroup("西庭_錆色の物置",garden);
            // Approximately 1.85m wide x .95m deep x 1.85m tall; front faces west.
            // Pull the shed west by ~1.94m and slightly north, clear of the projecting western room.
            Vector3 p=SitePoint(355,226,0);
            CreateBox("物置本体",p+Vector3.up*.865f,new Vector3(.95f,1.85f,1.85f),rust,shed);
            CreateRotatedBox("張り出した薄い屋根",p+Vector3.up*1.83f,new Vector3(1.12f,.065f,2.02f),new Vector3(0,0,-3),rust,shed);
            Material seam=WesternMaterial("GardenShedSeam",new Color(.23f,.14f,.105f));
            foreach(float z in new[]{-.88f,0,.88f})
                CreateDecorationBox("物置の縦継ぎ目",p+new Vector3(-.478f,.87f,z),new Vector3(.008f,1.79f,.012f),seam,shed);
            for(int i=0;i<16;i++)
                CreateDecorationBox("物置鋼板リブ",p+new Vector3(-.482f,.86f,-.84f+i*.112f),new Vector3(.012f,1.78f,.009f),rust,shed);
            foreach(float z in new[]{-.065f,.065f})
                CreateDecorationBox("物置取手",p+new Vector3(-.51f,.91f,z),new Vector3(.045f,.13f,.018f),seam,shed);
            foreach(float z in new[]{-.73f,.73f})
                CreateBox("物置基礎ブロック",p+new Vector3(0,-.025f,z),new Vector3(1.01f,.13f,.24f),GetMaterial("Concrete"),shed);
        }

        private sealed class GardenGeometry
        {
            private readonly List<Vector3> vertices=new();
            private readonly List<int> indices=new();
            public void Triangle(Vector3 a,Vector3 b,Vector3 c)
            {
                int i=vertices.Count;vertices.AddRange(new[]{a,b,c});indices.AddRange(new[]{i,i+1,i+2});
            }
            public void Leaf(Vector3 p,Quaternion rotation,float length,float width)
            {
                Vector3 a=p+rotation*new Vector3(0,0,-length/2),b=p+rotation*new Vector3(-width/2,0,0);
                Vector3 c=p+rotation*new Vector3(0,.025f,length/2),d=p+rotation*new Vector3(width/2,0,0);
                Triangle(a,b,c);Triangle(a,c,d);Triangle(c,b,a);Triangle(d,c,a);
            }
            public void Branch(Vector3 a,Vector3 b,float r1,float r2)
            {
                Quaternion q=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
                for(int i=0;i<9;i++)
                {
                    float u=i*Mathf.PI*2/9,v=(i+1)*Mathf.PI*2/9;
                    Vector3 d=q*new Vector3(Mathf.Cos(u),0,Mathf.Sin(u)),e=q*new Vector3(Mathf.Cos(v),0,Mathf.Sin(v));
                    Triangle(a+d*r1,b+d*r2,b+e*r2);Triangle(a+d*r1,b+e*r2,a+e*r1);
                }
            }
            public GameObject Save(Transform parent,string name,Material material)
            {
                string path="Assets/VirtualHouse/GeneratedMeshes/Garden_"+parent.name+"_"+name+".asset";
                Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
                mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);
                mesh.SetUVs(0,vertices.Select(p=>new Vector2(p.x,p.z)).ToList());
                mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                GameObject obj=new(name);obj.transform.SetParent(parent,false);
                obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;
                return obj;
            }
        }

        private static void CreateGardenTree(Transform garden,string name,float x,float v,float height,float radius,int seed,bool conifer,bool fruit)
        {
            Transform tree=NewGroup(name,garden);
            Vector3 origin=SitePoint(x,v);
            var random=new System.Random(seed);
            float R(float min,float max)=>Mathf.Lerp(min,max,(float)random.NextDouble());
            var wood=new GardenGeometry();
            var foliage=new[]{new GardenGeometry(),new GardenGeometry(),new GardenGeometry()};
            Material bark=WesternMaterial("GardenBark",new Color(.30f,.25f,.18f));
            float trunkRadius=height>4?.18f:height>2?.095f:.035f;
            Vector3 bend=origin+new Vector3(fruit?.25f:0,height*.42f,0);
            wood.Branch(origin,bend,trunkRadius,trunkRadius*.72f);
            wood.Branch(bend,origin+Vector3.up*height,trunkRadius*.72f,.018f);
            int branches=conifer?38:26;
            for(int j=0;j<branches;j++)
            {
                float angle=j*2.39996f,level=R(.38f,.92f),reach=radius*(conifer?(1-level*.72f):R(.50f,1f));
                Vector3 start=origin+Vector3.up*(height*level*.78f);
                Vector3 end=origin+new Vector3(Mathf.Cos(angle)*reach,height*level,Mathf.Sin(angle)*reach);
                wood.Branch(start,end,trunkRadius*.32f,.012f);
                for(int twig=0;twig<4;twig++)
                {
                    Vector3 tip=end+new Vector3(R(-.42f,.42f),R(-.13f,.42f),R(-.42f,.42f))*(height>2?1:.5f);
                    wood.Branch(Vector3.Lerp(start,end,.64f),tip,.018f,.004f);
                    for(int k=0;k<(conifer?32:25);k++)
                    {
                        Vector3 leaf=Vector3.Lerp(end,tip,R(0,1))+new Vector3(R(-.26f,.26f),R(-.20f,.20f),R(-.26f,.26f));
                        float size=conifer?R(.13f,.23f):height<2?R(.18f,.29f):R(.13f,.23f);
                        foliage[k%3].Leaf(leaf,Quaternion.Euler(R(-50,50),R(0,360),R(-45,45)),size,conifer?size*.22f:size*.55f);
                    }
                }
                if(fruit && j%3==0)
                {
                    GameObject apple=GameObject.CreatePrimitive(PrimitiveType.Sphere);apple.name="赤い実";
                    apple.transform.SetParent(tree,false);apple.transform.position=end-Vector3.up*.12f;
                    apple.transform.localScale=Vector3.one*.10f;
                    apple.GetComponent<Renderer>().sharedMaterial=WesternMaterial("GardenFruit",new Color(.57f,.15f,.08f));
                    Object.DestroyImmediate(apple.GetComponent<Collider>());
                }
            }
            wood.Save(tree,"幹と枝",bark);
            for(int i=0;i<3;i++)
                foliage[i].Save(tree,"葉群"+i,WesternMaterial((conifer?"GardenNeedles":"GardenLeaves")+i,
                    conifer?new Color(.12f+i*.032f,.24f+i*.035f,.10f+i*.015f):new Color(.18f+i*.043f,.32f+i*.04f,.09f+i*.023f)));
            var trunk=tree.gameObject.AddComponent<CapsuleCollider>();trunk.center=origin+Vector3.up*(height*.25f);
            trunk.height=height*.5f;trunk.radius=trunkRadius;
        }

        private static void CreateGardenGroundcover(Transform garden)
        {
            var geometry=new GardenGeometry();var random=new System.Random(930);
            // Local groundcover beds seen by the western trees, north wall and eastern shrubs.
            foreach(Vector4 bed in new[]{new Vector4(342,245,377,353),new Vector4(370,184,418,210),new Vector4(475,183,595,192),new Vector4(655,329,728,355),new Vector4(765,188,785,294)})
                for(int i=0;i<320;i++)
                {
                    float x=Mathf.Lerp(bed.x,bed.z,(float)random.NextDouble()),v=Mathf.Lerp(bed.y,bed.w,(float)random.NextDouble());
                    Vector3 p=SitePoint(x,v,-.065f);
                    for(int j=0;j<3;j++) geometry.Leaf(p+Vector3.up*.045f,Quaternion.Euler(20,j*120+i*17,15),.14f,.055f);
                }
            geometry.Save(garden,"地被植物",WesternMaterial("GardenGroundcover",new Color(.27f,.36f,.12f)));
        }

        private static void ValidatePhotoGarden(Transform house)
        {
            Transform garden=house.Find("敷地_図面7/庭_外観写真20260930");
            if(garden==null || garden.Find("西庭_錆色の物置")==null || garden.GetComponentsInChildren<CapsuleCollider>().Length!=8)
                throw new InvalidOperationException("Photo garden contents missing");
            // Southern apron, entry approach and eastern paving are reachable in both directions.
            ValidateAlternativeRoute(new[]{SitePoint(486,330,-.04f),SitePoint(550,330,-.04f),SitePoint(597,330,-.04f)});
            ValidateAlternativeRoute(new[]{SitePoint(631,375,-.04f),SitePoint(690,352,-.04f),SitePoint(750,332,-.04f),SitePoint(810,310,-.04f)});
            ValidateAlternativeRoute(new[]{SitePoint(331,230),SitePoint(331,260),SitePoint(342,270)});
            ValidateAlternativeRoute(new[]{SitePoint(379,205),SitePoint(379,240)});
            Debug.Log("PHOTO GARDEN VALIDATION PASSED: detached shed, 8 standing trees/shrubs, south apron, entry/east paving and front/rear shed access.");
        }

        private static void RenderGardenPreviews(Camera camera)
        {
            var labels=Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
            foreach(var label in labels) label.gameObject.SetActive(false);
            camera.orthographic=true;camera.orthographicSize=11.5f;
            camera.transform.position=SitePoint(255,515,22);
            camera.transform.LookAt(SitePoint(582,300,.8f));
            RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/garden-overview-preview.png"));
            camera.orthographic=false;camera.fieldOfView=65;
            camera.transform.position=SitePoint(275,290,3.0f);
            camera.transform.LookAt(SitePoint(385,257,1.8f));
            RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/garden-west-preview.png"));
            camera.transform.position=SitePoint(678,407,3.4f);
            camera.transform.LookAt(SitePoint(630,333,1.3f));
            RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/garden-entry-preview.png"));
            foreach(var label in labels) label.gameObject.SetActive(true);
        }

        public static void RenderSavedGardenPreviews()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(PlanCScene);
            RenderGardenPreviews(Camera.main);
            AssetDatabase.Refresh();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(PlanCScene);
        }
    }
}
