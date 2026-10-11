using System.IO;
using UnityEditor;
using UnityEngine;

namespace SlotMachine.Editor
{
    [InitializeOnLoad]
    public static class AutoSceneBuilder
    {
        static AutoSceneBuilder()
        {
            EditorApplication.delayCall += OnEditorReady;
        }

        private static void OnEditorReady()
        {
            string scenePath = "Assets/Scenes/SlotMachineScene.unity";
            if (!File.Exists(scenePath))
            {
                Debug.Log("<color=yellow>[SlotMachine AutoBuilder]</color> Scene not found, building now...");
                SlotMachineSceneBuilder.BuildScene();
                Debug.Log("<color=green>[SlotMachine AutoBuilder]</color> Scene built successfully!");
            }
            else
            {
                Debug.Log("<color=cyan>[SlotMachine AutoBuilder]</color> SlotMachineScene already exists!");
            }
        }
    }
}
