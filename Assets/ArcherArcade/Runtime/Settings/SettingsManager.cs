using ArcherArcade.Core;
using ArcherArcade.Logic.Save;
using ArcherArcade.Logic.Text;
using ArcherArcade.Theme;
using UnityEngine;

namespace ArcherArcade.Settings
{
    /// <summary>
    /// Reads and changes the Settings screen values (SettingsData in the save) and applies them to the services:
    /// volumes, haptics, theme, language, frame rate. Other flags (left-handed, bigger targets, sensitivity,
    /// assist, reduce motion, colour-blind) are read directly by the HUD and the match.
    /// </summary>
    public sealed class SettingsManager
    {
        public SettingsData Data => ServiceLocator.Profile.Data.Settings;

        public void ApplyAll()
        {
            SettingsData s = Data;
            ServiceLocator.Audio.SetVolumes(s.Music, s.Sfx);
            ServiceLocator.Haptics.Enabled = s.Haptics;
            ServiceLocator.Theme.SetMode(ThemeManager.Parse(s.Theme));
            Loc.Set(LangCodes.Parse(s.Language));
            ServiceLocator.Display.SetBatterySaver(s.BatterySaver);
        }

        void Changed()
        {
            ServiceLocator.Save.MarkDirty();
            GameEvents.RaiseSettingsChanged();
        }

        public void SetMusic(int v)
        {
            Data.Music = Mathf.Clamp(v, 0, 100);
            ServiceLocator.Audio.SetVolumes(Data.Music, Data.Sfx);
            Changed();
        }

        public void SetSfx(int v)
        {
            Data.Sfx = Mathf.Clamp(v, 0, 100);
            ServiceLocator.Audio.SetVolumes(Data.Music, Data.Sfx);
            Changed();
        }

        public void SetHaptics(bool on)
        {
            Data.Haptics = on;
            ServiceLocator.Haptics.Enabled = on;
            Changed();
        }

        public void SetTheme(ThemeMode mode)
        {
            Data.Theme = ThemeManager.Code(mode);
            ServiceLocator.Theme.SetMode(mode);
            Changed();
        }

        public void SetLanguage(Lang lang)
        {
            Data.Language = LangCodes.Code(lang);
            Loc.Set(lang);
            Changed();
        }

        public void SetBatterySaver(bool on)
        {
            Data.BatterySaver = on;
            ServiceLocator.Display.SetBatterySaver(on);
            Changed();
        }

        public void SetLeftHanded(bool on) { Data.LeftHanded = on; Changed(); }
        public void SetBiggerTargets(bool on) { Data.BiggerTargets = on; Changed(); }
        public void SetTrajectoryAssist(bool on) { Data.TrajectoryAssist = on; Changed(); }
        public void SetReduceMotion(bool on) { Data.ReduceMotion = on; Changed(); }
        public void SetColorBlind(bool on) { Data.ColorBlind = on; Changed(); }

        /// <summary>Aim sensitivity 0.5–1.5 (slider 0–100 in the UI).</summary>
        public void SetSensitivity(double v)
        {
            Data.AimSensitivity = v < 0.5 ? 0.5 : v > 1.5 ? 1.5 : v;
            Changed();
        }

        public bool ReduceMotion => Data.ReduceMotion;
        public bool LeftHanded => Data.LeftHanded;
        public bool BiggerTargets => Data.BiggerTargets;
        public bool ColorBlind => Data.ColorBlind;
    }
}
