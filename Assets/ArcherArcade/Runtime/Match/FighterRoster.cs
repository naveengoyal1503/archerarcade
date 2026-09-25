using ArcherArcade.Archers;
using ArcherArcade.Arena;
using ArcherArcade.Core;
using ArcherArcade.Feel;
using ArcherArcade.Logic;
using UnityEngine;

namespace ArcherArcade.Match
{
    /// <summary>
    /// The archers of a match on screen: one <see cref="ArcherView"/> per Logic fighter, the HP shown on the HUD
    /// (drops when the replay reaches each hit, not when Logic already knows), which fighter is visible on each
    /// side (Gauntlet archers walk in one after another), sliding / falling to new feet, and the injury looks.
    /// </summary>
    public sealed class FighterRoster
    {
        const float WalkIn = 6f;

        readonly MatchState _m;
        readonly ArcherView[] _views;
        readonly int[] _shownHp;
        readonly bool[] _visible;
        readonly Vector3[] _from, _to;
        readonly float[] _moveT, _moveDur;
        readonly bool[] _walking;
        readonly float[] _poof;
        readonly Color _tint;

        public int Count => _views.Length;

        public FighterRoster(Transform parent, MatchState m, string[] looks, Color tint)
        {
            _m = m;
            _tint = tint;
            int n = m.FighterCount;
            _views = new ArcherView[n];
            _shownHp = new int[n];
            _visible = new bool[n];
            _from = new Vector3[n];
            _to = new Vector3[n];
            _moveT = new float[n];
            _moveDur = new float[n];
            _walking = new bool[n];
            _poof = new float[n];
            for (int i = 0; i < n; i++)
            {
                Fighter f = m.GetFighter(i);
                ArcherView v = ArcherView.Create(parent, looks[i], f.Facing, WorldSprites.Archers + i * 50);
                v.transform.position = WorldSprites.V(f.Feet);
                _views[i] = v;
                _shownHp[i] = f.Hp;
                _moveT[i] = 1f;
                _poof[i] = -1f;
                bool active = m.ActiveFighter(f.Side) == i;
                _visible[i] = active;
                v.gameObject.SetActive(active);
                Tint(v);
                SyncLooks(i);
            }
        }

        void Tint(ArcherView v)
        {
            if (_tint != Color.white) v.SetAmbient(_tint);
        }

        public ArcherView View(int i) => i >= 0 && i < _views.Length ? _views[i] : null;
        public int ShownHp(int i) => _shownHp[i];
        public bool Visible(int i) => _visible[i];

        /// <summary>The fighter shown for a side right now (−1 if none).</summary>
        public int VisibleOnSide(int side)
        {
            for (int i = 0; i < _views.Length; i++)
                if (_visible[i] && _m.GetFighter(i).Side == side && _poof[i] < 0f) return i;
            for (int i = 0; i < _views.Length; i++)
                if (_visible[i] && _m.GetFighter(i).Side == side) return i;
            return -1;
        }

        public void ShowDamage(int i, int amount)
        {
            _shownHp[i] = Mathf.Max(0, _shownHp[i] - amount);
            SyncLooks(i);
        }

        public void ShowHeal(int i, int amount)
        {
            Fighter f = _m.GetFighter(i);
            _shownHp[i] = Mathf.Min(f.MaxHp, _shownHp[i] + amount);
            SyncLooks(i);
        }

        /// <summary>Injury look from the shown HP plus the Logic statuses (marks, burning, poison, bubble, helmet).</summary>
        public void SyncLooks(int i)
        {
            Fighter f = _m.GetFighter(i);
            InjuryStage stage = Injury.Stage(_shownHp[i], f.MaxHp);
            _views[i].SetLooks(stage, f.Marks, f.Status.IsBurning, f.Status.IsPoisoned, f.HasBubble, f.HelmetLeft);
        }

        /// <summary>Snaps HP and looks to Logic (end of a replay); optionally moves archers to their Logic feet.</summary>
        public void SyncAll(bool animateMoves, bool move = true)
        {
            for (int i = 0; i < _views.Length; i++)
            {
                Fighter f = _m.GetFighter(i);
                _shownHp[i] = f.Hp;
                SyncLooks(i);
                if (!move || !_visible[i]) continue;
                if (f.IsAlive) MoveTo(i, WorldSprites.V(f.Feet), animateMoves ? 0.6f : 0f, false);
            }
        }

        public void MoveTo(int i, Vector3 feet, float seconds, bool walk)
        {
            if (seconds <= 0f)
            {
                _views[i].transform.position = feet;
                _moveT[i] = 1f;
                return;
            }
            if ((feet - _views[i].transform.position).sqrMagnitude < 0.0001f) return;
            _from[i] = _views[i].transform.position;
            _to[i] = feet;
            _moveT[i] = 0f;
            _moveDur[i] = seconds;
            _walking[i] = walk;
        }

        /// <summary>Gauntlet: the next archer walks in from behind the edge.</summary>
        public void Enter(int i)
        {
            if (_visible[i]) return;
            Fighter f = _m.GetFighter(i);
            _visible[i] = true;
            ArcherView v = _views[i];
            v.gameObject.SetActive(true);
            Vector3 feet = WorldSprites.V(f.Feet);
            v.transform.position = feet - new Vector3(f.Facing * WalkIn, 0f, 0f);
            MoveTo(i, feet, 1.1f, true);
            _shownHp[i] = f.Hp;
            SyncLooks(i);
        }

        /// <summary>Knocked out: falls over; in a Gauntlet the body poofs away after a moment.</summary>
        public void KnockOut(int i, bool poofAway)
        {
            _views[i].KnockOut();
            if (poofAway) _poof[i] = 1.3f;
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < _views.Length; i++)
            {
                if (_moveT[i] < 1f)
                {
                    _moveT[i] = Mathf.Min(1f, _moveT[i] + dt / Mathf.Max(0.01f, _moveDur[i]));
                    float k = Tweening.Ease.Evaluate(_walking[i] ? Tweening.EaseType.OutQuad : Tweening.EaseType.OutCubic, _moveT[i]);
                    Vector3 p = Vector3.LerpUnclamped(_from[i], _to[i], k);
                    if (_walking[i]) p.y += Mathf.Abs(Mathf.Sin(_moveT[i] * 18f)) * 0.08f * (1f - _moveT[i]);
                    _views[i].transform.position = p;
                }
                if (_poof[i] >= 0f)
                {
                    _poof[i] -= dt;
                    if (_poof[i] < 0f)
                    {
                        FxSystem fx = FxSystem.Instance;
                        Vector3 at = _views[i].transform.position + Vector3.up * 0.5f;
                        if (fx)
                        {
                            fx.Smoke(at, 8, 1.1f, new Color(1f, 1f, 1f, 0.9f));
                            fx.Burst(ArtLibrary.Get(ArtLibrary.Fx, "star"), at + Vector3.up * 0.4f, 8, 3f, 0.8f,
                                new[] { new Color(1f, 0.82f, 0.25f), Color.white }, 0.22f, 3f, 2f);
                        }
                        _visible[i] = false;
                        _views[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        /// <summary>Whole-body box of a fighter for camera framing (feet, head top).</summary>
        public void Bounds(int i, out float x, out float feetY, out float topY)
        {
            Vector3 p = _views[i].transform.position;
            float scale = (float)_m.GetFighter(i).Def.BodyScale;
            x = p.x;
            feetY = p.y;
            topY = p.y + 2.1f * scale;
        }
    }
}
