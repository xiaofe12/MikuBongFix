using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace MikuBongFix
{
    [BepInPlugin("com.github.FelineEntity.MikuBongFix", "MikuBongFix", "1.0.2")]
    public class Plugin : BaseUnityPlugin
    {
        private const string BundleFileName = "mikupeak";
        private const string PrefabAssetPath = "assets/mikufumo/mikufumo.prefab";
        private const string MaterialAssetPath = "assets/mikufumo/m_mikufumo.mat";
        private const string IconTextureAssetPath = "assets/mikufumo/miku_icon.png";
        private const string MainTextureAssetPath = "assets/mikufumo/miku.png";
        private static readonly string[] PreferredTextureProps = { "_BaseMap", "_MainTex" };
        private static readonly Color MikuStyleTint = new Color(0.98f, 1f, 1f, 1f);

        private static ManualLogSource _log;
        internal static ManualLogSource Log
        {
            get { return _log; }
            private set { _log = value; }
        }

        private static AssetBundle _bundle;
        internal static AssetBundle Bundle
        {
            get { return _bundle; }
            private set { _bundle = value; }
        }

        private static GameObject _mochiPrefab;
        internal static GameObject MochiPrefab
        {
            get { return _mochiPrefab; }
            private set { _mochiPrefab = value; }
        }

        private static Material _mochiMaterial;
        internal static Material MochiMaterial
        {
            get { return _mochiMaterial; }
            private set { _mochiMaterial = value; }
        }

        private static Material _runtimeMikuMaterial;
        internal static Material RuntimeMikuMaterial
        {
            get { return _runtimeMikuMaterial; }
            private set { _runtimeMikuMaterial = value; }
        }

        private static Texture2D _mochiTexture;
        internal static Texture2D MochiTexture
        {
            get { return _mochiTexture; }
            private set { _mochiTexture = value; }
        }

        private static Texture2D _mikuMainTexture;
        internal static Texture2D MikuMainTexture
        {
            get { return _mikuMainTexture; }
            private set { _mikuMainTexture = value; }
        }

        private void Awake()
        {
            Log = Logger;
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
            LoadAssets();
        }

        private void LoadAssets()
        {
            string bundlePath = Path.Combine(directory, BundleFileName);
            Bundle = AssetBundle.LoadFromFile(bundlePath);
            if (Bundle == null)
            {
                Log.LogError("Failed to load AssetBundle: " + bundlePath);
                return;
            }

            MochiPrefab = Bundle.LoadAsset<GameObject>(PrefabAssetPath);
            MochiMaterial = Bundle.LoadAsset<Material>(MaterialAssetPath);
            MochiTexture = Bundle.LoadAsset<Texture2D>(IconTextureAssetPath);

            Texture2D bundledMainTexture = Bundle.LoadAsset<Texture2D>(MainTextureAssetPath);
            MikuMainTexture = CreateReadableTexture(bundledMainTexture) ?? bundledMainTexture;

            if (MochiPrefab == null)
            {
                Log.LogError("Failed to load replacement prefab from asset bundle.");
                return;
            }

            ConfigureMochiMaterial(MikuMainTexture);
            RuntimeMikuMaterial = CreateRuntimeMikuMaterial(MikuMainTexture);

            if (RuntimeMikuMaterial == null)
            {
                Log.LogWarning("Runtime fallback material was not created. The bundled material will be used when available.");
            }

            if (MochiTexture == null)
            {
                Log.LogWarning("Failed to load replacement icon texture.");
            }

            if (MikuMainTexture == null)
            {
                Log.LogWarning("Failed to load replacement main texture.");
            }

        }

        internal static void VerboseLog(string message)
        {
            // 调试日志已禁用 / debug logs disabled.
        }

        private static void ConfigureMochiMaterial(Texture2D mikuMainTexture)
        {
            if (MochiMaterial == null)
            {
                return;
            }

            if (MochiMaterial.shader == null || !MochiMaterial.shader.isSupported)
            {
                Shader fallback = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (fallback != null)
                {
                    MochiMaterial.shader = fallback;
                }
            }

            if (mikuMainTexture != null)
            {
                ApplyTextureSet(MochiMaterial, mikuMainTexture);
            }

            ApplyMikuColorStyle(MochiMaterial);
            VerboseLog("Configured bundled material: " + MochiMaterial.name);
        }

        private static bool TryAssignTextureIfMissing(Material material, string propertyName, Texture2D texture)
        {
            if (material == null || texture == null || string.IsNullOrEmpty(propertyName) || !material.HasProperty(propertyName))
            {
                return false;
            }

            try
            {
                if (material.GetTexture(propertyName) != null)
                {
                    return false;
                }

                material.SetTexture(propertyName, texture);
                material.SetTextureScale(propertyName, Vector2.one);
                material.SetTextureOffset(propertyName, Vector2.zero);
                return true;
            }
            catch (Exception ex)
            {
                VerboseLog("Skip texture assignment on material '" + material.name + "', property '" + propertyName + "': " + ex.Message);
                return false;
            }
        }

        private static void TryAssignTextureIfMissing(Material material, string[] propertyNames, Texture2D texture)
        {
            if (material == null || texture == null || propertyNames == null)
            {
                return;
            }

            for (int i = 0; i < propertyNames.Length; i++)
            {
                TryAssignTextureIfMissing(material, propertyNames[i], texture);
            }
        }

        private static void ApplyTextureSet(Material material, Texture2D albedo)
        {
            if (material == null)
            {
                return;
            }

            TryAssignTextureIfMissing(material, PreferredTextureProps, albedo);
            ApplyMikuColorStyle(material);
        }

        private static void ApplyMikuColorStyle(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", MikuStyleTint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", MikuStyleTint);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.1f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", 0f);
            if (material.HasProperty("_OcclusionStrength")) material.SetFloat("_OcclusionStrength", 0f);
            if (material.HasProperty("_SpecularHighlights")) material.SetFloat("_SpecularHighlights", 0f);
            if (material.HasProperty("_EnvironmentReflections")) material.SetFloat("_EnvironmentReflections", 0f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.black);
                material.DisableKeyword("_EMISSION");
            }

            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_METALLICSPECGLOSSMAP");
            material.DisableKeyword("_OCCLUSIONMAP");
        }

        private static Texture2D CreateReadableTexture(Texture2D source)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                source.GetPixel(0, 0);
                return source;
            }
            catch
            {
            }

            RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;

            try
            {
                Graphics.Blit(source, temporary);
                RenderTexture.active = temporary;

                Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true, false);
                readable.name = source.name + "_Readable";
                readable.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
                readable.Apply(true, false);
                return readable;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private static Material CreateRuntimeMikuMaterial(Texture2D texture)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("W/Peak_Standard");
            if (shader == null)
            {
                Log.LogError("Failed to create runtime material because no compatible shader was found.");
                return null;
            }

            Material material = new Material(shader)
            {
                name = "Miku_RuntimeMaterial",
                renderQueue = (int)RenderQueue.Geometry,
                color = Color.white
            };

            if (texture != null)
            {
                TryAssignTextureIfMissing(material, "_BaseMap", texture);
                TryAssignTextureIfMissing(material, "_MainTex", texture);
            }

            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 1f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);

            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_SURFACE_TYPE_OPAQUE");

            ApplyMikuColorStyle(material);
            return material;
        }

        public const string Name = "MikuBongFix";
        public const string Id = "com.github.FelineEntity.MikuBongFix";
        public const string Version = "1.0.2";

        internal static string directory
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        internal static Harmony Harmony = new Harmony(Id);
    }
}
