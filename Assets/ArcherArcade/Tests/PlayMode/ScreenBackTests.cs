using System;
using System.Collections;
using ArcherArcade.Core;
using ArcherArcade.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ArcherArcade.Tests
{
    /// <summary>
    /// PROGRESS Phase 15: every Home-scene screen opens without errors and Android Back returns to Home (CLAUDE.md:
    /// correct Back everywhere, no dead screens); Back on Home asks before leaving.
    /// </summary>
    public class ScreenBackTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (SceneUtility.GetBuildIndexByScenePath("Assets/ArcherArcade/Scenes/Home.unity") < 0) Assert.Ignore("Run ArcherArcade ▸ Build ▸ Scenes first.");
            GameManager.Ensure();
            ServiceLocator.Profile.Data.TutorialDone = true;
            SceneFlow.GoHome();
            float end = Time.realtimeSinceStartup + 20f;
            while ((UnityEngine.Object.FindAnyObjectByType<HomeSceneRoot>() == null || SceneFlow.Busy) && Time.realtimeSinceStartup < end) yield return null;
            yield return new WaitForSecondsRealtime(2.5f);
        }

        [UnityTest]
        public IEnumerator EveryTargetOpensAndBackReturnsHome()
        {
            UIManager ui = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            Assert.IsNotNull(ui, "Home UI");
            foreach (HomeTarget target in Enum.GetValues(typeof(HomeTarget)))
            {
                HomeSceneRoot.Open(ui, target);
                yield return new WaitForSecondsRealtime(0.3f);
                Assert.IsNotNull(ui.Current, target + " shows a screen");
                int guard = 0;
                while (!(ui.Current is HomeScreen) && guard++ < 6)
                {
                    ui.Back();
                    yield return new WaitForSecondsRealtime(0.25f);
                }
                Assert.IsInstanceOf<HomeScreen>(ui.Current, "Back from " + target + " returns Home");
                Assert.IsFalse(ui.HasModal, "no dialog left open after " + target);
            }
        }

        [UnityTest]
        public IEnumerator BackOnHomeAsksBeforeLeaving()
        {
            UIManager ui = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            ui.ResetTo(new HomeScreen());
            yield return null;
            ui.Back();
            yield return null;
            Assert.IsTrue(ui.HasModal, "exit confirm shown");
            ui.Back();
            yield return null;
            Assert.IsFalse(ui.HasModal, "Back = Stay");
        }
    }
}
