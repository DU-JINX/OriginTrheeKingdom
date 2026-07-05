using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 原版 Unity4 迁移工程的 Android 构建入口。
/// </summary>
public static class OriginalPortAndroidBuild
{
    private const string OutputPath = "Builds/Android/ThreeKingdomsOriginalPort-debug.apk";
    private const string ApplicationId = "com.xix.threekingdoms.originalport";
    private const string ProductName = "三国群英传原版迁移";

    private static readonly string[] BuildScenes =
    {
        "Assets/Scenes/StartScene.unity",
        "Assets/Scenes/ContinueGame.unity",
        "Assets/Scenes/HowToPlay.unity",
        "Assets/Scenes/SelectKing.unity",
        "Assets/Scenes/InternalAffairs.unity",
        "Assets/Scenes/Strategy.unity",
        "Assets/Scenes/SelectGeneralToWar.unity",
        "Assets/Scenes/WarScene.unity",
        "Assets/Scenes/GameVictory.unity",
        "Assets/Scenes/GameOver.unity"
    };

    /// <summary>
    /// 构建 Android debug APK。
    /// </summary>
    /// <returns>无返回值；成功时输出 APK，失败时抛出异常并让批处理退出失败。</returns>
    public static void BuildAndroidDebugApk()
    {
        // 1. 校验场景和输出目录，避免构建时靠 Unity 的模糊错误定位。
        ValidateBuildScenes();
        string outputAbsolutePath = Path.GetFullPath(OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputAbsolutePath));

        // 2. 写入 Android 构建设置，保持本迁移工程可重复构建。
        ApplyAndroidPlayerSettings();
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

        // 3. 执行 APK 构建，保留 debug 签名但关闭 development 标记，降低系统兼容性提示概率。
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = BuildScenes,
            locationPathName = outputAbsolutePath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);

        // 4. 明确失败原因，避免生成半截产物时被误认为成功。
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException("Android APK 构建失败：" + report.summary.result);
        }

        Debug.Log("Android APK 构建成功：" + outputAbsolutePath);
    }

    /// <summary>
    /// 校验构建场景是否全部存在。
    /// </summary>
    /// <returns>无返回值；缺失场景时抛出异常。</returns>
    public static void ValidateBuildScenes()
    {
        foreach (string scene in BuildScenes)
        {
            if (!File.Exists(scene))
            {
                throw new FileNotFoundException("构建场景不存在", scene);
            }
        }
    }

    /// <summary>
    /// 应用 Android PlayerSettings 和构建系统设置。
    /// </summary>
    /// <returns>无返回值；直接修改当前迁移工程的 Unity 设置。</returns>
    private static void ApplyAndroidPlayerSettings()
    {
        PlayerSettings.companyName = "xix";
        PlayerSettings.productName = ProductName;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
    }
}
