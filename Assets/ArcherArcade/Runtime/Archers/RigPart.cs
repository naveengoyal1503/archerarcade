namespace ArcherArcade.Archers
{
    /// <summary>
    /// Rig pieces in draw order (back to front), named after the design roster's part groups
    /// (Design/Character Roster: ranger_upper_arm_back … ranger_hand_front). The back arm moves in front of the
    /// torso for some poses (<see cref="RigFrame.BackArmFront"/>).
    /// </summary>
    public enum RigPart
    {
        UpperArmBack,
        ForearmBack,
        HandBack,
        UpperLegBack,
        LowerLegBack,
        FootBack,
        Cape,
        Quiver,
        Torso,
        UpperLegFront,
        LowerLegFront,
        FootFront,
        Head,
        Gear,
        Bow,
        UpperArmFront,
        ForearmFront,
        HandFront
    }
}
