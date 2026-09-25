using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcherArcade.Core
{
    /// <summary>
    /// Boot scene entry (first scene in the build, placed by the scene builder): starts the game services
    /// (save, settings, theme, audio, haptics, display rate) once, then opens the Home scene, which shows the splash.
    /// </summary>
    public sealed class BootSceneRoot : MonoBehaviour
    {
        void Start()
        {
            GameManager.Ensure();
            SceneManager.LoadScene(SceneNames.Home);
        }
    }
}
