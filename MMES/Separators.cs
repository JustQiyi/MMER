using System.IO.Compression;
using Newtonsoft.Json.Linq;
using static MMES.Logger;
using static MMES.Logger.LogLevel;
using static MMES.Variables;

namespace MMES;

internal class Separators
{
    internal static void FabricModSeparator(ZipArchiveEntry fabricModJsonEntry, string jarFile)
    {
        string jsonContent;
        using (var stream = fabricModJsonEntry.Open())
        using (var reader = new StreamReader(stream))
        {
            jsonContent = reader.ReadToEnd();
        }

        var jsonObject = JObject.Parse(jsonContent);

        var environmentJObject = jsonObject["environment"];
        var environment = environmentJObject?.ToString();
        if (environmentJObject == null && Variables.KeepStatus == KeepStatus.KeepCopy)
        {
            Log($"已按照之前的选项，将Environment为null的文件{jarFile}视为'*'模组", Warn);
            environment = "*";
        }

        if (environmentJObject == null && Variables.KeepStatus == KeepStatus.KeepSkip)
            Log($"已按照之前的选项，跳过了Environment为null的文件{jarFile}", Warn);

        if (environmentJObject == null && Variables.KeepStatus == KeepStatus.Unset)
        {
            Log($"{jarFile}的Environment是null，是否复制到TargetPath?", Warn);
            Log("输入y以继续，n以跳过该文件，k<y/n>以对后续文件执行同样操作。(默认:y)");
            Console.Write("> ");
            var enteredString = Console.ReadLine();
            switch (enteredString)
            {
                default:
                    Variables.KeepStatus = KeepStatus.Unset;
                    environment = "*";
                    break;
                case "n":
                    Variables.KeepStatus = KeepStatus.Unset;
                    break;
                case "ky":
                    Variables.KeepStatus = KeepStatus.KeepCopy;
                    environment = "*";
                    break;
                case "kn":
                    Variables.KeepStatus = KeepStatus.KeepSkip;
                    break;
            }
        }

        if (environment == "*" || environment == "server")
        {
            var fileName = Path.GetFileName(jarFile);
            var destinationPath = Path.Combine(TargetPath, fileName);

            if (File.Exists(destinationPath)) File.Delete(destinationPath);

            File.Copy(jarFile, destinationPath);
            Log($"已复制文件: {jarFile} to {destinationPath}", Success);
            CopiedCount++;
        }
        else
        {
            environment ??= "null";
            Log($"{jarFile}的Environment是{environment}，跳过", Warn);
        }
    }

    // feat: neoforge supported
    // 大致实现方法：解析neoForgeModTomlEntry
    // 获取 modId 为 Minecraft 的 Dependency 的 side，若 side == "both" 或 "server" 则复制
    /* Example:
     * [dependencies.Minecraft]
     * modId = "minecraft"
     * version = "1.17.1"
     * side = "BOTH" or "CLIENT" or "SERVER" :: Important
     */
    internal static void NeoForgeModSeparator(ZipArchiveEntry neoForgeModTomlEntry, string jarFile)
    {
        string tomlContent;
        using (var stream = neoForgeModTomlEntry.Open())
        using (var reader = new StreamReader(stream))
        {
            tomlContent = reader.ReadToEnd();
        }

        var tomlLines = tomlContent.Split('\n');
        string? side = null;

        foreach (var line in tomlLines)
            if (line.Trim().StartsWith("modId = \"minecraft\"", StringComparison.OrdinalIgnoreCase))
            {
                for (var i = Array.IndexOf(tomlLines, line); i < tomlLines.Length; i++)
                    if (tomlLines[i].Trim().StartsWith("side = ", StringComparison.OrdinalIgnoreCase))
                    {
                        side = tomlLines[i].Split('=')[1].Trim().Trim('"').ToLower();
                        break;
                    }

                break;
            }

        if (side == "both" || side == "server")
        {
            var fileName = Path.GetFileName(jarFile);
            var destinationPath = Path.Combine(TargetPath, fileName);

            if (File.Exists(destinationPath)) File.Delete(destinationPath);

            File.Copy(jarFile, destinationPath);
            Log($"已复制文件: {jarFile} to {destinationPath}", Success);
        }
        else
        {
            side ??= "null";
            Log($"{jarFile}的 side 是{side}，跳过", Warn);
        }
    }

    internal enum KeepStatus
    {
        KeepCopy,
        KeepSkip,
        Unset
    }
}