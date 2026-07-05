using UnityEngine;

/// <summary>
/// Android 分辨率兼容控制器，用于替代旧 UnityScript 版本。
/// </summary>
public sealed class Resolution : MonoBehaviour
{
    public int screenWidth = 640;
    public int screenHeight = 480;

    /// <summary>
    /// 初始化 Android 运行时分辨率和正交相机尺寸。
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

        // 4. 按旧 UnityScript 逻辑设置正交尺寸和屏幕分辨率。
        sceneCamera.orthographicSize = screenHeight / 2f;
        Screen.SetResolution(screenWidth, screenHeight, true);
    }
}
