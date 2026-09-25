using ArcherArcade.Core;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Small builder helpers for code-built uGUI (no prefabs): rectangles placed like the design's CSS (top-left
    /// origin, dp units at the 844 × 390 reference), rounded boxes, borders, shadows, labels, icons and layout groups.
    /// </summary>
    public static class UiKit
    {
        public static Palette P => ServiceLocator.Theme != null ? ServiceLocator.Theme.Palette : Palette.Light;
        public static bool Dark => ServiceLocator.Theme != null && ServiceLocator.Theme.IsDark;

        public static Color Hex(uint rgb, float a = 1f) => Palette.Hex(rgb, a);

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent ? parent.gameObject.layer : 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Fills the parent with CSS-like insets (left, top, right, bottom).</summary>
        public static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Absolute box from the parent's top-left corner (CSS left/top/width/height).</summary>
        public static RectTransform TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        /// <summary>Box anchored to one point of the parent (0..1) with its own pivot.</summary>
        public static RectTransform At(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        /// <summary>Horizontal band: full width with insets, fixed height from the top.</summary>
        public static RectTransform TopBand(RectTransform rt, float top, float height, float left = 0, float right = 0)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -top - height);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform BottomBand(RectTransform rt, float bottom, float height, float left = 0, float right = 0)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, bottom + height);
            return rt;
        }

        public static Image Box(Transform parent, string name, Color color, float radius)
        {
            RectTransform rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = radius > 0 ? ShapeSprites.RoundedRect(radius) : ShapeSprites.White;
            img.type = radius > 0 ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Circle image (for dots, avatars, knobs).</summary>
        public static Image Disc(Transform parent, string name, Color color)
        {
            RectTransform rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = ShapeSprites.Circle;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Border line on top of a box (CSS border), stretched over the parent.</summary>
        public static Image Border(Transform parent, Color color, float radius, float thickness)
        {
            Image img = Box(parent, "Border", color, 0);
            img.sprite = ShapeSprites.Outline(radius, thickness);
            img.type = Image.Type.Sliced;
            Stretch(img.rectTransform);
            return img;
        }

        /// <summary>Soft drop shadow behind a box (CSS box-shadow 0 y blur color); added as the parent's first child.</summary>
        public static Image SoftShadow(Transform parent, Color color, float radius, float blur, float y)
        {
            Image img = Box(parent, "Shadow", color, 0);
            img.sprite = ShapeSprites.Shadow(radius, blur);
            img.type = Image.Type.Sliced;
            float e = blur;
            Stretch(img.rectTransform, -e, -e + y, -e, -e - y);
            img.transform.SetAsFirstSibling();
            return img;
        }

        /// <summary>Glass card (DESIGN_TOKENS: card color + 1.5 dp line border).</summary>
        public static Image Card(Transform parent, string name, float radius = 22f)
        {
            Image img = Box(parent, name, P.Card, radius);
            Border(img.transform, P.Line, radius, 1.5f);
            return img;
        }

        /// <summary>Solid card (var(--solid)) with the design's hard shadow 0 5 0 var(--sh).</summary>
        public static Image SolidCard(Transform parent, string name, float radius, float depth)
        {
            RectTransform root = Rect(parent, name);
            if (depth > 0)
            {
                Image edge = Box(root, "Edge", P.Shadow, radius);
                Stretch(edge.rectTransform, 0, depth, 0, -depth);
            }
            Image face = Box(root, "Face", P.Solid, radius);
            Stretch(face.rectTransform);
            return face;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, FontRole role, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, string name = "Label")
        {
            RectTransform rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = FontLibrary.Get(role);
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.richText = true;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.margin = Vector4.zero;
            return t;
        }

        /// <summary>Label that wraps inside its box and shrinks (down to 70 %) if the text is long (Hinglish).</summary>
        public static TextMeshProUGUI Paragraph(Transform parent, string text, FontRole role, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, float lineSpacing = 0f)
        {
            TextMeshProUGUI t = Label(parent, text, role, size, color, align, "Text");
            t.enableWordWrapping = true;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = size * 0.7f;
            t.lineSpacing = lineSpacing;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        /// <summary>One-line label that shrinks to fit its box instead of overflowing.</summary>
        public static TextMeshProUGUI Fit(TextMeshProUGUI t, float minScale = 0.65f)
        {
            t.enableAutoSizing = true;
            t.fontSizeMax = t.fontSize;
            t.fontSizeMin = t.fontSize * minScale;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        public static TextMeshProUGUI Glyph(Transform parent, string icon, float size, Color color)
        {
            TextMeshProUGUI t = Label(parent, icon, FontRole.Icon, size, color, TextAlignmentOptions.Center, "Icon");
            ((RectTransform)t.transform).sizeDelta = new Vector2(size * 1.25f, size * 1.25f);
            return t;
        }

        /// <summary>Title with the design's hard text shadow (text-shadow 0 y 0 color).</summary>
        public static TextMeshProUGUI ShadowLabel(Transform parent, string text, FontRole role, float size, Color color,
            Color shadow, float y, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            RectTransform box = Rect(parent, "Title");
            TextMeshProUGUI back = Label(box, text, role, size, shadow, align, "Shadow");
            Stretch((RectTransform)back.transform, 0, y, 0, -y);
            TextMeshProUGUI front = Label(box, text, role, size, color, align, "Text");
            Stretch((RectTransform)front.transform);
            box.gameObject.AddComponent<ShadowTextSync>().Bind(front, back);
            return front;
        }

        public static RawImage Picture(Transform parent, string name, Texture tex)
        {
            RectTransform rt = Rect(parent, name);
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.raycastTarget = false;
            return raw;
        }

        public static HorizontalLayoutGroup Row(Component c, float spacing, TextAnchor align = TextAnchor.MiddleLeft,
            bool expandWidth = false, bool expandHeight = false, RectOffset pad = null)
        {
            var g = c.gameObject.GetComponent<HorizontalLayoutGroup>() ?? c.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = expandWidth;
            g.childForceExpandHeight = expandHeight;
            g.padding = pad ?? new RectOffset();
            return g;
        }

        public static VerticalLayoutGroup Column(Component c, float spacing, TextAnchor align = TextAnchor.UpperLeft,
            bool expandWidth = true, bool expandHeight = false, RectOffset pad = null)
        {
            var g = c.gameObject.GetComponent<VerticalLayoutGroup>() ?? c.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = expandWidth;
            g.childForceExpandHeight = expandHeight;
            g.padding = pad ?? new RectOffset();
            return g;
        }

        public static GridLayoutGroup Grid(Component c, Vector2 cell, Vector2 spacing, int columns)
        {
            var g = c.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell;
            g.spacing = spacing;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns;
            g.childAlignment = TextAnchor.UpperLeft;
            return g;
        }

        public static LayoutElement Size(Component c, float width = -1, float height = -1, float flexWidth = -1, float flexHeight = -1)
        {
            var e = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (width >= 0) { e.preferredWidth = width; e.minWidth = width; }
            if (height >= 0) { e.preferredHeight = height; e.minHeight = height; }
            if (flexWidth >= 0) e.flexibleWidth = flexWidth;
            if (flexHeight >= 0) e.flexibleHeight = flexHeight;
            return e;
        }

        /// <summary>Flexible spacer for rows / columns (CSS flex:1 div).</summary>
        public static RectTransform Spacer(Transform parent, float flex = 1f)
        {
            RectTransform rt = Rect(parent, "Spacer");
            Size(rt, -1, -1, flex, flex);
            return rt;
        }

        /// <summary>Makes a rect clickable without a visual (transparent raycast target).</summary>
        public static Image HitArea(RectTransform rt)
        {
            var img = rt.gameObject.GetComponent<Image>() ?? rt.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            img.raycastTarget = true;
            return img;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
