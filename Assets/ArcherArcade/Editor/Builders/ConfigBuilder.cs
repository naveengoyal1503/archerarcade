using System.IO;
using ArcherArcade.Core;
using UnityEditor;
using UnityEngine;

namespace ArcherArcade.EditorTools
{
    /// <summary>
    /// Creates Resources/GameConfig.asset (every tuning number, CLAUDE.md conventions) with the design's defaults.
    /// An existing asset is kept as is, so tuning done in the inspector is never overwritten.
    /// Menu ArcherArcade ▸ Build ▸ Game Config.
    /// </summary>
    public static class ConfigBuilder
    {
        public const string Path_ = "Assets/ArcherArcade/Resources/GameConfig.asset";

        [MenuItem("ArcherArcade/Build/Game Config")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameConfig>(Path_) != null)
            {
                Debug.Log("[ArcherArcade] GameConfig already exists (kept): " + Path_);
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path_));
            var config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, Path_);
            AssetDatabase.SaveAssets();
            Debug.Log("[ArcherArcade] GameConfig created with the default tuning: " + Path_);
        }
    }
}
