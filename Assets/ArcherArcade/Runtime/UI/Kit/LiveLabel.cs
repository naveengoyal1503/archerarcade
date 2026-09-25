using System;
using TMPro;
using UnityEngine;

namespace ArcherArcade.UI
{
    /// <summary>Label whose text follows a value while it changes (e.g. "Music · 70" while dragging).</summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class LiveLabel : MonoBehaviour
    {
        Func<string> _text;
        TextMeshProUGUI _label;

        public void Bind(Func<string> text)
        {
            _text = text;
            _label = GetComponent<TextMeshProUGUI>();
        }

        void LateUpdate()
        {
            if (_text == null || !_label) return;
            string s = _text();
            if (s != _label.text) _label.text = s;
        }
    }
}
