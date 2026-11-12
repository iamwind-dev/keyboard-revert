using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeyboardEscapeRecovery
{
    /// <summary>New exploration controls. This does not implement the recovered Android gameplay.</summary>
    public sealed class MapExplorer : MonoBehaviour
    {
        static MapExplorer instance;
        Camera view;
        readonly List<GameObject> roots = new List<GameObject>();
        readonly List<GameObject> landmarks = new List<GameObject>();
        bool menu = true;
        float speed = 25;
        Vector2 scroll;
        string search = "";
        int meshCount;
        readonly Dictionary<GameObject, bool> activationChanges = new Dictionary<GameObject, bool>();
        readonly Dictionary<Renderer, bool> rendererChanges = new Dictionary<Renderer, bool>();
        bool showInactive;
        readonly Dictionary<GameObject, bool> isolatedStates = new Dictionary<GameObject, bool>();
        string isolatedStage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartExplorer()
        {
            if (IsGameplayScene(SceneManager.GetActiveScene())) return;
            if (instance != null) return;
            instance = new GameObject("[recovery] map explorer").AddComponent<MapExplorer>();
            DontDestroyOnLoad(instance.gameObject);
            SceneManager.sceneLoaded += instance.SceneLoaded;
            instance.Install();
        }

        void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsGameplayScene(scene)) { Destroy(gameObject); return; }
            Install();
        }

        public static bool IsGameplayScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<KeyboardEscapeGameplay.World1GameplayScene>() != null) return true;
            return false;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            if (instance == this) instance = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Install()
        {
            roots.Clear();
            landmarks.Clear();
            meshCount = 0;
            activationChanges.Clear(); rendererChanges.Clear(); showInactive = false;
            isolatedStates.Clear(); isolatedStage = null;
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (camera != view) camera.enabled = false;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)) canvas.enabled = false;
            foreach (var audio in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None)) audio.enabled = false;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None)) listener.enabled = false;
            foreach (var body in FindObjectsByType<Rigidbody>(FindObjectsInactive.Include, FindObjectsSortMode.None)) body.isKinematic = true;
            foreach (var animator in FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None)) animator.enabled = false;
            foreach (var particles in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var settings = particles.main; settings.playOnAwake = false;
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                roots.Add(root);
                meshCount += root.GetComponentsInChildren<MeshFilter>(true).Length;
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    string name = transform.name.ToLowerInvariant();
                    if ((name.Contains("checkpoint") || name.StartsWith("stage") || name.StartsWith("zone") ||
                         name.Contains("_stage_") || name.EndsWith("lobby") || name.Contains("floor ")) &&
                        transform.GetComponentsInChildren<Renderer>(true).Length > 0)
                        landmarks.Add(transform.gameObject);
                }
            }
            if (view == null)
            {
                var cameraObject = new GameObject("[recovery] exploration camera");
                cameraObject.transform.SetParent(transform);
                view = cameraObject.AddComponent<Camera>();
                view.tag = "MainCamera";
            }
            view.enabled = true;
            view.nearClipPlane = .05f;
            view.farClipPlane = 100000;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.14f, .2f, .28f);
            view.fieldOfView = 60;
            view.cullingMask = ~0;
            FrameAll();
            foreach (var landmark in landmarks)
                if (landmark.name.EndsWith("_Stage_1", StringComparison.OrdinalIgnoreCase)) { InspectStage(landmark); break; }
        }

        public static Bounds BoundsFor(GameObject root)
        {
            bool found = false;
            Bounds bounds = new Bounds(root.transform.position, Vector3.one * 10);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                var next = renderer.bounds;
                if (!float.IsFinite(next.center.sqrMagnitude + next.size.sqrMagnitude)) continue;
                if (!found) { bounds = next; found = true; }
                else bounds.Encapsulate(next);
            }
            return bounds;
        }

        public static Vector3 OverviewPosition(Bounds bounds)
        {
            float distance = Mathf.Max(bounds.size.magnitude * 1.15f, 12);
            return bounds.center + new Vector3(.65f, .8f, -1).normalized * distance;
        }

        public void Frame(GameObject root)
        {
            FrameBounds(BoundsFor(root));
        }

        public void FrameAll()
        {
            RestoreStageVisibility();
            bool found = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 10);
            foreach (var root in roots)
            {
                if (root.GetComponentsInChildren<MeshFilter>(true).Length == 0) continue;
                if (!found) { bounds = BoundsFor(root); found = true; }
                else bounds.Encapsulate(BoundsFor(root));
            }
            FrameBounds(bounds);
        }

        void FrameBounds(Bounds bounds)
        {
            view.transform.position = OverviewPosition(bounds);
            view.transform.LookAt(bounds.center);
            speed = Mathf.Clamp(bounds.size.magnitude * .1f, 5, 1000);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) menu = !menu;
            if (Input.GetKeyDown(KeyCode.Escape)) { menu = true; Cursor.lockState = CursorLockMode.None; }
            bool looking = Input.GetMouseButton(1);
            Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !looking;
            if (menu && !looking) return;
            if (Input.GetKeyDown(KeyCode.F)) FrameAll();
            if (looking)
            {
                var angles = view.transform.eulerAngles;
                float pitch = angles.x > 180 ? angles.x - 360 : angles.x;
                view.transform.rotation = Quaternion.Euler(Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * 2, -89, 89), angles.y + Input.GetAxisRaw("Mouse X") * 2, 0);
            }
            float forward = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
            float side = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
            float up = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0);
            var move = view.transform.forward * forward + view.transform.right * side + Vector3.up * up;
            speed = Mathf.Clamp(speed * Mathf.Pow(1.3f, Input.mouseScrollDelta.y), 1, 5000);
            view.transform.position += Vector3.ClampMagnitude(move, 1) * speed * (Input.GetKey(KeyCode.LeftShift) ? 4 : 1) * Time.unscaledDeltaTime;
        }

        void OnGUI()
        {
            GUI.Label(new Rect(12, Screen.height - 30, Screen.width - 24, 24), "Recovered map geometry | RMB look + WASD | Q/E down/up | Shift fast | wheel speed | F overview | Tab menu");
            if (!menu) return;
            GUILayout.BeginArea(new Rect(10, 10, 390, Mathf.Max(120, Screen.height - 55)), GUI.skin.box);
            GUILayout.Label("Keyboard Escape - map architecture explorer");
            GUILayout.Label(SceneManager.GetActiveScene().name + " | " + meshCount + " mesh objects");
            GUILayout.Label("Original geometry; new free-flight controls. Gameplay is not restored.");
            if (GUILayout.Button("Frame entire map")) FrameAll();
            if (isolatedStage != null) GUILayout.Label("Isolated stage: " + isolatedStage + " (overview restores all stages)");
            if (GUILayout.Button(showInactive ? "Restore source visibility" : "Show inactive geometry (temporary)")) ToggleInactiveGeometry();
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("Source scenes");
            for (int index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(index);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (GUILayout.Button(sceneName)) { SceneManager.LoadScene(index); break; }
            }
            GUILayout.Space(10);
            GUILayout.Label("Focus source hierarchy / checkpoints");
            search = GUILayout.TextField(search);
            foreach (var root in roots)
                if (Matches(root) && GUILayout.Button("root: " + root.name)) Frame(root);
            int shown = 0;
            foreach (var item in landmarks)
            {
                if (item == null || !Matches(item)) continue;
                if (++shown > 100) { GUILayout.Label("Filter to narrow the landmark list."); break; }
                if (GUILayout.Button(item.name))
                {
                    if (item.name.Contains("_Stage_") || item.name.EndsWith("Lobby")) InspectStage(item);
                    else Frame(item);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        bool Matches(GameObject item) { return string.IsNullOrEmpty(search) || item.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0; }

        public void ToggleInactiveGeometry()
        {
            RestoreStageVisibility();
            if (!showInactive)
            {
                foreach (var root in roots)
                    foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                        if (!renderer.enabled) { rendererChanges[renderer] = false; renderer.enabled = true; }
                        for (var current = renderer.transform; current != null; current = current.parent)
                            if (!current.gameObject.activeSelf)
                            {
                                activationChanges[current.gameObject] = false;
                                current.gameObject.SetActive(true);
                            }
                    }
            }
            else
            {
                foreach (var pair in rendererChanges) if (pair.Key != null) pair.Key.enabled = pair.Value;
                foreach (var pair in activationChanges) if (pair.Key != null) pair.Key.SetActive(pair.Value);
                rendererChanges.Clear(); activationChanges.Clear();
            }
            showInactive = !showInactive;
        }

        public void InspectStage(GameObject stage)
        {
            if (showInactive) ToggleInactiveGeometry();
            RestoreStageVisibility();
            if (stage.transform.parent != null)
                foreach (Transform sibling in stage.transform.parent)
                {
                    isolatedStates[sibling.gameObject] = sibling.gameObject.activeSelf;
                    sibling.gameObject.SetActive(sibling.gameObject == stage);
                }
            isolatedStage = stage.name;
            Frame(stage);
        }

        void RestoreStageVisibility()
        {
            foreach (var pair in isolatedStates) if (pair.Key != null) pair.Key.SetActive(pair.Value);
            isolatedStates.Clear(); isolatedStage = null;
        }
    }
}
