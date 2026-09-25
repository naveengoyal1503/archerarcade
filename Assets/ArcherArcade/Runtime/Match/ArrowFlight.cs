using System.Collections.Generic;
using ArcherArcade.Logic;
using UnityEngine;

namespace ArcherArcade.Match
{
    /// <summary>
    /// The arrows of one shot in flight (pooled <see cref="ArrowView"/>s): copies the shot's paths (the Logic result
    /// is reused on the next call), replays them against the replay clock and keeps arrows stuck in walls, crates
    /// and the ground until the turn ends (GAME_DESIGN §3.2).
    /// </summary>
    public sealed class ArrowFlight : MonoBehaviour
    {
        const int PoolSize = 28;

        readonly List<ArrowView> _free = new List<ArrowView>(PoolSize);
        readonly List<ArrowView> _live = new List<ArrowView>(PoolSize);
        readonly List<ArrowPath> _paths = new List<ArrowPath>(16);
        readonly List<ArrowView> _byPath = new List<ArrowView>(16);

        public IReadOnlyList<ArrowPath> Paths => _paths;

        public static ArrowFlight Create(Transform parent)
        {
            var go = new GameObject("Arrows");
            go.transform.SetParent(parent, false);
            var f = go.AddComponent<ArrowFlight>();
            for (int i = 0; i < PoolSize; i++) f._free.Add(ArrowView.Create(go.transform));
            return f;
        }

        ArrowView Take()
        {
            ArrowView v;
            if (_free.Count > 0)
            {
                v = _free[_free.Count - 1];
                _free.RemoveAt(_free.Count - 1);
            }
            else
            {
                // Oldest stuck arrow makes room.
                v = _live[0];
                _live.RemoveAt(0);
            }
            _live.Add(v);
            return v;
        }

        /// <summary>Starts the arrows of <paramref name="shot"/>.</summary>
        public void Play(ShotResult shot, ArrowTip tip, AbilityKind ability, string trailId, Vector3 visualOffset, bool bigger)
        {
            _paths.Clear();
            _byPath.Clear();
            string sprite = ArrowView.SpriteFor(tip, ability);
            for (int i = 0; i < shot.Arrows.Count; i++)
            {
                ArrowPath p = shot.Arrows[i];
                _paths.Add(p);
                ArrowView v = Take();
                string s = p.Parent >= 0 && p.BounceCount == 0 && tip == ArrowTip.Split ? "arrow_split" : sprite;
                v.Launch(i, p, tip, s, trailId, visualOffset, bigger);
                _byPath.Add(v);
            }
        }

        public void SetTime(float t)
        {
            for (int i = 0; i < _byPath.Count; i++) _byPath[i].SetTime(t);
        }

        public ArrowView ViewFor(int pathIndex) => pathIndex >= 0 && pathIndex < _byPath.Count ? _byPath[pathIndex] : null;

        /// <summary>Position of the arrow the camera should follow (the one landing last that is still flying).</summary>
        public bool Lead(out Vector3 position)
        {
            position = Vector3.zero;
            double best = -1.0;
            bool any = false;
            for (int i = 0; i < _byPath.Count; i++)
            {
                ArrowView v = _byPath[i];
                if (!v.Flying || !v.gameObject.activeSelf) continue;
                if (v.Path.EndTime > best)
                {
                    best = v.Path.EndTime;
                    position = v.transform.position;
                    any = true;
                }
            }
            return any;
        }

        /// <summary>Hides one arrow (it went into a fighter's rig, or its target flew away).</summary>
        public void Hide(int pathIndex)
        {
            ArrowView v = ViewFor(pathIndex);
            if (!v) return;
            _live.Remove(v);
            v.Recycle(transform);
            _free.Add(v);
        }

        /// <summary>End of the turn: stuck arrows disappear.</summary>
        public void ClearAll()
        {
            for (int i = 0; i < _live.Count; i++)
            {
                _live[i].Recycle(transform);
                _free.Add(_live[i]);
            }
            _live.Clear();
            _byPath.Clear();
            _paths.Clear();
        }
    }
}
