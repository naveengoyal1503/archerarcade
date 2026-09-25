using System;
using ArcherArcade.Core;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>How to play (SCREEN_INVENTORY #31): every idea card, tap to see it again with its demo.</summary>
    public sealed class HowToPlayScreen : UiScreen
    {
        public override string Title => Loc.T("title_how_to");
        public override string Subtitle => Loc.T("how_to_sub");
        public override bool ShowCoins => false;

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            RectTransform view = UiKit.Rect(root, "View");
            UiKit.Stretch(view, 18f, 62f, 18f, 12f);
            view.gameObject.AddComponent<RectMask2D>();
            UiKit.HitArea(view);
            RectTransform content = UiKit.Rect(view, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            IdeaCard[] cards = (IdeaCard[])Enum.GetValues(typeof(IdeaCard));
            int rows = (cards.Length + 3) / 4;
            content.sizeDelta = new Vector2(0f, rows * 66f + 4f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(196f, 58f);
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(2, 2, 2, 2);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            content.gameObject.AddComponent<BadgeGridFitter>().Columns = 4;
            foreach (IdeaCard c in cards)
            {
                Button3D b = Button3D.Create(content, c.ToString(), p.Solid, p.Shadow, 14f, 3f, 2f);
                RectTransform row = UiKit.Rect(b.Body, "Row");
                UiKit.Stretch(row, 8f, 6f, 8f, 6f);
                UiKit.Row(row, 8f, TextAnchor.MiddleLeft);
                Image tile = UiKit.Box(row, "Tile", GameVisuals.IdeaColor(c), 10f);
                UiKit.Size(tile, 38f, 38f);
                TextMeshProUGUI ic = UiKit.Glyph(tile.transform, GameVisuals.IdeaIcon(c), 20f, Color.white);
                UiKit.Stretch((RectTransform)ic.transform);
                TextMeshProUGUI n = UiKit.Label(row, Loc.T("idea_" + c), FontRole.Display, 13.5f, p.Ink);
                UiKit.Size(n, -1, 36f, 1f);
                n.enableWordWrapping = true;
                UiKit.Fit(n);
                IdeaCard card = c;
                b.OnClick(() => Ui.ShowModal(new IdeaCardModal(card, false)));
            }
        }
    }
}
