using System;
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
    /// <summary>Preview before buying (GAME_DESIGN §8): skins on the live archer, trails on a flying arrow.</summary>
    public sealed class ShopPreviewModal : UiModal
    {
        readonly string _id;
        readonly Action _changed;
        RectTransform _arrow;
        readonly RectTransform[] _trail = new RectTransform[10];
        float _t;

        public ShopPreviewModal(string id, Action changed)
        {
            _id = id;
            _changed = changed;
        }

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Profile profile = ServiceLocator.Profile;
            CosmeticDef c = CosmeticCatalog.ById(_id);
            Image panel = UiKit.Box(root, "Panel", p.Solid, 26f);
            panel.raycastTarget = true;
            UiKit.At(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 260f));
            UiKit.SoftShadow(panel.transform, new Color(0, 0, 0, 0.25f), 26f, 30f, 10f);

            Image stage = UiKit.Box(panel.transform, "Stage", p.Track, 20f);
            UiKit.TopLeft(stage.rectTransform, 16f, 16f, 210f, 228f);
            stage.gameObject.AddComponent<RectMask2D>();
            if (c.Kind == CosmeticKind.Skin)
            {
                RectTransform box = UiKit.Rect(stage.transform, "Archer");
                UiKit.Stretch(box, 0, 10, 0, 0);
                ArcherStage s = ArcherStage.Create(box, c.Id, true);
                s.Demo();
            }
            else BuildTrail(stage.rectTransform, c.Id);

            RectTransform words = UiKit.Rect(panel.transform, "Words");
            UiKit.Stretch(words, 244f, 20f, 20f, 18f);
            UiKit.Column(words, 6f, TextAnchor.UpperLeft, true, false);
            TextMeshProUGUI n = UiKit.Label(words, Loc.T("cos_" + c.Id), FontRole.Display, 26f, p.Ink);
            UiKit.Size(n, -1, 32f);
            UiKit.Fit(n);
            string cat = c.Kind == CosmeticKind.Skin ? Loc.F("shop_skin", ArcherTable.Hero(c.ArcherId).Name) : Loc.T("shop_trail");
            TextMeshProUGUI k = UiKit.Label(words, cat, FontRole.Body, 13f, p.InkMuted);
            UiKit.Size(k, -1, 18f);
            string how = null;
            if (c.Source == CosmeticSource.Badge)
            {
                BadgeDef b = BadgeCatalog.ById(c.BadgeId);
                how = Loc.F("archers_skin_badge", Loc.T("cos_" + c.Id), b != null ? b.Name : c.BadgeId, TierName(c.BadgeTier));
            }
            else if (c.Source == CosmeticSource.DailyStreak) how = Loc.T("daily_note");
            if (how != null)
            {
                TextMeshProUGUI h = UiKit.Paragraph(words, how, FontRole.BodyBold, 12.5f, p.InkMuted, TextAlignmentOptions.TopLeft, 3f);
                UiKit.Size(h, -1, 64f);
            }
            UiKit.Spacer(words);

            bool owned = c.Source == CosmeticSource.Default || profile.Owns(c.Id);
            RectTransform buttons = UiKit.Rect(words, "Buttons");
            UiKit.Size(buttons, -1, 50f);
            UiKit.Row(buttons, 10f, TextAnchor.MiddleLeft, true, true, new RectOffset(0, 0, 3, 5));
            Button3D close = Button3D.Off(buttons, "Close", Loc.T("close"), 16f);
            close.OnClick(Close);
            if (!owned && c.Source == CosmeticSource.Shop)
            {
                Button3D buy = Button3D.Styled(buttons, "Buy", ButtonStyle.Gold, Loc.F("shop_buy", Fmt.Coins(c.Price)), 16f);
                buy.Sound = Feel.SoundId.UiConfirm;
                buy.OnClick(() =>
                {
                    BuyResult r = profile.Buy(c.Id);
                    if (r == BuyResult.NotEnoughCoins)
                    {
                        Close();
                        Ui.ShowModal(new CoinsModal(profile.Missing(c.Price)));
                        return;
                    }
                    if (r != BuyResult.Done) return;
                    if (c.Kind == CosmeticKind.Trail || profile.OwnsArcher(c.ArcherId)) profile.Equip(c.Id);
                    ServiceLocator.CommitProfile();
                    ServiceLocator.Audio?.Play(Feel.SoundId.Coin);
                    Ui.Toast(Loc.F("shop_bought", Loc.T("cos_" + c.Id)));
                    Close();
                    _changed?.Invoke();
                });
            }
            UIManager.PopPanel(panel.transform);
        }

        static string TierName(BadgeTier t) => t == BadgeTier.Gold ? Loc.T("tier_gold") : t == BadgeTier.Silver ? Loc.T("tier_silver") : Loc.T("tier_bronze");

        void BuildTrail(RectTransform stage, string id)
        {
            Color c = LoadoutScreen.TrailColor(id);
            Color[] rainbow = { UiKit.Hex(0xFF5FA2), UiKit.Hex(0xFF8A3D), UiKit.Hex(0xFFD23F), UiKit.Hex(0x2ED3A0), UiKit.Hex(0x38BDF8), UiKit.Hex(0x9D86FF) };
            for (int i = 0; i < _trail.Length; i++)
            {
                Color col = id == "trail_rainbow" ? rainbow[i % rainbow.Length] : c;
                Image d = UiKit.Box(stage, "Trail", col, 0);
                d.sprite = id == "trail_sparkle" ? ArtLibrary.Get(ArtLibrary.Fx, "spark") : ShapeSprites.Circle;
                d.preserveAspect = true;
                _trail[i] = d.rectTransform;
                UiKit.At(_trail[i], new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f));
            }
            Image a = UiKit.Box(stage, "Arrow", Color.white, 0);
            a.sprite = ArtLibrary.Get(ArtLibrary.Arrows, "arrow_normal");
            a.preserveAspect = true;
            _arrow = a.rectTransform;
            UiKit.At(_arrow, new Vector2(0f, 0f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(70f, 22f));
        }

        public override void Tick(float dt)
        {
            if (!_arrow) return;
            _t += dt;
            float cycle = 1.6f;
            for (int i = -1; i < _trail.Length; i++)
            {
                float k = Mathf.Repeat(_t - (i + 1) * 0.045f, cycle) / cycle;
                float x = 10f + k * 190f, y = 30f + Mathf.Sin(k * Mathf.PI) * 150f;
                if (i < 0)
                {
                    _arrow.anchoredPosition = new Vector2(x, y);
                    float dy = Mathf.Cos(k * Mathf.PI) * 150f * Mathf.PI, dx = 190f;
                    _arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                }
                else
                {
                    _trail[i].anchoredPosition = new Vector2(x, y);
                    _trail[i].localScale = Vector3.one * (1f - i * 0.08f);
                }
            }
        }
    }
}
