using System.Collections.Generic;
using ArcherArcade.Logic.Save;

namespace ArcherArcade.Logic.Meta
{
    /// <summary>
    /// Turns match events into Stats-screen counters and badge progress (GAME_DESIGN §9). Call after every
    /// ApplyShot / Tick of a single-player match with that call's events.
    /// </summary>
    public static class StatsRecorder
    {
        public const double LongShotMeters = 35.0;
        public const int StrongWind = 4;
        public const double QuickDrawSeconds = 3.0;

        /// <param name="shot">The shot of this call (null for a Tick).</param>
        /// <param name="aimSeconds">How long the player aimed before releasing (Quick Draw).</param>
        public static void Record(SaveData save, MatchState m, IReadOnlyList<MatchEvent> events, ShotResult shot, int player,
            double aimSeconds)
        {
            Fighter me = m.GetFighter(player);
            bool myShot = shot != null && shot.Accepted && shot.Shooter == player;
            bool triple = false;
            bool strongWind = false;
            if (myShot)
            {
                save.AddStat(StatKey.Shots, 1);
                if (aimSeconds < QuickDrawSeconds) save.AddStat(StatKey.QuickDraws, 1);
                triple = shot.Input.UseAbility && me.Def.Ability == AbilityKind.TripleShot;
                strongWind = System.Math.Abs(shot.Wind) >= StrongWind;
            }

            for (int i = 0; i < events.Count; i++)
            {
                MatchEvent e = events[i];
                bool mine = e.Source == player;
                switch (e.Kind)
                {
                    case MatchEventKind.Hit:
                        if (!mine) break;
                        save.AddStat(StatKey.Hits, 1);
                        if (e.Zone == HitZone.Head) save.AddStat(StatKey.Headshots, 1);
                        if (strongWind) save.AddStat(StatKey.StrongWindHits, 1);
                        if (e.Tip == ArrowTip.Electric) save.AddStat(StatKey.LightningStrikes, 1);
                        if (triple) save.AddStat(StatKey.TripleShotHits, 1);
                        double distance = System.Math.Abs(e.Point.X - me.Feet.X);
                        if (distance >= LongShotMeters) save.AddStat(StatKey.LongShotHits, 1);
                        if (distance > save.LongestShot) save.LongestShot = distance;
                        break;
                    case MatchEventKind.ChainHit:
                        if (mine) save.AddStat(StatKey.ElectricChains, 1);
                        break;
                    case MatchEventKind.BurnDamage:
                        if (e.Fighter >= 0 && m.GetFighter(e.Fighter).Side != me.Side) save.AddStat(StatKey.BurnDamage, e.Amount);
                        break;
                    case MatchEventKind.CrateBroken:
                    case MatchEventKind.CrateKnockedOff:
                    case MatchEventKind.Explosion:
                        if (mine) save.AddStat(StatKey.PropsDestroyed, 1);
                        break;
                    case MatchEventKind.TowerToppled:
                        if (mine) save.AddStat(StatKey.TowersToppled, 1);
                        break;
                    case MatchEventKind.TargetHit:
                        if (mine) save.AddStat(StatKey.TargetHits, 1);
                        break;
                    case MatchEventKind.AppleHit:
                        if (mine) save.AddStat(StatKey.ApplesHit, 1);
                        break;
                }
            }
        }
    }
}
