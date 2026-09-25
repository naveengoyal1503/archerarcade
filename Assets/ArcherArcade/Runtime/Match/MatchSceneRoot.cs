using System;
using System.Collections;
using System.Collections.Generic;
using ArcherArcade.AI;
using ArcherArcade.Archers;
using ArcherArcade.Arena;
using ArcherArcade.Core;
using ArcherArcade.Feel;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Logic.Modes;
using ArcherArcade.Tutorial;
using ArcherArcade.Tweening;
using ArcherArcade.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.Match
{
    /// <summary>
    /// Match scene entry and turn flow (SCREEN_INVENTORY 11–23): builds the arena, archers, camera and HUD for the
    /// requested mode, runs intro → aim (drag) or computer turn → shot replay (arrows + events on their exact
    /// times) → between-turn effects → next turn, then records the result and shows it. Logic (MatchState via
    /// <see cref="MatchSession"/>) decides everything; this only presents it and feeds it the player's input.
    /// </summary>
    public sealed class MatchSceneRoot : MonoBehaviour
    {
        enum Phase { Loading, Intro, Aim, Ai, Flight, Resolve, Between, Over, Result }

        static readonly Color HumanRing = new Color32(0xFF, 0xD2, 0x3F, 0xFF);

        Phase _phase = Phase.Loading;
        MatchRequest _request;
        Transform _worldRoot;
        TrajectoryPreview _preview;
        DragAim _drag;
        MatchJuice _juice;
        HudScreen _hud;
        RectTransform _overlay;
        Image _flash;
        MatchBanner _banner;
        TutorialOverlay _tutorial;
        AiTurn _ai;
        AiPlayer _autopilot;
        readonly TipDef _tipScratch = new TipDef();

        /// <summary>PlayMode tests: the player's turns are played by a Hard computer archer.</summary>
        public bool Autopilot;

        readonly List<MatchEvent> _shotEvents = new List<MatchEvent>(32);
        readonly List<MatchEvent> _turnEvents = new List<MatchEvent>(16);
        readonly List<MatchEvent> _deferred = new List<MatchEvent>(4);
        readonly List<float> _delayT = new List<float>(8);
        readonly List<Action> _delayA = new List<Action>(8);
        readonly ArrowPath?[] _ghost = new ArrowPath?[2];
        int _nextShot;
        float _replayT, _duration, _hold;
        double _clockAtShot;
        int _shooter = -1;
        bool _koThisShot;
        Vector3 _impact;
        int _lastHumanSide = -1;
        float _timerWarned;
        bool _started;

        // ---------- what the HUD reads ----------
        public MatchSession Session { get; private set; }
        public FighterRoster Roster { get; private set; }
        public ArenaView Arena { get; private set; }
        public ArrowFlight Arrows { get; private set; }
        public CameraRig Rig { get; private set; }
        public DamageNumbers Numbers { get; private set; }
        public UIManager Ui { get; private set; }
        public MatchState M => Session.Match;
        public ArrowTip SelectedTip { get; private set; } = ArrowTip.Normal;
        public bool AbilityArmed { get; private set; }
        public bool Paused { get; private set; }
        public bool Aiming => _phase == Phase.Aim;
        public bool AiTurnActive => _phase == Phase.Ai;
        public bool InFlight => _phase == Phase.Flight || _phase == Phase.Resolve;
        public bool IsOverPhase => _phase == Phase.Over || _phase == Phase.Result;
        public DragAim Drag => _drag;
        public AiTurn Ai => _ai;
        public bool TutorialActive => _tutorial != null;

        /// <summary>Seconds left on the turn timer, or −1 for no timer (Training).</summary>
        public float TimerSeconds
        {
            get
            {
                if (Session == null || Session.Mode == GameMode.Training || _tutorial != null) return -1f;
                return Mathf.Max(0f, (float)M.TurnTimeLeft);
            }
        }

        /// <summary>Changes whenever <see cref="TurnLabel"/> would (the HUD rebuilds the text only then).</summary>
        public int LabelKey
        {
            get
            {
                if (Session == null) return 0;
                int k = (int)_phase * 31 + M.CurrentSide * 7 + M.CurrentFighter.Index * 131 + (int)Loc.Current * 1009;
                if (_ai != null && !_ai.Thinking) k += 3;
                if (Session.Survival != null) k += Session.Survival.Wave * 4099;
                return k;
            }
        }

        /// <summary>The label under the HUD ("Your turn · drag back to aim", "Moss is aiming…").</summary>
        public string TurnLabel
        {
            get
            {
                if (Session == null) return "";
                string label;
                if (IsOverPhase) label = Loc.T("hud_ko");
                else if (_phase == Phase.Flight || _phase == Phase.Resolve || _phase == Phase.Between) label = Loc.T("hud_in_flight");
                else if (_phase == Phase.Ai) label = Loc.F(_ai != null && !_ai.Thinking ? "hud_cpu_aiming" : "hud_cpu_thinking", M.CurrentFighter.Def.Name);
                else if (Session.Mode == GameMode.Training) label = Loc.T("hud_training");
                else if (Session.Mode == GameMode.TwoPlayer) label = Loc.F("hud_player_turn", Session.Series.NameOnSide(M.CurrentSide));
                else label = Loc.T("hud_your_turn");
                if (Session.Mode == GameMode.Survival && Session.Survival != null) label = Loc.F("hud_wave", Session.Survival.Wave) + " · " + label;
                return label;
            }
        }

        public string NameOnSide(int side)
        {
            if (Session.Mode == GameMode.TwoPlayer) return Session.Series.NameOnSide(side);
            int f = Roster != null ? Roster.VisibleOnSide(side) : M.ActiveFighter(side);
            if (side == 0) return ServiceLocator.Profile.Data.PlayerName;
            if (f < 0) return Loc.T("hud_dummy");
            return M.GetFighter(f).Def.Name;
        }

        // ------------------------------------------------------------------ boot

        void Start()
        {
            GameManager.Ensure();
            TimeScaleDriver.Ensure(transform);
            TimeScaleDriver.ResetAll();
            ScreenShake.Clear();
            _request = SceneFlow.PendingMatch ?? DefaultRequest();
            Rig = CameraRig.Create(transform);
            Ui = UIManager.Create("MatchUI");
            Ui.RootBack = OnRootBack;
            _overlay = UiKit.Rect(Ui.SafeRoot, "MatchOverlay");
            UiKit.Stretch(_overlay);
            Numbers = DamageNumbers.Create(_overlay);
            Numbers.SetCamera(Rig.Camera);
            _banner = MatchBanner.Create(_overlay);
            _flash = UiKit.Box(Ui.Canvas.transform, "Flash", new Color(1f, 1f, 1f, 0f), 0f);
            UiKit.Stretch(_flash.rectTransform);
            _flash.raycastTarget = false;
            _juice = new MatchJuice(this);
            StartSession(_request);
        }

        static MatchRequest DefaultRequest()
        {
            int level = ServiceLocator.Profile != null ? ServiceLocator.Profile.ContinueLevel : 1;
            return new MatchRequest { Mode = GameMode.Campaign, Level = Mathf.Clamp(level, 1, WorldOne.LevelCount), Seed = (ulong)DateTime.UtcNow.Ticks };
        }

        void StartSession(MatchRequest request)
        {
            _request = request;
            Session = new MatchSession(request);
            _lastHumanSide = -1;
            BuildRound();
            PlayMusic();
        }

        void PlayMusic()
        {
            AudioManager a = ServiceLocator.Audio;
            if (a == null) return;
            if (Session.LevelDef != null && Session.LevelDef.Goal == GoalKind.Boss) a.PlayMusic(SoundId.MusicBoss);
            else if (Session.Mode == GameMode.Campaign) a.PlayMusic(SoundId.MusicWorld1);
            else a.PlayMusic(SoundId.MusicDuel);
        }

        /// <summary>Builds the world for the current match of the session (new round / wave / retry).</summary>
        void BuildRound()
        {
            StopAllCoroutines();
            _delayT.Clear();
            _delayA.Clear();
            TimeScaleDriver.ResetAll();
            Paused = false;
            if (_worldRoot) Destroy(_worldRoot.gameObject);
            if (_tutorial) Destroy(_tutorial.gameObject);
            _tutorial = null;
            Numbers.Clear();
            _worldRoot = new GameObject("World").transform;
            _worldRoot.SetParent(transform, false);
            FxSystem.Create(_worldRoot);
            bool dark = ServiceLocator.Theme != null && ServiceLocator.Theme.IsDark;
            uint seed = (uint)(Session.Match.Setup.Seed ^ (Session.Match.Setup.Seed >> 32));
            Arena = ArenaView.Create(_worldRoot, M, Session.Time, dark, seed);
            Rig.Bind(Arena);
            Rig.Camera.backgroundColor = Arena.Palette.Bottom;
            Roster = new FighterRoster(_worldRoot, M, Session.Looks, Arena.Palette.Chars);
            Arrows = ArrowFlight.Create(_worldRoot);
            _preview = TrajectoryPreview.Create(_worldRoot);
            if (_drag) Destroy(_drag.gameObject);
            _drag = DragAim.Create(transform, M.Setup.Shot);
            _drag.Began = OnDragBegan;
            _drag.Released = OnDragReleased;
            _drag.Cancelled = OnDragCancelled;
            _ghost[0] = _ghost[1] = null;
            _ai = null;
            _autopilot = null;
            AbilityArmed = false;
            SelectedTip = ArrowTip.Normal;
            Arena.SetWind(M.Wind);
            for (int i = 0; i < M.FighterCount; i++) Roster.View(i).SetArrowTip(ArrowTip.Normal);

            _hud = new HudScreen(this);
            Ui.ResetTo(_hud);
            _started = false;
            StartCoroutine(Intro());
        }

        // ------------------------------------------------------------------ intro

        IEnumerator Intro()
        {
            _phase = Phase.Intro;
            FrameOverview();
            Rig.Snap();
            yield return null;

            string title, sub;
            IntroTexts(out title, out sub);
            _banner.Show(title, sub, 1.8f);
            ServiceLocator.Audio?.Play(SoundId.TurnStart);
            yield return new WaitForSeconds(1.2f);

            // "New!" idea cards the player has not seen yet (LEVELS.md idea column), one after another.
            if (Session.LevelDef != null && Session.Mode == GameMode.Campaign)
            {
                foreach (IdeaCard card in Session.LevelDef.Ideas)
                {
                    if (ServiceLocator.Profile.HasSeen(card)) continue;
                    Ui.ShowModal(new IdeaCardModal(card, true));
                    while (Ui.HasModal) yield return null;
                }
            }
            if ((M.Setup.FirstTurn == FirstTurnRule.CoinFlip) && !M.IsSolo)
            {
                _banner.Show(Loc.F("hud_coin_flip", NameOnSide(M.CurrentSide)), null, 1.3f);
                ServiceLocator.Audio?.Play(SoundId.Coin);
                yield return new WaitForSeconds(1.1f);
            }
            _started = true;
            if (Session.Mode == GameMode.Campaign && Session.LevelDef.Number == 1 && !ServiceLocator.Profile.Data.TutorialDone)
                _tutorial = TutorialOverlay.Create(_overlay, this);
            BeginTurn();
        }

        void IntroTexts(out string title, out string sub)
        {
            switch (Session.Mode)
            {
                case GameMode.Campaign:
                {
                    LevelDef l = Session.LevelDef;
                    title = l.Goal == GoalKind.Boss ? l.Name + "!" : l.Name;
                    sub = Loc.F(l.Goal == GoalKind.Boss ? "ld_kicker_boss" : "ld_kicker_level", Loc.T("world_1"), l.Number) + " · " + GameVisuals.GoalText(l);
                    break;
                }
                case GameMode.Daily:
                    title = Loc.T("daily_kicker");
                    sub = GameVisuals.GoalText(Session.LevelDef);
                    break;
                case GameMode.TwoPlayer:
                    title = Loc.F("hud_round_start", Session.Series.Round + 1);
                    sub = Loc.F("pvp_score", Session.Series.Settings.Name1, Session.Series.Wins(0), Session.Series.Wins(1), Session.Series.Settings.Name2);
                    break;
                case GameMode.Survival:
                    title = Loc.F("hud_wave", Session.Survival.Wave);
                    sub = Loc.F("ld_opponent", M.GetFighter(M.ActiveFighter(1)).Def.Name);
                    break;
                case GameMode.Training:
                    title = Loc.T("tr_title");
                    sub = Loc.T("tr_goal");
                    break;
                default:
                    title = Loc.T("hud_fight");
                    sub = Loc.F("ld_opponent", M.GetFighter(M.ActiveFighter(1)).Def.Name);
                    break;
            }
        }

        // ------------------------------------------------------------------ turns

        void BeginTurn()
        {
            if (Session.IsOver)
            {
                StartCoroutine(FinishMatch());
                return;
            }
            Fighter f = M.CurrentFighter;
            int side = M.CurrentSide;
            Arena.SetWind(M.Wind);
            Arena.SyncProps(true, M.Clock);
            _timerWarned = 99f;
            AbilityArmed = false;
            if (!f.HasAmmo(SelectedTip)) SelectedTip = ArrowTip.Normal;
            for (int i = 0; i < M.FighterCount; i++)
            {
                ArcherView v = Roster.View(i);
                if (v && Roster.Visible(i) && M.GetFighter(i).IsAlive) v.Idle();
            }
            ServiceLocator.Audio?.Play(SoundId.TurnStart, 0.7f);
            UpdateLoops();
            _hud?.OnTurnStarted();

            if (Session.IsHuman(side) && !Autopilot)
            {
                if (Session.Mode == GameMode.TwoPlayer && _lastHumanSide >= 0 && _lastHumanSide != side)
                {
                    _phase = Phase.Between;
                    FrameAim(true);
                    Ui.ShowModal(new PassModal(Session.Series.NameOnSide(side), () => StartAim(side), PauseGame));
                    return;
                }
                StartAim(side);
            }
            else
            {
                StartAi();
            }
        }

        void StartAim(int side)
        {
            _lastHumanSide = side;
            _phase = Phase.Aim;
            Fighter f = M.CurrentFighter;
            Roster.View(f.Index).SetArrowTip(SelectedTip);
            Roster.View(f.Index).SetDrawSlow((float)f.Status.ActiveDrawSlow);
            _drag.Facing = f.Facing;
            _preview.ShowGhost(_ghost[side], Color.white);
            FrameAim(false);
        }

        void StartAi()
        {
            _phase = Phase.Ai;
            _preview.ShowGhost(null, Color.white);
            AiDecision d;
            if (Session.IsHuman(M.CurrentSide))
            {
                if (_autopilot == null) _autopilot = new AiPlayer(AiProfile.Hard(), M.DeriveSeed(99UL));
                d = _autopilot.Decide(M);
            }
            else d = Session.DecideAi();
            _ai = new AiTurn(d);
            Fighter f = M.CurrentFighter;
            Roster.View(f.Index).SetArrowTip(d.Input.UseAbility ? ArrowTip.Normal : d.Input.Tip);
            Roster.View(f.Index).SetDrawSlow((float)f.Status.ActiveDrawSlow);
            FrameAim(false);
        }

        // ------------------------------------------------------------------ input

        void OnDragBegan()
        {
            _tutorial?.OnDragBegan();
        }

        void OnDragCancelled()
        {
            if (_phase != Phase.Aim) return;
            _preview.Hide();
            Roster.View(M.CurrentFighter.Index)?.Idle(0.2f);
        }

        void OnDragReleased(float angle, float power)
        {
            if (_phase != Phase.Aim) return;
            _preview.Hide();
            var input = new ShotInput(angle, power, SelectedTip, AbilityArmed);
            Shoot(input, _drag.HeldSeconds);
        }

        /// <summary>Fires a shot for the current human turn (PlayMode tests, accessibility tools).</summary>
        public bool ShootNow(ShotInput input)
        {
            if (_phase != Phase.Aim || Paused || Ui.HasModal) return false;
            Shoot(input, 1f);
            return _phase == Phase.Flight;
        }

        public void SelectTip(ArrowTip tip)
        {
            if (_phase != Phase.Aim && _phase != Phase.Between) return;
            Fighter f = M.CurrentFighter;
            if (!f.HasAmmo(tip))
            {
                Toast(Loc.F("tip_no_ammo", Loc.T("tipname_" + tip)));
                return;
            }
            SelectedTip = tip;
            Roster.View(f.Index).SetArrowTip(tip);
            ServiceLocator.Audio?.Play(SoundId.UiTap);
            _hud?.Refresh();
        }

        public void ToggleAbility()
        {
            if (_phase != Phase.Aim) return;
            Fighter f = M.CurrentFighter;
            string name = Loc.T("ability_" + f.Def.Ability);
            if (!f.HasAbility) return;
            if (!f.AbilityReady)
            {
                int left = Mathf.Max(1, f.AbilityChargeNeeded - f.AbilityCharge);
                Toast(left == 1 ? Loc.F("ability_charging_one", name) : Loc.F("ability_charging", name, left));
                return;
            }
            AbilityArmed = !AbilityArmed;
            ServiceLocator.Audio?.Play(AbilityArmed ? SoundId.ToggleOn : SoundId.ToggleOff);
            if (AbilityArmed) Toast(Loc.F("hud_ability_armed", name));
            _hud?.Refresh();
        }

        // ------------------------------------------------------------------ shooting

        void Shoot(ShotInput input, float aimSeconds)
        {
            Fighter f = M.CurrentFighter;
            int side = f.Side;
            ArcherView view = Roster.View(f.Index);
            Vec2 bow = f.BowPosition(M.Setup.Shot);
            Vector3 offset = view ? view.ArrowTipWorld - WorldSprites.V(bow) : Vector3.zero;
            if (offset.sqrMagnitude > 1f) offset = Vector3.zero;
            AbilityKind ability = input.UseAbility ? f.Def.Ability : AbilityKind.None;
            if (ability == AbilityKind.RainOfLeaves) offset = Vector3.zero;

            ShotResult r = Session.Shoot(input);
            if (!r.Accepted)
            {
                if (r.Reason == ShotRejectReason.NoAmmo) Toast(Loc.F("tip_no_ammo", Loc.T("tipname_" + input.Tip)));
                SelectedTip = ArrowTip.Normal;
                return;
            }
            view?.Release();
            ServiceLocator.Audio?.Play(SoundId.Release);
            ServiceLocator.Audio?.Play(SoundId.Whoosh, 0.7f);
            ServiceLocator.Haptics?.Play(HapticId.Release);
            _tutorial?.OnShot();

            SplitEvents(M.Events);
            RecordStats(r, aimSeconds);
            _juice.BeginShot();
            _shooter = f.Index;
            _koThisShot = false;
            for (int i = 0; i < _shotEvents.Count; i++) if (_shotEvents[i].Kind == MatchEventKind.Knockout) _koThisShot = true;
            Arrows.Play(r, input.Tip, ability, TrailFor(side), offset, false);
            _clockAtShot = r.Clock;
            _duration = (float)r.Duration;
            _replayT = 0f;
            _nextShot = 0;
            _impact = r.Arrows.Count > 0 ? WorldSprites.V(r.Arrows[r.Arrows.Count - 1].EndPosition) : view.transform.position;
            for (int i = 0; i < r.Arrows.Count; i++)
                if (r.Arrows[i].EndTime >= _duration - 0.0001) _impact = WorldSprites.V(r.Arrows[i].EndPosition);
            if (Session.IsHuman(side) && r.Arrows.Count > 0 && ability != AbilityKind.RainOfLeaves) _ghost[side] = r.Arrows[0];
            AbilityArmed = false;
            _ai = null;
            _phase = Phase.Flight;
            if (!f.HasAmmo(SelectedTip)) SelectedTip = ArrowTip.Normal;
            _hud?.Refresh();
        }

        string TrailFor(int side)
        {
            if (Session.Mode == GameMode.TwoPlayer || side != 0) return "trail_classic";
            string t = ServiceLocator.Profile.Data.EquippedTrail;
            return string.IsNullOrEmpty(t) ? "trail_classic" : t;
        }

        static bool IsTurnKind(MatchEventKind k)
        {
            switch (k)
            {
                case MatchEventKind.AbilityReady:
                case MatchEventKind.PlatformMoved:
                case MatchEventKind.ShieldRaised:
                case MatchEventKind.ShieldsRotated:
                case MatchEventKind.BurnDamage:
                case MatchEventKind.PoisonDamage:
                case MatchEventKind.Healed:
                case MatchEventKind.BubbleCast:
                case MatchEventKind.VineWallGrown:
                case MatchEventKind.Enraged:
                case MatchEventKind.WindChanged:
                case MatchEventKind.TurnStarted:
                case MatchEventKind.TurnTimedOut:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Shot events play at their times; everything from the first between-turns event on plays after the hold.</summary>
        void SplitEvents(IReadOnlyList<MatchEvent> events)
        {
            _shotEvents.Clear();
            _turnEvents.Clear();
            _deferred.Clear();
            bool turn = false;
            for (int i = 0; i < events.Count; i++)
            {
                MatchEvent e = events[i];
                if (!turn && IsTurnKind(e.Kind)) turn = true;
                if (e.Kind == MatchEventKind.MatchOver) continue;
                if (turn) _turnEvents.Add(e);
                else if (e.Kind == MatchEventKind.FighterEntered) _deferred.Add(e);
                else _shotEvents.Add(e);
            }
        }

        void RecordStats(ShotResult shot, float aimSeconds)
        {
            if (Session.Mode == GameMode.TwoPlayer) return;
            StatsRecorder.Record(ServiceLocator.Profile.Data, M, M.Events, shot, 0, aimSeconds);
            ServiceLocator.Save?.MarkDirty();
        }

        // ------------------------------------------------------------------ frame

        void Update()
        {
            if (Session == null) return;
            float dt = Time.deltaTime;
            for (int i = _delayT.Count - 1; i >= 0; i--)
            {
                _delayT[i] -= dt;
                if (_delayT[i] > 0f) continue;
                Action a = _delayA[i];
                _delayT.RemoveAt(i);
                _delayA.RemoveAt(i);
                a?.Invoke();
            }
            Roster?.Tick(dt);
            _drag.Enabled = _phase == Phase.Aim && !Paused && !Ui.HasModal && _started;
            _drag.PixelsPerDp = Ui.Canvas ? Ui.Canvas.scaleFactor : 1f;

            switch (_phase)
            {
                case Phase.Aim: AimFrame(dt); break;
                case Phase.Ai: AiFrame(dt); break;
                case Phase.Flight: FlightFrame(dt); break;
                case Phase.Resolve:
                    _hold -= dt;
                    FrameImpact();
                    if (_hold <= 0f)
                    {
                        _phase = Phase.Between;
                        StartCoroutine(AfterShot());
                    }
                    break;
            }
            if (_phase != Phase.Flight && _phase != Phase.Resolve) Arena.TrackClock(M.Clock);
        }

        void AimFrame(float dt)
        {
            Fighter f = M.CurrentFighter;
            ArcherView v = Roster.View(f.Index);
            // The first-launch tutorial never runs out of time: a beginner reads, tries, and cancels at leisure.
            if (!Ui.HasModal && !Paused && _tutorial == null && TickTimer(dt)) return;
            if (_drag.Dragging)
            {
                v.Aim(_drag.AngleDeg, _drag.InDeadZone ? 0f : _drag.Power);
                if (_drag.InDeadZone || (AbilityArmed && f.Def.Ability == AbilityKind.RainOfLeaves)) _preview.Hide();
                else ShowPreview(f, _drag.AngleDeg, _drag.Power, v);
            }
            FrameAim(false);
        }

        void ShowPreview(Fighter f, float angle, float power, ArcherView v)
        {
            ShotConfig cfg = M.Setup.Shot;
            TipDef tip = M.BuildShotTip(f, SelectedTip, AbilityArmed, f.MultiArrowLeft, _tipScratch);
            Vec2 origin = f.BowPosition(cfg);
            Vec2 vel = Ballistics.LaunchVelocity(angle, power, f.Facing, cfg, tip.SpeedScale);
            Vec2 acc = Ballistics.Acceleration(M.Wind, tip.GravityScale, cfg, tip.WindScale);
            Vector3 offset = v.ArrowTipWorld - WorldSprites.V(origin);
            if (offset.sqrMagnitude > 1f) offset = Vector3.zero;
            _preview.Show(origin, vel, acc, f.Feet.Y, Session.PreviewShare(f.Side), offset, Rig.Size / 4.4f);
        }

        /// <summary>Turn timer (Logic's clock). Returns true when the turn timed out and the flow moved on.</summary>
        bool TickTimer(float dt)
        {
            float before = TimerSeconds;
            bool timedOut = Session.Tick(dt);
            float left = TimerSeconds;
            if (left >= 0f && left <= 3.05f && Mathf.Ceil(left) < Mathf.Ceil(before) && Mathf.Ceil(left) != _timerWarned)
            {
                _timerWarned = Mathf.Ceil(left);
                ServiceLocator.Audio?.Play(SoundId.TimerTick);
                if (Session.IsHuman(M.CurrentSide)) ServiceLocator.Haptics?.Play(HapticId.TimerWarn);
            }
            if (!timedOut) return false;
            _drag.CancelDrag();
            _preview.Hide();
            _ai = null;
            SplitEvents(M.Events);
            RecordStats(null, 0f);
            _phase = Phase.Between;
            StartCoroutine(AfterShot());
            return true;
        }

        void AiFrame(float dt)
        {
            if (TickTimer(dt)) return;
            if (_ai == null) return;
            bool wasThinking = _ai.Thinking;
            _ai.Tick(dt, out float angle, out float draw);
            ArcherView v = Roster.View(M.CurrentFighter.Index);
            if (wasThinking && !_ai.Thinking) ServiceLocator.Audio?.Play(SoundId.AiAim, 0.8f);
            if (!_ai.Thinking) v.Aim(angle, draw);
            FrameAim(false);
            if (_ai.Done) Shoot(_ai.Decision.Input, 5f);
        }

        void FlightFrame(float dt)
        {
            _replayT += dt;
            Arrows.SetTime(_replayT);
            while (_nextShot < _shotEvents.Count && _shotEvents[_nextShot].Time <= _replayT)
            {
                _juice.OnShotEvent(_shotEvents[_nextShot]);
                _nextShot++;
            }
            Arena.TrackClock(_clockAtShot + Mathf.Min(_replayT, _duration));
            FrameFlight();
            if (_replayT >= _duration && _nextShot >= _shotEvents.Count)
            {
                _phase = Phase.Resolve;
                _hold = _koThisShot ? 1.0f : 0.7f;
            }
        }

        IEnumerator AfterShot()
        {
            if (Session.IsOver)
            {
                yield return FinishMatch();
                yield break;
            }
            if (_koThisShot && _deferred.Count > 0) yield return new WaitForSeconds(0.9f);
            foreach (MatchEvent e in _deferred) yield return Enter(e.Fighter);
            _deferred.Clear();

            // Logic already applied between-turn damage; show HP as it was before those events play. Archers
            // move (platforms, drops) together with the props at the end.
            Roster.SyncAll(true, false);
            for (int i = 0; i < _turnEvents.Count; i++)
            {
                MatchEvent e = _turnEvents[i];
                if (e.Kind == MatchEventKind.BurnDamage || e.Kind == MatchEventKind.PoisonDamage) Roster.ShowHeal(e.Fighter, e.Amount);
                else if (e.Kind == MatchEventKind.Healed) Roster.ShowDamage(e.Fighter, e.Amount);
            }
            _hud?.Refresh();
            Arena.SyncProps(true, M.Clock);
            Arrows.ClearAll();
            for (int i = 0; i < M.FighterCount; i++) Roster.View(i)?.ClearStuckArrows();

            for (int i = 0; i < _turnEvents.Count; i++)
            {
                MatchEvent e = _turnEvents[i];
                if (e.Kind == MatchEventKind.FighterEntered)
                {
                    yield return Enter(e.Fighter);
                    continue;
                }
                if (e.Kind == MatchEventKind.TurnStarted || e.Kind == MatchEventKind.WindChanged) continue;
                float wait = _juice.OnTurnEvent(e);
                _hud?.Refresh();
                if (wait > 0f) yield return new WaitForSeconds(wait);
            }
            _turnEvents.Clear();
            if (Session.IsOver)
            {
                yield return FinishMatch();
                yield break;
            }
            Roster.SyncAll(true);
            BeginTurn();
        }

        IEnumerator Enter(int fighter)
        {
            Roster.Enter(fighter);
            _banner.Show(Loc.F("hud_next_archer", M.GetFighter(fighter).Def.Name), null, 1.3f);
            ServiceLocator.Audio?.Play(SoundId.TurnStart);
            yield return new WaitForSeconds(1.2f);
        }

        // ------------------------------------------------------------------ camera

        void FrameOverview()
        {
            float minX = float.MaxValue, maxX = float.MinValue, ground = float.MaxValue, top = 2f;
            for (int i = 0; i < M.FighterCount; i++)
            {
                if (!Roster.Visible(i)) continue;
                Roster.Bounds(i, out float x, out float feet, out float t);
                minX = Mathf.Min(minX, x - 2f);
                maxX = Mathf.Max(maxX, x + 2f);
                ground = Mathf.Min(ground, feet);
                top = Mathf.Max(top, t);
            }
            for (int i = 0; i < M.PropCount; i++)
            {
                Vector3 c = WorldSprites.V(M.PropRestShape(i).Center);
                if (!M.GetProp(i).Alive) continue;
                minX = Mathf.Min(minX, c.x - 1f);
                maxX = Mathf.Max(maxX, c.x + 1f);
                top = Mathf.Max(top, c.y + 1f);
            }
            if (ground == float.MaxValue) ground = 0f;
            Rig.Frame(minX, maxX, ground, top, 0.6f, maxX, 0.5f);
        }

        void FrameAim(bool snap)
        {
            int side = M.CurrentSide;
            int me = Roster.VisibleOnSide(side);
            if (me < 0) return;
            Roster.Bounds(me, out float sx, out float sFeet, out float sTop);
            int facing = M.GetFighter(me).Facing;
            float minX = sx - 2.2f, maxX = sx + 2.2f, ground = sFeet, top = sTop;
            int foe = Roster.VisibleOnSide(1 - side);
            if (foe >= 0)
            {
                Roster.Bounds(foe, out float fx, out float fFeet, out float fTop);
                if (Mathf.Abs(fx - sx) <= 19f)
                {
                    minX = Mathf.Min(minX, fx - 2.2f);
                    maxX = Mathf.Max(maxX, fx + 2.2f);
                    ground = Mathf.Min(ground, fFeet);
                    top = Mathf.Max(top, fTop);
                }
                else
                {
                    // Long duel: keep the shooter near the edge and look towards the opponent.
                    if (facing > 0) maxX = sx + 16.5f;
                    else minX = sx - 16.5f;
                }
            }
            else
            {
                // Solo level: frame the goal props.
                for (int i = 0; i < M.PropCount; i++)
                {
                    Prop p = M.GetProp(i);
                    if (!p.Alive || (p.Kind != PropKind.Target && p.Kind != PropKind.Apple && p.Kind != PropKind.Rope)) continue;
                    Vector3 c = WorldSprites.V(M.PropShapeAt(i, M.Clock).Center);
                    if (Mathf.Abs(c.x - sx) > 26f) continue;
                    minX = Mathf.Min(minX, c.x - 1.2f);
                    maxX = Mathf.Max(maxX, c.x + 1.2f);
                    top = Mathf.Max(top, c.y + 0.8f);
                }
            }
            float lead = _drag != null && _drag.Dragging ? 0.4f : 0f;
            Rig.Frame(minX, maxX + lead, ground, top, 0.5f, facing > 0 ? minX + 9f : maxX - 9f);
            if (snap) Rig.Snap();
        }

        void FrameFlight()
        {
            if (_shooter < 0) return;
            Roster.Bounds(_shooter, out float sx, out float sFeet, out float sTop);
            float minX = sx - 2f, maxX = sx + 2f, ground = sFeet, top = sTop;
            float focus = sx;
            if (Arrows.Lead(out Vector3 a))
            {
                minX = Mathf.Min(minX, a.x - 2f);
                maxX = Mathf.Max(maxX, a.x + 2f);
                top = Mathf.Max(top, a.y + 0.8f);
                ground = Mathf.Min(ground, Mathf.Max(a.y - 2f, ground - 3f));
                focus = a.x;
            }
            int foe = Roster.VisibleOnSide(1 - M.GetFighter(_shooter).Side);
            if (foe >= 0)
            {
                Roster.Bounds(foe, out float fx, out float fFeet, out float fTop);
                minX = Mathf.Min(minX, fx - 2f);
                maxX = Mathf.Max(maxX, fx + 2f);
                ground = Mathf.Min(ground, fFeet);
            }
            Rig.Frame(minX, maxX, ground, top, 0.35f, focus);
        }

        void FrameImpact()
        {
            if (_shooter < 0) return;
            int foe = Roster.VisibleOnSide(1 - M.GetFighter(_shooter).Side);
            float minX = _impact.x - 3f, maxX = _impact.x + 3f, ground = Mathf.Min(0f, _impact.y - 1f), top = _impact.y + 1.5f;
            if (foe >= 0)
            {
                Roster.Bounds(foe, out float fx, out float fFeet, out float fTop);
                if (Mathf.Abs(fx - _impact.x) < 14f)
                {
                    minX = Mathf.Min(minX, fx - 2f);
                    maxX = Mathf.Max(maxX, fx + 2f);
                    ground = Mathf.Min(ground, fFeet);
                    top = Mathf.Max(top, fTop);
                }
            }
            Rig.Frame(minX, maxX, ground, top, 0.5f, _impact.x);
        }

        // ------------------------------------------------------------------ helpers for juice / HUD

        public void Delay(float seconds, Action action)
        {
            _delayT.Add(seconds);
            _delayA.Add(action);
        }

        public void Toast(string message) => Ui.Toast(message);

        public void ScreenFlash(Color c, float seconds)
        {
            if (ServiceLocator.Settings != null && ServiceLocator.Settings.ReduceMotion) c.a *= 0.4f;
            Tween.KillTarget(_flash);
            Tween.Value(c.a, 0f, seconds, a => { if (_flash) _flash.color = new Color(c.r, c.g, c.b, a); }, EaseType.OutQuad, 0f, true, null, _flash);
        }

        public void OnShownHpChanged(int fighter) => _hud?.OnHpChanged(fighter);

        /// <summary>Crackle loop while any archer on screen is burning.</summary>
        void UpdateLoops()
        {
            bool burning = false;
            for (int i = 0; i < M.FighterCount; i++)
            {
                Fighter f = M.GetFighter(i);
                if (f.IsAlive && Roster.Visible(i) && f.Status.IsBurning) burning = true;
            }
            ServiceLocator.Audio?.Burning(burning && !IsOverPhase);
        }

        // ------------------------------------------------------------------ pause

        void OnRootBack()
        {
            if (_phase == Phase.Result) return;
            if (!Paused) PauseGame();
        }

        public void PauseGame()
        {
            if (Paused || _phase == Phase.Result) return;
            Paused = true;
            _drag.CancelDrag();
            _preview.Hide();
            TimeScaleDriver.Paused = true;
            ServiceLocator.Audio?.Duck(true);
            Ui.ShowModal(new PauseModal(this));
        }

        public void ResumeGame()
        {
            Paused = false;
            TimeScaleDriver.Paused = false;
            ServiceLocator.Audio?.Duck(false);
        }

        public void RestartMatch()
        {
            ResumeGame();
            Ui.CloseAllModals();
            MatchRequest r = _request.Clone();
            r.Boosters = new BoosterKind[0];
            if (Session.Mode == GameMode.TwoPlayer) Session.Series.Rematch();
            else r.Seed = (ulong)DateTime.UtcNow.Ticks;
            StartSession(r);
        }

        bool _keepPaused;

        public void OpenSettings()
        {
            _keepPaused = true;
            Ui.CloseAllModals();
            _keepPaused = false;
            Ui.Push(new SettingsScreen());
        }

        /// <summary>The pause dialog closed: resume unless it only made room for Settings.</summary>
        public void PauseClosed()
        {
            if (!_keepPaused) ResumeGame();
        }

        /// <summary>Back on the HUD after Settings: the match is still paused.</summary>
        public void HudShown()
        {
            if (Paused && !Ui.HasModal) Ui.ShowModal(new PauseModal(this));
        }

        public void QuitMatch()
        {
            ResumeGame();
            TimeScaleDriver.ResetAll();
            ServiceLocator.Save?.SaveNow();
            SceneFlow.GoHome(HomeTargetFor(Session.Mode));
        }

        static HomeTarget HomeTargetFor(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Campaign: return HomeTarget.Map;
                case GameMode.QuickDuel: return HomeTarget.QuickDuelSetup;
                case GameMode.TwoPlayer: return HomeTarget.PvpSetup;
                case GameMode.Daily: return HomeTarget.Daily;
                case GameMode.Training: return HomeTarget.TrainingSetup;
                case GameMode.Survival: return HomeTarget.Survival;
                default: return HomeTarget.Home;
            }
        }

        public void GoHome(HomeTarget target)
        {
            TimeScaleDriver.ResetAll();
            ServiceLocator.Save?.SaveNow();
            SceneFlow.GoHome(target);
        }

        // ------------------------------------------------------------------ tutorial

        public void FinishTutorial()
        {
            if (_tutorial) Destroy(_tutorial.gameObject);
            _tutorial = null;
            ServiceLocator.Profile.Data.TutorialDone = true;
            ServiceLocator.CommitProfile();
        }

        /// <summary>Where the current archer stands on screen (tutorial hand, power ring).</summary>
        public Vector2 ShooterScreen(out Vector2 chest)
        {
            ArcherView v = Roster.View(M.CurrentFighter.Index);
            chest = v ? Rig.ToScreen(v.ChestWorld) : Vector2.zero;
            return v ? Rig.ToScreen(v.transform.position) : Vector2.zero;
        }

        // ------------------------------------------------------------------ end of match

        IEnumerator FinishMatch()
        {
            if (_phase == Phase.Over || _phase == Phase.Result) yield break;
            _phase = Phase.Over;
            ServiceLocator.Audio?.Burning(false);
            if (_tutorial) FinishTutorial();
            _drag.CancelDrag();
            _preview.Hide();
            int winner = M.Winner;
            for (int i = 0; i < M.FighterCount; i++)
            {
                Fighter f = M.GetFighter(i);
                ArcherView v = Roster.View(i);
                if (!v || !Roster.Visible(i)) continue;
                if (f.Side == winner && f.IsAlive) v.Victory();
                else if (!f.IsAlive && !v.IsKnockedOut) v.KnockOut();
            }
            int hero = Roster.VisibleOnSide(winner >= 0 ? winner : 0);
            if (hero >= 0)
            {
                Roster.Bounds(hero, out float x, out float feet, out float top);
                Rig.Frame(x - 4f, x + 4f, feet, top, 0.6f);
            }
            bool playerWon = Session.PlayerWon;
            if (Session.Mode == GameMode.TwoPlayer || playerWon)
            {
                ServiceLocator.Audio?.Play(SoundId.Cheer);
                if (FxSystem.Instance && hero >= 0)
                    FxSystem.Instance.Burst(ArtLibrary.Get(ArtLibrary.Fx, "confetti"), Roster.View(hero).HeadWorld + Vector3.up * 1.5f, 30, 6f, 1.6f,
                        new Color[] { new Color32(0xFF, 0xD2, 0x3F, 0xFF), new Color32(0xFF, 0x5F, 0xA2, 0xFF), new Color32(0x38, 0xBD, 0xF8, 0xFF), new Color32(0x2E, 0xD3, 0xA0, 0xFF) },
                        0.14f, 5f, 3f);
            }
            yield return new WaitForSeconds(1.6f);

            // Survival: a won wave continues the run.
            if (Session.Mode == GameMode.Survival)
            {
                SurvivalRun run = Session.Survival;
                run.EndWave();
                if (!run.IsOver)
                {
                    _banner.Show(Loc.F("sv_wave_clear", run.WavesCleared), Loc.F("hud_heal", SurvivalRun.HealBetweenWaves), 1.6f);
                    ServiceLocator.Audio?.Play(SoundId.StingVictory);
                    yield return new WaitForSeconds(1.8f);
                    Session.NextRound();
                    BuildRound();
                    yield break;
                }
            }
            // 2-Player: the series may go on.
            if (Session.Mode == GameMode.TwoPlayer) Session.Series.RecordRound(winner);

            MatchOutcome outcome = Record();
            _phase = Phase.Result;
            ServiceLocator.Audio?.Sting(outcome.Won || Session.Mode == GameMode.TwoPlayer ? SoundId.StingVictory : SoundId.StingDefeat);
            ServiceLocator.Audio?.StopMusic();
            Ui.CloseAllModals();
            Ui.ResetTo(new ResultScreen(this, outcome));
        }

        MatchOutcome Record()
        {
            Profile profile = ServiceLocator.Profile;
            var o = new MatchOutcome { Mode = Session.Mode, Won = Session.PlayerWon };
            Fighter me = M.GetFighter(0);
            string archer = me.Def.Id;
            switch (Session.Mode)
            {
                case GameMode.Campaign:
                {
                    LevelDef level = Session.LevelDef;
                    LevelResult res = Session.Level.Result();
                    CampaignOutcome co = profile.RecordLevel(level, res);
                    o.Won = res.Won;
                    o.ShowStars = res.Won;
                    o.Stars = res.Stars;
                    o.PreviousStars = co.PreviousBestStars;
                    o.Coins = co.Coins.Total;
                    o.Breakdown = co.Coins;
                    o.FirstClear = co.FirstClear;
                    o.ChestReady = co.ChestReady;
                    o.NewTip = co.NewTip;
                    o.NewArchers.AddRange(co.NewArchers);
                    o.Badges.AddRange(co.Badges);
                    o.Badges.AddRange(profile.RecordMatchEnd(GameMode.Campaign, res.Won, res.HpFraction, res.TookNoDamage, archer));
                    o.Title = res.Won ? Loc.T("res_victory") : Loc.T("res_defeat");
                    o.Subtitle = res.Won ? (level.IsDuelGoal ? Loc.T("res_duel_won") : Loc.T("res_cleared"))
                        : Session.Level.DummyHit ? Loc.T("res_dummy_sub") : level.IsDuelGoal ? Loc.T("res_defeat_sub") : Loc.T("res_failed_sub");
                    if (level.IsDuelGoal) o.Stats.Add(new[] { res.PlayerTurns.ToString(), Loc.T("res_turns") });
                    else o.Stats.Add(new[] { res.ArrowsUsed.ToString(), Loc.T("res_arrows") });
                    o.Stats.Add(new[] { res.Headshots.ToString(), Loc.T("res_headshots") });
                    o.Stats.Add(new[] { Mathf.RoundToInt((float)res.HpFraction * 100f) + "%", Loc.T("res_hp") });
                    if (!res.Won)
                    {
                        if (LossHelp.ShowTip(co.LossesInARow)) o.Tip = Loc.T(level.LossTip);
                        o.OfferAssist = LossHelp.OfferAssist(co.LossesInARow) && !_request.Assist;
                    }
                    o.HasNextLevel = res.Won && level.Number < WorldOne.LevelCount;
                    o.NextLevel = level.Number + 1;
                    break;
                }
                case GameMode.Daily:
                {
                    bool won = Session.Level.Outcome == LevelOutcome.Won;
                    DailyOutcome d = profile.RecordDaily(_request.Day, won);
                    o.Won = won;
                    o.Coins = d.Coins;
                    o.StreakDays = d.Streak;
                    o.CosmeticId = d.CosmeticId;
                    o.Badges.AddRange(d.Badges);
                    o.Badges.AddRange(profile.RecordMatchEnd(GameMode.Daily, won, me.HpFraction, !Session.TookDamage, archer));
                    o.Title = won ? Loc.T("res_victory") : Loc.T("res_defeat");
                    o.Subtitle = won ? Loc.T("res_daily_done") : Loc.T("res_defeat_sub");
                    o.Stats.Add(new[] { Session.ArrowsUsed.ToString(), Loc.T("res_arrows") });
                    o.Stats.Add(new[] { Session.Headshots.ToString(), Loc.T("res_headshots") });
                    if (won && d.Streak > 0) o.Stats.Add(new[] { d.Streak.ToString(), Loc.F("res_streak", d.Streak) });
                    break;
                }
                case GameMode.QuickDuel:
                {
                    bool won = Session.PlayerWon;
                    CoinBreakdown c = Rewards.QuickDuel(_request.Difficulty, won, Session.Headshots, profile.Economy);
                    profile.Earn(c.Total);
                    o.Coins = c.Total;
                    o.Breakdown = c;
                    o.Badges.AddRange(profile.RecordMatchEnd(GameMode.QuickDuel, won, me.HpFraction, !Session.TookDamage, archer));
                    string foe = M.GetFighter(M.FighterCount - 1).Def.Name;
                    o.Title = won ? Loc.T("res_victory") : Loc.T("res_defeat");
                    o.Subtitle = won ? Loc.F("res_quick_won", foe) : Loc.T("res_defeat_sub");
                    o.Stats.Add(new[] { Session.ArrowsUsed.ToString(), Loc.T("res_arrows") });
                    o.Stats.Add(new[] { Session.Headshots.ToString(), Loc.T("res_headshots") });
                    o.Stats.Add(new[] { Mathf.RoundToInt((float)me.HpFraction * 100f) + "%", Loc.T("res_hp") });
                    break;
                }
                case GameMode.Survival:
                {
                    SurvivalRun run = Session.Survival;
                    int best = profile.Data.SurvivalBestWave;
                    CoinBreakdown c = Rewards.Survival(run.WavesCleared, Session.Headshots, profile.Economy);
                    profile.Earn(c.Total);
                    o.Coins = c.Total;
                    o.Breakdown = c;
                    o.Waves = run.WavesCleared;
                    o.NewBest = run.WavesCleared > best;
                    o.Won = run.WavesCleared > 0;
                    o.Badges.AddRange(profile.RecordSurvival(run.WavesCleared, archer));
                    o.Title = Loc.T("sv_run_over");
                    o.Subtitle = Loc.F("sv_reached", Mathf.Max(1, run.Wave));
                    o.Stats.Add(new[] { run.WavesCleared.ToString(), Loc.T("res_waves") });
                    o.Stats.Add(new[] { Session.Headshots.ToString(), Loc.T("res_headshots") });
                    o.Stats.Add(new[] { Mathf.Max(best, run.WavesCleared).ToString(), Loc.T("ld_best") });
                    break;
                }
                case GameMode.TwoPlayer:
                {
                    PvpSeries s = Session.Series;
                    int lastWinnerSide = M.Winner;
                    o.PvpRoundOnly = !s.IsOver;
                    o.WinnerName = s.IsOver ? (s.Winner == 0 ? s.Settings.Name1 : s.Settings.Name2) : s.NameOnSide(lastWinnerSide);
                    o.Score = Loc.F("pvp_score", s.Settings.Name1, s.Wins(0), s.Wins(1), s.Settings.Name2);
                    o.Title = s.IsOver ? Loc.F("pvp_series_win", o.WinnerName) : Loc.F("pvp_round_win", o.WinnerName, s.Round);
                    o.Subtitle = s.IsOver ? Loc.T("pvp_great") : o.Score;
                    o.Won = true;
                    if (s.IsOver) o.Badges.AddRange(profile.RecordMatchEnd(GameMode.TwoPlayer, s.Winner == 0, me.HpFraction, false, s.Settings.Archer1));
                    break;
                }
            }
            ServiceLocator.CommitProfile();
            ServiceLocator.Save?.SaveNow();
            return o;
        }

        // ------------------------------------------------------------------ result actions

        public void NextLevel(int level)
        {
            MatchRequest r = _request.Clone();
            r.Level = level;
            r.Boosters = new BoosterKind[0];
            r.Assist = false;
            r.Seed = (ulong)DateTime.UtcNow.Ticks;
            StartSession(r);
        }

        public void Retry(bool assist)
        {
            MatchRequest r = _request.Clone();
            r.Boosters = new BoosterKind[0];
            r.Assist = assist || (_request.Assist && Session.Mode == GameMode.Campaign);
            r.Seed = Session.Mode == GameMode.Daily ? (ulong)_request.Day : (ulong)DateTime.UtcNow.Ticks;
            StartSession(r);
        }

        public void NextPvpRound()
        {
            Session.NextRound();
            BuildRound();
        }

        public void PvpRematch(bool swap)
        {
            if (swap) Session.Series.SwapSides();
            Session.Series.Rematch();
            MatchRequest r = _request.Clone();
            r.Seed = (ulong)DateTime.UtcNow.Ticks;
            StartSession(r);
        }

        public HomeTarget ResultHomeTarget => HomeTargetFor(Session.Mode);

        void OnApplicationPause(bool paused)
        {
            if (paused && _phase != Phase.Result && _phase != Phase.Loading && Session != null && !Paused) PauseGame();
        }

        void OnDestroy()
        {
            TimeScaleDriver.ResetAll();
            ServiceLocator.Audio?.Creak(false);
            ServiceLocator.Audio?.Burning(false);
        }
    }
}
