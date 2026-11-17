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
        // IMG_20260930_111512728: north window is on the left when facing the
        // east-facing original vanity. Dimensions are estimated from the photo.
        private static void CreateOriginalWashroomWindow(Transform house)
        {
            Transform first = house.Find("1階");
            Transform old = first.Find("旧洗面スペース_北窓");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Transform group = NewGroup("旧洗面スペース_北窓", first);
            Vector3 wall = PlanPoint(707,179,0);
            const float bottom = .90f, height = 1.28f;
            float width = 30 * PlanScale;
            CutWindowOpening(house, wall, width, bottom, height, false);
            WindowX(group,"銀色の二枚引違いサッシ",wall.x/Grid,wall.z/Grid,
                width/Grid,height,bottom,2,false);
            Transform sash = group.GetChild(0);
            Material glass = GetMaterial("WashroomFrostedGlass");
            glass.CopyPropertiesFromMaterial(GetClearWindowMaterial());
            glass.color = new Color(.76f,.87f,.85f,.88f);
            glass.SetFloat("_Smoothness",.12f);
            EditorUtility.SetDirty(glass);
            Renderer pane = sash.Find("ガラス").GetComponent<Renderer>();
            pane.transform.localScale = new Vector3(width-.06f,height-.06f,.045f);
            pane.sharedMaterial = glass;
            pane.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Material silver = WesternMaterial("WashroomWindowSilver",new Color(.57f,.61f,.60f));
            foreach (Renderer part in sash.GetComponentsInChildren<Renderer>())
                if (part != pane) part.sharedMaterial = silver;
            Material wood = GetMaterial("LightWood");
            foreach (int sign in new[]{-1,1})
            {
                CreateBox("縦木枠",wall+new Vector3(sign*(width/2+.028f),bottom+height/2,-.025f),
                    new Vector3(.056f,height+.12f,.19f),wood,group);
                CreateBox(sign<0?"木製窓台":"上木枠",wall+new Vector3(0,bottom+(sign<0?-.035f:height+.035f),-.025f),
                    new Vector3(width+.11f,.07f,.19f),wood,group);
            }
            CreateDecorationBox("中央のクレセント錠",wall+new Vector3(.025f,bottom+height*.48f,-.065f),
                new Vector3(.025f,.09f,.025f),silver,group);
        }

        [MenuItem("Virtual House/Update Original Washroom Window All Plans")]
        public static void UpdateOriginalWashroomWindowAllPlans()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in new[]{OutputScene,RenovatedScene,AlternativeScene,PlanCScene})
                {
                    Materials.Clear();
                    var scene = EditorSceneManager.OpenScene(path);
                    Transform house = scene.GetRootGameObjects().First(g=>g.name=="住宅概形_図面ベース").transform;
                    CreateOriginalWashroomWindow(house);
                    Physics.SyncTransforms();
                    foreach (float x in new[]{700f,714f})
                        foreach (var hit in Physics.RaycastAll(PlanPoint(x,194,1.5f),Vector3.forward,.7f))
                            if (hit.collider.name.StartsWith("壁_") || hit.collider.name.StartsWith("濃灰色の腰壁"))
                                throw new System.InvalidOperationException("Washroom window blocked: "+hit.collider.name);
                    EditorSceneManager.SaveScene(scene);
                    foreach (var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                        label.GetComponent<Renderer>().enabled=false;
                    Camera camera = Camera.main;
                    camera.transform.position=PlanPoint(707,212,1.55f);
                    camera.transform.LookAt(PlanPoint(707,179,1.51f));
                    camera.fieldOfView=85;
                    RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/washroom-window-"+Path.GetFileNameWithoutExtension(path)+".png"));
                    Debug.Log("Original washroom window validated: "+path);
                }
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally { if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
