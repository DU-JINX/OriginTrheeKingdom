using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 二代威力加强版 PAK 资源导入工具。
/// </summary>
public static class Sango2RecoveredResourceImporter
{
    private const string MenuRoot = "工具/二代复原资源/";
    private const string RecoveredRoot = "/Users/xix/SangoPowerRecovered";
    private const string FaceManifestPath = RecoveredRoot + "/reports/pak_media/faces_100x120.tsv";
    private const string MapManifestPath = RecoveredRoot + "/reports/pak_media/map_large.tsv";
    private const string SkillManifestPath = RecoveredRoot + "/reports/pak_media/skill_frames.tsv";
    private const string OggManifestPath = RecoveredRoot + "/reports/pak_media/audio_ogg.tsv";
    private const string RiffManifestPath = RecoveredRoot + "/reports/pak_media/audio_riff.tsv";
    private const string FaceDirectory = "Assets/Graph/Sango2Recovered/Face";
    private const string MapDirectory = "Assets/Graph/Sango2Recovered/Map";
    private const string ResourceMapDirectory = "Assets/Graph/Resources/Sango2Recovered/Map";
    private const string SkillImportDirectory = "Assets/__Sango2RecoveredSkillFrameImport";
    private const string SkillBundleDirectory = "Assets/StreamingAssets/Sango2RecoveredBundles";
    private const string AudioDirectory = "Assets/Sound/Resources/Sango2Recovered";
    private const string HeadPrefabDirectory = "Assets/Prefabs/Resources/Head";
    private const string HeadMaterialDirectory = "Assets/Graph/Sango2Recovered/FaceMaterials";
    private const string HeadTemplatePath = HeadPrefabDirectory + "/Head001.prefab";
    private const string WorldMapPath = MapDirectory + "/Sango2WorldMap.png";
    private const string WorldMapGeneratedResourcePath = ResourceMapDirectory + "/Sango2WorldMapGenerated.png";
    private const string WorldMapPreviewPath = MapDirectory + "/Sango2WorldMapPreview.png";
    private const string SkillBundlePrefix = "sango2_skill_frames_";
    private const string SkillMapReportPath = "Assets/XML/Resources/Sango2SkillResourceMap.tsv";
    private const int RestoredFaceCount = 1055;
    private const int RestoredOggLimit = 18;
    private const int RestoredRiffLimit = 134;
    private const int SkillFramesPerBundle = 4000;
    private const float RestoredMapDisplayWidth = 854f;
    private const float RestoredMapDisplayHeight = 546.56f;

    /// <summary>
    /// 方法说明：导入 MOD06 已分类的头像、地图和音频资源。
    /// 参数说明：无。
    /// 返回说明：无返回值；资源缺失或导入失败时抛出异常。
    /// </summary>
    [MenuItem(MenuRoot + "导入PAK资源")]
    public static void ImportRecoveredResources()
    {
        // 1. 校验分类清单，避免导入半截资源。
        List<RecoveredMediaRecord> faceRecords = LoadMediaRecords(FaceManifestPath, RestoredFaceCount);
        List<RecoveredMediaRecord> mapRecords = LoadMediaRecords(MapManifestPath, 2);
        List<RecoveredMediaRecord> oggRecords = LoadMediaRecords(OggManifestPath, RestoredOggLimit);
        List<RecoveredMediaRecord> riffRecords = LoadMediaRecords(RiffManifestPath, RestoredRiffLimit);

        // 2. 复制头像和大地图资源到 Unity 工程内。
        CopyFaceResources(faceRecords);
        CopyMapResources(mapRecords);
        CopyAudioResources(oggRecords, "ogg");
        CopyAudioResources(riffRecords, "wav");

        // 3. 导入贴图和音频，确保 Resources.Load 能发现资源。
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureFaceTextures();
        ConfigureMapTextures();

        // 4. 基于现有 Head001 模板生成 1055 个头像 prefab。
        GenerateHeadPrefabs();
        WriteImportReport(faceRecords.Count, mapRecords.Count, oggRecords.Count, riffRecords.Count);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("二代复原资源导入完成：头像 " + faceRecords.Count + "，地图 " + mapRecords.Count + "，音频 " + (oggRecords.Count + riffRecords.Count));
    }

    /// <summary>
    /// 方法说明：把恢复出的二代地图绑定到选君主和战略场景。
    /// 参数说明：无。
    /// 返回说明：无返回值；地图对象缺失时抛出异常。
    /// </summary>
    [MenuItem(MenuRoot + "应用二代地图到场景")]
    public static void ApplyRecoveredMapToScenes()
    {
        // 1. 校验地图资源已导入。
        ValidateAssetFile(WorldMapPath);
        ValidateAssetFile(WorldMapPreviewPath);

        // 2. 更新选君主场景的背景和势力地图。
        Scene selectKingScene = EditorSceneManager.OpenScene("Assets/Scenes/SelectKing.unity", OpenSceneMode.Single);
        ApplySceneSprite(selectKingScene, "Background", WorldMapPreviewPath, 640f, 480f);
        ApplySceneSprite(selectKingScene, "SelectKing/Map", WorldMapPath, RestoredMapDisplayWidth, RestoredMapDisplayHeight);
        EditorSceneManager.SaveScene(selectKingScene);

        // 3. 更新战略场景主背景和内政势力地图。
        Scene strategyScene = EditorSceneManager.OpenScene("Assets/Scenes/Strategy.unity", OpenSceneMode.Single);
        ApplySceneSprite(strategyScene, "Background", WorldMapPath, RestoredMapDisplayWidth, RestoredMapDisplayHeight);
        ApplySceneSprite(strategyScene, "Main Camera/TotalCommands/SecondMenu/Map", WorldMapPath, RestoredMapDisplayWidth, RestoredMapDisplayHeight);
        EditorSceneManager.SaveScene(strategyScene);

        // 4. 保存资源并输出明确结果。
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("二代地图已应用到 SelectKing 和 Strategy 场景。");
    }

    /// <summary>
    /// 方法说明：把恢复出的技能帧打成 Android AssetBundle，并写出 MOD06 技能映射。
    /// 参数说明：无。
    /// 返回说明：无返回值；技能帧清单缺失或打包失败时抛出异常。
    /// </summary>
    [MenuItem(MenuRoot + "构建技能资源Bundle")]
    public static void BuildRecoveredSkillBundles()
    {
        // 1. 读取技能帧清单并清理临时导入目录。
        List<RecoveredMediaRecord> skillRecords = LoadAllMediaRecords(SkillManifestPath);
        DeleteAssetDirectory(SkillImportDirectory);
        Directory.CreateDirectory(SkillImportDirectory);
        Directory.CreateDirectory(SkillBundleDirectory);

        // 2. 复制技能帧到临时工程目录，并按批次写入 AssetBundle 名称。
        CopySkillFrameResources(skillRecords);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssignSkillFrameBundleNames(skillRecords.Count);

        // 3. 构建 Android AssetBundle，让 APK 通过 StreamingAssets 携带可加载技能帧。
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
            SkillBundleDirectory,
            BuildAssetBundleOptions.ChunkBasedCompression,
            BuildTarget.Android);
        if (manifest == null)
        {
            throw new InvalidOperationException("技能资源 AssetBundle 构建失败");
        }

        // 4. 写出 MOD06 技能 ID 到当前 Magic prefab 和恢复技能帧 bundle 的映射，并删除临时散图。
        WriteSkillResourceMap(skillRecords.Count);
        DeleteAssetDirectory(SkillImportDirectory);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("二代技能资源 Bundle 构建完成：帧数 " + skillRecords.Count + "，Bundle 数 " + GetSkillBundleCount(skillRecords.Count));
    }

    /// <summary>
    /// 方法说明：重建 1055 个恢复头像 prefab，并给每个头像绑定独立材质。
    /// 参数说明：无。
    /// 返回说明：无返回值；头像贴图或模板缺失时抛出异常。
    /// </summary>
    [MenuItem(MenuRoot + "重建头像Prefab材质")]
    public static void RebuildRecoveredHeadPrefabs()
    {
        // 1. 确保恢复头像贴图已经按正确导入参数进入工程。
        ConfigureFaceTextures();

        // 2. 用当前 Head001 模板重建所有 Head### prefab。
        GenerateHeadPrefabs();

        // 3. 保存并刷新，让 Resources.Load 读取到最新 prefab 和材质引用。
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("二代恢复头像 prefab 和独立材质已重建：" + RestoredFaceCount);
    }

    /// <summary>
    /// 方法说明：输出 exSprite 组件字段，辅助确认 Unity4 资源迁移结构。
    /// 参数说明：无。
    /// 返回说明：无返回值；结果输出到 Unity Console。
    /// </summary>
    [MenuItem(MenuRoot + "检查exSprite字段")]
    public static void DumpExSpriteSchema()
    {
        Type type = typeof(exSprite);
        foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            Debug.Log("exSprite field: " + field.Name + " type=" + field.FieldType.FullName + " public=" + field.IsPublic);
        }

        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            Debug.Log("exSprite property: " + property.Name + " type=" + property.PropertyType.FullName + " canWrite=" + property.CanWrite);
        }

        foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (method.Name.Contains("Sprite") || method.Name.Contains("Texture") || method.Name.Contains("Frame"))
            {
                Debug.Log("exSprite method: " + method.Name + " params=" + BuildMethodParameterLog(method));
            }
        }
    }

    /// <summary>
    /// 方法说明：输出选君主和战略场景中的地图对象结构。
    /// 参数说明：无。
    /// 返回说明：无返回值；结果输出到 Unity Console。
    /// </summary>
    [MenuItem(MenuRoot + "检查地图对象")]
    public static void DumpMapSceneObjects()
    {
        DumpMapSceneObjects("Assets/Scenes/SelectKing.unity");
        DumpMapSceneObjects("Assets/Scenes/Strategy.unity");
    }

    /// <summary>
    /// 方法说明：输出主场景中的全部大尺寸 exSprite。
    /// 参数说明：无。
    /// 返回说明：无返回值；结果输出到 Unity Console。
    /// </summary>
    [MenuItem(MenuRoot + "检查大图Sprite")]
    public static void DumpLargeSprites()
    {
        DumpLargeSprites("Assets/Scenes/SelectKing.unity");
        DumpLargeSprites("Assets/Scenes/Strategy.unity");
    }

    /// <summary>
    /// 方法说明：输出选择场景中的关键 UI 对象位置和文本。
    /// 参数说明：无。
    /// 返回说明：无返回值；结果输出到 Unity Console。
    /// </summary>
    [MenuItem(MenuRoot + "检查选择界面UI")]
    public static void DumpSelectSceneUi()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SelectKing.unity", OpenSceneMode.Single);
        Debug.Log("SELECT UI SCENE: " + scene.path + " loaded=" + scene.isLoaded);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                if (!IsSelectUiRelated(transform))
                {
                    continue;
                }

                Debug.Log("SELECT UI: " + BuildTransformPath(transform) +
                          " active=" + transform.gameObject.activeInHierarchy +
                          " localPos=" + FormatVector3(transform.localPosition) +
                          " worldPos=" + FormatVector3(transform.position) +
                          " components=" + BuildComponentLog(transform.gameObject) +
                          " text=" + GetFontText(transform.gameObject));
            }
        }
    }

    /// <summary>
    /// 方法说明：执行选择界面运行时初始化，并输出动态生成的副本、势力、头像和滚动控件。
    /// 参数说明：无。
    /// 返回说明：无返回值；结果输出到 Unity Console。
    /// </summary>
    [MenuItem(MenuRoot + "检查选择界面运行时UI")]
    public static void DumpSelectRuntimeUi()
    {
        DumpRuntimeModScreen();
        DumpRuntimeKingScreen();
    }

    /// <summary>
    /// 方法说明：渲染一张 MOD06 选择君主界面截图，用于确认左侧滚动列表视觉效果。
    /// 参数说明：无。
    /// 返回说明：无返回值；截图写入 Builds/Screenshots 目录。
    /// </summary>
    [MenuItem(MenuRoot + "截图选择君主界面")]
    public static void CaptureSelectKingRuntimeScreenshot()
    {
        const int captureWidth = 2048;
        const int captureHeight = 1152;

        // 1. 打开选择君主场景，并进入 MOD06 运行时选择君主状态。
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SelectKing.unity", OpenSceneMode.Single);
        SelectKingSceneController controller = FindSelectKingSceneController(scene);
        FindSceneCamera(scene).aspect = captureWidth / (float)captureHeight;
        Controller.MODSelect = 5;
        InvokePrivateControllerMethod(controller, "SelectKingEnter");
        Debug.Log("SELECT KING CAPTURE ROOT: kingListRoot=" + BuildTransformPath(controller.kingListRoot.transform) +
                  " localPos=" + FormatVector3(controller.kingListRoot.transform.localPosition) +
                  " localScale=" + FormatVector3(controller.kingListRoot.transform.localScale));
        Debug.Log("SELECT KING CAPTURE ROOT: menuAnim=" + BuildTransformPath(controller.menuAnim.transform) +
                  " localPos=" + FormatVector3(controller.menuAnim.transform.localPosition) +
                  " localScale=" + FormatVector3(controller.menuAnim.transform.localScale));

        // 2. 强制菜单动画停在最终位置，避免截图时菜单还在场外。
        ForceSelectKingMenuAtRest(controller);

        // 3. 从场景摄像机渲染当前画面。
        string outputPath = Path.Combine(Application.dataPath, "../Builds/Screenshots/select-king-scrollbar.png");
        CaptureSceneCameraPng(scene, outputPath, captureWidth, captureHeight);

        // 4. 输出绝对路径，便于直接打开检查。
        Debug.Log("SELECT KING SCREENSHOT: " + Path.GetFullPath(outputPath));
    }

    /// <summary>
    /// 方法说明：渲染一张剧本选择界面截图，用于确认剧本入口显示效果。
    /// 参数说明：无。
    /// 返回说明：无返回值；截图写入 Builds/Screenshots 目录。
    /// </summary>
    [MenuItem(MenuRoot + "截图剧本选择界面")]
    public static void CaptureSelectModRuntimeScreenshot()
    {
        // 1. 打开选择场景，并初始化剧本选择按钮。
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SelectKing.unity", OpenSceneMode.Single);
        SelectKingSceneController controller = FindSelectKingSceneController(scene);
        FindSceneCamera(scene).aspect = 2048f / 964f;
        InvokePrivateControllerMethod(controller, "SetupMODButtons");
        InvokePrivateControllerMethod(controller, "SelectMODEnter");

        // 2. 强制剧本选择菜单停在最终位置，避免截图时菜单还在右侧场外。
        ForceMenuAtRest(controller.selectMOD.GetComponent<MenuDisplayAnim>());

        // 3. 从场景摄像机渲染当前剧本选择画面。
        string outputPath = Path.Combine(Application.dataPath, "../Builds/Screenshots/select-mod-screen.png");
        CaptureSceneCameraPng(scene, outputPath, 2048, 964);

        // 4. 输出绝对路径，便于直接打开检查。
        Debug.Log("SELECT MOD SCREENSHOT: " + Path.GetFullPath(outputPath));
    }

    /// <summary>
    /// 方法说明：渲染一张 MOD06 战略势力地图截图，用于确认战略底部君主信息栏排版。
    /// 参数说明：无。
    /// 返回说明：无返回值；截图写入 Builds/Screenshots 目录。
    /// </summary>
    [MenuItem(MenuRoot + "截图战略势力地图")]
    public static void CaptureStrategyPowerMapRuntimeScreenshot()
    {
        const int captureWidth = 2048;
        const int captureHeight = 1152;

        // 1. 打开战略场景并加载 MOD06 正常运行数据。
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Strategy.unity", OpenSceneMode.Single);
        FindSceneCamera(scene).aspect = captureWidth / (float)captureHeight;
        Informations.Reset();
        StrategyController.Reset();
        Controller.MODSelect = MODLoadController.RestoredSango2Index;
        Controller.kingIndex = 0;
        if (!MODLoadController.Instance.LoadMOD(MODLoadController.RestoredSango2Index))
        {
            throw new InvalidOperationException("MOD06 加载失败，无法截图战略势力地图。");
        }
        Controller.kingIndex = FindActiveKingWithCitiesAndGenerals();

        // 2. 定位战略控制器和势力地图面板，只激活势力地图，避免旧主菜单和底栏重叠。
        StrategyController strategyController = FindSceneComponent<StrategyController>(scene);
        SyPowerMap powerMap = FindSceneComponent<SyPowerMap>(scene);
        InvokePrivateInstanceMethod(strategyController, "ApplyRestoredSango2StrategyMap");
        HideStrategyMainMenu(strategyController, powerMap.gameObject);
        powerMap.gameObject.SetActive(true);
        ForceStrategyPowerMapAtRest(powerMap);
        InvokePrivateInstanceMethod(strategyController, "BringStrategyMenuPanelToFront", new object[] { powerMap.gameObject });
        if (strategyController.hTimeCtrl != null)
        {
            strategyController.hTimeCtrl.gameObject.SetActive(false);
        }

        // 3. 强制相关动画停在最终位置，避免截图截到滑入过程。
        ForceMenuAtRest(powerMap.kingList.GetComponent<MenuDisplayAnim>());
        ForceMenuAtRest(powerMap.map.GetComponent<MenuDisplayAnim>());
        ForceMenuAtRest(powerMap.kingInfo.GetComponent<MenuDisplayAnim>());
        StrategyController.state = StrategyController.State.Pause;

        // 4. 从场景摄像机渲染当前战略势力地图画面。
        string outputPath = Path.Combine(Application.dataPath, "../Builds/Screenshots/strategy-power-map.png");
        CaptureSceneCameraPng(scene, outputPath, captureWidth, captureHeight);
        Debug.Log("STRATEGY POWER MAP SCREENSHOT: " + Path.GetFullPath(outputPath));
    }

    /// <summary>
    /// 方法说明：渲染一张 MOD06 战略主菜单截图，用于确认菜单背景和文字层级正常。
    /// 参数说明：无。
    /// 返回说明：无返回值；截图写入 Builds/Screenshots 目录。
    /// </summary>
    [MenuItem(MenuRoot + "截图战略主菜单")]
    public static void CaptureStrategyMainMenuRuntimeScreenshot()
    {
        const int captureWidth = 2048;
        const int captureHeight = 1152;

        // 1. 打开战略场景并加载 MOD06 数据。
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Strategy.unity", OpenSceneMode.Single);
        FindSceneCamera(scene).aspect = captureWidth / (float)captureHeight;
        PrepareRecoveredMod06Data();

        // 2. 走真实战略主菜单打开路径，验证主菜单层级修复。
        StrategyController strategyController = FindSceneComponent<StrategyController>(scene);
        InvokePrivateInstanceMethod(strategyController, "ApplyRestoredSango2StrategyMap");
        if (strategyController.flagsCtrl != null)
        {
            InvokePrivateInstanceMethod(strategyController.flagsCtrl, "Start");
            strategyController.flagsCtrl.SetFlagsAnimPause();
        }
        InvokePrivateInstanceMethod(strategyController, "ShowMainMenuAtScreenPosition", new object[] {
            new Vector3(captureWidth * 0.5f, captureHeight * 0.5f, 0f)
        });
        UnifiedGameFontController.RefreshSceneFontsForPreview();

        // 3. 从场景摄像机渲染当前战略主菜单画面。
        string outputPath = Path.Combine(Application.dataPath, "../Builds/Screenshots/strategy-main-menu.png");
        CaptureSceneCameraPng(scene, outputPath, captureWidth, captureHeight);
        Debug.Log("STRATEGY MAIN MENU SCREENSHOT: " + Path.GetFullPath(outputPath));
    }

    /// <summary>
    /// 方法说明：渲染一张 MOD06 普通战略地图截图，用于确认主地图城名和旗帜显示正常。
    /// 参数说明：无。
    /// 返回说明：无返回值；截图写入 Builds/Screenshots 目录。
    /// </summary>
    [MenuItem(MenuRoot + "截图战略普通地图")]
    public static void CaptureStrategyMapRuntimeScreenshot()
    {
        const int captureWidth = 2048;
        const int captureHeight = 1152;

        // 1. 打开战略场景并加载 MOD06 数据。
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Strategy.unity", OpenSceneMode.Single);
        FindSceneCamera(scene).aspect = captureWidth / (float)captureHeight;
        PrepareRecoveredMod06Data();

        // 2. 初始化地图、旗帜和城名，保持普通地图状态不打开任何菜单。
        StrategyController strategyController = FindSceneComponent<StrategyController>(scene);
        InvokePrivateInstanceMethod(strategyController, "ApplyRestoredSango2StrategyMap");
        HideStrategyMainMenu(strategyController, null);
        if (strategyController.flagsCtrl != null)
        {
            InvokePrivateInstanceMethod(strategyController.flagsCtrl, "Start");
            strategyController.flagsCtrl.SetFlagsAnimResume();
        }
        UnifiedGameFontController.RefreshSceneFontsForPreview();

        // 3. 从场景摄像机渲染普通战略地图。
        string outputPath = Path.Combine(Application.dataPath, "../Builds/Screenshots/strategy-map-overview.png");
        CaptureSceneCameraPng(scene, outputPath, captureWidth, captureHeight);
        Debug.Log("STRATEGY MAP SCREENSHOT: " + Path.GetFullPath(outputPath));
    }

    /// <summary>
    /// 方法说明：渲染战前主菜单、阵型菜单和武将位置菜单截图，用于确认文字虚影和面板压字已修复。
    /// 参数说明：无。
    /// 返回说明：无返回值；截图写入 Builds/Screenshots 目录。
    /// </summary>
    [MenuItem(MenuRoot + "截图战前重点页面")]
    public static void CaptureWarSelectRuntimeScreenshots()
    {
        const int captureWidth = 2048;
        const int captureHeight = 1152;

        // 1. 使用 MOD06 真实数据准备一组战前双方军队。
        PrepareRecoveredMod06Data();
        BuildFocusedBattleSample();

        // 2. 打开战前场景并执行控制器初始化。
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SelectGeneralToWar.unity", OpenSceneMode.Single);
        FindSceneCamera(scene).aspect = captureWidth / (float)captureHeight;
        SelectGeneralToWarController controller = FindSceneComponent<SelectGeneralToWarController>(scene);
        InvokePrivateInstanceMethod(controller, "Start");
        UnifiedGameFontController.RefreshSceneFontsForPreview();

        // 3. 截取战前主菜单。
        string mainPath = Path.Combine(Application.dataPath, "../Builds/Screenshots/war-select-main.png");
        CaptureSceneCameraPng(scene, mainPath, captureWidth, captureHeight);
        Debug.Log("WAR SELECT MAIN SCREENSHOT: " + Path.GetFullPath(mainPath));

        // 4. 打开阵型菜单并截取，检查红色选中项无双层虚影。
        InvokePrivateInstanceMethod(controller, "OnSelectFormation");
        InvokePrivateInstanceMethod(controller, "LateUpdate");
        ForceMenuAtRest(controller.selectFormation.GetComponent<MenuDisplayAnim>());
        UnifiedGameFontController.RefreshSceneFontsForPreview();
        string formationPath = Path.Combine(Application.dataPath, "../Builds/Screenshots/war-select-formation.png");
        CaptureSceneCameraPng(scene, formationPath, captureWidth, captureHeight);
        Debug.Log("WAR SELECT FORMATION SCREENSHOT: " + Path.GetFullPath(formationPath));

        // 5. 返回主菜单后打开武将位置菜单，确认不会残留阵型菜单互相压住。
        controller.OnReturnMain();
        InvokePrivateInstanceMethod(controller, "OnGeneralPosition");
        InvokePrivateInstanceMethod(controller, "LateUpdate");
        ForceMenuAtRest(controller.generalPos.GetComponent<MenuDisplayAnim>());
        UnifiedGameFontController.RefreshSceneFontsForPreview();
        string positionPath = Path.Combine(Application.dataPath, "../Builds/Screenshots/war-select-position.png");
        CaptureSceneCameraPng(scene, positionPath, captureWidth, captureHeight);
        Debug.Log("WAR SELECT POSITION SCREENSHOT: " + Path.GetFullPath(positionPath));
    }

    /// <summary>
    /// 方法说明：渲染战斗开场对白截图，用于确认蓝色对白框内文字不再空白。
    /// 参数说明：无。
    /// 返回说明：无返回值；截图写入 Builds/Screenshots 目录。
    /// </summary>
    [MenuItem(MenuRoot + "截图战斗对白")]
    public static void CaptureWarDialogueRuntimeScreenshot()
    {
        const int captureWidth = 2048;
        const int captureHeight = 1152;

        // 1. 使用 MOD06 真实数据准备战斗双方武将。
        PrepareRecoveredMod06Data();
        FocusedBattleSample sample = BuildFocusedBattleSample();
        WarSceneController.leftGeneralIdx = sample.leftGeneral;
        WarSceneController.rightGeneralIdx = sample.rightGeneral;
        WarSceneController.leftDefense = 0;
        WarSceneController.rightDefense = 0;
        WarSceneController.rightGeneralPosition = 0;

        // 2. 打开战斗场景并直接触发战斗对白组件。
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/WarScene.unity", OpenSceneMode.Single);
        FindSceneCamera(scene).aspect = captureWidth / (float)captureHeight;
        WarSceneController controller = FindSceneComponent<WarSceneController>(scene);
        InvokePrivateInstanceMethod(controller.infoPanel, "Start");
        string text = ZhongWen.Instance.GetGeneralName(sample.leftGeneral) + "率军出战!";
        controller.dialog.SetDialogue(text, WarSceneController.WhichSide.Left);
        AdvanceWarDialogueForCapture(controller.dialog, text.Length);
        UnifiedGameFontController.RefreshSceneFontsForPreview();

        // 3. 从场景摄像机渲染当前战斗对白画面。
        string outputPath = Path.Combine(Application.dataPath, "../Builds/Screenshots/war-dialogue.png");
        CaptureSceneCameraStackPng(scene, outputPath, captureWidth, captureHeight);
        Debug.Log("WAR DIALOGUE SCREENSHOT: " + Path.GetFullPath(outputPath));
    }

    /// <summary>
    /// 方法说明：逐个打开当前 QA 关注的主流程按钮并截图，覆盖选择、战略、战前和战斗对白页面。
    /// 参数说明：无。
    /// 返回说明：无返回值；截图写入 Builds/Screenshots/full-button-qa-current 目录。
    /// </summary>
    [MenuItem(MenuRoot + "全按钮QA截图")]
    public static void CaptureFullButtonQaScreenshots()
    {
        const int captureWidth = 2048;
        const int captureHeight = 1152;
        string outputDirectory = GetFullButtonQaDirectory();
        Directory.CreateDirectory(outputDirectory);

        CaptureSelectButtonQaScreenshots(outputDirectory, captureWidth, captureHeight);
        CaptureStrategyButtonQaScreenshots(outputDirectory, captureWidth, captureHeight);
        CaptureWarSelectButtonQaScreenshots(outputDirectory, captureWidth, captureHeight);
        CaptureWarDialogueButtonQaScreenshot(outputDirectory, captureWidth, captureHeight);

        Debug.Log("FULL BUTTON QA SCREENSHOTS: " + Path.GetFullPath(outputDirectory));
    }

    /// <summary>
    /// 方法说明：截取剧本选择、MOD06 选择君主和确认框页面。
    /// 参数说明：outputDirectory 为输出目录，captureWidth 为截图宽度，captureHeight 为截图高度。
    /// 返回说明：无返回值。
    /// </summary>
    private static void CaptureSelectButtonQaScreenshots(string outputDirectory, int captureWidth, int captureHeight)
    {
        Scene modScene = EditorSceneManager.OpenScene("Assets/Scenes/SelectKing.unity", OpenSceneMode.Single);
        SelectKingSceneController modController = FindSelectKingSceneController(modScene);
        FindSceneCamera(modScene).aspect = captureWidth / (float)captureHeight;
        InvokePrivateControllerMethod(modController, "SetupMODButtons");
        InvokePrivateControllerMethod(modController, "SelectMODEnter");
        ForceMenuAtRest(modController.selectMOD.GetComponent<MenuDisplayAnim>());
        CaptureSceneCameraPng(modScene, Path.Combine(outputDirectory, "01-select-mod.png"), captureWidth, captureHeight);

        Scene kingScene = EditorSceneManager.OpenScene("Assets/Scenes/SelectKing.unity", OpenSceneMode.Single);
        SelectKingSceneController kingController = FindSelectKingSceneController(kingScene);
        FindSceneCamera(kingScene).aspect = captureWidth / (float)captureHeight;
        Controller.MODSelect = MODLoadController.RestoredSango2Index;
        InvokePrivateControllerMethod(kingController, "SelectKingEnter");
        ForceSelectKingMenuAtRest(kingController);
        InvokePrivateControllerMethod(kingController, "OnCancelButton");
        CaptureSceneCameraPng(kingScene, Path.Combine(outputDirectory, "02-select-king.png"), captureWidth, captureHeight);
        InvokePrivateControllerMethod(kingController, "OnKingNameSelect", new object[] { FindActiveKingWithCitiesAndGenerals() });
        ForceSelectKingMenuAtRest(kingController);
        CaptureSceneCameraPng(kingScene, Path.Combine(outputDirectory, "03-select-king-confirm.png"), captureWidth, captureHeight);
    }

    /// <summary>
    /// 方法说明：逐个打开战略地图 HUD 和主菜单四个按钮状态并截图。
    /// 参数说明：outputDirectory 为输出目录，captureWidth 为截图宽度，captureHeight 为截图高度。
    /// 返回说明：无返回值。
    /// </summary>
    private static void CaptureStrategyButtonQaScreenshots(string outputDirectory, int captureWidth, int captureHeight)
    {
        StrategyController strategyController;
        Scene mapScene = PrepareStrategyQaScene(captureWidth, captureHeight, out strategyController);
        CaptureSceneCameraPng(mapScene, Path.Combine(outputDirectory, "04-strategy-map.png"), captureWidth, captureHeight);
		CaptureStrategyMapDragQaScreenshots(outputDirectory, captureWidth, captureHeight);

        Scene capitalScene = PrepareStrategyQaScene(captureWidth, captureHeight, out strategyController);
        strategyController.FocusPlayerCapitalFromHud();
        CaptureSceneCameraPng(capitalScene, Path.Combine(outputDirectory, "05-strategy-hud-capital.png"), captureWidth, captureHeight);

        Scene zoomScene = PrepareStrategyQaScene(captureWidth, captureHeight, out strategyController);
        strategyController.ZoomStrategyMapFromHud(0.82f);
        CaptureSceneCameraPng(zoomScene, Path.Combine(outputDirectory, "06-strategy-hud-zoom-in.png"), captureWidth, captureHeight);

        Scene powerHudScene = PrepareStrategyQaScene(captureWidth, captureHeight, out strategyController);
        strategyController.OpenPowerMapFromHud();
        ForceStrategyPowerMapAtRest(FindSceneComponent<SyPowerMap>(powerHudScene));
        CaptureSceneCameraPng(powerHudScene, Path.Combine(outputDirectory, "07-strategy-hud-power.png"), captureWidth, captureHeight);

        Scene menuHudScene = PrepareStrategyQaScene(captureWidth, captureHeight, out strategyController);
        strategyController.ShowMainMenuFromHud();
        ForceStrategyMenuAtRest(strategyController);
		UnifiedGameFontController.RefreshSceneFontsForPreview();
        MainMenu visibleMainMenu = strategyController.mainMenuCtrl.GetComponent<MainMenu>();
        InvokePrivateInstanceMethod(visibleMainMenu, "LateUpdate");
        CaptureSceneCameraPng(menuHudScene, Path.Combine(outputDirectory, "08-strategy-hud-menu.png"), captureWidth, captureHeight);
        CaptureStrategyCityCommandQa(outputDirectory, captureWidth, captureHeight);

        CaptureStrategyMainMenuCommandQa(outputDirectory, captureWidth, captureHeight, 0, "09-strategy-menu-power.png");
        CaptureStrategyMainMenuCommandQa(outputDirectory, captureWidth, captureHeight, 1, "10-strategy-menu-find-general.png");
        CaptureStrategyMainMenuCommandQa(outputDirectory, captureWidth, captureHeight, 2, "11-strategy-menu-how-to-play.png");
        CaptureStrategyMainMenuCommandQa(outputDirectory, captureWidth, captureHeight, 3, "12-strategy-menu-return-confirm.png");
    }

    /// <summary>
    /// 方法说明：打开战略主菜单并点击指定按钮，然后截取结果页面。
    /// 参数说明：outputDirectory 为输出目录，captureWidth 为截图宽度，captureHeight 为截图高度，commandIndex 为按钮序号，fileName 为输出文件名。
    /// 返回说明：无返回值。
    /// </summary>
    private static void CaptureStrategyMainMenuCommandQa(string outputDirectory, int captureWidth, int captureHeight, int commandIndex, string fileName)
    {
        StrategyController strategyController;
        Scene scene = PrepareStrategyQaScene(captureWidth, captureHeight, out strategyController);
        strategyController.ShowMainMenuFromHud();

        MainMenu mainMenu = strategyController.mainMenuCtrl.GetComponent<MainMenu>();
        if (commandIndex >= 0 && commandIndex < mainMenu.commands.Length)
        {
            mainMenu.commands[commandIndex].SetButtonState(Button.ButtonState.Clicked);
            mainMenu.ProcessClickedCommandButtons();
            Debug.Log("FULL BUTTON QA CLICKED: strategy-main-menu index=" + commandIndex);
        }
        ForceStrategyMenuAtRest(strategyController);
        HideStrategyLegacyHistory(strategyController);

        if (commandIndex == 0)
        {
            ForceStrategyPowerMapAtRest(FindSceneComponent<SyPowerMap>(scene));
        }
        else if (commandIndex == 1)
        {
            FindGeneralController findGeneral = FindSceneComponent<FindGeneralController>(scene);
            InvokePrivateInstanceMethod(findGeneral, "Start");
            InvokePrivateInstanceMethod(findGeneral, "OnEnable");
            ForceAllMenuAnimationsAtRest(findGeneral.gameObject);
            if (findGeneral.generalsList != null && findGeneral.generalsList.GetCount() > 0)
            {
                findGeneral.generalsList.SetItemSelected(0, true);
                ForceAllMenuAnimationsAtRest(findGeneral.gameObject);
                if (findGeneral.confirmBox != null)
                {
                    findGeneral.confirmBox.SetActive(false);
                }
            }
        }
        else if (commandIndex == 2)
        {
            SyHowToPlayController helpController = FindSceneComponent<SyHowToPlayController>(scene);
            InvokePrivateInstanceMethod(helpController, "EnsureHelpContent");
            ForceAllMenuAnimationsAtRest(helpController.gameObject);
        }

        UnifiedGameFontController.RefreshSceneFontsForPreview();
        CaptureSceneCameraPng(scene, Path.Combine(outputDirectory, fileName), captureWidth, captureHeight);
    }

    /// <summary>
    /// 方法说明：打开玩家城池的城内指令面板并截图，验证城名旗帜不会穿透半透明指令面板。
    /// 参数说明：outputDirectory 为输出目录，captureWidth 为截图宽度，captureHeight 为截图高度。
    /// 返回说明：无返回值。
    /// </summary>
    private static void CaptureStrategyCityCommandQa(string outputDirectory, int captureWidth, int captureHeight)
    {
        // 1. 准备战略地图并定位玩家第一个有效城池。
        StrategyController strategyController;
        Scene scene = PrepareStrategyQaScene(captureWidth, captureHeight, out strategyController);
        int cityIndex = FindFirstPlayerCityIndex();
        Camera camera = FindSceneCamera(scene);
        camera.transform.position = StrategyController.ClampCameraPosition(Informations.Instance.GetCityWorldPosition(cityIndex));

        // 2. 进入暂停态并持续隐藏地图标注，模拟真实打开城内指令时的层级状态。
        StrategyController.state = StrategyController.State.Pause;
        if (strategyController.flagsCtrl != null)
        {
            strategyController.flagsCtrl.SetFlagsAnimPause();
            strategyController.flagsCtrl.KeepPausedMapAnnotationsHidden();
        }
        HideStrategyLegacyHistory(strategyController);

        // 3. 打开城内指令控制器，并把所有滑入动画停在最终位置。
        CityCommandsController cityCommands = FindSceneComponent<CityCommandsController>(scene);
        InvokePrivateInstanceMethod(cityCommands, "Start");
        cityCommands.SetCity(cityIndex);
        InvokePrivateInstanceMethod(cityCommands, "OnEnable");
        ForceAllMenuAnimationsAtRest(cityCommands.gameObject);
        InvokePrivateInstanceMethod(cityCommands, "Update");

        // 4. 刷新字体并截图，输出给视觉验收使用。
        UnifiedGameFontController.RefreshSceneFontsForPreview();
        CaptureSceneCameraPng(scene, Path.Combine(outputDirectory, "08b-strategy-city-command.png"), captureWidth, captureHeight);
    }

    /// <summary>
    /// 方法说明：逐个打开战前菜单五个按钮状态并截图。
    /// 参数说明：outputDirectory 为输出目录，captureWidth 为截图宽度，captureHeight 为截图高度。
    /// 返回说明：无返回值。
    /// </summary>
    private static void CaptureWarSelectButtonQaScreenshots(string outputDirectory, int captureWidth, int captureHeight)
    {
        SelectGeneralToWarController controller;
        Scene mainScene = PrepareWarSelectQaScene(captureWidth, captureHeight, out controller);
        CaptureSceneCameraPng(mainScene, Path.Combine(outputDirectory, "13-war-select-main.png"), captureWidth, captureHeight);

        Scene formationScene = PrepareWarSelectQaScene(captureWidth, captureHeight, out controller);
        InvokePrivateInstanceMethod(controller, "OnSelectFormation");
        ForceAllMenuAnimationsAtRest(controller.gameObject);
		UnifiedGameFontController.RefreshSceneFontsForPreview();
		InvokePrivateInstanceMethod(controller.selectFormation, "LateUpdate");
        CaptureSceneCameraPng(formationScene, Path.Combine(outputDirectory, "14-war-select-formation.png"), captureWidth, captureHeight);

        Scene positionScene = PrepareWarSelectQaScene(captureWidth, captureHeight, out controller);
        InvokePrivateInstanceMethod(controller, "OnGeneralPosition");
        ForceAllMenuAnimationsAtRest(controller.gameObject);
		UnifiedGameFontController.RefreshSceneFontsForPreview();
		InvokePrivateInstanceMethod(controller.generalPos.GetComponent<SGGeneralPosition>(), "LateUpdate");
        CaptureSceneCameraPng(positionScene, Path.Combine(outputDirectory, "15-war-select-position.png"), captureWidth, captureHeight);

        Scene infoListScene = PrepareWarSelectQaScene(captureWidth, captureHeight, out controller);
        InvokePrivateInstanceMethod(controller, "OnGeneralInformation");
        ForceAllMenuAnimationsAtRest(controller.gameObject);
		UnifiedGameFontController.RefreshSceneFontsForPreview();
        CaptureSceneCameraPng(infoListScene, Path.Combine(outputDirectory, "16-war-select-info-list.png"), captureWidth, captureHeight);

        if (controller.generalsInfo != null && controller.generalsInfo.generalsList != null && controller.generalsInfo.generalsList.GetCount() > 0)
        {
            controller.generalsInfo.generalsList.SetItemSelected(0, true);
            InvokePrivateInstanceMethod(controller.generalsInfo, "OnSelectGeneral");
            ForceWarGeneralInformationAtRest(controller.generalsInfo);
            CaptureSceneCameraPng(infoListScene, Path.Combine(outputDirectory, "17-war-select-info-detail.png"), captureWidth, captureHeight);
        }

        Scene retreatScene = PrepareWarSelectQaScene(captureWidth, captureHeight, out controller);
        InvokePrivateInstanceMethod(controller, "OnEscape");
        ForceAllMenuAnimationsAtRest(controller.gameObject);
		UnifiedGameFontController.RefreshSceneFontsForPreview();
		InvokePrivateInstanceMethod(controller.retreatConfirm.GetComponent<SGRetreat>(), "LateUpdate");
        CaptureSceneCameraPng(retreatScene, Path.Combine(outputDirectory, "18-war-select-retreat.png"), captureWidth, captureHeight);

        Scene toWarScene = PrepareWarSelectQaScene(captureWidth, captureHeight, out controller);
        InvokePrivateInstanceMethod(controller, "OnToWar");
        Debug.Log("FULL BUTTON QA CLICKED: war-select 出战 -> WarScene load requested");
    }

    /// <summary>
    /// 方法说明：截取战斗对白页面，覆盖战前出战后的战斗文字框状态。
    /// 参数说明：outputDirectory 为输出目录，captureWidth 为截图宽度，captureHeight 为截图高度。
    /// 返回说明：无返回值。
    /// </summary>
    private static void CaptureWarDialogueButtonQaScreenshot(string outputDirectory, int captureWidth, int captureHeight)
    {
        PrepareRecoveredMod06Data();
        FocusedBattleSample sample = BuildFocusedBattleSample();
        WarSceneController.leftGeneralIdx = sample.leftGeneral;
        WarSceneController.rightGeneralIdx = sample.rightGeneral;
        WarSceneController.leftDefense = 0;
        WarSceneController.rightDefense = 0;
        WarSceneController.rightGeneralPosition = 0;

        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/WarScene.unity", OpenSceneMode.Single);
        FindSceneCamera(scene).aspect = captureWidth / (float)captureHeight;
        WarSceneController controller = FindSceneComponent<WarSceneController>(scene);
        InvokePrivateInstanceMethod(controller.infoPanel, "Start");
        string text = ZhongWen.Instance.GetGeneralName(sample.leftGeneral) + "率军出战!";
        controller.dialog.SetDialogue(text, WarSceneController.WhichSide.Left);
        AdvanceWarDialogueForCapture(controller.dialog, text.Length);
        UnifiedGameFontController.RefreshSceneFontsForPreview();
		InvokePrivateInstanceMethod(controller.dialog, "LateUpdate");
        CaptureSceneCameraStackPng(scene, Path.Combine(outputDirectory, "19-war-dialogue.png"), captureWidth, captureHeight);
    }

	/// <summary>
	/// 方法说明：调用战略地图实际拖动位移方法并生成拖动前后截图，验证相机位置和城池标注会随地图移动。
	/// 参数说明：outputDirectory 为输出目录，captureWidth 为截图宽度，captureHeight 为截图高度。
	/// 返回说明：无返回值；相机没有移动时抛出异常使 QA 失败。
	/// </summary>
	private static void CaptureStrategyMapDragQaScreenshots(string outputDirectory, int captureWidth, int captureHeight)
	{
		StrategyController strategyController;
		Scene dragScene = PrepareStrategyQaScene(captureWidth, captureHeight, out strategyController);
		Camera camera = FindSceneCamera(dragScene);
		Vector3 startPosition = StrategyController.ClampCameraPosition(Vector3.zero);
		camera.transform.position = startPosition;
		CaptureSceneCameraPng(dragScene, Path.Combine(outputDirectory, "04a-strategy-map-drag-before.png"), captureWidth, captureHeight);

		Vector3[] dragOffsets = new Vector3[] {
			new Vector3(90f, 60f, 0f),
			new Vector3(-90f, 0f, 0f),
			new Vector3(0f, 90f, 0f),
			new Vector3(0f, -90f, 0f)
		};
		string[] dragFileNames = new string[] {
			"04b-strategy-map-drag-right-down.png",
			"04c-strategy-map-drag-left.png",
			"04d-strategy-map-drag-up.png",
			"04e-strategy-map-drag-down.png"
		};

		for (int i = 0; i < dragOffsets.Length; i++)
		{
			camera.transform.position = startPosition;
			InvokePrivateInstanceMethod(strategyController, "ApplyCameraDragOffset", new object[] { dragOffsets[i] });
			Vector3 endPosition = camera.transform.position;
			Debug.Log("FULL BUTTON QA DRAG DETAIL: size=" + camera.orthographicSize
				+ " aspect=" + camera.aspect
				+ " offset=" + dragOffsets[i]
				+ " start=" + startPosition
				+ " end=" + endPosition);
			if (Vector2.Distance(new Vector2(startPosition.x, startPosition.y), new Vector2(endPosition.x, endPosition.y)) <= 1f)
			{
				throw new InvalidOperationException("战略地图拖动 QA 失败：相机位置未变化 offset=" + dragOffsets[i]);
			}

			CaptureSceneCameraPng(dragScene, Path.Combine(outputDirectory, dragFileNames[i]), captureWidth, captureHeight);
		}

		Debug.Log("FULL BUTTON QA DRAG PASS: start=" + startPosition + " directions=" + dragOffsets.Length);
	}

    /// <summary>
    /// 方法说明：准备战略场景 QA 状态，初始化 MOD06 地图、旗帜和菜单层级。
    /// 参数说明：captureWidth 为截图宽度，captureHeight 为截图高度，strategyController 输出战略控制器。
    /// 返回说明：返回已打开的战略场景。
    /// </summary>
    private static Scene PrepareStrategyQaScene(int captureWidth, int captureHeight, out StrategyController strategyController)
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Strategy.unity", OpenSceneMode.Single);
        FindSceneCamera(scene).aspect = captureWidth / (float)captureHeight;
        PrepareRecoveredMod06Data();
        strategyController = FindSceneComponent<StrategyController>(scene);
        InvokePrivateInstanceMethod(strategyController, "ApplyRestoredSango2StrategyMap");
        InvokePrivateInstanceMethod(strategyController, "EnsureStrategyMapHudController");
        HideStrategyMainMenu(strategyController, null);
        if (strategyController.flagsCtrl != null)
        {
            InvokePrivateInstanceMethod(strategyController.flagsCtrl, "Start");
            strategyController.flagsCtrl.SetFlagsAnimResume();
        }

        StrategyController.state = StrategyController.State.Normal;
        StrategyMapHudController mapHudController = strategyController.GetComponent<StrategyMapHudController>();
        if (mapHudController != null)
        {
            mapHudController.RefreshForCameraCapture();
        }
        UnifiedGameFontController.RefreshSceneFontsForPreview();
        return scene;
    }

    /// <summary>
    /// 方法说明：准备战前选择场景 QA 状态，初始化 MOD06 双方军队并执行场景 Start。
    /// 参数说明：captureWidth 为截图宽度，captureHeight 为截图高度，controller 输出战前控制器。
    /// 返回说明：返回已打开的战前场景。
    /// </summary>
    private static Scene PrepareWarSelectQaScene(int captureWidth, int captureHeight, out SelectGeneralToWarController controller)
    {
        PrepareRecoveredMod06Data();
        BuildFocusedBattleSample();
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SelectGeneralToWar.unity", OpenSceneMode.Single);
        FindSceneCamera(scene).aspect = captureWidth / (float)captureHeight;
        controller = FindSceneComponent<SelectGeneralToWarController>(scene);
        InvokePrivateInstanceMethod(controller, "Start");
        UnifiedGameFontController.RefreshSceneFontsForPreview();
        return scene;
    }

    /// <summary>
    /// 方法说明：把战略主菜单和所有子菜单动画强制停到最终位置。
    /// 参数说明：strategyController 为战略控制器。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ForceStrategyMenuAtRest(StrategyController strategyController)
    {
        if (strategyController == null || strategyController.mainMenuCtrl == null) return;

        ForceAllMenuAnimationsAtRest(strategyController.mainMenuCtrl);
        MainMenu mainMenu = strategyController.mainMenuCtrl.GetComponent<MainMenu>();
        if (mainMenu == null || mainMenu.commandAct == null) return;

        for (int i = 0; i < mainMenu.commandAct.Length; i++)
        {
            ForceAllMenuAnimationsAtRest(mainMenu.commandAct[i]);
        }
    }

    /// <summary>
    /// 方法说明：把战略势力地图的列表、地图和信息栏停到最终位置。
    /// 参数说明：powerMap 为势力地图控制器。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ForceStrategyPowerMapAtRest(SyPowerMap powerMap)
    {
        if (powerMap == null) return;

        Controller.kingIndex = FindActiveKingWithCitiesAndGenerals();
        powerMap.kingList.Clear();
        InvokePrivateInstanceMethod(powerMap, "OnEnable");
        int listIndex = FindPowerMapKingListIndex(powerMap, Controller.kingIndex);
        if (listIndex >= 0)
        {
            powerMap.kingList.SetItemSelected(listIndex, true);
        }
        powerMap.kingInfo.SetKing(Controller.kingIndex);
        HideStrategyLegacyHistoryForPowerMap(powerMap);
        ForceMenuAtRest(powerMap.kingList.GetComponent<MenuDisplayAnim>());
        ForceMenuAtRest(powerMap.map.GetComponent<MenuDisplayAnim>());
        ForceMenuAtRest(powerMap.kingInfo.GetComponent<MenuDisplayAnim>());
        UnifiedGameFontController.RefreshSceneFontsForPreview();
    }

    /// <summary>
    /// 方法说明：查找势力地图列表中指定君主所在行。
    /// 参数说明：powerMap 为势力地图控制器，kingIndex 为君主索引。
    /// 返回说明：找到返回列表行号，找不到返回 -1。
    /// </summary>
    private static int FindPowerMapKingListIndex(SyPowerMap powerMap, int kingIndex)
    {
        if (powerMap == null || powerMap.kingList == null) return -1;

        for (int i = 0; i < powerMap.kingList.GetCount(); i++)
        {
            ListItem item = powerMap.kingList.GetListItem(i);
            if (item == null || item.GetItemData() == null) continue;
            if ((int)item.GetItemData() == kingIndex)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 方法说明：势力图截图时隐藏旧年月框，避免旧时间框压住势力图标题。
    /// 参数说明：powerMap 为势力地图控制器。
    /// 返回说明：无返回值。
    /// </summary>
    private static void HideStrategyLegacyHistoryForPowerMap(SyPowerMap powerMap)
    {
        if (powerMap == null || powerMap.strCtrl == null || powerMap.strCtrl.hTimeCtrl == null) return;

        HideStrategyLegacyHistory(powerMap.strCtrl);
    }

    /// <summary>
    /// 方法说明：截图战略二级页面时隐藏仍在退场动画首帧的旧年月框，避免它压住页面标题。
    /// 参数说明：strategyController 为当前战略控制器。
    /// 返回说明：无返回值。
    /// </summary>
    private static void HideStrategyLegacyHistory(StrategyController strategyController)
    {
        if (strategyController == null || strategyController.hTimeCtrl == null) return;

        Renderer[] renderers = strategyController.hTimeCtrl.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = false;
            }
        }
    }

    /// <summary>
    /// 方法说明：把战前武将资料详情面板切到详情状态并停止所有动画。
    /// 参数说明：generalsInfo 为武将资料控制器。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ForceWarGeneralInformationAtRest(SGGeneralsInfoController generalsInfo)
    {
        if (generalsInfo == null) return;

        InvokePrivateInstanceMethod(generalsInfo, "SetGeneralsListVisible", new object[] { false });
        if (generalsInfo.generalsList != null)
        {
            generalsInfo.generalsList.gameObject.SetActive(false);
            if (generalsInfo.generalsList.slider != null && generalsInfo.generalsList.slider.parent != null)
            {
                generalsInfo.generalsList.slider.parent.gameObject.SetActive(false);
            }
        }
        MapController[] mapBackgrounds = generalsInfo.GetComponentsInChildren<MapController>(true);
        for (int i = 0; i < mapBackgrounds.Length; i++)
        {
            mapBackgrounds[i].gameObject.SetActive(false);
        }
        if (generalsInfo.information != null)
        {
            generalsInfo.information.SetActive(true);
        }
        ForceAllMenuAnimationsAtRest(generalsInfo.gameObject);
        UnifiedGameFontController.RefreshSceneFontsForPreview();
    }

    /// <summary>
    /// 方法说明：递归把对象下所有菜单动画停到原始位置。
    /// 参数说明：root 为待处理对象。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ForceAllMenuAnimationsAtRest(GameObject root)
    {
        if (root == null) return;

        MenuDisplayAnim[] menuAnimations = root.GetComponentsInChildren<MenuDisplayAnim>(true);
        for (int i = 0; i < menuAnimations.Length; i++)
        {
            ForceMenuAtRest(menuAnimations[i], false);
        }
    }

    /// <summary>
    /// 方法说明：返回全按钮 QA 截图输出目录。
    /// 参数说明：无。
    /// 返回说明：返回绝对路径。
    /// </summary>
    private static string GetFullButtonQaDirectory()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Screenshots/full-button-qa-current"));
    }

    /// <summary>
    /// 方法说明：在编辑器截图中推进战斗对白打字机若干帧，让截图能看到完整文本而不是首帧。
    /// 参数说明：dialog 为战斗对白组件，characterCount 为目标推进字符数。
    /// 返回说明：无返回值。
    /// </summary>
    private static void AdvanceWarDialogueForCapture(WSDialogue dialog, int characterCount)
    {
        if (dialog == null) return;

        int count = Mathf.Max(1, characterCount);
        for (int i = 0; i < count; i++)
        {
            System.Threading.Thread.Sleep(60);
            InvokePrivateInstanceMethod(dialog, "Update");
        }
    }

    /// <summary>
    /// 方法说明：抽样输出关键头像 prefab 的 exSprite 贴图和材质贴图路径。
    /// 参数说明：无。
    /// 返回说明：无返回值；结果输出到 Unity Console。
    /// </summary>
    [MenuItem(MenuRoot + "检查头像Prefab材质")]
    public static void DumpRecoveredHeadPrefabMaterials()
    {
        int[] headIndexes = new int[] { 270, 591, 618, 729 };
        for (int i = 0; i < headIndexes.Length; i++)
        {
            DumpRecoveredHeadPrefabMaterial(headIndexes[i]);
        }
    }

    /// <summary>
    /// 方法说明：输出单个头像 prefab 的贴图引用。
    /// 参数说明：headIndex 为头像序号。
    /// 返回说明：无返回值；资源缺失时抛出异常。
    /// </summary>
    private static void DumpRecoveredHeadPrefabMaterial(int headIndex)
    {
        string prefabPath = HeadPrefabDirectory + "/Head" + headIndex.ToString("D3") + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            throw new FileNotFoundException("头像 prefab 不存在", prefabPath);
        }

        exSprite sprite = prefab.GetComponent<exSprite>();
        Renderer renderer = prefab.GetComponent<Renderer>();
        string spriteTexturePath = "";
        string materialTexturePath = "";
        if (sprite != null)
        {
            string textureGuid = GetField<string>(sprite, "textureGUID");
            spriteTexturePath = string.IsNullOrEmpty(textureGuid) ? "" : AssetDatabase.GUIDToAssetPath(textureGuid);
        }

        if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.mainTexture != null)
        {
            materialTexturePath = AssetDatabase.GetAssetPath(renderer.sharedMaterial.mainTexture);
        }

        Debug.Log("HEAD PREFAB MATERIAL: Head" + headIndex.ToString("D3") + " spriteTexture=" + spriteTexturePath + " materialTexture=" + materialTexturePath);
    }

    /// <summary>
    /// 方法说明：初始化副本选择运行时界面，并输出可见副本文字。
    /// 参数说明：无。
    /// 返回说明：无返回值。
    /// </summary>
    private static void DumpRuntimeModScreen()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SelectKing.unity", OpenSceneMode.Single);
        SelectKingSceneController controller = FindSelectKingSceneController(scene);
        InvokePrivateControllerMethod(controller, "SetupMODButtons");
        InvokePrivateControllerMethod(controller, "SelectMODEnter");

        List<string> labels = CollectFontTexts(controller.selectMOD);
        Debug.Log("SELECT RUNTIME MOD COUNT: " + labels.Count + " labels=" + string.Join("|", labels.ToArray()));
    }

    /// <summary>
    /// 方法说明：初始化威力加强版势力选择运行时界面，并输出势力按钮、头像和滚动控件数量。
    /// 参数说明：无。
    /// 返回说明：无返回值。
    /// </summary>
    private static void DumpRuntimeKingScreen()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SelectKing.unity", OpenSceneMode.Single);
        SelectKingSceneController controller = FindSelectKingSceneController(scene);
        Controller.MODSelect = 5;
        InvokePrivateControllerMethod(controller, "SelectKingEnter");

        List<string> labels = CollectFontTexts(controller.kingListRoot);
        int headCount = CountSpritesByTexturePrefix(controller.kingListRoot, "Assets/Graph/Sango2Recovered/Face/Head");
        Debug.Log("SELECT RUNTIME KING LABEL COUNT: " + labels.Count + " labels=" + string.Join("|", labels.ToArray()));
        Debug.Log("SELECT RUNTIME KING HEAD COUNT: " + headCount);
    }

    /// <summary>
    /// 方法说明：把选择君主界面的菜单动画对象放回原始位置。
    /// 参数说明：controller 为选择君主场景控制器。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ForceSelectKingMenuAtRest(SelectKingSceneController controller)
    {
        ForceMenuAtRest(controller.infoAnim);
        ForceMenuAtRest(controller.mapAnim);
        ForceMenuAtRest(controller.menuAnim);
    }

    /// <summary>
    /// 方法说明：把单个菜单动画对象放回原始位置。
    /// 参数说明：menuAnim 为菜单动画组件。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ForceMenuAtRest(MenuDisplayAnim menuAnim)
    {
        ForceMenuAtRest(menuAnim, true);
    }

    /// <summary>
    /// 方法说明：把单个菜单动画对象放回原始位置，可选择是否强制激活对象。
    /// 参数说明：menuAnim 为菜单动画组件，activate 为 true 时会打开对象，为 false 时只定格当前位置。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ForceMenuAtRest(MenuDisplayAnim menuAnim, bool activate)
    {
        if (menuAnim == null) return;

        if (activate)
        {
            menuAnim.gameObject.SetActive(true);
        }
        menuAnim.transform.localPosition = menuAnim.GetOriginalPosition();
    }

    /// <summary>
    /// 方法说明：从场景摄像机渲染 PNG 截图。
    /// 参数说明：scene 为目标场景，outputPath 为输出路径，width 为截图宽度，height 为截图高度。
    /// 返回说明：无返回值；缺少摄像机时抛出异常。
    /// </summary>
    private static void CaptureSceneCameraPng(Scene scene, string outputPath, int width, int height)
    {
        Camera camera = FindSceneCamera(scene);
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect;

        try
        {
            camera.aspect = width / (float)height;
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    /// <summary>
    /// 方法说明：按场景中启用摄像机的 depth 顺序叠加渲染 PNG 截图，适用于战斗场景多摄像机画面。
    /// 参数说明：scene 为目标场景，outputPath 为输出路径，width 为截图宽度，height 为截图高度。
    /// 返回说明：无返回值；缺少可渲染摄像机时抛出异常。
    /// </summary>
    private static void CaptureSceneCameraStackPng(Scene scene, string outputPath, int width, int height)
    {
        List<Camera> cameras = FindSceneCameras(scene);
        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previousActive = RenderTexture.active;
        CameraClearFlags[] previousClearFlags = new CameraClearFlags[cameras.Count];
        RenderTexture[] previousTargets = new RenderTexture[cameras.Count];
        float[] previousAspects = new float[cameras.Count];

        try
        {
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.black);
            for (int i = 0; i < cameras.Count; i++)
            {
                Camera camera = cameras[i];
                previousClearFlags[i] = camera.clearFlags;
                previousTargets[i] = camera.targetTexture;
                previousAspects[i] = camera.aspect;

                if (i > 0 && camera.clearFlags == CameraClearFlags.SolidColor)
                {
                    camera.clearFlags = CameraClearFlags.Depth;
                }
                camera.aspect = width / (float)height;
                camera.targetTexture = renderTexture;
                camera.Render();
            }

            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            string directory = Path.GetDirectoryName(outputPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < cameras.Count; i++)
            {
                cameras[i].clearFlags = previousClearFlags[i];
                cameras[i].targetTexture = previousTargets[i];
                cameras[i].aspect = previousAspects[i];
            }
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    /// <summary>
    /// 方法说明：查找场景内用于截图的摄像机。
    /// 参数说明：scene 为目标场景。
    /// 返回说明：返回找到的摄像机；缺失时抛出异常。
    /// </summary>
    private static Camera FindSceneCamera(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Camera camera = root.GetComponentInChildren<Camera>(true);
            if (camera != null)
            {
                return camera;
            }
        }

        throw new InvalidOperationException("选择界面场景缺少摄像机：" + scene.path);
    }

    /// <summary>
    /// 方法说明：查找场景中所有启用的摄像机，并按 depth 从低到高排序。
    /// 参数说明：scene 为目标场景。
    /// 返回说明：返回摄像机列表；缺失时抛出异常。
    /// </summary>
    private static List<Camera> FindSceneCameras(Scene scene)
    {
        List<Camera> cameras = new List<Camera>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Camera[] rootCameras = root.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < rootCameras.Length; i++)
            {
                Camera camera = rootCameras[i];
                if (camera != null && camera.enabled && camera.gameObject.activeInHierarchy)
                {
                    cameras.Add(camera);
                }
            }
        }

        if (cameras.Count == 0)
        {
            throw new InvalidOperationException("场景缺少可渲染摄像机：" + scene.path);
        }

        cameras.Sort((left, right) => left.depth.CompareTo(right.depth));
        for (int i = 0; i < cameras.Count; i++)
        {
            Debug.Log("CAPTURE CAMERA STACK: " + BuildTransformPath(cameras[i].transform) +
                      " depth=" + cameras[i].depth +
                      " clear=" + cameras[i].clearFlags +
                      " culling=" + cameras[i].cullingMask);
        }

        return cameras;
    }

    /// <summary>
    /// 方法说明：查找选择君主场景控制器。
    /// 参数说明：scene 为已打开的选择场景。
    /// 返回说明：找到返回 SelectKingSceneController，缺失时抛出异常。
    /// </summary>
    private static SelectKingSceneController FindSelectKingSceneController(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            SelectKingSceneController controller = root.GetComponentInChildren<SelectKingSceneController>(true);
            if (controller != null)
            {
                return controller;
            }
        }

        throw new InvalidOperationException("选择界面控制器不存在：" + scene.path);
    }

    /// <summary>
    /// 方法说明：从指定场景根节点查找组件，包含未激活对象。
    /// 参数说明：scene 为目标场景。
    /// 返回说明：找到返回组件，缺失时抛出异常。
    /// </summary>
    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null)
            {
                return component;
            }
        }

        throw new InvalidOperationException("场景缺少组件：" + typeof(T).Name + " scene=" + scene.path);
    }

    /// <summary>
    /// 方法说明：隐藏战略旧主菜单和非目标子菜单，避免截图或运行中出现多层菜单叠加。
    /// 参数说明：strategyController 为战略控制器，activePanel 为需要保留显示的子面板。
    /// 返回说明：无返回值。
    /// </summary>
    private static void HideStrategyMainMenu(StrategyController strategyController, GameObject activePanel)
    {
        if (strategyController == null || strategyController.mainMenuCtrl == null) return;

        MainMenu mainMenu = strategyController.mainMenuCtrl.GetComponent<MainMenu>();
        if (mainMenu != null && mainMenu.commandAct != null)
        {
            for (int i = 0; i < mainMenu.commandAct.Length; i++)
            {
                GameObject panel = mainMenu.commandAct[i];
                if (panel != null && panel != activePanel)
                {
                    panel.SetActive(false);
                }
            }
        }

        strategyController.mainMenuCtrl.SetActive(false);
    }

    /// <summary>
    /// 方法说明：准备 MOD06 运行时数据，供编辑器截图入口复用。
    /// 参数说明：无。
    /// 返回说明：无返回值；MOD 加载失败时抛出异常。
    /// </summary>
    private static void PrepareRecoveredMod06Data()
    {
        Informations.Reset();
        StrategyController.Reset();
        Controller.MODSelect = MODLoadController.RestoredSango2Index;
        Controller.kingIndex = 0;
        if (!MODLoadController.Instance.LoadMOD(MODLoadController.RestoredSango2Index))
        {
            throw new InvalidOperationException("MOD06 加载失败，无法准备截图数据。");
        }
        Controller.kingIndex = FindActiveKingWithCitiesAndGenerals();
    }

    /// <summary>
    /// 方法说明：查找一个有城池且有武将的活跃势力，避免 QA 截图选到空势力导致信息栏全 0。
    /// 参数说明：无。
    /// 返回说明：返回有效势力索引，找不到时返回 0。
    /// </summary>
    private static int FindActiveKingWithCitiesAndGenerals()
    {
        for (int i = 0; i < Informations.Instance.kingNum; i++)
        {
            KingInfo kingInfo = Informations.Instance.GetKingInfo(i);
            if (kingInfo == null || kingInfo.active != 1) continue;
            if (kingInfo.cities == null || kingInfo.cities.Count == 0) continue;
            if (kingInfo.generals == null || kingInfo.generals.Count == 0) continue;
            return i;
        }

        return 0;
    }

    /// <summary>
    /// 方法说明：查找当前玩家势力第一个有效城池，用于战略城内指令截图。
    /// 参数说明：无。
    /// 返回说明：返回城池索引；找不到时抛出异常。
    /// </summary>
    private static int FindFirstPlayerCityIndex()
    {
        KingInfo kingInfo = Informations.Instance.GetKingInfo(Controller.kingIndex);
        if (kingInfo != null && kingInfo.cities != null)
        {
            for (int i = 0; i < kingInfo.cities.Count; i++)
            {
                int cityIndex = kingInfo.cities[i];
                if (cityIndex >= 0 && cityIndex < Informations.Instance.cityNum)
                {
                    return cityIndex;
                }
            }
        }

        throw new InvalidOperationException("当前玩家势力没有可用于城内指令截图的城池。");
    }

    /// <summary>
    /// 方法说明：从 MOD06 真实势力中构建一组战前/战斗截图用军队数据。
    /// 参数说明：无。
    /// 返回说明：返回左右双方主将和军队信息。
    /// </summary>
    private static FocusedBattleSample BuildFocusedBattleSample()
    {
        int mineKing = FindKingWithGenerals(0, -1, 2);
        int enemyKing = FindKingWithGenerals(0, mineKing, 2);
        ArmyInfo mine = BuildFocusedArmy(mineKing, enemyKing);
        ArmyInfo enemy = BuildFocusedArmy(enemyKing, mineKing);

        SelectGeneralToWarController.mode = 0;
        SelectGeneralToWarController.mine = mine;
        SelectGeneralToWarController.enemy = enemy;
        SelectGeneralToWarController.isWarBegin = true;
        SelectGeneralToWarController.warResult = 2;

        // 截图时把玩家君主设到不存在的势力，避免自动弹快速战斗确认框遮住战前菜单。
        Controller.kingIndex = -1000;

        return new FocusedBattleSample
        {
            leftGeneral = enemy.commander,
            rightGeneral = mine.commander,
            mine = mine,
            enemy = enemy
        };
    }

    /// <summary>
    /// 方法说明：查找一个拥有足够武将的有效势力。
    /// 参数说明：startIndex 为起始势力索引，excludeKing 为需要排除的势力索引，minGenerals 为最少武将数。
    /// 返回说明：返回势力索引；找不到时抛出异常。
    /// </summary>
    private static int FindKingWithGenerals(int startIndex, int excludeKing, int minGenerals)
    {
        for (int i = Mathf.Max(0, startIndex); i < Informations.Instance.kingNum; i++)
        {
            if (i == excludeKing) continue;

            KingInfo kingInfo = Informations.Instance.GetKingInfo(i);
            if (kingInfo == null || kingInfo.active != 1 || kingInfo.generals == null) continue;
            if (CountFocusedBattleReadyGenerals(kingInfo) >= minGenerals)
            {
                return i;
            }
        }

        throw new InvalidOperationException("MOD06 中找不到可用于截图的有效势力。");
    }

    /// <summary>
    /// 方法说明：用指定势力的现有武将构建截图用军队。
    /// 参数说明：kingIndex 为军队势力索引，targetKingIndex 为目标势力索引。
    /// 返回说明：返回军队信息。
    /// </summary>
    private static ArmyInfo BuildFocusedArmy(int kingIndex, int targetKingIndex)
    {
        KingInfo kingInfo = Informations.Instance.GetKingInfo(kingIndex);
        if (kingInfo == null || kingInfo.generals == null || kingInfo.generals.Count == 0)
        {
            throw new InvalidOperationException("势力缺少可出战武将：" + kingIndex);
        }

        ArmyInfo army = new ArmyInfo();
        army.king = kingIndex;
        army.cityFrom = PickFirstCity(kingIndex);
        army.cityTo = PickFirstCity(targetKingIndex);
        for (int i = 0; i < kingInfo.generals.Count && army.generals.Count < 3; i++)
        {
            int generalIndex = kingInfo.generals[i];
            if (!IsFocusedGeneralBattleReady(generalIndex)) continue;

            army.generals.Add(generalIndex);
        }
        if (army.generals.Count == 0)
        {
            throw new InvalidOperationException("势力缺少字段完整的出战武将：" + kingIndex);
        }

        army.commander = army.generals[0];
        return army;
    }

    /// <summary>
    /// 方法说明：读取势力的第一座城市，作为截图用军队出发或目标城市。
    /// 参数说明：kingIndex 为势力索引。
    /// 返回说明：有城市返回第一座城市索引，否则返回 0。
    /// </summary>
    private static int PickFirstCity(int kingIndex)
    {
        KingInfo kingInfo = Informations.Instance.GetKingInfo(kingIndex);
        if (kingInfo != null && kingInfo.cities != null && kingInfo.cities.Count > 0)
        {
            return kingInfo.cities[0];
        }

        return 0;
    }

    /// <summary>
    /// 方法说明：统计势力中字段完整、可用于截图战斗页面的武将数量。
    /// 参数说明：kingInfo 为势力信息。
    /// 返回说明：返回可用武将数量。
    /// </summary>
    private static int CountFocusedBattleReadyGenerals(KingInfo kingInfo)
    {
        if (kingInfo == null || kingInfo.generals == null) return 0;

        int count = 0;
        for (int i = 0; i < kingInfo.generals.Count; i++)
        {
            if (IsFocusedGeneralBattleReady(kingInfo.generals[i]))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 方法说明：判断武将是否具备战前和战斗截图所需的真实字段。
    /// 参数说明：generalIndex 为武将索引。
    /// 返回说明：字段完整返回 true，否则返回 false。
    /// </summary>
    private static bool IsFocusedGeneralBattleReady(int generalIndex)
    {
        GeneralInfo generalInfo = Informations.Instance.GetGeneralInfo(generalIndex);
        if (generalInfo == null)
        {
            return false;
        }

        return generalInfo.active == 1 &&
               generalInfo.healthMax > 0 &&
               generalInfo.healthCur > 0 &&
               generalInfo.formation != 0 &&
               generalInfo.formationCur != 0;
    }

    private struct FocusedBattleSample
    {
        public int leftGeneral;
        public int rightGeneral;
        public ArmyInfo mine;
        public ArmyInfo enemy;
    }

    /// <summary>
    /// 方法说明：反射调用选择界面控制器私有方法。
    /// 参数说明：controller 为目标控制器，methodName 为方法名。
    /// 返回说明：无返回值；方法缺失时抛出异常。
    /// </summary>
    private static void InvokePrivateControllerMethod(SelectKingSceneController controller, string methodName)
    {
        InvokePrivateControllerMethod(controller, methodName, null);
    }

    /// <summary>
    /// 方法说明：反射调用选择界面控制器私有方法，支持传入参数。
    /// 参数说明：controller 为目标控制器，methodName 为方法名，args 为调用参数。
    /// 返回说明：无返回值；方法缺失时抛出异常。
    /// </summary>
    private static void InvokePrivateControllerMethod(SelectKingSceneController controller, string methodName, object[] args)
    {
        MethodInfo method = typeof(SelectKingSceneController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new MissingMethodException(typeof(SelectKingSceneController).FullName, methodName);
        }

        method.Invoke(controller, args);
    }

    /// <summary>
    /// 方法说明：反射调用任意对象的私有无参方法。
    /// 参数说明：target 为目标对象，methodName 为方法名。
    /// 返回说明：无返回值；对象或方法缺失时抛出异常。
    /// </summary>
    private static void InvokePrivateInstanceMethod(object target, string methodName)
    {
        InvokePrivateInstanceMethod(target, methodName, null);
    }

    /// <summary>
    /// 方法说明：反射调用任意对象的私有方法，支持传入参数。
    /// 参数说明：target 为目标对象，methodName 为方法名，args 为调用参数。
    /// 返回说明：无返回值；对象或方法缺失时抛出异常。
    /// </summary>
    private static void InvokePrivateInstanceMethod(object target, string methodName, object[] args)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new MissingMethodException(target.GetType().FullName, methodName);
        }

        method.Invoke(target, args);
    }

    /// <summary>
    /// 方法说明：收集对象子树中的非空字体文本。
    /// 参数说明：root 为目标对象。
    /// 返回说明：返回非空文本列表。
    /// </summary>
    private static List<string> CollectFontTexts(GameObject root)
    {
        List<string> labels = new List<string>();
        if (root == null)
        {
            return labels;
        }

        exSpriteFont[] fonts = root.GetComponentsInChildren<exSpriteFont>(true);
        foreach (exSpriteFont font in fonts)
        {
            if (!string.IsNullOrEmpty(font.text))
            {
                labels.Add(font.text);
            }
        }

        return labels;
    }

    /// <summary>
    /// 方法说明：输出指定对象下 TextMesh 的关键渲染信息，辅助定位截图空白问题。
    /// 参数说明：root 为检查根对象，prefix 为日志前缀。
    /// 返回说明：无返回值。
    /// </summary>
    private static void DumpTextMeshes(GameObject root, string prefix)
    {
        if (root == null)
        {
            Debug.Log(prefix + " COUNT: 0 root=null");
            return;
        }

        TextMesh[] textMeshes = root.GetComponentsInChildren<TextMesh>(true);
        Debug.Log(prefix + " COUNT: " + textMeshes.Length);
        for (int i = 0; i < textMeshes.Length; i++)
        {
            TextMesh textMesh = textMeshes[i];
            Renderer renderer = textMesh.GetComponent<Renderer>();
            Debug.Log(prefix + ": " + BuildTransformPath(textMesh.transform) +
                      " text=" + textMesh.text +
                      " localPos=" + FormatVector3(textMesh.transform.localPosition) +
                      " worldPos=" + FormatVector3(textMesh.transform.position) +
                      " layer=" + textMesh.gameObject.layer +
                      " renderer=" + (renderer != null && renderer.enabled));
        }
    }

    /// <summary>
    /// 方法说明：统计对象子树中指定贴图路径前缀的 exSprite 数量。
    /// 参数说明：root 为目标对象，texturePrefix 为贴图资源路径前缀。
    /// 返回说明：返回匹配的 exSprite 数量。
    /// </summary>
    private static int CountSpritesByTexturePrefix(GameObject root, string texturePrefix)
    {
        if (root == null)
        {
            return 0;
        }

        int count = 0;
        exSprite[] sprites = root.GetComponentsInChildren<exSprite>(true);
        foreach (exSprite sprite in sprites)
        {
            string textureGuid = GetField<string>(sprite, "textureGUID");
            string texturePath = string.IsNullOrEmpty(textureGuid) ? "" : AssetDatabase.GUIDToAssetPath(textureGuid);
            if (texturePath.StartsWith(texturePrefix, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 方法说明：读取分类资源 TSV 清单。
    /// 参数说明：manifestPath 为 TSV 路径，limit 为最多读取条数。
    /// 返回说明：返回资源记录列表。
    /// </summary>
    private static List<RecoveredMediaRecord> LoadMediaRecords(string manifestPath, int limit)
    {
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("缺少 PAK 分类清单", manifestPath);
        }

        string[] lines = File.ReadAllLines(manifestPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException("PAK 分类清单为空：" + manifestPath);
        }

        List<RecoveredMediaRecord> records = new List<RecoveredMediaRecord>();
        for (int i = 1; i < lines.Length && records.Count < limit; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] columns = lines[i].Split('\t');
            if (columns.Length < 8)
            {
                throw new InvalidOperationException("PAK 分类清单格式错误，第 " + (i + 1) + " 行：" + manifestPath);
            }

            string sourcePath = columns[7];
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("PAK 已提取资源不存在", sourcePath);
            }

            records.Add(new RecoveredMediaRecord(
                int.Parse(columns[0]),
                columns[1],
                int.Parse(columns[2]),
                int.Parse(columns[3]),
                columns[4],
                int.Parse(columns[5]),
                int.Parse(columns[6]),
                sourcePath));
        }

        if (records.Count < limit)
        {
            throw new InvalidOperationException("PAK 分类清单数量不足：" + manifestPath + "，需要 " + limit + "，实际 " + records.Count);
        }

        return records;
    }

    /// <summary>
    /// 方法说明：读取分类资源 TSV 清单中的全部资源记录。
    /// 参数说明：manifestPath 为 TSV 路径。
    /// 返回说明：返回全部资源记录列表。
    /// </summary>
    private static List<RecoveredMediaRecord> LoadAllMediaRecords(string manifestPath)
    {
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("缺少 PAK 分类清单", manifestPath);
        }

        string[] lines = File.ReadAllLines(manifestPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException("PAK 分类清单为空：" + manifestPath);
        }

        List<RecoveredMediaRecord> records = new List<RecoveredMediaRecord>();
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] columns = lines[i].Split('\t');
            if (columns.Length < 8)
            {
                throw new InvalidOperationException("PAK 分类清单格式错误，第 " + (i + 1) + " 行：" + manifestPath);
            }

            string sourcePath = columns[7];
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("PAK 已提取资源不存在", sourcePath);
            }

            records.Add(new RecoveredMediaRecord(
                int.Parse(columns[0]),
                columns[1],
                int.Parse(columns[2]),
                int.Parse(columns[3]),
                columns[4],
                int.Parse(columns[5]),
                int.Parse(columns[6]),
                sourcePath));
        }

        return records;
    }

    /// <summary>
    /// 方法说明：打开场景并输出地图相关对象。
    /// 参数说明：scenePath 为场景资源路径。
    /// 返回说明：无返回值。
    /// </summary>
    private static void DumpMapSceneObjects(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Debug.Log("MAP SCENE: " + scenePath + " loaded=" + scene.isLoaded);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                if (IsMapRelated(transform.gameObject))
                {
                    Debug.Log("MAP OBJECT: " + BuildTransformPath(transform) + " components=" + BuildComponentLog(transform.gameObject));
                }
            }
        }
    }

    /// <summary>
    /// 方法说明：打开场景并输出大尺寸 exSprite 对象。
    /// 参数说明：scenePath 为场景资源路径。
    /// 返回说明：无返回值。
    /// </summary>
    private static void DumpLargeSprites(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Debug.Log("LARGE SPRITE SCENE: " + scenePath + " loaded=" + scene.isLoaded);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            exSprite[] sprites = root.GetComponentsInChildren<exSprite>(true);
            foreach (exSprite sprite in sprites)
            {
                float width = GetField<float>(sprite, "width_");
                float height = GetField<float>(sprite, "height_");
                if (width < 300f && height < 240f)
                {
                    continue;
                }

                string textureGuid = GetField<string>(sprite, "textureGUID");
                string texturePath = string.IsNullOrEmpty(textureGuid) ? "" : AssetDatabase.GUIDToAssetPath(textureGuid);
                Debug.Log("LARGE SPRITE: " + BuildTransformPath(sprite.transform) + " texture=" + texturePath + " size=" + width + "x" + height +
                          " localPos=" + FormatVector3(sprite.transform.localPosition) +
                          " worldPos=" + FormatVector3(sprite.transform.position) +
                          " scale=" + FormatVector3(sprite.transform.localScale));
            }
        }
    }

    /// <summary>
    /// 方法说明：判断对象是否和地图显示相关。
    /// 参数说明：gameObject 为待判断对象。
    /// 返回说明：地图相关返回 true，否则返回 false。
    /// </summary>
    private static bool IsMapRelated(GameObject gameObject)
    {
        if (gameObject.GetComponent<MapController>() != null)
        {
            return true;
        }

        if (gameObject.name.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0 ||
            gameObject.name.IndexOf("City", StringComparison.OrdinalIgnoreCase) >= 0 ||
            gameObject.name.IndexOf("Power", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 方法说明：判断对象是否属于选择界面关键 UI。
    /// 参数说明：transform 为待判断对象。
    /// 返回说明：属于副本、君主列表、头像或确认区域时返回 true。
    /// </summary>
    private static bool IsSelectUiRelated(Transform transform)
    {
        string path = BuildTransformPath(transform);
        return path.IndexOf("SelectMOD", StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.IndexOf("SelectKing", StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.IndexOf("MenuSelectKing", StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.IndexOf("KingInformation", StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.IndexOf("Confirm", StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// 方法说明：读取对象上的 exSpriteFont 文本。
    /// 参数说明：gameObject 为待读取对象。
    /// 返回说明：有字体返回文本，否则返回空字符串。
    /// </summary>
    private static string GetFontText(GameObject gameObject)
    {
        exSpriteFont font = gameObject.GetComponent<exSpriteFont>();
        return font == null ? "" : font.text;
    }

    /// <summary>
    /// 方法说明：生成对象组件和 exSprite 贴图日志。
    /// 参数说明：gameObject 为待输出对象。
    /// 返回说明：返回组件摘要。
    /// </summary>
    private static string BuildComponentLog(GameObject gameObject)
    {
        Component[] components = gameObject.GetComponents<Component>();
        List<string> names = new List<string>();
        foreach (Component component in components)
        {
            if (component == null)
            {
                names.Add("Missing");
                continue;
            }

            string item = component.GetType().Name;
            exSprite sprite = component as exSprite;
            if (sprite != null)
            {
                string textureGuid = GetField<string>(sprite, "textureGUID");
                string texturePath = string.IsNullOrEmpty(textureGuid) ? "" : AssetDatabase.GUIDToAssetPath(textureGuid);
                item += "(texture=" + texturePath + ",size=" + GetField<float>(sprite, "width_") + "x" + GetField<float>(sprite, "height_") + ")";
            }

            names.Add(item);
        }

        return string.Join(",", names.ToArray());
    }

    /// <summary>
    /// 方法说明：生成 Transform 的完整层级路径。
    /// 参数说明：transform 为目标 Transform。
    /// 返回说明：返回场景内层级路径。
    /// </summary>
    private static string BuildTransformPath(Transform transform)
    {
        List<string> parts = new List<string>();
        Transform cursor = transform;
        while (cursor != null)
        {
            parts.Add(cursor.name);
            cursor = cursor.parent;
        }

        parts.Reverse();
        return string.Join("/", parts.ToArray());
    }

    /// <summary>
    /// 方法说明：格式化三维坐标，便于日志对比场景布局。
    /// 参数说明：value 为待输出的 Vector3。
    /// 返回说明：返回保留一位小数的坐标字符串。
    /// </summary>
    private static string FormatVector3(Vector3 value)
    {
        return "(" + value.x.ToString("F1") + "," + value.y.ToString("F1") + "," + value.z.ToString("F1") + ")";
    }

    /// <summary>
    /// 方法说明：复制恢复出的 1055 张头像到工程目录。
    /// 参数说明：records 为头像资源记录。
    /// 返回说明：无返回值。
    /// </summary>
    private static void CopyFaceResources(IReadOnlyList<RecoveredMediaRecord> records)
    {
        Directory.CreateDirectory(FaceDirectory);
        for (int i = 0; i < records.Count; i++)
        {
            string targetPath = FaceDirectory + "/Head" + (i + 1).ToString("D3") + ".png";
            File.Copy(records[i].sourcePath, targetPath, true);
        }
    }

    /// <summary>
    /// 方法说明：复制恢复出的大地图和预览地图资源。
    /// 参数说明：records 为地图资源记录。
    /// 返回说明：无返回值。
    /// </summary>
    private static void CopyMapResources(IReadOnlyList<RecoveredMediaRecord> records)
    {
        Directory.CreateDirectory(MapDirectory);
        if (records.Count == 0)
        {
            throw new InvalidOperationException("没有可导入的二代地图资源");
        }

        for (int i = 0; i < records.Count; i++)
        {
            string targetName = i == 0 ? "Sango2WorldMap.png" : "Sango2WorldMapPreview.png";
            File.Copy(records[i].sourcePath, MapDirectory + "/" + targetName, true);
        }
    }

    /// <summary>
    /// 方法说明：校验 Unity 资源文件是否存在。
    /// 参数说明：assetPath 为 Unity 工程内资源路径。
    /// 返回说明：无返回值；缺失时抛出异常。
    /// </summary>
    private static void ValidateAssetFile(string assetPath)
    {
        if (!File.Exists(assetPath))
        {
            throw new FileNotFoundException("资源不存在，请先导入 PAK 资源", assetPath);
        }
    }

    /// <summary>
    /// 方法说明：按层级路径替换场景 exSprite 贴图。
    /// 参数说明：scene 为目标场景，hierarchyPath 为对象层级路径，texturePath 为贴图路径，width 为显示宽度，height 为显示高度。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ApplySceneSprite(Scene scene, string hierarchyPath, string texturePath, float width, float height)
    {
        GameObject target = FindSceneObject(scene, hierarchyPath);
        if (target == null)
        {
            throw new InvalidOperationException("场景对象不存在：" + scene.path + " / " + hierarchyPath);
        }

        exSprite sprite = target.GetComponent<exSprite>();
        if (sprite == null)
        {
            throw new InvalidOperationException("场景对象缺少 exSprite：" + scene.path + " / " + hierarchyPath);
        }

        ApplyTextureToExSprite(sprite, texturePath, width, height);
        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(sprite);
    }

    /// <summary>
    /// 方法说明：按层级路径查找场景对象。
    /// 参数说明：scene 为目标场景，hierarchyPath 为对象层级路径。
    /// 返回说明：找到时返回对象，否则返回 null。
    /// </summary>
    private static GameObject FindSceneObject(Scene scene, string hierarchyPath)
    {
        string[] names = hierarchyPath.Split('/');
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name != names[0])
            {
                continue;
            }

            Transform cursor = root.transform;
            for (int i = 1; i < names.Length && cursor != null; i++)
            {
                cursor = cursor.Find(names[i]);
            }

            return cursor == null ? null : cursor.gameObject;
        }

        return null;
    }

    /// <summary>
    /// 方法说明：复制恢复出的音频资源到 Resources 子目录。
    /// 参数说明：records 为音频资源记录，extension 为目标扩展名。
    /// 返回说明：无返回值。
    /// </summary>
    private static void CopyAudioResources(IReadOnlyList<RecoveredMediaRecord> records, string extension)
    {
        Directory.CreateDirectory(AudioDirectory);
        for (int i = 0; i < records.Count; i++)
        {
            string targetPath = AudioDirectory + "/Sango2_" + extension.ToUpperInvariant() + "_" + (i + 1).ToString("D3") + "." + extension;
            File.Copy(records[i].sourcePath, targetPath, true);
        }
    }

    /// <summary>
    /// 方法说明：复制技能帧资源到临时 Unity 导入目录。
    /// 参数说明：records 为技能帧记录。
    /// 返回说明：无返回值。
    /// </summary>
    private static void CopySkillFrameResources(IReadOnlyList<RecoveredMediaRecord> records)
    {
        for (int i = 0; i < records.Count; i++)
        {
            string targetPath = SkillImportDirectory + "/SkillFrame" + (i + 1).ToString("D5") + ".png";
            File.Copy(records[i].sourcePath, targetPath, true);
        }
    }

    /// <summary>
    /// 方法说明：给临时导入的技能帧分配 AssetBundle 名称。
    /// 参数说明：frameCount 为技能帧数量。
    /// 返回说明：无返回值。
    /// </summary>
    private static void AssignSkillFrameBundleNames(int frameCount)
    {
        for (int i = 1; i <= frameCount; i++)
        {
            string assetPath = SkillImportDirectory + "/SkillFrame" + i.ToString("D5") + ".png";
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                throw new InvalidOperationException("技能帧导入失败：" + assetPath);
            }

            int bundleIndex = (i - 1) / SkillFramesPerBundle + 1;
            importer.assetBundleName = SkillBundlePrefix + bundleIndex.ToString("D2");
        }
    }

    /// <summary>
    /// 方法说明：写出 MOD06 技能资源映射清单。
    /// 参数说明：skillFrameCount 为恢复出的技能帧总数。
    /// 返回说明：无返回值。
    /// </summary>
    private static void WriteSkillResourceMap(int skillFrameCount)
    {
        TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/XML/Resources/MagicConfig.xml");
        if (textAsset == null)
        {
            throw new FileNotFoundException("缺少技能配置", "Assets/XML/Resources/MagicConfig.xml");
        }

        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(textAsset.text.Trim());
        XmlNodeList magicNodes = xmlDocument.SelectNodes("/MagicConfig/MAGIC");
        if (magicNodes == null || magicNodes.Count == 0)
        {
            throw new InvalidOperationException("技能配置没有 MAGIC 节点");
        }

        using (StreamWriter writer = new StreamWriter(SkillMapReportPath, false))
        {
            writer.WriteLine("magic_sequence\tname\tscript\tprefab_resource\tbundle_group\tbundle_asset_range");
            foreach (XmlElement magicNode in magicNodes)
            {
                string sequence = magicNode.GetAttribute("SEQUENCE");
                string name = magicNode.GetAttribute("NAME");
                string script = magicNode.GetAttribute("SCRIPT");
                writer.WriteLine(sequence + "\t" + name + "\t" + script + "\tMagic/" + script + "\t" + BuildSkillBundleList(skillFrameCount) + "\tSkillFrame00001-SkillFrame" + skillFrameCount.ToString("D5"));
            }
        }
    }

    /// <summary>
    /// 方法说明：生成技能帧 AssetBundle 名称列表。
    /// 参数说明：skillFrameCount 为技能帧总数。
    /// 返回说明：返回逗号分隔的 Bundle 名称。
    /// </summary>
    private static string BuildSkillBundleList(int skillFrameCount)
    {
        int bundleCount = GetSkillBundleCount(skillFrameCount);
        List<string> names = new List<string>();
        for (int i = 1; i <= bundleCount; i++)
        {
            names.Add(SkillBundlePrefix + i.ToString("D2"));
        }

        return string.Join(",", names.ToArray());
    }

    /// <summary>
    /// 方法说明：计算技能帧 AssetBundle 数量。
    /// 参数说明：skillFrameCount 为技能帧总数。
    /// 返回说明：返回 Bundle 数量。
    /// </summary>
    private static int GetSkillBundleCount(int skillFrameCount)
    {
        return Mathf.CeilToInt(skillFrameCount / (float)SkillFramesPerBundle);
    }

    /// <summary>
    /// 方法说明：删除 Unity 工程内目录和对应 meta。
    /// 参数说明：assetDirectory 为工程内目录路径。
    /// 返回说明：无返回值。
    /// </summary>
    private static void DeleteAssetDirectory(string assetDirectory)
    {
        if (Directory.Exists(assetDirectory))
        {
            FileUtil.DeleteFileOrDirectory(assetDirectory);
        }

        string metaPath = assetDirectory + ".meta";
        if (File.Exists(metaPath))
        {
            FileUtil.DeleteFileOrDirectory(metaPath);
        }
    }

    /// <summary>
    /// 方法说明：设置头像贴图导入参数。
    /// 参数说明：无。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ConfigureFaceTextures()
    {
        for (int i = 1; i <= RestoredFaceCount; i++)
        {
            string path = FaceDirectory + "/Head" + i.ToString("D3") + ".png";
            ConfigureTexture(path, TextureImporterType.Default, 1024, false);
        }
    }

    /// <summary>
    /// 方法说明：设置大地图贴图导入参数。
    /// 参数说明：无。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ConfigureMapTextures()
    {
        ConfigureTexture(MapDirectory + "/Sango2WorldMap.png", TextureImporterType.Default, 4096, false, true);
        ConfigureTexture(WorldMapGeneratedResourcePath, TextureImporterType.Default, 8192, false, true);
        ConfigureTexture(MapDirectory + "/Sango2WorldMapPreview.png", TextureImporterType.Default, 2048, false);
    }

    /// <summary>
    /// 方法说明：只刷新二代恢复地图贴图的 Unity 导入参数，避免重跑完整 PAK 导入流程。
    /// 参数说明：无。
    /// 返回说明：无返回值。
    /// </summary>
    [MenuItem(MenuRoot + "配置地图贴图导入参数")]
    public static void ConfigureRecoveredMapTextureImportSettings()
    {
        ConfigureMapTextures();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    /// <summary>
    /// 方法说明：按统一参数设置贴图导入器。
    /// 参数说明：assetPath 为资源路径，textureType 为贴图类型，maxSize 为最大尺寸，mipmapEnabled 表示是否开启 mipmap，isUncompressed 表示是否禁用平台压缩。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ConfigureTexture(string assetPath, TextureImporterType textureType, int maxSize, bool mipmapEnabled, bool isUncompressed = false)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException("贴图导入器不存在：" + assetPath);
        }

        importer.textureType = textureType;
        importer.mipmapEnabled = mipmapEnabled;
        importer.isReadable = true;
        importer.maxTextureSize = maxSize;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        if (isUncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            ConfigureUncompressedPlatformTexture(importer, "DefaultTexturePlatform", maxSize);
            ConfigureUncompressedPlatformTexture(importer, "Standalone", maxSize);
            ConfigureUncompressedPlatformTexture(importer, "Android", maxSize);
        }
        importer.SaveAndReimport();
    }

    /// <summary>
    /// 方法说明：把指定平台的贴图导入参数设置为原尺寸无压缩。
    /// 参数说明：importer 为目标贴图导入器，platformName 为平台名称，maxSize 为最大贴图尺寸。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ConfigureUncompressedPlatformTexture(TextureImporter importer, string platformName, int maxSize)
    {
        TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(platformName);
        platformSettings.overridden = true;
        platformSettings.maxTextureSize = maxSize;
        platformSettings.format = TextureImporterFormat.RGB24;
        platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
        platformSettings.compressionQuality = 100;
        importer.SetPlatformTextureSettings(platformSettings);
    }

    /// <summary>
    /// 方法说明：根据恢复头像生成 Head001 到 Head1055 prefab。
    /// 参数说明：无。
    /// 返回说明：无返回值。
    /// </summary>
    private static void GenerateHeadPrefabs()
    {
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(HeadTemplatePath);
        if (template == null)
        {
            throw new FileNotFoundException("缺少头像 prefab 模板", HeadTemplatePath);
        }

        Directory.CreateDirectory(HeadPrefabDirectory);
        Directory.CreateDirectory(HeadMaterialDirectory);
        for (int i = 1; i <= RestoredFaceCount; i++)
        {
            string prefabPath = HeadPrefabDirectory + "/Head" + i.ToString("D3") + ".prefab";
            string texturePath = FaceDirectory + "/Head" + i.ToString("D3") + ".png";
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                throw new InvalidOperationException("头像贴图导入失败：" + texturePath);
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(template) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("头像 prefab 模板实例化失败：" + HeadTemplatePath);
            }

            instance.name = "Head" + i.ToString("D3");
            ApplyTextureToExSprite(instance, texture, texturePath, i);
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    /// <summary>
    /// 方法说明：把指定贴图绑定到 exSprite 头像对象。
    /// 参数说明：instance 为头像 prefab 实例，texture 为头像贴图，texturePath 为贴图路径，headIndex 为头像序号。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ApplyTextureToExSprite(GameObject instance, Texture2D texture, string texturePath, int headIndex)
    {
        exSprite sprite = instance.GetComponent<exSprite>();
        if (sprite == null)
        {
            throw new InvalidOperationException("头像 prefab 缺少 exSprite 组件：" + instance.name);
        }

        ApplyTextureToExSprite(sprite, texturePath, texture.width, texture.height);
        ApplyHeadMaterial(instance, texture, headIndex);
    }

    /// <summary>
    /// 方法说明：给头像 prefab 绑定独立材质，避免所有头像复用模板材质而显示同一张图。
    /// 参数说明：instance 为头像 prefab 实例，texture 为头像贴图，headIndex 为头像序号。
    /// 返回说明：无返回值；渲染器或材质 Shader 缺失时抛出异常。
    /// </summary>
    private static void ApplyHeadMaterial(GameObject instance, Texture2D texture, int headIndex)
    {
        Renderer renderer = instance.GetComponent<Renderer>();
        if (renderer == null)
        {
            throw new InvalidOperationException("头像 prefab 缺少 Renderer 组件：" + instance.name);
        }

        Material baseMaterial = renderer.sharedMaterial;
        Shader shader = baseMaterial == null ? Shader.Find("Unlit/Transparent") : baseMaterial.shader;
        if (shader == null)
        {
            throw new InvalidOperationException("头像材质 Shader 缺失：" + instance.name);
        }

        string materialPath = HeadMaterialDirectory + "/Head" + headIndex.ToString("D3") + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.mainTexture = texture;
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", Color.white);
        }

        renderer.sharedMaterial = material;
        EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(renderer);
    }

    /// <summary>
    /// 方法说明：把指定贴图路径写入 exSprite 并设置显示尺寸。
    /// 参数说明：sprite 为目标 exSprite，texturePath 为贴图路径，width 为显示宽度，height 为显示高度。
    /// 返回说明：无返回值。
    /// </summary>
    private static void ApplyTextureToExSprite(exSprite sprite, string texturePath, float width, float height)
    {
        string textureGuid = AssetDatabase.AssetPathToGUID(texturePath);
        if (string.IsNullOrEmpty(textureGuid))
        {
            throw new InvalidOperationException("贴图 GUID 为空：" + texturePath);
        }

        SetField(sprite, "textureGUID", textureGuid);
        SetField(sprite, "trimTexture", true);
        SetField(sprite, "customSize_", true);
        SetField(sprite, "width_", width);
        SetField(sprite, "height_", height);
        SetField(sprite, "atlas_", null);
        SetField(sprite, "index_", 0);
        SetField(sprite, "trimUV", new Rect(0f, 0f, 1f, 1f));
    }

    /// <summary>
    /// 方法说明：通过反射设置 exSprite 序列化字段。
    /// 参数说明：target 为目标对象，fieldName 为字段名，value 为字段值。
    /// 返回说明：无返回值。
    /// </summary>
    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException("exSprite 字段不存在：" + fieldName);
        }

        field.SetValue(target, value);
    }

    /// <summary>
    /// 方法说明：通过反射读取 exSprite 序列化字段。
    /// 参数说明：target 为目标对象，fieldName 为字段名。
    /// 返回说明：返回字段值。
    /// </summary>
    private static T GetField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
        {
            throw new InvalidOperationException("exSprite 字段不存在：" + fieldName);
        }

        return (T)field.GetValue(target);
    }

    /// <summary>
    /// 方法说明：写出导入报告。
    /// 参数说明：faceCount 为头像数，mapCount 为地图数，oggCount 为 OGG 数，riffCount 为 RIFF 数。
    /// 返回说明：无返回值。
    /// </summary>
    private static void WriteImportReport(int faceCount, int mapCount, int oggCount, int riffCount)
    {
        string reportPath = "Assets/XML/Resources/Sango2RecoveredResources.tsv";
        using (StreamWriter writer = new StreamWriter(reportPath, false))
        {
            writer.WriteLine("type\tcount\tpath");
            writer.WriteLine("face\t" + faceCount + "\t" + FaceDirectory);
            writer.WriteLine("map\t" + mapCount + "\t" + MapDirectory);
            writer.WriteLine("ogg\t" + oggCount + "\t" + AudioDirectory);
            writer.WriteLine("riff\t" + riffCount + "\t" + AudioDirectory);
        }
    }

    /// <summary>
    /// 方法说明：生成方法参数日志。
    /// 参数说明：method 为待输出参数的方法。
    /// 返回说明：返回参数摘要。
    /// </summary>
    private static string BuildMethodParameterLog(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        List<string> parts = new List<string>();
        foreach (ParameterInfo parameter in parameters)
        {
            parts.Add(parameter.ParameterType.FullName + " " + parameter.Name);
        }

        return string.Join(",", parts.ToArray());
    }

    /// <summary>
    /// 恢复资源清单记录。
    /// </summary>
    private sealed class RecoveredMediaRecord
    {
        public readonly int index;
        public readonly string pak;
        public readonly int offset;
        public readonly int size;
        public readonly string mediaType;
        public readonly int width;
        public readonly int height;
        public readonly string sourcePath;

        /// <summary>
        /// 方法说明：创建恢复资源清单记录。
        /// 参数说明：index 为序号，pak 为分卷名，offset 为偏移，size 为大小，mediaType 为媒体类型，width 为宽度，height 为高度，sourcePath 为源文件路径。
        /// 返回说明：构造函数无返回值。
        /// </summary>
        public RecoveredMediaRecord(int index, string pak, int offset, int size, string mediaType, int width, int height, string sourcePath)
        {
            this.index = index;
            this.pak = pak;
            this.offset = offset;
            this.size = size;
            this.mediaType = mediaType;
            this.width = width;
            this.height = height;
            this.sourcePath = sourcePath;
        }
    }
}
