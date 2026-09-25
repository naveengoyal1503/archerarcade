using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Logic.Modes;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class SurvivalTests
    {
        static SurvivalRun Run(ulong seed) => new SurvivalRun(LevelBuilder.DefaultPlayer(), seed);

        [Test]
        public void WavesGetHarder()
        {
            Assert.AreEqual("easy", SurvivalRun.AiFor(1));
            Assert.AreEqual("medium", SurvivalRun.AiFor(4));
            Assert.AreEqual("hard", SurvivalRun.AiFor(8));
            Assert.AreEqual("boss", SurvivalRun.AiFor(12));
            Assert.Less(SurvivalRun.HpFor(1), SurvivalRun.HpFor(10));
            Assert.AreEqual(200, SurvivalRun.HpFor(40));
        }

        [Test]
        public void SameSeedSameWaves()
        {
            SurvivalRun a = Run(42), b = Run(42);
            for (int w = 0; w < 6; w++)
            {
                MatchState ma = a.StartWave(), mb = b.StartWave();
                Assert.AreEqual(a.Opponent.EnemyId, b.Opponent.EnemyId);
                Assert.AreEqual(a.ArenaId, b.ArenaId);
                Assert.AreEqual(ma.ComputeHash(), mb.ComputeHash());
            }
        }

        [Test]
        public void HpCarriesOverWithAHeal()
        {
            SurvivalRun run = Run(7);
            MatchState m = run.StartWave();
            Fighter me = m.GetFighter(0);
            int max = me.MaxHp;
            me.Hp = 30;
            m.EndByGoal(0);
            run.EndWave();
            Assert.IsFalse(run.IsOver);
            Assert.AreEqual(1, run.WavesCleared);
            MatchState next = run.StartWave();
            Assert.AreEqual(30 + SurvivalRun.HealBetweenWaves, next.GetFighter(0).Hp);
            Assert.AreEqual(max, next.GetFighter(0).MaxHp);
            Assert.AreEqual(2, run.Wave);
        }

        [Test]
        public void LosingEndsTheRun()
        {
            SurvivalRun run = Run(9);
            MatchState m = run.StartWave();
            m.EndByGoal(1);
            run.EndWave();
            Assert.IsTrue(run.IsOver);
            Assert.AreEqual(0, run.WavesCleared);
        }

        [Test]
        public void EveryEarlyWaveIsReachable()
        {
            SurvivalRun run = Run(3);
            for (int w = 0; w < 12; w++)
            {
                MatchState m = run.StartWave();
                Fighter me = m.GetFighter(0), foe = m.GetFighter(1);
                var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), 1, m.Setup.Body.ZoneCenter(HitZone.Body, foe), m.Setup.Wind.Max);
                AimSolution sol;
                Assert.IsTrue(AimSolver.SolveValidated(req, m.Setup.Shot, m.BuildWorld(), m.Setup.Arena, 0, foe.Index, HitZone.None, out sol),
                    "wave " + run.Wave + " " + run.ArenaId);
                m.EndByGoal(0);
                run.EndWave();
            }
        }
    }
}
