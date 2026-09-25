using System.Collections.Generic;
using ArcherArcade.Core;
using ArcherArcade.Feel;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Match;
using ArcherArcade.Theme;
using ArcherArcade.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Match result (design `result`: victory state 07, defeat state 08; SCREEN_INVENTORY 16, 17, 23): purple glow
    /// and confetti on a win, calm dusk on a loss, the title, stars popping one by one, stat tiles, the coin pill
    /// with a coin fly, and the right buttons for the mode (Next level / Replay / Map, Retry with Assist, Next round /
    /// Rematch / Swap sides, chest ready). New tips, unlocked archers and badges follow as cards and toasts.
    /// </summary>
    public sealed class ResultScreen : UiScreen
    {
        readonly MatchSceneRoot _r;
        readonly MatchOutcome _o;
        readonly List<RectTransform> _stars = new List<RectTransform>();
        RectTransform _coins;
        float _t;
        bool _extrasShown;
        int _badgeIndex;
        float _nextBadge = 2.2f;

        public ResultScreen(MatchSceneRoot root, MatchOutcome outcome)
        {
            _r = root;
            _o = outcome;
        }

        bool Bright => _o.Won || _o.Mode == GameMode.TwoPlayer;

        public override bool ShowCoins => false;

        public override Texture Background => Bright
            ? ShapeSprites.Radial(UiKit.Hex(0x8E6BFF), UiKit.Hex(0x5536D6), 0.5f, 0.35f, 0.75f)
            : ShapeSprites.Radial(UiKit.Hex(0x6E6A9E), UiKit.Hex(0x3E3A6B), 0.5f, 0.35f, 0.75f);

        public override void Build(RectTransform root)
        {
            _stars.Clear();
            if (Bright) Confetti.Create(root);

            // Coin pill top right (the coin fly lands here).
            if (_o.Mode != GameMode.TwoPlayer)
            {
                RectTransform pill = Widgets.CoinPill(root);
                UiKit.At(pill, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -14f), new Vector2(120f, 36f));
            }

            RectTransform col = UiKit.Rect(root, "Col");
            UiKit.At(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(760f, 370f));
            UiKit.Column(col, 8f, TextAnchor.MiddleCenter, false, false);

            TextMeshProUGUI title = UiKit.ShadowLabel(col, _o.Title, FontRole.Display, 54f, Color.white, new Color(0f, 0f, 0f, 0.18f), 5f);
            UiKit.Size(title.transform.parent, 700f, 62f);
            UiKit.Fit(title, 0.5f);
            if (!string.IsNullOrEmpty(_o.Subtitle))
            {
                TextMeshProUGUI sub = UiKit.Label(col, _o.Subtitle, FontRole.BodyBold, 14f, new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.Center);
                UiKit.Size(sub, 640f, 20f);
                UiKit.Fit(sub, 0.7f);
            }
            if (_o.ShowStars) BuildStars(col);
            if (_o.Stats.Count > 0) BuildStats(col);
            if (_o.Mode != GameMode.TwoPlayer) BuildCoins(col);
            BuildButtons(col);
            if (!string.IsNullOrEmpty(_o.Tip))
            {
                TextMeshProUGUI tip = UiKit.Paragraph(col, Fmt.Tint(Icons.Psychology, Widgets.Gold) + " " + _o.Tip, FontRole.BodyBold, 12f, Color.white, TextAlignmentOptions.Top);
                UiKit.Size(tip, 380f, 32f);
            }
        }

        void BuildStars(RectTransform col)
        {
            RectTransform row = UiKit.Rect(col, "Stars");
            UiKit.Size(row, 220f, 58f);
            UiKit.Row(row, 10f, TextAnchor.LowerCenter, false, false);
            for (int i = 0; i < 3; i++)
            {
                bool on = i < _o.Stars;
                float size = i == 1 ? 56f : 44f;
                RectTransform box = UiKit.Rect(row, "Star" + i);
                UiKit.Size(box, size, size);
                TextMeshProUGUI back = UiKit.Glyph(box, Icons.Star, size, new Color(0f, 0f, 0f, 0.18f));
                UiKit.Stretch((RectTransform)back.transform, 0f, 4f, 0f, -4f);
                TextMeshProUGUI star = UiKit.Glyph(box, Icons.Star, size, on ? Widgets.Gold : new Color(1f, 1f, 1f, 0.28f));
                UiKit.Stretch((RectTransform)star.transform);
                if (i >= _o.PreviousStars && on)
                {
                    TextMeshProUGUI nw = UiKit.Label(box, "+", FontRole.Display, 14f, Color.white, TextAlignmentOptions.Center);
                    UiKit.At((RectTransform)nw.transform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 16f));
                }
                RectTransform inner = (RectTransform)star.transform;
                inner.localScale = Vector3.zero;
                ((RectTransform)back.transform).localScale = Vector3.zero;
                _stars.Add(inner);
                int index = i;
                float delay = 0.3f + i * 0.25f;
                Tween.Value(0f, 1f, 0.45f, k =>
                {
                    if (!inner) return;
                    inner.localScale = Vector3.one * k;
                    ((RectTransform)back.transform).localScale = Vector3.one * k;
                }, EaseType.OutBack, delay, true, () =>
                {
                    if (index < _o.Stars) ServiceLocator.Audio?.Play(index == 0 ? SoundId.Star1 : index == 1 ? SoundId.Star2 : SoundId.Star3);
                }, inner);
            }
        }

        void BuildStats(RectTransform col)
        {
            RectTransform row = UiKit.Rect(col, "Stats");
            UiKit.Size(row, 600f, 56f);
            UiKit.Row(row, 10f, TextAnchor.MiddleCenter, false, false);
            foreach (string[] s in _o.Stats)
            {
                Image tile = UiKit.Box(row, "Tile", new Color(1f, 1f, 1f, 0.18f), 14f);
                UiKit.Size(tile, 110f, 52f);
                TextMeshProUGUI v = UiKit.Label(tile.transform, s[0], FontRole.Display, 22f, Color.white, TextAlignmentOptions.Center);
                UiKit.Stretch((RectTransform)v.transform, 4f, 4f, 4f, 20f);
                TextMeshProUGUI k = UiKit.Label(tile.transform, s[1], FontRole.BodyBold, 11f, new Color(1f, 1f, 1f, 0.95f), TextAlignmentOptions.Center);
                UiKit.Stretch((RectTransform)k.transform, 4f, 30f, 4f, 4f);
                UiKit.Fit(k, 0.7f);
            }
        }

        void BuildCoins(RectTransform col)
        {
            RectTransform holder = UiKit.Rect(col, "CoinsHolder");
            UiKit.Size(holder, 300f, 40f);
            Image pill = UiKit.Box(holder, "Coins", Color.white, 16f);
            _coins = pill.rectTransform;
            UiKit.At(_coins, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 38f));
            UiKit.Row(pill, 8f, TextAnchor.MiddleCenter, false, false, new RectOffset(16, 16, 0, 0));
            Image coin = UiKit.Box(pill.transform, "Coin", Color.white, 0f);
            coin.sprite = ArtLibrary.Get(ArtLibrary.Fx, "coin");
            coin.preserveAspect = true;
            UiKit.Size(coin, 24f, 24f);
            string text = "+" + Loc.N(_o.Coins);
            TextMeshProUGUI t = UiKit.Label(pill.transform, text, FontRole.Display, 22f, Widgets.Navy, TextAlignmentOptions.Center);
            UiKit.Size(t, t.preferredWidth + 4f, 30f);
            _coins.sizeDelta = new Vector2(t.preferredWidth + 70f, 38f);
            if (_o.FirstClear && _o.Breakdown.Clear > 0)
            {
                TextMeshProUGUI fc = UiKit.Label(holder, Loc.F("res_first_clear", _o.Breakdown.Clear), FontRole.BodyBold, 11f, Widgets.Gold, TextAlignmentOptions.Left);
                UiKit.At((RectTransform)fc.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(_coins.sizeDelta.x * 0.5f + 8f, 0f), new Vector2(140f, 20f));
            }
            _coins.localScale = Vector3.zero;
            RectTransform c = _coins;
            Tween.Value(0f, 1f, 0.4f, k => { if (c) c.localScale = Vector3.one * k; }, EaseType.OutBack, 1f, true, () =>
            {
                if (_o.Coins > 0 && Root)
                {
                    ServiceLocator.Audio?.Play(SoundId.Coin);
                    CoinFly.Burst(Root, CenterIn(Root, c), Mathf.Clamp(_o.Coins / 10, 3, 10), 0f);
                }
            }, c);
        }

        static Vector2 CenterIn(RectTransform root, RectTransform child)
        {
            Vector3 world = child.TransformPoint(child.rect.center);
            Vector3 local = root.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        void BuildButtons(RectTransform col)
        {
            RectTransform row = UiKit.Rect(col, "Buttons");
            UiKit.Size(row, 700f, 56f);
            UiKit.Row(row, 10f, TextAnchor.MiddleCenter, false, false, new RectOffset(0, 0, 4, 6));
            switch (_o.Mode)
            {
                case GameMode.TwoPlayer:
                    if (_o.PvpRoundOnly)
                    {
                        White(row, Loc.T("home"), () => _r.GoHome(HomeTarget.Home));
                        Gold(row, Loc.T("pvp_next_round"), () => _r.NextPvpRound());
                    }
                    else
                    {
                        White(row, Loc.T("home"), () => _r.GoHome(HomeTarget.Home));
                        Blue(row, Loc.T("pvp_swap"), () => _r.PvpRematch(true));
                        Gold(row, Loc.T("rematch"), () => _r.PvpRematch(false));
                    }
                    break;
                case GameMode.Campaign:
                    White(row, _o.Won ? Loc.T("home") : Loc.T("map"), () => _r.GoHome(_o.Won ? HomeTarget.Home : HomeTarget.Map));
                    if (_o.Won)
                    {
                        Blue(row, Loc.T("replay"), () => _r.Retry(false));
                        if (_o.ChestReady > 0) Pink(row, Icons.Redeem + " " + Loc.T("res_open_chest"), () => _r.GoHome(HomeTarget.Chests));
                        if (_o.HasNextLevel) Gold(row, Loc.T("next_level"), () => _r.NextLevel(_o.NextLevel));
                        else White(row, Loc.T("map"), () => _r.GoHome(HomeTarget.Map));
                    }
                    else
                    {
                        Blue(row, Loc.T("retry"), () => _r.Retry(false));
                        if (_o.OfferAssist) Gold(row, Loc.T("res_assist_retry"), () => _r.Retry(true));
                    }
                    break;
                case GameMode.Daily:
                    White(row, Loc.T("home"), () => _r.GoHome(HomeTarget.Home));
                    if (_o.Won) Gold(row, Loc.T("daily_kicker"), () => _r.GoHome(HomeTarget.Daily));
                    else Blue(row, Loc.T("retry"), () => _r.Retry(false));
                    break;
                case GameMode.QuickDuel:
                    White(row, Loc.T("home"), () => _r.GoHome(HomeTarget.Home));
                    Gold(row, Loc.T("rematch"), () => _r.Retry(false));
                    break;
                default:
                    White(row, Loc.T("home"), () => _r.GoHome(HomeTarget.Home));
                    Gold(row, Loc.T("retry"), () => _r.Retry(false));
                    break;
            }
        }

        static Button3D White(RectTransform row, string label, System.Action click)
        {
            Button3D b = Button3D.Create(row, label, new Color(1f, 1f, 1f, 0.95f), new Color(0f, 0f, 0f, 0.18f), 15f, 4f, 3f);
            b.AddLabel(label, FontRole.Display, 17f, Widgets.Navy);
            Fit(b, label);
            b.OnClick(click);
            b.PopIn(1.2f);
            return b;
        }

        static Button3D Blue(RectTransform row, string label, System.Action click) => Styled(row, ButtonStyle.Info, label, click);
        static Button3D Gold(RectTransform row, string label, System.Action click) => Styled(row, ButtonStyle.Gold, label, click);
        static Button3D Pink(RectTransform row, string label, System.Action click) => Styled(row, ButtonStyle.Pvp, label, click);

        static Button3D Styled(RectTransform row, ButtonStyle style, string label, System.Action click)
        {
            Button3D b = Button3D.Styled(row, label, style, label, 17f, 15f);
            Fit(b, label);
            b.OnClick(click);
            b.PopIn(1.3f);
            return b;
        }

        static void Fit(Button3D b, string label)
        {
            float w = Mathf.Clamp(b.Text.preferredWidth + 40f, 110f, 230f);
            UiKit.Size(b, w, 48f);
        }

        public override bool OnBack()
        {
            _r.GoHome(_o.Mode == GameMode.Campaign ? HomeTarget.Map : _r.ResultHomeTarget);
            return true;
        }

        public override void Tick(float dt)
        {
            _t += dt;
            if (!_extrasShown && _t > 1.8f)
            {
                _extrasShown = true;
                ShowExtras();
            }
            if (_t > _nextBadge && _badgeIndex < _o.Badges.Count && !Ui.HasModal)
            {
                BadgeUpdate b = _o.Badges[_badgeIndex++];
                string tier = Loc.T(b.Tier == BadgeTier.Gold ? "tier_gold" : b.Tier == BadgeTier.Silver ? "tier_silver" : "tier_bronze");
                Ui.Toast(Icons.MilitaryTech + " " + Loc.F("new_badge", b.Badge.Name, tier));
                ServiceLocator.Audio?.Play(SoundId.Badge);
                _nextBadge = _t + 2.1f;
            }
            // Gentle float on the stars.
            for (int i = 0; i < _stars.Count; i++)
                if (_stars[i]) _stars[i].anchoredPosition = new Vector2(_stars[i].anchoredPosition.x, Mathf.Sin(_t * 2f + i) * 2f);
        }

        void ShowExtras()
        {
            if (_o.NewTip.HasValue) Ui.ShowModal(new IdeaCardModal(GameVisuals.TipCard(_o.NewTip.Value), true));
            foreach (string id in _o.NewArchers)
            {
                string archer = id;
                Ui.ShowModal(new UnlockModal(archer, () =>
                {
                    ServiceLocator.Profile.EquipArcher(archer);
                    ServiceLocator.CommitProfile();
                    _r.GoHome(HomeTarget.Archers);
                }));
            }
            if (!string.IsNullOrEmpty(_o.CosmeticId)) Ui.Toast(Icons.AutoAwesome + " " + Loc.T("cos_" + _o.CosmeticId));
            if (_o.NewBest) Ui.Toast(Icons.EmojiEvents + " " + Loc.T("res_new_best"));
        }
    }
}
