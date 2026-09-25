using ArcherArcade.Core;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// "New!" idea card (SCREEN_INVENTORY #7, LEVELS per-level rules): icon, title, one sentence and a small looping
    /// demo (tip cards show their impact effect). Shown once per idea (flag saved); re-readable in How to play.
    /// </summary>
    public sealed class IdeaCardModal : UiModal
    {
        readonly IdeaCard _card;
        readonly bool _markSeen;
        RectTransform _icon, _fx, _fx2;
        float _t;

        public IdeaCardModal(IdeaCard card, bool markSeen)
        {
            _card = card;
            _markSeen = markSeen;
        }

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Color c = GameVisuals.IdeaColor(_card);
            Image panel = UiKit.Box(root, "Panel", p.Solid, 26f);
            panel.raycastTarget = true;
            UiKit.At(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(470f, 200f));
            UiKit.SoftShadow(panel.transform, new Color(0, 0, 0, 0.25f), 26f, 30f, 10f);

            // Demo tile on the left.
            Image tile = UiKit.Box(panel.transform, "Demo", c, 20f);
            UiKit.TopLeft(tile.rectTransform, 18f, 18f, 164f, 164f);
            tile.gameObject.AddComponent<RectMask2D>();
            Image glow = UiKit.Disc(tile.transform, "Glow", new Color(1, 1, 1, 0.22f));
            UiKit.At(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130f, 130f));
            TextMeshProUGUI ic = UiKit.Glyph(tile.transform, GameVisuals.IdeaIcon(_card), 72f, Color.white);
            _icon = (RectTransform)ic.transform;
            UiKit.At(_icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
            string fx = FxFor(_card);
            if (fx != null)
            {
                Image f = UiKit.Box(tile.transform, "Fx", Color.white, 0);
                f.sprite = ArtLibrary.Get(ArtLibrary.Fx, fx);
                f.preserveAspect = true;
                _fx = f.rectTransform;
                UiKit.At(_fx, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, 30f), new Vector2(56f, 56f));
                Image f2 = UiKit.Box(tile.transform, "Fx2", new Color(1, 1, 1, 0.8f), 0);
                f2.sprite = f.sprite;
                f2.preserveAspect = true;
                _fx2 = f2.rectTransform;
                UiKit.At(_fx2, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-38f, -34f), new Vector2(38f, 38f));
            }

            RectTransform right = UiKit.Rect(panel.transform, "Words");
            UiKit.Stretch(right, 200f, 18f, 20f, 18f);
            UiKit.Column(right, 6f, TextAnchor.UpperLeft, true, false);
            RectTransform chipRow = UiKit.Rect(right, "ChipRow");
            UiKit.Size(chipRow, -1, 22f);
            UiKit.Row(chipRow, 0f, TextAnchor.MiddleLeft);
            Widgets.Chip(chipRow, Loc.T("ld_new"), Widgets.Pink, Color.white, 12f, 6f);
            TextMeshProUGUI title = UiKit.Label(right, Loc.T("idea_" + _card), FontRole.Display, 24f, p.Ink);
            UiKit.Size(title, -1, 30f);
            UiKit.Fit(title);
            TextMeshProUGUI text = UiKit.Paragraph(right, Loc.T("idea_" + _card + "_text"), FontRole.BodyBold, 14f, p.InkMuted, TextAlignmentOptions.TopLeft, 4f);
            UiKit.Size(text, -1, 64f);
            RectTransform btnRow = UiKit.Rect(right, "Buttons");
            UiKit.Size(btnRow, -1, 48f);
            Button3D ok = Button3D.Create(btnRow, "GotIt", c, Color.Lerp(c, Color.black, 0.28f), 14f, 4f, 3f);
            UiKit.At((RectTransform)ok.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 2f), new Vector2(170f, 44f));
            ok.AddLabel(Loc.T("idea_got_it"), FontRole.Display, 18f, Color.white);
            ok.OnClick(Close);
            UIManager.PopPanel(panel.transform);
            ServiceLocator.Audio?.Play(Feel.SoundId.Badge, 0.6f);
        }

        static string FxFor(IdeaCard c)
        {
            switch (c)
            {
                case IdeaCard.TipFire:
                case IdeaCard.ExplosiveBarrels: return "flame";
                case IdeaCard.TipElectric: return "spark";
                case IdeaCard.TipIce: return "ice_shard";
                case IdeaCard.TipPoison: return "bubble_small";
                case IdeaCard.TipBomb:
                case IdeaCard.TntCrates: return "puff";
                case IdeaCard.Wind: return "leaf";
                case IdeaCard.Headshots:
                case IdeaCard.Boss:
                case IdeaCard.MiniBoss: return "star";
                case IdeaCard.AppleShot:
                case IdeaCard.SwingingTargets: return "star";
                default: return "spark";
            }
        }

        public override void Tick(float dt)
        {
            _t += dt;
            if (_icon)
            {
                float bob = Mathf.Sin(_t * 2.6f) * 6f;
                float rot = _card == IdeaCard.Wind || _card == IdeaCard.SwingingTargets ? Mathf.Sin(_t * 2f) * 14f : 0f;
                _icon.anchoredPosition = new Vector2(0f, bob);
                _icon.localRotation = Quaternion.Euler(0f, 0f, rot);
                float s = 1f + Mathf.Sin(_t * 5f) * 0.04f;
                _icon.localScale = new Vector3(s, s, 1f);
            }
            if (_fx)
            {
                float k = Mathf.Repeat(_t, 1.2f) / 1.2f;
                _fx.localScale = Vector3.one * (0.6f + k * 0.6f);
                _fx.localRotation = Quaternion.Euler(0f, 0f, _t * 90f);
                _fx.GetComponent<Image>().color = new Color(1, 1, 1, 1f - k * 0.8f);
                float k2 = Mathf.Repeat(_t + 0.6f, 1.2f) / 1.2f;
                _fx2.localScale = Vector3.one * (0.6f + k2 * 0.6f);
                _fx2.anchoredPosition = new Vector2(-38f, -34f + k2 * 20f);
                _fx2.GetComponent<Image>().color = new Color(1, 1, 1, 1f - k2 * 0.8f);
            }
        }

        public override void OnClose()
        {
            if (!_markSeen) return;
            ServiceLocator.Profile.MarkSeen(_card);
            ServiceLocator.Save.MarkDirty();
        }
    }
}
