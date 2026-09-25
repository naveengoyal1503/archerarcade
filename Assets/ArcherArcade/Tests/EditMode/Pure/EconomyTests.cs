using System.Collections.Generic;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Logic.Save;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class EconomyTests
    {
        static Profile NewProfile(int coins = 0)
        {
            var p = new Profile(SaveCodec.NewSave(), new EconomyConfig());
            p.Earn(coins);
            return p;
        }

        static LevelResult Win(int level, int stars, int headshots = 0) => new LevelResult
        {
            LevelNumber = level, Won = true, Stars = stars, Headshots = headshots, PlayerTurns = 3, ArrowsUsed = 3, HpFraction = 1
        };

        [Test]
        public void WalletNeverGoesNegative()
        {
            var w = new Wallet(50);
            Assert.IsFalse(w.Spend(60));
            Assert.AreEqual(50, w.Coins);
            Assert.AreEqual(10, w.Missing(60));
            Assert.IsTrue(w.Spend(50));
            Assert.AreEqual(0, w.Coins);
            Assert.IsFalse(w.Spend(-5));
            w.Earn(-10);
            Assert.AreEqual(0, w.Coins);
            Assert.AreEqual(0, new Wallet(-100).Coins);
        }

        [Test]
        public void CampaignRewardsFollowTheDesign()
        {
            var cfg = new EconomyConfig();
            LevelDef l5 = WorldOne.Level(5);
            CoinBreakdown first = Rewards.Campaign(l5, true, true, 0, 3, 2, cfg);
            Assert.AreEqual(30, first.Clear);
            Assert.AreEqual(30, first.Stars, "+10 per new star");
            Assert.AreEqual(6, first.Headshots);
            CoinBreakdown replay = Rewards.Campaign(l5, true, false, 2, 3, 9, cfg);
            Assert.AreEqual(15, replay.Clear, "replay pays 50 %");
            Assert.AreEqual(10, replay.Stars, "only the new star");
            Assert.AreEqual(15, replay.Headshots, "headshot coins cap at 15");
            Assert.AreEqual(0, Rewards.Campaign(l5, false, true, 0, 0, 3, cfg).Total, "a loss pays nothing in the campaign");
        }

        [Test]
        public void QuickDuelAndTwoPlayerRewards()
        {
            var cfg = new EconomyConfig();
            Assert.AreEqual(10, Rewards.QuickDuel("easy", true, 0, cfg).Total);
            Assert.AreEqual(15, Rewards.QuickDuel("medium", true, 0, cfg).Total);
            Assert.AreEqual(25, Rewards.QuickDuel("hard", true, 0, cfg).Total);
            Assert.AreEqual(3, Rewards.QuickDuel("hard", false, 0, cfg).Total);
            Assert.AreEqual(0, Rewards.TwoPlayer().Total, "2-Player gives no coins");
        }

        [Test]
        public void SurvivalRewardsPerWaveAreCapped()
        {
            var cfg = new EconomyConfig();
            Assert.AreEqual(0, Rewards.Survival(0, 0, cfg).Total, "no wave cleared, no coins");
            Assert.AreEqual(cfg.SurvivalCoinsPerWave * 3, Rewards.Survival(3, 0, cfg).Total);
            Assert.AreEqual(cfg.SurvivalCoinsCap, Rewards.Survival(99, 0, cfg).Clear, "long runs are capped");
            Assert.AreEqual(cfg.SurvivalCoinsCap + cfg.HeadshotCoinsCap, Rewards.Survival(99, 50, cfg).Total);
        }

        [Test]
        public void UpgradeCostsAndLevels()
        {
            var cfg = new EconomyConfig();
            Assert.AreEqual(new[] { 100, 150, 220, 300, 400, 520, 660, 820, 1000 }, cfg.UpgradeCosts);
            Assert.AreEqual(10, cfg.MaxArcherLevel);
            Profile p = NewProfile(5000);
            Assert.AreEqual(UpgradeResult.NotOwned, p.Upgrade("fire"));
            for (int level = 2; level <= 10; level++)
            {
                Assert.AreEqual(UpgradeResult.Done, p.Upgrade("ranger"));
                Assert.AreEqual(level, p.ArcherLevel("ranger"));
            }
            Assert.AreEqual(UpgradeResult.MaxLevel, p.Upgrade("ranger"));
            Assert.AreEqual(5000 - 4170, p.Coins);
            Profile poor = NewProfile(50);
            Assert.AreEqual(UpgradeResult.NotEnoughCoins, poor.Upgrade("ranger"));
            Assert.AreEqual(50, poor.Coins);
            Assert.AreEqual(1, poor.ArcherLevel("ranger"));
        }

        [Test]
        public void ClearingLevelsPaysUnlocksAndKeepsBestStars()
        {
            Profile p = NewProfile();
            for (int n = 1; n <= 4; n++) p.RecordLevel(WorldOne.Level(n), Win(n, 3));
            CampaignOutcome o = p.RecordLevel(WorldOne.Level(5), Win(5, 2, 1));
            Assert.IsTrue(o.FirstClear);
            Assert.AreEqual(30 + 20 + 3, o.Coins.Total);
            Assert.AreEqual(1, o.ChestReady);
            CollectionAssert.Contains(o.NewArchers, "fire");
            Assert.IsTrue(p.OwnsArcher("fire"));
            Assert.AreEqual(5, p.HighestLevelCleared);
            Assert.IsTrue(p.IsLevelUnlocked(6));
            Assert.IsFalse(p.IsLevelUnlocked(7));
            Assert.AreEqual(6, p.ContinueLevel);

            CampaignOutcome again = p.RecordLevel(WorldOne.Level(5), Win(5, 1));
            Assert.IsFalse(again.FirstClear);
            Assert.AreEqual(15, again.Coins.Total);
            Assert.AreEqual(2, p.Level(5).Stars, "best stars kept");
            Assert.AreEqual(0, again.ChestReady, "chest only once");
        }

        [Test]
        public void TipUnlocksComeFromClears()
        {
            Profile p = NewProfile();
            Assert.IsFalse(p.IsTipUnlocked(ArrowTip.Fire));
            p.RecordLevel(WorldOne.Level(1), Win(1, 1));
            p.RecordLevel(WorldOne.Level(2), Win(2, 1));
            CampaignOutcome o = p.RecordLevel(WorldOne.Level(3), Win(3, 1));
            Assert.AreEqual(ArrowTip.Fire, o.NewTip);
            Assert.IsTrue(p.IsTipUnlocked(ArrowTip.Fire));
            Assert.IsFalse(p.IsTipUnlocked(ArrowTip.Electric));
        }

        [Test]
        public void ElectricArcherUnlocksAtTwentyStars()
        {
            Profile p = NewProfile();
            for (int n = 1; n <= 6; n++) p.RecordLevel(WorldOne.Level(n), Win(n, 3));
            Assert.AreEqual(18, p.TotalStars);
            Assert.IsFalse(p.OwnsArcher("electric"));
            CampaignOutcome o = p.RecordLevel(WorldOne.Level(7), Win(7, 2));
            CollectionAssert.Contains(o.NewArchers, "electric");
        }

        [Test]
        public void LossesCountForTipsAndAssist()
        {
            Profile p = NewProfile();
            var loss = new LevelResult { LevelNumber = 2, Won = false };
            p.RecordLevel(WorldOne.Level(2), loss);
            CampaignOutcome o = p.RecordLevel(WorldOne.Level(2), loss);
            Assert.AreEqual(2, o.LossesInARow);
            Assert.IsTrue(LossHelp.ShowTip(o.LossesInARow));
            p.RecordLevel(WorldOne.Level(2), Win(2, 2));
            Assert.AreEqual(0, p.Level(2).LossesInARow);
            Assert.AreEqual(3, p.Level(2).Attempts);
        }

        [Test]
        public void ChestsAreFixedAndOpenOnce()
        {
            var cfg = new EconomyConfig();
            ChestContents a = Chests.Open("w1_chest1", cfg, new List<string>());
            ChestContents b = Chests.Open("w1_chest1", cfg, new List<string>());
            Assert.AreEqual(a.Coins, b.Coins);
            Assert.AreEqual(a.CosmeticId, b.CosmeticId);
            for (int i = 1; i <= 4; i++)
            {
                ChestContents c = Chests.Open(Chests.IdFor(1, i), cfg, new List<string>());
                Assert.That(c.Coins, Is.InRange(80, 140));
            }
            Profile p = NewProfile();
            Assert.IsFalse(p.ChestAvailable(1));
            for (int n = 1; n <= 5; n++) p.RecordLevel(WorldOne.Level(n), Win(n, 1));
            int before = p.Coins;
            ChestContents opened = p.OpenChest(1);
            Assert.AreEqual(before + opened.Coins, p.Coins);
            Assert.IsFalse(p.ChestAvailable(1));
            Assert.AreEqual(0, p.OpenChest(1).Coins);
        }

        [Test]
        public void ChestNeverGivesSomethingYouOwn()
        {
            var cfg = new EconomyConfig { ChestCosmeticChance = 1.0 };
            var owned = new List<string>();
            foreach (CosmeticDef c in CosmeticCatalog.ChestPool()) owned.Add(c.Id);
            Assert.IsNull(Chests.Open("w1_chest2", cfg, owned).CosmeticId);
            owned.RemoveAt(0);
            Assert.AreEqual(CosmeticCatalog.ChestPool()[0].Id, Chests.Open("w1_chest2", cfg, owned).CosmeticId);
        }

        [Test]
        public void ShopSellsOnlyShopItemsForEarnedCoins()
        {
            Profile p = NewProfile(250);
            Assert.AreEqual(BuyResult.NotEnoughCoins, p.Buy("skin_ranger_forest"));
            Assert.AreEqual(BuyResult.Done, p.Buy("trail_sparkle"));
            Assert.AreEqual(50, p.Coins);
            Assert.AreEqual(BuyResult.AlreadyOwned, p.Buy("trail_sparkle"));
            Assert.AreEqual(BuyResult.NotForSale, p.Buy("skin_electric_neon"));
            Assert.AreEqual(BuyResult.NotForSale, p.Buy("trail_rainbow"));
            Assert.IsTrue(p.Equip("trail_sparkle"));
            Assert.AreEqual("trail_sparkle", p.Data.EquippedTrail);
            Assert.IsFalse(p.Equip("trail_comet"), "not owned");
            foreach (CosmeticDef c in CosmeticCatalog.All)
            {
                if (c.Source == CosmeticSource.Shop)
                {
                    Assert.That(c.Price, c.Kind == CosmeticKind.Skin ? Is.InRange(300, 600) : Is.InRange(200, 400), c.Id);
                }
            }
        }

        [Test]
        public void BoostersCostCoinsAndAreOptional()
        {
            Profile p = NewProfile(300);
            var all = new[] { BoosterKind.ShieldBubble, BoosterKind.MultiArrow, BoosterKind.IronHelmet, BoosterKind.ExtraHeart };
            Assert.AreEqual(450, p.BoostersCost(all));
            Assert.IsFalse(p.BuyBoosters(all));
            Assert.AreEqual(300, p.Coins, "all or nothing");
            Assert.IsTrue(p.BuyBoosters(new[] { BoosterKind.IronHelmet, BoosterKind.IronHelmet }));
            Assert.AreEqual(220, p.Coins, "duplicates charged once");
            Assert.IsTrue(p.BuyBoosters(new BoosterKind[0]));
        }

        [Test]
        public void DailyStreakPaysOncePerDayAndChestsEveryWeek()
        {
            Profile p = NewProfile();
            DailyOutcome d1 = p.RecordDaily(100, true);
            Assert.AreEqual(50, d1.Coins);
            Assert.AreEqual(1, d1.Streak);
            Assert.AreEqual(0, p.RecordDaily(100, true).Coins, "once per day");
            for (int day = 101; day <= 105; day++) p.RecordDaily(day, true);
            DailyOutcome d7 = p.RecordDaily(106, true);
            Assert.AreEqual(7, d7.Streak);
            Assert.IsTrue(d7.StreakChest);
            Assert.AreEqual(350, d7.Coins);
            Assert.AreEqual("trail_rainbow", d7.CosmeticId);
            Assert.IsTrue(p.Owns("trail_rainbow"));
            DailyOutcome gap = p.RecordDaily(110, true);
            Assert.AreEqual(1, gap.Streak, "missed days restart the streak");
            Assert.AreEqual(7, p.Data.Daily.BestStreak);
            Assert.AreEqual(0, p.CurrentStreak(112));
        }
    }
}
