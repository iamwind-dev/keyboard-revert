using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KeyboardEscapeRecovery
{
    public static class ExplorerChecks
    {
        public static void Run()
        {
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var type = Type.GetType("KeyboardEscapeRecovery.MapExplorer, Assembly-CSharp");
                Require(type != null, "MapExplorer is missing; recovered maps cannot yet be explored in Play mode.");
                var root = new GameObject("Test recovered map");
                var first = GameObject.CreatePrimitive(PrimitiveType.Cube);
                first.transform.SetParent(root.transform);
                first.transform.position = new Vector3(10, 2, 0);
                first.transform.localScale = new Vector3(4, 2, 6);
                var second = GameObject.CreatePrimitive(PrimitiveType.Cube);
                second.transform.SetParent(root.transform);
                second.transform.position = new Vector3(30, 2, 0);
                second.SetActive(false);
                var boundsMethod = type.GetMethod("BoundsFor", BindingFlags.Public | BindingFlags.Static);
                Require(boundsMethod != null, "Map bounds API is missing.");
                var bounds = (Bounds)boundsMethod.Invoke(null, new object[] {root});
                Require(bounds.Contains(new Vector3(8, 1, -3)), "Bounds must include the first mesh at its source transform.");
                Require(bounds.Contains(new Vector3(30.5f, 2.5f, .5f)), "Bounds must include inactive map geometry for exploration.");
                var frameMethod = type.GetMethod("OverviewPosition", BindingFlags.Public | BindingFlags.Static);
                Require(frameMethod != null, "Camera framing API is missing.");
                var position = (Vector3)frameMethod.Invoke(null, new object[] {bounds});
                Require(!bounds.Contains(position), "Overview camera must be outside map geometry.");
                Require(float.IsFinite(position.x + position.y + position.z), "Overview camera must remain finite.");
                var empty = new GameObject("Empty scene root");
                var emptyBounds = (Bounds)boundsMethod.Invoke(null, new object[] {empty});
                Require(emptyBounds.size.sqrMagnitude > 0, "Empty scene framing needs a usable fallback.");
                var explorerObject = new GameObject("Test explorer");
                var explorer = explorerObject.AddComponent(type);
                type.GetMethod("Install", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(explorer, null);
                second.GetComponent<Renderer>().enabled = false;
                type.GetMethod("ToggleInactiveGeometry").Invoke(explorer, null);
                Require(second.activeSelf && second.GetComponent<Renderer>().enabled, "Opt-in exploration must show inactive and disabled geometry.");
                type.GetMethod("ToggleInactiveGeometry").Invoke(explorer, null);
                Require(!second.activeSelf && !second.GetComponent<Renderer>().enabled, "Restoring visibility must preserve original inactive/disabled states.");
                var third = GameObject.CreatePrimitive(PrimitiveType.Cube);
                third.transform.SetParent(root.transform);
                type.GetMethod("InspectStage").Invoke(explorer, new object[] {first});
                Require(first.activeSelf && !third.activeSelf, "Inspecting a stage must hide sibling geometry.");
                type.GetMethod("FrameAll").Invoke(explorer, null);
                Require(third.activeSelf && !second.activeSelf, "Map overview must restore active and inactive sibling states.");
                UnityEngine.Object.DestroyImmediate(explorerObject);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(empty);
                File.WriteAllText(Path.GetFullPath("../analysis/explorer-checks.json"), "{\"passed\":true,\"checks\":9}");
                Debug.Log("EXPLORER_CHECKS_PASS: 9 assertions");
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogError("EXPLORER_CHECKS_FAIL: " + error);
                EditorApplication.Exit(1);
            }
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
