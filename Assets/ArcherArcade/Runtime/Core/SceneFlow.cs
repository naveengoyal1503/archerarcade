using System.Collections;
using ArcherArcade.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArcherArcade.Core
{
    /// <summary>
    /// Moves between the Home and Match scenes with a quick fade (250 ms, DESIGN_TOKENS §7) and hands over what to
    /// open next: <see cref="PendingMatch"/> for the Match scene, <see cref="PendingHome"/> for the Home scene.
    /// </summary>
    public static class SceneFlow
    {
        const float FadeSeconds = 0.25f;

        public static MatchRequest PendingMatch { get; private set; }
        public static HomeTarget PendingHome { get; private set; } = HomeTarget.Home;
        public static bool Busy { get; private set; }

        static CanvasGroup _fader;

        public static void StartMatch(MatchRequest request)
        {
            PendingMatch = request;
            Go(SceneNames.Match);
        }

        public static void GoHome(HomeTarget target = HomeTarget.Home)
        {
            PendingHome = target;
            Go(SceneNames.Home);
        }

        /// <summary>Consumes the Home target (the Home scene reads it once when it opens).</summary>
        public static HomeTarget TakeHomeTarget()
        {
            HomeTarget t = PendingHome;
            PendingHome = HomeTarget.Home;
            return t;
        }

        static void Go(string scene)
        {
            if (Busy) return;
            GameManager.Ensure();
            GameManager.Instance.StartCoroutine(Run(scene));
        }

        static IEnumerator Run(string scene)
        {
            Busy = true;
            CanvasGroup fader = Fader();
            fader.blocksRaycasts = true;
            yield return FadeTo(fader, 1f);
            Time.timeScale = 1f;
            Tween.KillAll();
            AsyncOperation op = SceneManager.LoadSceneAsync(scene);
            while (op != null && !op.isDone) yield return null;
            yield return null;
            yield return FadeTo(fader, 0f);
            fader.blocksRaycasts = false;
            Busy = false;
        }

        static IEnumerator FadeTo(CanvasGroup g, float target)
        {
            float from = g.alpha, t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, target, t / FadeSeconds);
                yield return null;
            }
            g.alpha = target;
        }

        static CanvasGroup Fader()
        {
            if (_fader) return _fader;
            var go = new GameObject("[Fader]", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            Object.DontDestroyOnLoad(go);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var img = new GameObject("Veil", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(go.transform, false);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.GetComponent<Image>().color = new Color32(0x15, 0x12, 0x2E, 0xFF);
            _fader = go.GetComponent<CanvasGroup>();
            _fader.alpha = 0f;
            _fader.blocksRaycasts = false;
            _fader.interactable = false;
            return _fader;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            PendingMatch = null;
            PendingHome = HomeTarget.Home;
            Busy = false;
            _fader = null;
        }
    }
}
