using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidIl2CppSmokeBuild
{
    private const string OutputEnvironmentVariable = "VLTK_ANDROID_SMOKE_OUT";

    [Serializable]
    private sealed class SmokeReport
    {
        public string unityVersion;
        public string outputPath;
        public string result;
        public ulong totalSizeBytes;
        public double totalTimeSeconds;
        public int totalErrors;
        public int totalWarnings;
        public string scriptingBackend;
        public int minimumApiLevel;
        public string architectures;
        public bool development;
        public bool customKeystore;
    }

    public static void Build()
    {
        string outputDirectory = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(outputDirectory))
            outputDirectory = "/tmp/vltk-android-il2cpp-api25-smoke";
        outputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(outputDirectory);

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path) != null)
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0)
            throw new BuildFailedException("Android smoke build has no enabled, importable scene");

        ScriptingImplementation previousBackend =
            PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
        AndroidSdkVersions previousMinimumApi = PlayerSettings.Android.minSdkVersion;
        AndroidArchitecture previousArchitectures = PlayerSettings.Android.targetArchitectures;
        bool previousDevelopment = EditorUserBuildSettings.development;
        bool previousAppBundle = EditorUserBuildSettings.buildAppBundle;
        bool previousCustomKeystore = PlayerSettings.Android.useCustomKeystore;
        string previousDefines =
            PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);

        string apkPath = Path.Combine(outputDirectory, "vltk-mobile-dev.apk");
        try
        {
            PlayerSettings.SetScriptingBackend(
                BuildTargetGroup.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Android,
                string.Join(
                    ";",
                    previousDefines.Split(';')
                        .Where(symbol => !string.Equals(
                            symbol.Trim(),
                            "VLTK_ENABLE_TESTS",
                            StringComparison.Ordinal))));
            AssetDatabase.SaveAssets();

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            var smoke = new SmokeReport
            {
                unityVersion = Application.unityVersion,
                outputPath = apkPath,
                result = summary.result.ToString(),
                totalSizeBytes = summary.totalSize,
                totalTimeSeconds = summary.totalTime.TotalSeconds,
                totalErrors = summary.totalErrors,
                totalWarnings = summary.totalWarnings,
                scriptingBackend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android).ToString(),
                minimumApiLevel = (int)PlayerSettings.Android.minSdkVersion,
                architectures = PlayerSettings.Android.targetArchitectures.ToString(),
                development = EditorUserBuildSettings.development,
                customKeystore = PlayerSettings.Android.useCustomKeystore,
            };
            File.WriteAllText(
                Path.Combine(outputDirectory, "build-report.json"),
                JsonUtility.ToJson(smoke, true) + "\n");

            if (summary.result != BuildResult.Succeeded || !File.Exists(apkPath))
                throw new BuildFailedException(
                    $"Android smoke build failed: {summary.result}, errors={summary.totalErrors}");
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, previousBackend);
            PlayerSettings.Android.minSdkVersion = previousMinimumApi;
            PlayerSettings.Android.targetArchitectures = previousArchitectures;
            PlayerSettings.Android.useCustomKeystore = previousCustomKeystore;
            EditorUserBuildSettings.development = previousDevelopment;
            EditorUserBuildSettings.buildAppBundle = previousAppBundle;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Android,
                previousDefines);
            AssetDatabase.SaveAssets();
        }
    }
}
