namespace ArcherArcade.Logic.Meta
{
    /// <summary>Earned coins. The balance can never go negative and there is no way to add coins except earning.</summary>
    public sealed class Wallet
    {
        public Wallet(int coins = 0, long totalEarned = 0)
        {
            Coins = coins < 0 ? 0 : coins;
            TotalEarned = totalEarned < 0 ? 0 : totalEarned;
        }

        public int Coins { get; private set; }

        /// <summary>All coins ever earned (Coin Keeper badge).</summary>
        public long TotalEarned { get; private set; }

        public void Earn(int amount)
        {
            if (amount <= 0) return;
            Coins = Coins > int.MaxValue - amount ? int.MaxValue : Coins + amount;
            TotalEarned += amount;
        }

        public bool CanAfford(int cost) => cost >= 0 && Coins >= cost;

        /// <summary>Spends if affordable; returns false and changes nothing otherwise.</summary>
        public bool Spend(int cost)
        {
            if (!CanAfford(cost)) return false;
            Coins -= cost;
            return true;
        }

        /// <summary>How many coins are missing for a price (0 if affordable) — for the "Not enough coins" modal.</summary>
        public int Missing(int cost) => cost > Coins ? cost - Coins : 0;
    }
}
