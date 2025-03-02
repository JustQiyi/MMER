using Newtonsoft.Json.Linq;
using System.IO.Compression;
using Tomlyn;
using Tomlyn.Model;
using static MMES.Logger;
using static MMES.Logger.LogLevel;
using static MMES.Variables;

namespace MMES;

internal class Handler
{
    internal static void FabricModReplicator(ZipArchiveEntry entry, string jarFile)
    {
        var json = ParseJson(entry);
        var environment = json["environment"]?.ToString();

        HandleEnvironmentDecision(
            environment,
            jarFile,
            validValues: new[] { "*", "server" },
            missingMessage: $"{jarFile}的Environment是null",
            skipMessage: $"{jarFile}的Environment是{{0}}，跳过"
        );
    }

    internal static void ForgeModReplicator(ZipArchiveEntry entry, string jarFile)
    {
        var toml = ParseToml(entry, jarFile);

        // 修正后的TOML解析方式
        if (toml.TryGetValue("mods", out var modsObj) &&
            modsObj is TomlTableArray modsArray &&
            modsArray.Count > 0)
        {
            var shouldCopy = CheckModSides(modsArray);
            HandleSideDecision(shouldCopy, jarFile, $"{jarFile} 是客户端专用模组");
        }
        else
        {
            HandleMissingModsDeclaration(jarFile);
        }
    }

    internal static void NeoForgeModReplicator(ZipArchiveEntry entry, string jarFile)
    {
        var toml = ParseToml(entry, jarFile);

        // 使用正确的TOML访问方式
        var dependencies = toml.TryGetValue("dependencies", out var depsObj)
            ? depsObj as TomlTable
            : null;

        var minecraftDep = dependencies?.TryGetValue("Minecraft", out var mcObj) == true
            ? mcObj as TomlTable
            : null;

        var side = minecraftDep?.TryGetValue("side", out var sideObj) == true
            ? sideObj.ToString().ToLower()
            : "client";

        HandleSideDecision(
            side == "both" || side == "server",
            jarFile,
            $"{jarFile} 的Minecraft依赖未配置服务端支持"
        );
    }

    private static void HandleMissingModsDeclaration(string jarFile)
    {
        string message = $"{jarFile} 缺少mods声明";

        if (Variables.KeepStatus == KeepStatus.KeepCopy)
        {
            CopyJar(jarFile);
            Log($"{message}，已强制复制", Warn);
        }
        else if (Variables.KeepStatus == KeepStatus.KeepSkip)
        {
            Log($"{message}，已跳过", Warn);
        }
        else
        {
            Log($"{message}，是否复制？(y/n/k[y/n])", Warn);
            if (PromptUser(jarFile)) CopyJar(jarFile);
        }
    }
    #region 核心逻辑
    private static JObject ParseJson(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return JObject.Parse(reader.ReadToEnd());
    }

    private static TomlTable ParseToml(ZipArchiveEntry entry, string jarFile)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var toml = Toml.Parse(reader.ReadToEnd(), sourcePath: jarFile);

        if (toml.HasErrors)
        {
            Log($"无效的TOML格式: {jarFile}", Error);
            throw new FormatException("Invalid TOML format");
        }

        return toml.ToModel();
    }

    private static bool CheckModSides(IEnumerable<TomlTable> modEntries)
    {
        foreach (var mod in modEntries)
        {
            var side = mod.TryGetValue("side", out var sideObj)
                ? sideObj.ToString().ToLower()
                : "both";

            if (side == "server" || side == "both")
                return true;
        }
        return false;
    }
    #endregion

    #region 用户交互
    private static void HandleEnvironmentDecision(
        string? environment,
        string jarFile,
        string[] validValues,
        string missingMessage,
        string skipMessage)
    {
        var actualValue = HandleNullCase(
            environment,
            jarFile,
            missingMessage,
            defaultValue: "*"
        );

        if (validValues.Contains(actualValue?.ToLower()))
        {
            CopyJar(jarFile);
        }
        else
        {
            Log(string.Format(skipMessage, actualValue ?? "null"), Warn);
        }
    }

    private static void HandleSideDecision(
        bool shouldCopy,
        string jarFile,
        string clientMessage)
    {
        if (shouldCopy)
        {
            CopyJar(jarFile);
        }
        else
        {
            HandleClientCase(jarFile, clientMessage);
        }
    }

    private static string? HandleNullCase(
        string? value,
        string jarFile,
        string message,
        string defaultValue = "*")
    {
        if (value != null) return value;

        switch (Variables.KeepStatus)
        {
            case KeepStatus.KeepCopy:
                Log($"{message}，已强制复制", Warn);
                return defaultValue;

            case KeepStatus.KeepSkip:
                Log($"{message}，已跳过", Warn);
                return null;

            default:
                Log($"{message}，是否复制到TargetPath? (y/n/k[y/n],默认:\"y\")", Warn);
                return PromptUser(jarFile) ? defaultValue : null;
        }
    }

    private static void HandleClientCase(string jarFile, string message)
    {
        switch (Variables.KeepStatus)
        {
            case KeepStatus.KeepCopy:
                CopyJar(jarFile);
                Log($"{message}，已强制复制", Warn);
                break;

            case KeepStatus.KeepSkip:
                Log($"{message}，已跳过", Warn);
                break;

            default:
                Log($"{message}，是否复制？(y/n/k[y/n])", Warn);
                if (PromptUser(jarFile)) CopyJar(jarFile);
                break;
        }
    }

    private static bool PromptUser(string jarFile)
    {
        Console.Write("> ");
        var input = Console.ReadLine()?.ToLower();

        switch (input)
        {
            case "ky":
                Variables.KeepStatus = KeepStatus.KeepCopy;
                return true;

            case "kn":
                Variables.KeepStatus = KeepStatus.KeepSkip;
                return false;

            case "n":
                return false;

            default: // 包括y和空输入
                return true;
        }
    }
    #endregion

    #region 文件操作
    private static void CopyJar(string jarFile)
    {
        var dest = Path.Combine(TargetPath, Path.GetFileName(jarFile));

        try
        {
            File.Copy(jarFile, dest, overwrite: true);
            Log($"已复制: {jarFile} => {dest}", Success);
            Interlocked.Increment(ref CopiedCount);
        }
        catch (Exception ex)
        {
            Log($"复制失败: {ex.Message}", Error);
        }
    }
    #endregion

    internal enum KeepStatus { KeepCopy, KeepSkip, Unset }
}