using System.Collections.Generic;
using System.IO;
using ArcherArcade.Core;
using ArcherArcade.Match;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcherArcade.EditorTools
{
    /// <summary>
    /// Generates the three scenes (CLAUDE.md: Boot, Home, Match) and puts them in Build Settings in that order.
    /// Every scene holds just its root component; all UI and world objects are built in code at runtime, so
    /// re-running this never loses hand edits (there are none). Menu ArcherArcade ▸ Build ▸ Scenes.
    /// </summary>
    public static class SceneBuilder
    {
        public const string Folder = "Assets/ArcherArcade/Scenes";

        [MenuItem("ArcherArcade/Build/Scenes")]
        public static void BuildScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Folder);
            var paths = new List<string>
            {
                Build(SceneNames.Boot, go => go.AddComponent<BootSceneRoot>(), new Color32(0x15, 0x12, 0x2E, 0xFF)),
                Build(SceneNames.Home, go => go.AddComponent<HomeSceneRoot>(), null),
                Build(SceneNames.Match, go => go.AddComponent<MatchSceneRoot>(), null)
            };
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string p in paths) scenes.Add(new EditorBuildSettingsScene(p, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[ArcherArcade] Scenes built: " + string.Join(", ", paths));
        }

        static string Build(string name, System.Action<GameObject> addRoot, Color? cameraColor)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(name + "Root");
            addRoot(root);
            if (cameraColor.HasValue)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = cameraColor.Value;
                cam.orthographic = true;
                cam.cullingMask = 0;
            }
            string path = Folder + "/" + name + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }
    }
}
