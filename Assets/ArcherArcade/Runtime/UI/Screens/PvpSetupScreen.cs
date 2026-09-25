using System.Collections.Generic;
using ArcherArcade.Archers;
using ArcherArcade.Core;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Logic.Modes;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// 2-Player setup (SCREEN_INVENTORY #21): names, archer + skin per player, HP handicap, arena (incl. Mirror
    /// Forest), best of 1/3/5, wind on/off and the aim preview length. Pass-and-play on one phone.
    /// </summary>
    public sealed class PvpSetupScreen : UiScreen
    {
        static readonly string[] Arenas = { "mirror", "meadow", "fence", "crates", "platforms", "barrels", "random" };
        readonly PvpSettings _s = new PvpSettings();
        int _arena;

        public PvpSetupScreen()
        {
            Profile p = ServiceLocator.Profile;
            _s.Name1 = p.Data.PvpName1;
            _s.Name2 = p.Data.PvpName2;
            _s.Archer1 = p.Data.EquippedArcher;
            _s.Archer2 = p.OwnsArcher("fire") && _s.Archer1 != "fire" ? "fire" : "ranger";
            _s.Skin1 = Skin(p, _s.Archer1);
            _s.Skin2 = Skin(p, _s.Archer2);
        }

        public override string Title => Loc.T("title_pvp");

        static string Skin(Profile p, string archer) =>
            p.Data.EquippedSkins.TryGetValue(archer, out string s) ? s : CosmeticCatalog.DefaultSkinFor(archer);

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            RectTransform col = UiKit.Rect(root, "Col");
            UiKit.Stretch(col, 18f, 62f, 18f, 16f);
            UiKit.Column(col, 10f, TextAnchor.UpperLeft, true, false);
            RectTransform players = UiKit.Rect(col, "Players");
            UiKit.Size(players, -1, 196f);
            UiKit.Row(players, 14f, TextAnchor.UpperLeft, true, true);
            PlayerCard(players, 1, p);
            PlayerCard(players, 2, p);

            RectTransform bottom = UiKit.Rect(col, "Bottom");
            UiKit.Size(bottom, -1, 96f);
            UiKit.Row(bottom, 10f, TextAnchor.MiddleLeft, false, true);
            Settings(bottom, p);
            RectTransform startHolder = UiKit.Rect(bottom, "StartHolder");
            UiKit.Size(startHolder, 200f, -1);
            Button3D start = Button3D.Styled(startHolder, "Start", ButtonStyle.Gold, Loc.T("pvp_start"), 22f, 18f, 5f, 4f);
            UiKit.Stretch((RectTransform)start.transform, 0, 14, 0, 19);
            start.Sound = Feel.SoundId.UiConfirm;
            start.OnClick(Start);
        }

        void PlayerCard(RectTransform parent, int n, Palette p)
        {
            Profile profile = ServiceLocator.Profile;
            Color c = n == 1 ? UiKit.Hex(0xE83E8C) : UiKit.Hex(0x6D4AFF);
            Image card = UiKit.Card(parent, "P" + n, 22f);
            UiKit.Column(card, 7f, TextAnchor.UpperLeft, true, false, new RectOffset(14, 14, 12, 12));

            RectTransform head = UiKit.Rect(card.transform, "Head");
            UiKit.Size(head, -1, 36f);
            UiKit.Row(head, 8f, TextAnchor.MiddleLeft);
            TextMeshProUGUI chip = Widgets.Chip(head, Loc.F("pvp_player", n), c, Color.white, 13f, 8f);
            TMP_InputField field = NameField(head, n == 1 ? _s.Name1 : _s.Name2, p);
            UiKit.Size(field, -1, 34f, 1f);
            field.onEndEdit.AddListener(v =>
            {
                string name = string.IsNullOrWhiteSpace(v) ? "P" + n : v.Trim();
                if (n == 1) { _s.Name1 = name; profile.Data.PvpName1 = name; }
                else { _s.Name2 = name; profile.Data.PvpName2 = name; }
                ServiceLocator.Save.MarkDirty();
            });

            // Archers (owned heroes) as portrait buttons.
            RectTransform archers = UiKit.Rect(card.transform, "Archers");
            UiKit.Size(archers, -1, 60f);
            UiKit.Row(archers, 8f, TextAnchor.MiddleLeft);
            string current = n == 1 ? _s.Archer1 : _s.Archer2;
            foreach (string id in ArcherTable.HeroIds)
            {
                if (!profile.OwnsArcher(id)) continue;
                bool on = id == current;
                Button3D b = Button3D.Create(archers, id, UiKit.Hex(ArcherLooks.Color(id)), UiKit.Hex(ArcherLooks.Color(id), 0.6f), 28f, 3f, 2f);
                UiKit.Size(b, 56f, 56f);
                b.Face.sprite = ShapeSprites.Circle;
                b.Face.type = Image.Type.Simple;
                if (b.Edge) { b.Edge.sprite = ShapeSprites.Circle; b.Edge.type = Image.Type.Simple; }
                Image face = UiKit.Box(b.Body, "Face", Color.white, 0);
                face.sprite = ArtLibrary.Portrait(ArcherLooks.ForHero(id, Skin(profile, id)), on ? "happy" : "normal");
                face.preserveAspect = true;
                UiKit.Stretch(face.rectTransform, 2, 2, 2, 2);
                if (on)
                {
                    Image ring = UiKit.Box(b.Body, "Ring", Widgets.TierGold, 0);
                    ring.sprite = ShapeSprites.Outline(28f, 3.5f);
                    ring.type = Image.Type.Sliced;
                    UiKit.Stretch(ring.rectTransform, -3, -3, -3, -3);
                }
                string pick = id;
                b.OnClick(() =>
                {
                    if (n == 1) { _s.Archer1 = pick; _s.Skin1 = Skin(profile, pick); }
                    else { _s.Archer2 = pick; _s.Skin2 = Skin(profile, pick); }
                    Ui.Refresh();
                });
            }
            TextMeshProUGUI name = UiKit.Label(archers, ArcherTable.Hero(current).Name, FontRole.Display, 15f, p.Ink);
            UiKit.Size(name, -1, 30f, 1f);
            UiKit.Fit(name);

            // Skin + handicap.
            RectTransform row = UiKit.Rect(card.transform, "Row");
            UiKit.Size(row, -1, 34f);
            UiKit.Row(row, 6f, TextAnchor.MiddleLeft);
            TextMeshProUGUI sl = UiKit.Label(row, Loc.T("ld_skin"), FontRole.Body, 12f, p.InkMuted);
            UiKit.Size(sl, sl.preferredWidth + 2f, 20f);
            string skinNow = n == 1 ? _s.Skin1 : _s.Skin2;
            foreach (CosmeticDef skin in CosmeticCatalog.SkinsFor(current))
            {
                bool owned = skin.Source == CosmeticSource.Default || profile.Owns(skin.Id);
                if (!owned) continue;
                RigAsset rig = ArtLibrary.Rig(ArcherLooks.ForHero(current, skin.Id));
                Button3D d = Button3D.Create(row, skin.Id, rig != null && rig.Swatches.Length > 0 ? rig.Swatches[0] : Color.gray, Color.clear, 12f, 0f, 1f);
                UiKit.Size(d, 26f, 26f);
                d.Face.sprite = ShapeSprites.Circle;
                d.Face.type = Image.Type.Simple;
                if (skin.Id == skinNow)
                {
                    Image ring = UiKit.Box(d.Body, "Ring", Widgets.TierGold, 0);
                    ring.sprite = ShapeSprites.Outline(13f, 3f);
                    ring.type = Image.Type.Sliced;
                    UiKit.Stretch(ring.rectTransform, -2, -2, -2, -2);
                }
                string sid = skin.Id;
                d.OnClick(() =>
                {
                    if (n == 1) _s.Skin1 = sid; else _s.Skin2 = sid;
                    Ui.Refresh();
                });
            }
            UiKit.Spacer(row);
            double hc = n == 1 ? _s.Handicap1 : _s.Handicap2;
            Stepper(row, Loc.F("pvp_handicap", hc.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)), p, delta =>
            {
                double v = System.Math.Round(System.Math.Min(PvpSettings.MaxHandicap, System.Math.Max(PvpSettings.MinHandicap, hc + delta)), 1);
                if (n == 1) _s.Handicap1 = v; else _s.Handicap2 = v;
                Ui.Refresh();
            });
        }

        static TMP_InputField NameField(Transform parent, string value, Palette p)
        {
            Image bg = UiKit.Box(parent, "Name", p.Solid, 10f);
            bg.raycastTarget = true;
            UiKit.Border(bg.transform, p.Line, 10f, 1.5f);
            RectTransform area = UiKit.Rect(bg.transform, "Text Area");
            UiKit.Stretch(area, 10, 2, 10, 2);
            area.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI ph = UiKit.Label(area, Loc.T("pvp_name"), FontRole.Body, 14f, p.InkMuted);
            UiKit.Stretch((RectTransform)ph.transform);
            TextMeshProUGUI text = UiKit.Label(area, "", FontRole.Display, 16f, p.Ink);
            UiKit.Stretch((RectTransform)text.transform);
            var field = bg.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = ph;
            field.fontAsset = FontLibrary.Get(FontRole.Display);
            field.characterLimit = PvpSettings.MaxNameLength;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.contentType = TMP_InputField.ContentType.Standard;
            field.caretColor = p.Ink;
            field.selectionColor = new Color(0.43f, 0.29f, 1f, 0.35f);
            field.text = value;
            return field;
        }

        static void Stepper(Transform parent, string label, Palette p, System.Action<double> change)
        {
            Button3D minus = Button3D.Create(parent, "Minus", p.Track, Color.clear, 9f, 0f, 1f);
            UiKit.Size(minus, 28f, 28f);
            minus.AddIcon(Icons.Remove, 16f, p.Ink);
            minus.OnClick(() => change(-0.1));
            TextMeshProUGUI t = UiKit.Label(parent, label, FontRole.Display, 14f, p.Ink, TextAlignmentOptions.Center);
            UiKit.Size(t, 66f, 28f);
            Button3D plus = Button3D.Create(parent, "Plus", p.Track, Color.clear, 9f, 0f, 1f);
            UiKit.Size(plus, 28f, 28f);
            plus.AddIcon(Icons.Add, 16f, p.Ink);
            plus.OnClick(() => change(0.1));
        }

        void Settings(RectTransform parent, Palette p)
        {
            Image card = UiKit.Card(parent, "Settings", 20f);
            UiKit.Size(card, -1, -1, 1f);
            UiKit.Row(card, 12f, TextAnchor.MiddleLeft, false, false, new RectOffset(14, 14, 8, 8));

            // Arena.
            RectTransform a = Group(card.transform, Loc.T("pvp_arena"), 150f, p);
            Button3D arena = Button3D.Create(a, "Arena", p.Solid, p.Shadow, 10f, 3f, 2f);
            UiKit.Size(arena, -1, 34f);
            arena.AddLabel(Loc.T("arena_" + Arenas[_arena]) + "  " + Icons.Sync, FontRole.Display, 14f, p.Ink);
            arena.OnClick(() => { _arena = (_arena + 1) % Arenas.Length; Ui.Refresh(); });

            // Best of.
            RectTransform b = Group(card.transform, Loc.T("pvp_best_of"), 130f, p);
            RectTransform seg = UiKit.Rect(b, "Seg");
            UiKit.Size(seg, -1, 34f);
            Image track = UiKit.Box(seg, "Track", p.Track, 12f);
            UiKit.Stretch(track.rectTransform);
            UiKit.Row(seg, 4f, TextAnchor.MiddleLeft, true, true, new RectOffset(3, 3, 3, 3));
            track.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            foreach (int bo in new[] { 1, 3, 5 })
            {
                bool on = _s.BestOf == bo;
                Button3D x = Button3D.Create(seg, "Bo" + bo, on ? p.Solid : Color.clear, Color.clear, 9f, 0f, 1f);
                x.AddLabel(bo.ToString(), FontRole.Display, 15f, p.Ink);
                int pick = bo;
                x.OnClick(() => { _s.BestOf = pick; Ui.Refresh(); });
            }

            // Wind.
            RectTransform w = Group(card.transform, Loc.T("pvp_wind"), 60f, p);
            RectTransform wr = UiKit.Rect(w, "Switch");
            UiKit.Size(wr, -1, 34f);
            UiKit.Row(wr, 0f, TextAnchor.MiddleLeft);
            SwitchToggle.Create(wr, _s.WindOn, on => _s.WindOn = on);

            // Preview length.
            RectTransform pv = Group(card.transform, Loc.F("pvp_preview", Mathf.RoundToInt((float)_s.PreviewShare * 100f)), -1f, p);
            UiKit.Size(pv, -1, -1, 1f);
            TextMeshProUGUI label = pv.GetComponentInChildren<TextMeshProUGUI>();
            SliderBar.Create(pv, Mathf.InverseLerp(0.1f, 0.6f, (float)_s.PreviewShare), v =>
            {
                _s.PreviewShare = System.Math.Round(Mathf.Lerp(0.1f, 0.6f, v), 2);
                label.text = Loc.F("pvp_preview", Mathf.RoundToInt((float)_s.PreviewShare * 100f));
            }, null);
        }

        static RectTransform Group(Transform parent, string title, float width, Palette p)
        {
            RectTransform g = UiKit.Rect(parent, title);
            if (width > 0) UiKit.Size(g, width, 64f);
            else UiKit.Size(g, -1, 64f);
            UiKit.Column(g, 4f, TextAnchor.MiddleLeft, true, false);
            TextMeshProUGUI t = UiKit.Label(g, title, FontRole.Body, 12f, p.InkMuted);
            UiKit.Size(t, -1, 16f);
            return g;
        }

        void Start()
        {
            ServiceLocator.Save.MarkDirty();
            var series = new PvpSeries(new PvpSettings
            {
                Name1 = _s.Name1, Name2 = _s.Name2, Archer1 = _s.Archer1, Archer2 = _s.Archer2, Skin1 = _s.Skin1, Skin2 = _s.Skin2,
                Arena = Arenas[_arena], BestOf = _s.BestOf, WindOn = _s.WindOn, PreviewShare = _s.PreviewShare,
                Handicap1 = _s.Handicap1, Handicap2 = _s.Handicap2
            });
            SceneFlow.StartMatch(new MatchRequest
            {
                Mode = GameMode.TwoPlayer, Series = series, ArenaId = Arenas[_arena], Seed = (ulong)System.DateTime.UtcNow.Ticks
            });
        }
    }
}
