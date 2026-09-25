namespace ArcherArcade.Logic
{
    /// <summary>Arena props (GAME_DESIGN §10). Swinging / moving targets are Targets with a PropMotion.</summary>
    public enum PropKind
    {
        /// <summary>Solid, never breaks; arrows stick.</summary>
        Wall,

        /// <summary>Breaks after 2 hits or 1 explosive. In a tower, any hit knocks it off.</summary>
        Crate,

        /// <summary>Any hit explodes it (2.5 m, 35) and blows its tower apart.</summary>
        TntCrate,

        /// <summary>Any hit explodes it (2 m, 30).</summary>
        ExplosiveBarrel,

        /// <summary>Arrows bounce off at the mirror angle keeping 80 % speed.</summary>
        BouncePad,

        /// <summary>Goal target (static, swinging or moving).</summary>
        Target,

        /// <summary>Apple on the friendly dummy's head (Apple Shot).</summary>
        Apple,

        /// <summary>Friendly dummy: hitting it spoils the Apple Shot.</summary>
        Dummy,

        /// <summary>Cut by any hit (Rescue).</summary>
        Rope,

        /// <summary>Wooden shield carried by a fighter; absorbs; Heavy / explosions knock it down for a turn.</summary>
        Shield,

        /// <summary>Platform an archer stands on; moves between turns.</summary>
        Platform,

        /// <summary>Forest Warden's vine wall: grows in front of the opponent; 2 hits, fire or explosions burn it at once.</summary>
        VineWall
    }
}
