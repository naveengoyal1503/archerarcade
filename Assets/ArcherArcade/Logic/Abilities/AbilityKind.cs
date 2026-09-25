namespace ArcherArcade.Logic
{
    /// <summary>Archer abilities (GAME_DESIGN §4) and boss moves (§6.4).</summary>
    public enum AbilityKind
    {
        None = 0,
        TripleShot = 1,
        MeteorArrow = 2,
        StormBolt = 3,
        ClusterBomb = 4,

        /// <summary>Captain Thorn (mini-boss): a wide volley of thorn arrows.</summary>
        ThornVolley = 5,

        /// <summary>Forest Warden (boss): arrows fall on the opponent from above.</summary>
        RainOfLeaves = 6
    }
}
