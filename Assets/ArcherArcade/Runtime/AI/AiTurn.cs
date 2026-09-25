using ArcherArcade.Logic;
using UnityEngine;

namespace ArcherArcade.AI
{
    /// <summary>
    /// How the computer's turn looks (SCREEN_INVENTORY 13): it "thinks" for a moment, then visibly draws — the bow
    /// swings to its chosen angle with a small overshoot and settles while the pull grows — and releases after
    /// <see cref="AiDecision.ThinkSeconds"/> (already slowed by Ice). The shot itself was decided by Logic.AiPlayer.
    /// </summary>
    public sealed class AiTurn
    {
        public const float RestAngle = 18f;

        readonly AiDecision _decision;
        readonly float _think, _aim;
        float _t;

        public AiTurn(AiDecision decision)
        {
            _decision = decision;
            float total = Mathf.Clamp((float)decision.ThinkSeconds, 0.9f, 4f);
            _think = total * 0.4f;
            _aim = total - _think;
        }

        public AiDecision Decision => _decision;
        public bool Thinking => _t < _think;
        public bool Done => _t >= _think + _aim;

        /// <summary>Advances the animation; outputs the bow angle and draw (0..1) to show.</summary>
        public void Tick(float dt, out float angle, out float draw)
        {
            _t += dt;
            float target = (float)_decision.Input.AngleDeg;
            float power = (float)_decision.Input.Power;
            if (_t < _think)
            {
                angle = RestAngle + Mathf.Sin(_t * 3f) * 3f;
                draw = 0f;
                return;
            }
            float k = Mathf.Clamp01((_t - _think) / _aim);
            float swing = 1f - Mathf.Pow(1f - Mathf.Clamp01(k * 1.6f), 3f);
            float overshoot = Mathf.Sin(Mathf.Clamp01(k * 1.6f) * Mathf.PI) * 6f * (1f - k);
            angle = Mathf.Lerp(RestAngle, target, swing) + overshoot + Mathf.Sin(_t * 9f) * 0.6f * (1f - k);
            draw = power * Tweening.Ease.Evaluate(Tweening.EaseType.OutQuad, Mathf.Clamp01(k * 1.25f));
        }
    }
}
