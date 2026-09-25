namespace ArcherArcade.UI
{
    /// <summary>Type roles from DESIGN_TOKENS §4: display Fredoka, body Nunito, and the icon font.</summary>
    public enum FontRole
    {
        /// <summary>Fredoka 700: titles, buttons, numbers.</summary>
        Display,
        /// <summary>Fredoka 600.</summary>
        DisplaySemi,
        /// <summary>Nunito 800: body, captions, labels.</summary>
        Body,
        /// <summary>Nunito 700: longer help text.</summary>
        BodyBold,
        /// <summary>Nunito 900.</summary>
        BodyBlack,
        /// <summary>Material Symbols Rounded glyphs (Icons.cs).</summary>
        Icon
    }
}
