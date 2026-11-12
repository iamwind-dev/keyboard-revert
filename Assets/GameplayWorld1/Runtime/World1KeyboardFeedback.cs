using UnityEngine;

namespace KeyboardEscapeGameplay
{
    // KeyboardGridRecovery concatenates one 20-vertex, 10-triangle key mesh per cell.
    sealed class World1KeyboardFeedback
    {
        const string GridName = "[reconstructed grid] keys";
        const int TrianglesPerKey = 10;
        const int VerticesPerKey = 20;
        const int GroundMask = ~((1 << 3) | (1 << 6) | (1 << 8));
        static readonly int PressId = Shader.PropertyToID("_KeyboardPress");

        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        Renderer current;
        int currentKey = -1;

        public void Observe(Vector3 feet, float skinWidth, bool canPress)
        {
            if (!canPress || !Physics.Raycast(feet + Vector3.up * .2f, Vector3.down,
                out var hit, .32f + skinWidth, GroundMask, QueryTriggerInteraction.Ignore)
                || hit.triangleIndex < 0 || hit.collider.name != GridName)
            {
                Release();
                return;
            }

            int key = hit.triangleIndex / TrianglesPerKey;
            if (current != null && current.transform == hit.collider.transform && currentKey == key)
                return;

            Release();
            if (!(hit.collider is MeshCollider meshCollider) || meshCollider.sharedMesh == null)
                return;
            var mesh = meshCollider.sharedMesh;
            if (mesh.subMeshCount != 1 || mesh.vertexCount % VerticesPerKey != 0 ||
                mesh.GetIndexCount(0) != (uint)(mesh.vertexCount / VerticesPerKey * TrianglesPerKey * 3))
                return;

            current = hit.collider.GetComponent<Renderer>();
            if (current == null)
                return;
            currentKey = key;
            current.GetPropertyBlock(properties);
            Vector4 press = properties.HasVector(PressId) ? properties.GetVector(PressId) : new Vector4(-1, 0, -1, 0);
            press.x = key;
            press.y = Time.time;
            properties.SetVector(PressId, press);
            current.SetPropertyBlock(properties);
        }

        public void Release()
        {
            if (current == null)
                return;
            current.GetPropertyBlock(properties);
            properties.SetVector(PressId, new Vector4(-1, 0, currentKey, Time.time));
            current.SetPropertyBlock(properties);
            current = null;
            currentKey = -1;
        }
    }
}
