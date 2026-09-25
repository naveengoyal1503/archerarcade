namespace ArcherArcade.Archers
{
    /// <summary>
    /// Body proportions of one character in design units (140 units = 1 m at body scale 1), exported from
    /// aa-characters.js dims(): torso, limb lengths and widths, hand radius, bow half-height and depth.
    /// </summary>
    public sealed class RigDims
    {
        public float TW = 66f, TH = 96f;
        public float UA = 40f, FA = 36f, UL = 34f, LL = 30f;
        public float WUA = 24f, WFA = 22f, WUL = 30f, WLL = 27f;
        public float HR = 15f, BH = 90f, BD = 34f, K = 1f;
    }
}
