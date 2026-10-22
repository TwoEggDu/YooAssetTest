using System;
using System.Collections;
using UniFramework.Event;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

// Opt-in integration probe. In normal Editor/Player sessions this creates no objects or listeners.
public sealed class DemoSmokeProbe : MonoBehaviour
{
    private readonly EventGroup events = new EventGroup();
    private bool finished;
    private bool sawError;
    private bool expectVersionFailure;
    private int downloadCount;
    private float deadline;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-yooSmoke") < 0) return;
        var instance = new GameObject("[YooAssetTest Smoke Probe]").AddComponent<DemoSmokeProbe>();
        DontDestroyOnLoad(instance.gameObject);
    }

    private void Awake()
    {
        expectVersionFailure = Array.IndexOf(Environment.GetCommandLineArgs(), "-yooExpectVersionFailure") >= 0;
        deadline = Time.realtimeSinceStartup + 120;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.logMessageReceived += OnLog;
        events.AddListener<PatchStepChangedEvent>(message => Debug.Log("[YooAssetTest] STEP " + ((PatchStepChangedEvent)message).Tips));
        events.AddListener<PatchFoundUpdateFilesEvent>(OnDownloadFound);
        events.AddListener<PatchInitializeFailedEvent>(message => Finish(false, "Package initialization failed"));
        events.AddListener<PatchPackageManifestUpdateFailedEvent>(message => Finish(false, "Manifest update failed"));
        events.AddListener<PatchPackageVersionRequestFailedEvent>(message =>
        {
            Finish(expectVersionFailure, expectVersionFailure ? "EXPECTED_VERSION_FAILURE" : "Version request failed");
        });
        events.AddListener<PatchWebFileDownloadFailedEvent>(message => Finish(false, "Bundle download failed"));
    }

    private void OnDownloadFound(IEventMessage message)
    {
        var found = (PatchFoundUpdateFilesEvent)message;
        downloadCount = found.TotalCount;
        Debug.Log($"[YooAssetTest] DOWNLOAD_FOUND count={found.TotalCount} bytes={found.TotalSizeBytes}");
        StartCoroutine(ConfirmDownload());
    }

    private IEnumerator ConfirmDownload()
    {
        yield return null;
        UserBeginDownloadWebFilesEvent.SendEventMessage();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "scene_home") StartCoroutine(VerifyAssets());
    }

    private IEnumerator VerifyAssets()
    {
        yield return null;
        var package = YooAssets.GetPackage("DefaultPackage");
        var asset = package.LoadAssetAsync<GameObject>("asteroid01");
        yield return asset;
        if (asset.Status != EOperationStatus.Succeeded || asset.AssetObject == null)
        {
            string error = asset.Error;
            asset.Release();
            Finish(false, "Prefab load failed: " + error);
            yield break;
        }
        var instance = Instantiate(asset.AssetObject);
        Destroy(instance);
        yield return null;
        asset.Release();
        Debug.Log($"[YooAssetTest] ASSET_LIFECYCLE_OK version={package.GetPackageVersion()} downloaded={downloadCount}");
        var label = FindObjectOfType<UIHomeWindow>();
        if (label == null) { Finish(false, "Home UI did not load"); yield break; }
        var versionText = label.transform.Find("version").GetComponent<UnityEngine.UI.Text>();
        Debug.Log("[YooAssetTest] HOME_UI " + versionText.text + " fontSize=" + versionText.fontSize);
        Finish(true, "HOME_SCENE_AND_PREFAB_OK");
    }

    private void Update()
    {
        if (!finished && Time.realtimeSinceStartup > deadline) Finish(false, "120 second timeout");
    }

    private void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error) sawError = true;
        if (type == LogType.Exception || type == LogType.Assert)
            Finish(false, "Unity error: " + message);
    }

    private void Finish(bool success, string reason)
    {
        if (finished) return;
        if (success && expectVersionFailure && reason != "EXPECTED_VERSION_FAILURE")
        {
            success = false;
            reason = "Expected version request failure, but the workflow succeeded";
        }
        if (success && sawError && reason != "EXPECTED_VERSION_FAILURE")
        {
            success = false;
            reason = "Unexpected Unity error occurred; inspect log";
        }
        finished = true;
        Debug.Log($"[YooAssetTest] SMOKE_{(success ? "PASS" : "FAIL")} {reason}");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(success ? 0 : 2);
#else
        Application.Quit(success ? 0 : 2);
#endif
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.logMessageReceived -= OnLog;
        events.RemoveAllListener();
    }
}
