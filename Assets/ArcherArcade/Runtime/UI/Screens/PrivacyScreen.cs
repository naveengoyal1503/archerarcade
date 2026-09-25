using ArcherArcade.Core;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>Privacy: fully offline, no ads, accounts, analytics or internet permission; saved on the phone.</summary>
    public sealed class PrivacyScreen : UiScreen
    {
        public override string Title => Loc.T("title_privacy");
        public override bool ShowCoins => false;

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Image card = UiKit.Card(root, "Card", 22f);
            UiKit.Stretch(card.rectTransform, 90f, 76f, 90f, 30f);
            UiKit.Column(card, 12f, TextAnchor.UpperCenter, true, false, new RectOffset(28, 28, 22, 22));
            TextMeshProUGUI ic = UiKit.Glyph(card.transform, Icons.PrivacyTip, 44f, Widgets.Green);
            UiKit.Size(ic, -1, 50f);
            TextMeshProUGUI body = UiKit.Paragraph(card.transform, Loc.T("privacy_body"), FontRole.BodyBold, 15f, p.Ink, TextAlignmentOptions.Top, 6f);
            UiKit.Size(body, -1, 150f);
            TextMeshProUGUI promise = UiKit.Label(card.transform, Loc.T("credits_free"), FontRole.Body, 12f, p.InkMuted, TextAlignmentOptions.Center);
            UiKit.Size(promise, -1, 18f);
            UiKit.Fit(promise);
        }
    }
}
