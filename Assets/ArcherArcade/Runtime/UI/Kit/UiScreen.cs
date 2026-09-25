using UnityEngine;

namespace ArcherArcade.UI
{
    /// <summary>
    /// One screen of the menus (SCREEN_INVENTORY). Screens are plain objects that build their UI into a root
    /// (rebuilt from scratch when the theme or language changes, so all colors and texts stay right).
    /// </summary>
    public abstract class UiScreen
    {
        public UIManager Ui { get; internal set; }
        public RectTransform Root { get; internal set; }

        /// <summary>Header title (null = no header, e.g. Home, Splash).</summary>
        public virtual string Title => null;
        public virtual string Subtitle => null;
        public virtual bool ShowCoins => true;
        /// <summary>A full-screen background of its own (splash, result: radial gradients); null = themed sky.</summary>
        public virtual Texture Background => null;
        /// <summary>No background at all: the game world shows through (match HUD).</summary>
        public virtual bool SeeThrough => false;

        public abstract void Build(RectTransform root);
        public virtual void OnShow() { }
        public virtual void OnHide() { }
        /// <summary>Android Back: return true when the screen handled it (e.g. closed a panel).</summary>
        public virtual bool OnBack() => false;
        public virtual void Tick(float dt) { }
    }
}
