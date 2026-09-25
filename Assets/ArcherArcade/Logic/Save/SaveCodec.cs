using System;
using System.Collections.Generic;
using ArcherArcade.Logic.Meta;

namespace ArcherArcade.Logic.Save
{
    /// <summary>
    /// SaveData ⇄ JSON. Reading is forgiving: a missing or malformed field gets its default and unknown fields are
    /// ignored, so a save from any older (or newer) version loads. <see cref="Migrate"/> upgrades old layouts.
    /// </summary>
    public static class SaveCodec
    {
        public static string ToJson(SaveData s)
        {
            var levels = new Dictionary<string, object>();
            foreach (KeyValuePair<string, LevelSave> kv in s.Levels)
            {
                LevelSave l = kv.Value;
                levels[kv.Key] = new Dictionary<string, object>
                {
                    { "cleared", l.Cleared }, { "stars", l.Stars }, { "bestTurns", l.BestTurns }, { "bestArrows", l.BestArrows },
                    { "attempts", l.Attempts }, { "lossesInARow", l.LossesInARow }
                };
            }
            var archers = new Dictionary<string, object>();
            foreach (KeyValuePair<string, int> kv in s.ArcherLevels) archers[kv.Key] = kv.Value;
            var skins = new Dictionary<string, object>();
            foreach (KeyValuePair<string, string> kv in s.EquippedSkins) skins[kv.Key] = kv.Value;
            var badges = new Dictionary<string, object>();
            foreach (KeyValuePair<string, BadgeTier> kv in s.BadgeTiers) badges[kv.Key] = (int)kv.Value;
            var stats = new Dictionary<string, object>();
            foreach (KeyValuePair<StatKey, long> kv in s.Stats) stats[kv.Key.ToString()] = kv.Value;
            var matches = new Dictionary<string, object>();
            foreach (KeyValuePair<string, int> kv in s.ArcherMatches) matches[kv.Key] = kv.Value;
            var tips = new List<object>();
            foreach (ArrowTip t in s.LoadoutTips) tips.Add(t.ToString());
            SettingsData st = s.Settings;

            var root = new Dictionary<string, object>
            {
                { "version", SaveData.CurrentVersion },
                { "playerName", s.PlayerName },
                { "coins", s.Coins },
                { "coinsEarned", s.CoinsEarned },
                { "archers", archers },
                { "equippedArcher", s.EquippedArcher },
                { "equippedSkins", skins },
                { "equippedTrail", s.EquippedTrail },
                { "ownedCosmetics", Strings(s.OwnedCosmetics) },
                { "loadoutTips", tips },
                { "levels", levels },
                { "badges", badges },
                { "pinnedBadges", Strings(s.PinnedBadges) },
                { "stats", stats },
                { "longestShot", s.LongestShot },
                { "winsByMode", Ints(s.WinsByMode) },
                { "lossesByMode", Ints(s.LossesByMode) },
                { "archerMatches", matches },
                { "wardenBestStars", s.WardenBestStars },
                {
                    "daily", new Dictionary<string, object>
                    {
                        { "lastSolvedDay", s.Daily.LastSolvedDay }, { "streak", s.Daily.Streak }, { "bestStreak", s.Daily.BestStreak },
                        { "streakChests", s.Daily.StreakChests }
                    }
                },
                { "chestsOpened", Ints(s.ChestsOpened.ToArray()) },
                {
                    "settings", new Dictionary<string, object>
                    {
                        { "music", st.Music }, { "sfx", st.Sfx }, { "haptics", st.Haptics }, { "theme", st.Theme },
                        { "language", st.Language }, { "leftHanded", st.LeftHanded }, { "biggerTargets", st.BiggerTargets },
                        { "aimSensitivity", st.AimSensitivity }, { "trajectoryAssist", st.TrajectoryAssist },
                        { "reduceMotion", st.ReduceMotion }, { "batterySaver", st.BatterySaver }, { "colorBlind", st.ColorBlind }
                    }
                },
                { "tutorialDone", s.TutorialDone },
                { "seenCards", Strings(s.SeenCards) },
                { "pvpName1", s.PvpName1 },
                { "pvpName2", s.PvpName2 }
            };
            return Json.Write(root);
        }

        /// <summary>Reads a save; returns a fresh save if the text is empty or not JSON at all.</summary>
        public static SaveData FromJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return NewSave();
            object parsed;
            try
            {
                parsed = Json.Read(text);
            }
            catch (FormatException)
            {
                return NewSave();
            }
            var root = parsed as Dictionary<string, object>;
            if (root == null) return NewSave();
            Migrate(root);

            var s = new SaveData
            {
                PlayerName = Str(root, "playerName", "Player"),
                Coins = Math.Max(0, Int(root, "coins", 0)),
                CoinsEarned = Math.Max(0L, Long(root, "coinsEarned", 0)),
                EquippedArcher = Str(root, "equippedArcher", "ranger"),
                EquippedTrail = Str(root, "equippedTrail", "trail_classic"),
                LongestShot = Dbl(root, "longestShot", 0),
                WardenBestStars = Int(root, "wardenBestStars", 0),
                TutorialDone = Bool(root, "tutorialDone", false),
                PvpName1 = Str(root, "pvpName1", "P1"),
                PvpName2 = Str(root, "pvpName2", "P2")
            };

            s.ArcherLevels.Clear();
            foreach (KeyValuePair<string, object> kv in Obj(root, "archers")) s.ArcherLevels[kv.Key] = Clamp(AsInt(kv.Value, 1), 1, 10);
            if (!s.ArcherLevels.ContainsKey("ranger")) s.ArcherLevels["ranger"] = 1;
            if (!s.ArcherLevels.ContainsKey(s.EquippedArcher)) s.EquippedArcher = "ranger";

            foreach (KeyValuePair<string, object> kv in Obj(root, "equippedSkins"))
            {
                if (kv.Value is string v) s.EquippedSkins[kv.Key] = v;
            }
            s.OwnedCosmetics.AddRange(StrList(root, "ownedCosmetics"));
            foreach (string t in StrList(root, "loadoutTips"))
            {
                if (Enum.TryParse(t, out ArrowTip tip) && tip != ArrowTip.Normal && !s.LoadoutTips.Contains(tip)) s.LoadoutTips.Add(tip);
            }

            foreach (KeyValuePair<string, object> kv in Obj(root, "levels"))
            {
                var l = kv.Value as Dictionary<string, object>;
                if (l == null) continue;
                s.Levels[kv.Key] = new LevelSave
                {
                    Cleared = Bool(l, "cleared", false),
                    Stars = Clamp(Int(l, "stars", 0), 0, 3),
                    BestTurns = Math.Max(0, Int(l, "bestTurns", 0)),
                    BestArrows = Math.Max(0, Int(l, "bestArrows", 0)),
                    Attempts = Math.Max(0, Int(l, "attempts", 0)),
                    LossesInARow = Math.Max(0, Int(l, "lossesInARow", 0))
                };
            }

            foreach (KeyValuePair<string, object> kv in Obj(root, "badges")) s.BadgeTiers[kv.Key] = (BadgeTier)Clamp(AsInt(kv.Value, 0), 0, 3);
            s.PinnedBadges.AddRange(StrList(root, "pinnedBadges"));
            foreach (KeyValuePair<string, object> kv in Obj(root, "stats"))
            {
                if (Enum.TryParse(kv.Key, out StatKey key)) s.Stats[key] = Math.Max(0L, AsLong(kv.Value, 0));
            }
            CopyInts(root, "winsByMode", s.WinsByMode);
            CopyInts(root, "lossesByMode", s.LossesByMode);
            foreach (KeyValuePair<string, object> kv in Obj(root, "archerMatches")) s.ArcherMatches[kv.Key] = Math.Max(0, AsInt(kv.Value, 0));

            Dictionary<string, object> d = Obj(root, "daily");
            s.Daily.LastSolvedDay = Int(d, "lastSolvedDay", -1);
            s.Daily.Streak = Math.Max(0, Int(d, "streak", 0));
            s.Daily.BestStreak = Math.Max(0, Int(d, "bestStreak", 0));
            s.Daily.StreakChests = Math.Max(0, Int(d, "streakChests", 0));

            if (root.TryGetValue("chestsOpened", out object chests) && chests is List<object> cl)
            {
                foreach (object c in cl) s.ChestsOpened.Add(AsInt(c, 0));
            }

            Dictionary<string, object> st = Obj(root, "settings");
            s.Settings.Music = Clamp(Int(st, "music", 70), 0, 100);
            s.Settings.Sfx = Clamp(Int(st, "sfx", 80), 0, 100);
            s.Settings.Haptics = Bool(st, "haptics", true);
            s.Settings.Theme = Str(st, "theme", "system");
            s.Settings.Language = Str(st, "language", "en");
            s.Settings.LeftHanded = Bool(st, "leftHanded", false);
            s.Settings.BiggerTargets = Bool(st, "biggerTargets", false);
            s.Settings.AimSensitivity = Math.Max(0.5, Math.Min(1.5, Dbl(st, "aimSensitivity", 1.0)));
            s.Settings.TrajectoryAssist = Bool(st, "trajectoryAssist", false);
            s.Settings.ReduceMotion = Bool(st, "reduceMotion", false);
            s.Settings.BatterySaver = Bool(st, "batterySaver", false);
            s.Settings.ColorBlind = Bool(st, "colorBlind", false);

            s.SeenCards.AddRange(StrList(root, "seenCards"));
            EnsureDefaults(s);
            return s;
        }

        /// <summary>A brand-new save with the default archer and cosmetics owned.</summary>
        public static SaveData NewSave()
        {
            var s = new SaveData();
            EnsureDefaults(s);
            return s;
        }

        /// <summary>Upgrades older save layouts in place (v1 is the first; later versions add steps here).</summary>
        public static void Migrate(Dictionary<string, object> root)
        {
            int version = Int(root, "version", 1);
            if (version < 1) root["version"] = 1;
        }

        static void EnsureDefaults(SaveData s)
        {
            foreach (CosmeticDef c in CosmeticCatalog.All)
            {
                if (c.Source == CosmeticSource.Default && !s.OwnedCosmetics.Contains(c.Id)) s.OwnedCosmetics.Add(c.Id);
            }
            foreach (string archer in s.ArcherLevels.Keys)
            {
                if (!s.EquippedSkins.ContainsKey(archer)) s.EquippedSkins[archer] = CosmeticCatalog.DefaultSkinFor(archer);
            }
            if (!s.OwnedCosmetics.Contains(s.EquippedTrail)) s.EquippedTrail = "trail_classic";
            if (s.PinnedBadges.Count > 3) s.PinnedBadges.RemoveRange(3, s.PinnedBadges.Count - 3);
        }

        // ---- helpers

        static List<object> Strings(List<string> list)
        {
            var o = new List<object>(list.Count);
            foreach (string s in list) o.Add(s);
            return o;
        }

        static List<object> Ints(int[] list)
        {
            var o = new List<object>(list.Length);
            foreach (int i in list) o.Add(i);
            return o;
        }

        static Dictionary<string, object> Obj(Dictionary<string, object> d, string key)
        {
            return d != null && d.TryGetValue(key, out object v) && v is Dictionary<string, object> o ? o : new Dictionary<string, object>();
        }

        static List<string> StrList(Dictionary<string, object> d, string key)
        {
            var list = new List<string>();
            if (d.TryGetValue(key, out object v) && v is List<object> l)
            {
                foreach (object o in l)
                {
                    if (o is string s && !list.Contains(s)) list.Add(s);
                }
            }
            return list;
        }

        static void CopyInts(Dictionary<string, object> d, string key, int[] into)
        {
            if (!d.TryGetValue(key, out object v) || !(v is List<object> l)) return;
            for (int i = 0; i < into.Length && i < l.Count; i++) into[i] = Math.Max(0, AsInt(l[i], 0));
        }

        static string Str(Dictionary<string, object> d, string key, string def) => d.TryGetValue(key, out object v) && v is string s ? s : def;
        static bool Bool(Dictionary<string, object> d, string key, bool def) => d.TryGetValue(key, out object v) && v is bool b ? b : def;
        static int Int(Dictionary<string, object> d, string key, int def) => d.TryGetValue(key, out object v) ? AsInt(v, def) : def;
        static long Long(Dictionary<string, object> d, string key, long def) => d.TryGetValue(key, out object v) ? AsLong(v, def) : def;
        static double Dbl(Dictionary<string, object> d, string key, double def) => d.TryGetValue(key, out object v) && v is double x ? x : def;
        static int AsInt(object v, int def) => v is double d && d >= int.MinValue && d <= int.MaxValue ? (int)d : def;
        static long AsLong(object v, long def) => v is double d && d >= long.MinValue && d <= long.MaxValue ? (long)d : def;
        static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
    }
}
