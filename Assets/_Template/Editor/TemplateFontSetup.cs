using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// TextMeshPro setup (menu: 3DUI > Setup > Fonts).
    ///  1. Imports TMP Essential Resources (default font + settings) if missing.
    ///  2. For every .ttf/.otf in Assets/_Template/Fonts, creates a *dynamic* TMP font asset and registers it
    ///     as a global fallback, so any glyph missing from the default font (e.g. Hangul) is rendered from it.
    ///     Put an OFL font such as Noto Sans KR there for Korean.
    /// </summary>
    public static class TemplateFontSetup
    {
        public const string FontDir = "Assets/_Template/Fonts";
        private const string EssentialsCheckPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("3DUI/Setup/Fonts (TextMeshPro + Korean fallback)", priority = 24)]
        public static void SetupFonts()
        {
            if (!ImportEssentials()) return; // import is asynchronous; run again after it finishes
            RegisterFallbackFonts();
        }

        /// <summary>Returns true if the essentials are already present.</summary>
        public static bool ImportEssentials()
        {
            if (File.Exists(EssentialsCheckPath)) return true;
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui/package.json");
            string pkg = info == null ? null : Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (pkg == null || !File.Exists(pkg))
            {
                Debug.LogError("[3DUI] TMP Essential Resources package not found.");
                return false;
            }
            AssetDatabase.ImportPackage(pkg, false);
            Debug.Log("[3DUI] Importing TMP Essential Resources... run 3DUI > Setup > Fonts again once it finishes to register fallback fonts.");
            return false;
        }

        /// <summary>Command line: imports the essentials, waits for the import, registers fonts, exits.</summary>
        public static void SetupFontsFromCommandLine()
        {
            if (File.Exists(EssentialsCheckPath))
            {
                RegisterFallbackFonts();
                EditorApplication.Exit(0);
                return;
            }
            AssetDatabase.importPackageCompleted += _ =>
            {
                AssetDatabase.Refresh();
                RegisterFallbackFonts();
                EditorApplication.Exit(0);
            };
            AssetDatabase.importPackageFailed += (_, error) =>
            {
                Debug.LogError("[3DUI] TMP import failed: " + error);
                EditorApplication.Exit(1);
            };
            ImportEssentials();
        }

        public static void RegisterFallbackFonts()
        {
            TemplateProjectSetup.EnsureFolder(FontDir);
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(EssentialsCheckPath);
            if (settings == null)
            {
                Debug.LogError("[3DUI] TMP Settings not found. Import TMP Essential Resources first.");
                return;
            }

            var fontFiles = Directory.GetFiles(FontDir)
                .Where(f => f.EndsWith(".ttf", System.StringComparison.OrdinalIgnoreCase) || f.EndsWith(".otf", System.StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Replace('\\', '/'))
                .ToArray();

            if (fontFiles.Length == 0)
            {
                Debug.LogWarning($"[3DUI] No font files in {FontDir}. Add e.g. NotoSansKR-Regular.ttf for Korean text, then run 3DUI > Setup > Fonts.");
                return;
            }

            var so = new SerializedObject(settings);
            var fallbacks = so.FindProperty("m_fallbackFontAssets");

            foreach (var fontPath in fontFiles)
            {
                string assetPath = Path.ChangeExtension(fontPath, null) + " SDF.asset";
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                if (fontAsset == null)
                {
                    var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
                    if (font == null) continue;
                    fontAsset = TMP_FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                        AtlasPopulationMode.Dynamic, true);
                    fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
                    AssetDatabase.CreateAsset(fontAsset, assetPath);
                    // Atlas textures and material must live inside the font asset to be saved.
                    for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                    {
                        var tex = fontAsset.atlasTextures[i];
                        if (tex == null) continue;
                        tex.name = fontAsset.name + " Atlas" + (i > 0 ? " " + i : "");
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }
                    fontAsset.material.name = fontAsset.name + " Material";
                    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                    fontAsset.isMultiAtlasTexturesEnabled = true;
                    EditorUtility.SetDirty(fontAsset);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[3DUI] Created dynamic TMP font asset {assetPath}");
                }

                bool present = false;
                for (int i = 0; i < fallbacks.arraySize; i++)
                    if (fallbacks.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset) present = true;
                if (!present)
                {
                    fallbacks.arraySize++;
                    fallbacks.GetArrayElementAtIndex(fallbacks.arraySize - 1).objectReferenceValue = fontAsset;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[3DUI] TMP fallback fonts registered: {fallbacks.arraySize}");
        }
    }
}
