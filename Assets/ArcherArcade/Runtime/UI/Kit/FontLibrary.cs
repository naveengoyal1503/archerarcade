using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ArcherArcade.UI
{
    /// <summary>
    /// TextMeshPro fonts built at runtime from Resources/Fonts/*.ttf (Tools/build_fonts.py), dynamic atlases.
    /// The icon font is a fallback of every text font, so labels can also show ❤ ✓ ▶ ★ and icon glyphs inline.
    /// </summary>
    public static class FontLibrary
    {
        static readonly string[] Files = { "Fredoka-Bold", "Fredoka-SemiBold", "Nunito-ExtraBold", "Nunito-Bold", "Nunito-Black", "Icons-Rounded" };
        static TMP_FontAsset[] _fonts;

        public static TMP_FontAsset Get(FontRole role)
        {
            Warm();
            TMP_FontAsset f = _fonts[(int)role];
            return f ? f : TMP_Settings.defaultFontAsset;
        }

        public static void Warm()
        {
            if (_fonts != null) return;
            _fonts = new TMP_FontAsset[Files.Length];
            for (int i = 0; i < Files.Length; i++)
            {
                var font = Resources.Load<Font>("Fonts/" + Files[i]);
                if (!font)
                {
                    Debug.LogWarning("Missing font Resources/Fonts/" + Files[i]);
                    continue;
                }
                bool icon = i == (int)FontRole.Icon;
                TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, icon ? 64 : 72, icon ? 6 : 8, GlyphRenderMode.SDFAA,
                    1024, 1024, AtlasPopulationMode.Dynamic, true);
                if (!asset) continue;
                asset.name = Files[i];
                _fonts[i] = asset;
            }
            TMP_FontAsset iconFont = _fonts[(int)FontRole.Icon];
            if (!iconFont) return;
            for (int i = 0; i < _fonts.Length; i++)
            {
                if (i == (int)FontRole.Icon || !_fonts[i]) continue;
                if (_fonts[i].fallbackFontAssetTable == null) _fonts[i].fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                _fonts[i].fallbackFontAssetTable.Add(iconFont);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _fonts = null;
    }
}
