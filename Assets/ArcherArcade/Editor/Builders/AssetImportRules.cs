using UnityEditor;
using UnityEngine;

namespace ArcherArcade.EditorTools
{
    /// <summary>
    /// Import settings for the generated assets (applied automatically on import):
    /// - Resources/Art atlases: sprites are cut at runtime from the JSON, so plain RGBA textures, no npot scaling,
    ///   clamped, bilinear; world atlases keep mipmaps (the camera zooms out during flight), UI pictures don't;
    ///   ASTC 4×4 on Android for crisp outlines.
    /// - Resources/Audio: short effects decompress on load, music streams; mono Vorbis.
    /// - Resources/Fonts: dynamic fonts with their data (runtime TMP font assets).
    /// </summary>
    public sealed class AssetImportRules : AssetPostprocessor
    {
        const string Art = "Assets/ArcherArcade/Resources/Art/";
        const string Audio = "Assets/ArcherArcade/Resources/Audio/";
        const string Fonts = "Assets/ArcherArcade/Resources/Fonts/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Art)) return;
            var ti = (TextureImporter)assetImporter;
            bool ui = assetPath.Contains("/Portraits") || assetPath.Contains("/Poses") || assetPath.EndsWith("/ui.png");
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.mipmapEnabled = !ui;
            ti.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.isReadable = false;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            var android = new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 4096,
                format = TextureImporterFormat.ASTC_4x4,
                textureCompression = TextureImporterCompression.CompressedHQ
            };
            ti.SetPlatformTextureSettings(android);
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Audio)) return;
            var ai = (AudioImporter)assetImporter;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool music = file.StartsWith("mus_");
            bool loop = music || file.EndsWith("_loop");
            ai.forceToMono = true;
            ai.loadInBackground = music;
            AudioImporterSampleSettings s = ai.defaultSampleSettings;
            s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = music || loop ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            s.quality = 0.6f;
            s.preloadAudioData = !music;
            ai.defaultSampleSettings = s;
        }

        void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith(Fonts)) return;
            if (assetImporter is TrueTypeFontImporter font)
            {
                font.includeFontData = true;
                font.fontTextureCase = FontTextureCase.Dynamic;
            }
        }
    }
}
