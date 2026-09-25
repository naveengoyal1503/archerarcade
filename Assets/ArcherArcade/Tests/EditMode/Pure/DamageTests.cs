using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class DamageTests
    {
        readonly DamageConfig _cfg = new DamageConfig();

        [Test]
        public void ZoneMultipliers()
        {
            ArcherDef r = ArcherTable.Ranger();
            Assert.AreEqual(50, Damage.Compute(25, r, 1, _cfg.ZoneMultiplier(HitZone.Head), Element.None, _cfg));
            Assert.AreEqual(25, Damage.Compute(25, r, 1, _cfg.ZoneMultiplier(HitZone.Body), Element.None, _cfg));
            Assert.AreEqual(15, Damage.Compute(25, r, 1, _cfg.ZoneMultiplier(HitZone.Legs), Element.None, _cfg));
            Assert.AreEqual(0, Damage.Compute(25, r, 1, _cfg.ZoneMultiplier(HitZone.None), Element.None, _cfg));
        }

        [Test]
        public void FourBodyHitsOrTwoHeadshotsKnockOutALevelOneArcher()
        {
            ArcherDef r = ArcherTable.Ranger();
            int hp = Damage.MaxHp(r, 1, _cfg);
            Assert.AreEqual(100, hp);
            Assert.AreEqual(hp, 4 * Damage.Compute(25, r, 1, 1.0, Element.None, _cfg));
            Assert.AreEqual(hp, 2 * Damage.Compute(25, r, 1, 2.0, Element.None, _cfg));
        }

        [Test]
        public void UpgradeLevelsAddFourPercentAndFourHp()
        {
            ArcherDef r = ArcherTable.Ranger();
            Assert.AreEqual(29, Damage.Compute(25, r, 5, 1.0, Element.None, _cfg)); // 25 × 1.16 = 29
            Assert.AreEqual(136, Damage.MaxHp(r, 10, _cfg));
        }

        [Test]
        public void OwnElementGivesTwentyPercent()
        {
            Assert.AreEqual(26, Damage.Compute(22, ArcherTable.FireArcher(), 1, 1.0, Element.Fire, _cfg)); // 26.4
            Assert.AreEqual(22, Damage.Compute(22, ArcherTable.Ranger(), 1, 1.0, Element.Fire, _cfg));
            Assert.AreEqual(21, Damage.Compute(22, ArcherTable.ElectricArcher(), 1, 1.0, Element.Fire, _cfg)); // 24/25 × 22 = 21.1
        }

        [Test]
        public void ArcherBaseDamageScalesTips()
        {
            Assert.AreEqual(24, Damage.Compute(25, ArcherTable.ElectricArcher(), 1, 1.0, Element.None, _cfg));
            Assert.AreEqual(22, Damage.Compute(25, ArcherTable.BombArcher(), 1, 1.0, Element.None, _cfg));
        }

        [Test]
        public void TinyHitsDoAtLeastOne()
        {
            Assert.AreEqual(1, Damage.Compute(0.4, ArcherTable.Ranger(), 1, 0.6, Element.None, _cfg));
        }

        [Test]
        public void StatusRefreshesInsteadOfStacking()
        {
            var s = new StatusEffects();
            s.AddBurn(6, 2);
            s.AddBurn(6, 2);
            int burn, poison;
            s.TickTurnStart(out burn, out poison);
            Assert.AreEqual(6, burn);
            s.TickTurnStart(out burn, out poison);
            Assert.AreEqual(6, burn);
            s.TickTurnStart(out burn, out poison);
            Assert.AreEqual(0, burn);
            Assert.IsFalse(s.IsBurning);
        }
    }
}
