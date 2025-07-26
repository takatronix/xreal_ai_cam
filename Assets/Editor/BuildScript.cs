using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

public class BuildScript
{
    [MenuItem("Build/Build Android")]
    public static void BuildAndroid()
    {
        Debug.Log("Starting XREAL AI Camera Android build...");
        
        // Build settings
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/XREALAICameraScene.unity" };
        buildPlayerOptions.locationPathName = "Build/xreal_ai_cam.apk";
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        // Android specific settings
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.companyName = "XREAL AI Team";
        PlayerSettings.productName = "XREAL AI Camera - Image Recognition";
        
        // Android XR settings - Updated for XREAL compatibility
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel28;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
        PlayerSettings.Android.useCustomKeystore = false;
        
        Debug.Log("Build settings configured for XREAL One Pro & XREAL Eye devices...");
        
        // Start build
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {summary.outputPath}");
            Debug.Log($"Build size: {summary.totalSize} bytes");
        }
        else
        {
            Debug.LogError($"Build failed: {summary.result}");
            foreach (var step in report.steps)
            {
                foreach (var message in step.messages)
                {
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                    {
                        Debug.LogError($"Build error: {message.content}");
                    }
                }
            }
        }
    }
    
    public static void BuildAndroidCommandLine()
    {
        BuildAndroid();
    }
} 