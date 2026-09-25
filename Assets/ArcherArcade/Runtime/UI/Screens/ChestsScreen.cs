using ArcherArcade.Core;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Chests (design `chests`, state 11): the four World 1 chests (after levels 5 / 10 / 15 / 20) with their art,
    /// contents, and Open / "Clear level N" / "Opened ✓". Contents are earned; nothing is for sale.
    /// </summary>
    public sealed class ChestsScreen : UiScreen
    {
        static readonly string[] Art = { "chest_wood", "chest_silver", "chest_gold", "chest_forest" };
        readonly RectTransform[] _float = new RectTransform[4];
        float _t;

        public override string Title => Loc.T("title_chests");

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Profile profile = ServiceLocator.Profile;
            RectTransform col = UiKit.Rect(root, "Col");
            UiKit.Stretch(col, 18f, 64f, 18f, 16f);
            UiKit.Column(col, 10f, TextAnchor.UpperLeft, true, false);
            RectTransform grid = UiKit.Rect(col, "Grid");
            UiKit.Size(grid, -1, -1, 1f, 1f);
            UiKit.Row(grid, 14f, TextAnchor.UpperLeft, true, true);
            for (int i = 1; i <= Chests.Count; i++) Card(grid, i, profile, p);
            TextMeshProUGUI note = UiKit.Label(col, Loc.T("chest_note"), FontRole.Body, 12f, p.InkMuted, TextAlignmentOptions.Center);
            UiKit.Size(note, -1, 18f);
            UiKit.Fit(note);
        }

        void Card(RectTransform grid, int chest, Profile profile, Palette p)
        {
            bool ready = profile.ChestAvailable(chest);
            bool opened = profile.Data.ChestsOpened.Contains(chest);
            Image card = UiKit.Card(grid, "Chest" + chest, 22f);
            UiKit.Column(card, 6f, TextAnchor.UpperCenter, true, false, new RectOffset(12, 12, 14, 14));
            RectTransform artBox = UiKit.Rect(card.transform, "ArtBox");
            UiKit.Size(artBox, -1, 96f);
            Image art = UiKit.Box(artBox, "Art", opened ? new Color(1, 1, 1, 0.55f) : Color.white, 0);
            art.sprite = ArtLibrary.Get(ArtLibrary.Ui, Art[chest - 1] + (opened ? "_open" : ""));
            art.preserveAspect = true;
            UiKit.At(art.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 96f));
            if (ready) _float[chest - 1] = art.rectTransform;
            TextMeshProUGUI name = UiKit.Label(card.transform, Loc.T("chest_" + chest), FontRole.Display, 19f, p.Ink, TextAlignmentOptions.Center);
            UiKit.Size(name, -1, 24f);
            UiKit.Fit(name);
            TextMeshProUGUI contents = UiKit.Paragraph(card.transform, Loc.F("chest_contents", profile.Economy.ChestCoinsMin, profile.Economy.ChestCoinsMax),
                FontRole.BodyBold, 11.5f, p.InkMuted, TextAlignmentOptions.Top, 3f);
            UiKit.Size(contents, -1, 34f);
            UiKit.Spacer(card.transform);
            Button3D cta;
            if (ready) cta = Button3D.Styled(card.transform, "Open", ButtonStyle.Gold, Loc.T("chest_open"), 16f);
            else cta = Button3D.Off(card.transform, "Cta", opened ? Loc.T("chest_opened") : Loc.F("chest_clear", chest * 5), 15f);
            UiKit.Size(cta, -1, 44f);
            cta.OnClick(() =>
            {
                if (ready) Ui.ShowModal(new ChestOpenModal(chest, () => Ui.Refresh()));
                else Ui.Toast(opened ? Loc.T("map_chest_opened") : Loc.F("map_chest_locked", chest * 5));
            });
        }

        public override void Tick(float dt)
        {
            _t += dt;
            for (int i = 0; i < _float.Length; i++)
            {
                if (_float[i]) _float[i].anchoredPosition = new Vector2(0f, Mathf.Sin((_t + i * 0.3f) * Mathf.PI * 2f / 2.2f) * 3f + 3f);
            }
        }
    }
}
