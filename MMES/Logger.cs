namespace MMES;

internal struct Logger
{
    internal enum LogLevel
    {
        Info,
        Success,
        Warn,
        Error
    }

    /// <summary>
    ///     记录日志
    /// </summary>
    /// <param name="content">内容</param>
    /// <param name="logLevel">等级(默认Info)</param>
    internal static void Log(string content, LogLevel logLevel = LogLevel.Info)
    {
        // TODO: 将日志内容写入文件
        Thread.CurrentThread.Name ??= "Main";
        var prefix = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{Thread.CurrentThread.Name}/{logLevel.ToString()}] ";
        Console.ForegroundColor = logLevel switch
        {
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Warn => ConsoleColor.Yellow,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Success => ConsoleColor.Green,
            _ => ConsoleColor.White
        };
        Console.WriteLine(prefix + content);
    }
}