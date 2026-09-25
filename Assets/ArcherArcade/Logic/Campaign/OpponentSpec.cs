namespace ArcherArcade.Logic.Campaign
{
    /// <summary>One enemy in a level: who, how smart, where.</summary>
    public sealed class OpponentSpec
    {
        public string EnemyId = "bandit";

        /// <summary>HP override (0 = the enemy type's default).</summary>
        public int Hp;

        /// <summary>AI profile id: easy / medium / hard / boss.</summary>
        public string Ai = "easy";

        /// <summary>Where it stands (set by the level's arena builder).</summary>
        public Vec2 Feet;

        public int StandOnTower = -1;
        public int StandOnProp = -1;

        public static OpponentSpec Of(string enemyId, string ai, int hp = 0) => new OpponentSpec { EnemyId = enemyId, Ai = ai, Hp = hp };

        public AiProfile Profile()
        {
            switch (Ai)
            {
                case "medium": return AiProfile.Medium();
                case "hard": return AiProfile.Hard();
                case "boss": return AiProfile.Boss();
                default: return AiProfile.Easy();
            }
        }
    }
}
