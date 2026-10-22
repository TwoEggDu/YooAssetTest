using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

// Integration helpers only. Runtime loading and updating remain the official Demo's code.
public static class DemoProjectSetup
{
    public const string BootScene = "Assets/Samples/Space Shooter/Boot.unity";
    private const string PackageName = "DefaultPackage";

    [MenuItem("YooAsset Test/1. Configure and Open Demo")]
    public static void Configure()
    {
        PlayerSettings.companyName = "YooAssetLearning";
        PlayerSettings.productName = "YooAssetTest";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.defaultScreenWidth = 960;
        PlayerSettings.defaultScreenHeight = 600;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScene, true) };
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tagList = tags.FindProperty("tags");
        bool found = false;
        for (int i = 0; i < tagList.arraySize; i++) found |= tagList.GetArrayElementAtIndex(i).stringValue == "Enemy";
        if (!found)
        {
            tagList.InsertArrayElementAtIndex(tagList.arraySize);
            tagList.GetArrayElementAtIndex(tagList.arraySize - 1).stringValue = "Enemy";
            tags.ApplyModifiedProperties();
        }
        BundleCollectorSettingData.FixFile();
        BundleCollectorSettingData.SaveFile();
        SetMode(EPlayMode.EditorSimulateMode);
        AssetDatabase.SaveAssets();
        Debug.Log("[YooAssetTest] CONFIGURED: Boot scene, Enemy tag, DefaultPackage.");
    }

    [MenuItem("YooAsset Test/2. Build Resources test-v1 (All Builtin)")]
    public static void BuildV1() { BuildResources("test-v1", EBundledCopyOption.ClearAndCopyAll); }

    [MenuItem("YooAsset Test/3. Build Resources test-v2 (Remote Only)")]
    public static void BuildV2() { BuildResources("test-v2", EBundledCopyOption.None); }

    private static void BuildResources(string version, EBundledCopyOption copyOption)
    {
        BundleCollectorSettingData.FixFile();
        var packRule = DefaultBundlePackRule.CreateShadersPackRuleResult();
        var parameters = new ScriptableBuildParameters
        {
            BuildOutputRoot = Path.GetFullPath("Bundles").Replace('\\', '/'),
            BundledFileRoot = BundleBuilderHelper.GetStreamingAssetsRoot(),
            BuildPipeline = nameof(YooAsset.Editor.ScriptableBuildPipeline),
            BuildBundleType = (int)EBundleType.AssetBundle,
            BuildTarget = BuildTarget.StandaloneWindows64,
            PackageName = PackageName,
            PackageVersion = version,
            EnableSharePackRule = true,
            VerifyBuildingResult = true,
            EnableAssetPathValidation = true,
            FileNameStyle = EFileNameStyle.HashName,
            CompressOption = ECompressOption.LZ4,
            BundledCopyOption = copyOption,
            BuiltinShadersBundleName = packRule.GetBundleName(PackageName, BundleCollectorSettingData.Setting.UniqueBundleName)
        };
        var result = new YooAsset.Editor.ScriptableBuildPipeline().Run(parameters, true);
        if (!result.Success) throw new InvalidOperationException(result.FailedTask + ": " + result.ErrorInfo);
        Debug.Log("[YooAssetTest] RESOURCES_BUILT " + version + " " + result.OutputPackageDirectory);
        AssetDatabase.Refresh();
    }

    [MenuItem("YooAsset Test/4. Build Windows Host Client")]
    public static void BuildHostPlayer() { BuildPlayer(EPlayMode.HostPlayMode, "Host"); }

    [MenuItem("YooAsset Test/5. Build Windows Offline Client")]
    public static void BuildOfflinePlayer() { BuildPlayer(EPlayMode.OfflinePlayMode, "Offline"); }

    private static void BuildPlayer(EPlayMode mode, string directory)
    {
        try
        {
            SetMode(mode);
            var path = "Builds/" + directory + "/YooAssetTest.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = UnityEditor.BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { BootScene }, locationPathName = path,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Player build failed: " + report.summary.result);
            Debug.Log("[YooAssetTest] PLAYER_BUILT " + mode + " " + path);
        }
        finally { SetMode(EPlayMode.EditorSimulateMode); }
    }

    private static void SetMode(EPlayMode mode)
    {
        EditorSceneManager.OpenScene(BootScene);
        var boot = UnityEngine.Object.FindObjectOfType<Boot>();
        if (boot == null) throw new InvalidOperationException("Official Boot component is missing.");
        boot.PlayMode = mode;
        EditorUtility.SetDirty(boot);
        EditorSceneManager.SaveOpenScenes();
    }

    public static void BuildInitial()
    {
        Configure();
        // Official Host options copy builtin manifests and require a builtin catalog.
        // Seed v1 first; the same client subsequently downloads the v2 delta from nginx.
        BuildV1();
        BuildHostPlayer();
        BuildOfflinePlayer();
    }

    [MenuItem("YooAsset Test/6. Rebuild Both Windows Clients (Existing Builtin Resources)")]
    public static void RebuildPlayers()
    {
        Configure();
        BuildHostPlayer();
        BuildOfflinePlayer();
    }

    public static void RunEditorSmoke()
    {
        Configure();
        EditorApplication.EnterPlaymode();
    }
}
