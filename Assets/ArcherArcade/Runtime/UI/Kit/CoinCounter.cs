using ArcherArcade.Core;
using ArcherArcade.Tweening;
using TMPro;
using UnityEngine;

namespace ArcherArcade.UI
{
    /// <summary>Keeps a coin label equal to the wallet; counts up/down with a small bump when coins change.</summary>
    public sealed class CoinCounter : MonoBehaviour
    {
        TextMeshProUGUI _label;
        int _shown;
        TweenHandle _count;

        public void Bind(TextMeshProUGUI label)
        {
            _label = label;
            _shown = ServiceLocator.Profile?.Coins ?? 0;
            _label.text = Loc.N(_shown);
        }

        void OnEnable() => GameEvents.CoinsChanged += Changed;
        void OnDisable() => GameEvents.CoinsChanged -= Changed;

        void Changed()
        {
            if (!_label || ServiceLocator.Profile == null) return;
            int target = ServiceLocator.Profile.Coins;
            if (target == _shown) return;
            int from = _shown;
            _shown = target;
            _count.Kill();
            _count = Tween.Value(from, target, 0.6f, v => { if (_label) _label.text = Loc.N(Mathf.RoundToInt(v)); }, EaseType.OutCubic, 0f, true, null, this);
            Tween.Scale(transform, Vector3.one * 1.12f, Vector3.one, 0.3f, EaseType.OutBack);
        }

        void OnDestroy() => Tween.KillTarget(this);
    }
}
