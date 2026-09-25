using System.Collections.Generic;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Logic.Save;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class BadgeAndStatsTests
    {
        static Profile NewProfile() => new Profile(SaveCodec.NewSave(), new EconomyConfig());

        [Test]
        public void TwentyTwoBadgesWithTheDesignThresholds()
        {
            IReadOnlyList<BadgeDef> all = BadgeCatalog.All;
            Assert.AreEqual(22, all.Count);
            var ids = new HashSet<string>();
            foreach (BadgeDef b in all)
            {
                Assert.IsTrue(ids.Add(b.Id), b.Id);
                Assert.Less(b.Bronze, b.Silver + (b.GoldIsBossThreeStars ? 1 : 0), b.Id);
                Assert.LessOrEqual(b.Silver, b.Gold, b.Id);
            }
            BadgeDef s = BadgeCatalog.ById("sharpshooter");
            Assert.AreEqual(new long[] { 10, 50, 200 }, new[] { s.Bronze, s.Silver, s.Gold });
            BadgeDef coin = BadgeCatalog.ById("coin_keeper");
            Assert.AreEqual(new long[] { 1000, 5000, 20000 }, new[] { coin.Bronze, coin.Silver, coin.Gold });
            BadgeDef hero = BadgeCatalog.ById("forest_hero");
            Assert.AreEqual(new long[] { 20, 40, 60 }, new[] { hero.Bronze, hero.Silver, hero.Gold });
        }

        [Test]
        public void EarningATierPaysCoinsOnce()
        {
            Profile p = NewProfile();
            p.Data.AddStat(StatKey.Headshots, 12);
            List<BadgeUpdate> ups = p.EvaluateBadges();
            BadgeUpdate u = ups.Find(x => x.Badge.Id == "sharpshooter");
            Assert.AreEqual(BadgeTier.Bronze, u.Tier);
            Assert.AreEqual(30, u.Coins);
            Assert.AreEqual(BadgeTier.Bronze, p.BadgeTierOf("sharpshooter"));
            Assert.IsFalse(p.EvaluateBadges().Exists(x => x.Badge.Id == "sharpshooter"), "no double pay");
            p.Data.AddStat(StatKey.Headshots, 300);
            List<BadgeUpdate> jump = p.EvaluateBadges().FindAll(x => x.Badge.Id == "sharpshooter");
            Assert.AreEqual(2, jump.Count, "silver and gold both paid");
            Assert.AreEqual(BadgeTier.Gold, p.BadgeTierOf("sharpshooter"));
        }

        [Test]
        public void WardenSlayerGoldNeedsThreeStarsOnTheBoss()
        {
            Profile p = NewProfile();
            p.Data.AddStat(StatKey.WardenWins, 5);
            p.EvaluateBadges();
            Assert.AreEqual(BadgeTier.Silver, p.BadgeTierOf("warden_slayer"));
            p.Data.WardenBestStars = 3;
            p.EvaluateBadges();
            Assert.AreEqual(BadgeTier.Gold, p.BadgeTierOf("warden_slayer"));
        }

        [Test]
        public void BadgesCanGrantCosmetics()
        {
            Profile p = NewProfile();
            p.Data.AddStat(StatKey.LightningStrikes, 50);
            List<BadgeUpdate> ups = p.EvaluateBadges();
            Assert.IsTrue(ups.Exists(x => x.CosmeticId == "skin_electric_neon"));
            Assert.IsTrue(p.Owns("skin_electric_neon"));
        }

        [Test]
        public void DerivedBadgesReadTheSave()
        {
            Profile p = NewProfile();
            p.Earn(1000);
            p.Data.ArcherLevels["fire"] = 1;
            p.EvaluateBadges();
            Assert.AreEqual(BadgeTier.Bronze, p.BadgeTierOf("coin_keeper"));
            Assert.AreEqual(BadgeTier.Bronze, p.BadgeTierOf("collector"));
            Assert.AreEqual(2, p.StatValue(StatKey.ArcherLevels));
        }

        [Test]
        public void PinUpToThreeEarnedBadges()
        {
            Profile p = NewProfile();
            Assert.IsFalse(p.TogglePin("sharpshooter"), "not earned yet");
            p.Data.AddStat(StatKey.Headshots, 10);
            p.Data.AddStat(StatKey.TargetHits, 25);
            p.Data.AddStat(StatKey.FlawlessWins, 1);
            p.Data.AddStat(StatKey.Comebacks, 1);
            p.EvaluateBadges();
            Assert.IsTrue(p.TogglePin("sharpshooter"));
            Assert.IsTrue(p.TogglePin("bullseye"));
            Assert.IsTrue(p.TogglePin("untouchable"));
            Assert.IsFalse(p.TogglePin("comeback"), "max 3");
            Assert.IsTrue(p.TogglePin("bullseye"), "unpin");
            Assert.IsTrue(p.TogglePin("comeback"));
            Assert.AreEqual(3, p.Data.PinnedBadges.Count);
        }

        [Test]
        public void StatsRecorderCountsShots()
        {
            SaveData save = SaveCodec.NewSave();
            MatchSetup s = TestArena.Duel(36, WindRange.Fixed(4), 2, playerTips: new[] { ArrowTip.Fire, ArrowTip.Electric });
            var m = new MatchState(s);
            ShotResult r = m.ApplyShot(TestArena.Aim(m, HitZone.Head, ArrowTip.Fire));
            StatsRecorder.Record(save, m, m.Events, r, 0, 1.5);
            Assert.AreEqual(1, save.Stat(StatKey.Shots));
            Assert.AreEqual(1, save.Stat(StatKey.Hits));
            Assert.AreEqual(1, save.Stat(StatKey.Headshots));
            Assert.AreEqual(1, save.Stat(StatKey.StrongWindHits));
            Assert.AreEqual(1, save.Stat(StatKey.LongShotHits));
            Assert.AreEqual(1, save.Stat(StatKey.QuickDraws));
            Assert.AreEqual(6, save.Stat(StatKey.BurnDamage), "burn ticks on the foe at its turn start");
            Assert.Greater(save.LongestShot, 35.0);
            ShotResult foe = m.ApplyShot(TestArena.Miss());
            StatsRecorder.Record(save, m, m.Events, foe, 0, 5);
            Assert.AreEqual(1, save.Stat(StatKey.Shots), "the foe's shot is not ours");
            ShotResult again = m.ApplyShot(TestArena.Miss());
            StatsRecorder.Record(save, m, m.Events, again, 0, 5);
            Assert.AreEqual(2, save.Stat(StatKey.Shots));
            Assert.AreEqual(12, save.Stat(StatKey.BurnDamage), "second burn tick when the foe's next turn starts");
        }

        [Test]
        public void MatchEndStatsAndFavouriteArcher()
        {
            Profile p = NewProfile();
            p.RecordMatchEnd(GameMode.QuickDuel, true, 1.0, true, "ranger");
            p.RecordMatchEnd(GameMode.QuickDuel, true, 0.1, false, "fire");
            p.RecordMatchEnd(GameMode.QuickDuel, false, 0, false, "fire");
            p.RecordMatchEnd(GameMode.TwoPlayer, true, 0.5, false, "fire");
            Assert.AreEqual(2, p.Data.WinsByMode[(int)GameMode.QuickDuel]);
            Assert.AreEqual(1, p.Data.LossesByMode[(int)GameMode.QuickDuel]);
            Assert.AreEqual(1, p.Data.Stat(StatKey.FlawlessWins));
            Assert.AreEqual(1, p.Data.Stat(StatKey.Comebacks));
            Assert.AreEqual(1, p.Data.Stat(StatKey.PvpMatches));
            Assert.AreEqual("fire", p.FavouriteArcher);
        }
    }
}
