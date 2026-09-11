using System.Reflection;

namespace Sites.Cp;

public static class BuildStamp
{
    public static string Text { get; } = Read();

    private static string Read()
    {
        var asm = Assembly.GetEntryAssembly() ?? typeof(BuildStamp).Assembly;
        return asm.GetCustomAttributes<AssemblyMetadataAttribute>()
                   .FirstOrDefault(a => a.Key == "BuildTimestamp")
                   ?.Value
               ?? "";
    }
}
