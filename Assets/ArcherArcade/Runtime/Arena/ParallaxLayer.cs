using UnityEngine;

namespace ArcherArcade.Arena
{
    /// <summary>
    /// One endless backdrop strip (World 1 parallax forest, GAME_DESIGN §12): tiles of one sprite that follow the
    /// camera at <see cref="Follow"/> of its speed and keep their size on screen when the camera zooms out, with
    /// their bottom edge a fixed distance above the bottom of the view.
    /// </summary>
    public sealed class ParallaxLayer : MonoBehaviour
    {
        const float RefSize = 4.4f;
        const int Tiles = 5;

        readonly SpriteRenderer[] _tiles = new SpriteRenderer[Tiles];
        float _follow, _scale, _lift, _bottom, _width, _bob;
        float _t;

        /// <summary>0 = fixed in the world, 1 = glued to the camera.</summary>
        public float Follow => _follow;

        public static ParallaxLayer Create(Transform parent, Sprite sprite, int order, float follow, float scale, float lift, Color tint,
            float bob = 0f)
        {
            var go = new GameObject("Parallax_" + (sprite ? sprite.name : "none"));
            go.transform.SetParent(parent, false);
            var layer = go.AddComponent<ParallaxLayer>();
            layer._follow = follow;
            layer._scale = scale;
            layer._lift = lift;
            layer._bob = bob;
            for (int i = 0; i < Tiles; i++)
            {
                layer._tiles[i] = WorldSprites.Make(go.transform, "Tile" + i, sprite, order);
                layer._tiles[i].color = tint;
            }
            if (sprite)
            {
                layer._width = sprite.bounds.size.x;
                layer._bottom = sprite.bounds.min.y;
            }
            return layer;
        }

        public void SetTint(Color c)
        {
            for (int i = 0; i < Tiles; i++) _tiles[i].color = c;
        }

        /// <summary>Places the tiles for a camera at <paramref name="cam"/> with orthographic half-height <paramref name="size"/>.</summary>
        public void Place(Vector3 cam, float size, float aspect, float dt)
        {
            if (_width <= 0f) return;
            _t += dt;
            float zoom = size / RefSize;
            float s = _scale * zoom;
            float tileW = _width * s * 0.998f;
            float viewBottom = cam.y - size;
            float y = viewBottom + _lift * zoom - _bottom * s + Mathf.Sin(_t * 0.6f) * _bob * zoom;
            float baseX = cam.x * _follow;
            float rel = cam.x - baseX;
            float half = size * aspect;
            float start = baseX + Mathf.Floor((rel - half) / tileW) * tileW;
            for (int i = 0; i < Tiles; i++)
            {
                Transform t = _tiles[i].transform;
                t.localScale = new Vector3(s, s, 1f);
                t.position = new Vector3(start + (i + 0.5f) * tileW, y, 0f);
            }
        }
    }
}
