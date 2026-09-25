using ArcherArcade.Core;
using ArcherArcade.Logic;
using UnityEngine;

namespace ArcherArcade.Arena
{
    /// <summary>Small helpers to place atlas sprites in the world (metres) for the arena, props and effects.</summary>
    public static class WorldSprites
    {
        /// <summary>Sorting orders of the match world, back to front.</summary>
        public const int Sky = -1000, SkyDecor = -990, FarIslands = -960, Backdrop = -950, Clouds = -945, TreesBack = -300,
            IslandUnder = -210, Island = -200, BushesBack = -150, Props = 40, Archers = 200, StuckArrows = 520, Flight = 600,
            Preview = 640, FrontDecor = 700, Weather = 720, Fx = 900;

        public static SpriteRenderer Make(Transform parent, string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        public static SpriteRenderer Make(Transform parent, string group, string id, int order) =>
            Make(parent, id, ArtLibrary.Get(group, id), order);

        /// <summary>Uniform scale so the sprite is <paramref name="meters"/> wide.</summary>
        public static float FitWidth(SpriteRenderer sr, float meters)
        {
            if (!sr || !sr.sprite) return 1f;
            float w = sr.sprite.bounds.size.x;
            float s = w > 0f ? meters / w : 1f;
            sr.transform.localScale = new Vector3(s, s, 1f);
            return s;
        }

        /// <summary>Uniform scale so the sprite is <paramref name="meters"/> tall.</summary>
        public static float FitHeight(SpriteRenderer sr, float meters)
        {
            if (!sr || !sr.sprite) return 1f;
            float h = sr.sprite.bounds.size.y;
            float s = h > 0f ? meters / h : 1f;
            sr.transform.localScale = new Vector3(s, s, 1f);
            return s;
        }

        /// <summary>A 9-sliced sprite drawn at <paramref name="size"/> metres (sprite pivot kept).</summary>
        public static SpriteRenderer Sliced(Transform parent, string group, string id, Vector2 size, int order)
        {
            SpriteRenderer sr = Make(parent, group, id, order);
            if (sr.sprite && sr.sprite.border.sqrMagnitude > 0f)
            {
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;
            }
            else if (sr.sprite)
            {
                Vector3 b = sr.sprite.bounds.size;
                sr.transform.localScale = new Vector3(size.x / Mathf.Max(0.01f, b.x), size.y / Mathf.Max(0.01f, b.y), 1f);
            }
            return sr;
        }

        /// <summary>A tiled sprite (ropes, vines) drawn at <paramref name="size"/> metres.</summary>
        public static SpriteRenderer Tiled(Transform parent, string group, string id, Vector2 size, int order)
        {
            SpriteRenderer sr = Make(parent, group, id, order);
            if (!sr.sprite) return sr;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            return sr;
        }

        public static Vector3 V(Vec2 v, float z = 0f) => new Vector3((float)v.X, (float)v.Y, z);

        public static Vector2 V2(Vec2 v) => new Vector2((float)v.X, (float)v.Y);
    }
}
