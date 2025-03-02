/*
 * Minecraft Mod Environment Separators
 * 作者: JustQiyi
 * 使用MIT协议分发.
 */

using System.IO.Compression;
using static MMES.Logger;
using static MMES.Logger.LogLevel;
using static MMES.Variables;
using static MMES.Handler;

namespace MMES;

public class Program
{
    private const string VerCode = "v1.1.0";
    private static bool _isProcessing;

    private const string Logo = $@"
███╗   ███╗███╗   ███╗███████╗███████╗
████╗ ████║████╗ ████║██╔════╝██╔════╝
██╔████╔██║██╔████╔██║█████╗  ███████╗
██║╚██╔╝██║██║╚██╔╝██║██╔══╝  ╚════██║
██║ ╚═╝ ██║██║ ╚═╝ ██║███████╗███████║
╚═╝     ╚═╝╚═╝     ╚═╝╚══════╝╚══════╝
MinecraftModEnvironmentSeparators {VerCode}
支持的模组加载器: Fabric、Quilt、(Neo)Forge";

    private const string HelpMessage = @"帮助信息
setTargetPath: 设定目标目录
start: 开始复制(将询问要从哪里分离[复制])
exit: 退出本程序
help: 查看此消息";

    public static async Task Main(string[] args)
    {
        Log(Logo);
        Directory.CreateDirectory(TargetPath);
        while (ProgramRun)
        {
            Thread.CurrentThread.Name = "Main";
            if (!_isProcessing)
            {
                ShowPrompt();
                var command = Console.ReadLine()?.Trim();
                HandleCommand(command);
            }
            else
            {
                await Task.Delay(50); // 降低CPU占用
            }
        }
    }

    private static void ShowPrompt()
    {
        Log("输入\"help\"以查看可用命令。");
        Log($"当前目标位置: {TargetPath}");
        Console.Write("> ");
    }

    private static void HandleCommand(string? command)
    {
        switch (command?.ToLower())
        {
            case "settargetpath":
                SetTargetPathCommand();
                break;
            case "start":
                StartCommand();
                break;
            case "exit":
                ProgramRun = false;
                break;
            case "help":
                Log(HelpMessage);
                break;
            default:
                if (!string.IsNullOrEmpty(command))
                    Log("无效命令，输入\"help\"查看帮助", Error);
                break;
        }
    }

    private static void SetTargetPathCommand()
    {
        var path = PromptForPath("请输入目标路径（输入 cancel 取消）:");
        if (path != null)
        {
            TargetPath = path;
            Log($"目标路径已设置为: {TargetPath}", Success);
        }
    }

    private static async void StartCommand()
    {
        var sourcePath = PromptForPath("请输入源路径（输入 cancel 取消）:");
        if (sourcePath == null) return;

        _isProcessing = true;
        try
        {
            Log($"开始处理: {sourcePath} → {TargetPath}");
            await ProcessModsWithLock(sourcePath);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private static string? PromptForPath(string prompt)
    {
        while (true)
        {
            Log(prompt);
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input)) continue;
            if (input.Equals("cancel", StringComparison.OrdinalIgnoreCase)) return null;
            if (Directory.Exists(input)) return input;

            Log("路径不存在，请检查后重试", Error);
        }
    }

    private static Task ProcessModsWithLock(string sourcePath)
    {
        return Task.Run(() =>
        {
            Thread.CurrentThread.Name = "TaskWorker";
            try
            {
                var jarFiles = Directory.GetFiles(sourcePath, "*.jar", SearchOption.AllDirectories);
                foreach (var jarFile in jarFiles)
                {
                    try
                    {
                        using var zip = ZipFile.OpenRead(jarFile);
                        Log($"读取文件 {jarFile}", Info);

                        var modType = DetectModType(zip);
                        if (modType == ModType.Unknown)
                        {
                            Log($"无法识别模组类型: {Path.GetFileName(jarFile)}", Warn);
                            continue;
                        }

                        ProcessModFile(zip, jarFile, modType);
                    }
                    catch (Exception ex)
                    {
                        Log($"处理文件 {Path.GetFileName(jarFile)} 时出错: {ex.Message}", Error);
                    }
                }
                Log($"任务完成! 共处理 {jarFiles.Length} 个文件，成功复制 {CopiedCount} 个文件", Success);
                Interlocked.Exchange(ref CopiedCount, 0);
            }
            catch (Exception ex)
            {
                Log($"处理过程中发生严重错误: {ex.Message}", Error);
            }
        });
    }

    private static ModType DetectModType(ZipArchive zip)
    {
        foreach (var entry in zip.Entries)
        {
            switch (entry.FullName)
            {
                case "fabric.mod.json":
                    return ModType.Fabric;
                case "META-INF/neoforge.mods.toml":
                    return ModType.NeoForge;
                case "META-INF/mods.toml":
                    return ModType.Forge;
            }
        }
        return ModType.Unknown;
    }

    private static void ProcessModFile(ZipArchive zip, string jarFile, ModType modType)
    {
        switch (modType)
        {
            case ModType.Fabric:
                var fabricEntry = zip.GetEntry("fabric.mod.json");
                FabricModReplicator(fabricEntry!, jarFile);
                break;
            case ModType.Forge:
                var forgeEntry = zip.GetEntry("META-INF/mods.toml");
                ForgeModReplicator(forgeEntry!, jarFile);
                break;
            case ModType.NeoForge:
                var neoForgeEntry = zip.GetEntry("META-INF/neoforge.mods.toml");
                NeoForgeModReplicator(neoForgeEntry!, jarFile);
                break;
        }
    }
}

public enum ModType { Fabric, Forge, NeoForge, Unknown }