using System.Collections.Generic;
using ArcherArcade.Logic.Meta;

namespace ArcherArcade.Logic.Save
{
    /// <summary>
    /// Everything saved (GAME_DESIGN §14). Versioned; fields are only ever added, with defaults, so old saves always
    /// load (see <see cref="SaveCodec"/>).
    /// </summary>
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string PlayerName = "Player";

        public int Coins;
        public long CoinsEarned;

        /// <summary>Owned archers and their upgrade level (1–10). The Ranger is always owned.</summary>
        public Dictionary<string, int> ArcherLevels = new Dictionary<string, int> { { "ranger", 1 } };

        public string EquippedArcher = "ranger";
        public Dictionary<string, string> EquippedSkins = new Dictionary<string, string>();
        public string EquippedTrail = "trail_classic";
        public List<string> OwnedCosmetics = new List<string>();

        /// <summary>Special tips picked in the Loadout last time (Normal is implied).</summary>
        public List<ArrowTip> LoadoutTips = new List<ArrowTip>();

        public Dictionary<string, LevelSave> Levels = new Dictionary<string, LevelSave>();

        public Dictionary<string, BadgeTier> BadgeTiers = new Dictionary<string, BadgeTier>();
        public List<string> PinnedBadges = new List<string>();

        public Dictionary<StatKey, long> Stats = new Dictionary<StatKey, long>();
        public double LongestShot;
        public int[] WinsByMode = new int[6];
        public int[] LossesByMode = new int[6];
        public Dictionary<string, int> ArcherMatches = new Dictionary<string, int>();
        public int WardenBestStars;

        public DailySave Daily = new DailySave();
        public List<int> ChestsOpened = new List<int>();

        public SettingsData Settings = new SettingsData();

        public bool TutorialDone;
        public List<string> SeenCards = new List<string>();

        public string PvpName1 = "P1";
        public string PvpName2 = "P2";

        /// <summary>Survival mode: best wave reached (added 2026-09-25, default 0).</summary>
        public int SurvivalBestWave;

        public long Stat(StatKey key) => Stats.TryGetValue(key, out long v) ? v : 0L;

        public void AddStat(StatKey key, long amount)
        {
            if (amount == 0) return;
            Stats[key] = Stat(key) + amount;
        }

        public LevelSave Level(string id)
        {
            if (!Levels.TryGetValue(id, out LevelSave l))
            {
                l = new LevelSave();
                Levels[id] = l;
            }
            return l;
        }
    }
}
