using System;
using ArcherArcade.Core;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>Reusable pieces of the design: icon buttons, pills, chips, medals, section titles.</summary>
    public static class Widgets
    {
        public static readonly Color Gold = UiKit.Hex(0xFFD23F);
        public static readonly Color GoldEdge = UiKit.Hex(0xD4A514);
        public static readonly Color StarGold = UiKit.Hex(0xFFB800);
        public static readonly Color Red = UiKit.Hex(0xE5484D);
        public static readonly Color Green = UiKit.Hex(0x12A67A);
        public static readonly Color Purple = UiKit.Hex(0x6D4AFF);
        public static readonly Color PurpleEdge = UiKit.Hex(0x4A2BD1);
        public static readonly Color Navy = UiKit.Hex(0x2A2350);
        public static readonly Color Pink = UiKit.Hex(0xE83E8C);
        public static readonly Color TierGold = UiKit.Hex(0xFFC928);
        public static readonly Color TierSilver = UiKit.Hex(0xC9D3E3);
        public static readonly Color TierBronze = UiKit.Hex(0xE09A5F);

        /// <summary>44 × 44 solid icon button with the soft edge (Home top bar), optional red dot.</summary>
        public static Button3D IconButton(Transform parent, string glyph, Action click, bool dot = false, float size = 44f)
        {
            Palette p = UiKit.P;
            Button3D b = Button3D.Create(parent, "IconButton", p.Solid, p.Shadow, 14f, 4f, 3f);
            UiKit.Size(b, size, size);
            b.AddIcon(glyph, 22f, p.Ink);
            if (dot)
            {
                Image ring = UiKit.Disc(b.Body, "DotRing", p.Solid);
                UiKit.At(ring.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-3.5f, -3.5f), new Vector2(17f, 17f));
                Image d = UiKit.Disc(b.Body, "Dot", Red);
                UiKit.At(d.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-3.5f, -3.5f), new Vector2(13f, 13f));
            }
            b.OnClick(click);
            return b;
        }

        /// <summary>The purple back button of every header (44 × 44, ←).</summary>
        public static Button3D BackButton(Transform parent, Action click)
        {
            Button3D b = Button3D.Create(parent, "Back", Purple, PurpleEdge, 14f, 4f, 3f);
            UiKit.Size(b, 44f, 44f);
            b.AddIcon(Icons.ArrowBack, 24f, Color.white);
            b.Sound = Feel.SoundId.UiBack;
            b.OnClick(click);
            return b;
        }

        /// <summary>
        /// Glass pill (h 36, radius 18, card + line border) with an icon and a Fredoka 17 number: coins, stars.
        /// </summary>
        public static TextMeshProUGUI Pill(Transform parent, Sprite icon, string glyph, Color glyphColor, string value, out RectTransform root)
        {
            Palette p = UiKit.P;
            Image bg = UiKit.Card(parent, "Pill", 18f);
            root = bg.rectTransform;
            UiKit.Size(bg, -1, 36f);
            UiKit.Row(bg, 6f, TextAnchor.MiddleLeft, false, false, new RectOffset(8, 13, 0, 0));
            bg.GetComponent<HorizontalLayoutGroup>().childControlHeight = false;
            if (icon)
            {
                Image im = UiKit.Box(bg.transform, "Icon", Color.white, 0);
                im.sprite = icon;
                im.type = Image.Type.Simple;
                im.preserveAspect = true;
                UiKit.Size(im, 22f, 22f);
                im.rectTransform.sizeDelta = new Vector2(22f, 22f);
            }
            else
            {
                TextMeshProUGUI g = UiKit.Glyph(bg.transform, glyph, 20f, glyphColor);
                UiKit.Size(g, 22f, 22f);
            }
            TextMeshProUGUI t = UiKit.Label(bg.transform, value, FontRole.Display, 17f, p.Ink, TextAlignmentOptions.MidlineLeft);
            t.rectTransform.sizeDelta = new Vector2(t.preferredWidth, 24f);
            var fitter = t.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rootFit = bg.gameObject.AddComponent<ContentSizeFitter>();
            rootFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return t;
        }

        /// <summary>Coin pill that follows the wallet (updates on every coin change, with a little bump).</summary>
        public static RectTransform CoinPill(Transform parent)
        {
            TextMeshProUGUI t = Pill(parent, ArtLibrary.Get(ArtLibrary.Fx, "coin"), Icons.MonetizationOn, Gold,
                Loc.N(ServiceLocator.Profile?.Coins ?? 0), out RectTransform root);
            root.gameObject.AddComponent<CoinCounter>().Bind(t);
            return root;
        }

        /// <summary>Small rounded chip (e.g. "New!", element tags).</summary>
        public static TextMeshProUGUI Chip(Transform parent, string text, Color bg, Color fg, float size = 11f, float radius = 6f)
        {
            Image b = UiKit.Box(parent, "Chip", bg, radius);
            UiKit.Row(b, 4f, TextAnchor.MiddleCenter, false, false, new RectOffset(6, 6, 2, 2));
            TextMeshProUGUI t = UiKit.Label(b.transform, text, FontRole.Display, size, fg, TextAlignmentOptions.Center);
            var fit = b.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return t;
        }

        public static Color TierColor(Logic.Meta.BadgeTier tier)
        {
            switch (tier)
            {
                case Logic.Meta.BadgeTier.Gold: return TierGold;
                case Logic.Meta.BadgeTier.Silver: return TierSilver;
                case Logic.Meta.BadgeTier.Bronze: return TierBronze;
                default: return UiKit.P.Track;
            }
        }

        /// <summary>Round badge medal: tier-coloured disc with the badge's icon and a hard shadow.</summary>
        public static RectTransform Medal(Transform parent, string glyph, Logic.Meta.BadgeTier tier, float size, bool dim = false)
        {
            RectTransform root = UiKit.Rect(parent, "Medal");
            root.sizeDelta = new Vector2(size, size);
            UiKit.Size(root, size, size);
            Image sh = UiKit.Disc(root, "Shadow", new Color(0, 0, 0, 0.15f));
            UiKit.Stretch(sh.rectTransform, 0, 3, 0, -3);
            Image disc = UiKit.Disc(root, "Disc", TierColor(tier));
            UiKit.Stretch(disc.rectTransform);
            TextMeshProUGUI g = UiKit.Glyph(root, glyph, size * 0.5f, tier == Logic.Meta.BadgeTier.None ? UiKit.P.InkMuted : Navy);
            UiKit.Stretch((RectTransform)g.transform);
            if (dim)
            {
                var cg = root.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0.5f;
            }
            return root;
        }

        /// <summary>Section heading in Fredoka (e.g. "Arrows", "Sound").</summary>
        public static TextMeshProUGUI Heading(Transform parent, string text, float size = 16f)
        {
            TextMeshProUGUI t = UiKit.Label(parent, text, FontRole.Display, size, UiKit.P.Ink);
            UiKit.Size(t, -1, size + 6f);
            return t;
        }

        /// <summary>Muted caption in Nunito 800.</summary>
        public static TextMeshProUGUI Caption(Transform parent, string text, float size = 12f, bool wrap = false)
        {
            TextMeshProUGUI t = wrap
                ? UiKit.Paragraph(parent, text, FontRole.Body, size, UiKit.P.InkMuted, TextAlignmentOptions.TopLeft)
                : UiKit.Label(parent, text, FontRole.Body, size, UiKit.P.InkMuted);
            return t;
        }

        /// <summary>Row of three stars (★ gold / ☆ muted) for map nodes and cards.</summary>
        public static TextMeshProUGUI Stars(Transform parent, int stars, float size, Color on, Color off)
        {
            string s = "";
            for (int i = 0; i < 3; i++) s += i < stars ? "<color=#" + ColorUtility.ToHtmlStringRGBA(on) + ">★</color>" : "<color=#" + ColorUtility.ToHtmlStringRGBA(off) + ">★</color>";
            TextMeshProUGUI t = UiKit.Label(parent, s, FontRole.Display, size, on, TextAlignmentOptions.Center, "Stars");
            t.characterSpacing = 4f;
            return t;
        }
    }
}
