namespace ArcherArcade.Logic
{
    /// <summary>
    /// Everything a player decides for one shot. The same ShotInput + match seed always gives the same result,
    /// which is what AI tests, replays and future online play are built on.
    /// </summary>
    public struct ShotInput
    {
        /// <summary>Degrees above horizontal, measured toward the shooter's facing direction.</summary>
        public double AngleDeg;

        /// <summary>0..1 (already eased; see Ballistics.PowerFromDrag).</summary>
        public double Power;

        public ArrowTip Tip;
        public bool UseAbility;

        public ShotInput(double angleDeg, double power, ArrowTip tip = ArrowTip.Normal, bool useAbility = false)
        {
            AngleDeg = angleDeg;
            Power = power;
            Tip = tip;
            UseAbility = useAbility;
        }
    }
}
