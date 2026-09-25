using System;
using ArcherArcade.Archers;
using ArcherArcade.Core;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>Archer upgrade sheet (SCREEN_INVENTORY #9): level n → n+1, stat changes, coin cost, confirm.</summary>
    public sealed class UpgradeModal : UiModal
    {
        readonly string _id;
        readonly Action _done;

        public UpgradeModal(string archerId, Action done)
        {
            _id = archerId;
            _done = done;
        }

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Profile profile = ServiceLocator.Profile;
            ArcherDef def = ArcherTable.Hero(_id);
            DamageConfig dmg = ServiceLocator.Config.Damage;
            int level = profile.ArcherLevel(_id), next = level + 1, cost = profile.UpgradeCost(_id);

            Image panel = UiKit.Box(root, "Panel", p.Solid, 26f);
            panel.raycastTarget = true;
            UiKit.At(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 0f));
            UiKit.SoftShadow(panel.transform, new Color(0, 0, 0, 0.25f), 26f, 30f, 10f);
            UiKit.Column(panel, 8f, TextAnchor.UpperCenter, true, false, new RectOffset(22, 22, 18, 20));
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform top = UiKit.Rect(panel.transform, "Top");
            UiKit.Size(top, -1, 64f);
            UiKit.Row(top, 12f, TextAnchor.MiddleCenter);
            RectTransform av = UiKit.Rect(top, "Avatar");
            UiKit.Size(av, 60f, 60f);
            Image disc = UiKit.Disc(av, "Disc", UiKit.Hex(ArcherLooks.Color(_id)));
            UiKit.Stretch(disc.rectTransform);
            string skin = profile.Data.EquippedSkins.TryGetValue(_id, out string s) ? s : CosmeticCatalog.DefaultSkinFor(_id);
            Image face = UiKit.Box(av, "Face", Color.white, 0);
            face.sprite = ArtLibrary.Portrait(ArcherLooks.ForHero(_id, skin), "happy");
            face.preserveAspect = true;
            UiKit.Stretch(face.rectTransform);
            RectTransform words = UiKit.Rect(top, "Words");
            UiKit.Size(words, 220f, 60f);
            UiKit.Column(words, 2f, TextAnchor.MiddleLeft, true, false);
            TextMeshProUGUI t = UiKit.Label(words, Loc.F("upgrade_title", def.Name), FontRole.Display, 21f, p.Ink);
            UiKit.Size(t, -1, 26f);
            UiKit.Fit(t);
            TextMeshProUGUI lv = UiKit.Label(words, Loc.F("upgrade_level", level, next), FontRole.Body, 13f, p.InkMuted);
            UiKit.Size(lv, -1, 18f);

            int hpA = def.BaseHp + (level - 1) * dmg.HpPerLevel, hpB = hpA + dmg.HpPerLevel;
            double dA = def.BaseDamage * (1.0 + (level - 1) * dmg.DamagePerLevel), dB = def.BaseDamage * (1.0 + level * dmg.DamagePerLevel);
            Row(panel.transform, Icons.Favorite, Loc.T("stat_hp"), hpA.ToString(), hpB.ToString(), Widgets.Green, p);
            Row(panel.transform, Icons.Bolt, Loc.T("stat_damage"), dA.ToString("0.0"), dB.ToString("0.0"), Widgets.Red, p);

            RectTransform buttons = UiKit.Rect(panel.transform, "Buttons");
            UiKit.Size(buttons, -1, 52f);
            UiKit.Row(buttons, 10f, TextAnchor.MiddleCenter, true, true, new RectOffset(0, 0, 4, 4));
            Button3D cancel = Button3D.Off(buttons, "Cancel", Loc.T("cancel"), 16f);
            cancel.OnClick(Close);
            Button3D go = Button3D.Styled(buttons, "Upgrade", ButtonStyle.Gold, Loc.F("upgrade_confirm", Fmt.Coins(cost)), 17f);
            go.Sound = Feel.SoundId.UiConfirm;
            go.OnClick(() =>
            {
                UpgradeResult r = profile.Upgrade(_id);
                if (r == UpgradeResult.NotEnoughCoins)
                {
                    Close();
                    Ui.ShowModal(new CoinsModal(profile.Missing(cost)));
                    return;
                }
                if (r != UpgradeResult.Done) return;
                profile.EvaluateBadges();
                ServiceLocator.CommitProfile();
                ServiceLocator.Audio?.Play(Feel.SoundId.Coin);
                ServiceLocator.Audio?.Play(Feel.SoundId.Badge, 0.7f);
                Ui.Toast(Loc.F("upgrade_done", def.Name, next));
                Close();
                _done?.Invoke();
            });
            UIManager.PopPanel(panel.transform);
        }

        static void Row(Transform parent, string icon, string label, string from, string to, Color c, Palette p)
        {
            Image row = UiKit.Box(parent, label, p.Track, 12f);
            UiKit.Size(row, -1, 36f);
            UiKit.Row(row, 8f, TextAnchor.MiddleLeft, false, false, new RectOffset(12, 12, 0, 0));
            TextMeshProUGUI ic = UiKit.Glyph(row.transform, icon, 18f, c);
            UiKit.Size(ic, 22f, 22f);
            TextMeshProUGUI l = UiKit.Label(row.transform, label, FontRole.Body, 13f, p.Ink);
            UiKit.Size(l, 90f, 20f);
            UiKit.Spacer(row.transform);
            TextMeshProUGUI v = UiKit.Label(row.transform, from + "  →  <color=" + Fmt.Hex(c) + ">" + to + "</color>", FontRole.Display, 16f, p.Ink, TextAlignmentOptions.MidlineRight);
            UiKit.Size(v, 140f, 20f);
        }
    }
}
