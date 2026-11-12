using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KeyboardEscapeRecovery
{
    public static class GeometryDiagnostics
    {
        [Serializable] public class Item { public string path, mesh; public Vector3 position; public Bounds bounds, meshBounds; public int vertices, submeshes; }
        [Serializable] public class Report { public Bounds stage1Bounds; public Item[] largestRenderers; }
        public static void Run()
        {
            var path = RecoveryEditor.ScenePaths().First(value => value.Contains("SpeedKeyboard_W1"));
            var scene = EditorSceneManager.OpenScene(path);
            var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            var stage = all.First(value => value.name == "W1_Stage_1");
            var items = all.Select(value => value.GetComponent<MeshFilter>()).Where(filter => filter && filter.sharedMesh && filter.GetComponent<Renderer>())
                .Select(filter => new Item {path = FullPath(filter.transform), mesh = AssetDatabase.GetAssetPath(filter.sharedMesh), position = filter.transform.position,
                    bounds = filter.GetComponent<Renderer>().bounds, meshBounds = filter.sharedMesh.bounds, vertices = filter.sharedMesh.vertexCount, submeshes = filter.sharedMesh.subMeshCount})
                .OrderByDescending(item => item.bounds.size.sqrMagnitude).Take(25).ToArray();
            File.WriteAllText(Path.GetFullPath("../analysis/geometry-diagnostics.json"), JsonUtility.ToJson(new Report {stage1Bounds = MapExplorer.BoundsFor(stage.gameObject), largestRenderers = items}, true));
            Debug.Log("GEOMETRY_DIAGNOSTICS_WRITTEN");
            RecoverySmoke.Run();
        }
        static string FullPath(Transform item) { return item.parent == null ? item.name : FullPath(item.parent) + "/" + item.name; }
    }
}
