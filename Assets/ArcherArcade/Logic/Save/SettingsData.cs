namespace ArcherArcade.Logic.Save
{
    /// <summary>Settings screen values (GAME_DESIGN §13, §14). Defaults match the design prototype.</summary>
    public sealed class SettingsData
    {
        /// <summary>0–100.</summary>
        public int Music = 70;
        public int Sfx = 80;
        public bool Haptics = true;

        /// <summary>"system", "light" or "dark".</summary>
        public string Theme = "system";

        /// <summary>"en" or "hinglish".</summary>
        public string Language = "en";

        public bool LeftHanded;
        public bool BiggerTargets;

        /// <summary>Drag-to-aim sensitivity multiplier (0.5–1.5).</summary>
        public double AimSensitivity = 1.0;

        /// <summary>Trajectory assist (longer preview) where allowed.</summary>
        public bool TrajectoryAssist;

        public bool ReduceMotion;
        public bool BatterySaver;
        public bool ColorBlind;
    }
}
