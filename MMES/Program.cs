/*
 * Minecraft Mod Environment Separators
 * MMES v1.0.1 by ClouderyStudio
 * Licensed by MIT.
 */

#region usings
using Newtonsoft.Json.Linq;
#endregion

#region Datas
using System.IO.Compression;

const string verCode = "v1.0.1";
const string logo = $@"███╗   ███╗███╗   ███╗███████╗███████╗
████╗ ████║████╗ ████║██╔════╝██╔════╝
██╔████╔██║██╔████╔██║█████╗  ███████╗
██║╚██╔╝██║██║╚██╔╝██║██╔══╝  ╚════██║
██║ ╚═╝ ██║██║ ╚═╝ ██║███████╗███████║
╚═╝     ╚═╝╚═╝     ╚═╝╚══════╝╚══════╝
MMES {verCode}
支持的模组加载器: Fabric";
var targetPath = AppDomain.CurrentDomain.BaseDirectory;
var programRun = true;
var keepStatus = KeepStatus.Unset;
#endregion

#region ProgramMain
Console.WriteLine(logo);

while (programRun)
{
    Console.WriteLine("请输入模组文件夹.输入exit以退出程序.");
    Console.WriteLine($"输入'setTargetPath'以设定最终位置(当前{targetPath})");
    Console.Write("> ");
    var path = Console.ReadLine();
    switch (path)
    {
        case "setTargetPath":
            var succeed = false;
            while (!succeed)
            {
                Console.WriteLine("请输入位置,输入cancel以取消:");
                Console.Write("> ");
                var enteredString = Console.ReadLine();
                if (enteredString == null)
                {
                    Console.WriteLine("请输入文本.");
                }
                else if (enteredString == "cancel")
                {
                    succeed = true;
                    break;
                }
                else if (!Path.Exists(enteredString))
                {
                    Console.WriteLine("路径不存在，请重试.");
                }
                else
                {
                    targetPath = enteredString;
                    Console.WriteLine($"已设定为{targetPath}");
                    succeed = true;
                    break;
                }
            }

            break;
        case null:
            Console.WriteLine("请输入文本.");
            break;
        case "exit":
            programRun = false;
            break;
        default:
            if (!Path.Exists(path))
            {
                Console.WriteLine("路径或命令不存在,请重试");
                break;
            }

            Console.WriteLine($"开始执行任务：{path}到{targetPath}");
            var jarFiles = Directory.GetFiles(path, "*.jar", SearchOption.AllDirectories);
            foreach (var jarFile in jarFiles)
                try
                {
                    string? jsonContent = null;
                    using (var archive = ZipFile.OpenRead(jarFile))
                    {
                        Console.WriteLine($"读取文件 {jarFile}");

                        var fabricModJsonEntry = archive.Entries.FirstOrDefault(e => e.Name == "fabric.mod.json");
                        if (fabricModJsonEntry != null)
                            using (var stream = fabricModJsonEntry.Open())
                            using (var reader = new StreamReader(stream))
                            {
                                jsonContent = reader.ReadToEnd();
                            }
                        else
                            Console.WriteLine($"{jarFile}不是fabric模组，跳过");
                    }

                    if (jsonContent != null)
                    {
                        var jsonObject = JObject.Parse(jsonContent);

                        var environmentJObject = jsonObject["environment"];
                        var environment = environmentJObject?.ToString();
                        if (environmentJObject == null && keepStatus == KeepStatus.KeepCopy)
                        {
                            Console.WriteLine($"已按照之前的选项，将Environment为null的文件{jarFile}视为'*'模组");
                            environment = "*";
                        }

                        if (environmentJObject == null && keepStatus == KeepStatus.KeepSkip)
                            Console.WriteLine($"已按照之前的选项，跳过了Environment为null的文件{jarFile}");

                        if (environmentJObject == null && keepStatus == KeepStatus.Unset)
                        {
                            Console.WriteLine($"{jarFile}的Environment是null，是否复制到TargetPath?");
                            Console.WriteLine("输入y以继续，n以跳过该文件，k<y/n>以对后续文件执行同样操作。(默认:y)");
                            Console.Write("> ");
                            var enteredString = Console.ReadLine();
                            switch (enteredString)
                            {
                                default:
                                    keepStatus = KeepStatus.Unset;
                                    environment = "*";
                                    break;
                                case "n":
                                    keepStatus = KeepStatus.Unset;
                                    break;
                                case "ky":
                                    keepStatus = KeepStatus.KeepCopy;
                                    environment = "*";
                                    break;
                                case "kn":
                                    keepStatus = KeepStatus.KeepSkip;
                                    break;
                            }
                        }

                        if (environment == "*" || environment == "server")
                        {
                            var fileName = Path.GetFileName(jarFile);
                            var destinationPath = Path.Combine(targetPath, fileName);

                            if (File.Exists(destinationPath)) File.Delete(destinationPath);

                            File.Copy(jarFile, destinationPath);
                            Console.WriteLine($"已复制文件: {jarFile} to {destinationPath}");
                        }
                        else
                        {
                            Console.WriteLine($"{jarFile}的Environment是{environment}，跳过");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing {jarFile}: {ex.Message}");
                }

            Console.WriteLine("任务已完成!");
            break;
    }
}

internal enum KeepStatus
{
    KeepCopy,
    KeepSkip,
    Unset
}
#endregion