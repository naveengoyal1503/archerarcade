using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class BoosterTests
    {
        static MatchSetup WithBoosters(BoosterKind[] player, BoosterKind[] enemy = null)
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm);
            s.Fighters[0].Boosters = player;
            s.Fighters[1].Boosters = enemy ?? new BoosterKind[0];
            return s;
        }

        [Test]
        public void ExtraHeartAddsTwentyFiveMaxHp()
        {
            var m = new MatchState(WithBoosters(new[] { BoosterKind.ExtraHeart }));
            Assert.AreEqual(125, m.GetFighter(0).MaxHp);
            Assert.AreEqual(125, m.GetFighter(0).Hp);
            Assert.AreEqual(100, m.GetFighter(1).MaxHp);
        }

        [Test]
        public void ShieldBubbleAbsorbsOnlyTheFirstArrow()
        {
            var m = new MatchState(WithBoosters(new BoosterKind[0], new[] { BoosterKind.ShieldBubble }));
            m.ApplyShot(TestArena.Aim(m, HitZone.Body));
            Assert.AreEqual(100, m.GetFighter(1).Hp);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Aim(m, HitZone.Body));
            Assert.AreEqual(75, m.GetFighter(1).Hp);
        }

        [Test]
        public void MultiArrowFansOnlyTheFirstShot()
        {
            var m = new MatchState(WithBoosters(new[] { BoosterKind.MultiArrow }));
            ShotResult first = m.ApplyShot(TestArena.Aim(m, HitZone.Body));
            Assert.AreEqual(3, first.Arrows.Count);
            Assert.IsFalse(m.GetFighter(0).MultiArrowLeft);
            m.ApplyShot(TestArena.Miss());
            ShotResult second = m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(1, second.Arrows.Count);
        }

        [Test]
        public void MultiArrowDoesNotStackWithTripleShot()
        {
            var m = new MatchState(WithBoosters(new[] { BoosterKind.MultiArrow }));
            m.GetFighter(0).AbilityCharge = 3;
            ShotResult r = m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Normal, true));
            Assert.AreEqual(3, r.Arrows.Count);
            foreach (MatchEvent e in m.Events)
            {
                if (e.Kind == MatchEventKind.Hit) Assert.AreEqual(15, e.Amount, "60 % once, not 36 %");
            }
        }

        [Test]
        public void IronHelmetTurnsTheFirstHeadshotIntoABodyHit()
        {
            var m = new MatchState(WithBoosters(new BoosterKind[0], new[] { BoosterKind.IronHelmet }));
            m.ApplyShot(TestArena.Aim(m, HitZone.Head));
            Assert.AreEqual(75, m.GetFighter(1).Hp);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Aim(m, HitZone.Head));
            Assert.AreEqual(25, m.GetFighter(1).Hp, "second headshot is a real one");
        }

        [Test]
        public void HelmetSavedHeadshotDoesNotSpeedUpTheAbility()
        {
            var m = new MatchState(WithBoosters(new BoosterKind[0], new[] { BoosterKind.IronHelmet }));
            m.ApplyShot(TestArena.Aim(m, HitZone.Head));
            Assert.AreEqual(3, m.GetFighter(0).AbilityChargeNeeded);
        }

        [Test]
        public void DuplicateBoostersApplyOnce()
        {
            var m = new MatchState(WithBoosters(new[] { BoosterKind.ExtraHeart, BoosterKind.ExtraHeart }));
            Assert.AreEqual(125, m.GetFighter(0).MaxHp);
        }
    }
}
