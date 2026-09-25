using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ArcherArcade.EditorTools
{
    /// <summary>
    /// Command-line entry points (see CLAUDE.md → Commands):
    /// -batchmode -projectPath &lt;root&gt; -quit -executeMethod ArcherArcade.EditorTools.Batch.BuildAll
    /// -batchmode -projectPath &lt;root&gt; -quit -executeMethod ArcherArcade.EditorTools.Batch.BuildApk
    /// </summary>
    public static class Batch
    {
        const string ApkPath = "Builds/Android/ArcherArcade.apk";
        const string AabPath = "Builds/Android/ArcherArcade.aab";

        /// <summary>Applies settings and re-runs every generator. Exits non-zero on compile errors.</summary>
        public static void BuildAll()
        {
            FailOnCompileErrors();
            ProjectSetup.Apply();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("ArcherArcade/Build/Android APK (dev)")]
        public static void BuildApk() => BuildAndroid(false);

        /// <summary>Release AAB. Only when Naveen asks ("aab bna").</summary>
        public static void BuildAab() => BuildAndroid(true);

        static void BuildAndroid(bool appBundle)
        {
            FailOnCompileErrors();
            ProjectSetup.Apply();
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
