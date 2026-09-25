using ArcherArcade.Feel;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Save;
using ArcherArcade.Settings;
using ArcherArcade.Theme;
using ArcherArcade.UI;
using UnityEngine;

namespace ArcherArcade.Core
{
    /// <summary>
    /// The app's root object (created by the Boot scene, or on demand when a scene is played directly in the
    /// editor): loads the save and sets up every service, tracks play time, and saves when the app goes to the
    /// background or quits (CLAUDE.md save rules).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameManager : MonoBehaviour
    {
        const float PlayTimeFlushSeconds = 60f;

        public static GameManager Instance { get; private set; }

        float _playSeconds;

        public static void Ensure()
        {
            if (Instance) return;
            var go = new GameObject("[Game]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<GameManager>();
            Instance.Boot();
        }

        void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Boot();
            }
        }

        void Boot()
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;

            GameConfig config = GameConfig.Load();
            ServiceLocator.Config = config;
            Logic.Campaign.LevelBuilder.Configure = config.Apply;
            var save = new SaveSystem(Application.persistentDataPath);
            ServiceLocator.Save = save;
            var profile = new Profile(save.Load(), config.Economy);
            ServiceLocator.Profile = profile;
            if (profile.GrantUnlockedArchers().Count > 0) save.MarkDirty();

            ServiceLocator.Theme = new ThemeManager();
            ServiceLocator.Display = new DisplayRate();
            ServiceLocator.Haptics = new HapticsManager();
            ServiceLocator.Audio = gameObject.AddComponent<AudioManager>();
            ServiceLocator.Settings = new SettingsManager();
            ServiceLocator.Settings.ApplyAll();
            FontLibrary.Warm();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _playSeconds += dt;
            if (_playSeconds >= PlayTimeFlushSeconds) FlushPlayTime();
            ServiceLocator.Display?.Tick(Time.unscaledTime);
        }

        void LateUpdate()
        {
            ServiceLocator.Save?.FlushIfDirty();
        }

        void FlushPlayTime()
        {
            int whole = (int)_playSeconds;
            if (whole <= 0 || ServiceLocator.Profile == null) return;
            _playSeconds -= whole;
            ServiceLocator.Profile.Data.AddStat(StatKey.PlaySeconds, whole);
            ServiceLocator.Save.MarkDirty();
        }

        void SaveNow()
        {
            FlushPlayTime();
            ServiceLocator.Save?.SaveNow();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
            else ServiceLocator.Theme?.Refresh();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus) SaveNow();
        }

        void OnApplicationQuit() => SaveNow();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;
    }
}
