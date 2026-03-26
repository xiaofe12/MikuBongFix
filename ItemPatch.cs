using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace MikuBongFix
{
    /// <summary>
    /// 标记替换后的 Miku 根节点 / Marker for replaced Miku root.
    /// </summary>
    public class MikuMarker : MonoBehaviour
    {
    }

    /// <summary>
    /// 防止模型被外部逻辑拉伸 / Guards model from unexpected scale deformation.
    /// </summary>
    public class MikuDeformGuard : MonoBehaviour
    {
        private const float SqueezeDuration = 0.78f;
        private const float SqueezeCompressPhase = 0.42f;

        private Transform[] _allTransforms = Array.Empty<Transform>();
        private Vector3[] _initialChildScales = Array.Empty<Vector3>();
        private Vector3 _rootLocalPosition;
        private Quaternion _rootLocalRotation;
        private Vector3 _rootLocalScale;
        private Item _boundItem;
        private bool _wasUsing;
        private float _squeezeElapsed = SqueezeDuration;

        public void Initialize(Vector3 rootLocalPosition, Quaternion rootLocalRotation, Vector3 rootLocalScale)
        {
            _rootLocalPosition = rootLocalPosition;
            _rootLocalRotation = rootLocalRotation;
            _rootLocalScale = rootLocalScale;
            _wasUsing = false;
            _squeezeElapsed = SqueezeDuration;
            Capture();
        }

        public void Bind(Item item)
        {
            _boundItem = item;
        }

        public void SetRootTarget(Vector3 rootLocalPosition, Quaternion rootLocalRotation, Vector3 rootLocalScale)
        {
            _rootLocalPosition = rootLocalPosition;
            _rootLocalRotation = rootLocalRotation;
            _rootLocalScale = rootLocalScale;
        }

        private bool IsHeldAndUsing()
        {
            return _boundItem != null
                && _boundItem.itemState == ItemState.Held
                && (_boundItem.isUsingPrimary || _boundItem.isUsingSecondary);
        }

        private void UpdateSingleSqueezeState()
        {
            bool isUsing = IsHeldAndUsing();
            if (isUsing && !_wasUsing)
            {
                _squeezeElapsed = 0f;
            }

            _wasUsing = isUsing;

            if (_squeezeElapsed < SqueezeDuration)
            {
                _squeezeElapsed += Time.deltaTime;
            }
        }

        private float EvaluateSingleSqueezeWeight()
        {
            if (_squeezeElapsed >= SqueezeDuration)
            {
                return 0f;
            }

            float normalized = Mathf.Clamp01(_squeezeElapsed / SqueezeDuration);
            if (normalized <= SqueezeCompressPhase)
            {
                return Mathf.SmoothStep(0f, 1f, normalized / SqueezeCompressPhase);
            }

            float releasePhase = (normalized - SqueezeCompressPhase) / (1f - SqueezeCompressPhase);
            return Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(releasePhase));
        }

        private Vector3 GetDesiredRootScale(float squeezeWeight)
        {
            if (squeezeWeight <= 0.0005f)
            {
                return _rootLocalScale;
            }

            Vector3 squeezeFactor = new Vector3(
                1f - (0.14f * squeezeWeight),
                1f + (0.11f * squeezeWeight),
                1f - (0.14f * squeezeWeight));
            return Vector3.Scale(_rootLocalScale, squeezeFactor);
        }

        public void Capture()
        {
            _allTransforms = GetComponentsInChildren<Transform>(true);
            _initialChildScales = new Vector3[_allTransforms.Length];

            for (int i = 0; i < _allTransforms.Length; i++)
            {
                _initialChildScales[i] = _allTransforms[i].localScale;
            }
        }

        private void LateUpdate()
        {
            if (_allTransforms.Length == 0 || _allTransforms.Length != _initialChildScales.Length)
            {
                Capture();
            }

            if (transform.localPosition != _rootLocalPosition)
            {
                transform.localPosition = _rootLocalPosition;
            }

            if (transform.localRotation != _rootLocalRotation)
            {
                transform.localRotation = _rootLocalRotation;
            }

            UpdateSingleSqueezeState();
            float squeezeWeight = EvaluateSingleSqueezeWeight();
            Vector3 desiredRootScale = GetDesiredRootScale(squeezeWeight);
            if (transform.localScale != desiredRootScale)
            {
                transform.localScale = desiredRootScale;
            }

            for (int i = 0; i < _allTransforms.Length; i++)
            {
                Transform current = _allTransforms[i];
                if (current == null || current == transform)
                {
                    continue;
                }

                if (current.localScale != _initialChildScales[i])
                {
                    current.localScale = _initialChildScales[i];
                }
            }
        }
    }

    /// <summary>
    /// 保底可见性守卫 / Keeps renderers alive and visible.
    /// </summary>
    public class MikuRendererGuard : MonoBehaviour
    {
        private const float RefreshInterval = 0.1f;

        private float _nextRefreshTime;
        private Item _boundItem;
        private Renderer[] _cachedRenderers = Array.Empty<Renderer>();

        public void Bind(Item item)
        {
            _boundItem = item;
        }

        private void LateUpdate()
        {
            if (Time.time < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.time + RefreshInterval;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            int targetLayer = _boundItem != null ? _boundItem.gameObject.layer : gameObject.layer;
            if (gameObject.layer != targetLayer)
            {
                gameObject.layer = targetLayer;
            }

            Renderer[] renderers = GetRenderableChildren();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                EnsureRendererVisible(renderer, targetLayer);
                NormalizeRendererMaterialColor(renderer);
            }
        }

        private Renderer[] GetRenderableChildren()
        {
            if (_cachedRenderers.Length == 0 || HasNullRenderer(_cachedRenderers))
            {
                _cachedRenderers = GetComponentsInChildren<Renderer>(true);
            }

            return _cachedRenderers;
        }

        private static bool HasNullRenderer(Renderer[] renderers)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureRendererVisible(Renderer renderer, int targetLayer)
        {
            if (!renderer.gameObject.activeSelf)
            {
                renderer.gameObject.SetActive(true);
            }

            if (renderer.forceRenderingOff)
            {
                renderer.forceRenderingOff = false;
            }

            if (!renderer.enabled)
            {
                renderer.enabled = true;
            }

            renderer.SetPropertyBlock(null);
            renderer.gameObject.layer = targetLayer;
            renderer.allowOcclusionWhenDynamic = false;
        }

        private static void NormalizeRendererMaterialColor(Renderer renderer)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null)
                {
                    continue;
                }

                Color mikuTint = Color.white;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", mikuTint);
                if (material.HasProperty("_Color")) material.SetColor("_Color", mikuTint);
            }
        }
    }

    [HarmonyPatch(typeof(Item))]
    public class ItemPatch
    {
        private const int VisibleLayer = 0;
        private const float BaseScaleMultiplier = 1f;
        private const float BackpackScaleMultiplier = 0.4f;
        private static readonly Color MikuMaterialTint = new Color(0.98f, 1f, 1f, 1f);
        private const string MikuVisualName = "MikuFumo_Visual";
        private static readonly Vector3 MikuBaseScale = new Vector3(1.5f, 1.5f, 1.5f);
        private static readonly Vector3 MikuFixedScale = MikuBaseScale * BaseScaleMultiplier;
        private static readonly Vector3 MikuBackpackScale = MikuBaseScale * BackpackScaleMultiplier;
        private static readonly Vector3 MikuLocalPosition = new Vector3(0f, 0.2f, 0.1f);
        private static readonly Quaternion MikuLocalRotation = Quaternion.identity;
        private static readonly string[] UnwantedFootKeywords = new[] { "hand", "glove", "mitten" };

        private static bool IsBingBong(Item item)
        {
            return item != null && item.name.IndexOf("BingBong", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMikuTransform(Transform transform)
        {
            return transform != null && transform.GetComponentInParent<MikuMarker>() != null;
        }

        private static bool IsInBackpack(Item item)
        {
            return item != null && item.itemState == ItemState.InBackpack;
        }

        private static bool ShouldShowReplacement(Item item)
        {
            return item != null;
        }

        private static Vector3 ResolveScaleByState(Item item)
        {
            return IsInBackpack(item) ? MikuBackpackScale : MikuFixedScale;
        }

        /// <summary>
        /// 在当前 Item 下定位替换模型根节点 / Finds Miku replacement root under current Item.
        /// </summary>
        private static Transform FindMikuRoot(Item item)
        {
            if (item == null)
            {
                return null;
            }

            MikuMarker[] markers = item.GetComponentsInChildren<MikuMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] != null)
                {
                    return markers[i].transform;
                }
            }

            return item.transform.Find(MikuVisualName);
        }

        private static Texture GetMikuTexture()
        {
            if (Plugin.MikuMainTexture != null)
            {
                return Plugin.MikuMainTexture;
            }

            Texture texture = TryGetTexture(Plugin.MochiMaterial);
            if (texture != null)
            {
                return texture;
            }

            return TryGetTexture(Plugin.RuntimeMikuMaterial);
        }

        private static Texture TryGetTexture(Material material)
        {
            if (material == null)
            {
                return null;
            }

            if (material.HasProperty("_BaseMap"))
            {
                Texture baseMap = material.GetTexture("_BaseMap");
                if (baseMap != null)
                {
                    return baseMap;
                }
            }

            if (material.HasProperty("_MainTex"))
            {
                return material.GetTexture("_MainTex");
            }

            return null;
        }

        private static bool TrySetTextureSafe(Material material, string propertyName, Texture texture)
        {
            if (material == null || string.IsNullOrEmpty(propertyName) || !material.HasProperty(propertyName))
            {
                return false;
            }

            try
            {
                material.SetTexture(propertyName, texture);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.VerboseLog("[ItemPatch] Skip SetTexture on material '" + material.name + "' property '" + propertyName + "': " + ex.Message);
                return false;
            }
        }

        private static void TrySetTextureTransformSafe(Material material, string propertyName, Vector2 scale, Vector2 offset)
        {
            if (material == null || string.IsNullOrEmpty(propertyName) || !material.HasProperty(propertyName))
            {
                return;
            }

            try
            {
                material.SetTextureScale(propertyName, scale);
                material.SetTextureOffset(propertyName, offset);
            }
            catch (Exception ex)
            {
                Plugin.VerboseLog("[ItemPatch] Skip texture transform on material '" + material.name + "' property '" + propertyName + "': " + ex.Message);
            }
        }

        private static void ApplyMikuTextureSafe(Material material, Texture mikuTexture)
        {
            if (material == null || mikuTexture == null)
            {
                return;
            }

            if (TrySetTextureSafe(material, "_BaseMap", mikuTexture))
            {
                TrySetTextureTransformSafe(material, "_BaseMap", Vector2.one, Vector2.zero);
            }

            if (TrySetTextureSafe(material, "_MainTex", mikuTexture))
            {
                TrySetTextureTransformSafe(material, "_MainTex", Vector2.one, Vector2.zero);
            }
        }

        private static void DisableOptionalSurfaceTextures(Material material)
        {
            if (material == null)
            {
                return;
            }

            TrySetTextureSafe(material, "_BumpMap", null);
            TrySetTextureSafe(material, "_NormalMap", null);
            TrySetTextureSafe(material, "_OcclusionMap", null);
            TrySetTextureSafe(material, "_MetallicGlossMap", null);
            TrySetTextureSafe(material, "_SpecGlossMap", null);

            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_METALLICSPECGLOSSMAP");
            material.DisableKeyword("_OCCLUSIONMAP");
        }

        private static void OptimizeTextureSampling(Texture texture)
        {
            Texture2D texture2D = texture as Texture2D;
            if (texture2D == null)
            {
                return;
            }

            texture2D.filterMode = FilterMode.Trilinear;
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.anisoLevel = Mathf.Max(texture2D.anisoLevel, 16);
            texture2D.mipMapBias = Mathf.Min(texture2D.mipMapBias, -0.75f);
        }

        private static void ApplyRealisticMaterialTuning(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", MikuMaterialTint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", MikuMaterialTint);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.1f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", 0f);
            if (material.HasProperty("_OcclusionStrength")) material.SetFloat("_OcclusionStrength", 0f);
            if (material.HasProperty("_SpecularHighlights")) material.SetFloat("_SpecularHighlights", 0f);
            if (material.HasProperty("_EnvironmentReflections")) material.SetFloat("_EnvironmentReflections", 0f);
            DisableOptionalSurfaceTextures(material);

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.black);
                material.DisableKeyword("_EMISSION");
            }
        }

        private static Material CreateFallbackMaterial()
        {
            Shader fallback = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("W/Peak_Standard");
            if (fallback == null)
            {
                return null;
            }

            return new Material(fallback) { color = Color.white };
        }

        private static Material CreateRendererMaterialInstance()
        {
            Material template = Plugin.RuntimeMikuMaterial ?? Plugin.MochiMaterial;
            if (template == null)
            {
                Material fallbackMaterial = CreateFallbackMaterial();
                if (fallbackMaterial == null)
                {
                    return null;
                }

                template = fallbackMaterial;
            }

            Material material = new Material(template);
            Texture mikuTexture = GetMikuTexture();

            if (mikuTexture != null)
            {
                OptimizeTextureSampling(mikuTexture);
                ApplyMikuTextureSafe(material, mikuTexture);
            }

            DisableOptionalSurfaceTextures(material);

            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.One);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
            if (material.HasProperty("_Cutoff")) material.SetFloat("_Cutoff", 0f);
            if (material.HasProperty("_LOD")) material.SetFloat("_LOD", 600f);

            material.renderQueue = (int)RenderQueue.Geometry;
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_SURFACE_TYPE_OPAQUE");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("LOD_FADE_CROSSFADE");

            ApplyRealisticMaterialTuning(material);

            return material;
        }

        private static Material[] BuildMaterialArray(Material[] sourceMaterials, int subMeshCount)
        {
            int count = Mathf.Max(1, subMeshCount);
            Material[] materials = new Material[count];

            for (int i = 0; i < count; i++)
            {
                Material material = CreateRendererMaterialInstance();
                if (material == null)
                {
                    material = CreateFallbackMaterial();
                }

                if (material != null)
                {
                    Texture mikuTexture = GetMikuTexture();
                    if (mikuTexture != null)
                    {
                        OptimizeTextureSampling(mikuTexture);
                        ApplyMikuTextureSafe(material, mikuTexture);
                    }

                    DisableOptionalSurfaceTextures(material);
                    ApplyRealisticMaterialTuning(material);
                }

                materials[i] = material;
            }

            return materials;
        }

        private static void ApplyMaterialToMeshRenderer(MeshRenderer renderer)
        {
            int subMeshCount = 1;
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                subMeshCount = Mathf.Max(1, meshFilter.sharedMesh.subMeshCount);
            }

            renderer.sharedMaterials = BuildMaterialArray(renderer.sharedMaterials, subMeshCount);
        }

        private static void ApplyMaterialToSkinnedRenderer(SkinnedMeshRenderer renderer)
        {
            int subMeshCount = 1;
            if (renderer.sharedMesh != null)
            {
                subMeshCount = Mathf.Max(1, renderer.sharedMesh.subMeshCount);
            }

            renderer.sharedMaterials = BuildMaterialArray(renderer.sharedMaterials, subMeshCount);

            if (renderer.sharedMesh != null)
            {
                Bounds meshBounds = renderer.sharedMesh.bounds;
                meshBounds.Expand(0.5f);
                renderer.localBounds = meshBounds;
            }
        }

        private static bool HasVisibleReplacementRenderers(Transform mikuRoot)
        {
            if (mikuRoot == null)
            {
                return false;
            }

            Renderer[] renderers = mikuRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    continue;
                }

                for (int j = 0; j < materials.Length; j++)
                {
                    if (materials[j] != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void DisableOriginalRenderers(Item item)
        {
            Renderer[] renderers = item.gameObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsMikuTransform(renderer.transform))
                {
                    continue;
                }

                renderer.enabled = false;
            }
        }

        private static void EnableOriginalRenderers(Item item)
        {
            Renderer[] renderers = item.gameObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsMikuTransform(renderer.transform))
                {
                    continue;
                }

                renderer.enabled = true;
            }
        }

        private static void DisableOriginalColliders(Item item)
        {
            Collider[] colliders = item.gameObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || IsMikuTransform(collider.transform))
                {
                    continue;
                }

                collider.enabled = false;
            }
        }

        private static Collider FindColliderTemplate(Item item)
        {
            if (item == null)
            {
                return null;
            }

            Collider[] colliders = item.gameObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null && !IsMikuTransform(collider.transform))
                {
                    return collider;
                }
            }

            return null;
        }

        private static Vector3 ClampColliderSize(Vector3 size)
        {
            return new Vector3(Mathf.Max(size.x, 0.02f), Mathf.Max(size.y, 0.02f), Mathf.Max(size.z, 0.02f));
        }

        private static void CopyColliderSettings(Collider source, Collider target)
        {
            if (target == null)
            {
                return;
            }

            if (source != null)
            {
                target.isTrigger = source.isTrigger;
                target.sharedMaterial = source.sharedMaterial;
                target.contactOffset = source.contactOffset;
            }

            target.enabled = true;
        }

        private static Collider AddBoxColliderForMeshRenderer(MeshRenderer renderer, Collider templateCollider)
        {
            if (renderer == null)
            {
                return null;
            }

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return null;
            }

            Bounds localBounds = meshFilter.sharedMesh.bounds;
            if (localBounds.size.sqrMagnitude <= 0f)
            {
                return null;
            }

            BoxCollider collider = renderer.gameObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = renderer.gameObject.AddComponent<BoxCollider>();
            }

            collider.center = localBounds.center;
            collider.size = ClampColliderSize(localBounds.size);
            CopyColliderSettings(templateCollider, collider);
            return collider;
        }

        private static Collider AddBoxColliderForSkinnedRenderer(SkinnedMeshRenderer renderer, Collider templateCollider)
        {
            if (renderer == null)
            {
                return null;
            }

            Bounds localBounds = renderer.localBounds;
            if (localBounds.size.sqrMagnitude <= 0f && renderer.sharedMesh != null)
            {
                localBounds = renderer.sharedMesh.bounds;
            }

            if (localBounds.size.sqrMagnitude <= 0f)
            {
                return null;
            }

            BoxCollider collider = renderer.gameObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = renderer.gameObject.AddComponent<BoxCollider>();
            }

            collider.center = localBounds.center;
            collider.size = ClampColliderSize(localBounds.size);
            CopyColliderSettings(templateCollider, collider);
            return collider;
        }

        private static void EncapsulateBoundsCorners(ref Bounds aggregateBounds, ref bool hasBounds, Transform root, Bounds rendererBounds)
        {
            Vector3 center = rendererBounds.center;
            Vector3 extents = rendererBounds.extents;

            Vector3[] corners = new[]
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, -extents.z),
                center + new Vector3(-extents.x, extents.y, extents.z),
                center + new Vector3(extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, extents.z),
                center + new Vector3(extents.x, extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, extents.z)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 localPoint = root.InverseTransformPoint(corners[i]);
                if (!hasBounds)
                {
                    aggregateBounds = new Bounds(localPoint, Vector3.zero);
                    hasBounds = true;
                    continue;
                }

                aggregateBounds.Encapsulate(localPoint);
            }
        }

        private static Collider AddFallbackRootCollider(Transform mikuRoot, Collider templateCollider)
        {
            if (mikuRoot == null)
            {
                return null;
            }

            Renderer[] renderers = mikuRoot.GetComponentsInChildren<Renderer>(true);
            Bounds aggregateBounds = default;
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                EncapsulateBoundsCorners(ref aggregateBounds, ref hasBounds, mikuRoot, renderer.bounds);
            }

            if (!hasBounds)
            {
                return null;
            }

            BoxCollider collider = mikuRoot.gameObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = mikuRoot.gameObject.AddComponent<BoxCollider>();
            }

            collider.center = aggregateBounds.center;
            collider.size = ClampColliderSize(aggregateBounds.size);
            CopyColliderSettings(templateCollider, collider);
            return collider;
        }

        private static void RebuildModelColliders(Item item, Transform mikuRoot)
        {
            if (item == null || mikuRoot == null)
            {
                return;
            }

            Collider templateCollider = FindColliderTemplate(item);
            Collider[] existingColliders = mikuRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < existingColliders.Length; i++)
            {
                Collider collider = existingColliders[i];
                if (collider != null)
                {
                    UnityEngine.Object.Destroy(collider);
                }
            }

            List<Collider> generatedColliders = new List<Collider>();

            MeshRenderer[] meshRenderers = mikuRoot.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                Collider collider = AddBoxColliderForMeshRenderer(meshRenderers[i], templateCollider);
                if (collider != null)
                {
                    generatedColliders.Add(collider);
                }
            }

            SkinnedMeshRenderer[] skinnedRenderers = mikuRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinnedRenderers.Length; i++)
            {
                Collider collider = AddBoxColliderForSkinnedRenderer(skinnedRenderers[i], templateCollider);
                if (collider != null)
                {
                    generatedColliders.Add(collider);
                }
            }

            if (generatedColliders.Count == 0)
            {
                Collider fallbackCollider = AddFallbackRootCollider(mikuRoot, templateCollider);
                if (fallbackCollider != null)
                {
                    generatedColliders.Add(fallbackCollider);
                }
            }

            if (generatedColliders.Count > 0)
            {
                item.colliders = generatedColliders.ToArray();
                DisableOriginalColliders(item);
            }
        }

        private static void EnsureModelColliders(Item item, Transform mikuRoot)
        {
            if (item == null || mikuRoot == null)
            {
                return;
            }

            Collider[] mikuColliders = mikuRoot.GetComponentsInChildren<Collider>(true);
            if (mikuColliders.Length == 0)
            {
                RebuildModelColliders(item, mikuRoot);
                mikuColliders = mikuRoot.GetComponentsInChildren<Collider>(true);
            }

            if (mikuColliders.Length > 0)
            {
                item.colliders = mikuColliders;
                DisableOriginalColliders(item);
            }
        }

        private static int DetermineTargetLayer(Item item)
        {
            if (item == null)
            {
                return VisibleLayer;
            }

            Renderer[] renderers = item.gameObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsMikuTransform(renderer.transform))
                {
                    continue;
                }

                return renderer.gameObject.layer;
            }

            return item.gameObject.layer;
        }

        private static void ConfigureMikuRenderer(Renderer renderer, int targetLayer)
        {
            renderer.gameObject.layer = targetLayer;
            renderer.allowOcclusionWhenDynamic = false;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.forceRenderingOff = false;
            renderer.enabled = true;
            renderer.SetPropertyBlock(null);

            if (!renderer.gameObject.activeSelf)
            {
                renderer.gameObject.SetActive(true);
            }
        }

        private static bool RendererHasMainTexSlot(Renderer renderer)
        {
            if (renderer == null)
            {
                return false;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material != null && material.HasProperty("_MainTex"))
                {
                    return true;
                }
            }

            return false;
        }

        private static Material CreateMainTexCompatMaterial(Texture texture)
        {
            Shader shader = Shader.Find("Standard")
                ?? Shader.Find("Unlit/Texture")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("W/Peak_Standard");

            if (shader == null)
            {
                return null;
            }

            Material material = new Material(shader)
            {
                name = "Miku_MainTexCompat",
                color = Color.white,
                renderQueue = (int)RenderQueue.Geometry
            };

            if (texture != null)
            {
                ApplyMikuTextureSafe(material, texture);
            }

            ApplyRealisticMaterialTuning(material);
            return material;
        }

        private static void EnsureRendererMainTexCompatibility(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                Material fallback = CreateMainTexCompatMaterial(GetMikuTexture()) ?? CreateRendererMaterialInstance();
                if (fallback != null)
                {
                    renderer.sharedMaterial = fallback;
                }

                return;
            }

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].HasProperty("_MainTex"))
                {
                    return;
                }
            }

            Material compat = CreateMainTexCompatMaterial(GetMikuTexture());
            if (compat == null)
            {
                return;
            }

            materials[0] = compat;
            renderer.sharedMaterials = materials;
        }

        private static void EnsureItemRendererRefs(Item item, Transform mikuRoot)
        {
            Renderer[] mikuRenderers = mikuRoot.GetComponentsInChildren<Renderer>(true);
            if (mikuRenderers.Length == 0)
            {
                return;
            }

            Renderer primaryRenderer = null;
            for (int i = 0; i < mikuRenderers.Length; i++)
            {
                Renderer renderer = mikuRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (primaryRenderer == null)
                {
                    primaryRenderer = renderer;
                }

                if (RendererHasMainTexSlot(renderer))
                {
                    primaryRenderer = renderer;
                    break;
                }
            }

            if (primaryRenderer == null)
            {
                return;
            }

            EnsureRendererMainTexCompatibility(primaryRenderer);
            item.mainRenderer = primaryRenderer;
            item.addtlRenderers = mikuRenderers;
        }

        /// <summary>
        /// 对 Miku 渲染组件做初始化 / Initializes renderer state and materials.
        /// </summary>
        private static void ApplyInitialMikuRendererSetup(Item item, Transform mikuRoot)
        {
            int targetLayer = DetermineTargetLayer(item);
            mikuRoot.gameObject.layer = targetLayer;

            MeshRenderer[] meshRenderers = mikuRoot.GetComponentsInChildren<MeshRenderer>(true);
            SkinnedMeshRenderer[] skinnedRenderers = mikuRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            Plugin.VerboseLog("[ItemPatch] Renderer layout for " + item.name + ": MeshRenderers=" + meshRenderers.Length + ", SkinnedMeshRenderers=" + skinnedRenderers.Length);

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                MeshRenderer renderer = meshRenderers[i];
                ConfigureMikuRenderer(renderer, targetLayer);
                ApplyMaterialToMeshRenderer(renderer);
            }

            for (int i = 0; i < skinnedRenderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = skinnedRenderers[i];
                renderer.updateWhenOffscreen = true;
                ConfigureMikuRenderer(renderer, targetLayer);
                ApplyMaterialToSkinnedRenderer(renderer);
            }

            EnsureItemRendererRefs(item, mikuRoot);
        }

        private static bool IsWhitelistedBehaviour(MonoBehaviour behaviour)
        {
            return behaviour is MikuMarker
                || behaviour is MikuRendererGuard
                || behaviour is MikuDeformGuard;
        }

        /// <summary>
        /// 清理替换体上的交互/物理组件 / Removes interactable and physics components from visual object.
        /// </summary>
        private static void SanitizeVisualObject(GameObject visualRoot, int targetLayer)
        {
            Collider[] colliders = visualRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                {
                    continue;
                }

                collider.enabled = false;
                UnityEngine.Object.Destroy(collider);
            }

            Rigidbody[] rigidbodies = visualRoot.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] != null)
                {
                    UnityEngine.Object.Destroy(rigidbodies[i]);
                }
            }

            Joint[] joints = visualRoot.GetComponentsInChildren<Joint>(true);
            for (int i = 0; i < joints.Length; i++)
            {
                if (joints[i] != null)
                {
                    UnityEngine.Object.Destroy(joints[i]);
                }
            }

            LODGroup[] lodGroups = visualRoot.GetComponentsInChildren<LODGroup>(true);
            for (int i = 0; i < lodGroups.Length; i++)
            {
                LODGroup lod = lodGroups[i];
                if (lod == null)
                {
                    continue;
                }

                lod.enabled = false;
                UnityEngine.Object.Destroy(lod);
            }

            MonoBehaviour[] behaviours = visualRoot.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || IsWhitelistedBehaviour(behaviour))
                {
                    continue;
                }

                behaviour.enabled = false;
                UnityEngine.Object.Destroy(behaviour);
            }

            Transform[] allTransforms = visualRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform t = allTransforms[i];
                if (t == null)
                {
                    continue;
                }

                t.gameObject.tag = "Untagged";
                t.gameObject.layer = targetLayer;
            }
        }

        private static bool ContainsKeyword(string value, string[] keywords)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string lower = value.ToLowerInvariant();
            for (int i = 0; i < keywords.Length; i++)
            {
                if (lower.Contains(keywords[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 移除脚部多余附件（例如手型网格）/ Removes extra foot-area attachments (e.g. hand-like meshes).
        /// </summary>
        private static void RemoveUnwantedFootAttachments(Transform mikuRoot)
        {
            Renderer[] renderers = mikuRoot.GetComponentsInChildren<Renderer>(true);
            int removedCount = 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!ContainsKeyword(renderer.gameObject.name, UnwantedFootKeywords))
                {
                    continue;
                }

                float localY = mikuRoot.InverseTransformPoint(renderer.bounds.center).y;
                if (localY > 0.2f)
                {
                    continue;
                }

                UnityEngine.Object.Destroy(renderer.gameObject);
                removedCount++;
            }

            if (removedCount > 0)
            {
                Plugin.VerboseLog("[ItemPatch] Removed unwanted foot attachments: " + removedCount);
            }
        }

        /// <summary>
        /// 同步可见性和渲染引用 / Syncs visibility and renderer references.
        /// </summary>
        private static void SyncVisibilityState(Item item)
        {
            Transform mikuRoot = FindMikuRoot(item);
            if (mikuRoot == null)
            {
                return;
            }

            EnsureModelColliders(item, mikuRoot);

            int targetLayer = DetermineTargetLayer(item);
            mikuRoot.gameObject.layer = targetLayer;
            Vector3 targetScale = ResolveScaleByState(item);

            if (mikuRoot.localPosition != MikuLocalPosition)
            {
                mikuRoot.localPosition = MikuLocalPosition;
            }

            if (mikuRoot.localRotation != MikuLocalRotation)
            {
                mikuRoot.localRotation = MikuLocalRotation;
            }

            if (mikuRoot.localScale != targetScale)
            {
                mikuRoot.localScale = targetScale;
            }

            MikuDeformGuard deformGuard = mikuRoot.GetComponent<MikuDeformGuard>();
            if (deformGuard != null)
            {
                deformGuard.SetRootTarget(MikuLocalPosition, MikuLocalRotation, targetScale);
            }

            bool shouldShow = ShouldShowReplacement(item);
            if (mikuRoot.gameObject.activeSelf != shouldShow)
            {
                mikuRoot.gameObject.SetActive(shouldShow);
            }

            if (!shouldShow)
            {
                return;
            }

            bool hasVisibleReplacement = HasVisibleReplacementRenderers(mikuRoot);
            if (hasVisibleReplacement)
            {
                DisableOriginalRenderers(item);
            }
            else
            {
                EnableOriginalRenderers(item);
            }

            Renderer[] renderers = mikuRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null)
                {
                    ConfigureMikuRenderer(renderer, targetLayer);
                }
            }

            EnsureItemRendererRefs(item, mikuRoot);
        }

        /// <summary>
        /// 确保 Miku 模型存在 / Ensures Miku model exists under the target item.
        /// </summary>
        private static Transform EnsureMikuModel(Item item, bool createIfMissing)
        {
            Transform existing = FindMikuRoot(item);
            if (existing != null)
            {
                return existing;
            }

            if (!createIfMissing)
            {
                return null;
            }

            if (Plugin.MochiPrefab == null)
            {
                Plugin.Log.LogWarning("[ItemPatch] MochiPrefab is null, cannot create Miku model yet.");
                return null;
            }

            GameObject mikuObject = UnityEngine.Object.Instantiate(Plugin.MochiPrefab, item.transform, false);
            mikuObject.name = MikuVisualName;
            mikuObject.SetActive(true);

            int targetLayer = DetermineTargetLayer(item);
            mikuObject.layer = targetLayer;

            mikuObject.transform.localPosition = MikuLocalPosition;
            mikuObject.transform.localRotation = MikuLocalRotation;
            Vector3 targetScale = ResolveScaleByState(item);
            mikuObject.transform.localScale = targetScale;

            if (mikuObject.GetComponent<MikuMarker>() == null)
            {
                mikuObject.AddComponent<MikuMarker>();
            }

            RemoveUnwantedFootAttachments(mikuObject.transform);
            SanitizeVisualObject(mikuObject, targetLayer);

            MikuRendererGuard rendererGuard = mikuObject.GetComponent<MikuRendererGuard>();
            if (rendererGuard == null)
            {
                rendererGuard = mikuObject.AddComponent<MikuRendererGuard>();
            }
            rendererGuard.Bind(item);

            MikuDeformGuard deformGuard = mikuObject.GetComponent<MikuDeformGuard>();
            if (deformGuard == null)
            {
                deformGuard = mikuObject.AddComponent<MikuDeformGuard>();
            }
            deformGuard.Bind(item);
            deformGuard.Initialize(MikuLocalPosition, MikuLocalRotation, targetScale);

            ApplyInitialMikuRendererSetup(item, mikuObject.transform);
            RebuildModelColliders(item, mikuObject.transform);
            SyncVisibilityState(item);

            Plugin.VerboseLog("[ItemPatch] Created Miku replacement model under item: " + item.name);
            return mikuObject.transform;
        }

        private static void EnsureReplacementAndVisibility(Item item, bool createIfMissing)
        {
            if (!IsBingBong(item))
            {
                return;
            }

            Transform mikuRoot = EnsureMikuModel(item, createIfMissing);
            if (mikuRoot == null)
            {
                return;
            }

            MikuRendererGuard guard = mikuRoot.GetComponent<MikuRendererGuard>();
            if (guard != null)
            {
                guard.Bind(item);
            }

            MikuDeformGuard deformGuard = mikuRoot.GetComponent<MikuDeformGuard>();
            if (deformGuard != null)
            {
                deformGuard.Bind(item);
            }

            SyncVisibilityState(item);
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Item_Start(Item __instance)
        {
            EnsureReplacementAndVisibility(__instance, true);
            if (IsBingBong(__instance))
            {
                Plugin.VerboseLog("[ItemPatch] Model replacement complete for: " + __instance.name);
            }
        }

        [HarmonyPatch("OnEnable")]
        [HarmonyPostfix]
        public static void Item_OnEnable(Item __instance)
        {
            EnsureReplacementAndVisibility(__instance, true);
        }

        [HarmonyPatch("SetState")]
        [HarmonyPrefix]
        public static void Item_SetState_Prefix(Item __instance)
        {
            EnsureReplacementAndVisibility(__instance, true);
        }

        [HarmonyPatch("SetState")]
        [HarmonyPostfix]
        public static void Item_SetState_Postfix(Item __instance)
        {
            EnsureReplacementAndVisibility(__instance, true);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Item_Update_Postfix(Item __instance)
        {
            EnsureReplacementAndVisibility(__instance, false);
        }

        [HarmonyPatch("RequestPickup")]
        [HarmonyPrefix]
        public static void Item_RequestPickup_Prefix(Item __instance)
        {
            EnsureReplacementAndVisibility(__instance, true);
        }

        [HarmonyPatch("RequestPickup")]
        [HarmonyPostfix]
        public static void Item_RequestPickup_Postfix(Item __instance)
        {
            EnsureReplacementAndVisibility(__instance, true);
        }

        [HarmonyPatch("HideRenderers")]
        [HarmonyPrefix]
        public static bool Item_HideRenderers_Prefix(Item __instance)
        {
            if (!IsBingBong(__instance))
            {
                return true;
            }

            Transform mikuRoot = EnsureMikuModel(__instance, false);
            if (mikuRoot == null)
            {
                return true;
            }

            mikuRoot.localScale = ResolveScaleByState(__instance);
            if (!mikuRoot.gameObject.activeSelf)
            {
                mikuRoot.gameObject.SetActive(true);
            }

            int targetLayer = DetermineTargetLayer(__instance);
            Renderer[] renderers = mikuRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null)
                {
                    ConfigureMikuRenderer(renderer, targetLayer);
                }
            }

            DisableOriginalRenderers(__instance);
            EnsureItemRendererRefs(__instance, mikuRoot);

            return false;
        }

        [HarmonyPatch("PutInBackpackRPC")]
        [HarmonyPostfix]
        public static void Item_PutInBackpackRPC_Postfix(Item __instance)
        {
            EnsureReplacementAndVisibility(__instance, false);
        }

        [HarmonyPatch("ClearDataFromBackpack")]
        [HarmonyPostfix]
        public static void Item_ClearDataFromBackpack_Postfix(Item __instance)
        {
            EnsureReplacementAndVisibility(__instance, false);
        }

        [HarmonyPatch(typeof(Item.ItemUIData), "GetIcon")]
        [HarmonyPostfix]
        public static void Item_GetIcon(Item.ItemUIData __instance, ref Texture2D __result)
        {
            if (__instance.itemName == "Bing Bong" && Plugin.MochiTexture != null)
            {
                __result = Plugin.MochiTexture;
            }
        }

        [HarmonyPatch("GetName")]
        [HarmonyPostfix]
        public static void Item_GetName_Postfix(Item __instance, ref string __result)
        {
            if (IsBingBong(__instance))
            {
                __result = "Miku";
            }
        }

        [HarmonyPatch("GetItemName")]
        [HarmonyPostfix]
        public static void Item_GetItemName_Postfix(Item __instance, ref string __result)
        {
            if (IsBingBong(__instance))
            {
                __result = "Miku";
            }
        }
    }

}
