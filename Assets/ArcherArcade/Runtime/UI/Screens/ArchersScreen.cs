using System.Collections.Generic;
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
    /// <summary>
    /// Archer select (design `archers`, state 09): roster, the chosen archer on a turntable (tap to turn), skin
    /// dots, level pips, HP / damage bars, passive, ability card with a live demo, Select and Upgrade. Locked
    /// archers show exactly what unlocks them — always by playing, never by paying.
    /// </summary>
    public sealed class ArchersScreen : UiScreen
    {
        string _view;
        ArcherStage _stage;

        public ArchersScreen(string archerId) => _view = archerId;

        public override string Title => Loc.T("title_archers");

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Profile profile = ServiceLocator.Profile;
            RectTransform row = UiKit.Rect(root, "Row");
            UiKit.Stretch(row, 18f, 62f, 18f, 16f);
            UiKit.Row(row, 14f, TextAnchor.UpperLeft, false, true);

            BuildRoster(row, profile, p);
            BuildStage(row, profile, p);
            BuildCard(row, profile, p);
        }

        void BuildRoster(RectTransform row, Profile profile, Palette p)
        {
            RectTransform roster = UiKit.Rect(row, "Roster");
            UiKit.Size(roster, 236f, -1, 0f, 1f);
            GridLayoutGroup g = UiKit.Grid(roster, new Vector2(114f, 112f), new Vector2(8f, 8f), 2);
            g.padding = new RectOffset(2, 2, 2, 2);
            foreach (string id in ArcherTable.HeroIds)
            {
                bool own = profile.OwnsArcher(id);
                bool viewing = id == _view, selected = id == profile.Data.EquippedArcher;
                Button3D b = Button3D.Create(roster, id, p.Solid, p.Shadow, 16f, 3f, 2f);
                if (viewing || selected) UiKit.Border(b.Face.transform, viewing ? Widgets.Purple : Widgets.Green, 16f, 3f);
                RectTransform body = b.Body;
                Image disc = UiKit.Disc(body, "Disc", UiKit.Hex(ArcherLooks.Color(id), own ? 1f : 0.45f));
                UiKit.At(disc.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(62f, 62f));
                Image face = UiKit.Box(disc.transform, "Portrait", own ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.8f), 0);
                face.sprite = ArtLibrary.Portrait(ArcherLooks.ForHero(id, Skin(profile, id)));
                face.preserveAspect = true;
                UiKit.Stretch(face.rectTransform, 2, 2, 2, 2);
                TextMeshProUGUI name = UiKit.Label(body, ArcherTable.Hero(id).Name, FontRole.Body, 11.5f, p.Ink, TextAlignmentOptions.Center);
                UiKit.At((RectTransform)name.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(108f, 18f));
                UiKit.Fit(name);
                string badge = own ? Loc.F("ld_tip_lv", profile.ArcherLevel(id)) : Icons.Lock;
                TextMeshProUGUI lv = UiKit.Label(body, badge, own ? FontRole.Body : FontRole.Icon, own ? 10f : 13f, p.InkMuted, TextAlignmentOptions.TopRight);
                UiKit.At((RectTransform)lv.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -6f), new Vector2(50f, 16f));
                string target = id;
                b.OnClick(() =>
                {
                    _view = target;
                    Ui.Refresh();
                });
            }
        }

        static string Skin(Profile profile, string archer) =>
            profile.Data.EquippedSkins.TryGetValue(archer, out string s) ? s : CosmeticCatalog.DefaultSkinFor(archer);

        void BuildStage(RectTransform row, Profile profile, Palette p)
        {
            bool own = profile.OwnsArcher(_view);
            RectTransform stage = UiKit.Rect(row, "Stage");
            UiKit.Size(stage, 200f, -1, 0f, 1f);
            TextMeshProUGUI hint = UiKit.Label(stage, Loc.T("archers_tap_turn"), FontRole.Body, 11f, p.InkMuted, TextAlignmentOptions.Center);
            UiKit.At((RectTransform)hint.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(190f, 16f));
            Image shadow = UiKit.Disc(stage, "Shadow", new Color(0, 0, 0, 0.16f));
            shadow.preserveAspect = false;
            UiKit.At(shadow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 62f), new Vector2(170f, 28f));
            RectTransform box = UiKit.Rect(stage, "Archer");
            UiKit.At(box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(200f, 210f));
            _stage = ArcherStage.Create(box, ArcherLooks.ForHero(_view, Skin(profile, _view)), true);
            if (!own) _stage.GetComponent<RawImage>().color = new Color(0.55f, 0.55f, 0.62f, 1f);

            RectTransform dots = UiKit.Rect(stage, "Skins");
            UiKit.At(dots, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(190f, 30f));
            UiKit.Row(dots, 8f, TextAnchor.MiddleCenter);
            foreach (CosmeticDef skin in CosmeticCatalog.SkinsFor(_view)) SkinDot(dots, skin, profile, own);
        }

        void SkinDot(RectTransform parent, CosmeticDef skin, Profile profile, bool archerOwned)
        {
            Palette p = UiKit.P;
            bool owned = profile.Owns(skin.Id) || skin.Source == CosmeticSource.Default;
            bool equipped = Skin(profile, _view) == skin.Id;
            RigAsset rig = ArtLibrary.Rig(ArcherLooks.ForHero(_view, skin.Id));
            Color swatch = rig != null && rig.Swatches.Length > 0 ? rig.Swatches[0] : Color.gray;
            Button3D b = Button3D.Create(parent, skin.Id, swatch, Color.clear, 13f, 0f, 1f);
            UiKit.Size(b, 28f, 28f);
            b.Face.sprite = ShapeSprites.Circle;
            b.Face.type = Image.Type.Simple;
            Image ring = UiKit.Box(b.Body, "Ring", equipped ? Widgets.Purple : p.Solid, 0);
            ring.sprite = ShapeSprites.Outline(14f, 3f);
            ring.type = Image.Type.Sliced;
            UiKit.Stretch(ring.rectTransform, -2, -2, -2, -2);
            if (!owned) b.AddIcon(Icons.Lock, 12f, Color.white);
            b.OnClick(() =>
            {
                string name = Loc.T("cos_" + skin.Id);
                if (!owned)
                {
                    if (skin.Source == CosmeticSource.Badge)
                        Ui.Toast(Loc.F("archers_skin_badge", name, BadgeCatalog.ById(skin.BadgeId)?.Name ?? skin.BadgeId, TierName(skin.BadgeTier)));
                    else Ui.Toast(Loc.F("archers_skin_locked", name));
                    _stage.SetLook(ArcherLooks.ForHero(_view, skin.Id));
                    return;
                }
                if (!archerOwned) return;
                if (skin.Source == CosmeticSource.Default) profile.Data.EquippedSkins[_view] = skin.Id;
                else profile.Equip(skin.Id);
                ServiceLocator.CommitProfile();
                Ui.Refresh();
            });
        }

        static string TierName(BadgeTier t) => t == BadgeTier.Gold ? Loc.T("tier_gold") : t == BadgeTier.Silver ? Loc.T("tier_silver") : Loc.T("tier_bronze");

        void BuildCard(RectTransform row, Profile profile, Palette p)
        {
            ArcherDef def = ArcherTable.Hero(_view);
            bool own = profile.OwnsArcher(_view);
            int level = own ? profile.ArcherLevel(_view) : 1;
            int max = profile.Economy.MaxArcherLevel;
            DamageConfig dmg = ServiceLocator.Config.Damage;

            Image card = UiKit.Card(row, "Card", 22f);
            UiKit.Size(card, -1, -1, 1f, 1f);
            UiKit.Column(card, 8f, TextAnchor.UpperLeft, true, false, new RectOffset(16, 16, 14, 14));

            RectTransform head = UiKit.Rect(card.transform, "Head");
            UiKit.Size(head, -1, 30f);
            UiKit.Row(head, 8f, TextAnchor.MiddleLeft);
            TextMeshProUGUI name = UiKit.Label(head, def.Name, FontRole.Display, 24f, p.Ink);
            UiKit.Size(name, name.preferredWidth + 2f, 30f);
            string el = "el_" + def.Element.ToString().ToLowerInvariant();
            TextMeshProUGUI chip = Widgets.Chip(head, ArcherLooks.ElementIcon(def.Element) + " " + Loc.T(el), UiKit.Hex(ArcherLooks.Color(_view)), UiKit.Hex(0x1D1840), 11f, 8f);
            chip.font = FontLibrary.Get(FontRole.Body);

            RectTransform lvRow = UiKit.Rect(card.transform, "Level");
            UiKit.Size(lvRow, -1, 14f);
            UiKit.Row(lvRow, 3f, TextAnchor.MiddleLeft);
            TextMeshProUGUI lvText = UiKit.Label(lvRow, Loc.F("archers_level", level, max), FontRole.Body, 11f, p.InkMuted);
            UiKit.Size(lvText, lvText.preferredWidth + 6f, 14f);
            for (int i = 0; i < max; i++)
            {
                Image pip = UiKit.Box(lvRow, "Pip", i < level ? Widgets.TierGold : p.Track, 3f);
                UiKit.Size(pip, 9f, 9f);
            }

            int hp = def.BaseHp + (level - 1) * dmg.HpPerLevel;
            double damage = def.BaseDamage * (1.0 + (level - 1) * dmg.DamagePerLevel);
            StatBar(card.transform, Loc.T("stat_hp"), hp.ToString(), hp / 150f, Widgets.Green, p);
            StatBar(card.transform, Loc.T("stat_damage"), damage.ToString("0"), (float)(damage / 40.0), Widgets.Red, p);

            TextMeshProUGUI passive = UiKit.Paragraph(card.transform, "<color=" + Fmt.Hex(p.Ink) + ">" + Loc.T("archers_passive") + "</color>" + Loc.T("passive_" + _view),
                FontRole.BodyBold, 12f, p.InkMuted, TextAlignmentOptions.TopLeft);
            UiKit.Size(passive, -1, 32f);

            // Ability card with a live demo on the stage.
            Image ab = UiKit.Box(card.transform, "Ability", p.Solid, 14f);
            UiKit.Size(ab, -1, 56f);
            UiKit.Row(ab, 10f, TextAnchor.MiddleLeft, false, false, new RectOffset(10, 10, 9, 9));
            Image tile = UiKit.Box(ab.transform, "Tile", UiKit.Hex(ArcherLooks.Color(_view)), 12f);
            UiKit.Size(tile, 38f, 38f);
            TextMeshProUGUI ti = UiKit.Glyph(tile.transform, ArcherLooks.ElementIcon(def.Element), 20f, UiKit.Hex(0x1D1840));
            UiKit.Stretch((RectTransform)ti.transform);
            RectTransform words = UiKit.Rect(ab.transform, "Words");
            UiKit.Size(words, -1, 38f, 1f);
            UiKit.Column(words, 0f, TextAnchor.MiddleLeft, true, false);
            TextMeshProUGUI an = UiKit.Label(words, Loc.T("ability_" + def.Ability), FontRole.Display, 15f, p.Ink);
            UiKit.Size(an, -1, 18f);
            TextMeshProUGUI ad = UiKit.Paragraph(words, Loc.T("ability_" + def.Ability + "_desc"), FontRole.BodyBold, 11.5f, p.InkMuted, TextAlignmentOptions.TopLeft);
            UiKit.Size(ad, -1, 22f);
            Button3D demo = Button3D.Create(ab.transform, "Demo", p.Track, Color.clear, 10f, 0f, 2f);
            demo.AddLabel(Loc.T("archers_demo"), FontRole.Body, 12f, p.Ink);
            UiKit.Size(demo, 76f, 32f);
            demo.OnClick(() => _stage?.Demo());

            UiKit.Spacer(card.transform);
            BuildButtons(card.transform, profile, def, own, level, max, p);
        }

        static void StatBar(Transform parent, string label, string value, float fill, Color c, Palette p)
        {
            RectTransform row = UiKit.Rect(parent, label);
            UiKit.Size(row, -1, 16f);
            UiKit.Row(row, 8f, TextAnchor.MiddleLeft);
            TextMeshProUGUI l = UiKit.Label(row, label, FontRole.Body, 12f, p.Ink);
            UiKit.Size(l, 74f, 16f);
            ProgressBar bar = ProgressBar.Create(row, p.Track, c, 10f, 5f);
            UiKit.Size(bar, -1, 10f, 1f);
            bar.SetValue(Mathf.Clamp01(fill), false);
            TextMeshProUGUI v = UiKit.Label(row, value, FontRole.Display, 13f, p.Ink, TextAlignmentOptions.MidlineRight);
            UiKit.Size(v, 32f, 16f);
        }

        void BuildButtons(Transform card, Profile profile, ArcherDef def, bool own, int level, int max, Palette p)
        {
            RectTransform row = UiKit.Rect(card, "Buttons");
            UiKit.Size(row, -1, 50f);
            UiKit.Row(row, 10f, TextAnchor.MiddleLeft, true, false);
            string lockText = null;
            if (!own)
            {
                ArcherUnlock u = def.Unlock;
                if (u.Kind == UnlockKind.Stars) lockText = Loc.F("archers_locked_stars", u.Value, profile.TotalStars);
                else if (u.Value >= Logic.Campaign.WorldOne.LevelCount) lockText = Loc.T("archers_locked_boss");
                else lockText = Loc.F("archers_locked_level", u.Value);
                Button3D locked = Button3D.Off(row, "Locked", Icons.Lock + " " + lockText, 14f, 15f);
                UiKit.Size(locked, -1, 46f, 1f);
                locked.OnClick(() => Ui.Toast(lockText));
                return;
            }
            bool selected = profile.Data.EquippedArcher == _view;
            Button3D primary = selected
                ? Button3D.Off(row, "Selected", Loc.T("archers_selected"), 16f, 15f)
                : Button3D.Styled(row, "Select", ButtonStyle.Primary, Loc.T("archers_select"), 16f, 15f);
            UiKit.Size(primary, -1, 46f, 1f);
            string name = def.Name;
            primary.OnClick(() =>
            {
                if (selected)
                {
                    Ui.Toast(Loc.F("archers_selected_toast", name));
                    return;
                }
                profile.EquipArcher(_view);
                ServiceLocator.CommitProfile();
                Ui.Toast(Loc.F("archers_selected_toast", name));
                Ui.Refresh();
            });
            if (level < max)
            {
                int cost = profile.UpgradeCost(_view);
                Button3D up = Button3D.Styled(row, "Upgrade", ButtonStyle.Gold, Loc.F("archers_upgrade", Fmt.Coins(cost)), 17f, 15f);
                UiKit.Size(up, -1, 46f, 1f);
                string id = _view;
                up.OnClick(() => Ui.ShowModal(new UpgradeModal(id, () => Ui.Refresh())));
            }
            else
            {
                Button3D maxed = Button3D.Off(row, "Max", Loc.T("archers_max"), 16f, 15f);
                UiKit.Size(maxed, -1, 46f, 1f);
            }
        }

        public override void OnShow() => ServiceLocator.Audio?.PlayMusic(Feel.SoundId.MusicHome);
    }
}
