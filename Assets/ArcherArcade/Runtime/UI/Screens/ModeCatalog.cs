using ArcherArcade.Core;

namespace ArcherArcade.UI
{
    /// <summary>The v1.0 modes with the design's colours (Campaign green, Quick Duel blue, 2 Players pink, Daily
    /// orange, Training brown, Survival purple).</summary>
    public static class ModeCatalog
    {
        public static ModeCard Campaign(UIManager ui) => new ModeCard
        {
            Name = Loc.T("mode_campaign"), Sub = Loc.T("mode_campaign_sub"), Icon = Icons.Map, Color = 0x12A67A, Shade = 0x0B7555,
            Open = () => ui.Push(new MapScreen())
        };

        public static ModeCard QuickDuel(UIManager ui) => new ModeCard
        {
            Name = Loc.T("mode_quick"), Sub = Loc.T("mode_quick_sub"), Icon = Icons.Swords, Color = 0x1C9AD6, Shade = 0x13709E,
            Open = () => ui.Push(LoadoutScreen.QuickDuel())
        };

        public static ModeCard TwoPlayer(UIManager ui) => new ModeCard
        {
            Name = Loc.T("mode_pvp"), Sub = Loc.T("mode_pvp_sub"), Icon = Icons.Group, Color = 0xE83E8C, Shade = 0xB02467,
            Open = () => ui.Push(new PvpSetupScreen())
        };

        public static ModeCard Daily(UIManager ui) => new ModeCard
        {
            Name = Loc.T("mode_daily"), Sub = Loc.T("mode_daily_sub"), Icon = Icons.CalendarMonth, Color = 0xF0641E, Shade = 0xB8460F,
            Open = () => ui.Push(new DailyScreen())
        };

        public static ModeCard Training(UIManager ui) => new ModeCard
        {
            Name = Loc.T("mode_training"), Sub = Loc.T("mode_training_sub"), Icon = Icons.Target, Color = 0x8E7A5B, Shade = 0x65563F,
            Open = () => ui.Push(LoadoutScreen.Training())
        };

        public static ModeCard Survival(UIManager ui) => new ModeCard
        {
            Name = Loc.T("mode_survival"),
            Sub = Loc.F("mode_survival_sub", ServiceLocator.Profile != null ? ServiceLocator.Profile.Data.SurvivalBestWave : 0),
            Icon = Icons.Waves, Color = 0x6D4AFF, Shade = 0x4A2BD1,
            Open = () => ui.Push(LoadoutScreen.Survival())
        };

        public static ModeCard[] All(UIManager ui) =>
            new[] { Campaign(ui), QuickDuel(ui), TwoPlayer(ui), Survival(ui), Daily(ui), Training(ui) };
    }
}
