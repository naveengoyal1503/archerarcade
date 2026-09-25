using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ArcherArcade.EditorTools
{
    /// <summary>
    /// Applies the fixed project settings from CLAUDE.md (Tech). Run once after opening the project, and again after
    /// any Unity upgrade: menu ArcherArcade ▸ Setup ▸ Apply Project Settings (also called by Batch.BuildAll).
    /// </summary>
    public static class ProjectSetup
    {
        public const string PackageId = "com.naveencodes.archerarcade";
        public const string ProductName = "Archer Arcade";
        public const string CompanyName = "NaveenCodes";

        [MenuItem("ArcherArcade/Setup/Apply Project Settings")]
        public static void Apply()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);

            // Landscape only.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // IL2CPP, ARM64 only, min API 25.
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;

            // Offline game: never request INTERNET (added only in ROADMAP 2.0).
            PlayerSettings.Android.forceInternetPermission = false;

            // No Unity splash, no analytics.
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.enableCrashReportAPI = false;

            Debug.Log("[ArcherArcade] Project settings applied: " + PackageId + ", landscape, IL2CPP ARM64, API 25+.");
        }
    }
}
