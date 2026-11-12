using UnityEngine;

namespace KeyboardEscapeGameplay
{
    [DefaultExecutionOrder(210)]
    public sealed class World1ShopPreview : MonoBehaviour
    {
        const int PreviewLayer = 31;
        static readonly Vector3 StagePosition = Vector3.down * 2000;
        static readonly Vector3 TrailCameraPosition = new Vector3(1.7f, 1.25f, 5f);
        static readonly Vector3 AuraCameraPosition = new Vector3(0, .9f, 5f);
        static readonly Vector3 CameraFocus = new Vector3(0, .9f, 0);
        Transform stage, model, effectAnchor;
        GameObject effect;
        Camera previewCamera;
        bool isOpen, showingAura;
        World1CosmeticMaterials materials;
        TrailRenderer[] trails;
        float trailStarted;
        int trailCycle;

        public RenderTexture Texture { get; private set; }

        internal void Initialize(World1Actor actor, World1CosmeticMaterials cosmeticMaterials)
        {
            materials = cosmeticMaterials;
            stage = new GameObject("Preview scene").transform;
            stage.SetParent(transform, false);
            stage.position = StagePosition;
            model = Instantiate(actor.visual, stage, false);
            model.name = "Preview character";
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            SetLayer(model);
            effectAnchor = new GameObject("Preview effect").transform;
            effectAnchor.SetParent(stage, false);

            Texture = new RenderTexture(384, 600, 24);
            Texture.name = "World 1 shop preview";
            Texture.Create();
            previewCamera = new GameObject("Preview camera").AddComponent<Camera>();
            previewCamera.transform.SetParent(stage, false);
            SetCameraPosition(TrailCameraPosition);
            previewCamera.cullingMask = 1 << PreviewLayer;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(.53f, .52f, .5f);
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 1.45f;
            previewCamera.nearClipPlane = .05f;
            previewCamera.farClipPlane = 16;
            previewCamera.targetTexture = Texture;
            previewCamera.enabled = false;
            var light = new GameObject("Preview light").AddComponent<Light>();
            light.transform.SetParent(stage, false);
            light.transform.localRotation = Quaternion.Euler(34, 145, 0);
            light.type = LightType.Directional;
            light.cullingMask = 1 << PreviewLayer;
            light.intensity = 1.25f;
            SetOpen(false);
        }

        internal void SetOpen(bool value)
        {
            isOpen = value;
            stage.gameObject.SetActive(value);
        }

        internal void Show(World1ShopCatalog.Item item)
        {
            if (effect != null)
            {
                effect.SetActive(false);
                Destroy(effect);
            }
            showingAura = item.aura;
            SetCameraPosition(showingAura ? AuraCameraPosition : TrailCameraPosition);
            effectAnchor.localPosition = Vector3.zero;
            effect = item.effect == null ? null : Instantiate(item.effect, showingAura ? model : effectAnchor, false);
            if (effect != null)
            {
                SetLayer(effect.transform);
                if (showingAura)
                {
                    effect.transform.SetLocalPositionAndRotation(item.auraPreviewOffset, Quaternion.identity);
                    materials.ApplyAura(effect);
                }
                else materials.Apply(effect);
                trails = effect.GetComponentsInChildren<TrailRenderer>();
            }
            else trails = null;
            trailStarted = Time.unscaledTime;
            trailCycle = -1;
            model.gameObject.SetActive(showingAura);
        }

        void SetCameraPosition(Vector3 position)
        {
            previewCamera.transform.SetLocalPositionAndRotation(position, Quaternion.LookRotation(CameraFocus - position));
        }

        void LateUpdate()
        {
            if (!isOpen) return;
            float time = Time.unscaledTime;
            if (!showingAura)
            {
                float step = (time - trailStarted) / 1.15f;
                int cycle = Mathf.FloorToInt(step);
                effectAnchor.localPosition = Vector3.up * Mathf.Lerp(-.3f, 1.8f, step - cycle);
                if (cycle != trailCycle && trails != null)
                    foreach (var trail in trails) trail.Clear();
                trailCycle = cycle;
            }
            previewCamera.Render();
        }

        static void SetLayer(Transform root)
        {
            foreach (var node in root.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = PreviewLayer;
        }

        void OnDestroy()
        {
            if (Texture == null) return;
            Texture.Release();
            Destroy(Texture);
        }
    }
}
