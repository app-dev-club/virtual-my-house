using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VirtualHouse.Editor
{
    public static partial class HouseBlockoutGenerator
    {
        private static void ApplyTatamiSouthOpening(Transform first)
        {
            // The south boundary is removable shoji, with a post between the rooms.
            foreach (string name in new[] { "壁_310_367_348_367", "壁_422_367_504_367", "壁_578_367_613_367" })
            {
                Transform wall = first.Find("壁/" + name);
                if (wall != null) Object.DestroyImmediate(wall.gameObject);
            }
            Transform west = first.Find("和室_西_写真ベース/障子・襖・欄間/南側障子_4枚");
            if (west != null)
                foreach (Transform leaf in west.Cast<Transform>().Where(t => t.name.StartsWith("障子_")).ToArray())
                    Object.DestroyImmediate(leaf.gameObject);
            Transform east = first.Find("和室_東_写真ベース/障子・襖・出窓収納");
            foreach (string name in new[] { "南障子_西", "南障子_東", "南無双窓下板" })
            {
                Transform leaf = east == null ? null : east.Find(name);
                if (leaf != null) Object.DestroyImmediate(leaf.gameObject);
            }
            Transform walls = first.Find("壁");
            if (walls.Find("和室間_南端柱") == null)
                CreateBox("和室間_南端柱", PlanPoint(462, 367, WallHeight / 2),
                    new Vector3(.12f, WallHeight, .12f), GetMaterial("LightWood"), walls);
        }

        [MenuItem("Virtual House/Remove South Tatami Shoji All Plans")]
        public static void UpdateTatamiSouthOpening()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                foreach (string path in new[] { OutputScene, RenovatedScene, AlternativeScene, PlanCScene })
                {
                    Materials.Clear();
                    var scene = EditorSceneManager.OpenScene(path);
                    Transform first = GameObject.Find("住宅概形_図面ベース").transform.Find("1階");
                    ApplyTatamiSouthOpening(first);
                    Physics.SyncTransforms();
                    // Both former wall spans and former sliding leaves must be open at body height.
                    foreach (float x in new[] { 320f, 340f, 360f, 400f, 435f, 450f, 475f, 495f, 520f, 560f, 585f, 600f })
                        if (Physics.OverlapBox(PlanPoint(x, 367, 1), new Vector3(.015f, .7f, .015f)).Length != 0)
                            throw new InvalidOperationException("South tatami opening obstructed at " + x);
                    Collider post = first.Find("壁/和室間_南端柱").GetComponent<Collider>();
                    if (!post.bounds.Contains(PlanPoint(462, 367, 1)))
                        throw new InvalidOperationException("Shared south post missing.");
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("TATAMI SOUTH OPENING VALIDATED: " + path);
                }
                AssetDatabase.SaveAssets();
            }
            finally { if (!Application.isBatchMode) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        }
    }
}
