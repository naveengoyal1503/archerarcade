using System;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>Centered card with an icon, title, message and one button (design "World locked" card).</summary>
    public sealed class InfoModal : UiModal
    {
        readonly string _icon, _title, _body, _button;
        readonly Color _face, _edge;
        readonly Action _then;

        public InfoModal(string icon, string title, string body, string button, Color face, Color edge, Action then = null)
        {
            _icon = icon;
            _title = title;
            _body = body;
            _button = button;
            _face = face;
            _edge = edge;
            _then = then;
        }

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Image panel = UiKit.Box(root, "Panel", p.Solid, 24f);
            panel.raycastTarget = true;
            UiKit.At(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 0f));
            UiKit.SoftShadow(panel.transform, new Color(0, 0, 0, 0.25f), 24f, 30f, 10f);
            UiKit.Column(panel, 6f, TextAnchor.UpperCenter, true, false, new RectOffset(22, 22, 22, 22));
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TextMeshProUGUI ic = UiKit.Glyph(panel.transform, _icon, 40f, p.Ink);
            UiKit.Size(ic, -1, 46f);
            TextMeshProUGUI t = UiKit.Label(panel.transform, _title, FontRole.Display, 22f, p.Ink, TextAlignmentOptions.Center);
            UiKit.Size(t, -1, 30f);
            UiKit.Fit(t);
            TextMeshProUGUI b = UiKit.Paragraph(panel.transform, _body, FontRole.BodyBold, 13f, p.InkMuted, TextAlignmentOptions.Top, 4f);
            UiKit.Size(b, -1, 58f);
            RectTransform row = UiKit.Rect(panel.transform, "ButtonRow");
            UiKit.Size(row, -1, 56f);
            Button3D btn = Button3D.Create(row, "OK", _face, _edge, 14f, 4f, 3f);
            UiKit.At((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(200f, 44f));
            btn.AddLabel(_button, FontRole.Display, 17f, Color.white);
            btn.OnClick(() =>
            {
                Close();
                _then?.Invoke();
            });
            UIManager.PopPanel(panel.transform);
        }
    }
}
