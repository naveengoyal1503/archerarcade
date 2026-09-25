using ArcherArcade.Logic.Meta;
using ArcherArcade.UI;
using UnityEngine;

namespace ArcherArcade.Core
{
    /// <summary>
    /// Home scene entry (placed by the scene builder): makes sure the game is booted, builds the menu UI and opens
    /// the right screen — the splash on app start (then the Level 1 tutorial on first launch, or Home), or the
    /// screen a match returned to (map after a campaign level, the setup screen after other modes).
    /// </summary>
    public sealed class HomeSceneRoot : MonoBehaviour
    {
        static bool _splashShown;
        UIManager _ui;

        void Start()
        {
            GameManager.Ensure();
            EnsureCamera();
            _ui = UIManager.Create("HomeUI");
            HomeTarget target = SceneFlow.TakeHomeTarget();
            if (!_splashShown)
            {
                _splashShown = true;
                _ui.ResetTo(new SplashScreen(AfterSplash));
                return;
            }
            Open(_ui, target);
        }

        void AfterSplash()
        {
            Profile p = ServiceLocator.Profile;
            if (!p.Data.TutorialDone && !p.Level(1).Cleared)
            {
                SceneFlow.StartMatch(new MatchRequest { Mode = GameMode.Campaign, Level = 1, Seed = (ulong)System.DateTime.UtcNow.Ticks });
                return;
            }
            _ui.ResetTo(new HomeScreen());
        }

        public static void Open(UIManager ui, HomeTarget target)
        {
            ui.ResetTo(new HomeScreen());
            switch (target)
            {
                case HomeTarget.Map: ui.Push(new MapScreen()); break;
                case HomeTarget.Modes: ui.Push(new ModesScreen()); break;
                case HomeTarget.Archers: ui.Push(new ArchersScreen(ServiceLocator.Profile.Data.EquippedArcher)); break;
                case HomeTarget.QuickDuelSetup: ui.Push(LoadoutScreen.QuickDuel()); break;
                case HomeTarget.PvpSetup: ui.Push(new PvpSetupScreen()); break;
                case HomeTarget.TrainingSetup: ui.Push(LoadoutScreen.Training()); break;
                case HomeTarget.Daily: ui.Push(new DailyScreen()); break;
                case HomeTarget.Chests: ui.Push(new ChestsScreen()); break;
                case HomeTarget.Survival: ui.Push(LoadoutScreen.Survival()); break;
            }
        }

        static void EnsureCamera()
        {
            if (Camera.main) return;
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x15, 0x12, 0x2E, 0xFF);
            cam.cullingMask = 0;
            cam.orthographic = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _splashShown = false;
    }
}
