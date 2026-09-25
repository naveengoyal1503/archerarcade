using System.Collections.Generic;
using ArcherArcade.Archers;
using ArcherArcade.Core;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Shop (design `shop`): coins only, earned by playing. Skins and arrow trails with a preview before buying;
    /// badge and streak rewards show how to earn them.
    /// </summary>
    public sealed class ShopScreen : UiScreen
    {
        public override string Title => Loc.T("title_shop");

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            RectTransform col = UiKit.Rect(root, "Col");
            UiKit.Stretch(col, 18f, 64f, 18f, 16f);
            UiKit.Column(col, 10f, TextAnchor.UpperLeft, true, false);
            Image note = UiKit.Box(col, "Note", Widgets.Green, 14f);
            UiKit.Size(note, -1, 36f);
            TextMeshProUGUI nt = UiKit.Label(note.transform, Fmt.Tint(Icons.MonetizationOn, Widgets.Gold) + " " + Loc.T("shop_note"), FontRole.Body, 13f, Color.white);
            UiKit.Stretch((RectTransform)nt.transform, 14, 0, 14, 0);
            UiKit.Fit(nt);

            var items = new List<CosmeticDef>();
            foreach (CosmeticDef c in CosmeticCatalog.All) if (c.Kind == CosmeticKind.Skin && c.Source != CosmeticSource.Default) items.Add(c);
            foreach (CosmeticDef c in CosmeticCatalog.All) if (c.Kind == CosmeticKind.Trail) items.Add(c);
            RectTransform grid = UiKit.Rect(col, "Grid");
            UiKit.Size(grid, -1, -1, 1f, 1f);
            UiKit.Column(grid, 10f, TextAnchor.UpperLeft, true, true);
            RectTransform line = null;
            for (int i = 0; i < items.Count; i++)
            {
                if (i % 4 == 0)
                {
                    line = UiKit.Rect(grid, "Line");
                    UiKit.Row(line, 10f, TextAnchor.MiddleLeft, true, true);
                }
                Item(line, items[i], p);
            }
        }

        void Item(RectTransform parent, CosmeticDef c, Palette p)
        {
            Profile profile = ServiceLocator.Profile;
            Image card = UiKit.Card(parent, c.Id, 18f);
            UiKit.Row(card, 10f, TextAnchor.MiddleLeft, false, false, new RectOffset(10, 10, 8, 8));
            Button3D icon = Button3D.Create(card.transform, "Icon", p.Solid, p.Shadow, 14f, 3f, 2f);
            UiKit.Size(icon, 56f, 56f);
            FillIcon(icon.Body, c);
            icon.OnClick(() => Ui.ShowModal(new ShopPreviewModal(c.Id, () => Ui.Refresh())));

            RectTransform words = UiKit.Rect(card.transform, "Words");
            UiKit.Size(words, -1, 70f, 1f);
            UiKit.Column(words, 2f, TextAnchor.MiddleLeft, true, false);
            TextMeshProUGUI n = UiKit.Label(words, Loc.T("cos_" + c.Id), FontRole.Display, 14f, p.Ink);
            UiKit.Size(n, -1, 17f);
            UiKit.Fit(n);
            string cat = c.Kind == CosmeticKind.Skin ? Loc.F("shop_skin", ArcherTable.Hero(c.ArcherId).Name) : Loc.T("shop_trail");
            TextMeshProUGUI k = UiKit.Label(words, cat, FontRole.Body, 10.5f, p.InkMuted);
            UiKit.Size(k, -1, 14f);
            UiKit.Fit(k);
            Button3D cta = Cta(words, c, profile, p);
            UiKit.Size(cta, -1, 28f);
        }

        public static void FillIcon(RectTransform body, CosmeticDef c)
        {
            if (c.Kind == CosmeticKind.Skin)
            {
                Image face = UiKit.Box(body, "Portrait", Color.white, 0);
                face.sprite = ArtLibrary.Portrait(c.Id);
                face.preserveAspect = true;
                UiKit.Stretch(face.rectTransform, 3, 3, 3, 3);
                return;
            }
            Color col = LoadoutScreen.TrailColor(c.Id);
            for (int i = 0; i < 4; i++)
            {
                Image dot = UiKit.Disc(body, "Dot", new Color(col.r, col.g, col.b, 1f - i * 0.22f));
                float s = 14f - i * 2.5f;
                UiKit.At(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(14f - i * 10f, -8f + i * 5f), new Vector2(s, s));
            }
            if (c.Id == "trail_rainbow")
            {
                Color[] rb = { UiKit.Hex(0xFF5FA2), UiKit.Hex(0xFFD23F), UiKit.Hex(0x2ED3A0), UiKit.Hex(0x38BDF8) };
                for (int i = 0; i < body.childCount && i < rb.Length; i++) body.GetChild(i).GetComponent<Image>().color = rb[i];
            }
            Image arrow = UiKit.Box(body, "Arrow", Color.white, 0);
            arrow.sprite = ArtLibrary.Get(ArtLibrary.Arrows, "arrow_normal");
            arrow.preserveAspect = true;
            UiKit.At(arrow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8f, 6f), new Vector2(40f, 14f));
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 25f);
        }

        Button3D Cta(Transform parent, CosmeticDef c, Profile profile, Palette p)
        {
            bool owned = c.Source == CosmeticSource.Default || profile.Owns(c.Id);
            bool equipped = c.Kind == CosmeticKind.Trail ? profile.Data.EquippedTrail == c.Id
                : profile.Data.EquippedSkins.TryGetValue(c.ArcherId, out string s) && s == c.Id;
            Button3D b;
            if (equipped) b = Button3D.Off(parent, "Equipped", Loc.T("shop_equipped"), 13f, 9f);
            else if (owned)
            {
                b = Button3D.Styled(parent, "Equip", ButtonStyle.Primary, Loc.T("shop_equip"), 13f, 9f, 3f, 2f);
                b.OnClick(() =>
                {
                    if (c.Kind == CosmeticKind.Skin && !profile.OwnsArcher(c.ArcherId))
                    {
                        Ui.Toast(Loc.F("shop_needs_archer", ArcherTable.Hero(c.ArcherId).Name));
                        return;
                    }
                    profile.Equip(c.Id);
                    ServiceLocator.CommitProfile();
                    Ui.Refresh();
                });
                return b;
            }
            else if (c.Source == CosmeticSource.Shop)
            {
                bool afford = profile.Coins >= c.Price;
                b = afford
                    ? Button3D.Styled(parent, "Buy", ButtonStyle.Gold, Fmt.Coins(c.Price), 13f, 9f, 3f, 2f)
                    : Button3D.Off(parent, "Buy", Fmt.Coins(c.Price), 13f, 9f);
                b.OnClick(() => Ui.ShowModal(new ShopPreviewModal(c.Id, () => Ui.Refresh())));
                return b;
            }
            else b = Button3D.Off(parent, "Earn", c.Source == CosmeticSource.Badge ? Loc.T("shop_badge") : Loc.T("shop_streak"), 12f, 9f);
            b.OnClick(() => Ui.ShowModal(new ShopPreviewModal(c.Id, () => Ui.Refresh())));
            return b;
        }
    }
}
