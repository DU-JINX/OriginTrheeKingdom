using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 二代威力加强版恢复 AssetBundle 加载器。
/// </summary>
public static class Sango2RecoveredBundleLoader
{
    private const string BundleDirectoryName = "Sango2RecoveredBundles";
    private const string SkillBundlePrefix = "sango2_skill_frames_";
    private const int SkillFramesPerBundle = 4000;

    /// <summary>
    /// 方法说明：按恢复技能帧序号异步加载 Texture2D。
    /// 参数说明：frameIndex 为从 1 开始的技能帧序号，onLoaded 为成功回调，onError 为失败回调。
    /// 返回说明：返回 Unity 协程枚举器。
    /// </summary>
    public static IEnumerator LoadSkillFrameTexture(int frameIndex, Action<Texture2D> onLoaded, Action<string> onError)
    {
        // 1. 校验帧号并计算所在 AssetBundle。
        if (frameIndex <= 0)
        {
            ReportError(onError, "技能帧序号必须从 1 开始：" + frameIndex);
            yield break;
        }

        string bundleName = GetSkillBundleName(frameIndex);
        string assetName = GetSkillFrameAssetName(frameIndex);

        // 2. 通过 UnityWebRequest 兼容 Android APK 内 StreamingAssets 路径。
        string bundleUri = BuildBundleUri(bundleName);
        using (UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(bundleUri))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                ReportError(onError, "技能资源 Bundle 加载失败：" + bundleUri + " / " + request.error);
                yield break;
            }

            // 3. 从 Bundle 中读取指定 Texture2D。
            AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(request);
            if (bundle == null)
            {
                ReportError(onError, "技能资源 Bundle 内容为空：" + bundleUri);
                yield break;
            }

            AssetBundleRequest assetRequest = bundle.LoadAssetAsync<Texture2D>(assetName);
            yield return assetRequest;
            Texture2D texture = assetRequest.asset as Texture2D;
            if (texture == null)
            {
                bundle.Unload(false);
                ReportError(onError, "技能帧资源不存在：" + bundleName + "/" + assetName);
                yield break;
            }

            // 4. 返回贴图并释放 Bundle 元数据，保留已加载贴图。
            bundle.Unload(false);
            if (onLoaded != null)
            {
                onLoaded(texture);
            }
        }
    }

    /// <summary>
    /// 方法说明：根据技能帧序号计算 AssetBundle 名称。
    /// 参数说明：frameIndex 为从 1 开始的技能帧序号。
    /// 返回说明：返回 AssetBundle 名称。
    /// </summary>
    public static string GetSkillBundleName(int frameIndex)
    {
        int bundleIndex = (frameIndex - 1) / SkillFramesPerBundle + 1;
        return SkillBundlePrefix + bundleIndex.ToString("D2");
    }

    /// <summary>
    /// 方法说明：根据技能帧序号计算 Bundle 内资源名。
    /// 参数说明：frameIndex 为从 1 开始的技能帧序号。
    /// 返回说明：返回 Bundle 内 Texture2D 资源名。
    /// </summary>
    public static string GetSkillFrameAssetName(int frameIndex)
    {
        return "SkillFrame" + frameIndex.ToString("D5");
    }

    /// <summary>
    /// 方法说明：生成 StreamingAssets 中 Bundle 的 URI。
    /// 参数说明：bundleName 为 Bundle 文件名。
    /// 返回说明：返回可交给 UnityWebRequest 使用的 URI。
    /// </summary>
    public static string BuildBundleUri(string bundleName)
    {
        return Path.Combine(Application.streamingAssetsPath, BundleDirectoryName, bundleName);
    }

    /// <summary>
    /// 方法说明：统一上报加载错误。
    /// 参数说明：onError 为错误回调，message 为错误文本。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ReportError(Action<string> onError, string message)
    {
        Debug.LogError(message);
        if (onError != null)
        {
            onError(message);
        }
    }
}
