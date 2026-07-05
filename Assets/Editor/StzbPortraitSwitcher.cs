using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 率土头像和原始头像的一键切换工具。
/// </summary>
public static class StzbPortraitSwitcher
{
    private const string MenuRoot = "工具/立绘切换/";
    private const string HeadDirectory = "Assets/Graph/Head";
    private const string VariantDirectory = "Assets/Graph/HeadVariants";
    private const string OriginalDirectory = VariantDirectory + "/Original";
    private const string StzbPortraitDirectory = VariantDirectory + "/StzbFiveStar";
    private const string MappingPath = VariantDirectory + "/replaced-heads.tsv";

    private enum PortraitVariant
    {
        Original,
        StzbPortrait
    }

    private sealed class PortraitEntry
    {
        public string HeadFile;
        public string ProjectName;
        public string UniqueName;
    }

    /// <summary>
    /// 方法说明：根据当前头像状态在原始头像和率土头像之间切换。
    /// 参数说明：无。
    /// 返回说明：无返回值；资源缺失或当前状态混合时抛出异常。
    /// </summary>
    [MenuItem(MenuRoot + "一键切换原始和率土头像")]
    public static void TogglePortraits()
    {
        // 1. 读取并校验头像映射，保证切换只覆盖已登记的武将头像。
        List<PortraitEntry> entries = LoadEntries();

        // 2. 判断当前 Head 目录属于哪一套资源，混合状态不自动猜测。
        PortraitVariant? currentVariant = GetCurrentVariant(entries);
        if (currentVariant == null)
        {
            throw new InvalidOperationException("当前头像处于混合状态，无法一键判断目标。请先使用“还原原始头像”或“使用率土头像”。");
        }

        // 3. 选择另一套资源并执行覆盖。
        PortraitVariant nextVariant = currentVariant == PortraitVariant.Original
            ? PortraitVariant.StzbPortrait
            : PortraitVariant.Original;
        ApplyVariant(nextVariant, entries);

        // 4. 输出切换结果，让用户可以在 Console 里确认本次动作。
        Debug.Log("立绘切换完成：" + GetVariantLabel(nextVariant) + "，替换数量：" + entries.Count);
    }

    /// <summary>
    /// 方法说明：强制把已登记武将头像切换为率土头像。
    /// 参数说明：无。
    /// 返回说明：无返回值；资源缺失时抛出异常。
    /// </summary>
    [MenuItem(MenuRoot + "使用率土头像")]
    public static void UseStzbPortraits()
    {
        List<PortraitEntry> entries = LoadEntries();
        ApplyVariant(PortraitVariant.StzbPortrait, entries);
        Debug.Log("已使用率土头像，替换数量：" + entries.Count);
    }

    /// <summary>
    /// 方法说明：强制把已登记武将头像还原为原始头像。
    /// 参数说明：无。
    /// 返回说明：无返回值；资源缺失时抛出异常。
    /// </summary>
    [MenuItem(MenuRoot + "还原原始头像")]
    public static void UseOriginalPortraits()
    {
        List<PortraitEntry> entries = LoadEntries();
        ApplyVariant(PortraitVariant.Original, entries);
        Debug.Log("已还原原始头像，替换数量：" + entries.Count);
    }

    /// <summary>
    /// 方法说明：检查切换资源和当前头像状态。
    /// 参数说明：无。
    /// 返回说明：无返回值；资源缺失时抛出异常。
    /// </summary>
    [MenuItem(MenuRoot + "检查切换资源")]
    public static void CheckSwitchResources()
    {
        List<PortraitEntry> entries = LoadEntries();
        PortraitVariant? currentVariant = GetCurrentVariant(entries);
        string currentLabel = currentVariant == null ? "混合状态" : GetVariantLabel(currentVariant.Value);
        Debug.Log("立绘切换资源检查通过。登记数量：" + entries.Count + "，当前状态：" + currentLabel);
    }

    /// <summary>
    /// 方法说明：把指定变体复制到游戏实际读取的 Head 目录。
    /// 参数说明：variant 表示目标变体；entries 表示需要替换的头像清单。
    /// 返回说明：无返回值；复制失败或资源缺失时抛出异常。
    /// </summary>
    private static void ApplyVariant(PortraitVariant variant, IReadOnlyList<PortraitEntry> entries)
    {
        // 1. 定位目标变体目录，避免把未登记目录误作为素材源。
        string sourceDirectory = GetVariantDirectory(variant);

        // 2. 逐项校验源文件和目标文件，任何缺失都立即失败。
        foreach (PortraitEntry entry in entries)
        {
            ValidateVariantFile(sourceDirectory, entry);
            ValidateTargetFile(entry);
        }

        // 3. 覆盖 Head 目录中的同名 PNG，保留 Unity 现有 .meta、材质和 prefab 引用。
        foreach (PortraitEntry entry in entries)
        {
            CopyVariantFile(sourceDirectory, entry);
        }

        // 4. 刷新 Unity 资源数据库，让编辑器立即看到切换后的头像。
        foreach (PortraitEntry entry in entries)
        {
            AssetDatabase.ImportAsset(GetTargetPath(entry), ImportAssetOptions.ForceUpdate);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    /// <summary>
    /// 方法说明：读取头像切换清单。
    /// 参数说明：无。
    /// 返回说明：返回已登记的头像条目列表；清单缺失或格式错误时抛出异常。
    /// </summary>
    private static List<PortraitEntry> LoadEntries()
    {
        if (!File.Exists(MappingPath))
        {
            throw new FileNotFoundException("缺少立绘切换清单", MappingPath);
        }

        string[] lines = File.ReadAllLines(MappingPath);
        if (lines.Length <= 1)
        {
            throw new InvalidOperationException("立绘切换清单为空：" + MappingPath);
        }

        List<PortraitEntry> entries = new List<PortraitEntry>();
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                continue;
            }

            string[] columns = lines[i].Split('\t');
            if (columns.Length < 6)
            {
                throw new InvalidOperationException("立绘切换清单格式错误，第 " + (i + 1) + " 行：" + lines[i]);
            }

            entries.Add(new PortraitEntry
            {
                HeadFile = columns[1],
                ProjectName = columns[2],
                UniqueName = columns[5]
            });
        }

        if (entries.Count == 0)
        {
            throw new InvalidOperationException("立绘切换清单没有有效条目：" + MappingPath);
        }

        return entries;
    }

    /// <summary>
    /// 方法说明：判断当前 Head 目录是否完全等于某个已登记变体。
    /// 参数说明：entries 表示需要检查的头像清单。
    /// 返回说明：返回当前变体；如果不是完整原始或完整率土状态，则返回 null。
    /// </summary>
    private static PortraitVariant? GetCurrentVariant(IReadOnlyList<PortraitEntry> entries)
    {
        bool isOriginal = IsVariantActive(PortraitVariant.Original, entries);
        bool isStzbPortrait = IsVariantActive(PortraitVariant.StzbPortrait, entries);

        if (isOriginal && !isStzbPortrait)
        {
            return PortraitVariant.Original;
        }

        if (isStzbPortrait && !isOriginal)
        {
            return PortraitVariant.StzbPortrait;
        }

        return null;
    }

    /// <summary>
    /// 方法说明：检查当前 Head 目录是否与指定变体逐文件一致。
    /// 参数说明：variant 表示待比较变体；entries 表示需要比较的头像清单。
    /// 返回说明：全部一致时返回 true，否则返回 false。
    /// </summary>
    private static bool IsVariantActive(PortraitVariant variant, IReadOnlyList<PortraitEntry> entries)
    {
        string sourceDirectory = GetVariantDirectory(variant);
        foreach (PortraitEntry entry in entries)
        {
            string sourcePath = Path.Combine(sourceDirectory, entry.HeadFile);
            string targetPath = GetTargetPath(entry);
            if (!File.Exists(sourcePath) || !File.Exists(targetPath))
            {
                return false;
            }

            if (!string.Equals(ComputeSha256(sourcePath), ComputeSha256(targetPath), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 方法说明：校验指定变体中的头像源文件是否存在。
    /// 参数说明：sourceDirectory 表示变体目录；entry 表示待校验头像条目。
    /// 返回说明：无返回值；缺失文件时抛出异常。
    /// </summary>
    private static void ValidateVariantFile(string sourceDirectory, PortraitEntry entry)
    {
        string sourcePath = Path.Combine(sourceDirectory, entry.HeadFile);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("缺少变体头像：" + entry.ProjectName + " / " + entry.UniqueName, sourcePath);
        }
    }

    /// <summary>
    /// 方法说明：校验 Head 目录中的目标头像文件是否存在。
    /// 参数说明：entry 表示待校验头像条目。
    /// 返回说明：无返回值；缺失文件时抛出异常。
    /// </summary>
    private static void ValidateTargetFile(PortraitEntry entry)
    {
        string targetPath = GetTargetPath(entry);
        if (!File.Exists(targetPath))
        {
            throw new FileNotFoundException("缺少目标头像：" + entry.ProjectName, targetPath);
        }
    }

    /// <summary>
    /// 方法说明：复制一个变体头像到 Head 目录。
    /// 参数说明：sourceDirectory 表示变体目录；entry 表示需要复制的头像条目。
    /// 返回说明：无返回值；复制失败时抛出异常。
    /// </summary>
    private static void CopyVariantFile(string sourceDirectory, PortraitEntry entry)
    {
        string sourcePath = Path.Combine(sourceDirectory, entry.HeadFile);
        string targetPath = GetTargetPath(entry);
        File.Copy(sourcePath, targetPath, true);
    }

    /// <summary>
    /// 方法说明：获取头像条目的 Head 目标路径。
    /// 参数说明：entry 表示头像条目。
    /// 返回说明：返回 Unity 工程相对路径。
    /// </summary>
    private static string GetTargetPath(PortraitEntry entry)
    {
        return HeadDirectory + "/" + entry.HeadFile;
    }

    /// <summary>
    /// 方法说明：获取指定头像变体的资源目录。
    /// 参数说明：variant 表示头像变体。
    /// 返回说明：返回 Unity 工程相对路径。
    /// </summary>
    private static string GetVariantDirectory(PortraitVariant variant)
    {
        switch (variant)
        {
            case PortraitVariant.Original:
                return OriginalDirectory;
            case PortraitVariant.StzbPortrait:
                return StzbPortraitDirectory;
            default:
                throw new ArgumentOutOfRangeException("variant", variant, "未知立绘变体");
        }
    }

    /// <summary>
    /// 方法说明：获取头像变体的中文显示名。
    /// 参数说明：variant 表示头像变体。
    /// 返回说明：返回用于 Console 输出的中文名称。
    /// </summary>
    private static string GetVariantLabel(PortraitVariant variant)
    {
        switch (variant)
        {
            case PortraitVariant.Original:
                return "原始头像";
            case PortraitVariant.StzbPortrait:
                return "率土头像";
            default:
                throw new ArgumentOutOfRangeException("variant", variant, "未知立绘变体");
        }
    }

    /// <summary>
    /// 方法说明：计算文件 SHA256，用于判断当前头像属于哪套变体。
    /// 参数说明：path 表示待计算的文件路径。
    /// 返回说明：返回小写十六进制 SHA256 字符串。
    /// </summary>
    private static string ComputeSha256(string path)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(File.ReadAllBytes(path));
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
