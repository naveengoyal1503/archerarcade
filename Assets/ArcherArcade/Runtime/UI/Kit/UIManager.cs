using System;
using System.Collections.Generic;
using ArcherArcade.Core;
using ArcherArcade.Feel;
using ArcherArcade.Theme;
using ArcherArcade.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace ArcherArcade.UI
{
    /// <summary>
    /// Router + back stack for one scene's UI (CLAUDE.md Core/UIManager): builds the canvas (844 × 390 reference,
    /// match 0.5), the safe area, the themed background, the header (back · title · coins), the screen stack,
    /// modals and toasts. Android Back closes the top modal, lets the screen handle it, pops the stack, or asks the
    /// scene (<see cref="RootBack"/>) at the root. Theme or language changes rebuild the visible screen.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        public const float RefWidth = 844f, RefHeight = 390f;

        readonly List<UiScreen> _stack = new List<UiScreen>();
        readonly List<UiModal> _modals = new List<UiModal>();
        readonly HashSet<UiScreen> _stale = new HashSet<UiScreen>();

        Canvas _canvas;
        RectTransform _safe, _screens, _headerLayer, _modalLayer;
        RawImage _background;
        ToastLayer _toasts;
        float _backCooldown;

        /// <summary>Back pressed on the root screen (Home asks to exit; Match pauses).</summary>
        public Action RootBack;

        public RectTransform SafeRoot => _safe;
        public Canvas Canvas => _canvas;
        public UiScreen Current => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
        public int Depth => _stack.Count;
        public bool HasModal => _modals.Count > 0;

        public static UIManager Create(string name = "UI", int sortingOrder = 10)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var ui = go.AddComponent<UIManager>();
            ui.Build(sortingOrder);
            return ui;
        }

        void Build(int sortingOrder)
        {
            gameObject.layer = 5;
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = sortingOrder;
            _canvas.pixelPerfect = false;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            var root = (RectTransform)transform;
            _background = UiKit.Picture(root, "Background", null);
            UiKit.Stretch(_background.rectTransform);
            _safe = UiKit.Rect(root, "SafeArea");
            UiKit.Stretch(_safe);
            _safe.gameObject.AddComponent<SafeArea>();
            _screens = UiKit.Rect(_safe, "Screens");
            UiKit.Stretch(_screens);
            _headerLayer = UiKit.Rect(_safe, "Header");
            UiKit.Stretch(_headerLayer);
            _modalLayer = UiKit.Rect(root, "Modals");
            UiKit.Stretch(_modalLayer);
            _toasts = ToastLayer.Create(_safe);
            PaintBackground();
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current) return;
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        void OnEnable()
        {
            GameEvents.ThemeChanged += ThemeOrLanguageChanged;
            GameEvents.LanguageChanged += ThemeOrLanguageChanged;
        }

        void OnDisable()
        {
            GameEvents.ThemeChanged -= ThemeOrLanguageChanged;
            GameEvents.LanguageChanged -= ThemeOrLanguageChanged;
        }

        void ThemeOrLanguageChanged()
        {
            PaintBackground();
            foreach (UiScreen s in _stack) _stale.Add(s);
            if (Current != null) Rebuild(Current, false);
            for (int i = 0; i < _modals.Count; i++) RebuildModal(_modals[i]);
        }

        public void PaintBackground()
        {
            Palette p = UiKit.P;
            Texture own = Current?.Background;
            _background.texture = own ? own : ShapeSprites.Vertical(p.BgTop, p.BgBottom);
        }

        // ---------- screens ----------

        public void Push(UiScreen screen)
        {
            if (Current != null)
            {
                Current.OnHide();
                if (Current.Root) Current.Root.gameObject.SetActive(false);
            }
            _stack.Add(screen);
            Show(screen, true);
        }

        /// <summary>Replaces the whole stack (e.g. going Home).</summary>
        public void ResetTo(UiScreen screen)
        {
            CloseAllModals();
            for (int i = _stack.Count - 1; i >= 0; i--) Destroy(_stack[i]);
            _stack.Clear();
            _stale.Clear();
            _stack.Add(screen);
            Show(screen, true);
        }

        /// <summary>Replaces the top screen (e.g. Loadout → Loadout of the next level).</summary>
        public void Replace(UiScreen screen)
        {
            if (Current != null)
            {
                UiScreen old = Current;
                _stack.RemoveAt(_stack.Count - 1);
                Destroy(old);
            }
            _stack.Add(screen);
            Show(screen, true);
        }

        public void Pop()
        {
            if (_stack.Count <= 1) return;
            UiScreen top = Current;
            _stack.RemoveAt(_stack.Count - 1);
            Destroy(top);
            UiScreen now = Current;
            if (_stale.Contains(now) || !now.Root) Rebuild(now, true);
            else
            {
                now.Root.gameObject.SetActive(true);
                BuildHeader(now);
                Animate(now.Root);
                now.OnShow();
            }
            PaintBackground();
        }

        /// <summary>Pops screens until one of type T is on top (returns false if none in the stack).</summary>
        public bool PopTo<T>() where T : UiScreen
        {
            int idx = _stack.FindLastIndex(s => s is T);
            if (idx < 0) return false;
            while (_stack.Count - 1 > idx) Pop();
            return true;
        }

        void Destroy(UiScreen s)
        {
            s.OnHide();
            _stale.Remove(s);
            if (s.Root) Destroy(s.Root.gameObject);
            s.Root = null;
        }

        void Show(UiScreen screen, bool animate)
        {
            screen.Ui = this;
            RectTransform root = UiKit.Rect(_screens, screen.GetType().Name);
            UiKit.Stretch(root);
            screen.Root = root;
            screen.Build(root);
            _stale.Remove(screen);
            BuildHeader(screen);
            PaintBackground();
            if (animate) Animate(root);
            screen.OnShow();
        }

        void Rebuild(UiScreen screen, bool animate)
        {
            if (screen.Root) Destroy(screen.Root.gameObject);
            Show(screen, animate);
        }

        /// <summary>Rebuilds the visible screen (after its data changed).</summary>
        public void Refresh()
        {
            if (Current != null) Rebuild(Current, false);
        }

        static void Animate(RectTransform root)
        {
            var cg = root.GetComponent<CanvasGroup>() ?? root.gameObject.AddComponent<CanvasGroup>();
            if (ServiceLocator.Settings != null && ServiceLocator.Settings.ReduceMotion)
            {
                cg.alpha = 1f;
                return;
            }
            Tween.Fade(cg, 0f, 1f, 0.22f, EaseType.OutQuad);
            Tween.Scale(root, Vector3.one * 0.97f, Vector3.one, 0.22f, EaseType.OutQuad);
        }

        void BuildHeader(UiScreen screen)
        {
            UiKit.Clear(_headerLayer);
            if (screen.Title == null) return;
            RectTransform bar = UiKit.Rect(_headerLayer, "Bar");
            UiKit.TopBand(bar, 0f, 58f, 18f, 18f);
            UiKit.Row(bar, 12f, TextAnchor.MiddleLeft);
            Widgets.BackButton(bar, Back);
            TextMeshProUGUI title = UiKit.Label(bar, screen.Title, FontRole.Display, 25f, UiKit.P.Ink);
            UiKit.Size(title, title.preferredWidth + 2f, 40f);
            if (!string.IsNullOrEmpty(screen.Subtitle))
            {
                TextMeshProUGUI sub = UiKit.Label(bar, screen.Subtitle, FontRole.Body, 13f, UiKit.P.InkMuted);
                UiKit.Size(sub, sub.preferredWidth + 2f, 24f);
            }
            UiKit.Spacer(bar);
            if (screen.ShowCoins) Widgets.CoinPill(bar);
        }

        // ---------- modals ----------

        public void ShowModal(UiModal modal)
        {
            modal.Ui = this;
            _modals.Add(modal);
            BuildModal(modal, true);
        }

        void BuildModal(UiModal modal, bool animate)
        {
            RectTransform root = UiKit.Rect(_modalLayer, modal.GetType().Name);
            UiKit.Stretch(root);
            modal.Root = root;
            Image dim = UiKit.Box(root, "Dim", modal.Dim, 0f);
            UiKit.Stretch(dim.rectTransform);
            dim.raycastTarget = true;
            if (modal.TapOutsideCloses)
            {
                var b = dim.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => CloseModal(modal));
            }
            RectTransform safe = UiKit.Rect(root, "Safe");
            UiKit.Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();
            modal.Build(safe);
            if (!animate) return;
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            Tween.Fade(cg, 0f, 1f, 0.18f);
        }

        void RebuildModal(UiModal modal)
        {
            if (modal.Root) Destroy(modal.Root.gameObject);
            BuildModal(modal, false);
        }

        public void CloseModal(UiModal modal)
        {
            if (!_modals.Remove(modal)) return;
            modal.OnClose();
            if (modal.Root) Destroy(modal.Root.gameObject);
            modal.Root = null;
        }

        public void CloseAllModals()
        {
            for (int i = _modals.Count - 1; i >= 0; i--) CloseModal(_modals[i]);
        }

        public UiModal TopModal => _modals.Count > 0 ? _modals[_modals.Count - 1] : null;

        /// <summary>Pops a dialog panel in (aaPop .28 s) — modals call this on their panel.</summary>
        public static void PopPanel(Transform panel, float delay = 0f)
        {
            if (ServiceLocator.Settings != null && ServiceLocator.Settings.ReduceMotion) return;
            panel.localScale = Vector3.one * 0.3f;
            Tween.Scale(panel, Vector3.one * 0.3f, Vector3.one, 0.28f, EaseType.OutBack, delay);
        }

        // ---------- toasts, back ----------

        public void Toast(string message) => _toasts.Show(message);

        /// <summary>What Android Back (and the header ← button) does.</summary>
        public void Back()
        {
            UiModal m = TopModal;
            if (m != null)
            {
                if (!m.OnBack()) CloseModal(m);
                return;
            }
            UiScreen s = Current;
            if (s != null && s.OnBack()) return;
            if (_stack.Count > 1)
            {
                ServiceLocator.Audio?.Play(SoundId.UiBack);
                Pop();
                return;
            }
            RootBack?.Invoke();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            Current?.Tick(dt);
            for (int i = 0; i < _modals.Count; i++) _modals[i].Tick(dt);
            if (_backCooldown > 0f) _backCooldown -= dt;
            if (BackPressed() && _backCooldown <= 0f && !SceneFlow.Busy)
            {
                _backCooldown = 0.15f;
                Back();
            }
        }

        static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}
