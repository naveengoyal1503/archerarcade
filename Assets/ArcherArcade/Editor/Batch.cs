using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArcherArcade.EditorTools
{
    /// <summary>
    /// Command-line entry points (see CLAUDE.md → Commands):
    /// -batchmode -projectPath &lt;root&gt; -quit -executeMethod ArcherArcade.EditorTools.Batch.BuildAll
    /// -batchmode -projectPath &lt;root&gt; -quit -executeMethod ArcherArcade.EditorTools.Batch.BuildApk
    /// -projectPath &lt;root&gt; -executeMethod ArcherArcade.EditorTools.Batch.Capture   (needs a Game view: not -batchmode)
    /// </summary>
    public static class Batch
    {
        const string ApkPath = "Builds/Android/ArcherArcade.apk";
        const string AabPath = "Builds/Android/ArcherArcade.aab";

        /// <summary>Applies settings and re-runs every generator. Exits non-zero on compile errors.</summary>
        [MenuItem("ArcherArcade/Build/All (settings, config, scenes, reimport)")]
        public static void BuildAll()
        {
            FailOnCompileErrors();
            ProjectSetup.Apply();
            ConfigBuilder.Build();
            Reimport("Assets/ArcherArcade/Resources/Art");
            Reimport("Assets/ArcherArcade/Resources/Audio");
            Reimport("Assets/ArcherArcade/Resources/Fonts");
            SceneBuilder.BuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[ArcherArcade] BuildAll done.");
        }

        static void Reimport(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                AssetDatabase.ImportAsset(folder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }

        [MenuItem("ArcherArcade/Build/Android APK (dev)")]
        public static void BuildApk() => BuildAndroid(false);

        /// <summary>Release AAB. Only when Naveen asks ("aab bna").</summary>
        public static void BuildAab() => BuildAndroid(true);

        static void BuildAndroid(bool appBundle)
        {
            FailOnCompileErrors();
            ProjectSetup.Apply();
            ConfigBuilder.Build();
            if (EditorBuildSettings.scenes.Length == 0) SceneBuilder.BuildScenes();
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) Fail("No scenes in Build Settings. Run ArcherArcade ▸ Build ▸ Scenes first.");

            EditorUserBuildSettings.buildAppBundle = appBundle;
            string path = appBundle ? AabPath : ApkPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded) Fail("Android build failed: " + report.summary.result);
            Debug.Log("[ArcherArcade] Built " + path + " (" + report.summary.totalSize / (1024 * 1024) + " MB)");
        }

        /// <summary>
        /// Design check: plays the game and saves a screenshot of every screen (light and dark) and of a match to
        /// Builds/Capture/. Needs a Game view, so run it from the menu or without -batchmode.
        /// </summary>
        [MenuItem("ArcherArcade/Capture/Screens + Match")]
        public static void Capture()
        {
            FailOnCompileErrors();
            if (EditorBuildSettings.scenes.Length == 0) SceneBuilder.BuildScenes();
            CaptureHook.Arm(Application.isBatchMode);
            EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
            EditorApplication.EnterPlaymode();
        }

        public static void FailOnCompileErrors()
        {
            if (EditorUtility.scriptCompilationFailed) Fail("Scripts have compile errors.");
        }

        static void Fail(string message)
        {
            Debug.LogError("[ArcherArcade] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw new InvalidOperationException(message);
        }
    }
}
