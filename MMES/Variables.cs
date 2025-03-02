using static MMER.Handler;

namespace MMER;

internal class Variables
{
    internal static string TargetPath = AppDomain.CurrentDomain.BaseDirectory + "target\\";
    internal static bool ProgramRun = true;
    internal static KeepStatus KeepStatus = KeepStatus.Unset;
    internal static int CopiedCount = 0;
}