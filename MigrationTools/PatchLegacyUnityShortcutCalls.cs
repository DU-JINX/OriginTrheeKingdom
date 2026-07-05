using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

/// <summary>
/// 修补 Unity4 插件 DLL 中已被 Unity 6000 删除的 Component 快捷属性调用。
/// </summary>
public static class PatchLegacyUnityShortcutCalls
{
    private static readonly Dictionary<string, string> ShortcutReturnTypes = new Dictionary<string, string>
    {
        { "get_renderer", "UnityEngine.Renderer" },
        { "get_camera", "UnityEngine.Camera" },
        { "get_audio", "UnityEngine.AudioSource" },
        { "get_collider", "UnityEngine.Collider" },
        { "get_light", "UnityEngine.Light" },
        { "get_rigidbody", "UnityEngine.Rigidbody" },
        { "get_animation", "UnityEngine.Animation" }
    };

    /// <summary>
    /// 命令行入口。
    /// </summary>
    /// <param name="args">参数 1 是待修补 DLL 路径，参数 2 是 UnityEngine 托管 DLL 目录。</param>
    /// <returns>进程退出码；0 表示成功，非 0 表示失败。</returns>
    public static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: PatchLegacyUnityShortcutCalls <plugin.dll> <unity-engine-managed-dir>");
            return 2;
        }

        string pluginPath = Path.GetFullPath(args[0]);
        string engineDir = Path.GetFullPath(args[1]);
        string backupPath = pluginPath + ".unity4-original";
        if (!File.Exists(backupPath))
        {
            File.Copy(pluginPath, backupPath);
        }

        DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(Path.GetDirectoryName(pluginPath));
        resolver.AddSearchDirectory(engineDir);
        ReaderParameters readerParameters = new ReaderParameters { AssemblyResolver = resolver, ReadWrite = false, InMemory = true };
        AssemblyDefinition pluginAssembly = AssemblyDefinition.ReadAssembly(pluginPath, readerParameters);
        List<ModuleDefinition> engineModules = LoadEngineModules(engineDir);

        int patched = PatchAssembly(pluginAssembly, engineModules);
        string patchedPath = pluginPath + ".patched";
        pluginAssembly.Write(patchedPath);
        File.Copy(patchedPath, pluginPath, true);
        File.Delete(patchedPath);
        Console.WriteLine("patched_calls=" + patched);
        Console.WriteLine("backup=" + backupPath);
        return 0;
    }

    /// <summary>
    /// 遍历程序集并替换旧 Unity4 Component 快捷属性。
    /// </summary>
    /// <param name="pluginAssembly">待修补的插件程序集。</param>
    /// <param name="engineModules">当前 UnityEngine 托管模块列表。</param>
    /// <returns>被替换的 IL 调用数量。</returns>
    private static int PatchAssembly(AssemblyDefinition pluginAssembly, List<ModuleDefinition> engineModules)
    {
        TypeDefinition componentType = FindType(engineModules, "UnityEngine.Component");
        MethodDefinition getComponentDefinition = FindGenericGetComponent(componentType);
        Dictionary<string, TypeReference> returnTypes = BuildReturnTypes(pluginAssembly.MainModule, engineModules);
        int patched = 0;

        foreach (TypeDefinition type in pluginAssembly.MainModule.Types)
        {
            patched += PatchType(type, pluginAssembly.MainModule, getComponentDefinition, returnTypes);
        }

        return patched;
    }

    /// <summary>
    /// 递归修补一个类型及其嵌套类型。
    /// </summary>
    /// <param name="type">待修补类型。</param>
    /// <param name="module">待修补模块。</param>
    /// <param name="getComponentDefinition">当前 Unity 的泛型 GetComponent 定义。</param>
    /// <param name="returnTypes">旧快捷属性到现代组件类型的映射。</param>
    /// <returns>被替换的 IL 调用数量。</returns>
    private static int PatchType(TypeDefinition type, ModuleDefinition module, MethodDefinition getComponentDefinition, Dictionary<string, TypeReference> returnTypes)
    {
        int patched = 0;
        foreach (MethodDefinition method in type.Methods)
        {
            if (!method.HasBody)
            {
                continue;
            }

            foreach (Instruction instruction in method.Body.Instructions)
            {
                MethodReference oldCall = instruction.Operand as MethodReference;
                if (oldCall == null || !ShortcutReturnTypes.ContainsKey(oldCall.Name))
                {
                    continue;
                }
                if (oldCall.DeclaringType == null || oldCall.DeclaringType.FullName != "UnityEngine.Component")
                {
                    continue;
                }

                GenericInstanceMethod replacement = new GenericInstanceMethod(module.ImportReference(getComponentDefinition));
                replacement.GenericArguments.Add(returnTypes[oldCall.Name]);
                instruction.OpCode = OpCodes.Callvirt;
                instruction.Operand = replacement;
                patched++;
            }
        }

        foreach (TypeDefinition nested in type.NestedTypes)
        {
            patched += PatchType(nested, module, getComponentDefinition, returnTypes);
        }

        return patched;
    }

    /// <summary>
    /// 构建旧快捷属性返回类型映射。
    /// </summary>
    /// <param name="targetModule">待修补模块。</param>
    /// <param name="engineModules">当前 UnityEngine 托管模块列表。</param>
    /// <returns>旧快捷属性名到导入后类型引用的映射。</returns>
    private static Dictionary<string, TypeReference> BuildReturnTypes(ModuleDefinition targetModule, List<ModuleDefinition> engineModules)
    {
        Dictionary<string, TypeReference> result = new Dictionary<string, TypeReference>();
        foreach (KeyValuePair<string, string> pair in ShortcutReturnTypes)
        {
            TypeDefinition type = FindType(engineModules, pair.Value);
            result[pair.Key] = targetModule.ImportReference(type);
        }
        return result;
    }

    /// <summary>
    /// 读取 UnityEngine 托管目录下的全部模块。
    /// </summary>
    /// <param name="engineDir">UnityEngine 托管 DLL 目录。</param>
    /// <returns>模块定义列表。</returns>
    private static List<ModuleDefinition> LoadEngineModules(string engineDir)
    {
        List<ModuleDefinition> modules = new List<ModuleDefinition>();
        foreach (string dllPath in Directory.GetFiles(engineDir, "UnityEngine*.dll"))
        {
            modules.Add(AssemblyDefinition.ReadAssembly(dllPath).MainModule);
        }
        return modules;
    }

    /// <summary>
    /// 在多个模块中查找指定完整类型名。
    /// </summary>
    /// <param name="modules">待搜索模块列表。</param>
    /// <param name="fullName">完整类型名。</param>
    /// <returns>找到的类型定义。</returns>
    private static TypeDefinition FindType(List<ModuleDefinition> modules, string fullName)
    {
        foreach (ModuleDefinition module in modules)
        {
            TypeDefinition found = FindTypeOrNull(module, fullName);
            if (found != null)
            {
                return found;
            }
        }
        throw new InvalidOperationException("Type not found: " + fullName);
    }

    /// <summary>
    /// 查找指定完整类型名。
    /// </summary>
    /// <param name="module">待搜索模块。</param>
    /// <param name="fullName">完整类型名。</param>
    /// <returns>找到的类型定义；未找到返回空。</returns>
    private static TypeDefinition FindTypeOrNull(ModuleDefinition module, string fullName)
    {
        foreach (TypeDefinition type in module.Types)
        {
            TypeDefinition found = FindTypeRecursive(type, fullName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    /// <summary>
    /// 递归查找指定完整类型名。
    /// </summary>
    /// <param name="type">当前类型。</param>
    /// <param name="fullName">完整类型名。</param>
    /// <returns>找到的类型定义；未找到返回空。</returns>
    private static TypeDefinition FindTypeRecursive(TypeDefinition type, string fullName)
    {
        if (type.FullName == fullName)
        {
            return type;
        }
        foreach (TypeDefinition nested in type.NestedTypes)
        {
            TypeDefinition found = FindTypeRecursive(nested, fullName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }

    /// <summary>
    /// 查找当前 Unity 的 Component.GetComponent 泛型方法。
    /// </summary>
    /// <param name="componentType">UnityEngine.Component 类型定义。</param>
    /// <returns>GetComponent 泛型方法定义。</returns>
    private static MethodDefinition FindGenericGetComponent(TypeDefinition componentType)
    {
        foreach (MethodDefinition method in componentType.Methods)
        {
            if (method.Name == "GetComponent" && method.HasGenericParameters && method.Parameters.Count == 0)
            {
                return method;
            }
        }
        throw new InvalidOperationException("Component.GetComponent<T>() not found.");
    }
}
