using System;
using ArcherArcade.Core;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>Badge detail: medal, name, what it counts, the three tiers with their targets, progress, pin.</summary>
    public sealed class BadgeModal : UiModal
    {
        readonly string _id;
        readonly Action _changed;

        public BadgeModal(string id, Action changed)
        {
            _id = id;
            _changed = changed;
        }

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Profile profile = ServiceLocator.Profile;
            BadgeDef b = BadgeCatalog.ById(_id);
            BadgeTier tier = profile.BadgeTierOf(_id);
            Image panel = UiKit.Box(root, "Panel", p.Solid, 26f);
            panel.raycastTarget = true;
            UiKit.At(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 0f));
            UiKit.SoftShadow(panel.transform, new Color(0, 0, 0, 0.25f), 26f, 30f, 10f);
            UiKit.Column(panel, 8f, TextAnchor.UpperLeft, true, false, new RectOffset(20, 20, 18, 18));
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform head = UiKit.Rect(panel.transform, "Head");
            UiKit.Size(head, -1, 64f);
            UiKit.Row(head, 12f, TextAnchor.MiddleLeft);
            Widgets.Medal(head, Icons.ByName(b.Icon), tier, 60f, tier == BadgeTier.None);
            RectTransform words = UiKit.Rect(head, "Words");
            UiKit.Size(words, -1, 60f, 1f);
            UiKit.Column(words, 2f, TextAnchor.MiddleLeft, true, false);
            TextMeshProUGUI n = UiKit.Label(words, b.Name, FontRole.Display, 22f, p.Ink);
            UiKit.Size(n, -1, 28f);
            TextMeshProUGUI d = UiKit.Label(words, Loc.T("badge_" + b.Id + "_desc"), FontRole.Body, 12.5f, p.InkMuted);
            UiKit.Size(d, -1, 18f);
            UiKit.Fit(d);

            long value = profile.StatValue(b.Stat);
            for (BadgeTier t = BadgeTier.Bronze; t <= BadgeTier.Gold; t++)
            {
                bool got = tier >= t;
                Image row = UiKit.Box(panel.transform, t.ToString(), p.Track, 12f);
                UiKit.Size(row, -1, 34f);
                UiKit.Row(row, 8f, TextAnchor.MiddleLeft, false, false, new RectOffset(10, 12, 0, 0));
                Image dot = UiKit.Disc(row.transform, "Tier", Widgets.TierColor(t));
                UiKit.Size(dot, 20f, 20f);
                TextMeshProUGUI tn = UiKit.Label(row.transform, BadgesScreen.TierName(t), FontRole.Display, 14f, p.Ink);
                UiKit.Size(tn, 80f, 20f);
                string target = b.GoldIsBossThreeStars && t == BadgeTier.Gold ? "★★★" : Loc.N(b.Threshold(t));
                TextMeshProUGUI tv = UiKit.Label(row.transform, target, FontRole.Body, 13f, p.InkMuted);
                UiKit.Size(tv, -1, 20f, 1f);
                TextMeshProUGUI ok = UiKit.Glyph(row.transform, got ? Icons.CheckCircle : Icons.RadioButtonUnchecked, 18f, got ? Widgets.Green : p.InkMuted);
                UiKit.Size(ok, 22f, 22f);
            }
            if (tier < BadgeTier.Gold)
            {
                RectTransform prog = UiKit.Rect(panel.transform, "Progress");
                UiKit.Size(prog, -1, 18f);
                UiKit.Row(prog, 8f, TextAnchor.MiddleLeft);
                ProgressBar bar = ProgressBar.Create(prog, p.Track, Widgets.Green, 10f, 5f);
                UiKit.Size(bar, -1, 10f, 1f);
                bar.SetValue((float)profile.BadgeProgress(b), false);
                long next = b.Threshold(tier + 1);
                TextMeshProUGUI pv = UiKit.Label(prog, Loc.N(Math.Min(value, next)) + "/" + Loc.N(next), FontRole.Body, 11f, p.InkMuted, TextAlignmentOptions.MidlineRight);
                UiKit.Size(pv, 80f, 16f);
            }

            RectTransform buttons = UiKit.Rect(panel.transform, "Buttons");
            UiKit.Size(buttons, -1, 50f);
            UiKit.Row(buttons, 10f, TextAnchor.MiddleCenter, true, true, new RectOffset(0, 0, 4, 4));
            Button3D close = Button3D.Off(buttons, "Close", Loc.T("close"), 16f);
            close.OnClick(Close);
            if (tier != BadgeTier.None)
            {
                bool pinned = profile.Data.PinnedBadges.Contains(_id);
                Button3D pin = Button3D.Styled(buttons, "Pin", ButtonStyle.Primary, Icons.PushPin + " " + (pinned ? Loc.T("badges_unpin") : Loc.T("badges_pin")), 15f);
                pin.OnClick(() =>
                {
                    if (!profile.TogglePin(_id))
                    {
                        Ui.Toast(Loc.T("badges_max_pins"));
                        return;
                    }
                    ServiceLocator.CommitProfile();
                    Close();
                    _changed?.Invoke();
                });
            }
            UIManager.PopPanel(panel.transform);
        }
    }
}
