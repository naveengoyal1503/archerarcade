using ArcherArcade.Arena;
using ArcherArcade.Core;
using ArcherArcade.Logic;
using UnityEngine;

namespace ArcherArcade.Match
{
    /// <summary>
    /// Aim help (GAME_DESIGN §3.1): white dots along the first share of the arc (45 % easy / 30 % medium / 18 %
    /// hard…), fading out towards the end, plus the faint ghost arc and landing marker of your last shot (§12 juice
    /// list). Pooled dots; the arc is exact (same Ballistics as Logic).
    /// </summary>
    public sealed class TrajectoryPreview : MonoBehaviour
    {
        const int Dots = 40, GhostDots = 36;
        const float Spacing = 0.42f; // metres between dots

        readonly SpriteRenderer[] _dots = new SpriteRenderer[Dots];
        readonly SpriteRenderer[] _ghost = new SpriteRenderer[GhostDots];
        SpriteRenderer _marker;
        float _t;

        public static TrajectoryPreview Create(Transform parent)
        {
            var go = new GameObject("Preview");
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<TrajectoryPreview>();
            Sprite dot = ArtLibrary.Get(ArtLibrary.Fx, "dot");
            for (int i = 0; i < Dots; i++)
            {
                p._dots[i] = WorldSprites.Make(go.transform, "Dot" + i, dot, WorldSprites.Preview);
                p._dots[i].enabled = false;
            }
            for (int i = 0; i < GhostDots; i++)
            {
                p._ghost[i] = WorldSprites.Make(go.transform, "Ghost" + i, dot, WorldSprites.Preview - 1);
                p._ghost[i].enabled = false;
            }
            p._marker = WorldSprites.Make(go.transform, ArtLibrary.Fx, "ring", WorldSprites.Preview - 1);
            p._marker.enabled = false;
            return p;
        }

        /// <summary>
        /// Shows the first <paramref name="share"/> of the arc from <paramref name="origin"/> (full arc = back down to
        /// <paramref name="groundY"/>). <paramref name="visualOffset"/> blends the first dots onto the drawn arrow.
        /// </summary>
        public void Show(Vec2 origin, Vec2 velocity, Vec2 accel, double groundY, double share, Vector3 visualOffset, float zoom)
        {
            _t += Time.unscaledDeltaTime;
            double full = Ballistics.TimeToHeight(origin, velocity, accel, groundY);
            if (full <= 0.0) full = 2.0;
            double end = full * share;
            // Walk the arc in equal distance steps (roughly): dt = spacing / speed.
            double t = 0.0;
            int n = 0;
            float scroll = (_t * 1.6f) % 1f;
            while (n < Dots && t < end)
            {
                Vec2 v = Ballistics.VelocityAt(velocity, accel, t);
                double speed = System.Math.Max(4.0, v.Length);
                double step = Spacing * zoom / speed;
                double tt = t + step * scroll;
                if (tt > end) break;
                Vec2 p = Ballistics.PositionAt(origin, velocity, accel, tt);
                float blend = 1f - Mathf.Clamp01((float)tt / 0.15f);
                SpriteRenderer d = _dots[n];
                d.enabled = true;
                d.transform.position = WorldSprites.V(p) + visualOffset * blend;
                float k = (float)(tt / end);
                float size = Mathf.Lerp(0.15f, 0.08f, k) * zoom;
                WorldSprites.FitWidth(d, size);
                d.color = new Color(1f, 1f, 1f, 1f - k * 0.8f);
                t += step;
                n++;
            }
            for (int i = n; i < Dots; i++) _dots[i].enabled = false;
        }

        public void Hide()
        {
            for (int i = 0; i < Dots; i++) _dots[i].enabled = false;
        }

        /// <summary>Faint dotted arc of a finished shot and a ring where it stopped (null path = hide).</summary>
        public void ShowGhost(ArrowPath? path, Color tint)
        {
            if (!path.HasValue)
            {
                for (int i = 0; i < GhostDots; i++) _ghost[i].enabled = false;
                _marker.enabled = false;
                return;
            }
            ArrowPath a = path.Value;
            double dur = a.EndTime - a.StartTime;
            for (int i = 0; i < GhostDots; i++)
            {
                double t = dur * (i + 1) / (GhostDots + 1);
                Vec2 p = Ballistics.PositionAt(a.StartPosition, a.StartVelocity, a.Acceleration, t);
                SpriteRenderer d = _ghost[i];
                d.enabled = true;
                d.transform.position = WorldSprites.V(p);
                WorldSprites.FitWidth(d, 0.07f);
                d.color = new Color(tint.r, tint.g, tint.b, 0.28f);
            }
            _marker.enabled = true;
            _marker.transform.position = WorldSprites.V(a.EndPosition);
            WorldSprites.FitWidth(_marker, 0.34f);
            _marker.color = new Color(tint.r, tint.g, tint.b, 0.6f);
        }
    }
}
