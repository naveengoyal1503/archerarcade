namespace ArcherArcade.Logic.Meta
{
    /// <summary>Coins from one result, itemised for the Victory screen.</summary>
    public struct CoinBreakdown
    {
        public int Clear;
        public int Stars;
        public int Headshots;
        public int Total => Clear + Stars + Headshots;
    }
}
