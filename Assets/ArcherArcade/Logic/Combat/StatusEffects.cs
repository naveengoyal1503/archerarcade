namespace ArcherArcade.Logic
{
    /// <summary>
    /// Status effects on one archer. Damage-over-time ticks at the start of the affected archer's turn.
    /// Re-applying an effect refreshes it (keeps the stronger value) instead of stacking.
    /// </summary>
    public struct StatusEffects
    {
        public int BurnPerTurn;
        public int BurnTurns;
        public int PoisonPerTurn;
        public int PoisonTurns;

        /// <summary>Seconds taken off the next turn timer (Electric stun).</summary>
        public double PendingStunSeconds;

        /// <summary>Slower draw on the next turn (Ice). Moves to <see cref="ActiveDrawSlow"/> when that turn starts.</summary>
        public double PendingDrawSlow;

        /// <summary>Draw slowdown for the current turn (read by visuals and the AI's think time).</summary>
        public double ActiveDrawSlow;

        public bool IsBurning => BurnTurns > 0;
        public bool IsPoisoned => PoisonTurns > 0;

        public void AddBurn(int perTurn, int turns)
        {
            if (perTurn <= 0 || turns <= 0) return;
            if (perTurn > BurnPerTurn) BurnPerTurn = perTurn;
            if (turns > BurnTurns) BurnTurns = turns;
        }

        public void AddPoison(int perTurn, int turns)
        {
            if (perTurn <= 0 || turns <= 0) return;
            if (perTurn > PoisonPerTurn) PoisonPerTurn = perTurn;
            if (turns > PoisonTurns) PoisonTurns = turns;
        }

        public void AddStun(double seconds)
        {
            if (seconds > PendingStunSeconds) PendingStunSeconds = seconds;
        }

        public void AddFreeze(double slow)
        {
            if (slow > PendingDrawSlow) PendingDrawSlow = slow;
        }

        /// <summary>Start of this archer's turn: returns burn and poison damage and counts both down.</summary>
        public void TickTurnStart(out int burnDamage, out int poisonDamage)
        {
            burnDamage = 0;
            poisonDamage = 0;
            if (BurnTurns > 0)
            {
                burnDamage = BurnPerTurn;
                BurnTurns--;
                if (BurnTurns == 0) BurnPerTurn = 0;
            }
            if (PoisonTurns > 0)
            {
                poisonDamage = PoisonPerTurn;
                PoisonTurns--;
                if (PoisonTurns == 0) PoisonPerTurn = 0;
            }
            ActiveDrawSlow = PendingDrawSlow;
            PendingDrawSlow = 0.0;
        }

        /// <summary>Takes the pending stun for this turn's timer.</summary>
        public double ConsumeStun()
        {
            double s = PendingStunSeconds;
            PendingStunSeconds = 0.0;
            return s;
        }

        public void EndTurn()
        {
            ActiveDrawSlow = 0.0;
        }
    }
}
