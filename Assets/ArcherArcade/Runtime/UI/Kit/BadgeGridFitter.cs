using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>Keeps a fixed-column grid's cells as wide as the scroll view allows (the design's 1fr columns).</summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class BadgeGridFitter : MonoBehaviour
    {
        public int Columns = 6;
        GridLayoutGroup _grid;
        float _lastWidth = -1f;

        void LateUpdate()
        {
            if (!_grid) _grid = GetComponent<GridLayoutGroup>();
            float w = ((RectTransform)transform).rect.width;
            if (Mathf.Approximately(w, _lastWidth) || w <= 0f) return;
            _lastWidth = w;
            float cell = (w - _grid.padding.horizontal - _grid.spacing.x * (Columns - 1)) / Columns;
            _grid.cellSize = new Vector2(Mathf.Floor(cell), _grid.cellSize.y);
        }
    }
}
