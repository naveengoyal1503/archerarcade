using System.Collections.Generic;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Logic.Save;

namespace ArcherArcade.Logic.Meta
{
    /// <summary>
    /// The player's progress and every rule that changes it: coins, campaign stars and unlocks, archers and
    /// upgrades, cosmetics, chests, the daily streak, stats and badges. Pure C# over <see cref="SaveData"/>, so all
    /// of it is unit-tested; the Runtime only saves the data after calls that change it.
    /// </summary>
    public sealed class Profile
    {
        public Profile(SaveData data, EconomyConfig economy)
        {
            Data = data ?? SaveCodec.NewSave();
            Economy = economy ?? new EconomyConfig();
        }

        public SaveData Data { get; }
        public EconomyConfig Economy { get; }

        // ------------------------------------------------------------------ coins

        public int Coins => Data.Coins;

        public void Earn(int amount)
        {
            if (amount <= 0) return;
            var w = new Wallet(Data.Coins, Data.CoinsEarned);
            w.Earn(amount);
            Data.Coins = w.Coins;
            Data.CoinsEarned = w.TotalEarned;
        }

        public bool Spend(int cost)
        {
            var w = new Wallet(Data.Coins, Data.CoinsEarned);
            if (!w.Spend(cost)) return false;
            Data.Coins = w.Coins;
            return true;
        }

        public int Missing(int cost) => cost > Data.Coins ? cost - Data.Coins : 0;

        // ------------------------------------------------------------------ campaign

        public LevelSave Level(int number) => Data.Level(LevelId(number));

        public static string LevelId(int number) => "w1_l" + number.ToString("00");

        public int HighestLevelCleared
        {
            get
            {
                int best = 0;
                for (int n = 1; n <= WorldOne.LevelCount; n++)
                {
                    if (Data.Levels.TryGetValue(LevelId(n), out LevelSave l) && l.Cleared) best = n;
                }
                return best;
            }
        }

        public int TotalStars
        {
            get
            {
                int total = 0;
                foreach (LevelSave l in Data.Levels.Values) total += l.Stars;
                return total;
            }
        }

        public bool IsLevelUnlocked(int number) => number <= 1 || (Data.Levels.TryGetValue(LevelId(number - 1), out LevelSave l) && l.Cleared);

        /// <summary>Next level to play from the big Play button (the first not cleared, or the last).</summary>
        public int ContinueLevel
        {
            get
            {
                for (int n = 1; n <= WorldOne.LevelCount; n++)
                {
                    if (!Level(n).Cleared) return n;
                }
                return WorldOne.LevelCount;
            }
        }

        public CampaignOutcome RecordLevel(LevelDef level, LevelResult result)
        {
            LevelSave l = Level(level.Number);
            var o = new CampaignOutcome { Won = result.Won, Stars = result.Stars, PreviousBestStars = l.Stars };
            l.Attempts++;
            if (!result.Won)
            {
                l.LossesInARow++;
                o.LossesInARow = l.LossesInARow;
                Data.LossesByMode[(int)GameMode.Campaign]++;
                return o;
            }

            o.FirstClear = !l.Cleared;
            o.Coins = Rewards.Campaign(level, true, o.FirstClear, l.Stars, result.Stars, result.Headshots, Economy);
            Earn(o.Coins.Total);
            l.Cleared = true;
            l.LossesInARow = 0;
            if (result.Stars > l.Stars) l.Stars = result.Stars;
            if (level.IsDuelGoal)
            {
                if (l.BestTurns == 0 || result.PlayerTurns < l.BestTurns) l.BestTurns = result.PlayerTurns;
            }
            else if (l.BestArrows == 0 || result.ArrowsUsed < l.BestArrows)
            {
                l.BestArrows = result.ArrowsUsed;
            }
            Data.WinsByMode[(int)GameMode.Campaign]++;

            if (o.FirstClear)
            {
                o.NewTip = level.UnlockTip;
                if (level.Chest > 0 && !Data.ChestsOpened.Contains(level.Chest)) o.ChestReady = level.Chest;
            }
            if (level.Goal == GoalKind.Boss)
            {
                Data.AddStat(StatKey.WardenWins, 1);
                if (result.Stars > Data.WardenBestStars) Data.WardenBestStars = result.Stars;
            }
            o.NewArchers.AddRange(GrantUnlockedArchers());
            o.Badges.AddRange(EvaluateBadges());
            return o;
        }

        // ------------------------------------------------------------------ chests

        public bool ChestAvailable(int index) =>
            index >= 1 && index <= Chests.Count && Level(index * 5).Cleared && !Data.ChestsOpened.Contains(index);

        public ChestContents OpenChest(int index)
        {
            if (!ChestAvailable(index)) return default;
            ChestContents c = Chests.Open(Chests.IdFor(1, index), Economy, Data.OwnedCosmetics);
            Data.ChestsOpened.Add(index);
            Earn(c.Coins);
            if (c.CosmeticId != null && !Data.OwnedCosmetics.Contains(c.CosmeticId)) Data.OwnedCosmetics.Add(c.CosmeticId);
            return c;
        }

        // ------------------------------------------------------------------ archers

        public bool OwnsArcher(string id) => Data.ArcherLevels.ContainsKey(id);

        public int ArcherLevel(string id) => Data.ArcherLevels.TryGetValue(id, out int l) ? l : 0;

        /// <summary>Gives every hero whose unlock rule is now met; returns the new ones.</summary>
        public List<string> GrantUnlockedArchers()
        {
            var granted = new List<string>();
            int cleared = HighestLevelCleared;
            int stars = TotalStars;
            foreach (ArcherDef hero in ArcherTable.Heroes())
            {
                if (OwnsArcher(hero.Id) || !hero.Unlock.IsMet(cleared, stars)) continue;
                Data.ArcherLevels[hero.Id] = 1;
                Data.EquippedSkins[hero.Id] = CosmeticCatalog.DefaultSkinFor(hero.Id);
                granted.Add(hero.Id);
            }
            return granted;
        }

        public int UpgradeCost(string id) => OwnsArcher(id) ? Economy.UpgradeCost(ArcherLevel(id)) : -1;

        public UpgradeResult Upgrade(string id)
        {
            if (!OwnsArcher(id)) return UpgradeResult.NotOwned;
            int cost = Economy.UpgradeCost(ArcherLevel(id));
            if (cost < 0) return UpgradeResult.MaxLevel;
            if (!Spend(cost)) return UpgradeResult.NotEnoughCoins;
            Data.ArcherLevels[id] = ArcherLevel(id) + 1;
            return UpgradeResult.Done;
        }

        public void EquipArcher(string id)
        {
            if (OwnsArcher(id)) Data.EquippedArcher = id;
        }

        // ------------------------------------------------------------------ tips, loadout, boosters

        public bool IsTipUnlocked(ArrowTip tip) => TipUnlocks.IsUnlocked(tip, HighestLevelCleared);

        public Loadout CurrentLoadout()
        {
            var tips = new List<ArrowTip>();
            foreach (ArrowTip t in Data.LoadoutTips)
            {
                if (IsTipUnlocked(t) && tips.Count < LoadoutRules.MaxSpecialTips) tips.Add(t);
            }
            string archer = Data.EquippedArcher;
            return new Loadout
            {
                ArcherId = archer,
                SkinId = Data.EquippedSkins.TryGetValue(archer, out string skin) ? skin : CosmeticCatalog.DefaultSkinFor(archer),
                TrailId = Data.EquippedTrail,
                Tips = tips.ToArray()
            };
        }

        public void SaveLoadoutTips(ArrowTip[] tips)
        {
            Data.LoadoutTips.Clear();
            foreach (ArrowTip t in tips)
            {
                if (t != ArrowTip.Normal && !Data.LoadoutTips.Contains(t)) Data.LoadoutTips.Add(t);
            }
        }

        public int BoostersCost(BoosterKind[] boosters)
        {
            int total = 0;
            var seen = new List<BoosterKind>();
            foreach (BoosterKind b in boosters)
            {
                if (seen.Contains(b)) continue;
                seen.Add(b);
                total += Economy.BoosterCost(b);
            }
            return total;
        }

        /// <summary>Pays for the chosen boosters when the match starts (all or nothing).</summary>
        public bool BuyBoosters(BoosterKind[] boosters) => Spend(BoostersCost(boosters));

        /// <summary>Match entry for the player from a loadout and the archer's upgrade level.</summary>
        public FighterSpec PlayerSpec(Loadout loadout) => LoadoutRules.ToFighterSpec(loadout, ArcherLevel(loadout.ArcherId), new Vec2(0, 0), 1);

        // ------------------------------------------------------------------ cosmetics

        public bool Owns(string cosmeticId) => Data.OwnedCosmetics.Contains(cosmeticId);

        public BuyResult Buy(string cosmeticId)
        {
            CosmeticDef c = CosmeticCatalog.ById(cosmeticId);
            if (c == null) return BuyResult.Unknown;
            if (Owns(cosmeticId)) return BuyResult.AlreadyOwned;
            if (c.Source != CosmeticSource.Shop || c.Price <= 0) return BuyResult.NotForSale;
            if (!Spend(c.Price)) return BuyResult.NotEnoughCoins;
            Data.OwnedCosmetics.Add(cosmeticId);
            return BuyResult.Done;
        }

        public bool Equip(string cosmeticId)
        {
            CosmeticDef c = CosmeticCatalog.ById(cosmeticId);
            if (c == null || !Owns(cosmeticId)) return false;
            if (c.Kind == CosmeticKind.Trail)
            {
                Data.EquippedTrail = cosmeticId;
                return true;
            }
            if (!OwnsArcher(c.ArcherId)) return false;
            Data.EquippedSkins[c.ArcherId] = cosmeticId;
            return true;
        }

        // ------------------------------------------------------------------ daily challenge

        /// <summary>Solving today's challenge: 50 coins once per day, streak +1 (or restart), chest every 7 days.</summary>
        public DailyOutcome RecordDaily(int day, bool solved)
        {
            var o = new DailyOutcome();
            DailySave d = Data.Daily;
            Data.WinsByMode[(int)GameMode.Daily] += solved ? 1 : 0;
            Data.LossesByMode[(int)GameMode.Daily] += solved ? 0 : 1;
            if (!solved || d.LastSolvedDay == day) return o;
            d.Streak = d.LastSolvedDay == day - 1 ? d.Streak + 1 : 1;
            d.LastSolvedDay = day;
            if (d.Streak > d.BestStreak) d.BestStreak = d.Streak;
            o.Coins = Economy.DailyChallenge;
            if (d.Streak % Economy.DailyStreakDays == 0)
            {
                o.StreakChest = true;
                o.Coins += Economy.DailyStreakChestCoins;
                d.StreakChests++;
                if (!Owns(Economy.DailyStreakChestTrail))
                {
                    Data.OwnedCosmetics.Add(Economy.DailyStreakChestTrail);
                    o.CosmeticId = Economy.DailyStreakChestTrail;
                }
            }
            o.Streak = d.Streak;
            Earn(o.Coins);
            o.Badges.AddRange(EvaluateBadges());
            return o;
        }

        /// <summary>The streak shown today (0 if a day was missed).</summary>
        public int CurrentStreak(int today)
        {
            DailySave d = Data.Daily;
            return d.LastSolvedDay >= today - 1 ? d.Streak : 0;
        }

        public bool DailySolved(int today) => Data.Daily.LastSolvedDay == today;

        // ------------------------------------------------------------------ stats, badges

        public long StatValue(StatKey key)
        {
            switch (key)
            {
                case StatKey.WorldOneStars: return TotalStars;
                case StatKey.DailyStreakBest: return Data.Daily.BestStreak;
                case StatKey.ArchersOwned: return Data.ArcherLevels.Count;
                case StatKey.ArcherLevels:
                {
                    long sum = 0;
                    foreach (int l in Data.ArcherLevels.Values) sum += l;
                    return sum;
                }
                case StatKey.CoinsEarned: return Data.CoinsEarned;
                default: return Data.Stat(key);
            }
        }

        public BadgeTier BadgeTierOf(string id) => Data.BadgeTiers.TryGetValue(id, out BadgeTier t) ? t : BadgeTier.None;

        public BadgeTier ComputeTier(BadgeDef b)
        {
            long v = StatValue(b.Stat);
            BadgeTier tier = BadgeTier.None;
            if (v >= b.Bronze) tier = BadgeTier.Bronze;
            if (v >= b.Silver) tier = BadgeTier.Silver;
            if (b.GoldIsBossThreeStars ? Data.WardenBestStars >= 3 && tier >= BadgeTier.Bronze : v >= b.Gold) tier = BadgeTier.Gold;
            return tier;
        }

        /// <summary>Progress toward the next tier (0..1) for the badge wall.</summary>
        public double BadgeProgress(BadgeDef b)
        {
            BadgeTier t = BadgeTierOf(b.Id);
            if (t == BadgeTier.Gold) return 1.0;
            long next = b.Threshold(t + 1);
            if (b.GoldIsBossThreeStars && t == BadgeTier.Silver) return Data.WardenBestStars / 3.0;
            return next <= 0 ? 1.0 : DetMath.Clamp01((double)StatValue(b.Stat) / next);
        }

        /// <summary>Raises badge tiers that are now earned; pays each new tier and grants badge cosmetics.</summary>
        public List<BadgeUpdate> EvaluateBadges()
        {
            var updates = new List<BadgeUpdate>();
            foreach (BadgeDef b in BadgeCatalog.All)
            {
                BadgeTier now = ComputeTier(b);
                BadgeTier had = BadgeTierOf(b.Id);
                for (BadgeTier t = had + 1; t <= now; t++)
                {
                    var u = new BadgeUpdate { Badge = b, Tier = t, Coins = Economy.BadgeReward(t) };
                    foreach (CosmeticDef c in CosmeticCatalog.All)
                    {
                        if (c.Source == CosmeticSource.Badge && c.BadgeId == b.Id && c.BadgeTier == t && !Owns(c.Id))
                        {
                            Data.OwnedCosmetics.Add(c.Id);
                            u.CosmeticId = c.Id;
                        }
                    }
                    Earn(u.Coins);
                    updates.Add(u);
                }
                if (now > had) Data.BadgeTiers[b.Id] = now;
            }
            return updates;
        }

        /// <summary>Pins or unpins a badge on the Home profile card (max 3, earned badges only).</summary>
        public bool TogglePin(string badgeId)
        {
            if (Data.PinnedBadges.Remove(badgeId)) return true;
            if (BadgeTierOf(badgeId) == BadgeTier.None || Data.PinnedBadges.Count >= 3) return false;
            Data.PinnedBadges.Add(badgeId);
            return true;
        }

        /// <summary>End of any match for the Stats screen and match badges.</summary>
        public List<BadgeUpdate> RecordMatchEnd(GameMode mode, bool won, double hpFraction, bool tookNoDamage, string archerId)
        {
            if (mode != GameMode.Campaign && mode != GameMode.Daily)
            {
                if (won) Data.WinsByMode[(int)mode]++;
                else Data.LossesByMode[(int)mode]++;
            }
            if (mode == GameMode.TwoPlayer)
            {
                Data.AddStat(StatKey.PvpMatches, 1);
            }
            else if (won)
            {
                if (tookNoDamage) Data.AddStat(StatKey.FlawlessWins, 1);
                if (hpFraction < 0.15) Data.AddStat(StatKey.Comebacks, 1);
            }
            if (!string.IsNullOrEmpty(archerId))
            {
                Data.ArcherMatches.TryGetValue(archerId, out int n);
                Data.ArcherMatches[archerId] = n + 1;
            }
            return EvaluateBadges();
        }

        /// <summary>End of a Survival run: best wave, wins/losses (a run counts as won when it cleared a wave).</summary>
        public List<BadgeUpdate> RecordSurvival(int wavesCleared, string archerId)
        {
            if (wavesCleared > Data.SurvivalBestWave) Data.SurvivalBestWave = wavesCleared;
            if (wavesCleared > 0) Data.WinsByMode[(int)GameMode.Survival]++;
            else Data.LossesByMode[(int)GameMode.Survival]++;
            if (!string.IsNullOrEmpty(archerId))
            {
                Data.ArcherMatches.TryGetValue(archerId, out int n);
                Data.ArcherMatches[archerId] = n + 1;
            }
            return EvaluateBadges();
        }

        public string FavouriteArcher
        {
            get
            {
                string best = Data.EquippedArcher;
                int most = -1;
                foreach (KeyValuePair<string, int> kv in Data.ArcherMatches)
                {
                    if (kv.Value > most)
                    {
                        most = kv.Value;
                        best = kv.Key;
                    }
                }
                return best;
            }
        }

        // ------------------------------------------------------------------ idea cards, tutorial

        public bool HasSeen(IdeaCard card) => Data.SeenCards.Contains(card.ToString());

        public void MarkSeen(IdeaCard card)
        {
            if (!HasSeen(card)) Data.SeenCards.Add(card.ToString());
        }
    }
}
