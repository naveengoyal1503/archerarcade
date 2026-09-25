using System;
using System.Collections;
using System.IO;
using ArcherArcade.Core;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Match;
using ArcherArcade.Theme;
using ArcherArcade.UI;
using UnityEditor;
using UnityEngine;

namespace ArcherArcade.EditorTools
{
    /// <summary>
    /// Design check (CLAUDE.md commands: Batch.Capture): walks every menu screen in light and dark theme and a few
    /// matches (Quick Duel on crates with the autopilot, a TNT level, the boss), saving PNGs to Builds/Capture/ to
    /// compare against the Claude Design prototype. Uses real time so pauses never stall it.
    /// </summary>
    public sealed class CaptureRunner : MonoBehaviour
    {
        const string Folder = "Builds/Capture";
        public bool QuitWhenDone;
        int _index;

        IEnumerator Start()
        {
            Directory.CreateDirectory(Folder);
            // Let Boot → Home and the splash finish.
            yield return WaitFor(() => FindAnyObjectByType<HomeSceneRoot>() != null || FindAnyObjectByType<MatchSceneRoot>() != null, 20f);
            yield return new WaitForSecondsRealtime(2.6f);
            if (FindAnyObjectByType<MatchSceneRoot>() != null)
            {
                yield return Shot("tutorial_level1");
                SceneFlow.GoHome();
                yield return WaitFor(() => FindAnyObjectByType<HomeSceneRoot>() != null, 20f);
                yield return new WaitForSecondsRealtime(1f);
            }

            foreach (ThemeMode mode in new[] { ThemeMode.Light, ThemeMode.Dark })
            {
                ServiceLocator.Theme?.SetMode(mode);
                string theme = mode == ThemeMode.Dark ? "dark" : "light";
                UIManager ui = FindAnyObjectByType<UIManager>();
                if (ui == null) break;
                Func<UiScreen>[] screens =
                {
                    () => null,
                    () => new ModesScreen(),
                    () => new MapScreen(),
                    () => new ArchersScreen(ServiceLocator.Profile.Data.EquippedArcher),
                    () => LoadoutScreen.Campaign(1),
                    () => LoadoutScreen.QuickDuel(),
                    () => LoadoutScreen.Survival(),
                    () => new PvpSetupScreen(),
                    () => new DailyScreen(),
                    () => new ChestsScreen(),
                    () => new ShopScreen(),
                    () => new BadgesScreen(),
                    () => new StatsScreen(),
                    () => new SettingsScreen(),
                    () => new HowToPlayScreen(),
                    () => new CreditsScreen(),
                    () => new PrivacyScreen()
                };
                foreach (Func<UiScreen> make in screens)
                {
                    ui.ResetTo(new HomeScreen());
                    UiScreen s = make();
                    if (s != null) ui.Push(s);
                    yield return new WaitForSecondsRealtime(0.7f);
                    yield return Shot((s != null ? s.GetType().Name : "HomeScreen") + "_" + theme);
                }
            }
            ServiceLocator.Theme?.SetMode(ThemeMode.Light);

            yield return Match(new MatchRequest { Mode = GameMode.QuickDuel, Difficulty = "medium", ArenaId = "crates", Seed = 7 }, "quickduel", 6);
            yield return Match(new MatchRequest { Mode = GameMode.Campaign, Level = 13, Seed = 13 }, "level13", 4);
            yield return Match(new MatchRequest { Mode = GameMode.Campaign, Level = 20, Seed = 20 }, "boss", 4);

            Debug.Log("[ArcherArcade] Capture done: " + _index + " images in " + Path.GetFullPath(Folder));
            EditorApplication.ExitPlaymode();
            if (QuitWhenDone) EditorApplication.Exit(0);
        }

        IEnumerator Match(MatchRequest request, string name, int shots)
        {
            SceneFlow.StartMatch(request);
            yield return WaitFor(() => FindAnyObjectByType<MatchSceneRoot>() != null, 20f);
            MatchSceneRoot root = FindAnyObjectByType<MatchSceneRoot>();
            root.Autopilot = true;
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot(name + "_intro");
            for (int i = 0; i < shots; i++)
            {
                yield return new WaitForSecondsRealtime(2.2f);
                if (root == null) yield break;
                yield return Shot(name + "_" + i);
            }
            SceneFlow.GoHome();
            yield return WaitFor(() => FindAnyObjectByType<HomeSceneRoot>() != null, 20f);
            yield return new WaitForSecondsRealtime(0.8f);
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(Folder, (_index++).ToString("00") + "_" + name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            yield return null;
        }

        static IEnumerator WaitFor(Func<bool> condition, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
        }
    }
}
