/*
 * Minecraft Mod Environment Separators
 * 作者: JustQiyi
 * 使用MIT协议分发.
 */

using System.IO.Compression;
using static MMES.Logger;
using static MMES.Logger.LogLevel;
using static MMES.Variables;
using static MMES.Separators;

namespace MMES;

public class Program
{
    private const string VerCode = "v1.0.3";

    private const string Logo = $@"
███╗   ███╗███╗   ███╗███████╗███████╗
████╗ ████║████╗ ████║██╔════╝██╔════╝
██╔████╔██║██╔████╔██║█████╗  ███████╗
██║╚██╔╝██║██║╚██╔╝██║██╔══╝  ╚════██║
██║ ╚═╝ ██║██║ ╚═╝ ██║███████╗███████║
╚═╝     ╚═╝╚═╝     ╚═╝╚══════╝╚══════╝
MinecraftModEnvironmentSeparators {VerCode}
支持的模组加载器: Fabric、Quilt、NeoForge";

    private const string HelpMessage = @"帮助信息
setTargetPath: 设定目标目录
start: 开始复制(将询问要从哪里分离[复制])
exit: 退出本程序
help: 查看此消息";

    public static async Task Main(string[] args)
    {
        Log(Logo);
        if (!Directory.Exists(TargetPath)) Directory.CreateDirectory(TargetPath);
        while (ProgramRun)
        {
            Thread.CurrentThread.Name = "Main";
            Log("输入\"help\"以查看可用命令。");
            Log($"当前目标位置:{TargetPath}。");
            Console.Write("> ");
            var command = Console.ReadLine();
            switch (command)
            {
                case "setTargetPath":
                    while (true)
                    {
                        Log("请输入位置,输入cancel以取消:");
                        Console.Write("> ");
                        var enteredString = Console.ReadLine();
                        if (enteredString == null)
                        {
                            Log("请输入文本.", Error);
                        }
                        else if (enteredString == "cancel")
                        {
                            break;
                        }
                        else if (!Path.Exists(enteredString))
                        {
                            Log("路径不存在，请重试.", Error);
                        }
                        else
                        {
                            TargetPath = enteredString;
                            Log($"已设定为{TargetPath}", Success);
                            break;
                        }
                    }

                    break;
                case "exit":
                    ProgramRun = false;
                    break;
                default:
                    Log("无效的命令，请重试。输入\"help\"查看可用命令。", Error);
                    break;
                case "help":
                    Log(HelpMessage);
                    break;
                case "start":
                    while (true)
                    {
                        Log("请输入位置,输入cancel以取消:");
                        Console.Write("> ");
                        var enteredString = Console.ReadLine();
                        if (enteredString == null)
                        {
                            Log("请输入文本.", Error);
                        }
                        else if (enteredString == "cancel")
                        {
                            break;
                        }
                        else if (!Path.Exists(enteredString))
                        {
                            Log("路径不存在，请重试.", Error);
                        }
                        else
                        {
                            Log($"开始执行任务：{enteredString}到{TargetPath}");
                            await TaskWorker(enteredString);
                            break;
                        }
                    }

                    break;
            }
        }
    }

    /// <summary>
    ///     执行任务
    /// </summary>
    /// <param name="path">模组文件夹</param>
    /// <returns>Task</returns>
    public static Task TaskWorker(string path)
    {
        return Task.Run(() =>
        {
            Thread.CurrentThread.Name = "TaskWorker";
            try
            {
                var jarFiles = Directory.GetFiles(path, "*.jar", SearchOption.AllDirectories);
                foreach (var jarFile in jarFiles)
                    try
                    {
                        using var archive = ZipFile.OpenRead(jarFile);
                        Log($"读取文件 {jarFile}");

                        /* WARN: 不保证所有的NeoForge模组都会有neoforge.mods.toml
                         * 即无法保证正确识别所有NeoForge模组!(例如SinytraConnector)
                         */
                        // Fabric
                        var fabricModJsonEntry = archive.Entries.FirstOrDefault(e => e.Name == "fabric.mod.json");
                        if (fabricModJsonEntry != null)
                        {
                            FabricModSeparator(fabricModJsonEntry, jarFile);
                        }
                        else
                        {
                            // NeoForge
                            var neoForgeModTomlEntry =
                                archive.Entries.FirstOrDefault(e => e.Name == "neoforge.mods.toml");
                            if (neoForgeModTomlEntry != null)
                            {
                                NeoForgeModSeparator(neoForgeModTomlEntry, jarFile);
                                CopiedCount++;
                            }
                            else
                            {
                                Log($"{jarFile}既不是Forge也不是Fabric模组，已跳过", Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"在对{jarFile}执行操作时发生错误！错误信息 {ex.Message}", Error);
                    }

                Log($"任务已完成，共{jarFiles.Length}个文件，复制了{CopiedCount}个文件到指定文件夹。", Success);
                CopiedCount = 0;
            }
            catch (Exception ex)
            {
                Log($"执行任务时出现错误: {ex.Message}", Error);
            }
        });
    }
}