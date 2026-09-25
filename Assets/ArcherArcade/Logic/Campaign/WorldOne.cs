namespace ArcherArcade.Logic.Campaign
{
    /// <summary>
    /// World 1 "Whispering Forest": levels 1–20 exactly as LEVELS.md (goal, opponent, distance, wind, props, par,
    /// preview, rewards, unlocks, idea cards). Each call builds fresh objects, so callers may modify them.
    /// </summary>
    public static class WorldOne
    {
        public const int LevelCount = 20;

        public static LevelDef[] Levels()
        {
            var all = new LevelDef[LevelCount];
            for (int i = 0; i < LevelCount; i++) all[i] = Level(i + 1);
            return all;
        }

        public static LevelDef Level(int number)
        {
            switch (number)
            {
                case 1: return L01();
                case 2: return L02();
                case 3: return L03();
                case 4: return L04();
                case 5: return L05();
                case 6: return L06();
                case 7: return L07();
                case 8: return L08();
                case 9: return L09();
                case 10: return L10();
                case 11: return L11();
                case 12: return L12();
                case 13: return L13();
                case 14: return L14();
                case 15: return L15();
                case 16: return L16();
                case 17: return L17();
                case 18: return L18();
                case 19: return L19();
                case 20: return L20();
                default: throw new System.ArgumentOutOfRangeException(nameof(number), "World 1 has levels 1–20.");
            }
        }

        static OpponentSpec[] One(string id, string ai, int hp = 0) => new[] { OpponentSpec.Of(id, ai, hp) };

        // 1 · First Arrow · E · Targets: hit 3 static targets · 12–18 m · wind 0 · open meadow · Tutorial · par 4 · 60 %
        static LevelDef L01() => new LevelDef
        {
            Number = 1, Name = "First Arrow", Tier = LevelTier.Easy, Goal = GoalKind.Targets, TargetCount = 3,
            DistanceMin = 12, DistanceMax = 18, Wind = WindRange.Calm, Par = 4, PreviewShare = 0.60, RewardCoins = 20,
            Ideas = new[] { IdeaCard.Tutorial }, LossTip = "tip_drag_back",
            BuildArena = a =>
            {
                a.Island(15, 12);
                a.Target(12, 1.3);
                a.Target(15, 2.1);
                a.Target(18, 1.4);
            }
        };

        // 2 · Hello, Pip · E · Duel · Scout Pip (Easy, 60 HP) · 16 m · wind 0 · open meadow · Duels & HP · par 6 · 45 %
        static LevelDef L02() => new LevelDef
        {
            Number = 2, Name = "Hello, Pip", Tier = LevelTier.Easy, Goal = GoalKind.Duel, Opponents = One("scout_pip", "easy", 60),
            DistanceMin = 16, DistanceMax = 16, Wind = WindRange.Calm, Par = 6, PreviewShare = 0.45, RewardCoins = 20,
            Ideas = new[] { IdeaCard.DuelsAndHp }, LossTip = "tip_aim_body",
            BuildArena = a =>
            {
                a.Island(16, 6);
                a.Opponent(0, 16);
            }
        };

        // 3 · Behind the Fence · E · Duel · Scout Pip (Easy, 80 HP) · 18 m · wind 0–1 · wooden wall in the middle ·
        //     Walls + Fire tip unlocked · par 6 · 45 % · 20 + Fire tip
        static LevelDef L03() => new LevelDef
        {
            Number = 3, Name = "Behind the Fence", Tier = LevelTier.Easy, Goal = GoalKind.Duel, Opponents = One("scout_pip", "easy", 80),
            DistanceMin = 18, DistanceMax = 18, Wind = new WindRange(0, 1), Par = 6, PreviewShare = 0.45, RewardCoins = 20,
            UnlockTip = ArrowTip.Fire, Ideas = new[] { IdeaCard.Walls }, LossTip = "tip_arc_over",
            BuildArena = a =>
            {
                a.Island(9, 3);
                a.Wall(9, 0, 2.2);
                a.Island(18, 6);
                a.Opponent(0, 18);
            }
        };

        // 4 · Feel the Breeze · E · Targets: 4 targets · 15–25 m · wind 1 · open, leaves drifting · Wind · par 6 · 45 %
        static LevelDef L04() => new LevelDef
        {
            Number = 4, Name = "Feel the Breeze", Tier = LevelTier.Easy, Goal = GoalKind.Targets, TargetCount = 4,
            DistanceMin = 15, DistanceMax = 25, Wind = WindRange.Fixed(1), Par = 6, PreviewShare = 0.45, RewardCoins = 20,
            Ideas = new[] { IdeaCard.Wind }, LossTip = "tip_aim_wind",
            BuildArena = a =>
            {
                a.Island(20, 14);
                a.Target(15, 1.4);
                a.Target(18, 2.4);
                a.Target(21, 1.2);
                a.Target(25, 2.0);
            }
        };

        // 5 · Headhunter · M · Duel · Hunter Moss (Medium) · 20 m · wind 0–2 · low wall · Headshots · par 5 · 30 % ·
        //     30 + Chest 1 · unlocks Fire Archer
        static LevelDef L05() => new LevelDef
        {
            Number = 5, Name = "Headhunter", Tier = LevelTier.Medium, Goal = GoalKind.Duel, Opponents = One("hunter_moss", "medium"),
            DistanceMin = 20, DistanceMax = 20, Wind = new WindRange(0, 2), Par = 5, PreviewShare = 0.30, RewardCoins = 30,
            Chest = 1, UnlockArcherId = "fire", Ideas = new[] { IdeaCard.Headshots }, LossTip = "tip_headshot",
            BuildArena = a =>
            {
                a.Island(10, 3);
                a.Wall(10, 0, 1.4);
                a.Island(20, 6);
                a.Opponent(0, 20);
            }
        };

        // 6 · Crate Escape · E · Duel · Crossbow Scout (Easy) · 18 m · wind 0–1 · crate tower between the islands ·
        //     Crate towers · par 6 · 45 %
        static LevelDef L06() => new LevelDef
        {
            Number = 6, Name = "Crate Escape", Tier = LevelTier.Easy, Goal = GoalKind.Duel, Opponents = One("crossbow_scout", "easy"),
            DistanceMin = 18, DistanceMax = 18, Wind = new WindRange(0, 1), Par = 6, PreviewShare = 0.45, RewardCoins = 20,
            Ideas = new[] { IdeaCard.CrateTowers }, LossTip = "tip_knock_crates",
            BuildArena = a =>
            {
                a.Island(9, 3);
                a.Tower(0, 9, 0, 4);
                a.Island(18, 6);
                a.Opponent(0, 18);
            }
        };

        // 7 · Apple of My Eye · M · Apple Shot: hit 3 apples · friendly dummy · 14–22 m · wind 0–2 · dummy on stumps ·
        //     Apple Shot · par 4 · 30 % · 30 + Electric tip
        static LevelDef L07() => new LevelDef
        {
            Number = 7, Name = "Apple of My Eye", Tier = LevelTier.Medium, Goal = GoalKind.AppleShot, TargetCount = 3,
            DistanceMin = 14, DistanceMax = 22, Wind = new WindRange(0, 2), Par = 4, PreviewShare = 0.30, RewardCoins = 30,
            UnlockTip = ArrowTip.Electric, Ideas = new[] { IdeaCard.AppleShot }, LossTip = "tip_apple_high",
            BuildArena = a =>
            {
                a.Island(18, 12);
                for (int i = 0; i < 3; i++)
                {
                    double x = 14 + i * 4;
                    a.Wall(x, 0, 0.5, 0.3);
                    a.Dummy(x, 0.5);
                    a.Apple(x, 0.5);
                }
            }
        };

        // 8 · Swing Time · M · Targets: 4 swinging targets · 18–26 m · wind 0–2 · pendulum targets · par 6 · 30 %
        static LevelDef L08() => new LevelDef
        {
            Number = 8, Name = "Swing Time", Tier = LevelTier.Medium, Goal = GoalKind.Targets, TargetCount = 4,
            DistanceMin = 18, DistanceMax = 26, Wind = new WindRange(0, 2), Par = 6, PreviewShare = 0.30, RewardCoins = 30,
            Ideas = new[] { IdeaCard.SwingingTargets }, LossTip = "tip_time_swing",
            BuildArena = a =>
            {
                a.Island(22, 12);
                a.SwingTarget(18, 5.5, 2.5, 25, 2.4, 0);
                a.SwingTarget(21, 6.0, 2.8, 30, 2.8, 90);
                a.SwingTarget(24, 5.5, 2.4, 25, 3.2, 180);
                a.SwingTarget(26, 6.5, 3.0, 20, 2.6, 270);
            }
        };

        // 9 · Moss Returns · H · Duel · Hunter Moss (Hard) · 24 m · wind 1–3 · moving platform (enemy) · par 5 · 18 % ·
        //     45 + Split tip
        static LevelDef L09() => new LevelDef
        {
            Number = 9, Name = "Moss Returns", Tier = LevelTier.Hard, Goal = GoalKind.Duel, Opponents = One("hunter_moss", "hard"),
            DistanceMin = 24, DistanceMax = 24, Wind = new WindRange(1, 3), Par = 5, PreviewShare = 0.18, RewardCoins = 45,
            UnlockTip = ArrowTip.Split, Ideas = new[] { IdeaCard.MovingPlatforms }, LossTip = "tip_platform_wait",
            BuildArena = a =>
            {
                int platform = a.Platform(24, 0.0, 1.4, new Vec2(0, 0), new Vec2(0, 3), 4);
                a.Opponent(0, 24, 0.0);
                a.Opponents[0].StandOnProp = platform;
            }
        };

        // 10 · Captain Thorn · MB · Duel (mini-boss) · Captain Thorn (Hard, 160 HP, Thorn Volley) · 26 m · wind 1–3 ·
        //      stump fort + crate tower with a TNT crate · Mini-boss + TNT crates · par 7 · 18 % · 80 + Chest 2
        static LevelDef L10() => new LevelDef
        {
            Number = 10, Name = "Captain Thorn", Tier = LevelTier.MiniBoss, Goal = GoalKind.Duel,
            Opponents = One("captain_thorn", "hard", 160),
            DistanceMin = 26, DistanceMax = 26, Wind = new WindRange(1, 3), Par = 7, PreviewShare = 0.18, RewardCoins = 80,
            Chest = 2, Ideas = new[] { IdeaCard.MiniBoss, IdeaCard.TntCrates }, LossTip = "tip_blow_tnt",
            BuildArena = a =>
            {
                a.Island(26, 6, 1.2);
                a.Tower(0, 23.6, 1.2, 3, 1);
                a.Opponent(0, 26, 1.2);
            }
        };

        // 11 · Shield Up · E · Duel · Shield Bearer (Easy) · 20 m · wind 0–1 · shield blocks body shots; Bomb tip knocks it
        //      down · Shields & Bomb tips · par 6 · 45 % · 20 + Bomb tip (the level gives Bomb ×2)
        static LevelDef L11() => new LevelDef
        {
            Number = 11, Name = "Shield Up", Tier = LevelTier.Easy, Goal = GoalKind.Duel, Opponents = One("shield_bearer", "easy"),
            DistanceMin = 20, DistanceMax = 20, Wind = new WindRange(0, 1), Par = 6, PreviewShare = 0.45, RewardCoins = 20,
            UnlockTip = ArrowTip.Bomb, ForcedTips = new[] { ArrowTip.Bomb }, Ideas = new[] { IdeaCard.ShieldsAndBombs },
            LossTip = "tip_bomb_shield",
            BuildArena = a =>
            {
                a.Island(20, 6);
                a.Opponent(0, 20);
            }
        };

        // 12 · Boing! · M · Trick Shot: hit 2 hidden targets · 18–24 m · wind 0–2 · bounce pads, tall wall · par 4 · 30 %
        static LevelDef L12() => new LevelDef
        {
            Number = 12, Name = "Boing!", Tier = LevelTier.Medium, Goal = GoalKind.TrickShot, TargetCount = 2,
            DistanceMin = 18, DistanceMax = 24, Wind = new WindRange(0, 2), Par = 4, PreviewShare = 0.30, RewardCoins = 30,
            Ideas = new[] { IdeaCard.BouncePads }, LossTip = "tip_bounce",
            BuildArena = a =>
            {
                // Targets hide under a roof behind a tall wall; the only way in is a bounce off the pad wall beyond.
                a.Island(21, 20);
                a.Wall(14, 0, 5.0, 0.3);
                a.Add(PropSpec.Of(PropKind.Wall, Shape.Box(new Vec2(19.25, 3.4), new Vec2(5.25, 0.2))));
                a.Pad(28.5, 3.5, 0.2, 3.5);
                a.Target(19.5, 1.2);
                a.Target(22.5, 1.0);
            }
        };

        // 13 · Twig Twins · M · Gauntlet: 2 archers in a row · Twig Twins (Twin Shooters, Medium) · 20 / 24 m · wind 1–3 ·
        //      two stumps · Gauntlet + Heavy tip · par 9 · 30 % · 30 + Heavy tip
        static LevelDef L13() => new LevelDef
        {
            Number = 13, Name = "Twig Twins", Tier = LevelTier.Medium, Goal = GoalKind.Gauntlet,
            Opponents = new[] { OpponentSpec.Of("twig_twin", "medium"), OpponentSpec.Of("twig_twin", "medium") },
            DistanceMin = 20, DistanceMax = 24, Wind = new WindRange(1, 3), Par = 9, PreviewShare = 0.30, RewardCoins = 30,
            UnlockTip = ArrowTip.Heavy, Ideas = new[] { IdeaCard.Gauntlet }, LossTip = "tip_save_hp",
            BuildArena = a =>
            {
                a.Island(20, 3, 0.6);
                a.Island(24, 3, 0.6);
                a.Opponent(0, 20, 0.6);
                a.Opponent(1, 24, 0.6);
            }
        };

        // 14 · Cut the Rope · H · Rescue: cut 1 rope while an enemy shoots · Hunter Moss (Medium) · 22 m · wind 1–3 ·
        //      cage on a rope, enemy on a tower · Rescue · par 5 · 18 %
        static LevelDef L14() => new LevelDef
        {
            Number = 14, Name = "Cut the Rope", Tier = LevelTier.Hard, Goal = GoalKind.Rescue, TargetCount = 1,
            Opponents = One("hunter_moss", "medium"),
            DistanceMin = 22, DistanceMax = 22, Wind = new WindRange(1, 3), Par = 5, PreviewShare = 0.18, RewardCoins = 45,
            Ideas = new[] { IdeaCard.Rescue }, LossTip = "tip_cut_rope",
            BuildArena = a =>
            {
                a.Rope(12, 4.4, 7.5);
                a.Add(PropSpec.Of(PropKind.Wall, Shape.Box(new Vec2(12, 3.8), new Vec2(0.6, 0.6))));
                a.Island(22, 5);
                a.Tower(0, 22, 0, 3);
                a.Opponent(0, 22, 2.7);
                a.Opponents[0].StandOnTower = 0;
            }
        };

        // 15 · Kaboom Valley · M · Duel · Tower Sniper (Medium) on a crate tower · 24 m · wind 1–3 · explosive barrels +
        //      TNT tower under the sniper · Explosive barrels, topple the tower, Ice tip · par 5 · 30 % · 30 + Chest 3 + Ice
        static LevelDef L15() => new LevelDef
        {
            Number = 15, Name = "Kaboom Valley", Tier = LevelTier.Medium, Goal = GoalKind.Duel, Opponents = One("tower_sniper", "medium"),
            DistanceMin = 24, DistanceMax = 24, Wind = new WindRange(1, 3), Par = 5, PreviewShare = 0.30, RewardCoins = 30,
            Chest = 3, UnlockTip = ArrowTip.Ice, Ideas = new[] { IdeaCard.ExplosiveBarrels }, LossTip = "tip_topple_tower",
            BuildArena = a =>
            {
                a.Island(24, 8);
                a.Tower(0, 24, 0, 4, 1);
                a.Barrel(21.3, 0);
                a.Barrel(26.6, 0);
                a.Opponent(0, 24, 3.6);
                a.Opponents[0].StandOnTower = 0;
            }
        };

        // 16 · Dusk Duel · E · Duel · Healer Druid (Easy, heals 10 every 2 turns) · 20 m · wind 0–2 · fireflies, darker sky ·
        //      Healers · par 6 · 45 %
        static LevelDef L16() => new LevelDef
        {
            Number = 16, Name = "Dusk Duel", Tier = LevelTier.Easy, Goal = GoalKind.Duel, Opponents = One("healer_druid", "easy"),
            DistanceMin = 20, DistanceMax = 20, Wind = new WindRange(0, 2), Par = 6, PreviewShare = 0.45, RewardCoins = 20,
            Ideas = new[] { IdeaCard.Healers }, Time = TimeOfDay.Dusk, LossTip = "tip_finish_fast",
            BuildArena = a =>
            {
                a.Island(20, 6);
                a.Opponent(0, 20);
            }
        };

        // 17 · Split Decision · M · Targets: 3 targets behind cover · 20–28 m · wind 1–3 · cover rocks, targets clustered ·
        //      Split arrows · par 3 · 30 % (the level gives Split ×2)
        static LevelDef L17() => new LevelDef
        {
            Number = 17, Name = "Split Decision", Tier = LevelTier.Medium, Goal = GoalKind.Targets, TargetCount = 3,
            DistanceMin = 20, DistanceMax = 28, Wind = new WindRange(1, 3), Par = 3, PreviewShare = 0.30, RewardCoins = 30,
            ForcedTips = new[] { ArrowTip.Split }, Ideas = new[] { IdeaCard.SplitArrows }, Time = TimeOfDay.Dusk,
            LossTip = "tip_split_cluster",
            BuildArena = a =>
            {
                a.Island(24, 12);
                a.Wall(20.5, 0, 2.4, 0.4);
                a.Target(23.5, 1.0);
                a.Target(24.6, 1.9);
                a.Target(25.7, 1.1);
            }
        };

        // 18 · Bramble's Revenge · H · Duel · Ranger Bramble (Hard, Triple Shot + Bubble every 3 turns) · 28 m · wind 2–4 ·
        //      moving platforms both sides, crate tower · Shield bubbles + Poison tip · par 5 · 18 % · 45 + Poison tip
        static LevelDef L18() => new LevelDef
        {
            Number = 18, Name = "Bramble's Revenge", Tier = LevelTier.Hard, Goal = GoalKind.Duel,
            Opponents = One("ranger_bramble", "hard"),
            DistanceMin = 28, DistanceMax = 28, Wind = new WindRange(2, 4), Par = 5, PreviewShare = 0.18, RewardCoins = 45,
            UnlockTip = ArrowTip.Poison, Ideas = new[] { IdeaCard.ShieldBubbles }, Time = TimeOfDay.Dusk, LossTip = "tip_pop_bubble",
            BuildArena = a =>
            {
                a.PlayerStandOnProp = a.Platform(0, 0.0, 1.4, new Vec2(0, 0), new Vec2(0, 2.0), 4);
                a.Island(14, 3);
                a.Tower(0, 14, 0, 5);
                int enemyPlatform = a.Platform(28, 0.0, 1.4, new Vec2(0, 2.0), new Vec2(0, 0), 4);
                a.Opponent(0, 28, 0.0);
                a.Opponents[0].StandOnProp = enemyPlatform;
            }
        };

        // 19 · The Long Night · H · Gauntlet: 3 archers · Crossbow Scout (Medium), Healer Druid (Hard), Hunter Moss (Hard) ·
        //      22 / 26 / 30 m · wind 2–4 · night, bounce pad, barrels, TNT tower · par 12 · 18 % · 45
        static LevelDef L19() => new LevelDef
        {
            Number = 19, Name = "The Long Night", Tier = LevelTier.Hard, Goal = GoalKind.Gauntlet,
            Opponents = new[]
            {
                OpponentSpec.Of("crossbow_scout", "medium"), OpponentSpec.Of("healer_druid", "hard"), OpponentSpec.Of("hunter_moss", "hard")
            },
            DistanceMin = 22, DistanceMax = 30, Wind = new WindRange(2, 4), Par = 12, PreviewShare = 0.18, RewardCoins = 45,
            Time = TimeOfDay.Night, LossTip = "tip_save_hp",
            BuildArena = a =>
            {
                a.Island(12, 6);
                a.Pad(10.5, 0.1, 1.0, 0.1);
                a.Tower(0, 13.8, 0, 4, 0);
                a.Island(26, 12);
                a.Barrel(24, 0);
                a.Barrel(28.2, 0);
                a.Opponent(0, 22);
                a.Opponent(1, 26);
                a.Opponent(2, 30);
            }
        };

        // 20 · The Forest Warden · B · Boss (GAME_DESIGN §6.4) · Forest Warden (Boss, 250 HP) · 30 m · wind 2–5 ·
        //      stump fort, rotating shields, vine walls · Boss card · par 9 · 18 % · 150 + Chest 4 · unlocks Bomb Archer
        static LevelDef L20() => new LevelDef
        {
            Number = 20, Name = "The Forest Warden", Tier = LevelTier.Boss, Goal = GoalKind.Boss,
            Opponents = One("forest_warden", "boss", 250),
            DistanceMin = 30, DistanceMax = 30, Wind = new WindRange(2, 5), Par = 9, PreviewShare = 0.18, RewardCoins = 150,
            Chest = 4, UnlockArcherId = "bomb", Ideas = new[] { IdeaCard.Boss }, Time = TimeOfDay.Night, LossTip = "tip_weak_spot",
            BuildArena = a =>
            {
                a.Island(30, 6, 1.5);
                a.Opponent(0, 30, 1.5);
            }
        };
    }
}
