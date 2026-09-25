using ArcherArcade.Logic;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Logic.Save;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class SaveTests
    {
        [Test]
        public void NewSaveHasTheStarterAndDefaults()
        {
            SaveData s = SaveCodec.NewSave();
            Assert.AreEqual(SaveData.CurrentVersion, s.Version);
            Assert.AreEqual(1, s.ArcherLevels["ranger"]);
            Assert.AreEqual("ranger", s.EquippedArcher);
            CollectionAssert.Contains(s.OwnedCosmetics, "skin_ranger_default");
            CollectionAssert.Contains(s.OwnedCosmetics, "trail_classic");
            Assert.AreEqual("skin_ranger_default", s.EquippedSkins["ranger"]);
            Assert.AreEqual(70, s.Settings.Music);
            Assert.AreEqual(80, s.Settings.Sfx);
            Assert.IsTrue(s.Settings.Haptics);
        }

        [Test]
        public void RoundTripKeepsEverything()
        {
            var p = new Profile(SaveCodec.NewSave(), new EconomyConfig());
            p.Earn(1234);
            p.Data.ArcherLevels["fire"] = 4;
            p.Data.EquippedArcher = "fire";
            p.Data.EquippedSkins["fire"] = "skin_fire_ember";
            p.Data.OwnedCosmetics.Add("skin_fire_ember");
            p.SaveLoadoutTips(new[] { ArrowTip.Fire, ArrowTip.Heavy });
            p.Level(3).Cleared = true;
            p.Level(3).Stars = 2;
            p.Level(3).BestTurns = 4;
            p.Data.AddStat(StatKey.Headshots, 17);
            p.Data.BadgeTiers["sharpshooter"] = BadgeTier.Bronze;
            p.Data.PinnedBadges.Add("sharpshooter");
            p.Data.Daily.Streak = 3;
            p.Data.Daily.LastSolvedDay = 20001;
            p.Data.ChestsOpened.Add(1);
            p.Data.Settings.Language = "hinglish";
            p.Data.Settings.LeftHanded = true;
            p.Data.Settings.AimSensitivity = 1.25;
            p.Data.WinsByMode[1] = 5;
            p.Data.LongestShot = 37.5;
            p.Data.SeenCards.Add("Tutorial");
            p.Data.TutorialDone = true;
            p.Data.PlayerName = "Naveen \"N\"";

            string json = SaveCodec.ToJson(p.Data);
            SaveData r = SaveCodec.FromJson(json);
            Assert.AreEqual(1234, r.Coins);
            Assert.AreEqual(1234, r.CoinsEarned);
            Assert.AreEqual(4, r.ArcherLevels["fire"]);
            Assert.AreEqual("fire", r.EquippedArcher);
            Assert.AreEqual("skin_fire_ember", r.EquippedSkins["fire"]);
            CollectionAssert.AreEqual(new[] { ArrowTip.Fire, ArrowTip.Heavy }, r.LoadoutTips);
            Assert.IsTrue(r.Levels["w1_l03"].Cleared);
            Assert.AreEqual(2, r.Levels["w1_l03"].Stars);
            Assert.AreEqual(4, r.Levels["w1_l03"].BestTurns);
            Assert.AreEqual(17, r.Stat(StatKey.Headshots));
            Assert.AreEqual(BadgeTier.Bronze, r.BadgeTiers["sharpshooter"]);
            CollectionAssert.AreEqual(new[] { "sharpshooter" }, r.PinnedBadges);
            Assert.AreEqual(3, r.Daily.Streak);
            Assert.AreEqual(20001, r.Daily.LastSolvedDay);
            CollectionAssert.AreEqual(new[] { 1 }, r.ChestsOpened);
            Assert.AreEqual("hinglish", r.Settings.Language);
            Assert.IsTrue(r.Settings.LeftHanded);
            Assert.AreEqual(1.25, r.Settings.AimSensitivity, 1e-12);
            Assert.AreEqual(5, r.WinsByMode[1]);
            Assert.AreEqual(37.5, r.LongestShot, 1e-12);
            Assert.IsTrue(r.TutorialDone);
            Assert.AreEqual("Naveen \"N\"", r.PlayerName);
            Assert.AreEqual(json, SaveCodec.ToJson(r), "stable output");
        }

        [Test]
        public void MissingFieldsGetDefaults()
        {
            SaveData s = SaveCodec.FromJson("{\"version\": 1, \"coins\": 40}");
            Assert.AreEqual(40, s.Coins);
            Assert.AreEqual(1, s.ArcherLevels["ranger"]);
            Assert.AreEqual(70, s.Settings.Music);
            Assert.AreEqual(-1, s.Daily.LastSolvedDay);
            CollectionAssert.Contains(s.OwnedCosmetics, "trail_classic");
        }

        [Test]
        public void BrokenOrHostileValuesAreRepaired()
        {
            SaveData s = SaveCodec.FromJson(
                "{\"coins\": -500, \"archers\": {\"fire\": 99}, \"equippedArcher\": \"ghost\", \"settings\": {\"music\": 900, " +
                "\"aimSensitivity\": 9}, \"levels\": {\"w1_l01\": {\"stars\": 7}}, \"pinnedBadges\": [\"a\",\"b\",\"c\",\"d\"], " +
                "\"futureField\": {\"x\": [1,2,3]}}");
            Assert.AreEqual(0, s.Coins);
            Assert.AreEqual(10, s.ArcherLevels["fire"]);
            Assert.AreEqual(1, s.ArcherLevels["ranger"], "the starter is always owned");
            Assert.AreEqual("ranger", s.EquippedArcher);
            Assert.AreEqual(100, s.Settings.Music);
            Assert.AreEqual(1.5, s.Settings.AimSensitivity);
            Assert.AreEqual(3, s.Levels["w1_l01"].Stars);
            Assert.AreEqual(3, s.PinnedBadges.Count);
        }

        [Test]
        public void OldSavesWithFiveModesStillLoad()
        {
            SaveData s = SaveCodec.FromJson("{\"version\": 1, \"winsByMode\": [1,2,3,4,5], \"lossesByMode\": [0,1,0,1,0]}");
            Assert.AreEqual(6, s.WinsByMode.Length);
            Assert.AreEqual(5, s.WinsByMode[4]);
            Assert.AreEqual(0, s.WinsByMode[(int)GameMode.Survival]);
            Assert.AreEqual(0, s.SurvivalBestWave);
            var p = new Profile(s, new EconomyConfig());
            p.RecordSurvival(4, "ranger");
            Assert.AreEqual(4, p.Data.SurvivalBestWave);
            p.RecordSurvival(2, "ranger");
            Assert.AreEqual(4, p.Data.SurvivalBestWave, "best is kept");
            Assert.AreEqual(4, SaveCodec.FromJson(SaveCodec.ToJson(p.Data)).SurvivalBestWave);
        }

        [Test]
        public void GarbageGivesAFreshSave()
        {
            Assert.AreEqual(0, SaveCodec.FromJson("not json {").Coins);
            Assert.AreEqual(0, SaveCodec.FromJson("").Coins);
            Assert.AreEqual(0, SaveCodec.FromJson("[1,2]").Coins);
            Assert.AreEqual(0, SaveCodec.FromJson(null).Coins);
        }

        [Test]
        public void JsonHandlesUnicodeAndEscapes()
        {
            object v = Json.Read(Json.Write(new System.Collections.Generic.Dictionary<string, object>
            {
                { "name", "Maa ❤️ \\ \" \n tab\t" }, { "n", 2.5 }, { "big", 12345678901.0 }, { "neg", -3.0 }, { "b", false }, { "z", null }
            }));
            var d = (System.Collections.Generic.Dictionary<string, object>)v;
            Assert.AreEqual("Maa ❤️ \\ \" \n tab\t", d["name"]);
            Assert.AreEqual(2.5, d["n"]);
            Assert.AreEqual(12345678901.0, d["big"]);
            Assert.AreEqual(-3.0, d["neg"]);
            Assert.AreEqual(false, d["b"]);
            Assert.IsNull(d["z"]);
        }
    }
}
