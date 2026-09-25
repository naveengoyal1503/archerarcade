using UnityEngine;

namespace ArcherArcade.UI
{
    /// <summary>A dialog over the current screen (dim overlay + popping panel). Back closes it unless handled.</summary>
    public abstract class UiModal
    {
        public UIManager Ui { get; internal set; }
        public RectTransform Root { get; internal set; }

        /// <summary>Dim colour behind the panel (design rgba(20,16,50,.55); some modals use their own).</summary>
        public virtual Color Dim => new Color32(20, 16, 50, 140);
        /// <summary>Tap on the dim area closes the modal.</summary>
        public virtual bool TapOutsideCloses => true;

        public abstract void Build(RectTransform root);
        public virtual bool OnBack() => false;
        public virtual void OnClose() { }
        public virtual void Tick(float dt) { }

        public void Close() => Ui?.CloseModal(this);
    }
}
