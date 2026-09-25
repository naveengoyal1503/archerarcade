using System;
using ArcherArcade.Core;
using ArcherArcade.Logic.Save;
using ArcherArcade.Logic.Text;
using ArcherArcade.Settings;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Settings (design `settings`, state 13): Sound (music, effects, haptics, aim sensitivity), Display (theme,
    /// battery saver, colour-blind, reduce motion, language) and Controls &amp; access (name, left-handed, bigger
    /// targets, trajectory assist, How to play, Credits, Privacy). Footer: "In loving memory of Maa ❤".
    /// </summary>
    public sealed class SettingsScreen : UiScreen
    {
        public override string Title => Loc.T("title_settings");

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            SettingsManager sm = ServiceLocator.Settings;
            SettingsData s = sm.Data;
            RectTransform col = UiKit.Rect(root, "Col");
            UiKit.Stretch(col, 18f, 62f, 18f, 12f);
            UiKit.Column(col, 8f, TextAnchor.UpperLeft, true, false);
            RectTransform cards = UiKit.Rect(col, "Cards");
            UiKit.Size(cards, -1, -1, 1f, 1f);
            UiKit.Row(cards, 12f, TextAnchor.UpperLeft, true, true);

            // Sound.
            RectTransform sound = Card(cards, Loc.T("set_sound"), p);
            TextMeshProUGUI music = SliderRow(sound, Loc.F("set_music", s.Music), s.Music / 100f, p, v =>
            {
                sm.Data.Music = Mathf.RoundToInt(v * 100f);
                ServiceLocator.Audio.SetVolumes(sm.Data.Music, sm.Data.Sfx);
            }, v => sm.SetMusic(Mathf.RoundToInt(v * 100f)));
            music.gameObject.AddComponent<LiveLabel>().Bind(() => Loc.F("set_music", sm.Data.Music));
            TextMeshProUGUI sfx = SliderRow(sound, Loc.F("set_sfx", s.Sfx), s.Sfx / 100f, p, v =>
            {
                sm.Data.Sfx = Mathf.RoundToInt(v * 100f);
                ServiceLocator.Audio.SetVolumes(sm.Data.Music, sm.Data.Sfx);
            }, v =>
            {
                sm.SetSfx(Mathf.RoundToInt(v * 100f));
                ServiceLocator.Audio.Play(Feel.SoundId.UiTap);
            });
            sfx.gameObject.AddComponent<LiveLabel>().Bind(() => Loc.F("set_sfx", sm.Data.Sfx));
            Toggle(sound, Loc.T("set_haptics"), Loc.T("set_haptics_desc"), s.Haptics, p, on =>
            {
                sm.SetHaptics(on);
                if (on) ServiceLocator.Haptics.Play(Feel.HapticId.Hit);
            });
            int sens = Mathf.RoundToInt((float)(s.AimSensitivity - 0.5) * 100f);
            TextMeshProUGUI sl = SliderRow(sound, Loc.F("set_sensitivity", sens), sens / 100f, p, v => sm.Data.AimSensitivity = 0.5 + v, v => sm.SetSensitivity(0.5 + v));
            sl.gameObject.AddComponent<LiveLabel>().Bind(() => Loc.F("set_sensitivity", Mathf.RoundToInt((float)(sm.Data.AimSensitivity - 0.5) * 100f)));

            // Display.
            RectTransform display = Card(cards, Loc.T("set_display"), p);
            Segmented(display, new[] { Icons.Contrast + " " + Loc.T("set_theme_auto"), Icons.LightMode + " " + Loc.T("set_theme_light"), Icons.DarkMode + " " + Loc.T("set_theme_dark") },
                (int)ThemeManager.Parse(s.Theme), p, i => sm.SetTheme((ThemeMode)i));
            Toggle(display, Loc.T("set_battery"), Loc.T("set_battery_desc"), s.BatterySaver, p, sm.SetBatterySaver);
            Toggle(display, Loc.T("set_colorblind"), Loc.T("set_colorblind_desc"), s.ColorBlind, p, sm.SetColorBlind);
            Toggle(display, Loc.T("set_reduce_motion"), Loc.T("set_reduce_motion_desc"), s.ReduceMotion, p, sm.SetReduceMotion);
            TextMeshProUGUI lang = UiKit.Label(display, Icons.Translate + " " + Loc.T("set_language"), FontRole.Body, 12.5f, p.Ink);
            UiKit.Size(lang, -1, 16f);
            Segmented(display, new[] { Loc.T("set_lang_en"), Loc.T("set_lang_hi") }, (int)Loc.Current, p, i => sm.SetLanguage((Lang)i));

            // Controls & access.
            RectTransform controls = Card(cards, Loc.T("set_controls"), p);
            NameField(controls, p);
            Toggle(controls, Loc.T("set_left"), Loc.T("set_left_desc"), s.LeftHanded, p, sm.SetLeftHanded);
            Toggle(controls, Loc.T("set_big"), Loc.T("set_big_desc"), s.BiggerTargets, p, sm.SetBiggerTargets);
            Toggle(controls, Loc.T("set_assist"), Loc.T("set_assist_desc"), s.TrajectoryAssist, p, sm.SetTrajectoryAssist);
            Button3D how = Button3D.Styled(controls, "HowTo", ButtonStyle.Primary, Loc.T("set_how_to"), 15f, 13f);
            UiKit.Size(how, -1, 36f);
            how.OnClick(() => Ui.Push(new HowToPlayScreen()));
            RectTransform pair = UiKit.Rect(controls, "Pair");
            UiKit.Size(pair, -1, 40f);
            UiKit.Row(pair, 8f, TextAnchor.UpperLeft, true, false, new RectOffset(0, 0, 4, 0));
            Button3D credits = Button3D.Styled(pair, "Credits", ButtonStyle.Primary, Loc.T("set_credits"), 15f, 13f);
            UiKit.Size(credits, -1, 36f);
            credits.OnClick(() => Ui.Push(new CreditsScreen()));
            Button3D privacy = Button3D.Create(pair, "Privacy", p.Track, Color.clear, 13f, 0f, 2f);
            privacy.AddLabel(Loc.T("set_privacy"), FontRole.Display, 15f, p.Ink);
            UiKit.Size(privacy, -1, 36f);
            privacy.OnClick(() => Ui.Push(new PrivacyScreen()));

            TextMeshProUGUI tribute = UiKit.Label(col, Loc.T("tribute"), FontRole.Body, 13f, p.Ink, TextAlignmentOptions.Center);
            UiKit.Size(tribute, -1, 18f);
            tribute.text = Loc.T("tribute").Replace("❤", "<color=#E5484D>❤</color>");
        }

        static RectTransform Card(RectTransform parent, string title, Palette p)
        {
            Image card = UiKit.Card(parent, title, 20f);
            RectTransform view = UiKit.Rect(card.transform, "View");
            UiKit.Stretch(view);
            view.gameObject.AddComponent<RectMask2D>();
            UiKit.HitArea(view);
            RectTransform content = UiKit.Rect(view, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            UiKit.Column(content, 6f, TextAnchor.UpperLeft, true, false, new RectOffset(14, 14, 12, 12));
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            TextMeshProUGUI t = UiKit.Label(content, title, FontRole.Display, 16f, p.Ink);
            UiKit.Size(t, -1, 20f);
            return content;
        }

        static TextMeshProUGUI SliderRow(RectTransform parent, string label, float value, Palette p, Action<float> change, Action<float> commit)
        {
            RectTransform box = UiKit.Rect(parent, "Slider");
            UiKit.Size(box, -1, 42f);
            UiKit.Column(box, 2f, TextAnchor.UpperLeft, true, false);
            TextMeshProUGUI l = UiKit.Label(box, label, FontRole.Body, 12.5f, p.Ink);
            UiKit.Size(l, -1, 16f);
            SliderBar.Create(box, value, change, commit);
            return l;
        }

        static void Toggle(RectTransform parent, string label, string desc, bool on, Palette p, Action<bool> changed)
        {
            RectTransform row = UiKit.Rect(parent, label);
            UiKit.Size(row, -1, 38f);
            UiKit.Row(row, 10f, TextAnchor.MiddleLeft);
            RectTransform words = UiKit.Rect(row, "Words");
            UiKit.Size(words, -1, 36f, 1f);
            UiKit.Column(words, 0f, TextAnchor.MiddleLeft, true, false);
            TextMeshProUGUI l = UiKit.Label(words, label, FontRole.Body, 13f, p.Ink);
            UiKit.Size(l, -1, 17f);
            UiKit.Fit(l);
            TextMeshProUGUI d = UiKit.Label(words, desc, FontRole.BodyBold, 10.5f, p.InkMuted);
            UiKit.Size(d, -1, 14f);
            UiKit.Fit(d, 0.6f);
            SwitchToggle.Create(row, on, changed);
        }

        void Segmented(RectTransform parent, string[] options, int selected, Palette p, Action<int> pick)
        {
            RectTransform seg = UiKit.Rect(parent, "Segmented");
            UiKit.Size(seg, -1, 38f);
            Image track = UiKit.Box(seg, "Track", p.Track, 14f);
            UiKit.Stretch(track.rectTransform);
            track.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            UiKit.Row(seg, 6f, TextAnchor.MiddleLeft, true, true, new RectOffset(4, 4, 4, 4));
            for (int i = 0; i < options.Length; i++)
            {
                bool on = i == selected;
                Button3D b = Button3D.Create(seg, "Opt" + i, on ? p.Solid : Color.clear, Color.clear, 10f, 0f, 1f);
                b.AddLabel(options[i], FontRole.Display, 13.5f, p.Ink);
                int index = i;
                b.OnClick(() =>
                {
                    if (index == selected) return;
                    pick(index);
                    Ui.Refresh();
                });
            }
        }

        static void NameField(RectTransform parent, Palette p)
        {
            TextMeshProUGUI l = UiKit.Label(parent, Loc.T("set_name"), FontRole.Body, 12.5f, p.Ink);
            UiKit.Size(l, -1, 16f);
            Image bg = UiKit.Box(parent, "Name", p.Solid, 10f);
            bg.raycastTarget = true;
            UiKit.Size(bg, -1, 34f);
            UiKit.Border(bg.transform, p.Line, 10f, 1.5f);
            RectTransform area = UiKit.Rect(bg.transform, "Text Area");
            UiKit.Stretch(area, 10, 2, 10, 2);
            area.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI ph = UiKit.Label(area, Loc.T("player_default"), FontRole.Body, 14f, p.InkMuted);
            UiKit.Stretch((RectTransform)ph.transform);
            TextMeshProUGUI text = UiKit.Label(area, "", FontRole.Display, 15f, p.Ink);
            UiKit.Stretch((RectTransform)text.transform);
            var field = bg.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = ph;
            field.fontAsset = FontLibrary.Get(FontRole.Display);
            field.characterLimit = 14;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.caretColor = p.Ink;
            field.text = ServiceLocator.Profile.Data.PlayerName;
            field.onEndEdit.AddListener(v =>
            {
                ServiceLocator.Profile.Data.PlayerName = string.IsNullOrWhiteSpace(v) ? "Player" : v.Trim();
                ServiceLocator.CommitProfile();
            });
        }
    }
}
