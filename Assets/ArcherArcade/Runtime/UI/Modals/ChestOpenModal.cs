using System;
using System.Collections.Generic;
using ArcherArcade.Core;
using ArcherArcade.Feel;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Chest opening (design `chestOpen`, state 11): the chest shakes, bursts open with a glow, loot cards pop in
    /// one after another (coins, then a skin or trail if the chest has one), coins fly to the wallet, Collect.
    /// </summary>
    public sealed class ChestOpenModal : UiModal
    {
        static readonly string[] Art = { "chest_wood", "chest_silver", "chest_gold", "chest_forest" };
        readonly int _chest;
        readonly Action _done;
        RectTransform _stage, _chestRt, _glow, _loot, _title;
        Image _chestImg;
        Button3D _collect;
        float _t;
        int _phase;
        ChestContents _contents;
        readonly List<RectTransform> _rays = new List<RectTransform>();

        public ChestOpenModal(int chest, Action done)
        {
            _chest = chest;
            _done = done;
        }

        public override Color Dim => new Color32(20, 16, 50, 170);
        public override bool TapOutsideCloses => false;

        public override void Build(RectTransform root)
        {
            _stage = UiKit.Rect(root, "Stage");
            UiKit.Stretch(_stage);
            UiKit.HitArea(_stage);
            var tap = _stage.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => { if (_phase == 0) _t = Mathf.Max(_t, 0.9f); });

            for (int i = 0; i < 10; i++)
            {
                Image ray = UiKit.Box(_stage, "Ray", new Color(1f, 0.95f, 0.75f, 0f), 0);
                ray.sprite = ShapeSprites.White;
                RectTransform rt = ray.rectTransform;
                UiKit.At(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(26f, 260f));
                rt.localRotation = Quaternion.Euler(0f, 0f, i * 36f);
                _rays.Add(rt);
            }
            Image glow = UiKit.Disc(_stage, "Glow", new Color(1f, 0.95f, 0.7f, 0f));
            _glow = glow.rectTransform;
            UiKit.At(_glow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(240f, 240f));
            _chestImg = UiKit.Box(_stage, "Chest", Color.white, 0);
            _chestImg.sprite = ArtLibrary.Get(ArtLibrary.Ui, Art[_chest - 1]);
            _chestImg.preserveAspect = true;
            _chestRt = _chestImg.rectTransform;
            UiKit.At(_chestRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(190f, 170f));

            TextMeshProUGUI title = UiKit.ShadowLabel(_stage, Loc.T("chest_tap"), FontRole.Display, 34f, Color.white, new Color(0, 0, 0, 0.2f), 4f);
            _title = title.transform.parent as RectTransform;
            UiKit.At(_title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(700f, 44f));
            _titleText = title;

            _loot = UiKit.Rect(_stage, "Loot");
            UiKit.At(_loot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(560f, 120f));
            UiKit.Row(_loot, 12f, TextAnchor.MiddleCenter);
            _collect = Button3D.Styled(_stage, "Collect", Theme.ButtonStyle.Gold, Loc.T("collect"), 20f, 16f);
            UiKit.At((RectTransform)_collect.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(200f, 50f));
            _collect.gameObject.SetActive(false);
            _collect.OnClick(() =>
            {
                Close();
                _done?.Invoke();
            });
            ServiceLocator.Audio?.Play(SoundId.ChestShake);
        }

        TextMeshProUGUI _titleText;

        public override void Tick(float dt)
        {
            _t += dt;
            if (_phase == 0)
            {
                // Shake harder and harder, then open.
                float k = Mathf.Clamp01(_t / 0.9f);
                _chestRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_t * 40f) * 8f * k);
                _chestRt.localScale = Vector3.one * (1f + Mathf.Sin(_t * 20f) * 0.03f * k);
                if (_t >= 0.9f) Open();
            }
            float spin = _t * 18f;
            for (int i = 0; i < _rays.Count; i++) _rays[i].localRotation = Quaternion.Euler(0f, 0f, i * 36f + spin);
        }

        void Open()
        {
            _phase = 1;
            _chestRt.localRotation = Quaternion.identity;
            _chestRt.localScale = Vector3.one;
            Profile profile = ServiceLocator.Profile;
            _contents = profile.OpenChest(_chest);
            profile.EvaluateBadges();
            ServiceLocator.CommitProfile();
            ServiceLocator.Audio?.Play(SoundId.ChestOpen);
            ServiceLocator.Haptics?.Play(HapticId.Win);
            _chestImg.sprite = ArtLibrary.Get(ArtLibrary.Ui, Art[_chest - 1] + "_open");
            Tween.Scale(_chestRt, Vector3.one * 1.25f, Vector3.one, 0.45f, EaseType.OutBack);
            Tween.Value(0f, 1f, 0.5f, a =>
            {
                if (!_glow) return;
                _glow.GetComponent<Image>().color = new Color(1f, 0.95f, 0.7f, a * 0.85f);
                _glow.localScale = Vector3.one * (0.4f + a * 1.1f);
                foreach (RectTransform r in _rays) if (r) r.GetComponent<Image>().color = new Color(1f, 0.95f, 0.75f, a * 0.28f);
            }, EaseType.OutCubic, 0f, true, null, _glow);
            Tween.AnchoredPosition(_chestRt, _chestRt.anchoredPosition, new Vector2(0f, 96f), 0.5f, EaseType.OutCubic, 0.2f);
            Tween.Scale(_chestRt, Vector3.one, Vector3.one * 0.62f, 0.5f, EaseType.OutCubic, 0.2f);
            _titleText.text = Loc.F("chest_opened_title", Loc.T("chest_" + _chest));

            LootCard(Icons.MonetizationOn, Loc.F("coins_n", _contents.Coins), null, 0.35f);
            if (_contents.CosmeticId != null)
            {
                CosmeticDef c = CosmeticCatalog.ById(_contents.CosmeticId);
                Sprite pic = c != null && c.Kind == CosmeticKind.Skin ? ArtLibrary.Portrait(_contents.CosmeticId, "happy") : null;
                LootCard(c != null && c.Kind == CosmeticKind.Trail ? Icons.AutoAwesome : Icons.Checkroom, Loc.T("cos_" + _contents.CosmeticId), pic, 0.6f);
            }
            CoinFly.Burst(_stage, new Vector2(0f, 60f), 10, 0.5f);
            _collect.gameObject.SetActive(true);
            _collect.PopIn(1.0f);
        }

        void LootCard(string icon, string name, Sprite picture, float delay)
        {
            Image card = UiKit.Box(_loot, "Loot", UiKit.P.Solid, 20f);
            UiKit.Size(card, 150f, 110f);
            UiKit.Column(card, 4f, TextAnchor.MiddleCenter, true, false, new RectOffset(8, 8, 12, 12));
            if (picture)
            {
                Image pic = UiKit.Box(card.transform, "Pic", Color.white, 0);
                pic.sprite = picture;
                pic.preserveAspect = true;
                UiKit.Size(pic, -1, 48f);
            }
            else
            {
                TextMeshProUGUI ic = UiKit.Glyph(card.transform, icon, 38f, icon == Icons.MonetizationOn ? Widgets.StarGold : Widgets.Purple);
                UiKit.Size(ic, -1, 46f);
            }
            TextMeshProUGUI n = UiKit.Label(card.transform, name, FontRole.Display, 17f, UiKit.P.Ink, TextAlignmentOptions.Center);
            UiKit.Size(n, -1, 24f);
            UiKit.Fit(n);
            card.transform.localScale = Vector3.zero;
            Tween.Scale(card.transform, Vector3.one * 0.3f, Vector3.one, 0.45f, EaseType.OutBack, delay);
        }
    }
}
