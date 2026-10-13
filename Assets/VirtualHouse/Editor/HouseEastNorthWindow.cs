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
        [MenuItem("Virtual House/Update East Room North Window All Plans")]
        public static void UpdateEastRoomNorthWindowAllPlans()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach(string path in new[]{OutputScene,RenovatedScene,AlternativeScene,PlanCScene})
                {
                    Materials.Clear();
                    var scene=EditorSceneManager.OpenScene(path);
                    Transform house=scene.GetRootGameObjects().First(g=>g.name=="住宅概形_図面ベース").transform;
                    Transform fittings=house.Find("1階/和室_東_写真ベース/障子・襖・出窓収納");
                    foreach(string name in new[]{"北出窓収納","北側引戸"})
                    {
                        Transform old=fittings.Find(name);
                        if(old!=null) Object.DestroyImmediate(old.gameObject);
                    }
                    CreateNorthBayStorage(fittings,PlanPoint(462,216,0).x,PlanPoint(613,216,0).x,PlanPoint(462,216,0).z);
                    Transform trim=house.Find("外観_写真ベース/腰壁と窓");
                    if(trim.Find("東和室北収納窓")==null)
                        PhotoWindow(trim,"東和室北収納窓",499.5f,174,66,.91f,1.08f,2);
                    Physics.SyncTransforms();
                    // Check the view through both leaves, avoiding mullions and fine bars.
                    foreach(float x in new[]{484f,514f})
                    {
                        Vector3 origin=PlanPoint(x,208,1.4f);
                        foreach(var hit in Physics.RaycastAll(origin,Vector3.forward,1f))
                            if(hit.collider.name.StartsWith("壁_") || hit.collider.name.StartsWith("濃灰色"))
                                throw new InvalidOperationException("East north window blocked by "+hit.collider.name);
                    }
                    var bay=fittings.Find("北出窓収納");
                    if(bay.Find("収納天板").GetComponent<Collider>().bounds.min.z<PlanPoint(462,216,0).z)
                        throw new InvalidOperationException("Storage projects into tatami room");
                    EditorSceneManager.SaveScene(scene);
                    foreach(var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)) label.GetComponent<Renderer>().enabled=false;
                    Camera camera=Camera.main;camera.transform.position=PlanPoint(508,289,1.6f);
                    camera.transform.LookAt(PlanPoint(510,198,1.15f));camera.fieldOfView=67;
                    RenderCameraToPng(camera,Path.Combine(Application.dataPath,"VirtualHouse/east-north-window-"+Path.GetFileNameWithoutExtension(path)+".png"));
                    Debug.Log("East north window opening and storage bounds passed: "+path);
                }
                AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            }
            finally { if(!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
