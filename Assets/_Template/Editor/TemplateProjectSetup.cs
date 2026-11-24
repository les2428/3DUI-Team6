using System.IO;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using WebXR;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// One-click project configuration for WebXR (menu: 3DUI > Setup).
    /// Safe to run multiple times.
    /// </summary>
    public static class TemplateProjectSetup
    {
        private const string SettingsDir = "Assets/_Template/Settings";
        private const string WebGLTemplateName = "PROJECT:WebXRFullView2020";

        [MenuItem("3DUI/Setup/Apply All Project Settings", priority = 0)]
        public static void ApplyAll()
        {
            SetupRenderPipeline();
            SetupPlayerSettings();
            CopyWebGLTemplates();
            SetupPlayerSettings(); // again, now that the WebGL template exists
            SetupXRManagement();
            TemplateFontSetup.SetupFonts();
            AssetDatabase.SaveAssets();
            Debug.Log("[3DUI] Project settings applied (URP, WebGL player settings, WebXR templates, XR Plug-in Management).");
        }

        // ------------------------------------------------------------------ URP

        [MenuItem("3DUI/Setup/Render Pipeline (URP)", priority = 20)]
        public static void SetupRenderPipeline()
        {
            EnsureFolder(SettingsDir);
            string rendererPath = SettingsDir + "/WebXR_Renderer.asset";
            string urpPath = SettingsDir + "/WebXR_URP.asset";

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpPath);
            if (urp == null)
            {
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
                urp = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urp, urpPath);
            }

            // Lightweight defaults for standalone headsets (Snapdragon XR2 class) and laptops.
            urp.msaaSampleCount = 2;
            urp.supportsHDR = false;
            urp.renderScale = 1f;
            urp.shadowDistance = 25f;
            urp.shadowCascadeCount = 1;
            urp.supportsCameraDepthTexture = false;
            urp.supportsCameraOpaqueTexture = false;
            EditorUtility.SetDirty(urp);

            GraphicsSettings.defaultRenderPipeline = urp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(current, false);
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------ Player

        [MenuItem("3DUI/Setup/Player Settings (WebGL)", priority = 21)]
        public static void SetupPlayerSettings()
        {
            PlayerSettings.companyName = "Hanyang 3DUI";
            PlayerSettings.productName = "3DUI WebXR Space";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });

            // Gzip + decompression fallback: works on any static host (GitHub Pages, school/lab servers)
            // without special Content-Encoding headers. Switch to Brotli if your server is configured for it.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = false; // dev: always fetch the latest build (turn on for production)
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.powerPreference = WebGLPowerPreference.HighPerformance;

            if (Directory.Exists("Assets/WebGLTemplates/WebXRFullView2020"))
                PlayerSettings.WebGL.template = WebGLTemplateName;
        }

        // ------------------------------------------------------------------ WebGL templates

        [MenuItem("3DUI/Setup/Copy WebXR WebGL Templates", priority = 22)]
        public static void CopyWebGLTemplates()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.de-panther.webxr/package.json");
            if (info == null)
            {
                Debug.LogError("[3DUI] WebXR Export package (com.de-panther.webxr) not found.");
                return;
            }
            string src = Path.Combine(info.resolvedPath, "Hidden~", "WebGLTemplates");
            string dst = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "WebGLTemplates");
            if (!Directory.Exists(src))
            {
                Debug.LogError("[3DUI] WebGLTemplates not found in WebXR package: " + src);
                return;
            }
            CopyDirectory(src, dst, overwrite: false);
            AssetDatabase.Refresh();
        }

        private static void CopyDirectory(string src, string dst, bool overwrite)
        {
            Directory.CreateDirectory(dst);
            foreach (var file in Directory.GetFiles(src))
            {
                string target = Path.Combine(dst, Path.GetFileName(file));
                if (overwrite || !File.Exists(target)) File.Copy(file, target, true);
            }
            foreach (var dir in Directory.GetDirectories(src))
                CopyDirectory(dir, Path.Combine(dst, Path.GetFileName(dir)), overwrite);
        }

        // ------------------------------------------------------------------ XR Plug-in Management

        [MenuItem("3DUI/Setup/XR Plug-in Management (WebXR)", priority = 23)]
        public static void SetupXRManagement()
        {
            EnsureFolder("Assets/XR");
            EnsureFolder("Assets/XR/Settings");

            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
            {
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }

            if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.WebGL))
                perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.WebGL);
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.WebGL))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.WebGL);

            var general = perTarget.SettingsForBuildTarget(BuildTargetGroup.WebGL);
            general.InitManagerOnStart = true;
            bool assigned = XRPackageMetadataStore.AssignLoader(general.AssignedSettings, typeof(WebXRLoader).FullName, BuildTargetGroup.WebGL);
            if (!assigned && !general.AssignedSettings.activeLoaders.Count.Equals(0))
                assigned = true; // already assigned

            if (!EditorBuildSettings.TryGetConfigObject("WebXR.Settings", out WebXRSettings webxrSettings) || webxrSettings == null)
            {
                webxrSettings = ScriptableObject.CreateInstance<WebXRSettings>();
                AssetDatabase.CreateAsset(webxrSettings, "Assets/XR/Settings/WebXRSettings.asset");
                EditorBuildSettings.AddConfigObject("WebXR.Settings", webxrSettings, true);
            }

            EditorUtility.SetDirty(perTarget);
            AssetDatabase.SaveAssets();
            Debug.Log($"[3DUI] WebXR loader assigned for WebGL: {assigned}");
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
