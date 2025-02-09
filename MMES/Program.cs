/*
 * Minecraft Mod Environment Separators
 * MMES v1.0.0 by ClouderyStudio
 * Licensed by MIT.
 */

#region usings
using Newtonsoft.Json.Linq;
#endregion

#region Datas
using System.IO.Compression;

const string verCode = "v1.0.0";
const string logo = $@"███╗   ███╗███╗   ███╗███████╗███████╗
████╗ ████║████╗ ████║██╔════╝██╔════╝
██╔████╔██║██╔████╔██║█████╗  ███████╗
██║╚██╔╝██║██║╚██╔╝██║██╔══╝  ╚════██║
██║ ╚═╝ ██║██║ ╚═╝ ██║███████╗███████║
╚═╝     ╚═╝╚═╝     ╚═╝╚══════╝╚══════╝
MMES {verCode}";
string targetPath = AppDomain.CurrentDomain.BaseDirectory;
bool programRun = true;
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
            bool unsuccessed = true;
            while (unsuccessed)
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
                    unsuccessed = false;
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
                    unsuccessed = false;
                    break;
                }
            }
            break;
        case null:
            Console.WriteLine("请输入文本.");
            break;
        case "exit":
            programRun = false;
            Console.WriteLine("Goodbye!");
            break;
        default:
            if (!Path.Exists(path))
            {
                Console.WriteLine("路径不存在,请重试");
                break;
            }
            else
            {
                Console.WriteLine($"开始执行任务：{path}到{targetPath}");
                var jarFiles = Directory.GetFiles(path, "*.jar", SearchOption.AllDirectories);
                foreach (var jarFile in jarFiles)
                {
                    try
                    {
                        // 读取fabric.mod.json文件内容
                        string? jsonContent = null;
                        using (var archive = ZipFile.OpenRead(jarFile))
                        {
                            Console.WriteLine($"读取文件 {jarFile}");
                            // 查找fabric.mod.json文件
                            var fabricModJsonEntry = archive.Entries.FirstOrDefault(e => e.Name == "fabric.mod.json");
                            if (fabricModJsonEntry != null)
                            {
                                using (var stream = fabricModJsonEntry.Open())
                                using (var reader = new StreamReader(stream))
                                {
                                    jsonContent = reader.ReadToEnd();
                                }
                            }
                        } // 这里会关闭ZipArchive，释放文件锁

                        // 如果读取到了fabric.mod.json文件内容
                        if (jsonContent != null)
                        {
                            var jsonObject = JObject.Parse(jsonContent);

                            // 检查environment字段
                            var environment = jsonObject["environment"]?.ToString();
                            if (environment == "*" || environment == "server")
                            {
                                // 复制JAR文件到程序执行的位置
                                string fileName = Path.GetFileName(jarFile);
                                string destinationPath = Path.Combine(targetPath, fileName);

                                // 如果目标文件已存在，先删除
                                if (File.Exists(destinationPath))
                                {
                                    File.Delete(destinationPath);
                                }

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
                }
                break;
            }
    }
}
#endregion