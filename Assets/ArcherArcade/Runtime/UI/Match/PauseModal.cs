using ArcherArcade.Core;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Match;
using ArcherArcade.Theme;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcherArcade.UI
{
    /// <summary>
    /// Pause (design `match` + `pause`, state 03; SCREEN_INVENTORY 15): Resume, Restart, Quit, a sound toggle and
    /// Settings. The match is frozen (timeScale 0) underneath. Back = Resume.
    /// </summary>
    public sealed class PauseModal : UiModal
    {
        static int _music = -1, _sfx = -1;

        readonly MatchSceneRoot _r;
        TextMeshProUGUI _sound;

        public PauseModal(MatchSceneRoot root) => _r = root;

        public override Color Dim => new Color32(20, 16, 50, 140);
        public override bool TapOutsideCloses => false;

        public override void Build(RectTransform root)
        {
            Palette p = UiKit.P;
            Image panel = UiKit.Box(root, "Panel", p.Solid, 26f);
            UiKit.At(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 0f));
            panel.raycastTarget = true;
            UiKit.SoftShadow(panel.transform, new Color(0f, 0f, 0f, 0.25f), 26f, 30f, 10f);
            UiKit.Column(panel, 10f, TextAnchor.UpperCenter, true, false, new RectOffset(20, 20, 18, 20));
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI title = UiKit.Label(panel.transform, Loc.T("pause_title"), FontRole.Display, 28f, p.Ink, TextAlignmentOptions.Center);
            UiKit.Size(title, -1, 36f);

            Button3D resume = Button3D.Styled(panel.transform, "Resume", ButtonStyle.Success, Loc.T("pause_resume"), 20f, 16f);
            UiKit.Size(resume, -1, 50f);
            resume.OnClick(Resume);

            RectTransform row = UiKit.Rect(panel.transform, "Row");
            UiKit.Size(row, -1, 48f);
            UiKit.Row(row, 10f, TextAnchor.UpperCenter, true, false, new RectOffset(0, 0, 0, 4));
            Button3D restart = Button3D.Styled(row, "Restart", ButtonStyle.Info, Loc.T("pause_restart"), 16f);
            UiKit.Size(restart, -1, 44f, 1f);
            restart.OnClick(() => _r.RestartMatch());
            restart.gameObject.SetActive(_r.Session.Mode != GameMode.Training);
            Button3D quit = Button3D.Styled(row, "Quit", ButtonStyle.Danger, Loc.T("pause_quit"), 16f);
            UiKit.Size(quit, -1, 44f, 1f);
            quit.OnClick(() => _r.QuitMatch());

            RectTransform row2 = UiKit.Rect(panel.transform, "Row2");
            UiKit.Size(row2, -1, 36f);
            UiKit.Row(row2, 10f, TextAnchor.MiddleCenter, true, true);
            Button3D sound = Button3D.Create(row2, "Sound", p.Track, Color.clear, 12f, 0f, 2f);
            _sound = sound.AddLabel(SoundLabel(), FontRole.BodyBold, 13f, p.Ink);
            sound.OnClick(ToggleSound);
            Button3D settings = Button3D.Create(row2, "Settings", p.Track, Color.clear, 12f, 0f, 2f);
            settings.AddLabel(Icons.Settings + " " + Loc.T("pause_settings"), FontRole.BodyBold, 13f, p.Ink);
            settings.OnClick(() => _r.OpenSettings());
            UIManager.PopPanel(panel.transform);
        }

        static bool SoundOn => ServiceLocator.Settings == null || ServiceLocator.Settings.Data.Sfx > 0 || ServiceLocator.Settings.Data.Music > 0;

        static string SoundLabel() => (SoundOn ? Icons.VolumeUp + " " + Loc.T("pause_sound_on") : Icons.VolumeOff + " " + Loc.T("pause_sound_off"));

        void ToggleSound()
        {
            var sm = ServiceLocator.Settings;
            if (sm == null) return;
            if (SoundOn)
            {
                _music = sm.Data.Music;
                _sfx = sm.Data.Sfx;
                sm.SetMusic(0);
                sm.SetSfx(0);
            }
            else
            {
                // Back to the volumes before muting (or the defaults).
                var defaults = new Logic.Save.SettingsData();
                sm.SetMusic(_music > 0 ? _music : defaults.Music);
                sm.SetSfx(_sfx > 0 ? _sfx : defaults.Sfx);
            }
            _sound.text = SoundLabel();
        }

        void Resume()
        {
            Close();
        }

        public override void OnClose() => _r.PauseClosed();

        public override bool OnBack()
        {
            Resume();
            return true;
        }
    }
}
