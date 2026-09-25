using ArcherArcade.Archers;
using ArcherArcade.Core;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Match;
using ArcherArcade.Theme;
using ArcherArcade.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Match HUD (design `match`, states 05 / 06; SCREEN_INVENTORY 11–13): pause, both HP panels with portraits
    /// and a gold ring on whoever's turn it is, the turn timer ring, the wind pill with bars, the arrow-tip slots
    /// with ammo, the turn label and the ability button with charge pips. Extras from GAME_DESIGN §12: goal /
    /// wave / round chip, hits and headshots counters, active boosters, status icons, Gauntlet dots, a power ring
    /// with angle while dragging. Left-handed mirrors the bottom bar; bigger targets scale the HUD 1.15×.
    /// </summary>
    public sealed class HudScreen : UiScreen
    {
        static readonly Color Navy = UiKit.Hex(0x2A2350);
        static readonly Color PanelBg = new Color(1f, 1f, 1f, 0.9f);
        static readonly Color Ring = UiKit.Hex(0xFFD23F);
        static readonly Color SlotRing = UiKit.Hex(0xFFC928);

        readonly MatchSceneRoot _r;

        readonly Image[] _panelRing = new Image[2];
        readonly ProgressBar[] _bar = new ProgressBar[2];
        readonly TextMeshProUGUI[] _name = new TextMeshProUGUI[2], _hp = new TextMeshProUGUI[2];
        readonly Image[] _avatar = new Image[2], _face = new Image[2];
        readonly RectTransform[] _status = new RectTransform[2];
        readonly RectTransform[] _panel = new RectTransform[2];
        RectTransform _gauntlet;
        TextMeshProUGUI _timer, _wind, _label, _goal, _counters;
        Image _timerRing, _timerDisc;
        RectTransform _timerRt, _goalRt, _labelRt, _slotsRt, _boostRt;
        readonly Image[] _windBars = new Image[5];
        Button3D _ability;
        Image _abilityRing;
        CanvasGroup _abilityGroup;
        readonly Image[] _pips = new Image[4];
        TextMeshProUGUI _abilityName, _abilityIcon;
        RectTransform _abilityRt;
        RectTransform _aimLayer, _powerRt, _dragStartRt, _dragLineRt;
        Image _powerRing;
        TextMeshProUGUI _powerText;

        Button3D[] _slots = new Button3D[0];
        ArrowTip[] _slotTips = new ArrowTip[0];
        TextMeshProUGUI[] _slotText = new TextMeshProUGUI[0];
        Image[] _slotRing = new Image[0];
        int[] _slotAmmo = new int[0];
        CanvasGroup[] _slotGroup = new CanvasGroup[0];
        int _slotsFor = -1;

        readonly int[] _shownFighter = { -2, -2 };
        readonly int[] _shownHp = { -1, -1 };
        readonly int[] _shownStatus = { -1, -1 };
        int _shownTimer = -99, _shownWind = -99, _shownCharge = -1, _shownPower = -1, _shownAngle = -999;
        int _labelKey = int.MinValue, _goalKey = int.MinValue, _countersKey = int.MinValue;
        bool _shownArmed, _shownReady;
        float _t;

        public HudScreen(MatchSceneRoot root) => _r = root;

        public override bool SeeThrough => true;
        public override bool ShowCoins => false;

        bool LeftHanded => ServiceLocator.Settings != null && ServiceLocator.Settings.LeftHanded;
        bool Big => ServiceLocator.Settings != null && ServiceLocator.Settings.BiggerTargets;
        bool ColorBlind => ServiceLocator.Settings != null && ServiceLocator.Settings.ColorBlind;

        // ------------------------------------------------------------------ build

        public override void Build(RectTransform root)
        {
            for (int i = 0; i < 2; i++)
            {
                _shownFighter[i] = -2;
                _shownHp[i] = -1;
            }
            _shownTimer = _shownWind = -99;
            _shownCharge = _shownPower = -1;
            _shownAngle = -999;
            _labelKey = _goalKey = _countersKey = int.MinValue;
            _shownStatus[0] = _shownStatus[1] = -1;
            _slotsFor = -1;
            _settingsSig = SettingsSignature();
            _rebuild = false;
            float scale = Big ? 1.15f : 1f;

            RectTransform top = UiKit.Rect(root, "Top");
            UiKit.TopBand(top, 12f, 110f, 12f, 12f);

            // Pause.
            Button3D pause = Button3D.Create(top, "Pause", new Color(1f, 1f, 1f, 0.92f), new Color(0f, 0f, 0f, 0.18f), 14f, 4f, 3f);
            UiKit.TopLeft((RectTransform)pause.transform, 0f, 0f, 44f, 44f);
            pause.AddIcon(Icons.Pause, 22f, Navy);
            pause.OnClick(() => _r.PauseGame());
            pause.transform.localScale = Vector3.one * scale;

            BuildPanel(top, 0);
            BuildPanel(top, 1);

            // Timer + wind (centre column).
            RectTransform mid = UiKit.Rect(top, "Centre");
            UiKit.At(mid, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(200f, 110f));
            _timerRt = UiKit.Rect(mid, "Timer");
            UiKit.At(_timerRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -27f), new Vector2(54f, 54f));
            Image tshadow = UiKit.Disc(_timerRt, "Shadow", new Color(0f, 0f, 0f, 0.15f));
            UiKit.Stretch(tshadow.rectTransform, 2f, 6f, 2f, -2f);
            _timerDisc = UiKit.Disc(_timerRt, "Disc", new Color(1f, 1f, 1f, 0.95f));
            UiKit.Stretch(_timerDisc.rectTransform, 2f, 2f, 2f, 2f);
            _timerRing = UiKit.Box(_timerRt, "Ring", Ring, 0f);
            _timerRing.sprite = ShapeSprites.Ring;
            _timerRing.type = Image.Type.Filled;
            _timerRing.fillMethod = Image.FillMethod.Radial360;
            _timerRing.fillOrigin = (int)Image.Origin360.Top;
            _timerRing.fillClockwise = false;
            UiKit.Stretch(_timerRing.rectTransform);
            _timer = UiKit.Label(_timerRt, "12", FontRole.Display, 23f, Navy, TextAlignmentOptions.Center);
            UiKit.Stretch((RectTransform)_timer.transform);

            Image windBg = UiKit.Box(mid, "Wind", new Color(Navy.r, Navy.g, Navy.b, 0.78f), 10f);
            UiKit.At(windBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -59f), new Vector2(150f, 24f));
            UiKit.Row(windBg, 5f, TextAnchor.MiddleCenter, false, false, new RectOffset(8, 8, 0, 0));
            TextMeshProUGUI wi = UiKit.Glyph(windBg.transform, Icons.Air, 14f, Color.white);
            UiKit.Size(wi, 16f, 20f);
            _wind = UiKit.Label(windBg.transform, "", FontRole.BodyBold, 12f, Color.white, TextAlignmentOptions.Center);
            UiKit.Size(_wind, 62f, 20f);
            RectTransform bars = UiKit.Rect(windBg.transform, "Bars");
            UiKit.Size(bars, 34f, 12f);
            UiKit.Row(bars, 2f, TextAnchor.LowerLeft, false, false);
            for (int i = 0; i < 5; i++)
            {
                _windBars[i] = UiKit.Box(bars, "Bar" + i, new Color(1f, 1f, 1f, 0.25f), 1.5f);
                UiKit.Size(_windBars[i], 5f, 4f + i * 2f);
            }

            Image goalBg = UiKit.Box(mid, "Goal", new Color(1f, 1f, 1f, 0.88f), 10f);
            _goalRt = goalBg.rectTransform;
            UiKit.At(_goalRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(180f, 20f));
            _goal = UiKit.Label(goalBg.transform, "", FontRole.BodyBold, 11f, Navy, TextAlignmentOptions.Center);
            UiKit.Stretch((RectTransform)_goal.transform, 6f, 0f, 6f, 0f);

            // Bottom bar.
            RectTransform bottom = UiKit.Rect(root, "Bottom");
            UiKit.BottomBand(bottom, 12f, 110f, 14f, 14f);
            bool lh = LeftHanded;
            _slotsRt = UiKit.Rect(bottom, "Slots");
            UiKit.At(_slotsRt, new Vector2(lh ? 1f : 0f, 0f), new Vector2(lh ? 1f : 0f, 0f), Vector2.zero, new Vector2(300f, 58f));
            UiKit.Row(_slotsRt, 7f, lh ? TextAnchor.LowerRight : TextAnchor.LowerLeft, false, false);
            _slotsRt.localScale = Vector3.one * scale;

            Image labelBg = UiKit.Box(bottom, "TurnLabel", new Color(Navy.r, Navy.g, Navy.b, 0.8f), 14f);
            _labelRt = labelBg.rectTransform;
            UiKit.At(_labelRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(260f, 32f));
            _label = UiKit.Label(labelBg.transform, "", FontRole.BodyBold, 13f, Color.white, TextAlignmentOptions.Center);
            UiKit.Stretch((RectTransform)_label.transform, 12f, 0f, 12f, 0f);

            BuildAbility(bottom, lh, scale);

            // Aim layer: power ring round the archer and the drag marker.
            _aimLayer = UiKit.Rect(root, "Aim");
            UiKit.Stretch(_aimLayer);
            _aimLayer.SetAsFirstSibling();
            _dragLineRt = UiKit.Box(_aimLayer, "DragLine", new Color(1f, 1f, 1f, 0.7f), 0f).rectTransform;
            _dragLineRt.pivot = new Vector2(0f, 0.5f);
            _dragStartRt = UiKit.Rect(_aimLayer, "DragStart");
            _dragStartRt.sizeDelta = new Vector2(26f, 26f);
            Image ds = UiKit.Box(_dragStartRt, "Ring", new Color(1f, 1f, 1f, 0.9f), 0f);
            ds.sprite = ShapeSprites.Ring;
            UiKit.Stretch(ds.rectTransform);
            _powerRt = UiKit.Rect(_aimLayer, "Power");
            _powerRt.sizeDelta = new Vector2(120f, 120f);
            Image track = UiKit.Box(_powerRt, "Track", new Color(1f, 1f, 1f, 0.25f), 0f);
            track.sprite = ShapeSprites.Ring;
            UiKit.Stretch(track.rectTransform);
            _powerRing = UiKit.Box(_powerRt, "Fill", Widgets.Green, 0f);
            _powerRing.sprite = ShapeSprites.Ring;
            _powerRing.type = Image.Type.Filled;
            _powerRing.fillMethod = Image.FillMethod.Radial360;
            _powerRing.fillOrigin = (int)Image.Origin360.Bottom;
            UiKit.Stretch(_powerRing.rectTransform);
            _powerText = UiKit.Label(_powerRt, "", FontRole.Display, 15f, Color.white, TextAlignmentOptions.Center);
            ((RectTransform)_powerText.transform).anchorMin = ((RectTransform)_powerText.transform).anchorMax = new Vector2(0.5f, 1f);
            ((RectTransform)_powerText.transform).anchoredPosition = new Vector2(0f, 14f);
            ((RectTransform)_powerText.transform).sizeDelta = new Vector2(160f, 22f);
            _powerText.outlineWidth = 0.25f;
            _powerText.outlineColor = new Color32(0x2A, 0x23, 0x50, 0xFF);
            _aimLayer.gameObject.SetActive(false);
            RefreshAll();
        }

        void BuildPanel(RectTransform top, int side)
        {
            bool right = side == 1;
            RectTransform panel = UiKit.Rect(top, "Panel" + side);
            _panel[side] = panel;
            if (right) UiKit.At(panel, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(220f, 48f));
            else UiKit.TopLeft(panel, 54f, 0f, 220f, 48f);
            if (Big) panel.localScale = Vector3.one * 1.08f;
            UiKit.SoftShadow(panel, new Color(0f, 0f, 0f, 0.12f), 16f, 12f, 4f);
            _panelRing[side] = UiKit.Box(panel, "Ring", Ring, 0f);
            _panelRing[side].sprite = ShapeSprites.Outline(19f, 3f);
            _panelRing[side].type = Image.Type.Sliced;
            UiKit.Stretch(_panelRing[side].rectTransform, -3f, -3f, -3f, -3f);
            Image bg = UiKit.Box(panel, "Bg", PanelBg, 16f);
            UiKit.Stretch(bg.rectTransform);

            RectTransform av = UiKit.Rect(panel, "Avatar");
            UiKit.At(av, new Vector2(right ? 1f : 0f, 0.5f), new Vector2(right ? 1f : 0f, 0.5f), new Vector2(right ? -6f : 6f, 0f), new Vector2(36f, 36f));
            _avatar[side] = UiKit.Disc(av, "Disc", Widgets.Purple);
            UiKit.Stretch(_avatar[side].rectTransform);
            // Round portrait: the disc masks the face (design: 36 dp circle in the archer's colour).
            _avatar[side].gameObject.AddComponent<Mask>().showMaskGraphic = true;
            _face[side] = UiKit.Box(_avatar[side].transform, "Face", Color.white, 0f);
            _face[side].preserveAspect = true;
            UiKit.Stretch(_face[side].rectTransform, -2f, -1f, -2f, -5f);

            RectTransform words = UiKit.Rect(panel, "Words");
            UiKit.Stretch(words, right ? 10f : 50f, 6f, right ? 50f : 10f, 6f);
            _name[side] = UiKit.Label(words, "", FontRole.Display, 13f, Navy, right ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft);
            UiKit.Stretch((RectTransform)_name[side].transform, right ? 40f : 0f, 0f, right ? 0f : 40f, 18f);
            UiKit.Fit(_name[side], 0.7f);
            _hp[side] = UiKit.Label(words, "", FontRole.Display, 13f, Navy, right ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.TopRight);
            UiKit.Stretch((RectTransform)_hp[side].transform, 0f, 0f, 0f, 18f);
            Color fill = HpColor(side);
            _bar[side] = ProgressBar.Create(words, new Color(Navy.r, Navy.g, Navy.b, 0.12f), fill, 10f, 5f, right, true);
            RectTransform br = (RectTransform)_bar[side].transform;
            br.anchorMin = new Vector2(0f, 0f);
            br.anchorMax = new Vector2(1f, 0f);
            br.pivot = new Vector2(0.5f, 0f);
            br.offsetMin = new Vector2(0f, 2f);
            br.offsetMax = new Vector2(0f, 12f);

            _status[side] = UiKit.Rect(top, "Status" + side);
            if (right) UiKit.At(_status[side], new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -54f), new Vector2(220f, 22f));
            else UiKit.TopLeft(_status[side], 54f, 54f, 220f, 22f);
            UiKit.Row(_status[side], 4f, right ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, false, false);

            if (right)
            {
                _gauntlet = UiKit.Rect(top, "Gauntlet");
                UiKit.At(_gauntlet, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-232f, -4f), new Vector2(40f, 10f));
                UiKit.Row(_gauntlet, 3f, TextAnchor.MiddleRight, false, false);
            }
            else
            {
                _boostRt = UiKit.Rect(top, "Counters");
                UiKit.TopLeft(_boostRt, 0f, 52f, 50f, 60f);
            }
        }

        void BuildAbility(RectTransform bottom, bool lh, float scale)
        {
            _abilityRt = UiKit.Rect(bottom, "Ability");
            UiKit.At(_abilityRt, new Vector2(lh ? 0f : 1f, 0f), new Vector2(lh ? 0f : 1f, 0f), Vector2.zero, new Vector2(90f, 106f));
            _abilityRt.localScale = Vector3.one * scale;
            _abilityGroup = _abilityRt.gameObject.AddComponent<CanvasGroup>();
            _ability = Button3D.Create(_abilityRt, "Button", Widgets.Purple, new Color(0f, 0f, 0f, 0.2f), 35f, 5f, 4f);
            UiKit.At((RectTransform)_ability.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(70f, 70f));
            _ability.Face.sprite = ShapeSprites.Circle;
            _ability.Face.type = Image.Type.Simple;
            if (_ability.Edge)
            {
                _ability.Edge.sprite = ShapeSprites.Circle;
                _ability.Edge.type = Image.Type.Simple;
            }
            _abilityRing = UiKit.Box(_ability.Body, "Ring", Color.white, 0f);
            _abilityRing.sprite = ShapeSprites.Ring;
            UiKit.Stretch(_abilityRing.rectTransform, -1f, -1f, -1f, -1f);
            _abilityIcon = _ability.AddIcon(Icons.Target, 30f, UiKit.Hex(0x1D1840));
            _ability.OnClick(() => _r.ToggleAbility());
            RectTransform pips = UiKit.Rect(_abilityRt, "Pips");
            UiKit.At(pips, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(70f, 6f));
            UiKit.Row(pips, 3f, TextAnchor.MiddleCenter, false, false);
            for (int i = 0; i < _pips.Length; i++)
            {
                _pips[i] = UiKit.Box(pips, "Pip" + i, new Color(1f, 1f, 1f, 0.5f), 3f);
                UiKit.Size(_pips[i], 14f, 6f);
            }
            Image nameBg = UiKit.Box(_abilityRt, "Name", new Color(Navy.r, Navy.g, Navy.b, 0.78f), 7f);
            UiKit.At(nameBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(96f, 17f));
            _abilityName = UiKit.Label(nameBg.transform, "", FontRole.BodyBold, 10.5f, Color.white, TextAlignmentOptions.Center);
            UiKit.Stretch((RectTransform)_abilityName.transform, 4f, 0f, 4f, 0f);
            UiKit.Fit(_abilityName, 0.7f);
        }

        Color HpColor(int side)
        {
            if (ColorBlind) return UiKit.Hex(side == 0 ? 0x1C9AD6u : 0xF59E0Bu);
            return UiKit.Hex(side == 0 ? 0x12A67Au : 0xE5484Du);
        }

        // ------------------------------------------------------------------ updates

        int _settingsSig;
        bool _rebuild;

        static int SettingsSignature()
        {
            var s = ServiceLocator.Settings;
            if (s == null) return 0;
            return (s.LeftHanded ? 1 : 0) | (s.BiggerTargets ? 2 : 0) | (s.ColorBlind ? 4 : 0) | (s.ReduceMotion ? 8 : 0);
        }

        public override void OnShow()
        {
            // Back from Settings: rebuild once if the layout options changed (left-handed, bigger targets…).
            if (SettingsSignature() != _settingsSig) _rebuild = true;
            _r.HudShown();
        }

        public override bool OnBack()
        {
            _r.PauseGame();
            return true;
        }

        /// <summary>Everything, now (after a build or a big change).</summary>
        public void Refresh() => RefreshAll();

        void RefreshAll()
        {
            if (Root == null || _r.Session == null || _r.Roster == null) return;
            _labelKey = _goalKey = _countersKey = int.MinValue;
            _shownStatus[0] = _shownStatus[1] = -1;
            _shownCharge = -1;
            for (int s = 0; s < 2; s++) _shownFighter[s] = -2;
            RebuildSlots();
            UpdateAll(false);
        }

        public void OnTurnStarted()
        {
            RefreshAll();
            if (ServiceLocator.Settings != null && ServiceLocator.Settings.ReduceMotion) return;
            int side = _r.M.CurrentSide;
            if (_panel[side]) Tween.Scale(_panel[side], Vector3.one * 1.08f, Vector3.one * (Big ? 1.08f : 1f), 0.3f, EaseType.OutBack);
        }

        public void OnHpChanged(int fighter)
        {
            if (Root == null) return;
            int side = _r.M.GetFighter(fighter).Side;
            UpdatePanel(side, true);
            if (_panel[side] && (ServiceLocator.Settings == null || !ServiceLocator.Settings.ReduceMotion))
            {
                RectTransform p = _panel[side];
                Tween.KillTarget(p, true);
                Vector2 home = p.anchoredPosition;
                Tween.Value(0f, 1f, 0.3f, k => { if (p) p.anchoredPosition = home + new Vector2(Mathf.Sin(k * 40f) * 5f * (1f - k), 0f); },
                    EaseType.Linear, 0f, true, () => { if (p) p.anchoredPosition = home; }, p);
            }
        }

        void UpdateAll(bool animate)
        {
            UpdatePanel(0, animate);
            UpdatePanel(1, animate);
            UpdateTimer();
            UpdateWind();
            UpdateLabelAndGoal();
            UpdateSlots();
            UpdateAbility();
        }

        void UpdatePanel(int side, bool animate)
        {
            MatchState m = _r.M;
            int f = _r.Roster.VisibleOnSide(side);
            if (m.IsSolo && side == 1)
            {
                _panel[1].gameObject.SetActive(false);
                _status[1].gameObject.SetActive(false);
                return;
            }
            _panel[side].gameObject.SetActive(f >= 0);
            if (f < 0) return;
            Fighter fighter = m.GetFighter(f);
            if (_shownFighter[side] != f)
            {
                _shownFighter[side] = f;
                _shownHp[side] = -1;
                _name[side].text = _r.NameOnSide(side);
                _avatar[side].color = UiKit.Hex(ArcherLooks.Color(fighter.Def.Id));
                _face[side].sprite = ArtLibrary.Portrait(_r.Session.Looks[f]);
                animate = false;
                RebuildGauntlet();
            }
            int hp = _r.Roster.ShownHp(f);
            if (hp != _shownHp[side])
            {
                bool drop = _shownHp[side] >= 0 && hp < _shownHp[side];
                _shownHp[side] = hp;
                _hp[side].text = hp.ToString();
                _bar[side].SetValue((float)hp / Mathf.Max(1, fighter.MaxHp), animate);
                string expr = hp <= 0 ? "hurt" : drop ? "hurt" : (float)hp / fighter.MaxHp < 0.35f ? "hurt" : "normal";
                Sprite face = ArtLibrary.Portrait(_r.Session.Looks[f], expr);
                if (face) _face[side].sprite = face;
            }
            bool turn = !_r.IsOverPhase && m.CurrentSide == side && m.Phase == MatchPhase.Aiming;
            _panelRing[side].enabled = turn;
            UpdateStatus(side, fighter);
        }

        void UpdateStatus(int side, Fighter f)
        {
            RectTransform s = _status[side];
            s.gameObject.SetActive(true);
            int mask = (f.Status.IsBurning ? 1 : 0) | (f.Status.IsPoisoned ? 2 : 0) | (f.Status.PendingStunSeconds > 0.0 ? 4 : 0) |
                (f.Status.PendingDrawSlow > 0.0 || f.Status.ActiveDrawSlow > 0.0 ? 8 : 0) | (f.HasBubble ? 16 : 0) | (f.HelmetLeft ? 32 : 0) |
                (f.MultiArrowLeft ? 64 : 0) | (f.Enraged ? 128 : 0) | (f.Index << 8);
            if (mask == _shownStatus[side]) return;
            _shownStatus[side] = mask;
            UiKit.Clear(s);
            if (f.Status.IsBurning) StatusIcon(s, Icons.LocalFireDepartment, ElementColors.Main(Element.Fire));
            if (f.Status.IsPoisoned) StatusIcon(s, Icons.Science, ElementColors.Main(Element.Poison));
            if (f.Status.PendingStunSeconds > 0.0) StatusIcon(s, Icons.ElectricBolt, ElementColors.Main(Element.Electric));
            if (f.Status.PendingDrawSlow > 0.0 || f.Status.ActiveDrawSlow > 0.0) StatusIcon(s, Icons.AcUnit, ElementColors.Main(Element.Ice));
            if (f.HasBubble) StatusIcon(s, Icons.BubbleChart, UiKit.Hex(0x38BDF8));
            if (f.HelmetLeft) StatusIcon(s, Icons.Shield, UiKit.Hex(0x9AA3B5));
            if (f.MultiArrowLeft) StatusIcon(s, Icons.CallSplit, UiKit.Hex(0xFF5FA2));
            if (f.Enraged) StatusIcon(s, Icons.Skull, UiKit.Hex(0xE5484D));
        }

        static void StatusIcon(RectTransform parent, string glyph, Color c)
        {
            Image bg = UiKit.Disc(parent, "S", new Color(1f, 1f, 1f, 0.92f));
            UiKit.Size(bg, 20f, 20f);
            TextMeshProUGUI t = UiKit.Glyph(bg.transform, glyph, 13f, c);
            UiKit.Stretch((RectTransform)t.transform);
        }

        void RebuildGauntlet()
        {
            if (!_gauntlet) return;
            UiKit.Clear(_gauntlet);
            MatchState m = _r.M;
            int count = 0;
            for (int i = 0; i < m.FighterCount; i++) if (m.GetFighter(i).Side == 1) count++;
            if (count <= 1) return;
            for (int i = 0; i < m.FighterCount; i++)
            {
                Fighter f = m.GetFighter(i);
                if (f.Side != 1) continue;
                Image d = UiKit.Disc(_gauntlet, "Dot", f.IsAlive || _r.Roster.Visible(i) ? Color.white : new Color(1f, 1f, 1f, 0.3f));
                UiKit.Size(d, 9f, 9f);
            }
        }

        void UpdateTimer()
        {
            float left = _r.TimerSeconds;
            if (left < 0f)
            {
                if (_shownTimer != -1)
                {
                    _shownTimer = -1;
                    _timer.text = "∞";
                    _timerRing.fillAmount = 1f;
                }
                return;
            }
            int whole = Mathf.CeilToInt(left);
            double total = _r.M.Setup.Rules.TurnSeconds;
            _timerRing.fillAmount = total > 0 ? Mathf.Clamp01(left / (float)total) : 0f;
            if (whole != _shownTimer)
            {
                _shownTimer = whole;
                _timer.text = whole.ToString();
                bool warn = whole <= 3 && _r.Aiming;
                _timer.color = warn ? Widgets.Red : Navy;
                _timerRing.color = warn ? Widgets.Red : whole <= 6 ? UiKit.Hex(0xFF8A3D) : Ring;
                if (warn && (ServiceLocator.Settings == null || !ServiceLocator.Settings.ReduceMotion))
                    Tween.Scale(_timerRt, Vector3.one * 1.18f, Vector3.one, 0.3f, EaseType.OutBack);
            }
        }

        void UpdateWind()
        {
            int wind = _r.M.Wind;
            if (wind == _shownWind) return;
            _shownWind = wind;
            int n = Mathf.Abs(wind);
            _wind.text = n == 0 ? Loc.T("hud_no_wind") : wind < 0 ? "← " + n : n + " →";
            for (int i = 0; i < _windBars.Length; i++)
                _windBars[i].color = i < n ? (n >= 4 ? UiKit.Hex(0xFF8A3D) : Color.white) : new Color(1f, 1f, 1f, 0.25f);
        }

        void UpdateLabelAndGoal()
        {
            int lk = _r.LabelKey;
            if (lk != _labelKey)
            {
                _labelKey = lk;
                _label.text = _r.TurnLabel;
                float w = Mathf.Clamp(_label.preferredWidth + 28f, 140f, 360f);
                _labelRt.sizeDelta = new Vector2(w, 32f);
            }
            int gk = GoalKey();
            if (gk != _goalKey)
            {
                _goalKey = gk;
                string goal = GoalText();
                _goalRt.gameObject.SetActive(!string.IsNullOrEmpty(goal));
                _goal.text = goal ?? "";
                _goalRt.sizeDelta = new Vector2(Mathf.Clamp(_goal.preferredWidth + 18f, 60f, 240f), 20f);
            }
            int ck = _r.Session.Hits * 1000 + _r.Session.Headshots;
            if (ck != _countersKey && _boostRt)
            {
                _countersKey = ck;
                string counters = CountersText();
                UiKit.Clear(_boostRt);
                if (!string.IsNullOrEmpty(counters))
                {
                    Image bg = UiKit.Box(_boostRt, "Chip", new Color(Navy.r, Navy.g, Navy.b, 0.7f), 9f);
                    UiKit.TopLeft(bg.rectTransform, 0f, 0f, 50f, 38f);
                    TextMeshProUGUI t = UiKit.Label(bg.transform, counters, FontRole.BodyBold, 10.5f, Color.white, TextAlignmentOptions.Center);
                    t.lineSpacing = -10f;
                    UiKit.Stretch((RectTransform)t.transform, 2f, 2f, 2f, 2f);
                }
            }
        }

        /// <summary>Changes whenever the goal chip's text would (no string building per frame).</summary>
        int GoalKey()
        {
            MatchSession s = _r.Session;
            int k = (int)s.Mode * 7919 + (int)Loc.Current * 104729;
            if (s.Level != null) k += s.Level.TargetsHit * 31 + s.Level.ApplesHit * 131 + s.Level.ArrowsUsed * 1031 + s.RoundNumber;
            for (int i = 0; i < s.Match.FighterCount; i++) if (!s.Match.GetFighter(i).IsAlive) k += 17 << i;
            if (s.Series != null) k += s.Series.Round * 37 + s.Series.Wins(0) * 101 + s.Series.Wins(1) * 211;
            if (s.Survival != null) k += s.Survival.Wave * 53;
            if (s.Training != null) k += s.Training.Shots * 59 + s.Training.Hits * 61 + s.Training.Bullseyes * 67;
            return k;
        }

        string GoalText()
        {
            MatchSession s = _r.Session;
            switch (s.Mode)
            {
                case GameMode.Campaign:
                case GameMode.Daily:
                {
                    LevelDef l = s.LevelDef;
                    LevelRun run = s.Level;
                    switch (l.Goal)
                    {
                        case GoalKind.Targets:
                        case GoalKind.TrickShot:
                            return Fmt.Tint("◎ ", Widgets.Red) + Loc.F("goal_progress", run.TargetsHit, l.TargetCount) + "  ·  " + Loc.F("goal_arrows_left", run.ArrowsLeft);
                        case GoalKind.AppleShot:
                            return Fmt.Tint("● ", Widgets.Red) + Loc.F("goal_progress", run.ApplesHit, l.TargetCount) + "  ·  " + Loc.F("goal_arrows_left", run.ArrowsLeft);
                        case GoalKind.Rescue:
                            return Loc.T("goal_rescue");
                        case GoalKind.Gauntlet:
                        {
                            int beaten = 0, total = 0;
                            for (int i = 0; i < s.Match.FighterCount; i++)
                            {
                                Fighter f = s.Match.GetFighter(i);
                                if (f.Side != 1) continue;
                                total++;
                                if (!f.IsAlive) beaten++;
                            }
                            return Loc.F("goal_progress", beaten, total) + " · " + Loc.F("goal_gauntlet", total);
                        }
                    }
                    return null;
                }
                case GameMode.TwoPlayer:
                    return Loc.F("hud_round", s.Series.Round + 1, s.Series.Wins(0), s.Series.Wins(1));
                case GameMode.Survival:
                    return Loc.F("hud_wave", s.Survival != null ? s.Survival.Wave : 1);
                case GameMode.Training:
                    return s.Training != null ? Loc.F("tr_stats", s.Training.Hits, s.Training.Shots, s.Training.Bullseyes) : null;
            }
            return null;
        }

        string CountersText()
        {
            MatchSession s = _r.Session;
            if (s.Mode == GameMode.TwoPlayer || s.Mode == GameMode.Training) return null;
            return Fmt.Tint("◎", Widgets.Gold) + " " + s.Hits + "\n" + Fmt.Tint("★", Widgets.Gold) + " " + s.Headshots;
        }

        // ---------- tip slots ----------

        void RebuildSlots()
        {
            MatchState m = _r.M;
            int side = m.CurrentSide;
            bool human = _r.Session.IsHuman(side);
            int f = human ? m.CurrentFighter.Index : m.ActiveFighter(0);
            if (f < 0) f = 0;
            if (f == _slotsFor && _slots.Length > 0) return;
            _slotsFor = f;
            UiKit.Clear(_slotsRt);
            Fighter fighter = m.GetFighter(f);
            var tips = new System.Collections.Generic.List<ArrowTip>();
            if (fighter.Spec.NormalArrows) tips.Add(ArrowTip.Normal);
            if (fighter.Spec.Tips != null)
                foreach (ArrowTip t in fighter.Spec.Tips)
                    if (t != ArrowTip.Normal && !tips.Contains(t)) tips.Add(t);
            _slotTips = tips.ToArray();
            _slots = new Button3D[_slotTips.Length];
            _slotText = new TextMeshProUGUI[_slotTips.Length];
            _slotRing = new Image[_slotTips.Length];
            _slotAmmo = new int[_slotTips.Length];
            _slotGroup = new CanvasGroup[_slotTips.Length];
            for (int i = 0; i < _slotAmmo.Length; i++) _slotAmmo[i] = int.MinValue;
            for (int i = 0; i < _slotTips.Length; i++)
            {
                ArrowTip tip = _slotTips[i];
                Button3D b = Button3D.Create(_slotsRt, "Slot" + tip, new Color(1f, 1f, 1f, 0.92f), Color.clear, 14f, 0f, 2f);
                UiKit.Size(b, 60f, 54f);
                Image ring = UiKit.Box(b.Body, "Ring", SlotRing, 0f);
                ring.sprite = ShapeSprites.Outline(14f, 3f);
                ring.type = Image.Type.Sliced;
                UiKit.Stretch(ring.rectTransform);
                _slotRing[i] = ring;
                TextMeshProUGUI icon = UiKit.Glyph(b.Body, GameVisuals.TipIcon(tip), 18f, GameVisuals.TipColor(tip));
                UiKit.At((RectTransform)icon.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(26f, 24f));
                TextMeshProUGUI text = UiKit.Label(b.Body, "", FontRole.BodyBold, 10f, Navy, TextAlignmentOptions.Center);
                UiKit.At((RectTransform)text.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(58f, 16f));
                UiKit.Fit(text, 0.7f);
                _slotText[i] = text;
                _slotGroup[i] = b.gameObject.AddComponent<CanvasGroup>();
                b.OnClick(() => _r.SelectTip(tip));
                _slots[i] = b;
            }
            UpdateSlots();
        }

        void UpdateSlots()
        {
            MatchState m = _r.M;
            if (_slotsFor < 0 || _slotsFor >= m.FighterCount) return;
            Fighter f = m.GetFighter(_slotsFor);
            bool myTurn = _r.Aiming && m.CurrentFighter.Index == _slotsFor;
            for (int i = 0; i < _slots.Length; i++)
            {
                ArrowTip tip = _slotTips[i];
                int ammo = f.Ammo[(int)tip];
                if (_slotAmmo[i] != ammo)
                {
                    _slotAmmo[i] = ammo;
                    _slotText[i].text = Loc.T("tipname_" + tip) + " ×" + (ammo < 0 ? "∞" : ammo.ToString());
                }
                _slotRing[i].enabled = tip == _r.SelectedTip;
                CanvasGroup cg = _slotGroup[i];
                cg.alpha = ammo == 0 ? 0.45f : myTurn || !_r.Session.IsHuman(m.CurrentSide) ? 1f : 0.75f;
            }
        }

        // ---------- ability ----------

        void UpdateAbility()
        {
            MatchState m = _r.M;
            int side = m.CurrentSide;
            int fi = _r.Session.IsHuman(side) ? m.CurrentFighter.Index : m.ActiveFighter(0);
            if (fi < 0)
            {
                _abilityRt.gameObject.SetActive(false);
                return;
            }
            Fighter f = m.GetFighter(fi);
            _abilityRt.gameObject.SetActive(f.HasAbility);
            if (!f.HasAbility) return;
            int charge = Mathf.Min(f.AbilityCharge, f.AbilityChargeNeeded);
            bool ready = f.AbilityReady && _r.Aiming && m.CurrentFighter.Index == fi;
            bool armed = _r.AbilityArmed;
            if (charge == _shownCharge && ready == _shownReady && armed == _shownArmed) return;
            _shownCharge = charge;
            _shownReady = ready;
            _shownArmed = armed;
            Color c = UiKit.Hex(ArcherLooks.Color(f.Def.Id));
            _ability.SetColors(ready || armed ? c : Color.Lerp(c, new Color(0.6f, 0.6f, 0.65f), 0.7f), new Color(0f, 0f, 0f, 0.2f));
            _abilityIcon.text = ArcherLooks.ElementIcon(f.Def.Element);
            _abilityRing.color = armed ? Ring : Color.white;
            _abilityGroup.alpha = ready || armed ? 1f : 0.75f;
            _abilityName.text = Loc.T("ability_" + f.Def.Ability);
            int need = Mathf.Clamp(f.AbilityChargeNeeded, 1, _pips.Length);
            for (int i = 0; i < _pips.Length; i++)
            {
                _pips[i].gameObject.SetActive(i < need);
                _pips[i].color = i < charge ? Ring : new Color(1f, 1f, 1f, 0.5f);
            }
        }

        // ------------------------------------------------------------------ per frame

        public override void Tick(float dt)
        {
            if (Root == null || _r.Session == null || _r.Roster == null) return;
            if (_rebuild)
            {
                _rebuild = false;
                Ui.Refresh();
                return;
            }
            _t += dt;
            RebuildSlots();
            UpdateAll(true);

            // Pulse the ability when it can be used (aaPulse 1.2 s) and the turn ring.
            if (_shownReady && !_shownArmed && (ServiceLocator.Settings == null || !ServiceLocator.Settings.ReduceMotion))
                _ability.transform.localScale = Vector3.one * (1f + Mathf.Sin(_t * Mathf.PI * 2f / 1.2f) * 0.06f);
            else _ability.transform.localScale = Vector3.one;
            for (int s = 0; s < 2; s++)
                if (_panelRing[s].enabled) _panelRing[s].color = new Color(Ring.r, Ring.g, Ring.b, 0.65f + Mathf.Sin(_t * 5f) * 0.35f);

            UpdateAim();
        }

        void UpdateAim()
        {
            DragAim d = _r.Drag;
            bool show = d != null && d.Dragging && _r.Aiming;
            if (_aimLayer.gameObject.activeSelf != show) _aimLayer.gameObject.SetActive(show);
            if (!show) return;
            _r.ShooterScreen(out Vector2 chest);
            ToLocal(chest, out Vector2 c);
            ToLocal(d.StartScreen, out Vector2 s);
            ToLocal(d.CurrentScreen, out Vector2 n);
            _powerRt.anchoredPosition = c;
            float power = d.InDeadZone ? 0f : d.Power;
            _powerRing.fillAmount = power;
            _powerRing.color = power < 0.5f ? Color.Lerp(Widgets.Green, Widgets.Gold, power * 2f) : Color.Lerp(Widgets.Gold, UiKit.Hex(0xFF8A3D), (power - 0.5f) * 2f);
            int pct = Mathf.RoundToInt(power * 100f), ang = Mathf.RoundToInt(d.AngleDeg);
            if (pct != _shownPower || ang != _shownAngle)
            {
                _shownPower = pct;
                _shownAngle = ang;
                _powerText.text = pct + "% · " + ang + "°";
            }
            _dragStartRt.anchoredPosition = s;
            Vector2 delta = n - s;
            _dragLineRt.anchoredPosition = s;
            _dragLineRt.sizeDelta = new Vector2(delta.magnitude, 2f);
            _dragLineRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        void ToLocal(Vector2 screen, out Vector2 local)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_aimLayer, screen, null, out local);
        }
    }
}
