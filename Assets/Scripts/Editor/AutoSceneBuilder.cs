using UnityEditor;
using UnityEngine;

namespace SlotMachine.Editor
{
    [InitializeOnLoad]
    public static class AutoSceneBuilder
    {
        private const string VersionKey = "SlotMachine_SceneBuilt_v2_lever_fix";

        static AutoSceneBuilder()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            if (!SessionState.GetBool(VersionKey, false))
            {
                SessionState.SetBool(VersionKey, true);
                Debug.Log("<color=yellow>[SlotMachine AutoBuilder]</color> Rebuilding scene with lever and input fixes...");
                SlotMachineSceneBuilder.BuildScene();
                Debug.Log("<color=green>[SlotMachine AutoBuilder]</color> Scene rebuild completed!");
            }
        }
    }
}
