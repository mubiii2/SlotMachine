using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SlotMachine.Editor
{
    public static class SlotMachineBuildScript
    {
        [MenuItem("SlotMachine/Build WebGL")]
        public static void BuildWebGL()
        {
            Debug.Log("<color=cyan>[SlotMachine Build]</color> Starting WebGL Build process...");

            // Build the current configured scene
            string scenePath = "Assets/Scenes/SlotMachineScene.unity";
            if (!File.Exists(scenePath))
            {
                SlotMachineSceneBuilder.BuildScene();
            }

            string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Build", "WebGL");
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            // Player Settings for WebGL
            PlayerSettings.companyName = "SlotMachineDev";
            PlayerSettings.productName = "Classic Casino Slot Machine";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            // WebGL compression format: Disabled is best for local testing and universal server hosting without gzip/brotli mime-type headers issues
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;

            string[] scenes = new string[] { "Assets/Scenes/SlotMachineScene.unity" };

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=green>[SlotMachine Build SUCCESS]</color> WebGL Build completed in {summary.totalTime.TotalSeconds:F1}s! Output size: {summary.totalSize} bytes");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"<color=red>[SlotMachine Build FAILED]</color> Errors: {summary.totalErrors}");
                throw new Exception($"Build failed with {summary.totalErrors} errors.");
            }
        }
    }
}
