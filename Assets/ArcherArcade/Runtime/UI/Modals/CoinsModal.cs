using ArcherArcade.Core;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Coins sheet (SCREEN_INVENTORY #33) and "Not enough coins" (#34): the balance or the exact gap, and every way
    /// coins are earned. There is no way to buy coins.
    /// </summary>
    public sealed class CoinsModal : UiModal
    {
        readonly int _missing;

        public CoinsModal(int missing) => _missing = missing;

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Image panel = UiKit.Box(root, "Panel", p.Solid, 26f);
            panel.raycastTarget = true;
            UiKit.At(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 0f));
            UiKit.SoftShadow(panel.transform, new Color(0, 0, 0, 0.25f), 26f, 30f, 10f);
            UiKit.Column(panel, 6f, TextAnchor.UpperLeft, true, false, new RectOffset(22, 22, 18, 18));
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform head = UiKit.Rect(panel.transform, "Head");
            UiKit.Size(head, -1, 40f);
            UiKit.Row(head, 10f, TextAnchor.MiddleLeft);
            Image coin = UiKit.Box(head, "Coin", Color.white, 0);
            coin.sprite = ArtLibrary.Get(ArtLibrary.Fx, "coin");
            coin.preserveAspect = true;
            UiKit.Size(coin, 34f, 34f);
            TextMeshProUGUI title = UiKit.Label(head, _missing > 0 ? Loc.T("coins_missing_title") : Loc.T("coins_title"), FontRole.Display, 22f, p.Ink);
            UiKit.Size(title, -1, 30f, 1f);
            TextMeshProUGUI bal = UiKit.Label(head, Loc.N(ServiceLocator.Profile.Coins), FontRole.Display, 22f, p.Ink, TextAlignmentOptions.MidlineRight);
            UiKit.Size(bal, 90f, 30f);

            TextMeshProUGUI lead = UiKit.Label(panel.transform,
                _missing > 0 ? Loc.F("coins_missing_body", Fmt.Coins(_missing)) : Loc.T("coins_where"), FontRole.Body, 13f,
                _missing > 0 ? Widgets.Red : p.InkMuted);
            UiKit.Size(lead, -1, 22f);
            UiKit.Fit(lead);
            string[] keys = { "coins_src_campaign", "coins_src_headshots", "coins_src_badges", "coins_src_chests", "coins_src_daily", "coins_src_quick" };
            string[] icons = { Icons.Map, Icons.Target, Icons.MilitaryTech, Icons.Redeem, Icons.CalendarMonth, Icons.Swords };
            for (int i = 0; i < keys.Length; i++)
            {
                RectTransform row = UiKit.Rect(panel.transform, keys[i]);
                UiKit.Size(row, -1, 22f);
                UiKit.Row(row, 8f, TextAnchor.MiddleLeft);
                TextMeshProUGUI ic = UiKit.Glyph(row, icons[i], 16f, Widgets.Purple);
                UiKit.Size(ic, 20f, 20f);
                TextMeshProUGUI t = UiKit.Label(row, Loc.T(keys[i]), FontRole.BodyBold, 13f, p.Ink);
                UiKit.Size(t, -1, 20f, 1f);
                UiKit.Fit(t);
            }
            TextMeshProUGUI note = UiKit.Label(panel.transform, Loc.T("coins_no_buy"), FontRole.Body, 12f, Widgets.Green);
            UiKit.Size(note, -1, 22f);
            UiKit.Fit(note);
            RectTransform bRow = UiKit.Rect(panel.transform, "Close");
            UiKit.Size(bRow, -1, 52f);
            Button3D ok = Button3D.Styled(bRow, "OK", ButtonStyle.Primary, Loc.T("ok"), 17f);
            UiKit.At((RectTransform)ok.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(180f, 44f));
            ok.OnClick(Close);
            UIManager.PopPanel(panel.transform);
        }
    }
}
