using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeyboardEscapeRecovery
{
    public sealed class RecoveryEditor : EditorWindow
    {
        Vector2 scroll;
        [MenuItem("Recovery/Open map browser")]
        public static void ShowBrowser() { GetWindow<RecoveryEditor>("Recovered maps"); }

        public static void OpenForUser()
        {
            string first = ScenePaths().First(path => path.Contains("SpeedKeyboard_W1"));
            EditorSceneManager.OpenScene(first);
            FrameScene();
            GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            EditorApplication.delayCall += () => EditorApplication.EnterPlaymode();
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Scenes and transforms recovered from the APK. Play starts new free-flight controls; Android gameplay has not been restored. Raw component data is preserved in the adjacent raw-export folder.", MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var scene in ScenePaths())
            {
                if (GUILayout.Button(Path.GetFileNameWithoutExtension(scene)))
                {
                    if (EditorApplication.isPlaying) SceneManager.LoadScene(Path.GetFileNameWithoutExtension(scene));
                    else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene(scene);
                        FrameScene();
                    }
                }
            }
            if (GUILayout.Button("Frame current map in Scene view")) FrameScene();
            EditorGUILayout.EndScrollView();
        }

        public static string[] ScenePaths()
        {
            return AssetDatabase.FindAssets("t:Scene", new[] {"Assets/Recovered"}).Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path.Contains("SpeedKeyboard_W1") ? 0 : path.Contains("SpeedKeyboard_W2") ? 1 : path.Contains("SpeedKeyboard_W3") ? 2 : 3)
                .ThenBy(path => path).ToArray();
        }

        static Bounds SceneBounds()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(root => root.GetComponentsInChildren<MeshFilter>(true).Length > 0).ToArray();
            var bounds = new Bounds(Vector3.zero, Vector3.one * 10);
            if (roots.Length == 0) return bounds;
            bounds = MapExplorer.BoundsFor(roots[0]);
            foreach (var root in roots.Skip(1)) bounds.Encapsulate(MapExplorer.BoundsFor(root));
            return bounds;
        }

        [MenuItem("Recovery/Frame current map")]
        public static void FrameScene()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) sceneView = GetWindow<SceneView>();
            sceneView.Frame(SceneBounds(), false);
        }

        public static void Prepare()
        {
            try
            {
                ConvertMaterials();
                KeyboardGridRecovery.RunContractChecks();
                string[] scenes = ScenePaths();
                if (scenes.Length != 12) throw new InvalidOperationException("Expected 12 recovered source scenes, found " + scenes.Length);
                EditorBuildSettings.scenes = scenes.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
                PlayerSettings.companyName = "Local map research";
                PlayerSettings.productName = "Keyboard Escape map explorer";
                var results = new List<SceneCheck>();
                var grids = new List<KeyboardGridRecovery.Report>();
                Directory.CreateDirectory(Path.GetFullPath("../captures"));
                foreach (string path in scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    grids.Add(KeyboardGridRecovery.Apply(scene));
                    var filters = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true)).ToArray();
                    var renderers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                    var check = new SceneCheck {scene = path, roots = scene.rootCount, meshFilters = filters.Length,
                        missingMeshes = filters.Count(filter => filter.sharedMesh == null),
                        missingMaterials = renderers.Sum(renderer => renderer.sharedMaterials.Count(material => material == null)),
                        missingScripts = scene.GetRootGameObjects().Sum(CountMissingScripts)};
                    results.Add(check);
                    if (check.missingScripts != 0) throw new InvalidOperationException("Unexpected recovered scripts in exploration copy: " + path);
                    if (!path.Contains("Splash") && !path.EndsWith("Home.unity")) Capture(scene.name);
                    Debug.Log("RECOVERED_SCENE: " + JsonUtility.ToJson(check));
                    EditorSceneManager.SaveScene(scene);
                    EditorUtility.UnloadUnusedAssetsImmediate();
                }
                File.WriteAllText(Path.GetFullPath("../analysis/unity-scenes.json"), JsonUtility.ToJson(new Report {scenes = results.ToArray()}, true));
                File.WriteAllText(Path.GetFullPath("../analysis/unity-keyboards.json"), "{\"scenes\":[" + string.Join(",", grids.Select(grid => JsonUtility.ToJson(grid))) + "]}");
                EditorSceneManager.OpenScene(scenes[0]);
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
                Debug.Log("RECOVERY_PREPARE_PASS: " + scenes.Length + " scenes");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        static int CountMissingScripts(GameObject root)
        {
            return root.GetComponentsInChildren<Transform>(true).Sum(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject));
        }

        static void ConvertMaterials()
        {
            Shader shader = Shader.Find("Recovery/MapSurface");
            if (shader == null) throw new InvalidOperationException("Exploration shader missing");
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] {"Assets/Recovered"}))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material == null) continue;
                Texture texture = null;
                Vector2 scale = Vector2.one, offset = Vector2.zero;
                var saved = new SerializedObject(material);
                var textures = saved.FindProperty("m_SavedProperties.m_TexEnvs");
                foreach (string property in new[] {"_BaseMap", "_MainTex", "_SketchPreviewBaseMap", "_Albedo"})
                {
                    for (int index = 0; index < textures.arraySize; index++)
                    {
                        var pair = textures.GetArrayElementAtIndex(index);
                        if (pair.FindPropertyRelative("first").stringValue != property) continue;
                        var value = pair.FindPropertyRelative("second");
                        texture = value.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
                        if (texture == null) continue;
                        scale = value.FindPropertyRelative("m_Scale").vector2Value;
                        offset = value.FindPropertyRelative("m_Offset").vector2Value;
                        break;
                    }
                    if (texture != null) break;
                }
                Color color = Color.white;
                var colors = saved.FindProperty("m_SavedProperties.m_Colors");
                foreach (string property in new[] {"_Color", "_BaseColor"})
                    for (int index = 0; index < colors.arraySize; index++)
                    {
                        var pair = colors.GetArrayElementAtIndex(index);
                        if (pair.FindPropertyRelative("first").stringValue == property) color = pair.FindPropertyRelative("second").colorValue;
                    }
                bool vertex = material.name == "Lego_Vertex" ||
                    (material.HasProperty("_SketchPreviewUseVertexColor") && material.GetFloat("_SketchPreviewUseVertexColor") > .5f);
                material.shader = shader;
                material.SetColor("_Color", color);
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", scale); material.SetTextureOffset("_MainTex", offset);
                material.SetFloat("_VertexColor", vertex ? 1 : 0);
                material.renderQueue = -1;
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }

        public static void RecaptureAndCheck()
        {
            try
            {
                KeyboardGridRecovery.RunContractChecks();
                foreach (string path in ScenePaths())
                {
                    if (path.Contains("Splash") || path.EndsWith("Home.unity")) continue;
                    var scene = EditorSceneManager.OpenScene(path);
                    Capture(scene.name);
                    if (path.Contains("SpeedKeyboard_W1"))
                        foreach (var root in scene.GetRootGameObjects())
                            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                                if (item.name == "W1_Stage_1" || item.name == "W1_Stage_5")
                                {
                                    var originalStates = item.parent.Cast<Transform>().ToDictionary(sibling => sibling.gameObject, sibling => sibling.gameObject.activeSelf);
                                    foreach (var pair in originalStates) pair.Key.SetActive(pair.Key == item.gameObject);
                                    Capture(item.name, MapExplorer.BoundsFor(item.gameObject));
                                    foreach (var pair in originalStates) pair.Key.SetActive(pair.Value);
                                }
                    EditorUtility.UnloadUnusedAssetsImmediate();
                }
                ExplorerChecks.Run();
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        static void Capture(string name, Bounds? focused = null)
        {
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)) canvas.enabled = false;
            foreach (var original in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None)) original.enabled = false;
            var cameraObject = new GameObject("[verification] camera");
            var camera = cameraObject.AddComponent<Camera>();
            var bounds = focused ?? SceneBounds();
            camera.transform.position = MapExplorer.OverviewPosition(bounds);
            camera.transform.LookAt(bounds.center);
            camera.nearClipPlane = .05f; camera.farClipPlane = 100000;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.14f,.2f,.28f);
            SaveCameraImage(camera, name);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        public static void SaveCameraImage(Camera camera, string name)
        {
            var target = new RenderTexture(1280, 800, 24);
            camera.targetTexture = target;
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(1280, 800, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0,0,1280,800),0,0); image.Apply();
            File.WriteAllBytes(Path.GetFullPath("../captures/" + name + ".png"), image.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }

        [Serializable] public sealed class SceneCheck { public string scene; public int roots, meshFilters, missingMeshes, missingMaterials, missingScripts; }
        [Serializable] public sealed class Report { public SceneCheck[] scenes; }
    }
}
