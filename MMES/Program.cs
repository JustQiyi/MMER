/*
 * Minecraft Mod Environment Separators
 * MMES v1.0.2 by ClouderyStudio
 * 使用MIT协议分发.
 */
#region Usings
using System.IO.Compression;
using Newtonsoft.Json.Linq;
using static MMES.Logger;
using static MMES.Logger.LogLevel;
#endregion

namespace MMES;

public class Program
{
    private const string VerCode = "v1.0.2";

    private const string Logo = $@"
███╗   ███╗███╗   ███╗███████╗███████╗
████╗ ████║████╗ ████║██╔════╝██╔════╝
██╔████╔██║██╔████╔██║█████╗  ███████╗
██║╚██╔╝██║██║╚██╔╝██║██╔══╝  ╚════██║
██║ ╚═╝ ██║██║ ╚═╝ ██║███████╗███████║
╚═╝     ╚═╝╚═╝     ╚═╝╚══════╝╚══════╝
MinecraftModEnvironmentSeparators {VerCode}
支持的模组加载器: Fabric";

    private static string _targetPath = AppDomain.CurrentDomain.BaseDirectory + "target\\";
    private static bool _programRun = true;
    private static KeepStatus _keepStatus = KeepStatus.Unset;

    public static async Task Main(string[] args)
    {
        Log(Logo);
        if (!Directory.Exists(_targetPath)) Directory.CreateDirectory(_targetPath);
        while (_programRun)
        {
            Thread.CurrentThread.Name = "Main";
            // TODO: 指令化
            Log("请输入模组文件夹.输入exit以退出程序.");
            Log($"输入'setTargetPath'以设定最终位置(当前为{_targetPath})");
            Console.Write("> ");
            var path = Console.ReadLine();
            switch (path)
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
                        else if (enteredString == "cancel") break;
                        else if (!Path.Exists(enteredString))
                        {
                            Log("路径不存在，请重试.", Error);
                        }
                        else
                        {
                            _targetPath = enteredString;
                            Log($"已设定为{_targetPath}", Success);
                            break;
                        }
                    }

                    break;
                case null:
                    Log("请输入文本.", Error);
                    break;
                case "exit":
                    _programRun = false;
                    break;
                default:
                    if (!Path.Exists(path))
                    {
                        Log("路径或命令不存在,请重试", Error);
                        break;
                    }

                    Log($"开始执行任务：{path}到{_targetPath}");
                    await TaskWorker(path);
                    break;
            }
        }
    }
    /// <summary>
    /// 执行任务
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
                var copiedCount = 0;
                foreach (var jarFile in jarFiles)
                    try
                    {
                        string? jsonContent = null;
                        using (var archive = ZipFile.OpenRead(jarFile))
                        {
                            Log($"读取文件 {jarFile}");

                            var fabricModJsonEntry = archive.Entries.FirstOrDefault(e => e.Name == "fabric.mod.json");
                            if (fabricModJsonEntry != null)
                                using (var stream = fabricModJsonEntry.Open())
                                using (var reader = new StreamReader(stream))
                                {
                                    jsonContent = reader.ReadToEnd();
                                }
                            else
                                Log($"{jarFile}不是fabric模组，跳过", Warn);
                        }

                        if (jsonContent != null)
                        {
                            var jsonObject = JObject.Parse(jsonContent);

                            var environmentJObject = jsonObject["environment"];
                            var environment = environmentJObject?.ToString();
                            if (environmentJObject == null && _keepStatus == KeepStatus.KeepCopy)
                            {
                                Log($"已按照之前的选项，将Environment为null的文件{jarFile}视为'*'模组", Warn);
                                environment = "*";
                            }

                            if (environmentJObject == null && _keepStatus == KeepStatus.KeepSkip)
                                Log($"已按照之前的选项，跳过了Environment为null的文件{jarFile}", Warn);

                            if (environmentJObject == null && _keepStatus == KeepStatus.Unset)
                            {
                                Log($"{jarFile}的Environment是null，是否复制到TargetPath?", Warn);
                                Log("输入y以继续，n以跳过该文件，k<y/n>以对后续文件执行同样操作。(默认:y)");
                                Console.Write("> ");
                                var enteredString = Console.ReadLine();
                                switch (enteredString)
                                {
                                    default:
                                        _keepStatus = KeepStatus.Unset;
                                        environment = "*";
                                        break;
                                    case "n":
                                        _keepStatus = KeepStatus.Unset;
                                        break;
                                    case "ky":
                                        _keepStatus = KeepStatus.KeepCopy;
                                        environment = "*";
                                        break;
                                    case "kn":
                                        _keepStatus = KeepStatus.KeepSkip;
                                        break;
                                }
                            }

                            if (environment == "*" || environment == "server")
                            {
                                var fileName = Path.GetFileName(jarFile);
                                var destinationPath = Path.Combine(_targetPath, fileName);

                                if (File.Exists(destinationPath)) File.Delete(destinationPath);

                                File.Copy(jarFile, destinationPath);
                                copiedCount++;
                                Log($"已复制文件: {jarFile} to {destinationPath}", Success);
                            }
                            else
                            {
                                environment ??= "null";
                                Log($"{jarFile}的Environment是{environment}，跳过", Warn);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"在对{jarFile}执行操作时发生错误！错误信息 {ex.Message}", Error);
                    }

                Log($"任务已完成，共{jarFiles.Length}个文件，复制了{copiedCount}个文件到指定文件夹。", Success);
            }
            catch (Exception ex)
            {
                Log($"执行任务时出现错误: {ex.Message}", Error);
            }
        });
    }

    internal enum KeepStatus
    {
        KeepCopy,
        KeepSkip,
        Unset
    }
}