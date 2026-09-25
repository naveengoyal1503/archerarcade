using ArcherArcade.Core;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Badges (design `badges`): the pinned panel (3 shown on the Home profile) and the wall of 22 badges with
    /// bronze / silver / gold tiers and progress. Tap a badge for details and to pin or unpin it.
    /// </summary>
    public sealed class BadgesScreen : UiScreen
    {
        public override string Title => Loc.T("title_badges");

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Profile profile = ServiceLocator.Profile;
            RectTransform row = UiKit.Rect(root, "Row");
            UiKit.Stretch(row, 18f, 62f, 18f, 14f);
            UiKit.Row(row, 14f, TextAnchor.UpperLeft, false, true);

            Image pinned = UiKit.Card(row, "Pinned", 22f);
            UiKit.Size(pinned, 176f, -1, 0f, 1f);
            UiKit.Column(pinned, 8f, TextAnchor.UpperLeft, true, false, new RectOffset(12, 12, 12, 12));
            TextMeshProUGUI pt = UiKit.Label(pinned.transform, Loc.T("badges_pinned"), FontRole.Display, 16f, p.Ink);
            UiKit.Size(pt, -1, 20f);
            TextMeshProUGUI help = UiKit.Paragraph(pinned.transform, Loc.T("badges_pinned_help"), FontRole.BodyBold, 11f, p.InkMuted, TextAlignmentOptions.TopLeft, 3f);
            UiKit.Size(help, -1, 44f);
            int count = 0;
            foreach (string id in profile.Data.PinnedBadges)
            {
                BadgeDef b = BadgeCatalog.ById(id);
                if (b == null) continue;
                count++;
                Image r = UiKit.Box(pinned.transform, "Pin", p.Solid, 12f);
                UiKit.Size(r, -1, 44f);
                UiKit.Row(r, 8f, TextAnchor.MiddleLeft, false, false, new RectOffset(6, 6, 0, 0));
                Widgets.Medal(r.transform, Icons.ByName(b.Icon), profile.BadgeTierOf(id), 32f);
                TextMeshProUGUI n = UiKit.Label(r.transform, b.Name, FontRole.Body, 12f, p.Ink);
                UiKit.Size(n, -1, 20f, 1f);
                UiKit.Fit(n);
            }
            if (count == 0)
            {
                TextMeshProUGUI none = UiKit.Paragraph(pinned.transform, Loc.T("badges_none_pinned"), FontRole.BodyBold, 12f, p.InkMuted, TextAlignmentOptions.TopLeft);
                UiKit.Size(none, -1, 40f);
            }
            UiKit.Spacer(pinned.transform);
            int earned = 0;
            foreach (BadgeDef b in BadgeCatalog.All) if (profile.BadgeTierOf(b.Id) != BadgeTier.None) earned++;
            TextMeshProUGUI e = UiKit.Label(pinned.transform, earned + " / " + BadgeCatalog.All.Count + "<size=12><color=" + Fmt.Hex(p.InkMuted) + ">" +
                Loc.T("badges_earned") + "</color></size>", FontRole.Display, 22f, p.Ink);
            UiKit.Size(e, -1, 28f);

            // Wall: vertical scroll, 6 columns, 98 dp rows.
            RectTransform view = UiKit.Rect(row, "Wall");
            UiKit.Size(view, -1, -1, 1f, 1f);
            view.gameObject.AddComponent<RectMask2D>();
            UiKit.HitArea(view);
            RectTransform content = UiKit.Rect(view, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            int rows = (BadgeCatalog.All.Count + 5) / 6;
            content.sizeDelta = new Vector2(0f, rows * 106f + 4f);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(90f, 98f);
            grid.spacing = new Vector2(8f, 8f);
            grid.padding = new RectOffset(2, 2, 2, 2);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            var fitter = content.gameObject.AddComponent<BadgeGridFitter>();
            fitter.Columns = 6;
            foreach (BadgeDef b in BadgeCatalog.All) Tile(content, b, profile, p);
        }

        void Tile(RectTransform parent, BadgeDef b, Profile profile, Palette p)
        {
            BadgeTier tier = profile.BadgeTierOf(b.Id);
            bool pin = profile.Data.PinnedBadges.Contains(b.Id);
            Button3D t = Button3D.Create(parent, b.Id, p.Solid, p.Shadow, 16f, 3f, 2f);
            if (pin) UiKit.Border(t.Face.transform, Widgets.Purple, 16f, 3f);
            RectTransform col = UiKit.Rect(t.Body, "Col");
            UiKit.Stretch(col, 4f, 6f, 4f, 6f);
            UiKit.Column(col, 3f, TextAnchor.MiddleCenter, true, false);
            RectTransform medalRow = UiKit.Rect(col, "MedalRow");
            UiKit.Size(medalRow, -1, 40f);
            RectTransform medal = Widgets.Medal(medalRow, Icons.ByName(b.Icon), tier, 38f, tier == BadgeTier.None);
            UiKit.At(medal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f));
            TextMeshProUGUI n = UiKit.Label(col, b.Name, FontRole.Body, 10.5f, p.Ink, TextAlignmentOptions.Center);
            UiKit.Size(n, -1, 13f);
            UiKit.Fit(n);
            string meta = tier == BadgeTier.None ? profile.StatValue(b.Stat) + "/" + b.Bronze
                : TierName(tier) + (pin ? " " + Icons.PushPin : "");
            TextMeshProUGUI m = UiKit.Label(col, meta, FontRole.Body, 9.5f, p.InkMuted, TextAlignmentOptions.Center);
            UiKit.Size(m, -1, 12f);
            UiKit.Fit(m);
            t.OnClick(() => Ui.ShowModal(new BadgeModal(b.Id, () => Ui.Refresh())));
        }

        public static string TierName(BadgeTier t) =>
            t == BadgeTier.Gold ? Loc.T("tier_gold") : t == BadgeTier.Silver ? Loc.T("tier_silver") : Loc.T("tier_bronze");
    }
}
