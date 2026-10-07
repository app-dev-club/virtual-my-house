using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirtualHouse.Editor
{
    // Explicit batch entry point; no work occurs in ordinary editor sessions.
    public static class HouseSceneSwitchValidation
    {
        private const string Pending = "VirtualHouse.SwitchValidation";
        private static int stage;
        private static double deadline;
        private static int settledFrames;

        [InitializeOnLoadMethod]
        private static void Resume()
        {
            if (!SessionState.GetBool(Pending, false)) return;
            deadline = EditorApplication.timeSinceStartup + 90;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/HouseBlockout.unity");
            SessionState.SetBool(Pending, true);
            Resume();
            EditorApplication.EnterPlaymode();
        }

        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Scene switch validation timed out");
                if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
                var switcher = UnityEngine.Object.FindFirstObjectByType<HouseSceneSwitcher>();
                if (switcher == null) return;
                string expected = stage == 1 ? HouseSceneSwitcher.After : HouseSceneSwitcher.Before;
                if (SceneManager.GetActiveScene().name != expected) { settledFrames = 0; return; }
                if (++settledFrames < 15) return;
                settledFrames = 0;
                if (stage == 1 && (GameObject.Find("脱衣所_残す柱") == null || GameObject.Find("洗濯機") == null))
                    throw new Exception("Renovated fixtures are not active at runtime");
                if (stage == 2)
                {
                    if (GameObject.Find("壁_651_141_651_179") == null) throw new Exception("Original WC partition was not restored");
                    Debug.Log("SCENE SWITCH VALIDATION PASSED: original -> renovated -> original, fixtures active, original partition restored.");
                    Finish(0);
                    return;
                }
                switcher.SendMessage("Switch", SendMessageOptions.RequireReceiver);
                stage++;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Finish(1);
            }
        }

        private static void Finish(int code)
        {
            SessionState.SetBool(Pending, false);
            EditorApplication.update -= Tick;
            EditorApplication.Exit(code);
        }
    }
}
