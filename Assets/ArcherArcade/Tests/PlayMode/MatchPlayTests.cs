using System;
using System.Collections;
using ArcherArcade.Core;
using ArcherArcade.Feel;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Logic.Modes;
using ArcherArcade.Match;
using ArcherArcade.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ArcherArcade.Tests
{
    /// <summary>
    /// PROGRESS Phase 15 PlayMode checks: a full Quick Duel played by the autopilot reaches the result screen,
    /// every mode's match starts, 2-Player shows the pass-the-phone cover between turns, and pause really freezes
    /// time. Waits in real time (lesson from MindTap: popups pause the game). Needs the scenes in Build Settings
    /// (ArcherArcade ▸ Build ▸ Scenes).
    /// </summary>
    public class MatchPlayTests
    {
        static bool ScenesReady => SceneUtility.GetBuildIndexByScenePath("Assets/ArcherArcade/Scenes/Match.unity") >= 0;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!ScenesReady) Assert.Ignore("Run ArcherArcade ▸ Build ▸ Scenes first.");
            GameManager.Ensure();
            ServiceLocator.Profile.Data.TutorialDone = true;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            TimeScaleDriver.ResetAll();
        }

        static IEnumerator Until(Func<bool> condition, float seconds, string what)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        static IEnumerator StartMatch(MatchRequest request)
        {
            SceneFlow.StartMatch(request);
            yield return Until(() => UnityEngine.Object.FindAnyObjectByType<MatchSceneRoot>() != null && !SceneFlow.Busy, 20f, "the Match scene");
        }

        static MatchSceneRoot Root => UnityEngine.Object.FindAnyObjectByType<MatchSceneRoot>();

        [UnityTest]
        public IEnumerator QuickDuelOnAutopilotReachesTheResult()
        {
            yield return StartMatch(new MatchRequest { Mode = GameMode.QuickDuel, Difficulty = "easy", ArenaId = "meadow", Seed = 11 });
            MatchSceneRoot root = Root;
            root.Autopilot = true;
            TimeScaleDriver.Speed = 4f;
            yield return Until(() => root.Ui.Current is ResultScreen, 240f, "the result screen");
            Assert.IsTrue(root.Session.IsOver);
        }

        [UnityTest]
        public IEnumerator EveryModeStartsAMatch()
        {
            var series = new PvpSeries(new PvpSettings());
            MatchRequest[] requests =
            {
                new MatchRequest { Mode = GameMode.Campaign, Level = 1, Seed = 1 },
                new MatchRequest { Mode = GameMode.Campaign, Level = 13, Seed = 2 },
                new MatchRequest { Mode = GameMode.Campaign, Level = 20, Seed = 3 },
                new MatchRequest { Mode = GameMode.QuickDuel, ArenaId = "platforms", Seed = 4 },
                new MatchRequest { Mode = GameMode.TwoPlayer, Series = series, Seed = 5 },
                new MatchRequest { Mode = GameMode.Daily, Day = 100, Seed = 100 },
                new MatchRequest { Mode = GameMode.Training, TrainingDistance = 20, Seed = 6 },
                new MatchRequest { Mode = GameMode.Survival, Seed = 7 }
            };
            foreach (MatchRequest r in requests)
            {
                yield return StartMatch(r);
                yield return Until(() =>
                {
                    // "New!" idea cards wait for a tap before the first turn.
                    if (Root != null && Root.Ui.TopModal is IdeaCardModal) Root.Ui.CloseAllModals();
                    return Root != null && (Root.Aiming || Root.AiTurnActive);
                }, 20f, r.Mode + " first turn");
                Assert.IsNotNull(Root.Ui.Current as HudScreen, r.Mode + " shows the HUD");
                SceneFlow.GoHome();
                yield return Until(() => UnityEngine.Object.FindAnyObjectByType<HomeSceneRoot>() != null && !SceneFlow.Busy, 20f, "Home");
            }
        }

        [UnityTest]
        public IEnumerator TwoPlayerCoversTheBoardBetweenTurns()
        {
            var series = new PvpSeries(new PvpSettings { WindOn = false });
            yield return StartMatch(new MatchRequest { Mode = GameMode.TwoPlayer, Series = series, Seed = 21 });
            MatchSceneRoot root = Root;
            yield return Until(() => root.Aiming, 20f, "the first turn");
            int first = root.M.CurrentSide;
            Assert.IsTrue(root.ShootNow(new ShotInput(40, 0.3)), "shot accepted");
            yield return Until(() => root.Ui.TopModal is PassModal || root.Session.IsOver, 30f, "the pass cover");
            Assert.IsInstanceOf<PassModal>(root.Ui.TopModal);
            ((PassModal)root.Ui.TopModal).Ready();
            yield return Until(() => root.Aiming, 10f, "the second player's turn");
            Assert.AreNotEqual(first, root.M.CurrentSide);
        }

        [UnityTest]
        public IEnumerator PauseFreezesTimeAndBackResumes()
        {
            yield return StartMatch(new MatchRequest { Mode = GameMode.QuickDuel, Seed = 31 });
            MatchSceneRoot root = Root;
            yield return Until(() => root.Aiming || root.AiTurnActive, 20f, "a turn");
            root.PauseGame();
            yield return null;
            Assert.AreEqual(0f, Time.timeScale, "paused");
            Assert.IsInstanceOf<PauseModal>(root.Ui.TopModal);
            float before = (float)root.M.TurnTimeLeft;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(before, (float)root.M.TurnTimeLeft, 0.0001f, "the turn timer stops while paused");
            root.Ui.Back();
            yield return null;
            Assert.IsFalse(root.Paused);
            Assert.Greater(Time.timeScale, 0f);
        }
    }
}
