using System;
using UnityEngine;

namespace ArcherArcade.Tweening
{
    /// <summary>
    /// Own tween utility (no DOTween). Tweens live in a fixed pool of slots driven by one hidden runner, so starting
    /// a tween allocates nothing except the caller's callbacks. UI tweens use unscaled time by default, so they keep
    /// running while the game is paused (timeScale 0) or in slow-mo.
    /// </summary>
    public static class Tween
    {
        struct Slot
        {
            public bool Active;
            public int Version;
            public object Target;
            public float From;
            public float To;
            public float Duration;
            public float Delay;
            public float Elapsed;
            public EaseType Ease;
            public bool Unscaled;
            public Action<float> OnUpdate;
            public Action OnComplete;
        }

        const int Capacity = 256;
        static readonly Slot[] Slots = new Slot[Capacity];
        static int _versionCounter;
        static TweenRunner _runner;

        public static TweenHandle Value(float from, float to, float duration, Action<float> onUpdate,
            EaseType ease = EaseType.OutQuad, float delay = 0f, bool unscaled = true, Action onComplete = null,
            object target = null)
        {
            EnsureRunner();
            int index = FindFreeSlot();
            _versionCounter = _versionCounter == int.MaxValue ? 1 : _versionCounter + 1;
            Slots[index] = new Slot
            {
                Active = true,
                Version = _versionCounter,
                Target = target,
                From = from,
                To = to,
                Duration = Mathf.Max(0f, duration),
                Delay = Mathf.Max(0f, delay),
                Elapsed = 0f,
                Ease = ease,
                Unscaled = unscaled,
                OnUpdate = onUpdate,
                OnComplete = onComplete
            };
            if (delay <= 0f) onUpdate?.Invoke(from);
            return new TweenHandle(index, _versionCounter);
        }

        public static TweenHandle Scale(Transform t, Vector3 from, Vector3 to, float duration,
            EaseType ease = EaseType.OutBack, float delay = 0f, Action onComplete = null)
        {
            return Value(0f, 1f, duration, k => { if (t) t.localScale = Vector3.LerpUnclamped(from, to, k); },
                ease, delay, true, onComplete, t);
        }

        public static TweenHandle Fade(CanvasGroup group, float from, float to, float duration,
            EaseType ease = EaseType.OutQuad, float delay = 0f, Action onComplete = null)
        {
            return Value(from, to, duration, a => { if (group) group.alpha = a; }, ease, delay, true, onComplete, group);
        }

        public static TweenHandle AnchoredPosition(RectTransform rt, Vector2 from, Vector2 to, float duration,
            EaseType ease = EaseType.OutCubic, float delay = 0f, Action onComplete = null)
        {
            return Value(0f, 1f, duration, k => { if (rt) rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k); },
                ease, delay, true, onComplete, rt);
        }

        public static bool IsRunning(TweenHandle h)
        {
            return h.IsValid && h.Slot >= 0 && h.Slot < Capacity && Slots[h.Slot].Active && Slots[h.Slot].Version == h.Version;
        }

        public static void Kill(TweenHandle h, bool complete = false)
        {
            if (!IsRunning(h)) return;
            Finish(h.Slot, complete);
        }

        /// <summary>Kills every tween started with this target (screens call this when they close).</summary>
        public static void KillTarget(object target, bool complete = false)
        {
            if (target == null) return;
            for (int i = 0; i < Capacity; i++)
            {
                if (Slots[i].Active && ReferenceEquals(Slots[i].Target, target)) Finish(i, complete);
            }
        }

        public static void KillAll()
        {
            for (int i = 0; i < Capacity; i++) Slots[i] = default;
        }

        internal static void Update(float scaledDt, float unscaledDt)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (!Slots[i].Active) continue;
                float dt = Slots[i].Unscaled ? unscaledDt : scaledDt;
                if (Slots[i].Delay > 0f)
                {
                    Slots[i].Delay -= dt;
                    if (Slots[i].Delay > 0f) continue;
                    dt = -Slots[i].Delay;
                    Slots[i].Delay = 0f;
                }
                Slots[i].Elapsed += dt;
                if (Slots[i].Elapsed >= Slots[i].Duration)
                {
                    Finish(i, true);
                    continue;
                }
                float k = Ease.Evaluate(Slots[i].Ease, Slots[i].Elapsed / Slots[i].Duration);
                Slots[i].OnUpdate?.Invoke(Mathf.LerpUnclamped(Slots[i].From, Slots[i].To, k));
            }
        }

        static void Finish(int index, bool complete)
        {
            Slot s = Slots[index];
            Slots[index] = default;
            if (!complete) return;
            s.OnUpdate?.Invoke(s.To);
            s.OnComplete?.Invoke();
        }

        static int FindFreeSlot()
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (!Slots[i].Active) return i;
            }
            // Pool full: finish the oldest tween so new UI feedback is never dropped.
            int oldest = 0;
            for (int i = 1; i < Capacity; i++)
            {
                if (Slots[i].Version < Slots[oldest].Version) oldest = i;
            }
            Finish(oldest, true);
            return oldest;
        }

        static void EnsureRunner()
        {
            if (_runner) return;
            var go = new GameObject("[Tween]");
            go.hideFlags = HideFlags.HideInHierarchy;
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<TweenRunner>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            KillAll();
            _versionCounter = 0;
            _runner = null;
        }
    }
}
