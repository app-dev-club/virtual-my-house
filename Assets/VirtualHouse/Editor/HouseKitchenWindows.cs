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
        // Photo estimate: 300mm from the inside wall face OUT towards the sash.
        // Architectural details live outside the furniture group, so renovations retain them.
        private static void CreateKitchenWindowRecesses(Transform house)
        {
            Transform first = house.Find("1階");
            Transform old = first.Find("キッチン窓_くぼみと窓台");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Transform original = first.Find("キッチン_写真ベース");
            foreach (string name in new[] { "南窓敷居", "南窓上枠", "東窓敷居" })
            {
                Transform trim = original?.Find(name);
                if (trim != null) Object.DestroyImmediate(trim.gameObject);
            }
            Transform group = NewGroup("キッチン窓_くぼみと窓台", first);
            AddKitchenWindowRecess(group, "南窓", PlanPoint(764,423,0), 108*PlanScale, .92f, 1.05f, false);
            AddKitchenWindowRecess(group, "東窓", PlanPoint(840,376,0), 65*PlanScale, .90f, 1.10f, true);
            CreateKitchenAppliances(house);
        }

        private static void AddKitchenWindowRecess(Transform parent, string name, Vector3 wall,
            float width, float bottom, float height, bool east)
        {
            Transform group = NewGroup(name, parent);
            Vector3 inward = east ? Vector3.left : Vector3.forward;
            Vector3 across = east ? Vector3.forward : Vector3.right;
            const float back = .0375f, depth = .30f, thickness = .035f;
            Vector3 insideFace = wall + inward*(WallThickness/2);
            Vector3 sash = insideFace - inward*(depth+back);
            Transform exterior = parent.root.Find("外観_写真ベース/腰壁と窓");
            Transform window = exterior.Find(east ? "東DK腰窓" : "DK南腰窓");
            Transform glass = window.Find("ガラス");
            // Set an absolute plane, so repeated updates never accumulate displacement.
            float offset = Vector3.Dot(sash-glass.position, inward);
            window.position += inward*offset;
            foreach (Transform hood in exterior)
            {
                if (hood.name != "窓庇" || Mathf.Abs(hood.position.y-(bottom+height+.14f)) > .001f
                    || Mathf.Abs(Vector3.Dot(hood.position-wall, across)) > .01f) continue;
                Vector3 outerEdge = sash-inward*.20f;
                hood.position = (insideFace+outerEdge)/2 + Vector3.up*(bottom+height+.14f);
                float hoodDepth = depth+back+.20f;
                hood.localScale = east ? new Vector3(hoodDepth,.07f,width+.26f)
                    : new Vector3(width+.26f,.07f,hoodDepth);
            }
            Material wood = GetMaterial("LightWood");
            void Part(string label, float u, float y, float d, float w, float h, float length, Material material)
                => CreateBox(label, sash + across*u + Vector3.up*y + inward*d,
                    east ? new Vector3(length,h,w) : new Vector3(w,h,length), material, group);
            // Cap the existing wall below the opening without coplanar top surfaces.
            Part("木製窓台_奥行300mm", 0, bottom+.01f-thickness/2, back+depth/2,
                width+.07f, thickness, depth, wood);
            Part("上見込み_木製", 0, bottom+height+thickness/2, back+depth/2,
                width+.07f, thickness, depth, wood);
            foreach (int sign in new[] {-1,1})
            {
                Part("側面見込み", sign*(width/2+thickness/2), bottom+height/2, back+depth/2,
                    thickness, height, depth, east ? GetMaterial("Wall") : wood);
                Part("室内側縦木枠", sign*(width/2+thickness/2), bottom+height/2, back+depth-.017f,
                    .055f, height+.07f, .04f, wood);
            }
            Part("室内側上木枠", 0, bottom+height+thickness/2, back+depth-.017f,
                width+.07f, thickness, .04f, wood);
            Part("窓台前縁", 0, bottom+.01f-thickness/2, back+depth-.0095f,
                width+.07f, .045f, .025f, wood);
        }

        [MenuItem("Virtual House/Update Kitchen Window Recesses All Plans")]
        public static void UpdateKitchenWindowRecesses()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in new[] {OutputScene, RenovatedScene, AlternativeScene, PlanCScene})
                {
                    Materials.Clear();
                    var scene = EditorSceneManager.OpenScene(path);
                    Transform house = scene.GetRootGameObjects().First(g => g.name == "住宅概形_図面ベース").transform;
                    CreateKitchenWindowRecesses(house);
                    Physics.SyncTransforms();
                    foreach (Transform recess in house.Find("1階/キッチン窓_くぼみと窓台"))
                    {
                        var shelf = recess.Find("木製窓台_奥行300mm").GetComponent<Collider>();
                        bool east = recess.name == "東窓";
                        Vector3 wall = east ? PlanPoint(840,376,0) : PlanPoint(764,423,0);
                        if (east ? shelf.bounds.min.x < wall.x-WallThickness/2-.001f
                            : shelf.bounds.max.z > wall.z+WallThickness/2+.001f)
                            throw new InvalidOperationException("Window ledge projects into the room: " + path);
                        Transform glass = house.Find("外観_写真ベース/腰壁と窓/"+(east ? "東DK腰窓" : "DK南腰窓")+"/ガラス");
                        if (east ? glass.position.x <= wall.x+WallThickness/2 : glass.position.z >= wall.z-WallThickness/2)
                            throw new InvalidOperationException("Window sash must project outside the wall: " + path);
                        if (!Physics.Raycast(shelf.bounds.center+Vector3.up*.15f, Vector3.down, out var hit, .2f)
                            || hit.collider != shelf)
                            throw new InvalidOperationException("Kitchen window ledge is obstructed: " + path + "/" + recess.name);
                    }
                    EditorSceneManager.SaveScene(scene);
                    foreach (TextMesh label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                        label.GetComponent<Renderer>().enabled = false;
                    Camera camera = Camera.main;
                    camera.transform.position = PlanPoint(740,345,1.65f);
                    camera.transform.LookAt(PlanPoint(812,405,1.35f));
                    camera.fieldOfView = 75;
                    RenderCameraToPng(camera, Path.Combine(Application.dataPath,"VirtualHouse/kitchen-windows-"+Path.GetFileNameWithoutExtension(path)+".png"));
                    if (path == OutputScene)
                    {
                        camera.transform.position = PlanPoint(901,482,1.65f);
                        camera.transform.LookAt(PlanPoint(815,405,1.4f));
                        RenderCameraToPng(camera, Path.Combine(Application.dataPath,"VirtualHouse/kitchen-windows-exterior.png"));
                    }
                    Debug.Log("Kitchen window recesses and usable ledges validated: " + path);
                }
            }
            finally { if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
