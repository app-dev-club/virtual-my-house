using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        private static Material WesternMaterial(string key, Color color)
        {
            Material material = GetMaterial(key);
            material.color = color;
            material.SetFloat("_Smoothness", 0.22f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetClearWindowMaterial()
        {
            Material material = GetMaterial("ClearWindow");
            material.color = new Color(0.80f, 0.92f, 0.96f, 0.12f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetFloat("_Smoothness", 0.65f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Split solid wall and skirt boxes around each window, including their colliders.
        // Existing exterior windows were previously just opaque panels placed over solid walls.
        private static void CutWindowOpening(Transform root, Vector3 p, float width, float bottom, float height, bool side)
        {
            Bounds cut = new Bounds(p + Vector3.up * (bottom + height / 2f),
                side ? new Vector3(0.46f, height, width) : new Vector3(width, height, 0.46f));
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.name.StartsWith("壁_") && !renderer.name.StartsWith("濃灰色の腰壁")) continue;
                Bounds box = renderer.bounds;
                if (!box.Intersects(cut)) continue;
                float l = side ? box.min.z : box.min.x, r = side ? box.max.z : box.max.x;
                float cl = Mathf.Max(l, side ? cut.min.z : cut.min.x);
                float cr = Mathf.Min(r, side ? cut.max.z : cut.max.x);
                float low = Mathf.Max(box.min.y, cut.min.y), high = Mathf.Min(box.max.y, cut.max.y);
                if (cr <= cl || high <= low) continue;
                Transform parent = renderer.transform.parent;
                Material material = renderer.sharedMaterial;
                string name = renderer.name;
                void Piece(float left, float right, float y0, float y1)
                {
                    if (right-left < 0.001f || y1-y0 < 0.001f) return;
                    Vector3 pos = side ? new Vector3(box.center.x, (y0+y1)/2f, (left+right)/2f)
                        : new Vector3((left+right)/2f, (y0+y1)/2f, box.center.z);
                    Vector3 size = side ? new Vector3(box.size.x,y1-y0,right-left)
                        : new Vector3(right-left,y1-y0,box.size.z);
                    CreateBox(name+"_窓開口",pos,size,material,parent);
                }
                Piece(l,cl,box.min.y,box.max.y); Piece(cr,r,box.min.y,box.max.y);
                Piece(cl,cr,box.min.y,low); Piece(cl,cr,high,box.max.y);
                Object.DestroyImmediate(renderer.gameObject);
            }
            Bounds view = cut;
            view.Expand(-0.02f);
            foreach (MeshRenderer wall in root.GetComponentsInChildren<MeshRenderer>())
                if ((wall.name.StartsWith("壁_") || wall.name.StartsWith("濃灰色の腰壁")) && wall.bounds.Intersects(view))
                    throw new System.InvalidOperationException($"Window view blocked by {wall.name}");
            Debug.Log($"Window opening clear at {p}, upper={bottom > 3f}");
        }

        private static void CreateOpenCurtains(Transform parent, Vector3 center, float width, float height, bool side)
        {
            Transform curtains = NewGroup("カーテン_左右に開いた状態", parent);
            float bundle = Mathf.Min(0.28f, width * 0.10f);
            foreach (int end in new[] { -1, 1 })
                for (int i=0; i<6; i++)
                {
                    float offset = end * (width/2f-bundle/2f) + (i-2.5f)*bundle/6f;
                    Vector3 pos = center + (side ? Vector3.forward : Vector3.right)*offset
                        + (side ? Vector3.right : Vector3.forward)*(i%2)*0.024f;
                    CreateDecorationBox($"カーテンひだ_{end}_{i}",pos,
                        side ? new Vector3(0.065f,height,bundle/6f) : new Vector3(bundle/6f,height,0.065f),
                        GetMaterial("CurtainOrange"),curtains);
                }
            CreateDecorationBox("カーテンレール", center+Vector3.up*(height/2f+0.06f),
                side ? new Vector3(0.04f,0.035f,width+0.12f) : new Vector3(width+0.12f,0.035f,0.04f),
                GetMaterial("WindowFrame"),curtains);
        }

        private static void CreateWesternRoomInterior(Transform first)
        {
            Transform room = NewGroup("洋室_写真ベース",first);
            Vector3 sw=PlanPoint(158,442,0), ne=PlanPoint(310,291,0);
            float w=ne.x-sw.x, d=ne.z-sw.z, cx=(sw.x+ne.x)/2, cz=(sw.z+ne.z)/2;
            Material wallpaper=WesternMaterial("WesternWallpaper",new Color(0.72f,0.66f,0.59f));
            Material floor=WesternMaterial("WesternFloor",new Color(0.57f,0.40f,0.22f));
            Material ivory=WesternMaterial("WesternIvory",new Color(0.82f,0.76f,0.62f));
            Material rug=WesternMaterial("WesternRug",new Color(0.34f,0.27f,0.20f));
            // Recolour the actual wall segments, so window and door holes remain real openings.
            foreach(Transform wall in first.Find("壁"))
            {
                Bounds b=wall.GetComponent<Renderer>().bounds;
                if(b.center.x>=sw.x-0.5f && b.center.x<=ne.x+0.02f && b.center.z>=sw.z-0.01f && b.center.z<=ne.z+0.01f)
                    wall.GetComponent<Renderer>().sharedMaterial=wallpaper;
            }
            for(int x=0;x<8;x++) for(int z=0;z<8;z++)
                CreateDecorationBox($"床タイル_{x}_{z}",new Vector3(sw.x+w*(x+0.5f)/8,0.012f,sw.z+d*(z+0.5f)/8),
                    new Vector3(w/8-0.006f,0.02f,d/8-0.006f),floor,room);
            CreateDecorationBox("ラグ",new Vector3(cx,0.027f,cz),new Vector3(2.45f,0.016f,2.30f),rug,room);
            CreateDecorationBox("ラグ中央",new Vector3(cx,0.037f,cz),new Vector3(2.18f,0.006f,2.03f),floor,room);
            CreateDecorationBox("洋室天井",new Vector3(cx,2.60f,cz),new Vector3(w,0.06f,d),wallpaper,room);
            // Estimated position in the solid northern section of the east wall, clear of the hall door.
            float shelfZ=PlanPoint(310,330,0).z;
            Transform cabinet=NewGroup("造付け飾り棚",room);
            float shelfX=ne.x-0.30f;
            CreateBox("下部収納",new Vector3(shelfX,0.43f,shelfZ),new Vector3(0.40f,0.86f,1.9f),GetMaterial("Cabinet"),cabinet);
            CreateBox("カウンター",new Vector3(shelfX,0.90f,shelfZ),new Vector3(0.46f,0.06f,1.96f),GetMaterial("LightWood"),cabinet);
            foreach(int side in new[]{-1,1})
                CreateDecorationBox("飾り棚側枠",new Vector3(shelfX,1.50f,shelfZ+side*0.96f),new Vector3(0.4f,1.2f,0.04f),GetMaterial("LightWood"),cabinet);
            for(int i=0;i<2;i++)
                CreateBox("違い棚",new Vector3(shelfX,1.43f+i*0.15f,shelfZ+(i==0?-0.46f:0.46f)),new Vector3(0.36f,0.035f,0.88f),GetMaterial("DarkWood"),cabinet);
            // Decorative fireplace (no fire), matching the pale surround and dark mantel.
            Transform fireplace=NewGroup("暖炉風飾り",room);
            float fx=PlanPoint(255,291,0).x, fz=ne.z-0.25f;
            CreateBox("暖炉背面",new Vector3(fx,0.55f,fz+0.13f),new Vector3(1.2f,1.1f,0.06f),GetMaterial("DarkWood"),fireplace);
            foreach(int side in new[]{-1,1})
                CreateBox("暖炉側柱",new Vector3(fx+side*0.49f,0.55f,fz),new Vector3(0.22f,1.1f,0.28f),ivory,fireplace);
            CreateBox("暖炉上枠",new Vector3(fx,1.0f,fz),new Vector3(1.1f,0.20f,0.28f),ivory,fireplace);
            CreateBox("マントルピース",new Vector3(fx,1.15f,fz),new Vector3(1.38f,0.07f,0.40f),GetMaterial("DarkWood"),fireplace);
            CreateBox("炉台",new Vector3(fx,0.055f,fz),new Vector3(1.3f,0.11f,0.42f),GetMaterial("DarkWood"),fireplace);
            CreateDecorationBox("壁灯台座",new Vector3(ne.x-0.12f,1.98f,ne.z-0.8f),new Vector3(0.12f,0.26f,0.15f),GetMaterial("LightWood"),room);
            CreateWarmLight(room,"洋室壁灯",new Vector3(ne.x-0.24f,2.12f,ne.z-0.8f),3.7f,0.6f);
            CreateOpenCurtains(room,PlanPoint(234,438,1.18f),132*PlanScale,2.12f,false);
            CreateOpenCurtains(room,PlanPoint(195,296,1.48f),58*PlanScale,1.30f,false);
            CreateOpenCurtains(room,PlanPoint(144,367,1.48f),94*PlanScale,1.30f,true);
            CreateWesternFurniturePrefabs();
        }

        private static void CreateWesternFurniturePrefabs()
        {
            const string folder="Assets/VirtualHouse/Prefabs";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/VirtualHouse","Prefabs");
            Material fabric=WesternMaterial("WesternUpholstery",new Color(0.57f,0.57f,0.36f));
            foreach(bool sofa in new[]{false,true})
            {
                GameObject root=new(sofa?"WesternSofa":"WesternArmchair");
                float width=sofa?1.7f:0.78f;
                CreateBox("座面",new Vector3(0,0.43f,0),new Vector3(width,0.18f,0.70f),fabric,root.transform);
                CreateBox("背もたれ",new Vector3(0,0.77f,0.31f),new Vector3(width,0.58f,0.14f),fabric,root.transform);
                foreach(int side in new[]{-1,1})
                    CreateBox("肘掛け",new Vector3(side*(width/2-0.04f),0.57f,0),new Vector3(0.14f,0.28f,0.72f),fabric,root.transform);
                foreach(int x in new[]{-1,1}) foreach(int z in new[]{-1,1})
                    CreateBox("脚",new Vector3(x*(width/2-0.12f),0.16f,z*0.25f),new Vector3(0.07f,0.32f,0.07f),GetMaterial("DarkWood"),root.transform);
                PrefabUtility.SaveAsPrefabAsset(root,folder+"/"+root.name+".prefab");
                Object.DestroyImmediate(root);
            }
            GameObject table=new("WesternCoffeeTable");
            CreateBox("天板",new Vector3(0,0.46f,0),new Vector3(1.4f,0.07f,0.55f),GetMaterial("WesternIvory"),table.transform);
            foreach(int x in new[]{-1,1}) foreach(int z in new[]{-1,1})
                CreateBox("脚",new Vector3(x*0.57f,0.215f,z*0.19f),new Vector3(0.055f,0.43f,0.055f),GetMaterial("DarkWood"),table.transform);
            PrefabUtility.SaveAsPrefabAsset(table,folder+"/WesternCoffeeTable.prefab");
            Object.DestroyImmediate(table);
        }

        public static void RebuildWesternRoom()
        {
            Generate();
            ValidateNandoOpening();
            foreach(string name in new[]{"WesternArmchair","WesternSofa","WesternCoffeeTable"})
                if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VirtualHouse/Prefabs/"+name+".prefab")==null)
                    throw new System.InvalidOperationException("Missing furniture prefab: "+name);
            RenderWesternRoomPreview();
        }

        [MenuItem("Virtual House/Render Western Room Preview")]
        public static void RenderWesternRoomPreview()
        {
            EditorSceneManager.OpenScene(OutputScene,OpenSceneMode.Single);
            Camera camera=Camera.main;
            camera.transform.position=PlanPoint(292,409,1.65f);
            camera.transform.LookAt(PlanPoint(194,324,1.20f));
            camera.fieldOfView=75;
            foreach(TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) label.GetComponent<Renderer>().enabled=false;
            RenderTexture target=new(1280,900,24);
            Texture2D image=new(1280,900,TextureFormat.RGB24,false);
            RenderTexture previous=RenderTexture.active;
            camera.targetTexture=target; RenderTexture.active=target;
            camera.Render(); image.ReadPixels(new Rect(0,0,1280,900),0,0); image.Apply();
            camera.targetTexture=null; RenderTexture.active=previous;
            File.WriteAllBytes(Path.Combine(Application.dataPath,"VirtualHouse/western-room-preview.png"),image.EncodeToPNG());
            Object.DestroyImmediate(image); Object.DestroyImmediate(target);
            AssetDatabase.Refresh();
            Debug.Log("Western room generated: furniture prefabs saved; curtains open; exterior window openings cut.");
        }
    }
}
