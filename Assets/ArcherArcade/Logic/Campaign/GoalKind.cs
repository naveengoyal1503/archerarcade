namespace ArcherArcade.Logic.Campaign
{
    /// <summary>Level goal types (GAME_DESIGN §7.1).</summary>
    public enum GoalKind
    {
        /// <summary>Knock out the opponent.</summary>
        Duel,

        /// <summary>Hit N targets (static, swinging, moving) within the arrow limit.</summary>
        Targets,

        /// <summary>Hit the apples on the friendly dummies' heads without touching a dummy.</summary>
        AppleShot,

        /// <summary>Defeat 2–3 enemy archers one after another; your HP carries over.</summary>
        Gauntlet,

        /// <summary>Shoot the rope to free a captured friend while an enemy shoots at you.</summary>
        Rescue,

        /// <summary>The targets are behind cover: use a bounce pad or break a crate.</summary>
        TrickShot,

        /// <summary>Boss fight with special rules (GAME_DESIGN §6.4).</summary>
        Boss
    }
}
