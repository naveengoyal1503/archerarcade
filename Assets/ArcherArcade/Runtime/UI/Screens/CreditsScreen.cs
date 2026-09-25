using ArcherArcade.Core;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>Credits (design `credits`, state 14): logo, studio, developer, promises, licences and the Maa tribute.</summary>
    public sealed class CreditsScreen : UiScreen
    {
        RectTransform _heart;
        float _t;

        public override string Title => Loc.T("title_credits");
        public override bool ShowCoins => false;

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            RectTransform col = UiKit.Rect(root, "Col");
            UiKit.Stretch(col, 18f, 58f, 18f, 12f);
            UiKit.Column(col, 5f, TextAnchor.MiddleCenter, false, false);

            RectTransform logoBox = UiKit.Rect(col, "Logo");
            UiKit.Size(logoBox, 64f, 69f);
            Image edge = UiKit.Box(logoBox, "Edge", Widgets.GoldEdge, 20f);
            UiKit.At(edge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -5f), new Vector2(64f, 64f));
            Image face = UiKit.Box(logoBox, "Face", Widgets.Gold, 20f);
            UiKit.At(face.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(64f, 64f));
            Image bow = UiKit.Box(face.transform, "Bow", Color.white, 0);
            bow.sprite = ArtLibrary.Get(ArtLibrary.Ui, "logo_bow");
            bow.preserveAspect = true;
            UiKit.Stretch(bow.rectTransform, 8, 7, 8, 7);

            Line(col, Loc.T("app_name"), FontRole.Display, 34f, p.Ink, 40f);
            Line(col, Loc.T("credits_by"), FontRole.Body, 13f, p.InkMuted, 18f);
            Line(col, Loc.T("credits_dev"), FontRole.Body, 13f, p.InkMuted, 18f);
            Line(col, Loc.T("credits_free"), FontRole.Body, 12f, p.InkMuted, 18f);
            TextMeshProUGUI lic = Line(col, Loc.T("credits_licences"), FontRole.BodyBold, 10.5f, p.InkMuted, 28f);
            lic.enableWordWrapping = true;
            ((RectTransform)lic.transform).sizeDelta = new Vector2(620f, 28f);
            lic.GetComponent<LayoutElement>().preferredWidth = 620f;

            Image card = UiKit.Card(col, "Tribute", 20f);
            UiKit.Size(card, 380f, 56f);
            RectTransform row = UiKit.Rect(card.transform, "Row");
            UiKit.Stretch(row);
            UiKit.Row(row, 8f, TextAnchor.MiddleCenter);
            string tribute = Loc.T("tribute").Replace(" ❤", "");
            TextMeshProUGUI t = UiKit.Label(row, tribute, FontRole.Display, 24f, p.Ink, TextAlignmentOptions.Center);
            UiKit.Size(t, t.preferredWidth + 2f, 40f);
            TextMeshProUGUI heart = UiKit.Glyph(row, Icons.Favorite, 26f, Widgets.Red);
            UiKit.Size(heart, 30f, 30f);
            _heart = (RectTransform)heart.transform;
        }

        static TextMeshProUGUI Line(RectTransform col, string text, FontRole role, float size, Color c, float h)
        {
            TextMeshProUGUI t = UiKit.Label(col, text, role, size, c, TextAlignmentOptions.Center);
            UiKit.Size(t, Mathf.Min(760f, t.preferredWidth + 6f), h);
            return t;
        }

        public override void Tick(float dt)
        {
            _t += dt;
            if (!_heart) return;
            float beat = Mathf.Repeat(_t, 1.2f);
            float s = 1f + (beat < 0.15f ? beat / 0.15f : beat < 0.3f ? 1f - (beat - 0.15f) / 0.15f : 0f) * 0.18f;
            _heart.localScale = new Vector3(s, s, 1f);
        }
    }
}
