using System;
using System.Collections.Generic;
using ArcherArcade.Archers;
using ArcherArcade.Core;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Logic.Modes;
using ArcherArcade.Logic.Save;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Level intro + loadout (design `loadout`; SCREEN_INVENTORY #6, #7, #10, #20, #25): the coloured card on the
    /// left (kicker, title, goal, "New!" idea, difficulty / arena / distance / wind pickers, par and opponent,
    /// assist offer, START ▶) and on the right the arrow tips (pick 3), boosters, archer, skin and trail.
    /// </summary>
    public sealed class LoadoutScreen : UiScreen
    {
        readonly GameMode _mode;
        readonly int _level;
        readonly int _day;
        readonly List<ArrowTip> _tips = new List<ArrowTip>();
        readonly List<BoosterKind> _boosters = new List<BoosterKind>();
        string _difficulty = "medium";
        int _arena;
        int _distance = 20;
        int _wind;
        bool _assist;
        bool _ideasShown;

        static readonly string[] Arenas = { "random", "meadow", "fence", "crates", "platforms", "barrels" };

        LoadoutScreen(GameMode mode, int level = 0, int day = 0)
        {
            _mode = mode;
            _level = level;
            _day = day;
            Profile p = ServiceLocator.Profile;
            foreach (ArrowTip t in p.CurrentLoadout().Tips) _tips.Add(t);
        }

        public static LoadoutScreen Campaign(int level) => new LoadoutScreen(GameMode.Campaign, level);
        public static LoadoutScreen QuickDuel() => new LoadoutScreen(GameMode.QuickDuel);
        public static LoadoutScreen Daily(int day) => new LoadoutScreen(GameMode.Daily, 0, day);
        public static LoadoutScreen Training() => new LoadoutScreen(GameMode.Training);
        public static LoadoutScreen Survival() => new LoadoutScreen(GameMode.Survival);

        public override string Title => Loc.T("title_loadout");
        public override string Subtitle => Loc.T("loadout_sub");

        LevelDef Level => _mode == GameMode.Campaign ? WorldOne.Level(_level) : _mode == GameMode.Daily ? DailyChallenge.ForDay(_day).Level : null;

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            RectTransform row = UiKit.Rect(root, "Row");
            UiKit.Stretch(row, 18f, 62f, 18f, 16f);
            UiKit.Row(row, 14f, TextAnchor.UpperLeft, false, true);
            BuildCard(row, p);
            BuildRight(row, p);
        }

        public override void OnShow()
        {
            if (_ideasShown) return;
            _ideasShown = true;
            LevelDef l = Level;
            if (l == null || _mode != GameMode.Campaign) return;
            Profile profile = ServiceLocator.Profile;
            foreach (IdeaCard c in l.Ideas)
            {
                if (profile.HasSeen(c)) continue;
                Ui.ShowModal(new IdeaCardModal(c, true));
                break;
            }
        }

        // ---------------------------------------------------------------- left card

        void Colors(out Color face, out Color edge, out string kicker, out string title, out string goal)
        {
            LevelDef l = Level;
            switch (_mode)
            {
                case GameMode.QuickDuel:
                    face = UiKit.Hex(0x1C9AD6); edge = UiKit.Hex(0x13709E);
                    kicker = Loc.T("qd_kicker"); title = Loc.T("qd_title"); goal = Loc.T("goal_duel");
                    break;
                case GameMode.Daily:
                    DailyChallenge.Daily d = DailyChallenge.ForDay(_day);
                    face = UiKit.Hex(0x1C9AD6); edge = UiKit.Hex(0x13709E);
                    kicker = Loc.T("daily_kicker"); title = Loc.T("tpl_" + d.Template); goal = GameVisuals.GoalText(l);
                    break;
                case GameMode.Training:
                    face = UiKit.Hex(0x8E7A5B); edge = UiKit.Hex(0x65563F);
                    kicker = Loc.T("tr_kicker"); title = Loc.T("tr_title"); goal = Loc.T("tr_goal");
                    break;
                case GameMode.Survival:
                    face = UiKit.Hex(0x6D4AFF); edge = UiKit.Hex(0x4A2BD1);
                    kicker = Loc.T("sv_kicker"); title = Loc.T("sv_title"); goal = Loc.T("sv_goal");
                    break;
                default:
                    bool boss = l.Goal == GoalKind.Boss;
                    face = UiKit.Hex(boss ? 0xE5484Du : 0x12A67Au); edge = UiKit.Hex(boss ? 0xB02D32u : 0x0B7555u);
                    string world = Loc.T("world_1").ToUpperInvariant();
                    kicker = boss ? Loc.F("ld_kicker_boss", world, l.Number) : Loc.F("ld_kicker_level", world, l.Number);
                    title = l.Name;
                    goal = GameVisuals.GoalText(l);
                    break;
            }
        }

        void BuildCard(RectTransform row, Palette p)
        {
            Colors(out Color face, out Color edge, out string kicker, out string title, out string goal);
            RectTransform holder = UiKit.Rect(row, "Card");
            UiKit.Size(holder, 246f, -1, 0f, 1f);
            Image e = UiKit.Box(holder, "Edge", edge, 22f);
            UiKit.Stretch(e.rectTransform, 0, 5, 0, -5);
            Image card = UiKit.Box(holder, "Face", face, 22f);
            UiKit.Stretch(card.rectTransform);
            UiKit.Column(card, 7f, TextAnchor.UpperLeft, true, false, new RectOffset(16, 16, 14, 14));

            TextMeshProUGUI k = UiKit.Label(card.transform, kicker, FontRole.Body, 12f, new Color(1, 1, 1, 0.95f));
            UiKit.Size(k, -1, 16f);
            UiKit.Fit(k);
            TextMeshProUGUI t = UiKit.ShadowLabel(card.transform, title, FontRole.Display, 30f, Color.white, new Color(0, 0, 0, 0.15f), 3f,
                TextAlignmentOptions.MidlineLeft);
            UiKit.Size(t.transform.parent as RectTransform, -1, 34f);
            UiKit.Fit(t, 0.6f);
            TextMeshProUGUI g = UiKit.Paragraph(card.transform, Icons.Target + " " + goal, FontRole.Body, 13f, Color.white, TextAlignmentOptions.TopLeft);
            UiKit.Size(g, -1, 34f);

            LevelDef l = Level;
            if (_mode == GameMode.Campaign && l.Ideas.Length > 0) NewLine(card.transform, l.Ideas[0]);
            if (_mode == GameMode.Daily)
            {
                DailyChallenge.Daily d = DailyChallenge.ForDay(_day);
                Chip(card.transform, Loc.T("twist_" + d.Twist));
            }
            if (_mode == GameMode.QuickDuel) BuildDifficulty(card.transform, face);
            if (_mode == GameMode.Training) BuildTraining(card.transform, face);

            string meta = Meta();
            if (!string.IsNullOrEmpty(meta))
            {
                TextMeshProUGUI m = UiKit.Paragraph(card.transform, meta, FontRole.Body, 12f, new Color(1, 1, 1, 0.95f), TextAlignmentOptions.TopLeft);
                UiKit.Size(m, -1, 32f);
            }
            if (_mode == GameMode.Campaign && LossHelp.OfferAssist(ServiceLocator.Profile.Level(_level).LossesInARow)) BuildAssist(card.transform);

            UiKit.Spacer(card.transform);
            RectTransform startHolder = UiKit.Rect(card.transform, "StartHolder");
            UiKit.Size(startHolder, -1, 63f);
            Button3D start = Button3D.Styled(startHolder, "Start", ButtonStyle.Gold, Loc.T("ld_start"), 26f, 18f, 5f, 4f);
            UiKit.Stretch((RectTransform)start.transform, 0, 0, 0, 5);
            start.Sound = Feel.SoundId.UiConfirm;
            start.OnClick(Start);
        }

        void NewLine(Transform parent, IdeaCard idea)
        {
            Button3D b = Button3D.Create(parent, "New", new Color(1, 1, 1, 0.95f), Color.clear, 12f, 0f, 2f);
            UiKit.Size(b, -1, 44f);
            RectTransform row = UiKit.Rect(b.Body, "Row");
            UiKit.Stretch(row, 10f, 6f, 10f, 6f);
            UiKit.Row(row, 6f, TextAnchor.MiddleLeft);
            TextMeshProUGUI chip = Widgets.Chip(row, Loc.T("ld_new"), Widgets.Pink, Color.white, 11f, 6f);
            chip.transform.parent.gameObject.AddComponent<LayoutElement>().minWidth = 40f;
            TextMeshProUGUI text = UiKit.Paragraph(row, Loc.T("idea_" + idea) + ": " + Loc.T("idea_" + idea + "_text"), FontRole.BodyBold, 11.5f,
                Widgets.Navy, TextAlignmentOptions.MidlineLeft);
            UiKit.Size(text, -1, 32f, 1f);
            b.OnClick(() => Ui.ShowModal(new IdeaCardModal(idea, false)));
        }

        static void Chip(Transform parent, string text)
        {
            Image c = UiKit.Box(parent, "Chip", new Color(1, 1, 1, 0.2f), 10f);
            UiKit.Size(c, -1, 26f);
            TextMeshProUGUI t = UiKit.Label(c.transform, text, FontRole.Body, 12f, Color.white);
            UiKit.Stretch((RectTransform)t.transform, 10, 0, 10, 0);
        }

        void BuildDifficulty(Transform parent, Color face)
        {
            RectTransform row = UiKit.Rect(parent, "Difficulty");
            UiKit.Size(row, -1, 32f);
            UiKit.Row(row, 6f, TextAnchor.MiddleLeft, true, true);
            foreach (string d in Logic.Modes.QuickDuel.Difficulties)
            {
                bool on = d == _difficulty;
                Button3D b = Button3D.Create(row, d, on ? Color.white : new Color(1, 1, 1, 0.22f), Color.clear, 10f, 0f, 2f);
                b.AddLabel(Loc.T("diff_" + d), FontRole.Display, 13f, on ? UiKit.Hex(0x13709E) : Color.white);
                string pick = d;
                b.OnClick(() =>
                {
                    _difficulty = pick;
                    Ui.Refresh();
                });
            }
            Button3D arena = Button3D.Create(parent, "Arena", new Color(1, 1, 1, 0.22f), Color.clear, 10f, 0f, 2f);
            UiKit.Size(arena, -1, 30f);
            arena.AddLabel(Loc.T("qd_arena") + ": " + Loc.T("arena_" + Arenas[_arena]) + "  " + Icons.Sync, FontRole.Display, 13f, Color.white);
            arena.OnClick(() =>
            {
                _arena = (_arena + 1) % Arenas.Length;
                Ui.Refresh();
            });
        }

        void BuildTraining(Transform parent, Color face)
        {
            TextMeshProUGUI dl = UiKit.Label(parent, Loc.T("tr_distance"), FontRole.Body, 12f, Color.white);
            UiKit.Size(dl, -1, 16f);
            RectTransform row = UiKit.Rect(parent, "Distance");
            UiKit.Size(row, -1, 30f);
            UiKit.Row(row, 6f, TextAnchor.MiddleLeft, true, true);
            foreach (int d in TrainingRange.Distances)
            {
                bool on = d == _distance;
                Button3D b = Button3D.Create(row, d.ToString(), on ? Color.white : new Color(1, 1, 1, 0.22f), Color.clear, 10f, 0f, 2f);
                b.AddLabel(Loc.F("meters", d), FontRole.Display, 13f, on ? UiKit.Hex(0x65563F) : Color.white);
                int pick = d;
                b.OnClick(() =>
                {
                    _distance = pick;
                    Ui.Refresh();
                });
            }
            RectTransform wind = UiKit.Rect(parent, "Wind");
            UiKit.Size(wind, -1, 30f);
            UiKit.Row(wind, 6f, TextAnchor.MiddleLeft);
            Button3D minus = Button3D.Create(wind, "Minus", new Color(1, 1, 1, 0.22f), Color.clear, 10f, 0f, 2f);
            UiKit.Size(minus, 36f, 30f);
            minus.AddIcon(Icons.Remove, 18f, Color.white);
            TextMeshProUGUI w = UiKit.Label(wind, Loc.T("tr_wind") + " " + WindText(_wind), FontRole.Display, 14f, Color.white, TextAlignmentOptions.Center);
            UiKit.Size(w, -1, 30f, 1f);
            Button3D plus = Button3D.Create(wind, "Plus", new Color(1, 1, 1, 0.22f), Color.clear, 10f, 0f, 2f);
            UiKit.Size(plus, 36f, 30f);
            plus.AddIcon(Icons.Add, 18f, Color.white);
            minus.OnClick(() => { _wind = Mathf.Max(-5, _wind - 1); Ui.Refresh(); });
            plus.OnClick(() => { _wind = Mathf.Min(5, _wind + 1); Ui.Refresh(); });
        }

        public static string WindText(int wind) =>
            wind == 0 ? "0" : wind < 0 ? Icons.ArrowBack + " " + (-wind) : wind + " " + Icons.ArrowForward;

        void BuildAssist(Transform parent)
        {
            Button3D b = Button3D.Create(parent, "Assist", _assist ? Color.white : new Color(1, 1, 1, 0.22f), Color.clear, 10f, 0f, 2f);
            UiKit.Size(b, -1, 30f);
            b.AddLabel((_assist ? Icons.CheckCircle + " " + Loc.T("ld_assist") : Loc.T("ld_assist_offer")), FontRole.Body, 11.5f,
                _assist ? UiKit.Hex(0x0B7555) : Color.white);
            b.OnClick(() =>
            {
                _assist = !_assist;
                Ui.Refresh();
            });
        }

        string Meta()
        {
            Profile profile = ServiceLocator.Profile;
            LevelDef l = Level;
            switch (_mode)
            {
                case GameMode.Campaign:
                {
                    string par = l.IsDuelGoal ? Loc.F("ld_par_turns", l.Par) : Loc.F("ld_par_arrows", l.Par);
                    string opp = l.Opponents.Length > 0 ? " · " + Loc.F("ld_opponent", Names(l)) : "";
                    LevelSave save = profile.Level(_level);
                    string stars = save.Cleared ? "\n" + StarsText(save.Stars) : "";
                    return par + opp + stars;
                }
                case GameMode.Daily:
                    return Loc.F("daily_reward", profile.Economy.DailyChallenge);
                case GameMode.QuickDuel:
                    return Loc.T("qd_meta");
                case GameMode.Survival:
                    return Loc.F("sv_best", profile.Data.SurvivalBestWave);
                default:
                    return null;
            }
        }

        static string Names(LevelDef l)
        {
            var names = new List<string>();
            foreach (OpponentSpec o in l.Opponents)
            {
                string n = EnemyTable.ById(o.EnemyId).Name;
                if (!names.Contains(n)) names.Add(n);
            }
            return string.Join(", ", names);
        }

        static string StarsText(int n)
        {
            string s = "";
            for (int i = 0; i < 3; i++) s += i < n ? "<color=#FFD23F>★</color>" : "<color=#FFFFFF60>★</color>";
            return s;
        }

        // ---------------------------------------------------------------- right column

        void BuildRight(RectTransform row, Palette p)
        {
            RectTransform col = UiKit.Rect(row, "Right");
            UiKit.Size(col, -1, -1, 1f, 1f);
            UiKit.Column(col, 8f, TextAnchor.UpperLeft, true, false);
            BuildTips(col, p);
            if (_mode != GameMode.Training) BuildBoosters(col, p);
            BuildArcherRow(col, p);
        }

        ArrowTip[] ForcedTips()
        {
            LevelDef l = Level;
            if (l == null) return null;
            if (l.OnlyTip.HasValue) return new[] { l.OnlyTip.Value };
            return l.ForcedTips;
        }

        void BuildTips(RectTransform col, Palette p)
        {
            Profile profile = ServiceLocator.Profile;
            ArrowTip[] forced = ForcedTips();
            RectTransform head = UiKit.Rect(col, "TipsHead");
            UiKit.Size(head, -1, 20f);
            UiKit.Row(head, 6f, TextAnchor.LowerLeft);
            TextMeshProUGUI t = UiKit.Label(head, Loc.T("ld_arrows"), FontRole.Display, 16f, p.Ink);
            UiKit.Size(t, t.preferredWidth + 2f, 20f);
            TextMeshProUGUI hint = UiKit.Label(head, forced != null ? Loc.F("ld_forced", TipNames(forced)) : Loc.T("ld_arrows_hint"), FontRole.Body, 12f, p.InkMuted);
            UiKit.Size(hint, -1, 20f, 1f);
            UiKit.Fit(hint);
            if (forced == null)
            {
                TextMeshProUGUI count = UiKit.Label(head, _tips.Count + "/" + LoadoutRules.MaxSpecialTips, FontRole.Body, 12f, p.InkMuted, TextAlignmentOptions.MidlineRight);
                UiKit.Size(count, 30f, 20f);
            }

            RectTransform grid = UiKit.Rect(col, "Tips");
            UiKit.Size(grid, -1, 110f);
            UiKit.Column(grid, 6f, TextAnchor.UpperLeft, true, true);
            RectTransform line = null;
            TipTable table = TipTable.CreateDefault();
            foreach (ArrowTip tip in Enum.GetValues(typeof(ArrowTip)))
            {
                if ((int)tip % 4 == 0)
                {
                    line = UiKit.Rect(grid, "Line");
                    UiKit.Row(line, 7f, TextAnchor.MiddleLeft, true, true);
                }
                bool unlocked = profile.IsTipUnlocked(tip) || _mode == GameMode.Training && profile.IsTipUnlocked(tip);
                bool picked = tip == ArrowTip.Normal ? (forced == null || Array.IndexOf(forced, ArrowTip.Normal) >= 0) : forced != null ? Array.IndexOf(forced, tip) >= 0 : _tips.Contains(tip);
                TipTile(line, tip, unlocked, picked, table[tip].Ammo, forced != null, p);
            }
        }

        static string TipNames(ArrowTip[] tips)
        {
            var n = new List<string>();
            foreach (ArrowTip t in tips) n.Add(Loc.T("tipname_" + t));
            return string.Join(", ", n);
        }

        void TipTile(RectTransform parent, ArrowTip tip, bool unlocked, bool picked, int ammo, bool locked, Palette p)
        {
            Button3D b = Button3D.Create(parent, tip.ToString(), picked ? p.Solid : p.Card, Color.clear, 14f, 0f, 2f);
            if (picked) UiKit.Border(b.Face.transform, Widgets.TierGold, 14f, 3f);
            else UiKit.Border(b.Face.transform, p.Line, 14f, 1.5f);
            RectTransform col = UiKit.Rect(b.Body, "Col");
            UiKit.Stretch(col, 2f, 4f, 2f, 4f);
            UiKit.Column(col, 0f, TextAnchor.MiddleCenter, true, false);
            TextMeshProUGUI ic = UiKit.Glyph(col, unlocked ? GameVisuals.TipIcon(tip) : Icons.Lock, 19f, unlocked ? GameVisuals.TipColor(tip) : p.InkMuted);
            UiKit.Size(ic, -1, 20f);
            TextMeshProUGUI n = UiKit.Label(col, Loc.T("tipname_" + tip), FontRole.Body, 10.5f, p.Ink, TextAlignmentOptions.Center);
            UiKit.Size(n, -1, 13f);
            UiKit.Fit(n);
            string sub = !unlocked ? Loc.F("ld_tip_lv", TipUnlocks.LevelFor(tip)) : ammo < 0 ? "×∞" : "×" + ammo;
            TextMeshProUGUI a = UiKit.Label(col, sub, FontRole.Body, 9.5f, p.InkMuted, TextAlignmentOptions.Center);
            UiKit.Size(a, -1, 12f);
            if (!unlocked)
            {
                var cg = b.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0.6f;
            }
            b.OnClick(() =>
            {
                if (tip == ArrowTip.Normal) { Ui.Toast(Loc.T("ld_normal_always")); return; }
                if (!unlocked) { Ui.Toast(Loc.F("ld_tip_locked", Loc.T("tipname_" + tip), TipUnlocks.LevelFor(tip))); return; }
                if (locked) { Ui.Toast(Loc.F("ld_forced", TipNames(ForcedTips()))); return; }
                if (_tips.Contains(tip)) _tips.Remove(tip);
                else if (_tips.Count >= LoadoutRules.MaxSpecialTips) { Ui.Toast(Loc.T("ld_max_tips")); return; }
                else _tips.Add(tip);
                ServiceLocator.Profile.SaveLoadoutTips(_tips.ToArray());
                ServiceLocator.Save.MarkDirty();
                Ui.Refresh();
            });
        }

        void BuildBoosters(RectTransform col, Palette p)
        {
            Profile profile = ServiceLocator.Profile;
            RectTransform head = UiKit.Rect(col, "BoostersHead");
            UiKit.Size(head, -1, 20f);
            UiKit.Row(head, 6f, TextAnchor.LowerLeft);
            TextMeshProUGUI t = UiKit.Label(head, Loc.T("ld_boosters"), FontRole.Display, 16f, p.Ink);
            UiKit.Size(t, t.preferredWidth + 2f, 20f);
            TextMeshProUGUI hint = UiKit.Label(head, Loc.T("ld_boosters_hint"), FontRole.Body, 12f, p.InkMuted);
            UiKit.Size(hint, -1, 20f, 1f);
            int total = profile.BoostersCost(_boosters.ToArray());
            if (total > 0)
            {
                TextMeshProUGUI cost = UiKit.Label(head, Fmt.Coins(total), FontRole.Display, 14f, total > profile.Coins ? Widgets.Red : p.Ink, TextAlignmentOptions.MidlineRight);
                UiKit.Size(cost, 80f, 20f);
            }
            RectTransform row = UiKit.Rect(col, "Boosters");
            UiKit.Size(row, -1, 60f);
            UiKit.Row(row, 7f, TextAnchor.MiddleLeft, true, true);
            foreach (BoosterKind b in Enum.GetValues(typeof(BoosterKind)))
            {
                bool on = _boosters.Contains(b);
                Button3D tile = Button3D.Create(row, b.ToString(), on ? p.Solid : p.Card, Color.clear, 14f, 0f, 2f);
                UiKit.Border(tile.Face.transform, on ? Widgets.TierGold : p.Line, 14f, on ? 3f : 1.5f);
                RectTransform r = UiKit.Rect(tile.Body, "Row");
                UiKit.Stretch(r, 8f, 4f, 6f, 4f);
                UiKit.Row(r, 6f, TextAnchor.MiddleLeft);
                Image disc = UiKit.Disc(r, "Disc", GameVisuals.BoosterColor(b));
                UiKit.Size(disc, 32f, 32f);
                TextMeshProUGUI ic = UiKit.Glyph(disc.transform, GameVisuals.BoosterIcon(b), 18f, Color.white);
                UiKit.Stretch((RectTransform)ic.transform);
                RectTransform words = UiKit.Rect(r, "Words");
                UiKit.Size(words, -1, 44f, 1f);
                UiKit.Column(words, 0f, TextAnchor.MiddleLeft, true, false);
                TextMeshProUGUI n = UiKit.Label(words, Loc.T("booster_" + b), FontRole.Display, 12.5f, p.Ink);
                UiKit.Size(n, -1, 16f);
                UiKit.Fit(n);
                TextMeshProUGUI c = UiKit.Label(words, Fmt.Coins(profile.Economy.BoosterCost(b)), FontRole.Display, 12f, p.InkMuted);
                UiKit.Size(c, -1, 15f);
                BoosterKind kind = b;
                tile.OnClick(() =>
                {
                    if (_boosters.Contains(kind)) _boosters.Remove(kind);
                    else
                    {
                        _boosters.Add(kind);
                        Ui.Toast(Loc.T("booster_" + kind + "_desc"));
                    }
                    Ui.Refresh();
                });
            }
        }

        void BuildArcherRow(RectTransform col, Palette p)
        {
            Profile profile = ServiceLocator.Profile;
            string archer = profile.Data.EquippedArcher;
            RectTransform row = UiKit.Rect(col, "ArcherRow");
            UiKit.Size(row, -1, 58f);
            UiKit.Row(row, 8f, TextAnchor.MiddleLeft, false, true);

            Button3D pick = Button3D.Create(row, "Archer", p.Solid, p.Shadow, 14f, 3f, 2f);
            UiKit.Size(pick, -1, 54f, 1.4f);
            RectTransform r = UiKit.Rect(pick.Body, "Row");
            UiKit.Stretch(r, 6f, 4f, 10f, 4f);
            UiKit.Row(r, 8f, TextAnchor.MiddleLeft);
            Image disc = UiKit.Disc(r, "Disc", UiKit.Hex(ArcherLooks.Color(archer)));
            UiKit.Size(disc, 44f, 44f);
            string skin = profile.Data.EquippedSkins.TryGetValue(archer, out string s) ? s : CosmeticCatalog.DefaultSkinFor(archer);
            Image face = UiKit.Box(disc.transform, "Face", Color.white, 0);
            face.sprite = ArtLibrary.Portrait(ArcherLooks.ForHero(archer, skin));
            face.preserveAspect = true;
            UiKit.Stretch(face.rectTransform);
            RectTransform words = UiKit.Rect(r, "Words");
            UiKit.Size(words, -1, 44f, 1f);
            UiKit.Column(words, 0f, TextAnchor.MiddleLeft, true, false);
            TextMeshProUGUI n = UiKit.Label(words, ArcherTable.Hero(archer).Name + "  <size=11><color=" + Fmt.Hex(p.InkMuted) + ">" +
                Loc.F("ld_tip_lv", profile.ArcherLevel(archer)) + "</color></size>", FontRole.Display, 15f, p.Ink);
            UiKit.Size(n, -1, 20f);
            UiKit.Fit(n);
            TextMeshProUGUI c = UiKit.Label(words, Loc.T("ld_change"), FontRole.Body, 11f, Widgets.Purple);
            UiKit.Size(c, -1, 16f);
            pick.OnClick(() => Ui.Push(new ArchersScreen(archer)));

            // Trail picker: the owned trails as swatches.
            Image trails = UiKit.Card(row, "Trails", 14f);
            UiKit.Size(trails, -1, 54f, 1f);
            UiKit.Column(trails, 2f, TextAnchor.MiddleLeft, true, false, new RectOffset(10, 8, 5, 5));
            TextMeshProUGUI tl = UiKit.Label(trails.transform, Loc.T("ld_trail"), FontRole.Body, 11f, p.InkMuted);
            UiKit.Size(tl, -1, 14f);
            RectTransform sw = UiKit.Rect(trails.transform, "Swatches");
            UiKit.Size(sw, -1, 26f);
            UiKit.Row(sw, 6f, TextAnchor.MiddleLeft);
            foreach (CosmeticDef t in CosmeticCatalog.All)
            {
                if (t.Kind != CosmeticKind.Trail) continue;
                bool owned = profile.Owns(t.Id) || t.Source == CosmeticSource.Default;
                bool eq = profile.Data.EquippedTrail == t.Id;
                Button3D d = Button3D.Create(sw, t.Id, TrailColor(t.Id), Color.clear, 12f, 0f, 1f);
                UiKit.Size(d, 26f, 26f);
                d.Face.sprite = ShapeSprites.Circle;
                d.Face.type = Image.Type.Simple;
                if (eq)
                {
                    Image ring = UiKit.Box(d.Body, "Ring", Widgets.TierGold, 0);
                    ring.sprite = ShapeSprites.Outline(13f, 3f);
                    ring.type = Image.Type.Sliced;
                    UiKit.Stretch(ring.rectTransform, -2, -2, -2, -2);
                }
                if (!owned) d.AddIcon(Icons.Lock, 11f, Color.white);
                string id = t.Id;
                d.OnClick(() =>
                {
                    if (!owned) { Ui.Toast(Loc.T("cos_" + id) + " · " + Loc.T("title_shop")); return; }
                    profile.Data.EquippedTrail = id;
                    ServiceLocator.Save.MarkDirty();
                    Ui.Refresh();
                });
            }
        }

        public static Color TrailColor(string id)
        {
            switch (id)
            {
                case "trail_sparkle": return UiKit.Hex(0xFFD23F);
                case "trail_rainbow": return UiKit.Hex(0xFF5FA2);
                case "trail_comet": return UiKit.Hex(0x38BDF8);
                default: return UiKit.Hex(0xC9D2DC);
            }
        }

        // ---------------------------------------------------------------- start

        void Start()
        {
            Profile profile = ServiceLocator.Profile;
            BoosterKind[] boosters = _boosters.ToArray();
            int cost = profile.BoostersCost(boosters);
            if (cost > profile.Coins)
            {
                Ui.ShowModal(new CoinsModal(profile.Missing(cost)));
                return;
            }
            var loadout = profile.CurrentLoadout();
            loadout.Tips = _tips.ToArray();
            loadout.Boosters = boosters;
            if (LoadoutRules.Validate(loadout, profile.OwnsArcher(loadout.ArcherId), profile.HighestLevelCleared) != LoadoutError.None)
            {
                _tips.Clear();
                Ui.Refresh();
                return;
            }
            if (cost > 0) profile.BuyBoosters(boosters);
            profile.SaveLoadoutTips(_tips.ToArray());
            ServiceLocator.CommitProfile();
            ServiceLocator.Save.SaveNow();
            var request = new MatchRequest
            {
                Mode = _mode, Level = _level, Day = _day, Difficulty = _difficulty, ArenaId = Arenas[_arena],
                TrainingDistance = _distance, TrainingWind = _wind, Boosters = boosters, Assist = _assist,
                Seed = _mode == GameMode.Daily ? (ulong)_day : (ulong)DateTime.UtcNow.Ticks
            };
            SceneFlow.StartMatch(request);
        }
    }
}
