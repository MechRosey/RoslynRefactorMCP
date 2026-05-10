using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

[McpServerToolType]
internal static class RoslynatorTools
{
    [McpServerTool(Name = "roslynator-analyze")]
    [Description("Run Roslynator diagnostics analysis on a solution or project.")]
    public static Task<string> Analyze(
        [Description("Absolute path to .sln or .csproj.")] string solution)
        => RunRoslynatorAsync(["analyze", solution]);

    [McpServerTool(Name = "roslynator-fix")]
    [Description("Apply Roslynator auto-fixes to a solution or project. Run roslynator-analyze first to see what will be fixed.")]
    public static Task<string> Fix(
        [Description("Absolute path to .sln or .csproj.")] string solution)
        => RunRoslynatorAsync(["fix", solution]);

    [McpServerTool(Name = "roslynator-format")]
    [Description("Format code in a solution or project using Roslynator.")]
    public static Task<string> Format(
        [Description("Absolute path to .sln or .csproj.")] string solution)
        => RunRoslynatorAsync(["format", solution]);

    [McpServerTool(Name = "roslynator-spellcheck")]
    [Description("Check spelling of identifiers and comments in a solution or project via Roslynator.")]
    public static Task<string> Spellcheck(
        [Description("Absolute path to .sln or .csproj.")] string solution)
        => RunRoslynatorAsync(["spellcheck", solution]);

    [McpServerTool(Name = "roslynator-lloc")]
    [Description("Count logical lines of code in a solution or project via Roslynator.")]
    public static Task<string> Lloc(
        [Description("Absolute path to .sln or .csproj.")] string solution)
        => RunRoslynatorAsync(["lloc", solution]);

    private static async Task<string> RunRoslynatorAsync(IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo("roslynator")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string a in args) psi.ArgumentList.Add(a);

        using Process proc = Process.Start(psi)
            ?? throw new InvalidOperationException(
                "roslynator not found. Install with: dotnet tool install -g Roslynator.DotNet.Cli");

        Task<string> stdout = proc.StandardOutput.ReadToEndAsync();
        Task<string> stderr = proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();

        StringBuilder sb = new();
        string o = await stdout, e = await stderr;
        if (!string.IsNullOrWhiteSpace(o)) sb.AppendLine(o);
        if (!string.IsNullOrWhiteSpace(e)) sb.AppendLine(e);
        if (proc.ExitCode != 0) sb.AppendLine($"Exit code: {proc.ExitCode}");
        return sb.ToString().TrimEnd();
    }
}
