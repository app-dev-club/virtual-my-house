using System;
using System.Collections.Generic;
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
        // Image (7) is approximately half the floor-plan scale: 1.82m / 37.5px.
        // Align the inner outline (exterior walls), not the roof, at the entry's NW corner.
        // Image (612,316) corresponds to floor-plan (613,423). Do not rescale the house.
        private const float SiteScale = 1.82f / 37.5f;
        private static Vector3 SitePoint(float x,float v,float y=-.09f)
            => PlanPoint(613,423,y)+new Vector3((x-612)*SiteScale,0,(316-v)*SiteScale);

        private static void CreateTracedSite(Transform house)
        {
            bool hasOutdoorLayout=house.Find("敷地_図面7/"+OutdoorName)!=null;
            foreach(string name in new[]{"敷地_仮","基準グリッド_1マス0.909m","敷地_図面7"})
            {
                Transform old=house.Find(name);
                if(old!=null) Object.DestroyImmediate(old.gameObject);
            }
            Transform site=NewGroup("敷地_図面7",house);
            Vector3[] corners={SitePoint(308,158),SitePoint(856,158),SitePoint(856,329),SitePoint(308,466)};
            var vertices=new List<Vector3>(corners);
            vertices.AddRange(corners.Select(p=>p-Vector3.up*.18f));
            var triangles=new List<int>{0,1,2,0,2,3,4,6,5,4,7,6};
            for(int i=0;i<4;i++)
            {
                int next=(i+1)%4;
                triangles.AddRange(new[]{i,i+4,next,next,i+4,next+4});
            }
            const string path="Assets/VirtualHouse/GeneratedMeshes/TrapezoidSite.asset";
            Mesh mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            GameObject ground=new GameObject("庭_台形");
            ground.transform.SetParent(site,false);
            ground.AddComponent<MeshFilter>().sharedMesh=mesh;
            ground.AddComponent<MeshRenderer>().sharedMaterial=GetMaterial("Ground");
            ground.AddComponent<MeshCollider>().sharedMesh=mesh;
            // A flat boundary marker, not an inferred fence or wall.
            for(int i=0;i<4;i++)
            {
                Vector3 a=corners[i],b=corners[(i+1)%4];
                GameObject edge=CreateBox("敷地境界_"+i,(a+b)/2+Vector3.up*.012f,
                    new Vector3(.055f,.012f,Vector3.Distance(a,b)),GetMaterial("Concrete"),site);
                edge.transform.rotation=Quaternion.LookRotation(b-a,Vector3.up);
                Object.DestroyImmediate(edge.GetComponent<Collider>());
            }
            CreatePhotoGarden(site);
            if(hasOutdoorLayout) CreateOutdoorLayout(house);
        }

        [MenuItem("Virtual House/Update And Validate Site All Plans")]
        public static void UpdateAndValidateSite()
        {
            // Save and restore the user's scene setup; only change the common site group.
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in new[]{OutputScene,RenovatedScene,AlternativeScene,PlanCScene})
                {
                    Materials.Clear();
                    var scene=EditorSceneManager.OpenScene(path);
                    Transform house=scene.GetRootGameObjects().First(g=>g.name=="住宅概形_図面ベース").transform;
                    CreateTracedSite(house);
                    EditorSceneManager.SaveScene(scene);
                    AssetDatabase.SaveAssets();
                    Physics.SyncTransforms();
                    var collider=house.Find("敷地_図面7/庭_台形").GetComponent<MeshCollider>();
                    foreach(Vector2 p in new[]{new Vector2(320,170),new Vector2(840,170),new Vector2(840,325),new Vector2(320,450)})
                        if(!collider.Raycast(new Ray(SitePoint(p.x,p.y,5),Vector3.down),out _,10))
                            throw new InvalidOperationException("Site interior has no ground: "+p);
                    foreach(Vector2 p in new[]{new Vector2(300,200),new Vector2(865,200),new Vector2(500,150),new Vector2(800,370)})
                        if(collider.Raycast(new Ray(SitePoint(p.x,p.y,5),Vector3.down),out _,10))
                            throw new InvalidOperationException("Ground extends beyond trapezoid: "+p);
                    Vector3[] perimeter={SitePoint(325,166),SitePoint(835,166),SitePoint(835,310),SitePoint(600,375),SitePoint(325,430),SitePoint(325,166)};
                    if(house.Find("敷地_図面7/"+OutdoorName)!=null)
                        perimeter=new[]{SitePoint(325,166),SitePoint(835,166),SitePoint(841,320),SitePoint(735,344),SitePoint(600,385),SitePoint(475,406),SitePoint(440,405),SitePoint(365,405),SitePoint(325,430),SitePoint(325,166)};
                    var route=new List<Vector3>{perimeter[0]};
                    // The indoor walk helper allows four seconds per leg; subdivide long garden edges.
                    for(int i=1;i<perimeter.Length;i++)
                    {
                        int steps=Mathf.CeilToInt(Vector3.Distance(perimeter[i-1],perimeter[i])/4f);
                        for(int step=1;step<=steps;step++) route.Add(Vector3.Lerp(perimeter[i-1],perimeter[i],(float)step/steps));
                    }
                    ValidateAlternativeRoute(route.ToArray());
                    ValidatePhotoGarden(house);
                    Debug.Log("SITE VALIDATION PASSED: "+path+"; inside/outside boundary and bidirectional garden perimeter walk.");
                }
                Camera camera=Camera.main;
                RenderGardenPreviews(camera);
                GameObject.Find("外観_写真ベース").SetActive(false);
                GameObject.Find("2階").SetActive(false);
                camera.transform.position=SitePoint(582,312,35);
                camera.transform.LookAt(SitePoint(582,312,0),Vector3.forward);
                camera.orthographic=true;camera.orthographicSize=8.5f;
                camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.86f,.87f,.85f);
                RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/site-plan-preview.png"));
                AssetDatabase.Refresh();
                // Preview and player test poses are never saved.
            }
            finally
            {
                if(!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.OpenScene(PlanCScene);
            }
        }
    }
}
