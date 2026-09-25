namespace ArcherArcade.Logic.Campaign
{
    /// <summary>Turns a <see cref="LevelDef"/> plus the player's match entry into a <see cref="MatchSetup"/>.</summary>
    public static class LevelBuilder
    {
        public static MatchSetup Build(LevelDef level, FighterSpec player, ulong seed)
        {
            var arena = new LevelArena(level.Opponents);
            level.BuildArena?.Invoke(arena);

            var setup = new MatchSetup
            {
                Seed = seed,
                Arena = arena.Layout,
                Wind = level.Wind,
                FirstTurn = FirstTurnRule.SideZero
            };
            setup.Props.AddRange(arena.Props);

            player.Side = 0;
            player.Feet = arena.PlayerFeet;
            player.Facing = 1;
            player.StandOnProp = arena.PlayerStandOnProp;
            player.StandOnTower = -1;
            if (level.ForcedTips != null) player.Tips = level.ForcedTips;
            setup.Fighters.Add(player);

            for (int i = 0; i < level.Opponents.Length; i++)
            {
                OpponentSpec o = level.Opponents[i];
                setup.Fighters.Add(new FighterSpec
                {
                    Def = EnemyTable.ById(o.EnemyId, o.Hp),
                    Side = 1,
                    Feet = o.Feet,
                    Facing = -1,
                    StandOnTower = o.StandOnTower,
                    StandOnProp = o.StandOnProp
                });
            }
            return setup;
        }

        /// <summary>A level-1 Ranger with no special tips or boosters (used by tests and validation).</summary>
        public static FighterSpec DefaultPlayer() => new FighterSpec { Def = ArcherTable.Ranger(), Level = 1 };
    }
}
