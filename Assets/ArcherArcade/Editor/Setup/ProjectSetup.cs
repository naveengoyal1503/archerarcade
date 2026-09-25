using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcherArcade.EditorTools
{
    /// <summary>
    /// Applies the fixed project settings from CLAUDE.md (Tech). Run once after opening the project, and again after
    /// any Unity upgrade: menu ArcherArcade ▸ Setup ▸ Apply Project Settings (also called by Batch.BuildAll).
    /// Landscape Android (IL2CPP ARM64, API 25+), no INTERNET, no Unity splash; Gamma colour space so translucent UI
    /// matches the CSS design (like MindTap); URP asset tuned for a 2D mobile game; the new Input System; TMP
    /// essentials; and the shaders the game finds at runtime always included in builds.
    /// </summary>
    public static class ProjectSetup
    {
        public const string PackageId = "com.naveencodes.archerarcade";
        public const string ProductName = "Archer Arcade";
        public const string CompanyName = "NaveenCodes";
        public const string Version = "1.0.0";
        public const int VersionCode = 1;
        const string SettingsFolder = "Assets/ArcherArcade/Settings";

        static readonly string[] AlwaysIncluded =
        {
            "Sprites/Default", "UI/Default", "TextMeshPro/Mobile/Distance Field", "TextMeshPro/Distance Field", "TextMeshPro/Sprite"
        };

        [MenuItem("ArcherArcade/Setup/Apply Project Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.Android.bundleVersionCode = Math.Max(PlayerSettings.Android.bundleVersionCode, VersionCode);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);

            // Landscape only.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;

            // IL2CPP, ARM64 only, min API 25.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // Offline game: never request INTERNET (added only in ROADMAP 2.0).
            PlayerSettings.Android.forceInternetPermission = false;

            // No Unity splash, no analytics.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.enableCrashReportAPI = false;

            // Frame rate is set by DisplayRate (Application.targetFrameRate), so vSync must not cap it.
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.vSyncCount = 0;
                QualitySettings.antiAliasing = 0;
            }

            SetIcons();
            EnsureUrp();
            UseNewInputSystem();
            ImportTmpEssentials();
            IncludeShaders();
            AssetDatabase.SaveAssets();
            Debug.Log("[ArcherArcade] Project settings applied: " + PackageId + ", landscape, IL2CPP ARM64, API 25+, Gamma, URP, Input System.");
        }

        // ---------------------------------------------------------------- icons

        const string IconFolder = "Assets/ArcherArcade/Art/Icon/";

        /// <summary>
        /// Launcher icon from Tools/art/make_store_art.py: the default icon for every size, plus adaptive layers
        /// (purple glow background, gold bow tile foreground) when the Android module is installed.
        /// </summary>
        static void SetIcons()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconFolder + "icon_1024.png");
            if (icon == null)
            {
                Debug.LogWarning("[ArcherArcade] Icon missing: run python Tools/art/make_store_art.py");
                return;
            }
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            var bg = AssetDatabase.LoadAssetAtPath<Texture2D>(IconFolder + "icon_background.png");
            var fg = AssetDatabase.LoadAssetAtPath<Texture2D>(IconFolder + "icon_foreground.png");
            Type kinds = Type.GetType("UnityEditor.Android.AndroidPlatformIconKind, UnityEditor.Android.Extensions");
            PropertyInfo adaptiveProp = kinds?.GetProperty("Adaptive", BindingFlags.Public | BindingFlags.Static);
            if (adaptiveProp == null || bg == null || fg == null) return;
            var adaptive = (PlatformIconKind)adaptiveProp.GetValue(null);
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, adaptive);
            foreach (PlatformIcon i in icons) i.SetTextures(bg, fg);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, adaptive, icons);
        }

        // ---------------------------------------------------------------- URP

        /// <summary>
        /// Creates a URP asset + Universal renderer for a 2D game (no HDR, no MSAA, no shadows, no depth texture) and
        /// assigns it to Graphics and every quality level. Reflection keeps this compiling before URP is imported.
        /// </summary>
        static void EnsureUrp()
        {
            Type assetType = Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
            Type dataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalRendererData, Unity.RenderPipelines.Universal.Runtime");
            Type baseDataType = Type.GetType("UnityEngine.Rendering.Universal.ScriptableRendererData, Unity.RenderPipelines.Universal.Runtime");
            if (assetType == null || dataType == null || baseDataType == null)
            {
                Debug.LogWarning("[ArcherArcade] URP package not loaded yet; run Apply Project Settings again after the import.");
                return;
            }
            Directory.CreateDirectory(SettingsFolder);
            string assetPath = SettingsFolder + "/URP_Mobile.asset";
            string dataPath = SettingsFolder + "/URP_Mobile_Renderer.asset";
            var asset = AssetDatabase.LoadAssetAtPath(assetPath, assetType) as RenderPipelineAsset;
            if (asset == null)
            {
                var data = ScriptableObject.CreateInstance(dataType);
                AssetDatabase.CreateAsset(data, dataPath);
                MethodInfo create = assetType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, null, new[] { baseDataType }, null);
                if (create == null)
                {
                    Debug.LogWarning("[ArcherArcade] UniversalRenderPipelineAsset.Create not found; create a URP asset by hand.");
                    return;
                }
                asset = (RenderPipelineAsset)create.Invoke(null, new object[] { data });
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            var so = new SerializedObject(asset);
            SetBool(so, "m_SupportsHDR", false);
            SetInt(so, "m_MSAA", 1);
            SetFloat(so, "m_RenderScale", 1f);
            SetBool(so, "m_RequireDepthTexture", false);
            SetBool(so, "m_RequireOpaqueTexture", false);
            SetBool(so, "m_MainLightShadowsSupported", false);
            SetBool(so, "m_AdditionalLightShadowsSupported", false);
            SetInt(so, "m_AdditionalLightsRenderingMode", 0);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        static void SetBool(SerializedObject so, string name, bool v)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.Boolean) p.boolValue = v;
        }

        static void SetInt(SerializedObject so, string name, int v)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null) return;
            if (p.propertyType == SerializedPropertyType.Integer) p.intValue = v;
            else if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = v;
        }

        static void SetFloat(SerializedObject so, string name, float v)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p != null && p.propertyType == SerializedPropertyType.Float) p.floatValue = v;
        }

        // ---------------------------------------------------------------- input, text, shaders

        /// <summary>Player Settings ▸ Active Input Handling = Input System (the editor asks to restart once).</summary>
        static void UseNewInputSystem()
        {
            UnityEngine.Object[] ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (ps == null || ps.Length == 0) return;
            var so = new SerializedObject(ps[0]);
            SerializedProperty p = so.FindProperty("activeInputHandler");
            if (p == null || p.intValue == 1) return;
            p.intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[ArcherArcade] Active Input Handling set to the Input System package. Restart the editor if Unity asks.");
        }

        /// <summary>TMP shaders + settings (Unity 6 keeps them in the uGUI package until imported).</summary>
        static void ImportTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return;
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            string full = info != null ? Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage") : null;
            if (full == null || !File.Exists(full))
            {
                Debug.LogWarning("[ArcherArcade] TMP Essential Resources not found; use Window ▸ TextMeshPro ▸ Import TMP Essential Resources.");
                return;
            }
            AssetDatabase.ImportPackage(full, false);
        }

        /// <summary>Shaders the game finds by name at runtime (trails, sprites, runtime TMP fonts) go into every build.</summary>
        static void IncludeShaders()
        {
            UnityEngine.Object[] gs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (gs == null || gs.Length == 0) return;
            var so = new SerializedObject(gs[0]);
            SerializedProperty list = so.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) return;
            foreach (string name in AlwaysIncluded)
            {
                Shader shader = Shader.Find(name);
                if (shader == null) continue;
                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) present = true;
                if (present) continue;
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
