using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeyboardEscapeRecovery
{
    [InitializeOnLoad]
    public static class RecoverySmoke
    {
        const string Running = "RecoverySmoke.Running";
        const string Current = "RecoverySmoke.Current";
        const string Completed = "RecoverySmoke.Completed";
        const string Failed = "RecoverySmoke.Failed";
        static double next;
        static readonly string[] Scenes = {"Minigame_SpeedKeyboard_W1_Optimized", "MiniGame_EasyObby", "Minigame_PaintKeyboard"};

        static RecoverySmoke()
        {
            EditorApplication.playModeStateChanged += StateChanged;
            EditorApplication.update += Update;
            Application.logMessageReceived += (message, stack, type) =>
            {
                if (SessionState.GetBool(Running, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                    SessionState.SetBool(Failed, true);
            };
        }

        public static void Run()
        {
            var vertexMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Recovered/Material/Lego_Vertex.mat");
            if (!vertexMaterial) throw new InvalidOperationException("Original LEGO vertex-color material missing.");
            vertexMaterial.SetFloat("_VertexColor", 1);
            EditorUtility.SetDirty(vertexMaterial);
            AssetDatabase.SaveAssets();
            SessionState.SetBool(Running, true);
            SessionState.SetInt(Current, 0);
            SessionState.SetInt(Completed, 0);
            SessionState.SetBool(Failed, false);
            string path = RecoveryEditor.ScenePaths().First(value => value.Contains(Scenes[0]));
            EditorSceneManager.OpenScene(path);
            EditorApplication.EnterPlaymode();
        }

        static void StateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Running, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) next = EditorApplication.timeSinceStartup + 3;
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Running, false);
                if (SessionState.GetInt(Completed, 0) != Scenes.Length || SessionState.GetBool(Failed, false))
                {
                    Debug.LogError("RECOVERY_PLAYMODE_FAIL: interrupted or runtime errors occurred.");
                    EditorApplication.Exit(1);
                    return;
                }
                File.WriteAllText(Path.GetFullPath("../analysis/playmode-smoke.json"), "{\"passed\":true,\"scenes\":[\"W1\",\"EasyObby\",\"PaintKeyboard\"],\"checks\":\"scene switching, explorer instance, enabled camera, finite camera pose\"}");
                Debug.Log("RECOVERY_PLAYMODE_PASS: W1 -> EasyObby -> PaintKeyboard");
                EditorApplication.Exit(0);
            }
        }

        static void Update()
        {
            if (!SessionState.GetBool(Running, false) || !EditorApplication.isPlaying || next == 0 || EditorApplication.timeSinceStartup < next) return;
            next = 0;
            try
            {
                var explorers = UnityEngine.Object.FindObjectsByType<MapExplorer>(FindObjectsSortMode.None);
                if (explorers.Length != 1) throw new InvalidOperationException("Expected exactly one persistent map explorer.");
                var camera = explorers[0].GetComponentInChildren<Camera>();
                if (camera == null || !camera.enabled || !float.IsFinite(camera.transform.position.sqrMagnitude)) throw new InvalidOperationException("Exploration camera invalid.");
                int index = SessionState.GetInt(Current, 0);
                if (SceneManager.GetActiveScene().name != Scenes[index]) throw new InvalidOperationException("Scene transition failed.");
                Debug.Log("RECOVERY_PLAYMODE_SCENE_PASS: " + Scenes[index]);
                if (index == 0) RecoveryEditor.SaveCameraImage(camera, "W1_playmode");
                SessionState.SetInt(Completed, index + 1);
                if (++index == Scenes.Length) EditorApplication.ExitPlaymode();
                else
                {
                    SessionState.SetInt(Current, index);
                    SceneManager.LoadScene(Scenes[index]);
                    next = EditorApplication.timeSinceStartup + 3;
                }
            }
            catch (Exception error)
            {
                SessionState.SetBool(Running, false);
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }
    }
}
