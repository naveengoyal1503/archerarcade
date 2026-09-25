using UnityEditor;
using UnityEngine;

namespace ArcherArcade.EditorTools
{
    /// <summary>
    /// Starts the <see cref="CaptureRunner"/> when play mode begins after Batch.Capture armed it (survives the
    /// domain reload into play mode through SessionState).
    /// </summary>
    [InitializeOnLoad]
    public static class CaptureHook
    {
        const string Key = "ArcherArcade.Capture", QuitKey = "ArcherArcade.Capture.Quit";

        static CaptureHook()
        {
            EditorApplication.playModeStateChanged += Changed;
        }

        public static void Arm(bool quitWhenDone)
        {
            SessionState.SetBool(Key, true);
            SessionState.SetBool(QuitKey, quitWhenDone);
        }

        static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, false);
            var go = new GameObject("[Capture]");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<CaptureRunner>().QuitWhenDone = SessionState.GetBool(QuitKey, false);
        }
    }
}
