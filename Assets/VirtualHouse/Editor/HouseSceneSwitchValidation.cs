using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace VirtualHouse.Editor
{
    // Explicit batch entry point; no work occurs in ordinary editor sessions.
    public static class HouseSceneSwitchValidation
    {
        private const string Pending = "VirtualHouse.SwitchValidation";
        private static int stage;
        private static double deadline;
        private static int settledFrames;
        private static Touchscreen testTouch;
        private static Mouse testMouse;

        public static void RunTouch()
        {
            SessionState.SetBool(Pending + ".Touch", true);
            Run();
        }

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
                string expected = stage == 1 ? HouseSceneSwitcher.After : stage == 2 ? HouseSceneSwitcher.Alternative : HouseSceneSwitcher.Before;
                if (SceneManager.GetActiveScene().name != expected) { settledFrames = 0; return; }
                bool touchMode = SessionState.GetBool(Pending + ".Touch", false);
                if (touchMode && testTouch == null)
                {
                    InputSystem.settings = UnityEngine.Object.Instantiate(InputSystem.settings);
                    InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                    InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    testTouch = InputSystem.AddDevice<Touchscreen>();
                    testMouse = InputSystem.AddDevice<Mouse>();
                }
                Rect area = (Rect)typeof(HouseSceneSwitcher).GetProperty("ButtonRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
                Vector2 position = new Vector2(area.center.x, Screen.height - area.center.y);
                ++settledFrames;
                if (touchMode && stage > 0 && settledFrames == 2)
                {
                    InputSystem.QueueStateEvent(testTouch,new TouchState {touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=position});
                    InputSystem.QueueStateEvent(testMouse,new MouseState {position=position}.WithButton(MouseButton.Left));
                }
                if (touchMode && stage > 0 && settledFrames == 3)
                    InputSystem.QueueStateEvent(testMouse,new MouseState {position=position});
                if (touchMode)
                {
                    InputSystem.Update();
                    switcher.SendMessage("Update", SendMessageOptions.RequireReceiver);
                }
                if (settledFrames < 15) return;
                settledFrames = 0;
                var player = UnityEngine.Object.FindFirstObjectByType<HouseFirstPersonController>();
                Vector3 comparisonPosition = new Vector3((670f-158f)*1.82f/75f, 0, (423f-328f)*1.82f/75f);
                if (stage == 0)
                    player.RestoreScenePose(comparisonPosition, Quaternion.Euler(0,123,0),20);
                else if (Vector3.Distance(player.transform.position, comparisonPosition) > .12f
                    || Quaternion.Angle(player.transform.rotation, Quaternion.Euler(0,123,0)) > .1f
                    || Mathf.Abs(player.ViewPitch-20) > .1f)
                    throw new Exception("Player position or view changed across scene switch: " + player.transform.position);
                if (stage == 1 && (GameObject.Find("脱衣所_残す柱") == null || GameObject.Find("洗濯機") == null))
                    throw new Exception("Renovated fixtures are not active at runtime");
                if (stage == 2)
                {
                    if (GameObject.Find("壁_688_254_746_254") == null || GameObject.Find("壁_688_291_764_291") != null)
                        throw new Exception("Alternative dressing layout is not active");
                }
                if (stage == 3)
                {
                    if (GameObject.Find("壁_651_141_651_179") == null) throw new Exception("Original WC partition was not restored");
                    Debug.Log("SCENE SWITCH VALIDATION PASSED: original -> plan A -> plan B -> original, player position/yaw/pitch preserved, fixtures active, original partition restored.");
                    Finish(0);
                    return;
                }
                if (touchMode)
                {
                    InputSystem.QueueStateEvent(testTouch,new TouchState {touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=position});
                    InputSystem.Update();
                    Debug.Log($"Touch switch stage {stage}: screen {Screen.width}x{Screen.height}, button {area}, hit {HouseSceneSwitcher.IsSwitchArea(position)}, pressed {testTouch.primaryTouch.press.wasPressedThisFrame}");
                    switcher.SendMessage("Update", SendMessageOptions.RequireReceiver);
                }
                else switcher.SendMessage("Switch", SendMessageOptions.RequireReceiver);
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
            SessionState.SetBool(Pending + ".Touch", false);
            EditorApplication.update -= Tick;
            EditorApplication.Exit(code);
        }
    }
}
