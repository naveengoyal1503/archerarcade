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
    /// <summary>"Fire Archer unlocked!" (SCREEN_INVENTORY #19): the new archer cheering, Try now / Later.</summary>
    public sealed class UnlockModal : UiModal
    {
        readonly string _archer;
        readonly Action _tryNow;

        public UnlockModal(string archerId, Action tryNow)
        {
            _archer = archerId;
            _tryNow = tryNow;
        }

        public override Color Dim => new Color32(20, 16, 50, 170);

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            ArcherDef def = ArcherTable.Hero(_archer);
            Image panel = UiKit.Box(root, "Panel", p.Solid, 26f);
            panel.raycastTarget = true;
            UiKit.At(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 250f));
            UiKit.SoftShadow(panel.transform, new Color(0, 0, 0, 0.25f), 26f, 30f, 10f);
            Image stageBg = UiKit.Box(panel.transform, "StageBg", UiKit.Hex(ArcherLooks.Color(_archer), 0.35f), 20f);
            UiKit.TopLeft(stageBg.rectTransform, 16f, 16f, 180f, 218f);
            ArcherStage stage = ArcherStage.Create(stageBg.rectTransform, ArcherLooks.ForHero(_archer, CosmeticCatalog.DefaultSkinFor(_archer)), true);
            stage.View?.Victory();

            RectTransform words = UiKit.Rect(panel.transform, "Words");
            UiKit.Stretch(words, 214f, 24f, 20f, 18f);
            UiKit.Column(words, 6f, TextAnchor.UpperLeft, true, false);
            TextMeshProUGUI t = UiKit.Label(words, Loc.F("unlock_title", def.Name), FontRole.Display, 24f, p.Ink);
            UiKit.Size(t, -1, 30f);
            UiKit.Fit(t, 0.6f);
            TextMeshProUGUI b = UiKit.Label(words, Loc.T("unlock_body"), FontRole.BodyBold, 13f, p.InkMuted);
            UiKit.Size(b, -1, 20f);
            TextMeshProUGUI pass = UiKit.Paragraph(words, "<b>" + Loc.T("archers_passive") + "</b>" + Loc.T("passive_" + _archer), FontRole.BodyBold, 12f, p.Ink, TextAlignmentOptions.TopLeft);
            UiKit.Size(pass, -1, 34f);
            TextMeshProUGUI ab = UiKit.Paragraph(words, "<b>" + Loc.T("ability_" + def.Ability) + ":</b> " + Loc.T("ability_" + def.Ability + "_desc"), FontRole.BodyBold, 12f, p.Ink, TextAlignmentOptions.TopLeft);
            UiKit.Size(ab, -1, 34f);
            UiKit.Spacer(words);
            RectTransform row = UiKit.Rect(words, "Buttons");
            UiKit.Size(row, -1, 50f);
            UiKit.Row(row, 10f, TextAnchor.MiddleLeft, true, true, new RectOffset(0, 0, 3, 5));
            Button3D later = Button3D.Off(row, "Later", Loc.T("unlock_later"), 15f);
            later.OnClick(Close);
            Button3D go = Button3D.Styled(row, "Try", ButtonStyle.Gold, Loc.T("unlock_try"), 16f);
            go.OnClick(() =>
            {
                ServiceLocator.Profile.EquipArcher(_archer);
                ServiceLocator.CommitProfile();
                Close();
                _tryNow?.Invoke();
            });
            UIManager.PopPanel(panel.transform);
            ServiceLocator.Audio?.Play(Feel.SoundId.Cheer, 0.7f);
            ServiceLocator.Haptics?.Play(Feel.HapticId.Win);
        }
    }
}
