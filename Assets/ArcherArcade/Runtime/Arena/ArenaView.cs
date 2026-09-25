using System.Collections.Generic;
using ArcherArcade.Core;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using UnityEngine;

namespace ArcherArcade.Arena
{
    /// <summary>
    /// The World 1 arena for one match (GAME_DESIGN §12, LEVELS.md): sky, 4-layer parallax forest, floating islands
    /// assembled from tiles with grass, flowers, bushes, trees and mushrooms, walls, the swing-target frame and one
    /// <see cref="PropView"/> per Logic prop. Lighting follows the level's time of day and the theme. Decoration is
    /// placed with a local seed (visual only, never touches gameplay randomness).
    /// </summary>
    public sealed class ArenaView : MonoBehaviour
    {
        MatchState _m;
        SkyLayer _sky;
        WeatherFx _weather;
        readonly List<ParallaxLayer> _layers = new List<ParallaxLayer>();
        readonly List<Transform> _floaters = new List<Transform>();
        readonly List<Vector3> _floaterHome = new List<Vector3>();
        PropView[] _props;
        Transform _world;
        uint _rng;
        float _t;

        public SkyPalette Palette { get; private set; }
        public MatchState Match => _m;

        public static ArenaView Create(Transform parent, MatchState m, TimeOfDay time, bool dark, uint seed)
        {
            var go = new GameObject("Arena");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<ArenaView>();
            a.Build(m, time, dark, seed);
            return a;
        }

        float R()
        {
            _rng ^= _rng << 13;
            _rng ^= _rng >> 17;
            _rng ^= _rng << 5;
            return (_rng & 0xFFFFFF) / 16777216f;
        }

        float Range(float a, float b) => a + (b - a) * R();

        void Build(MatchState m, TimeOfDay time, bool dark, uint seed)
        {
            _m = m;
            _rng = seed * 2654435761u | 1u;
            Palette = SkyPalette.For(time, dark);
            SkyPalette pal = Palette;
            _sky = SkyLayer.Create(transform, pal, dark, seed);

            Color far = pal.Far, mid = Color.Lerp(pal.Far, pal.World, 0.5f), near = Color.Lerp(pal.Far, pal.World, 0.8f);
            Sprite islandFar = ArtLibrary.Get(ArtLibrary.Backdrop, "island_far");
            for (int i = 0; i < 3; i++)
            {
                SpriteRenderer f = WorldSprites.Make(transform, "FarIsland" + i, islandFar, WorldSprites.FarIslands);
                f.color = new Color(far.r, far.g, far.b, 0.9f);
                _floaters.Add(f.transform);
                _floaterHome.Add(new Vector3(Range(-0.9f, 0.9f), Range(0.25f, 0.62f), Range(0.5f, 0.9f)));
            }
            _layers.Add(ParallaxLayer.Create(transform, ArtLibrary.Get(ArtLibrary.Backdrop, "hills_far"), WorldSprites.Backdrop, 0.92f, 0.62f, -0.3f, far));
            _layers.Add(ParallaxLayer.Create(transform, ArtLibrary.Get(ArtLibrary.Backdrop, "forest_far"), WorldSprites.Backdrop + 10, 0.84f, 0.66f, -0.55f, mid));
            _layers.Add(ParallaxLayer.Create(transform, ArtLibrary.Get(ArtLibrary.Backdrop, "hills_near"), WorldSprites.Backdrop + 20, 0.7f, 0.6f, -1.05f, near));

            _world = new GameObject("World").transform;
            _world.SetParent(transform, false);
            ArenaLayout layout = m.Setup.Arena;
            for (int i = 0; i < layout.Grounds.Count; i++) BuildIsland(layout.Grounds[i], i, time);
            for (int i = 0; i < layout.Walls.Count; i++)
            {
                Shape w = layout.Walls[i];
                SpriteRenderer sr = WorldSprites.Sliced(_world, ArtLibrary.Props, "stone_wall",
                    new Vector2((float)w.HalfSize.X * 2f + 0.06f, (float)w.HalfSize.Y * 2f + 0.08f), WorldSprites.Props - 2);
                sr.transform.position = WorldSprites.V(w.A);
            }
            BuildSwingFrame();

            _props = new PropView[m.PropCount];
            for (int i = 0; i < m.PropCount; i++) _props[i] = PropView.Create(_world, m, i);

            if (pal.World != Color.white)
                foreach (SpriteRenderer sr in _world.GetComponentsInChildren<SpriteRenderer>(true)) sr.color *= pal.World;

            float minX = (float)layout.MinX + 25f, maxX = (float)layout.MaxX - 25f;
            _weather = WeatherFx.Create(transform, pal.Fireflies, minX, maxX);
        }

        void BuildIsland(Shape g, int index, TimeOfDay time)
        {
            var root = new GameObject("Island" + index).transform;
            root.SetParent(_world, false);
            float top = (float)(g.A.Y + g.HalfSize.Y), left = (float)(g.A.X - g.HalfSize.X), right = (float)(g.A.X + g.HalfSize.X);
            float width = right - left;
            const float capW = 0.55f;

            Sprite mid = ArtLibrary.Get(ArtLibrary.Props, "island_mid");
            float inner = Mathf.Max(0.2f, width - capW * 2f);
            int n = Mathf.Max(1, Mathf.RoundToInt(inner));
            float tileW = inner / n;
            float midW = mid ? mid.bounds.size.x : 1f;
            for (int i = 0; i < n; i++)
            {
                SpriteRenderer t = WorldSprites.Make(root, "Mid" + i, mid, WorldSprites.Island);
                t.transform.position = new Vector3(left + capW + (i + 0.5f) * tileW, top, 0f);
                t.transform.localScale = new Vector3(tileW / midW * 1.01f, 1f, 1f);
            }
            SpriteRenderer l = WorldSprites.Make(root, ArtLibrary.Props, "island_left", WorldSprites.Island + 1);
            l.transform.position = new Vector3(left + capW + 0.02f, top, 0f);
            SpriteRenderer r = WorldSprites.Make(root, ArtLibrary.Props, "island_right", WorldSprites.Island + 1);
            r.transform.position = new Vector3(right - capW - 0.02f, top, 0f);

            // Hanging rock underneath.
            float midBottom = top - 1.2f;
            string[] under = { "island_under_1", "island_under_2", "island_under_3" };
            float[] share = { 0.78f, 0.46f, 0.36f };
            float[] at = { 0.5f, 0.22f, 0.8f };
            for (int i = 0; i < 3; i++)
            {
                SpriteRenderer u = WorldSprites.Make(root, ArtLibrary.Props, under[i], WorldSprites.IslandUnder - i);
                if (!u.sprite) continue;
                float s = Mathf.Min(1.7f, width * share[i] / u.sprite.bounds.size.x);
                u.transform.localScale = new Vector3(s, s, 1f);
                u.transform.position = new Vector3(Mathf.Lerp(left, right, at[i]), midBottom + 0.12f, 0f);
            }

            // Decoration: keep clear of archers and props so hit zones stay readable.
            bool night = time == TimeOfDay.Night;
            var busy = new List<float>();
            for (int i = 0; i < _m.FighterCount; i++) busy.Add((float)_m.GetFighter(i).Feet.X);
            for (int i = 0; i < _m.PropCount; i++) busy.Add((float)_m.PropRestShape(i).Center.X);

            if (width >= 4f)
            {
                int trees = width >= 7f ? 2 : 1;
                for (int i = 0; i < trees; i++)
                {
                    float x = i == 0 ? left + Range(0.5f, 1.1f) : right - Range(0.5f, 1.1f);
                    string id = night ? "tree_night" : R() < 0.45f ? "tree_pine" : "tree_round";
                    SpriteRenderer tree = WorldSprites.Make(root, ArtLibrary.Scenery, id, WorldSprites.TreesBack + i);
                    WorldSprites.FitHeight(tree, Range(2.5f, 3.3f));
                    tree.transform.position = new Vector3(x, top + 0.05f, 0f);
                    tree.flipX = R() < 0.5f;
                }
            }
            int bushes = Mathf.Clamp(Mathf.RoundToInt(width / 2.6f), 1, 4);
            for (int i = 0; i < bushes; i++)
            {
                float x = Range(left + 0.4f, right - 0.4f);
                SpriteRenderer b = WorldSprites.Make(root, ArtLibrary.Scenery, R() < 0.35f ? "bush_berry" : "bush", WorldSprites.BushesBack + i);
                WorldSprites.FitWidth(b, Range(0.9f, 1.3f));
                b.transform.position = new Vector3(x, top + 0.04f, 0f);
                b.flipX = R() < 0.5f;
            }
            string[] flowers = { "flower_pink", "flower_white", "flower_yellow" };
            for (float x = left + 0.25f; x < right - 0.2f; x += Range(0.55f, 0.95f))
            {
                if (Near(busy, x, 0.55f)) continue;
                float pick = R();
                SpriteRenderer d;
                if (pick < 0.55f)
                {
                    d = WorldSprites.Make(root, ArtLibrary.Scenery, "grass", WorldSprites.FrontDecor);
                    WorldSprites.FitWidth(d, Range(0.32f, 0.46f));
                }
                else if (pick < 0.82f)
                {
                    d = WorldSprites.Make(root, ArtLibrary.Scenery, flowers[(int)(R() * 3f) % 3], WorldSprites.FrontDecor - 1);
                    WorldSprites.FitHeight(d, Range(0.34f, 0.46f));
                }
                else if (pick < 0.92f)
                {
                    d = WorldSprites.Make(root, ArtLibrary.Scenery, "mushroom", WorldSprites.FrontDecor - 2);
                    WorldSprites.FitHeight(d, Range(0.28f, 0.38f));
                }
                else
                {
                    d = WorldSprites.Make(root, ArtLibrary.Scenery, "rock", WorldSprites.FrontDecor - 3);
                    WorldSprites.FitWidth(d, Range(0.4f, 0.6f));
                }
                d.transform.position = new Vector3(x, top + 0.02f, 0f);
                d.flipX = R() < 0.5f;
            }
        }

        static bool Near(List<float> xs, float x, float d)
        {
            for (int i = 0; i < xs.Count; i++) if (Mathf.Abs(xs[i] - x) < d) return true;
            return false;
        }

        /// <summary>Wooden frame the swinging targets hang from (a beam on two posts).</summary>
        void BuildSwingFrame()
        {
            float minX = float.MaxValue, maxX = float.MinValue, beamY = float.MinValue;
            var pivots = new List<Vector3>();
            for (int i = 0; i < _m.PropCount; i++)
            {
                PropMotion mo = _m.GetProp(i).Spec.Motion;
                if (mo.Kind != MotionKind.Swing) continue;
                Vector3 p = WorldSprites.V(mo.Pivot);
                pivots.Add(p);
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                beamY = Mathf.Max(beamY, p.y);
            }
            if (pivots.Count == 0) return;
            beamY += 0.2f;
            float x0 = minX - 0.8f, x1 = maxX + 0.8f;
            SpriteRenderer beam = WorldSprites.Sliced(_world, ArtLibrary.Props, "platform", new Vector2(x1 - x0, 0.34f), WorldSprites.Props - 6);
            beam.transform.position = new Vector3((x0 + x1) * 0.5f, beamY, 0f);
            float[] posts = { x0 + 0.25f, x1 - 0.25f };
            foreach (float x in posts)
            {
                float ground = GroundTopBelow(x, beamY);
                if (ground < -50f) ground = beamY - 7f;
                float len = beamY - ground;
                SpriteRenderer post = WorldSprites.Sliced(_world, ArtLibrary.Props, "target_post", new Vector2(0.26f, len), WorldSprites.Props - 7);
                post.transform.position = new Vector3(x, ground + len * 0.5f, 0f);
            }
            foreach (Vector3 p in pivots)
            {
                float len = beamY - p.y;
                if (len < 0.05f) continue;
                SpriteRenderer rope = WorldSprites.Tiled(_world, ArtLibrary.Props, "rope", new Vector2(0.1f, len), WorldSprites.Props - 5);
                rope.transform.position = new Vector3(p.x, p.y + len * 0.5f, 0f);
            }
        }

        /// <summary>Top of the highest island under (x, y), or −99 over the void.</summary>
        public float GroundTopBelow(float x, float y)
        {
            ArenaLayout a = _m.Setup.Arena;
            float best = -99f;
            for (int i = 0; i < a.Grounds.Count; i++)
            {
                Shape g = a.Grounds[i];
                float top = (float)(g.A.Y + g.HalfSize.Y);
                if (Mathf.Abs(x - (float)g.A.X) <= (float)g.HalfSize.X + 0.3f && top <= y + 0.01f && top > best) best = top;
            }
            return best;
        }

        public PropView Prop(int index) => _props != null && index >= 0 && index < _props.Length ? _props[index] : null;

        public void SetWind(int wind)
        {
            _sky.SetWind(wind);
            _weather.SetWind(wind);
        }

        /// <summary>Moving targets follow the match clock.</summary>
        public void TrackClock(double clock)
        {
            for (int i = 0; i < _props.Length; i++) _props[i].TrackClock(clock);
        }

        public void SyncProps(bool animate, double clock)
        {
            for (int i = 0; i < _props.Length; i++) _props[i].Sync(animate, clock);
        }

        /// <summary>Called by the camera after it moved (sky, parallax and weather follow it).</summary>
        public void Place(Vector3 cam, float size, float aspect)
        {
            float dt = Time.deltaTime;
            _t += dt;
            _sky.Place(cam, size, aspect, dt);
            for (int i = 0; i < _layers.Count; i++) _layers[i].Place(cam, size, aspect, dt);
            float zoom = size / 4.4f, halfW = size * aspect;
            for (int i = 0; i < _floaters.Count; i++)
            {
                Vector3 h = _floaterHome[i];
                Transform f = _floaters[i];
                f.position = new Vector3(cam.x * 0.93f + h.x * halfW * 1.6f,
                    cam.y - size + h.y * size * 2f + Mathf.Sin(_t * 0.5f + i * 2f) * 0.12f * zoom, 0f);
                float s = h.z * zoom * 0.55f;
                f.localScale = new Vector3(s, s, 1f);
            }
            _weather.Place(cam, size, aspect, dt);
        }
    }
}
