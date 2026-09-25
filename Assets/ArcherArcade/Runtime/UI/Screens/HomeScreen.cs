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
    /// Home (design `home`, state 02): profile pill with pinned badges, star + coin pills, five icon buttons,
    /// the equipped archer idling on a stage (live 2.5D), the big orange PLAY card (continues the campaign), four
    /// mode cards and "All modes →". Back asks before leaving the game.
    /// </summary>
    public sealed class HomeScreen : UiScreen
    {
        RectTransform _playArrow;
        ArcherStage _stage;
        float _t;

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Profile profile = ServiceLocator.Profile;

            // Hill behind the stage (design: left -80, right -80, bottom -70, height 170, round top).
            Image hill = UiKit.Disc(root, "Hill", p.Hill);
            hill.preserveAspect = false;
            RectTransform hr = hill.rectTransform;
            hr.anchorMin = new Vector2(0f, 0f);
            hr.anchorMax = new Vector2(1f, 0f);
            hr.pivot = new Vector2(0.5f, 0f);
            hr.offsetMin = new Vector2(-80f, -240f);
            hr.offsetMax = new Vector2(80f, 100f);

            BuildTopBar(root, profile, p);
            BuildStage(root, profile, p);
            BuildRight(root, profile);
        }

        void BuildTopBar(RectTransform root, Profile profile, Palette p)
        {
            RectTransform bar = UiKit.Rect(root, "TopBar");
            UiKit.TopBand(bar, 14f, 48f, 18f, 18f);
            UiKit.Row(bar, 10f, TextAnchor.MiddleLeft);

            // Profile pill → Stats.
            Button3D pill = Button3D.Create(bar, "Profile", p.Card, Color.clear, 24f, 0f, 2f);
            UiKit.Border(pill.Face.transform, p.Line, 24f, 1.5f);
            UiKit.Row(pill.Body, 10f, TextAnchor.MiddleLeft, false, false, new RectOffset(5, 14, 0, 0));
            pill.Face.GetComponent<RectTransform>().SetAsFirstSibling();
            var le = pill.Face.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
            string archer = profile.Data.EquippedArcher;
            RectTransform avatar = UiKit.Rect(pill.Body, "Avatar");
            UiKit.Size(avatar, 38f, 38f);
            Image disc = UiKit.Disc(avatar, "Disc", UiKit.Hex(ArcherLooks.Color(archer)));
            UiKit.Stretch(disc.rectTransform);
            Image face = UiKit.Box(avatar, "Portrait", Color.white, 0);
            face.sprite = ArtLibrary.Portrait(ArcherLooks.ForHero(archer, Skin(profile, archer)));
            face.preserveAspect = true;
            UiKit.Stretch(face.rectTransform, 1, 1, 1, 1);
            RectTransform names = UiKit.Rect(pill.Body, "Names");
            UiKit.Column(names, 0f, TextAnchor.MiddleLeft, true, false);
            TextMeshProUGUI n = UiKit.Label(names, string.IsNullOrEmpty(profile.Data.PlayerName) ? Loc.T("player_default") : profile.Data.PlayerName,
                FontRole.Display, 15f, p.Ink);
            UiKit.Size(n, -1, 17f);
            TextMeshProUGUI lv = UiKit.Label(names, Loc.F("home_level", profile.ContinueLevel), FontRole.Body, 11f, p.InkMuted);
            UiKit.Size(lv, -1, 14f);
            UiKit.Size(names, Mathf.Max(n.preferredWidth, lv.preferredWidth) + 4f, 36f);
            RectTransform pins = UiKit.Rect(pill.Body, "Pins");
            UiKit.Row(pins, 3f, TextAnchor.MiddleLeft);
            int pinned = 0;
            foreach (string id in profile.Data.PinnedBadges)
            {
                BadgeDef b = BadgeCatalog.ById(id);
                if (b == null) continue;
                Widgets.Medal(pins, Icons.ByName(b.Icon), profile.BadgeTierOf(id), 24f);
                pinned++;
            }
            UiKit.Size(pins, pinned * 27f, 24f);
            var fit = pill.gameObject.AddComponent<LayoutElement>();
            fit.preferredWidth = 5f + 38f + 10f + Mathf.Max(n.preferredWidth, lv.preferredWidth) + 4f + (pinned > 0 ? 10f + pinned * 27f : 0f) + 14f;
            fit.preferredHeight = 48f;
            pill.OnClick(() => Ui.Push(new StatsScreen()));

            UiKit.Spacer(bar);
            Widgets.Pill(bar, null, "★", Widgets.StarGold, Loc.N(profile.TotalStars), out RectTransform _);
            RectTransform coins = Widgets.CoinPill(bar);
            var coinButton = coins.gameObject.AddComponent<Button>();
            coinButton.transition = Selectable.Transition.None;
            coins.GetComponent<Image>().raycastTarget = true;
            coinButton.onClick.AddListener(() => Ui.ShowModal(new CoinsModal(0)));

            bool chest = false;
            for (int i = 1; i <= Chests.Count; i++) chest |= profile.ChestAvailable(i);
            Widgets.IconButton(bar, Icons.Redeem, () => Ui.Push(new ChestsScreen()), chest);
            Widgets.IconButton(bar, Icons.Storefront, () => Ui.Push(new ShopScreen()));
            Widgets.IconButton(bar, Icons.MilitaryTech, () => Ui.Push(new BadgesScreen()));
            Widgets.IconButton(bar, Icons.BarChart, () => Ui.Push(new StatsScreen()));
            Widgets.IconButton(bar, Icons.Settings, () => Ui.Push(new SettingsScreen()));
        }

        static string Skin(Profile profile, string archer) =>
            profile.Data.EquippedSkins.TryGetValue(archer, out string s) ? s : CosmeticCatalog.DefaultSkinFor(archer);

        void BuildStage(RectTransform root, Profile profile, Palette p)
        {
            RectTransform col = UiKit.Rect(root, "Stage");
            col.anchorMin = new Vector2(0f, 0f);
            col.anchorMax = new Vector2(0f, 1f);
            col.pivot = new Vector2(0f, 0.5f);
            col.offsetMin = new Vector2(30f, 14f);
            col.offsetMax = new Vector2(290f, -80f);

            string archerId = profile.Data.EquippedArcher;
            ArcherDef def = ArcherTable.Hero(archerId);
            Image chip = UiKit.Card(col, "NameChip", 14f);
            UiKit.At(chip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(240f, 34f));
            UiKit.Row(chip, 8f, TextAnchor.MiddleCenter, false, false, new RectOffset(12, 12, 0, 0));
            TextMeshProUGUI name = UiKit.Label(chip.transform, def.Name, FontRole.Display, 17f, p.Ink);
            UiKit.Size(name, name.preferredWidth + 2f, 30f);
            TextMeshProUGUI sub = UiKit.Label(chip.transform, Loc.F("home_lv_ability", profile.ArcherLevel(archerId), Loc.T("ability_" + def.Ability)),
                FontRole.Body, 11f, p.InkMuted);
            UiKit.Size(sub, sub.preferredWidth + 2f, 30f);
            chip.rectTransform.sizeDelta = new Vector2(Mathf.Min(260f, name.preferredWidth + sub.preferredWidth + 36f), 34f);

            // Shadow ellipse 170 × 26 under the archer, then the live archer.
            Image shadow = UiKit.Disc(col, "GroundShadow", new Color(0, 0, 0, 0.16f));
            shadow.preserveAspect = false;
            UiKit.At(shadow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 62f), new Vector2(170f, 26f));
            RectTransform stageBox = UiKit.Rect(col, "Archer");
            UiKit.At(stageBox, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(240f, 200f));
            _stage = ArcherStage.Create(stageBox, ArcherLooks.ForHero(archerId, Skin(profile, archerId)), true);

            Button3D archers = Button3D.Styled(col, "Archers", ButtonStyle.Primary, Loc.T("home_archers"), 16f);
            UiKit.At((RectTransform)archers.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(140f, 40f));
            archers.OnClick(() => Ui.Push(new ArchersScreen(archerId)));
        }

        void BuildRight(RectTransform root, Profile profile)
        {
            RectTransform right = UiKit.Rect(root, "Right");
            UiKit.Stretch(right, 316f, 80f, 18f, 18f);

            // PLAY hero card: 108 high, radius 28, #F0641E with a 7 dp edge and a warm glow.
            RectTransform playHolder = UiKit.Rect(right, "PlayHolder");
            UiKit.TopBand(playHolder, 0f, 108f);
            UiKit.SoftShadow(playHolder, new Color(0.72f, 0.27f, 0.06f, 0.3f), 28f, 24f, 14f);
            Button3D play = Button3D.Create(playHolder, "Play", UiKit.Hex(0xF0641E), UiKit.Hex(0xB8460F), 28f, 7f, 5f);
            UiKit.Stretch((RectTransform)play.transform);
            play.Sound = Feel.SoundId.UiConfirm;
            RectTransform row = UiKit.Rect(play.Body, "Row");
            UiKit.Stretch(row, 30f, 0f, 16f, 0f);
            UiKit.Row(row, 18f, TextAnchor.MiddleLeft);
            TextMeshProUGUI big = UiKit.ShadowLabel(row, Loc.T("home_play"), FontRole.Display, 54f, Color.white, new Color(0, 0, 0, 0.18f), 4f,
                TextAlignmentOptions.MidlineLeft);
            UiKit.Size(big.transform.parent as RectTransform, big.preferredWidth + 4f, 70f);
            RectTransform words = UiKit.Rect(row, "Words");
            UiKit.Column(words, 0f, TextAnchor.MiddleLeft, true, false);
            UiKit.Size(words, -1, 60f, 1f);
            TextMeshProUGUI camp = UiKit.Label(words, Loc.T("home_campaign"), FontRole.Body, 14f, Color.white);
            UiKit.Size(camp, -1, 20f);
            bool allCleared = profile.HighestLevelCleared >= Logic.Campaign.WorldOne.LevelCount;
            TextMeshProUGUI lvl = UiKit.Label(words, allCleared ? Loc.T("home_world_done") : Loc.F("home_world_level", profile.ContinueLevel),
                FontRole.Display, 19f, Color.white);
            UiKit.Size(lvl, -1, 26f);
            UiKit.Fit(lvl);
            RectTransform circleHolder = UiKit.Rect(row, "Go");
            UiKit.Size(circleHolder, 68f, 68f);
            Image circle = UiKit.Disc(circleHolder, "Circle", Color.white);
            UiKit.Stretch(circle.rectTransform);
            _playArrow = circle.rectTransform;
            TextMeshProUGUI arrow = UiKit.Glyph(circle.transform, Icons.PlayArrow, 38f, UiKit.Hex(0xF0641E));
            UiKit.Stretch((RectTransform)arrow.transform, 4f, 0f, 0f, 0f);
            play.OnClick(() => Ui.Push(LoadoutScreen.Campaign(profile.ContinueLevel)));

            // Four mode cards (design: Quick Duel, 2 Players, Survival, Daily).
            RectTransform grid = UiKit.Rect(right, "Modes");
            grid.anchorMin = Vector2.zero;
            grid.anchorMax = Vector2.one;
            grid.offsetMin = new Vector2(0f, 26f);
            grid.offsetMax = new Vector2(0f, -122f);
            UiKit.Row(grid, 12f, TextAnchor.UpperLeft, true, true);
            ModeCard[] cards = { ModeCatalog.QuickDuel(Ui), ModeCatalog.TwoPlayer(Ui), ModeCatalog.Survival(Ui), ModeCatalog.Daily(Ui) };
            foreach (ModeCard m in cards) BuildModeCard(grid, m);

            TextMeshProUGUI all = UiKit.Label(right, Loc.T("home_all_modes"), FontRole.Body, 13f, Color.white, TextAlignmentOptions.MidlineRight);
            RectTransform allRt = (RectTransform)all.transform;
            UiKit.At(allRt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-4f, 0f), new Vector2(160f, 20f));
            all.raycastTarget = true;
            var link = all.gameObject.AddComponent<Button>();
            link.transition = Selectable.Transition.None;
            link.onClick.AddListener(() =>
            {
                ServiceLocator.Audio?.Play(Feel.SoundId.UiTap);
                Ui.Push(new ModesScreen());
            });
        }

        static void BuildModeCard(RectTransform parent, ModeCard m)
        {
            Button3D b = Button3D.Create(parent, "Mode", UiKit.Hex(m.Color), UiKit.Hex(m.Shade), 22f, 5f, 4f);
            RectTransform body = b.Body;
            Image tile = UiKit.Box(body, "IconTile", new Color(1, 1, 1, 0.95f), 14f);
            UiKit.TopLeft(tile.rectTransform, 12f, 12f, 42f, 42f);
            TextMeshProUGUI ic = UiKit.Glyph(tile.transform, m.Icon, 24f, UiKit.Hex(m.Color));
            UiKit.Stretch((RectTransform)ic.transform);
            TextMeshProUGUI name = UiKit.Label(body, m.Name, FontRole.Display, 18f, Color.white, TextAlignmentOptions.BottomLeft);
            RectTransform nr = (RectTransform)name.transform;
            nr.anchorMin = new Vector2(0f, 0f);
            nr.anchorMax = new Vector2(1f, 0f);
            nr.pivot = new Vector2(0f, 0f);
            nr.offsetMin = new Vector2(14f, 44f);
            nr.offsetMax = new Vector2(-8f, 66f);
            UiKit.Fit(name);
            TextMeshProUGUI sub = UiKit.Paragraph(body, m.Sub, FontRole.Body, 11f, new Color(1, 1, 1, 0.95f), TextAlignmentOptions.TopLeft);
            RectTransform sr = (RectTransform)sub.transform;
            sr.anchorMin = new Vector2(0f, 0f);
            sr.anchorMax = new Vector2(1f, 0f);
            sr.pivot = new Vector2(0f, 0f);
            sr.offsetMin = new Vector2(14f, 12f);
            sr.offsetMax = new Vector2(-8f, 44f);
            b.OnClick(m.Open);
        }

        public override void OnShow()
        {
            ServiceLocator.Audio?.PlayMusic(Feel.SoundId.MusicHome);
        }

        public override void Tick(float dt)
        {
            _t += dt;
            if (_playArrow && (ServiceLocator.Settings == null || !ServiceLocator.Settings.ReduceMotion))
            {
                float s = 1f + (Mathf.Sin(_t * Mathf.PI * 2f / 1.8f) * 0.5f + 0.5f) * 0.07f;
                _playArrow.localScale = new Vector3(s, s, 1f);
            }
        }

        public override bool OnBack()
        {
            Ui.ShowModal(new ConfirmModal(Icons.ExitToApp, Loc.T("exit_title"), Loc.T("exit_body"), Loc.T("exit_stay"), Loc.T("exit_quit"),
                ButtonStyle.Danger, Application.Quit));
            return true;
        }
    }
}
