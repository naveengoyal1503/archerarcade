namespace ArcherArcade.Logic
{
    /// <summary>How hurt an archer looks (GAME_DESIGN §3.3.2). Cartoon only, never gore.</summary>
    public enum InjuryStage
    {
        /// <summary>100–76 % HP: clean, confident idle.</summary>
        Clean = 0,

        /// <summary>75–51 %: scratches, torn sleeve, band-aid.</summary>
        Scratched = 1,

        /// <summary>50–26 %: limp, cracked armor, sweat.</summary>
        Limping = 2,

        /// <summary>25–1 %: dizzy stars, bandaged head, slower draw.</summary>
        Dizzy = 3,

        /// <summary>0: poof, stars, sleeping Z's.</summary>
        KnockedOut = 4
    }
}
