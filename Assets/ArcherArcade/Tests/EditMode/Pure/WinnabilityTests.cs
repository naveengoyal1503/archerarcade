using ArcherArcade.Logic.Campaign;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    /// <summary>
    /// PROGRESS Phase 7/8: "every level winnable without boosters". A perfect-aim Ranger with the progression a
    /// player has at that point (upgrades from earned coins, tips unlocked so far) and no boosters must beat every
    /// level against its real computer opponents on most seeds.
    /// </summary>
    public class WinnabilityTests
    {
        [Test]
        public void EveryLevelIsWinnableWithoutBoosters([Range(1, 20)] int number)
        {
            LevelDef level = WorldOne.Level(number);
            const int seeds = 8;
            int wins = 0;
            string log = "";
            for (ulong seed = 1; seed <= seeds; seed++)
            {
                LevelResult r = PerfectPlayer.Play(WorldOne.Level(number), seed);
                if (r.Won) wins++;
                log += (r.Won ? "W" : "L") + r.PlayerTurns + " ";
            }
            TestContext.WriteLine("level " + number + " (" + level.Name + "): " + wins + "/" + seeds + "  " + log);
            Assert.GreaterOrEqual(wins, seeds * 3 / 4, "level " + number + " (" + level.Name + ") too hard: " + log);
        }
    }
}
