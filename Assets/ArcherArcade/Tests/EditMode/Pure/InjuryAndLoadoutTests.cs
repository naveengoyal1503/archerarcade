using ArcherArcade.Logic;
using ArcherArcade.Logic.Meta;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class InjuryAndLoadoutTests
    {
        [TestCase(100, InjuryStage.Clean)]
        [TestCase(76, InjuryStage.Clean)]
        [TestCase(75, InjuryStage.Scratched)]
        [TestCase(51, InjuryStage.Scratched)]
        [TestCase(50, InjuryStage.Limping)]
        [TestCase(26, InjuryStage.Limping)]
        [TestCase(25, InjuryStage.Dizzy)]
        [TestCase(1, InjuryStage.Dizzy)]
        [TestCase(0, InjuryStage.KnockedOut)]
        public void InjuryStageFollowsHp(int hp, InjuryStage stage)
        {
            Assert.AreEqual(stage, Injury.Stage(hp, 100));
        }

        [Test]
        public void InjuryStageUsesTheShareOfMaxHp()
        {
            Assert.AreEqual(InjuryStage.Scratched, Injury.Stage(187, 250));
            Assert.AreEqual(InjuryStage.Clean, Injury.Stage(188, 250));
        }

        [Test]
        public void ElementHitsLeaveMarks()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Fire, ArrowTip.Ice }));
            m.ApplyShot(TestArena.Aim(m, HitZone.Legs, ArrowTip.Fire));
            Assert.AreEqual(ElementMarks.Soot, m.GetFighter(1).Marks);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Aim(m, HitZone.Legs, ArrowTip.Ice));
            Assert.AreEqual(ElementMarks.Soot | ElementMarks.Frost, m.GetFighter(1).Marks);
            Assert.AreEqual(ElementMarks.None, m.GetFighter(0).Marks);
        }

        [Test]
        public void TipUnlockLevelsMatchTheLevelTable()
        {
            Assert.AreEqual(0, TipUnlocks.LevelFor(ArrowTip.Normal));
            Assert.AreEqual(3, TipUnlocks.LevelFor(ArrowTip.Fire));
            Assert.AreEqual(7, TipUnlocks.LevelFor(ArrowTip.Electric));
            Assert.AreEqual(9, TipUnlocks.LevelFor(ArrowTip.Split));
            Assert.AreEqual(11, TipUnlocks.LevelFor(ArrowTip.Bomb));
            Assert.AreEqual(13, TipUnlocks.LevelFor(ArrowTip.Heavy));
            Assert.AreEqual(15, TipUnlocks.LevelFor(ArrowTip.Ice));
            Assert.AreEqual(18, TipUnlocks.LevelFor(ArrowTip.Poison));
            Assert.AreEqual(ArrowTip.Bomb, TipUnlocks.UnlockedBy(11));
            Assert.IsNull(TipUnlocks.UnlockedBy(12));
        }

        [Test]
        public void ArcherUnlockRules()
        {
            Assert.IsTrue(ArcherTable.Ranger().Unlock.IsMet(0, 0));
            Assert.IsFalse(ArcherTable.FireArcher().Unlock.IsMet(4, 60));
            Assert.IsTrue(ArcherTable.FireArcher().Unlock.IsMet(5, 0));
            Assert.IsFalse(ArcherTable.ElectricArcher().Unlock.IsMet(20, 19));
            Assert.IsTrue(ArcherTable.ElectricArcher().Unlock.IsMet(0, 20));
            Assert.IsFalse(ArcherTable.BombArcher().Unlock.IsMet(19, 60));
            Assert.IsTrue(ArcherTable.BombArcher().Unlock.IsMet(20, 0));
        }

        [Test]
        public void EveryHeroHasItsAbility()
        {
            Assert.AreEqual(AbilityKind.TripleShot, ArcherTable.Ranger().Ability);
            Assert.AreEqual(AbilityKind.MeteorArrow, ArcherTable.FireArcher().Ability);
            Assert.AreEqual(AbilityKind.StormBolt, ArcherTable.ElectricArcher().Ability);
            Assert.AreEqual(AbilityKind.ClusterBomb, ArcherTable.BombArcher().Ability);
            Assert.AreEqual(4, ArcherTable.Heroes().Length);
        }

        [Test]
        public void LoadoutAllowsThreeUnlockedSpecialTips()
        {
            var ok = new Loadout { Tips = new[] { ArrowTip.Fire, ArrowTip.Electric, ArrowTip.Split } };
            Assert.AreEqual(LoadoutError.None, LoadoutRules.Validate(ok, true, 9));
            Assert.AreEqual(LoadoutError.TipLocked, LoadoutRules.Validate(ok, true, 8));
            var four = new Loadout { Tips = new[] { ArrowTip.Fire, ArrowTip.Electric, ArrowTip.Split, ArrowTip.Bomb } };
            Assert.AreEqual(LoadoutError.TooManyTips, LoadoutRules.Validate(four, true, 20));
            var dup = new Loadout { Tips = new[] { ArrowTip.Fire, ArrowTip.Fire } };
            Assert.AreEqual(LoadoutError.DuplicateTip, LoadoutRules.Validate(dup, true, 20));
            var normal = new Loadout { Tips = new[] { ArrowTip.Normal } };
            Assert.AreEqual(LoadoutError.TipNotAllowed, LoadoutRules.Validate(normal, true, 20));
            Assert.AreEqual(LoadoutError.ArcherLocked, LoadoutRules.Validate(new Loadout(), false, 20));
        }

        [Test]
        public void LoadoutAllowsOneOfEachBooster()
        {
            var all = new Loadout
            {
                Boosters = new[] { BoosterKind.ShieldBubble, BoosterKind.MultiArrow, BoosterKind.IronHelmet, BoosterKind.ExtraHeart }
            };
            Assert.AreEqual(LoadoutError.None, LoadoutRules.Validate(all, true, 0));
            var dup = new Loadout { Boosters = new[] { BoosterKind.ExtraHeart, BoosterKind.ExtraHeart } };
            Assert.AreEqual(LoadoutError.DuplicateBooster, LoadoutRules.Validate(dup, true, 0));
        }

        [Test]
        public void LoadoutBuildsTheMatchEntry()
        {
            var l = new Loadout { ArcherId = "fire", Tips = new[] { ArrowTip.Fire }, Boosters = new[] { BoosterKind.ExtraHeart } };
            FighterSpec spec = LoadoutRules.ToFighterSpec(l, 3, new Vec2(0, 0), 1);
            Assert.AreEqual("fire", spec.Def.Id);
            Assert.AreEqual(3, spec.Level);
            MatchSetup s = TestArena.Duel(20, WindRange.Calm);
            s.Fighters[0] = spec;
            var m = new MatchState(s);
            Assert.AreEqual(95 + 8 + 25, m.GetFighter(0).MaxHp);
            Assert.AreEqual(3, m.GetFighter(0).Ammo[(int)ArrowTip.Fire]);
        }
    }
}
