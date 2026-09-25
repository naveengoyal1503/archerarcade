using ArcherArcade.Core;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>All modes (design `modes`): 3 × 2 grid of solid cards with a coloured 62 dp icon tile.</summary>
    public sealed class ModesScreen : UiScreen
    {
        public override string Title => Loc.T("title_modes");

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            RectTransform grid = UiKit.Rect(root, "Grid");
            UiKit.Stretch(grid, 18f, 66f, 18f, 18f);
            UiKit.Column(grid, 14f, TextAnchor.UpperLeft, true, true);
            ModeCard[] all = ModeCatalog.All(Ui);
            for (int r = 0; r < 2; r++)
            {
                RectTransform row = UiKit.Rect(grid, "Row" + r);
                UiKit.Row(row, 14f, TextAnchor.MiddleLeft, true, true);
                for (int c = 0; c < 3; c++)
                {
                    int i = r * 3 + c;
                    if (i < all.Length) Card(row, all[i], p);
                }
            }
        }

        static void Card(RectTransform parent, ModeCard m, Palette p)
        {
            Button3D b = Button3D.Create(parent, m.Name, p.Solid, p.Shadow, 22f, 5f, 4f);
            RectTransform row = UiKit.Rect(b.Body, "Row");
            UiKit.Stretch(row, 16f, 14f, 16f, 14f);
            UiKit.Row(row, 14f, TextAnchor.MiddleLeft);
            RectTransform tileHolder = UiKit.Rect(row, "Tile");
            UiKit.Size(tileHolder, 62f, 66f);
            Image edge = UiKit.Box(tileHolder, "Edge", UiKit.Hex(m.Shade), 20f);
            UiKit.At(edge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), new Vector2(62f, 62f));
            Image tile = UiKit.Box(tileHolder, "Face", UiKit.Hex(m.Color), 20f);
            UiKit.At(tile.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(62f, 62f));
            TextMeshProUGUI ic = UiKit.Glyph(tile.transform, m.Icon, 32f, Color.white);
            UiKit.Stretch((RectTransform)ic.transform);
            RectTransform words = UiKit.Rect(row, "Words");
            UiKit.Column(words, 3f, TextAnchor.MiddleLeft, true, false);
            UiKit.Size(words, -1, 80f, 1f);
            TextMeshProUGUI name = UiKit.Label(words, m.Name, FontRole.Display, 19f, p.Ink);
            UiKit.Size(name, -1, 22f);
            UiKit.Fit(name);
            TextMeshProUGUI sub = UiKit.Paragraph(words, m.Sub, FontRole.BodyBold, 12f, p.InkMuted, TextAlignmentOptions.TopLeft);
            UiKit.Size(sub, -1, 34f);
            b.OnClick(m.Open);
        }
    }
}
