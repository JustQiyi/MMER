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

    internal static void ForgeModSeparator(ZipArchiveEntry forgeModTomlEntry, string jarFile)
    {
        string tomlContent;
        using (var stream = forgeModTomlEntry.Open())
        using (var reader = new StreamReader(stream))
        {
            tomlContent = reader.ReadToEnd();
        }

        // TODO: 到这里不会写了 Forge 的 mods.toml 有点复杂
        var fileName = Path.GetFileName(jarFile);
        var destinationPath = Path.Combine(TargetPath, fileName);

        if (File.Exists(destinationPath)) File.Delete(destinationPath);

        File.Copy(jarFile, destinationPath);
        Log($"已复制文件: {jarFile} to {destinationPath}", Success);
    }

    internal enum KeepStatus
    {
        KeepCopy,
        KeepSkip,
        Unset
    }
}