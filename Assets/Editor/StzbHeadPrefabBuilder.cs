using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 工具说明：把率土头像源图生成到独立 Resources 目录，避免和 MOD06 二代恢复头像共用 Head 路径。
/// </summary>
public static class StzbHeadPrefabBuilder
{
    private const string MenuRoot = "工具/率土头像/";
    private const string SourceDirectory = "Assets/Graph/Head";
    private const string OutputPrefabDirectory = "Assets/Prefabs/Resources/StzbHead";
    private const string OutputMaterialDirectory = "Assets/Graph/Head/Materials/StzbHead";
    private const string TemplatePrefabPath = "Assets/Prefabs/Resources/Head/Head001.prefab";
    private const int StzbHeadCount = 255;

    /// <summary>
    /// 方法说明：生成 01-05 剧本使用的率土头像 Resources prefab。
    /// 参数说明：无参数。
    /// 返回说明：无返回值；源图或模板缺失时抛出异常。
    /// </summary>
    [MenuItem(MenuRoot + "重建运行时头像Prefab")]
    public static void RebuildStzbHeadPrefabs()
    {
        // 1. 校验源图完整性，缺图时直接失败，避免运行时静默拿错头像。
        ValidateStzbHeadSourceTextures();

        // 2. 基于现有 exSprite 头像模板生成独立 Resources/StzbHead prefab。
        GameObject template = LoadTemplatePrefab();
        Directory.CreateDirectory(OutputPrefabDirectory);
        Directory.CreateDirectory(OutputMaterialDirectory);
        for (int i = 1; i <= StzbHeadCount; i++)
        {
            GenerateStzbHeadPrefab(template, i);
        }

        // 3. 保存 prefab 和材质资源。
        AssetDatabase.SaveAssets();

        // 4. 刷新资源数据库，让 GeneralsHeadSelect 可以通过 Resources.Load 读取。
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("率土运行时头像 prefab 已重建：" + StzbHeadCount);
    }

    /// <summary>
    /// 方法说明：校验率土头像源图是否连续存在。
    /// 参数说明：无参数。
    /// 返回说明：无返回值；缺少任意源图时抛出异常。
    /// </summary>
    private static void ValidateStzbHeadSourceTextures()
    {
        for (int i = 1; i <= StzbHeadCount; i++)
        {
            string texturePath = GetSourceTexturePath(i);
            if (!File.Exists(texturePath))
            {
                throw new FileNotFoundException("率土头像源图缺失", texturePath);
            }
        }
    }

    /// <summary>
    /// 方法说明：读取头像 prefab 模板。
    /// 参数说明：无参数。
    /// 返回说明：返回模板 prefab。
    /// </summary>
    private static GameObject LoadTemplatePrefab()
    {
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefabPath);
        if (template == null)
        {
            throw new FileNotFoundException("头像 prefab 模板缺失", TemplatePrefabPath);
        }

        return template;
    }

    /// <summary>
    /// 方法说明：生成单个率土头像 prefab。
    /// 参数说明：template 为头像模板，headIndex 为头像编号。
    /// 返回说明：无返回值。
    /// </summary>
    private static void GenerateStzbHeadPrefab(GameObject template, int headIndex)
    {
        string texturePath = GetSourceTexturePath(headIndex);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            throw new InvalidOperationException("率土头像贴图导入失败：" + texturePath);
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(template) as GameObject;
        if (instance == null)
        {
            throw new InvalidOperationException("头像 prefab 模板实例化失败：" + TemplatePrefabPath);
        }

        instance.name = "Head" + headIndex.ToString("D3");
        ApplyTextureToExSprite(instance, texture, texturePath, headIndex);
        PrefabUtility.SaveAsPrefabAsset(instance, GetOutputPrefabPath(headIndex));
        UnityEngine.Object.DestroyImmediate(instance);
    }

    /// <summary>
    /// 方法说明：把率土头像贴图绑定到 exSprite prefab 实例。
    /// 参数说明：instance 为头像实例，texture 为源贴图，texturePath 为源贴图路径，headIndex 为头像编号。
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
    /// 方法说明：给率土头像 prefab 绑定独立材质。
    /// 参数说明：instance 为头像实例，texture 为源贴图，headIndex 为头像编号。
    /// 返回说明：无返回值。
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

        string materialPath = OutputMaterialDirectory + "/Head" + headIndex.ToString("D3") + ".mat";
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
    /// 方法说明：读取率土头像源图路径。
    /// 参数说明：headIndex 为头像编号。
    /// 返回说明：返回工程内源图路径。
    /// </summary>
    private static string GetSourceTexturePath(int headIndex)
    {
        return SourceDirectory + "/" + headIndex + ".png";
    }

    /// <summary>
    /// 方法说明：读取率土头像输出 prefab 路径。
    /// 参数说明：headIndex 为头像编号。
    /// 返回说明：返回工程内 prefab 路径。
    /// </summary>
    private static string GetOutputPrefabPath(int headIndex)
    {
        return OutputPrefabDirectory + "/Head" + headIndex.ToString("D3") + ".prefab";
    }
}
