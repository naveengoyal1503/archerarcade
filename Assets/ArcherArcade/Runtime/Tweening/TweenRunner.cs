using UnityEngine;

namespace ArcherArcade.Tweening
{
    /// <summary>Hidden driver for <see cref="Tween"/>. Created on first use.</summary>
    [DefaultExecutionOrder(-500)]
    public sealed class TweenRunner : MonoBehaviour
    {
        void Update()
        {
            Tween.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }
    }
}
