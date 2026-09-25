using System;
using System.Globalization;
using ArcherArcade.Core;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Logic.Modes;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Daily Challenge (design `daily`): today's seeded level (same everywhere, offline), its twist, Play, and the
    /// 7-day streak with the day-7 reward. Missing a day resets the streak; nothing can be bought to restore it.
    /// </summary>
    public sealed class DailyScreen : UiScreen
    {
        public override string Title => Loc.T("title_daily");

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Profile profile = ServiceLocator.Profile;
            int today = DailyChallenge.DayNumber(DateTime.UtcNow);
            DailyChallenge.Daily daily = DailyChallenge.ForDay(today);
            bool solved = profile.DailySolved(today);

            RectTransform row = UiKit.Rect(root, "Row");
            UiKit.Stretch(row, 18f, 62f, 18f, 16f);
            UiKit.Row(row, 14f, TextAnchor.UpperLeft, false, true);

            // Blue card.
            RectTransform holder = UiKit.Rect(row, "Today");
            UiKit.Size(holder, -1, -1, 1.3f, 1f);
            Image edge = UiKit.Box(holder, "Edge", UiKit.Hex(0x13709E), 22f);
            UiKit.Stretch(edge.rectTransform, 0, 5, 0, -5);
            Image card = UiKit.Box(holder, "Face", UiKit.Hex(0x1C9AD6), 22f);
            UiKit.Stretch(card.rectTransform);
            UiKit.Column(card, 8f, TextAnchor.UpperLeft, true, false, new RectOffset(18, 18, 16, 16));
            DateTime now = DateTime.UtcNow;
            string date = now.ToString("ddd d MMM", CultureInfo.InvariantCulture).ToUpperInvariant();
            string seed = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            TextMeshProUGUI k = UiKit.Label(card.transform, Loc.F("daily_seed", date, seed), FontRole.Body, 12f, new Color(1, 1, 1, 0.95f));
            UiKit.Size(k, -1, 16f);
            TextMeshProUGUI t = UiKit.ShadowLabel(card.transform, Loc.T("tpl_" + daily.Template), FontRole.Display, 32f, Color.white, new Color(0, 0, 0, 0.15f), 3f,
                TextAlignmentOptions.MidlineLeft);
            UiKit.Size(t.transform.parent as RectTransform, -1, 38f);
            UiKit.Fit(t, 0.6f);
            TextMeshProUGUI same = UiKit.Paragraph(card.transform, Loc.T("daily_same"), FontRole.Body, 13f, Color.white, TextAlignmentOptions.TopLeft);
            UiKit.Size(same, -1, 36f);
            RectTransform chips = UiKit.Rect(card.transform, "Mods");
            UiKit.Size(chips, -1, 26f);
            UiKit.Row(chips, 6f, TextAnchor.MiddleLeft);
            Mod(chips, Icons.AutoAwesome + " " + Loc.T("twist_" + daily.Twist));
            Mod(chips, Icons.Target + " " + GameVisuals.GoalText(daily.Level));
            TextMeshProUGUI reward = UiKit.Label(card.transform, Loc.F("daily_reward", profile.Economy.DailyChallenge), FontRole.Body, 12f, new Color(1, 1, 1, 0.95f));
            UiKit.Size(reward, -1, 18f);
            UiKit.Spacer(card.transform);
            RectTransform playHolder = UiKit.Rect(card.transform, "PlayHolder");
            UiKit.Size(playHolder, -1, 61f);
            Button3D play = solved
                ? Button3D.Off(playHolder, "Solved", Loc.T("daily_solved"), 20f, 18f)
                : Button3D.Styled(playHolder, "Play", ButtonStyle.Gold, Loc.T("daily_play"), 24f, 18f, 5f, 4f);
            UiKit.Stretch((RectTransform)play.transform, 0, 0, 0, 5);
            if (solved) play.SetColors(new Color(1, 1, 1, 0.25f), Color.clear);
            if (solved && play.Text) play.Text.color = Color.white;
            play.OnClick(() =>
            {
                if (solved) { Ui.Toast(Loc.T("daily_solved")); return; }
                Ui.Push(LoadoutScreen.Daily(today));
            });

            // Streak card.
            Image streakCard = UiKit.Card(row, "Streak", 22f);
            UiKit.Size(streakCard, -1, -1, 1f, 1f);
            UiKit.Column(streakCard, 10f, TextAnchor.UpperLeft, true, false, new RectOffset(14, 14, 14, 14));
            int streak = profile.CurrentStreak(today);
            RectTransform head = UiKit.Rect(streakCard.transform, "Head");
            UiKit.Size(head, -1, 24f);
            UiKit.Row(head, 6f, TextAnchor.MiddleLeft);
            TextMeshProUGUI fire = UiKit.Glyph(head, Icons.LocalFireDepartment, 20f, UiKit.Hex(0xF0641E));
            UiKit.Size(fire, 22f, 22f);
            TextMeshProUGUI st = UiKit.Label(head, Loc.F("daily_streak", streak), FontRole.Display, 18f, p.Ink);
            UiKit.Size(st, -1, 24f, 1f);
            TextMeshProUGUI best = UiKit.Label(head, Loc.F("daily_best", profile.Data.Daily.BestStreak), FontRole.Body, 12f, p.InkMuted, TextAlignmentOptions.MidlineRight);
            UiKit.Size(best, 70f, 24f);

            RectTransform grid = UiKit.Rect(streakCard.transform, "Days");
            UiKit.Size(grid, -1, 62f);
            UiKit.Row(grid, 5f, TextAnchor.MiddleLeft, true, true);
            int days = profile.Economy.DailyStreakDays;
            int doneInCycle = solved ? (streak - 1) % days + 1 : streak % days;
            int todayIndex = solved ? doneInCycle - 1 : doneInCycle;
            string[] names = { "day_mon", "day_tue", "day_wed", "day_thu", "day_fri", "day_sat", "day_sun" };
            for (int i = 0; i < days; i++)
            {
                DateTime d = now.Date.AddDays(i - todayIndex);
                bool done = i < doneInCycle;
                bool isToday = i == todayIndex;
                Color bg = done ? Widgets.Green : p.Solid;
                Image cell = UiKit.Box(grid, "Day" + i, bg, 12f);
                if (isToday && !done) UiKit.Border(cell.transform, UiKit.Hex(0xF0641E), 12f, 2.5f);
                RectTransform c = UiKit.Rect(cell.transform, "Col");
                UiKit.Stretch(c, 2, 6, 2, 6);
                UiKit.Column(c, 2f, TextAnchor.MiddleCenter, true, false);
                int dow = ((int)d.DayOfWeek + 6) % 7;
                TextMeshProUGUI dn = UiKit.Label(c, Loc.T(names[dow]), FontRole.Body, 10f, done ? Color.white : p.Ink, TextAlignmentOptions.Center);
                UiKit.Size(dn, -1, 14f);
                UiKit.Fit(dn);
                string icon = done ? Icons.Check : i == days - 1 ? Icons.CardGiftcard : isToday ? Icons.PlayArrow : "·";
                TextMeshProUGUI ic = UiKit.Label(c, icon, done || i == days - 1 || isToday ? FontRole.Icon : FontRole.Display, 16f,
                    done ? Color.white : i == days - 1 ? UiKit.Hex(0xE83E8C) : isToday ? UiKit.Hex(0xF0641E) : p.InkMuted, TextAlignmentOptions.Center);
                UiKit.Size(ic, -1, 20f);
            }
            TextMeshProUGUI note = UiKit.Paragraph(streakCard.transform, Loc.T("daily_note"), FontRole.BodyBold, 12f, p.InkMuted, TextAlignmentOptions.TopLeft, 4f);
            UiKit.Size(note, -1, 60f);
            UiKit.Spacer(streakCard.transform);
            Image trail = UiKit.Box(streakCard.transform, "Reward", p.Solid, 14f);
            UiKit.Size(trail, -1, 44f);
            UiKit.Row(trail, 8f, TextAnchor.MiddleLeft, false, false, new RectOffset(10, 10, 0, 0));
            Image chest = UiKit.Box(trail.transform, "Chest", Color.white, 0);
            chest.sprite = ArtLibrary.Get(ArtLibrary.Ui, "chest_gold");
            chest.preserveAspect = true;
            UiKit.Size(chest, 36f, 36f);
            TextMeshProUGUI rw = UiKit.Label(trail.transform, Fmt.Coins(profile.Economy.DailyStreakChestCoins) + " + " + Loc.T("cos_trail_rainbow"),
                FontRole.Display, 15f, p.Ink);
            UiKit.Size(rw, -1, 30f, 1f);
        }

        static void Mod(Transform parent, string text)
        {
            Image c = UiKit.Box(parent, "Mod", new Color(1, 1, 1, 0.2f), 10f);
            TextMeshProUGUI t = UiKit.Label(c.transform, text, FontRole.Body, 12f, Color.white);
            UiKit.Stretch((RectTransform)t.transform, 10, 0, 10, 0);
            UiKit.Size(c, t.preferredWidth + 22f, 26f);
        }

        public override void OnShow() => ServiceLocator.Audio?.PlayMusic(Feel.SoundId.MusicHome);
    }
}
