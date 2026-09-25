using ArcherArcade.Archers;
using ArcherArcade.Arena;
using ArcherArcade.Core;
using ArcherArcade.Feel;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Modes;
using ArcherArcade.Theme;
using UnityEngine;

namespace ArcherArcade.Match
{
    /// <summary>
    /// What every match event looks, sounds and feels like (GAME_DESIGN §3.3, §3.3.1, §12 juice list): bursts,
    /// hit-stop (70 / 45 / 30 ms), shake (8 / 5 dp), damage numbers, "HEADSHOT!", combos, element impacts
    /// (fireball, lightning from the sky, bomb shockwave, ice, poison cloud), props breaking, knockout slow-mo and
    /// "POOF!". Pure presentation: Logic has already decided everything.
    /// </summary>
    public sealed class MatchJuice
    {
        static readonly Color Gold = new Color32(0xFF, 0xD2, 0x3F, 0xFF);
        static readonly Color Pink = new Color32(0xFF, 0x5F, 0xA2, 0xFF);
        static readonly Color Sky = new Color32(0x38, 0xBD, 0xF8, 0xFF);
        static readonly Color Mint = new Color32(0x2E, 0xD3, 0xA0, 0xFF);
        static readonly Color Orange = new Color32(0xFF, 0x8A, 0x3D, 0xFF);
        static readonly Color Red = new Color32(0xE5, 0x48, 0x4D, 0xFF);
        static readonly Color Wood = new Color32(0xC9, 0x8A, 0x4B, 0xFF);
        static readonly Color WoodLight = new Color32(0xE8, 0xB0, 0x7A, 0xFF);
        static readonly Color WoodDark = new Color32(0x9A, 0x62, 0x34, 0xFF);
        static readonly Color Leaf = new Color32(0x7E, 0xD9, 0x57, 0xFF);
        static readonly Color[] Party = { Gold, Pink, Sky, Mint, Orange };
        static readonly Color[] WoodBits = { Wood, WoodLight, WoodDark };
        static readonly Color[] Poof = { Color.white, Gold, Pink, new Color32(0xC9, 0xD3, 0xE3, 0xFF) };

        readonly MatchSceneRoot _root;
        int _hitsThisShot;
        Vector3 _lastChain;

        public MatchJuice(MatchSceneRoot root) => _root = root;

        FxSystem Fx => FxSystem.Instance;
        MatchState M => _root.Session.Match;
        static AudioManager Audio => ServiceLocator.Audio;
        static HapticsManager Haptics => ServiceLocator.Haptics;
        static Sprite S(string id) => ArtLibrary.Get(ArtLibrary.Fx, id);

        void Text(Vector3 at, string key, Color c, float size = 24f, float life = 1.2f) => _root.Numbers.Show(at, Loc.T(key), c, size, life);

        public void BeginShot()
        {
            _hitsThisShot = 0;
        }

        /// <summary>Head position of a fighter for text above it.</summary>
        Vector3 Above(int fighter, float up = 0.55f)
        {
            ArcherView v = _root.Roster.View(fighter);
            return v ? v.HeadWorld + Vector3.up * up : Vector3.zero;
        }

        // ------------------------------------------------------------------ shot events

        public void OnShotEvent(MatchEvent e)
        {
            Vector3 p = WorldSprites.V(e.Point);
            switch (e.Kind)
            {
                case MatchEventKind.AbilityUsed: AbilityCast(e); break;
                case MatchEventKind.BoosterUsed:
                    _root.Numbers.Show(Above(e.Fighter), Loc.T("booster_" + (BoosterKind)e.Amount), Gold, 20f, 1.1f);
                    Audio?.Play(SoundId.TripleFan);
                    break;
                case MatchEventKind.RainOfLeaves:
                    if (Fx) Fx.Burst(S("leaf"), p + Vector3.up * 6f, 18, 4f, 1.6f, new Color[] { Leaf, Mint, Gold }, 0.3f, 2f, 0f);
                    Audio?.Play(SoundId.Storm);
                    break;
                case MatchEventKind.Hit: Hit(e, p); break;
                case MatchEventKind.ChainHit: Chain(e, p); break;
                case MatchEventKind.SplashHit:
                case MatchEventKind.ExplosionHit:
                    Damage(e.Fighter, e.Amount, WorldSprites.V(e.Point), false);
                    break;
                case MatchEventKind.Miss: Miss(e, p); break;
                case MatchEventKind.BurnApplied:
                    Text(Above(e.Fighter, 0.9f), "hud_burn", Orange, 20f);
                    _root.Roster.SyncLooks(e.Fighter);
                    Audio?.Play(SoundId.Burn);
                    break;
                case MatchEventKind.PoisonApplied:
                    Text(Above(e.Fighter, 0.9f), "hud_poison", new Color32(0x9B, 0xE1, 0x5D, 0xFF), 20f);
                    _root.Roster.SyncLooks(e.Fighter);
                    break;
                case MatchEventKind.Stunned:
                    Text(Above(e.Fighter, 0.9f), "hud_stun", Gold, 20f);
                    if (Fx) Fx.Burst(S("star"), Above(e.Fighter, 0.1f), 6, 2f, 0.8f, new Color[] { Gold, Color.white }, 0.16f, 0f, 1f);
                    break;
                case MatchEventKind.Frozen:
                    Text(Above(e.Fighter, 0.9f), "hud_frozen", new Color32(0x7F, 0xD6, 0xF7, 0xFF), 20f);
                    _root.Roster.View(e.Fighter)?.Freeze(1.2f);
                    break;
                case MatchEventKind.Knockout: Knockout(e); break;
                case MatchEventKind.PropHit: PropHit(e, p); break;
                case MatchEventKind.CrateBroken: CrateBroken(e, p); break;
                case MatchEventKind.CrateKnockedOff:
                    _root.Arena.Prop(e.Prop)?.FlyOff(Push(e) * 4f, 3f);
                    Splinters(p, 8);
                    Audio?.Play(SoundId.HitWood);
                    ScreenShake.Add(3f);
                    _root.Arrows.Hide(e.Arrow);
                    break;
                case MatchEventKind.TowerToppled: Topple(e, p); break;
                case MatchEventKind.Explosion: Explosion(e, p); break;
                case MatchEventKind.Bounce:
                    _root.Arena.Prop(e.Prop)?.Hit();
                    if (Fx) Fx.Ring(p, Mint, 0.9f, 0.35f);
                    Text(p + Vector3.up * 0.4f, "hud_boing", Mint, 18f, 0.8f);
                    Audio?.Play(SoundId.Bounce);
                    break;
                case MatchEventKind.TargetHit: TargetHit(e, p); break;
                case MatchEventKind.AppleHit:
                    _root.Arena.Prop(e.Prop)?.FlyOff(Push(e) * 3f, 3.5f);
                    if (Fx) Fx.Burst(S("star"), p, 12, 4f, 0.9f, new Color[] { Gold, Red, Color.white }, 0.2f, 4f, 1.5f);
                    Text(p + Vector3.up * 0.5f, "hud_apple", Red, 24f);
                    Audio?.Play(SoundId.HitWood);
                    Audio?.Play(SoundId.Coin, 0.8f, 1.2f);
                    _root.Arrows.Hide(e.Arrow);
                    TimeScaleDriver.HitStop(45f);
                    break;
                case MatchEventKind.DummyHit:
                    _root.Arena.Prop(e.Prop)?.Hit();
                    StickToProp(e);
                    Text(p + Vector3.up * 0.5f, "hud_dummy_hit", Red, 20f, 1.4f);
                    Audio?.Play(SoundId.HitWood);
                    break;
                case MatchEventKind.RopeCut:
                    _root.Arena.Prop(e.Prop)?.CutRope();
                    _root.Arrows.Hide(e.Arrow);
                    if (Fx) Fx.Burst(S("splinter"), p, 5, 2f, 0.6f, WoodBits, 0.14f);
                    Text(p + Vector3.up * 0.4f, "hud_rope", Mint, 26f, 1.4f);
                    Audio?.Play(SoundId.HitWood, 1f, 1.3f);
                    Audio?.Play(SoundId.Cheer, 0.8f);
                    break;
                case MatchEventKind.ShieldBlocked:
                {
                    PropView pv = _root.Arena.Prop(e.Prop);
                    pv?.Hit();
                    bool boss = e.Prop >= 0 && M.GetProp(e.Prop).Spec.RotatingSlot >= 0;
                    StickToProp(e);
                    Splinters(p, 5);
                    if (boss && Fx) Fx.Burst(S("spark"), p, 8, 3f, 0.3f, new Color[] { Color.white, Gold }, 0.14f, 0f, 0f);
                    Text(p + Vector3.up * 0.4f, "hud_blocked", Color.white, 22f);
                    Audio?.Play(boss ? SoundId.HitMetal : SoundId.HitShield);
                    ScreenShake.Add(3f);
                    TimeScaleDriver.HitStop(30f);
                    break;
                }
                case MatchEventKind.ShieldKnockedDown:
                    _root.Arena.Prop(e.Prop)?.KnockDown();
                    _root.Arrows.Hide(e.Arrow);
                    Splinters(p, 10);
                    Audio?.Play(SoundId.HitShield, 1f, 0.8f);
                    ScreenShake.Add(5f);
                    break;
                case MatchEventKind.Knockback:
                    _root.Roster.MoveTo(e.Fighter, p, 0.3f, false);
                    if (Fx) Fx.Smoke(p, 3, 0.5f, new Color(0.95f, 0.9f, 0.8f, 0.8f));
                    break;
                case MatchEventKind.Stumble:
                    Text(Above(e.Fighter), "hud_whoa", Color.white, 20f);
                    _root.Roster.View(e.Fighter)?.Hit();
                    break;
                case MatchEventKind.FighterDropped:
                    _root.Roster.MoveTo(e.Fighter, p, 0.4f, false);
                    if (Fx) Fx.Smoke(p, 5, 0.7f, new Color(0.95f, 0.9f, 0.8f, 0.8f));
                    ScreenShake.Add(4f);
                    break;
                case MatchEventKind.BubbleAbsorbed:
                case MatchEventKind.BubblePopped:
                {
                    _root.Roster.SyncLooks(e.Fighter);
                    if (Fx)
                    {
                        Fx.Burst(S("bubble_small"), _root.Roster.View(e.Fighter).ChestWorld, 14, 3f, 0.7f, new Color[] { Sky, Color.white }, 0.2f, 1f, 1f);
                        Fx.Ring(_root.Roster.View(e.Fighter).ChestWorld, Sky, 1.3f, 0.3f);
                    }
                    Text(Above(e.Fighter), e.Kind == MatchEventKind.BubbleAbsorbed ? "hud_bubble" : "hud_bubble_pop", Sky, 22f);
                    Audio?.Play(SoundId.BubblePop);
                    if (e.Kind == MatchEventKind.BubbleAbsorbed) _root.Arrows.Hide(e.Arrow);
                    break;
                }
                case MatchEventKind.HelmetSaved:
                {
                    _root.Roster.SyncLooks(e.Fighter);
                    Vector3 head = _root.Roster.View(e.Fighter).HeadWorld;
                    if (Fx)
                    {
                        Fx.Emit(S("helmet"), head + Vector3.up * 0.25f, new Vector3(-M.GetFighter(e.Fighter).Facing * 2.5f, 5f, 0f), 1.2f, 0.5f, 0.5f,
                            Color.white, new Color(1f, 1f, 1f, 0f), 14f, 0f, 540f, WorldSprites.Fx);
                        Fx.Burst(S("spark"), head, 8, 3f, 0.3f, new Color[] { Color.white, Gold }, 0.14f, 0f, 0f);
                    }
                    Text(Above(e.Fighter), "hud_helmet", Color.white, 24f);
                    Audio?.Play(SoundId.HitMetal);
                    ScreenShake.Add(4f);
                    break;
                }
                case MatchEventKind.ExtraShot:
                    Text(Above(M.CurrentFighter.Index, 0.9f), "hud_extra_shot", Gold, 20f);
                    break;
            }
        }

        static float Facing(MatchState m, int fighter) => fighter >= 0 ? m.GetFighter(fighter).Facing : 1f;

        Vector2 Push(MatchEvent e)
        {
            ArrowView v = _root.Arrows.ViewFor(e.Arrow);
            if (v)
            {
                Vector2 d = WorldSprites.V2(v.Path.EndVelocity).normalized;
                return d;
            }
            return new Vector2(e.Source >= 0 ? Facing(M, e.Source) : 1f, 0f);
        }

        void AbilityCast(MatchEvent e)
        {
            var ability = (AbilityKind)e.Amount;
            ArcherView v = _root.Roster.View(e.Fighter);
            Color c = ElementColors.Main(M.GetFighter(e.Fighter).Def.Element);
            if (Fx && v)
            {
                Fx.Ring(v.ChestWorld, c, 1.6f, 0.45f);
                Fx.Flash(v.ChestWorld, new Color(c.r, c.g, c.b, 0.8f), 2.4f, 0.35f);
                Fx.Burst(S("spark"), v.GripWorld, 12, 3f, 0.5f, new Color[] { c, Color.white }, 0.18f, 0f, 0.5f);
            }
            _root.Numbers.Show(Above(e.Fighter, 0.9f), Loc.T("ability_" + ability) + "!", Gold, 24f, 1.3f);
            string sound;
            switch (ability)
            {
                case AbilityKind.MeteorArrow: sound = SoundId.Meteor; break;
                case AbilityKind.StormBolt: sound = SoundId.Storm; break;
                case AbilityKind.ClusterBomb: sound = SoundId.Cluster; break;
                default: sound = SoundId.TripleFan; break;
            }
            Audio?.Play(sound);
            _root.Rig.Punch(0.5f);
        }

        void Hit(MatchEvent e, Vector3 p)
        {
            int target = e.Fighter;
            ArcherView v = _root.Roster.View(target);
            ArrowView arrow = _root.Arrows.ViewFor(e.Arrow);
            if (v && arrow && M.GetFighter(target).IsAlive)
            {
                Vector2 vel = WorldSprites.V2(arrow.Path.EndVelocity);
                v.AddStuckArrow(p, Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg, e.Zone, arrow.Sprite);
            }
            _root.Arrows.Hide(e.Arrow);
            bool head = e.Zone == HitZone.Head || e.Zone == HitZone.WeakSpot;
            Damage(target, e.Amount, p, head);
            _hitsThisShot++;
            if (Fx)
            {
                if (head)
                {
                    Fx.Burst(S("star"), p, 16, 5f, 0.7f, Party, 0.22f, 6f, 1.5f);
                    Fx.Flash(p, new Color(1f, 0.9f, 0.4f, 0.9f), 1.6f, 0.2f);
                    Fx.Ring(p, Gold, 0.8f, 0.3f);
                }
                else if (e.Zone == HitZone.Body) Fx.Burst(S("dot"), p, 12, 4f, 0.55f, new Color[] { Color.white, Gold, Pink }, 0.12f, 6f, 1.5f);
                else Fx.Burst(S("dot"), p, 6, 3f, 0.45f, new Color[] { Color.white, Gold }, 0.1f, 6f, 1f);
            }
            if (e.Zone == HitZone.WeakSpot) Text(Above(target, 0.9f), "hud_weak_spot", Gold, 28f, 1.3f);
            else if (head) Text(Above(target, 0.9f), "hud_headshot", Gold, 28f, 1.3f);
            if (_hitsThisShot == 2) Text(Above(target, 1.35f), "hud_double", Pink, 24f, 1.2f);
            else if (_hitsThisShot == 3) Text(Above(target, 1.35f), "hud_triple", Pink, 26f, 1.3f);

            float ms = head ? 70f : e.Zone == HitZone.Body ? 45f : 30f;
            TimeScaleDriver.HitStop(ms);
            ScreenShake.Add(head ? 8f : e.Zone == HitZone.Body ? 5f : 3f);
            if (head) _root.Rig.Punch(0.7f);
            Haptics?.Play(head ? HapticId.Headshot : HapticId.Hit);
            Audio?.Play(head ? SoundId.Headshot : SoundId.HitBody);
            Impact(e.Tip, p, target);
        }

        /// <summary>Damage number, HP drop and the hurt flinch.</summary>
        void Damage(int fighter, int amount, Vector3 at, bool crit)
        {
            if (fighter < 0 || amount <= 0) return;
            _root.Roster.ShowDamage(fighter, amount);
            _root.Roster.View(fighter)?.Hit();
            _root.Numbers.Show(at + Vector3.up * 0.25f, "−" + amount, crit ? Gold : Color.white, crit ? 30f : 22f, 1f);
            _root.OnShownHpChanged(fighter);
        }

        /// <summary>The tip's element on impact (GAME_DESIGN §3.3.1).</summary>
        void Impact(ArrowTip tip, Vector3 p, int target)
        {
            FxSystem fx = Fx;
            if (!fx) return;
            switch (tip)
            {
                case ArrowTip.Fire:
                    fx.Flash(p, new Color(1f, 0.6f, 0.2f, 0.9f), 2.6f, 0.4f);
                    fx.Burst(S("flame"), p, 14, 3.5f, 0.5f, new Color[] { Orange, Gold, new Color32(0xFF, 0x5A, 0x1F, 0xFF) }, 0.3f, -2f, 1.5f);
                    fx.Smoke(p + Vector3.up * 0.3f, 4, 0.7f, new Color(0.35f, 0.3f, 0.3f, 0.7f));
                    Audio?.Play(SoundId.FireBurst);
                    break;
                case ArrowTip.Electric:
                    _root.Delay(0.15f, () =>
                    {
                        FxSystem f = FxSystem.Instance;
                        if (!f) return;
                        f.Lightning(p, 9f);
                        f.Burst(S("spark"), p, 14, 5f, 0.35f, new Color[] { Color.white, Sky, Gold }, 0.2f, 0f, 0.5f);
                        _root.ScreenFlash(new Color(1f, 1f, 0.85f, 0.55f), 0.18f);
                        ScreenShake.Add(6f);
                        Audio?.Play(SoundId.LightningStrike);
                        Audio?.Play(SoundId.Thunder, 0.7f);
                    });
                    Audio?.Play(SoundId.Zap);
                    break;
                case ArrowTip.Bomb:
                    Boom(p, 1.8f, SoundId.Bomb);
                    break;
                case ArrowTip.Ice:
                    fx.Burst(S("ice_shard"), p, 12, 4f, 0.6f, new Color[] { Color.white, new Color32(0x7F, 0xD6, 0xF7, 0xFF) }, 0.24f, 8f, 1.5f);
                    fx.Flash(p, new Color(0.8f, 0.95f, 1f, 0.8f), 2.2f, 0.3f);
                    fx.Smoke(p, 4, 0.6f, new Color(0.85f, 0.96f, 1f, 0.7f));
                    Audio?.Play(SoundId.Freeze);
                    break;
                case ArrowTip.Poison:
                    fx.Emit(S("poison_cloud"), p, Vector3.up * 0.3f, 1.4f, 0.8f, 1.8f, new Color(0.6f, 1f, 0.4f, 0.85f), new Color(0.6f, 1f, 0.4f, 0f), 0f, 1f, 20f, WorldSprites.Fx - 2);
                    fx.Burst(S("bubble_small"), p, 10, 2f, 0.9f, new Color[] { new Color32(0x9B, 0xE1, 0x5D, 0xFF), new Color32(0xC6, 0xFF, 0x4A, 0xFF) }, 0.14f, -1f, 1f);
                    Audio?.Play(SoundId.PoisonCloud);
                    break;
                case ArrowTip.Heavy:
                    fx.Smoke(p, 4, 0.6f, new Color(0.95f, 0.9f, 0.8f, 0.8f));
                    ScreenShake.Add(3f);
                    break;
            }
        }

        void Boom(Vector3 p, float radius, string sound)
        {
            FxSystem fx = Fx;
            if (fx)
            {
                fx.Flash(p, new Color(1f, 0.95f, 0.7f, 1f), radius * 2.4f, 0.3f);
                fx.Ring(p, new Color(1f, 0.85f, 0.5f, 0.9f), radius, 0.4f);
                fx.Burst(S("flame"), p, 14, 6f, 0.45f, new Color[] { Orange, Gold, Red }, 0.34f, -1f, 2f);
                fx.Smoke(p + Vector3.up * 0.2f, 9, 1.2f, new Color(0.4f, 0.36f, 0.4f, 0.85f));
                fx.Burst(S("splinter"), p, 10, 7f, 0.9f, WoodBits, 0.18f, 14f, 3f);
            }
            _root.Numbers.Show(p + Vector3.up * 0.8f, Loc.T("hud_boom"), Orange, 30f, 1.1f);
            Audio?.Play(sound);
            ScreenShake.Add(11f);
            TimeScaleDriver.HitStop(55f);
            TimeScaleDriver.SlowMo(0.35f, 0.1f);
            Haptics?.Play(HapticId.Headshot);
            _root.Rig.Punch(0.6f);
        }

        void Chain(MatchEvent e, Vector3 p)
        {
            FxSystem fx = Fx;
            Vector3 from = _lastChain == Vector3.zero ? p : _lastChain;
            ArrowView a = _root.Arrows.ViewFor(e.Arrow);
            if (a) from = WorldSprites.V(a.Path.EndPosition);
            if (fx)
            {
                // Sparks jump along the line.
                for (int i = 0; i <= 6; i++)
                {
                    Vector3 q = Vector3.Lerp(from, p, i / 6f) + new Vector3(0f, fx.Range(-0.2f, 0.2f), 0f);
                    fx.Emit(S("spark"), q, Vector3.zero, 0.25f, 0.3f, 0.05f, Color.white, new Color(0.25f, 0.88f, 0.95f, 0f), 0f, 0f, 500f, WorldSprites.Fx + 2);
                }
                fx.Flash(p, new Color(0.7f, 1f, 1f, 0.8f), 1.4f, 0.2f);
            }
            _lastChain = p;
            Text(p + Vector3.up * 0.5f, "hud_zap", Sky, 20f, 0.9f);
            Audio?.Play(SoundId.Zap, 0.8f, 1.2f);
            if (e.Fighter >= 0) Damage(e.Fighter, e.Amount, p, false);
            else _root.Arena.Prop(e.Prop)?.Hit();
        }

        void Miss(MatchEvent e, Vector3 p)
        {
            ArrowView a = _root.Arrows.ViewFor(e.Arrow);
            if (a == null) return;
            ContactKind c = a.Path.Contact;
            if (c == ContactKind.Ground)
            {
                if (Fx)
                {
                    Fx.Burst(S("dot"), p, 5, 2f, 0.45f, new Color[] { Leaf, new Color32(0xB9, 0x8A, 0x5A, 0xFF) }, 0.1f, 8f, 2f);
                    Fx.Smoke(p, 2, 0.4f, new Color(0.95f, 0.9f, 0.8f, 0.6f));
                }
                Audio?.Play(SoundId.HitWood, 0.6f, 0.7f);
            }
            else if (c == ContactKind.Wall)
            {
                if (Fx) Fx.Burst(S("dot"), p, 6, 2.5f, 0.4f, new Color[] { new Color32(0xC9, 0xC3, 0xD6, 0xFF), Color.white }, 0.1f, 8f, 1f);
                Audio?.Play(SoundId.HitStone);
                ScreenShake.Add(2f);
            }
        }

        void PropHit(MatchEvent e, Vector3 p)
        {
            PropView pv = _root.Arena.Prop(e.Prop);
            pv?.Hit();
            StickToProp(e);
            PropKind kind = e.Prop >= 0 ? M.GetProp(e.Prop).Kind : PropKind.Wall;
            switch (kind)
            {
                case PropKind.VineWall:
                    if (Fx) Fx.Burst(S("leaf"), p, 8, 3f, 0.8f, new Color[] { Leaf, Mint }, 0.2f, 4f, 1f);
                    Audio?.Play(SoundId.HitWood, 0.9f, 0.9f);
                    break;
                case PropKind.Wall:
                    bool stone = M.PropRestShape(e.Prop).HalfSize.Y * 2.0 > 4.0;
                    Splinters(p, stone ? 0 : 6);
                    if (stone && Fx) Fx.Burst(S("dot"), p, 6, 2.5f, 0.4f, new Color[] { new Color32(0xC9, 0xC3, 0xD6, 0xFF), Color.white }, 0.1f, 8f, 1f);
                    Audio?.Play(stone ? SoundId.HitStone : SoundId.HitWood);
                    break;
                default:
                    Splinters(p, 6);
                    Audio?.Play(SoundId.HitWood);
                    break;
            }
            ScreenShake.Add(2f);
            TimeScaleDriver.HitStop(25f);
        }

        void StickToProp(MatchEvent e)
        {
            ArrowView a = _root.Arrows.ViewFor(e.Arrow);
            PropView pv = _root.Arena.Prop(e.Prop);
            if (a && pv) a.AttachTo(pv.Body);
        }

        void Splinters(Vector3 p, int n)
        {
            if (n > 0 && Fx) Fx.Burst(S("splinter"), p, n, 3.5f, 0.7f, WoodBits, 0.16f, 12f, 2f);
        }

        void CrateBroken(MatchEvent e, Vector3 p)
        {
            PropView pv = _root.Arena.Prop(e.Prop);
            Vector3 c = pv ? pv.Center : p;
            pv?.Vanish();
            _root.Arrows.Hide(e.Arrow);
            if (Fx)
            {
                Fx.Burst(ArtLibrary.Get(ArtLibrary.Props, "crate_plank"), c, 6, 5f, 1f, new Color[] { Color.white }, 0.6f, 14f, 3f, WorldSprites.Fx, 520f);
                Fx.Burst(S("splinter"), c, 12, 5f, 0.8f, WoodBits, 0.18f, 12f, 2f);
                Fx.Smoke(c, 5, 0.9f, new Color(0.95f, 0.9f, 0.8f, 0.8f));
            }
            Audio?.Play(SoundId.CrateBreak);
            ScreenShake.Add(4f);
            TimeScaleDriver.HitStop(40f);
        }

        void Topple(MatchEvent e, Vector3 p)
        {
            int tower = e.Amount;
            for (int i = 0; i < M.PropCount; i++)
            {
                Prop prop = M.GetProp(i);
                if (prop.Spec.Tower != tower || prop.Kind == PropKind.TntCrate) continue;
                PropView pv = _root.Arena.Prop(i);
                if (pv == null || !pv.Shown) continue;
                Vector3 d = pv.Center - p;
                float dir = d.x >= 0f ? 1f : -1f;
                pv.FlyOff(new Vector2(dir * (2.5f + Mathf.Abs(d.x) * 2f), d.y * 1.5f), 4f);
            }
            _root.Arrows.Hide(e.Arrow);
            Text(p + Vector3.up * 1f, "hud_topple", WoodLight, 28f, 1.2f);
            Audio?.Play(SoundId.TowerTopple);
            ScreenShake.Add(7f);
        }

        void Explosion(MatchEvent e, Vector3 p)
        {
            PropView pv = _root.Arena.Prop(e.Prop);
            pv?.Vanish();
            bool tnt = e.Prop >= 0 && M.GetProp(e.Prop).Kind == PropKind.TntCrate;
            double radius = tnt ? M.Setup.PropRules.TntRadius : M.Setup.PropRules.BarrelRadius;
            Boom(p, (float)radius, tnt ? SoundId.TntBoom : SoundId.BarrelBoom);
            _root.ScreenFlash(new Color(1f, 0.9f, 0.7f, 0.35f), 0.15f);
            _root.Arrows.Hide(e.Arrow);
        }

        void TargetHit(MatchEvent e, Vector3 p)
        {
            PropView pv = _root.Arena.Prop(e.Prop);
            Prop prop = M.GetProp(e.Prop);
            if (prop.Spec.Durable)
            {
                pv?.Hit();
                StickToProp(e);
                Shape s = M.PropShapeAt(e.Prop, M.Clock);
                bool bull = System.Math.Abs(e.Point.Y - s.A.Y) <= TrainingRange.BullseyeHalfHeight;
                Text(p + Vector3.up * 0.5f, bull ? "tr_bullseye" : "hud_target", bull ? Gold : Color.white, bull ? 28f : 22f);
                if (bull && Fx) Fx.Burst(S("confetti"), p, 18, 5f, 1.1f, Party, 0.14f, 6f, 3f);
                Audio?.Play(bull ? SoundId.Headshot : SoundId.HitWood);
                TimeScaleDriver.HitStop(bull ? 60f : 35f);
            }
            else
            {
                pv?.FlyOff(Push(e) * 2.5f, 3f);
                _root.Arrows.Hide(e.Arrow);
                if (Fx)
                {
                    Fx.Burst(S("confetti"), p, 20, 5f, 1.2f, Party, 0.14f, 6f, 3f);
                    Fx.Ring(p, Gold, 0.9f, 0.3f);
                }
                Text(p + Vector3.up * 0.5f, "hud_target", Gold, 26f);
                Audio?.Play(SoundId.HitWood);
                Audio?.Play(SoundId.Coin, 0.8f, 1.1f);
                TimeScaleDriver.HitStop(50f);
                ScreenShake.Add(3f);
            }
            Haptics?.Play(HapticId.Hit);
        }

        void Knockout(MatchEvent e)
        {
            int f = e.Fighter;
            ArcherView v = _root.Roster.View(f);
            Vector3 chest = v ? v.ChestWorld : WorldSprites.V(e.Point);
            bool more = MoreOnSide(f);
            _root.Roster.KnockOut(f, more);
            if (Fx)
            {
                Fx.Burst(S("puff"), chest, 10, 3f, 0.9f, Poof, 0.5f, 0f, 1f);
                Fx.Burst(S("star"), chest + Vector3.up * 0.4f, 24, 6f, 1f, Poof, 0.2f, 5f, 2f);
                Fx.Ring(chest, Color.white, 1.8f, 0.4f);
            }
            _root.Numbers.Show(Above(f, 1.1f), Loc.T("hud_poof"), Pink, 34f, 1.4f);
            TimeScaleDriver.SlowMo(0.25f, 0.8f);
            ScreenShake.Add(9f);
            _root.Rig.Punch(1f);
            Audio?.Play(SoundId.Knockout);
            Haptics?.Play(HapticId.Win);
        }

        bool MoreOnSide(int fighter)
        {
            int side = M.GetFighter(fighter).Side;
            for (int i = 0; i < M.FighterCount; i++)
            {
                Fighter o = M.GetFighter(i);
                if (i != fighter && o.Side == side && o.IsAlive) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ turn events

        /// <summary>Plays a between-turns event; returns how long to wait before the next one.</summary>
        public float OnTurnEvent(MatchEvent e)
        {
            Vector3 p = WorldSprites.V(e.Point);
            switch (e.Kind)
            {
                case MatchEventKind.AbilityReady:
                {
                    Fighter f = M.GetFighter(e.Fighter);
                    string name = Loc.T("ability_" + (AbilityKind)e.Amount);
                    if (_root.Session.IsHuman(f.Side)) _root.Toast(Loc.F("ability_ready", name));
                    _root.Numbers.Show(Above(e.Fighter, 0.9f), name + " ✓", Gold, 20f, 1.2f);
                    if (Fx) Fx.Ring(_root.Roster.View(e.Fighter).ChestWorld, Gold, 1.4f, 0.4f);
                    Audio?.Play(SoundId.UiConfirm);
                    return 0.35f;
                }
                case MatchEventKind.PlatformMoved:
                case MatchEventKind.ShieldsRotated:
                case MatchEventKind.ShieldRaised:
                    _root.Arena.SyncProps(true, M.Clock);
                    return 0.15f;
                case MatchEventKind.VineWallGrown:
                    _root.Arena.SyncProps(true, M.Clock);
                    if (Fx) Fx.Burst(S("leaf"), p, 12, 3f, 0.9f, new Color[] { Leaf, Mint }, 0.22f, 3f, 2f);
                    Text(p + Vector3.up * 1.6f, "hud_vines", Leaf, 24f, 1.2f);
                    Audio?.Play(SoundId.Storm, 0.7f, 1.2f);
                    return 0.7f;
                case MatchEventKind.BurnDamage:
                {
                    ArcherView v = _root.Roster.View(e.Fighter);
                    if (Fx && v) Fx.Burst(S("flame"), v.ChestWorld, 10, 2f, 0.5f, new Color[] { Orange, Gold }, 0.26f, -2f, 1.5f);
                    Damage(e.Fighter, e.Amount, v ? v.ChestWorld : p, false);
                    Audio?.Play(SoundId.Burn);
                    return 0.6f;
                }
                case MatchEventKind.PoisonDamage:
                {
                    ArcherView v = _root.Roster.View(e.Fighter);
                    if (Fx && v) Fx.Burst(S("bubble_small"), v.ChestWorld, 10, 2f, 0.7f, new Color[] { new Color32(0x9B, 0xE1, 0x5D, 0xFF) }, 0.14f, -1f, 1f);
                    Damage(e.Fighter, e.Amount, v ? v.ChestWorld : p, false);
                    Audio?.Play(SoundId.PoisonCloud, 0.8f);
                    return 0.6f;
                }
                case MatchEventKind.Healed:
                {
                    ArcherView v = _root.Roster.View(e.Fighter);
                    _root.Roster.ShowHeal(e.Fighter, e.Amount);
                    _root.OnShownHpChanged(e.Fighter);
                    if (Fx && v) Fx.Burst(S("heal_plus"), v.ChestWorld, 10, 1.5f, 1f, new Color[] { Mint, Color.white }, 0.22f, -2f, 1.5f);
                    _root.Numbers.Show(Above(e.Fighter), Loc.F("hud_heal", e.Amount), Mint, 24f, 1.2f);
                    Audio?.Play(SoundId.Heal);
                    return 0.7f;
                }
                case MatchEventKind.BubbleCast:
                    _root.Roster.SyncLooks(e.Fighter);
                    if (Fx) Fx.Ring(_root.Roster.View(e.Fighter).ChestWorld, Sky, 1.5f, 0.4f);
                    Text(Above(e.Fighter), "hud_bubble", Sky, 22f);
                    Audio?.Play(SoundId.BubbleCast);
                    return 0.6f;
                case MatchEventKind.Enraged:
                {
                    ArcherView v = _root.Roster.View(e.Fighter);
                    if (Fx && v)
                    {
                        Fx.Flash(v.ChestWorld, new Color(1f, 0.3f, 0.3f, 0.8f), 3.2f, 0.4f);
                        Fx.Burst(S("flame"), v.ChestWorld, 14, 3f, 0.6f, new Color[] { Red, Orange }, 0.3f, -2f, 1.5f);
                    }
                    Text(Above(e.Fighter, 0.9f), "hud_enraged", Red, 30f, 1.4f);
                    ScreenShake.Add(6f);
                    Audio?.Play(SoundId.Thunder);
                    return 0.9f;
                }
                case MatchEventKind.Knockout:
                    Knockout(e);
                    return 1.3f;
                case MatchEventKind.TurnTimedOut:
                    _root.Toast(Loc.T("hud_times_up"));
                    return 0.3f;
            }
            return 0f;
        }
    }
}
