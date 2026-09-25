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
    /// Stats (design `stats`): 4 × 2 tiles (accuracy, headshots, matches won, win rate, longest shot, favourite
    /// archer, best daily streak, time played) and a strip with wins / losses for every mode.
    /// </summary>
    public sealed class StatsScreen : UiScreen
    {
        public override string Title => Loc.T("title_stats");

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Profile profile = ServiceLocator.Profile;
            long shots = profile.Data.Stat(StatKey.Shots), hits = profile.Data.Stat(StatKey.Hits) + profile.Data.Stat(StatKey.TargetHits);
            int accuracy = shots == 0 ? 0 : (int)System.Math.Round(100.0 * System.Math.Min(hits, shots) / shots);
            int wins = 0, losses = 0;
            for (int i = 0; i < profile.Data.WinsByMode.Length; i++)
            {
                wins += profile.Data.WinsByMode[i];
                losses += profile.Data.LossesByMode[i];
            }
            int winRate = wins + losses == 0 ? 0 : (int)System.Math.Round(100.0 * wins / (wins + losses));
            long secs = profile.Data.Stat(StatKey.PlaySeconds);
            string playTime = secs >= 3600 ? Loc.F("st_hm", secs / 3600, secs / 60 % 60) : Loc.F("st_m", secs / 60);
            RectTransform col = UiKit.Rect(root, "Col");
            UiKit.Stretch(col, 18f, 62f, 18f, 16f);
            UiKit.Column(col, 12f, TextAnchor.UpperLeft, true, false);

            string fav = profile.FavouriteArcher;
            string[] icons = { Icons.Target, Icons.Explosion, Icons.EmojiEvents, Icons.TrendingUp, Icons.Straighten, null, Icons.CalendarMonth, Icons.Timer };
            string[] keys = { "st_accuracy", "st_headshots", "st_won", "st_winrate", "st_longest", "st_favourite", "st_streak", "st_time" };
            string[] values =
            {
                accuracy + "%", Loc.N(profile.Data.Stat(StatKey.Headshots)), Loc.N(wins), winRate + "%",
                Loc.F("meters", profile.Data.LongestShot.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)),
                ArcherTable.Hero(fav).Name, Loc.F("st_days", profile.Data.Daily.BestStreak), playTime
            };
            for (int r = 0; r < 2; r++)
            {
                RectTransform row = UiKit.Rect(col, "Row" + r);
                UiKit.Size(row, -1, -1, 1f, 1f);
                UiKit.Row(row, 12f, TextAnchor.UpperLeft, true, true);
                for (int c = 0; c < 4; c++)
                {
                    int i = r * 4 + c;
                    Tile(row, icons[i], keys[i], values[i], i == 5 ? fav : null, p);
                }
            }

            // Per-mode wins / losses.
            Image strip = UiKit.Card(col, "Modes", 18f);
            UiKit.Size(strip, -1, 50f);
            UiKit.Row(strip, 14f, TextAnchor.MiddleLeft, false, false, new RectOffset(14, 14, 0, 0));
            ModeCell(strip.transform, Icons.Map, Loc.T("mode_campaign"), WL(profile, GameMode.Campaign), p);
            ModeCell(strip.transform, Icons.Swords, Loc.T("mode_quick"), WL(profile, GameMode.QuickDuel), p);
            ModeCell(strip.transform, Icons.Group, Loc.T("mode_pvp"), Loc.N(profile.Data.Stat(StatKey.PvpMatches)), p);
            ModeCell(strip.transform, Icons.CalendarMonth, Loc.T("mode_daily"), WL(profile, GameMode.Daily), p);
            ModeCell(strip.transform, Icons.Waves, Loc.T("mode_survival"), Loc.F("hud_wave", profile.Data.SurvivalBestWave), p);
            ModeCell(strip.transform, Icons.Star, Loc.T("st_stars"), profile.TotalStars + "/60", p);
        }

        static string WL(Profile p, GameMode m) => p.Data.WinsByMode[(int)m] + "–" + p.Data.LossesByMode[(int)m];

        static void Tile(RectTransform row, string icon, string key, string value, string archer, Palette p)
        {
            Image card = UiKit.Card(row, key, 20f);
            RectTransform rt = card.rectTransform;
            if (archer != null)
            {
                RectTransform av = UiKit.Rect(card.transform, "Avatar");
                UiKit.TopLeft(av, 14f, 12f, 34f, 34f);
                Image disc = UiKit.Disc(av, "Disc", UiKit.Hex(ArcherLooks.Color(archer)));
                UiKit.Stretch(disc.rectTransform);
                Image face = UiKit.Box(av, "Face", Color.white, 0);
                face.sprite = ArtLibrary.Portrait(archer);
                face.preserveAspect = true;
                UiKit.Stretch(face.rectTransform);
            }
            else
            {
                TextMeshProUGUI ic = UiKit.Glyph(card.transform, icon, 24f, Widgets.Purple);
                UiKit.TopLeft((RectTransform)ic.transform, 12f, 10f, 32f, 32f);
            }
            TextMeshProUGUI v = UiKit.Label(card.transform, value, FontRole.Display, 28f, p.Ink, TextAlignmentOptions.BottomLeft);
            RectTransform vr = (RectTransform)v.transform;
            vr.anchorMin = new Vector2(0f, 0f);
            vr.anchorMax = new Vector2(1f, 0f);
            vr.pivot = new Vector2(0f, 0f);
            vr.offsetMin = new Vector2(14f, 30f);
            vr.offsetMax = new Vector2(-10f, 64f);
            UiKit.Fit(v, 0.5f);
            TextMeshProUGUI k = UiKit.Label(card.transform, Loc.T(key), FontRole.Body, 12f, p.InkMuted, TextAlignmentOptions.BottomLeft);
            RectTransform kr = (RectTransform)k.transform;
            kr.anchorMin = new Vector2(0f, 0f);
            kr.anchorMax = new Vector2(1f, 0f);
            kr.pivot = new Vector2(0f, 0f);
            kr.offsetMin = new Vector2(14f, 12f);
            kr.offsetMax = new Vector2(-10f, 30f);
            UiKit.Fit(k);
        }

        static void ModeCell(Transform parent, string icon, string name, string value, Palette p)
        {
            RectTransform cell = UiKit.Rect(parent, name);
            UiKit.Row(cell, 5f, TextAnchor.MiddleLeft);
            TextMeshProUGUI ic = UiKit.Glyph(cell, icon, 16f, p.InkMuted);
            UiKit.Size(ic, 18f, 18f);
            TextMeshProUGUI t = UiKit.Label(cell, name + " <b><color=" + Fmt.Hex(p.Ink) + ">" + value + "</color></b>", FontRole.Body, 12f, p.InkMuted);
            UiKit.Size(t, t.preferredWidth + 4f, 20f);
            UiKit.Size(cell, t.preferredWidth + 28f, 24f, 1f);
        }
    }
}
