#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace KeyboardEscapeRecovery
{
    // Display reconstruction from serialized grid positions, not recovered native gameplay.
    public static class KeyboardGridRecovery
    {
        const string ManifestPath = "Assets/Recovery/keyboard-grids.json";
        const string PrefabPath = "Assets/Recovered/GameObject/M_Key.prefab";
        const string OutputRoot = "Assets/Recovery/Generated";
        const string ChildName = "[reconstructed grid] keys";
        [Serializable] public sealed class Manifest { public Grid[] keyboards; }
        [Serializable] public sealed class Grid
        {
            public string scenePath, hierarchyPath;
            public long gameObjectFileId;
            public int layoutMode, keyCount;
            public Vector2Int fieldSize;
            public Vector3 topLeft;
            public Vector2 step;
            public Color[] colors, presetColors;
        }
        [Serializable] public sealed class Report
        {
            public string scenePath;
            public int grids, keys, skippedSurfaces, generatedMeshes;
            public string palettePolicy = "Exact preset colors when present; otherwise first original palette color. No seeded letters/colors reconstructed.";
            public string placementAssumption = "Grid x=topLeft.x+column*step.x; z=topLeft.z-row*step.y. Surface layouts are not reconstructed.";
        }

        public static Report Apply(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("A loaded saved scene is required.");
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            if (manifest?.keyboards == null) throw new InvalidDataException("Keyboard manifest is empty.");
            var selected = manifest.keyboards.Where(g => g.scenePath == scene.path).ToArray();
            var report = new Report { scenePath = scene.path, skippedSurfaces = selected.Count(g => g.layoutMode == 1) };
            var grids = selected.Where(g => g.layoutMode == 0).ToArray();
            if (selected.Any(g => g.layoutMode != 0 && g.layoutMode != 1)) throw new InvalidDataException("Unknown keyboard layout mode.");
            if (grids.Length == 0) return report;
            var objects = new Dictionary<long, GameObject>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    long id = checked((long)GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).targetObjectId);
                    if (id != 0) objects[id] = t.gameObject;
                }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (!prefab) throw new FileNotFoundException("Original key prefab not found.", PrefabPath);
            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length != 1 || !filters[0].sharedMesh) throw new InvalidDataException("Expected exactly one original key mesh.");
            var source = filters[0].sharedMesh;
            if (!source.isReadable) throw new InvalidOperationException("Original key mesh is not readable: " + AssetDatabase.GetAssetPath(source));
            var template = source.vertices;
            var frame = Matrix4x4.identity;
            for (var t = filters[0].transform; t != null; t = t.parent)
            {
                frame = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale) * frame;
                if (t == prefab.transform) break; // Includes root scale 2 and mesh-child scale 3.
            }
            for (int i = 0; i < template.Length; i++) template[i] = frame.MultiplyPoint3x4(template[i]);
            string folder = OutputRoot + "/" + Path.GetFileNameWithoutExtension(scene.path);
            foreach (var grid in grids)
            {
                ValidateGrid(grid);
                if (!objects.ContainsKey(grid.gameObjectFileId)) throw new InvalidDataException("Unmatched original GameObject ID " + grid.gameObjectFileId + ": " + grid.hierarchyPath);
                var matches = objects[grid.gameObjectFileId].transform.Cast<Transform>().Where(t => t.name == ChildName).ToArray();
                if (matches.Length > 1) throw new InvalidDataException("Duplicate recovery children: " + grid.hierarchyPath);
                if (matches.Length == 1)
                {
                    var filter = matches[0].GetComponent<MeshFilter>();
                    string expected = folder + "/Grid_" + grid.gameObjectFileId + ".asset";
                    if (!filter || !filter.sharedMesh || AssetDatabase.GetAssetPath(filter.sharedMesh) != expected)
                        throw new InvalidDataException("Existing child is not an owned recovery mesh: " + grid.hierarchyPath);
                }
            }
            EnsureFolder(folder);
            var material = GetMaterial(folder, filters[0].GetComponent<Renderer>()?.sharedMaterial);
            foreach (var grid in grids)
            {
                var mesh = BuildGridMesh(grid, template, source, frame);
                ValidateMesh(grid, mesh, template);
                string path = folder + "/Grid_" + grid.gameObjectFileId + ".asset";
                var existing = AssetDatabase.LoadMainAssetAtPath(path);
                if (existing && !(existing is Mesh)) throw new InvalidDataException("Recovery mesh path contains a different asset: " + path);
                if (existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = (Mesh)existing; EditorUtility.SetDirty(mesh); }
                else AssetDatabase.CreateAsset(mesh, path);
                var parent = objects[grid.gameObjectFileId].transform;
                var child = parent.Cast<Transform>().FirstOrDefault(t => t.name == ChildName);
                if (!child) { var go = new GameObject(ChildName); child = go.transform; child.SetParent(parent, false); }
                child.localPosition = Vector3.zero; child.localRotation = Quaternion.identity; child.localScale = Vector3.one;
                var mf = child.GetComponent<MeshFilter>();
                if (!mf) mf = child.gameObject.AddComponent<MeshFilter>();
                var mr = child.GetComponent<MeshRenderer>();
                if (!mr) mr = child.gameObject.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh; mr.sharedMaterial = material;
                report.grids++; report.keys = checked(report.keys + grid.keyCount); report.generatedMeshes++;
            }
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Keyboard grid recovery: " + JsonUtility.ToJson(report));
            return report;
        }

        public static void ValidateGrid(Grid g)
        {
            if (g == null || g.fieldSize.x <= 0 || g.fieldSize.y <= 0 || g.keyCount != (long)g.fieldSize.x * g.fieldSize.y)
                throw new InvalidDataException("Grid dimensions/key count disagree.");
            if (!Finite(g.topLeft.x) || !Finite(g.topLeft.y) || !Finite(g.topLeft.z) || !Finite(g.step.x) || !Finite(g.step.y) || g.step.x <= 0 || g.step.y <= 0)
                throw new InvalidDataException("Grid coordinates/step are invalid.");
            if (g.presetColors != null && g.presetColors.Length != 0 && g.presetColors.Length != g.keyCount)
                throw new InvalidDataException("Preset colors/key count disagree.");
        }
        static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        static Vector3 Position(Grid g, int key) => g.topLeft + new Vector3((key % g.fieldSize.x) * g.step.x, 0, -(key / g.fieldSize.x) * g.step.y);
        static Mesh BuildGridMesh(Grid g, Vector3[] template, Mesh source, Matrix4x4 frame)
        {
            int n = template.Length, total = checked(n * g.keyCount);
            var vertices = new Vector3[total]; var colors = new Color[total]; var uv = new Vector2[total];
            var sourceUv = source.uv; var sourceNormals = source.normals; var normals = sourceNormals.Length == n ? new Vector3[total] : null;
            var sourceTriangles = source.triangles; var triangles = new int[checked(sourceTriangles.Length * g.keyCount)];
            var normalFrame = frame.inverse.transpose;
            Color fallback = g.colors != null && g.colors.Length > 0 ? g.colors[0] : Color.white;
            for (int key = 0; key < g.keyCount; key++)
            {
                var origin = Position(g, key); int start = key * n;
                Color color = g.presetColors != null && g.presetColors.Length == g.keyCount ? g.presetColors[key] : fallback;
                for (int j = 0; j < n; j++)
                {
                    vertices[start+j] = template[j] + origin; colors[start+j] = color;
                    if (sourceUv.Length == n) uv[start+j] = sourceUv[j];
                    if (normals != null) normals[start+j] = normalFrame.MultiplyVector(sourceNormals[j]).normalized;
                }
                for (int j = 0; j < sourceTriangles.Length; j++) triangles[key * sourceTriangles.Length+j] = start + sourceTriangles[j];
            }
            var mesh = new Mesh { name = "ReconstructedGrid_" + g.gameObjectFileId, indexFormat = total > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = vertices; mesh.colors = colors; mesh.uv = uv; mesh.triangles = triangles;
            if (normals != null) mesh.normals = normals; else mesh.RecalculateNormals();
            mesh.RecalculateBounds(); return mesh;
        }
        static void ValidateMesh(Grid g, Mesh mesh, Vector3[] template)
        {
            if (mesh.vertexCount != checked(template.Length*g.keyCount)) throw new InvalidDataException("Generated vertex count mismatch.");
            var vertices = mesh.vertices; var first = Position(g, 0); var last = Position(g, g.keyCount-1);
            if ((vertices[0]-(template[0]+first)).sqrMagnitude > 1e-8f || (vertices[(g.keyCount-1)*template.Length]-(template[0]+last)).sqrMagnitude > 1e-8f)
                throw new InvalidDataException("Generated grid endpoints mismatch.");
            var expected = new Bounds(template[0]+first, Vector3.zero);
            foreach (var vertex in template) { expected.Encapsulate(vertex+first); expected.Encapsulate(vertex+last); }
            if ((mesh.bounds.center-expected.center).sqrMagnitude > 1e-5f || (mesh.bounds.size-expected.size).sqrMagnitude > 1e-5f)
                throw new InvalidDataException("Generated grid bounds mismatch.");
        }
        static void EnsureFolder(string folder)
        {
            string current = "Assets";
            foreach (string part in folder.Split('/').Skip(1)) { string next = current+"/"+part; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part); current = next; }
        }
        static Material GetMaterial(string folder, Material original)
        {
            string path = folder+"/Grid.mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Recovery/MapSurface"); if (!shader) throw new InvalidOperationException("Recovery/MapSurface shader not found.");
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader; material.SetFloat("_VertexColor", 1); material.SetColor("_Color", Color.white);
            Texture texture = original && original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : original && original.HasProperty("_MainTex") ? original.GetTexture("_MainTex") : null;
            material.SetTexture("_MainTex", texture); EditorUtility.SetDirty(material); return material;
        }

        // Invoke before Apply in Unity; independent expected positions + invalid-data cases.
        public static void RunContractChecks()
        {
            var grid = new Grid { fieldSize = new Vector2Int(3,2), keyCount = 6, topLeft = new Vector3(-.9f,0,.45f), step = new Vector2(.9f,.9f) };
            ValidateGrid(grid);
            if ((Position(grid,0)-new Vector3(-.9f,0,.45f)).sqrMagnitude > 1e-8f || (Position(grid,5)-new Vector3(.9f,0,-.45f)).sqrMagnitude > 1e-8f)
                throw new Exception("Grid coordinate contract failed.");
            grid.keyCount = 5; bool rejected = false; try { ValidateGrid(grid); } catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new Exception("Mismatched count was accepted.");
            grid.keyCount = 6; grid.presetColors = new Color[2]; rejected = false;
            try { ValidateGrid(grid); } catch (InvalidDataException) { rejected = true; }
            if (!rejected) throw new Exception("Mismatched preset colors were accepted.");
            Debug.Log("KeyboardGridRecovery contract checks passed.");
        }
    }
}
#endif
