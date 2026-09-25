using TMPro;
using UnityEngine;

namespace ArcherArcade.UI
{
    /// <summary>Keeps a text-shadow copy in step with its label (text, size, visibility).</summary>
    public sealed class ShadowTextSync : MonoBehaviour
    {
        TextMeshProUGUI _front, _back;

        public void Bind(TextMeshProUGUI front, TextMeshProUGUI back)
        {
            _front = front;
            _back = back;
        }

        void LateUpdate()
        {
            if (!_front || !_back) return;
            if (!ReferenceEquals(_back.text, _front.text) && _back.text != _front.text) _back.text = _front.text;
            if (_back.fontSize != _front.fontSize) _back.fontSize = _front.fontSize;
            if (_back.enabled != _front.enabled) _back.enabled = _front.enabled;
        }
    }
}
