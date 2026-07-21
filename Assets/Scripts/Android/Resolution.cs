using UnityEngine;

/// <summary>
/// Android 分辨率兼容控制器，用于替代旧 UnityScript 版本。
/// </summary>
public sealed class Resolution : MonoBehaviour
{
    public int screenWidth = 1280;
    public int screenHeight = 720;

    private const int MinimumHighDefinitionWidth = 1280;
    private const int MinimumHighDefinitionHeight = 720;
    private const float LegacyReferenceHeight = 480f;

    /// <summary>
    /// 方法说明：初始化 Android 运行时分辨率和正交相机尺寸。
    /// 参数说明：无。
    /// </summary>
    /// <returns>无返回值。</returns>
    private void Start()
    {
        // 1. 保持旧工程默认帧率策略。
        Application.targetFrameRate = -1;

        // 2. 仅在 Android 运行时应用旧工程固定分辨率策略。
        if (Application.platform != RuntimePlatform.Android)
        {
            return;
        }

        // 3. 主相机必须真实存在，缺失就是迁移错误，不做假兜底。
        Camera sceneCamera = GetComponent<Camera>();
        if (sceneCamera == null)
        {
            throw new MissingComponentException("Resolution requires a Camera on the same GameObject.");
        }

        // 4. 使用设备真实高清输出，镜头仍保持旧工程 480 高世界坐标，避免画面被拉远。
        QualitySettings.masterTextureLimit = 0;
        sceneCamera.orthographicSize = LegacyReferenceHeight / 2f;
        Vector2Int targetSize = GetAndroidTargetResolution();
        Screen.SetResolution(targetSize.x, targetSize.y, true);
    }

    /// <summary>
    /// 方法说明：计算 Android 运行时目标高清分辨率。
    /// 参数说明：无。
    /// 返回说明：返回不低于 1280x720 的当前设备分辨率，设备信息不可用时使用 Inspector 配置值。
    /// </summary>
    private Vector2Int GetAndroidTargetResolution()
    {
        int targetWidth = Mathf.Max(screenWidth, MinimumHighDefinitionWidth);
        int targetHeight = Mathf.Max(screenHeight, MinimumHighDefinitionHeight);

        if (Screen.currentResolution.width > 0 && Screen.currentResolution.height > 0)
        {
            targetWidth = Mathf.Max(Screen.currentResolution.width, MinimumHighDefinitionWidth);
            targetHeight = Mathf.Max(Screen.currentResolution.height, MinimumHighDefinitionHeight);
        }

        return new Vector2Int(targetWidth, targetHeight);
    }
}
