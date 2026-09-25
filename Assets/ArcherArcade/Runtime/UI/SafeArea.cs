using UnityEngine;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Fits its RectTransform to Screen.safeArea (notches, rounded corners, cut-outs). Wrap every UI root with it.
    /// Re-applies when the safe area, resolution or orientation changes.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        RectTransform _rect;
        Rect _applied;
        Vector2Int _screen;

        void OnEnable()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != _applied || Screen.width != _screen.x || Screen.height != _screen.y) Apply();
        }

        void Apply()
        {
            if (_rect == null || Screen.width <= 0 || Screen.height <= 0) return;
            Rect area = Screen.safeArea;
            _applied = area;
            _screen = new Vector2Int(Screen.width, Screen.height);
            Vector2 min = area.position;
            Vector2 max = area.position + area.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
