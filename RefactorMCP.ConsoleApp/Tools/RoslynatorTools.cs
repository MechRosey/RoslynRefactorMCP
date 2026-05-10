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
        List<string> allArgs = new List<string>(args);

        // Roslynator 0.12.0 is incompatible with .NET 10+ MSBuild (MSBuildConstants.InvalidPathChars removed).
        // Inject --msbuild-path pointing at the highest compatible SDK (major version below 10).
        string? sdkPath = FindCompatibleSdkPath();
        if (sdkPath != null)
        {
            allArgs.Add("--msbuild-path");
            allArgs.Add(sdkPath);
        }

        var psi = new ProcessStartInfo("roslynator")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string a in allArgs) psi.ArgumentList.Add(a);

        Process proc;
        try
        {
            proc = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null.");
        }
        catch (Exception ex)
        {
            return $"Failed to start roslynator: {ex.Message}\nInstall with: dotnet tool install -g Roslynator.DotNet.Cli";
        }

        using (proc)
        {
            Task<string> stdout = proc.StandardOutput.ReadToEndAsync();
            Task<string> stderr = proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            StringBuilder sb = new();
            string o = await stdout, e = await stderr;
            if (!string.IsNullOrWhiteSpace(o)) sb.AppendLine(o);
            if (!string.IsNullOrWhiteSpace(e)) sb.AppendLine(e);
            // Exit code 1 from analyze means diagnostics found, not a fatal error.
            if (proc.ExitCode > 1) sb.AppendLine($"Exit code: {proc.ExitCode}");
            return sb.ToString().TrimEnd();
        }
    }

    private static string? FindCompatibleSdkPath()
    {
        string dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet");
        string sdksDir = Path.Combine(dotnetRoot, "sdk");
        if (!Directory.Exists(sdksDir)) return null;

        return Directory.GetDirectories(sdksDir)
            .Select(d => (Path: d, Version: TryParseVersion(Path.GetFileName(d))))
            .Where(x => x.Version != null && x.Version.Major < 10)
            .OrderByDescending(x => x.Version)
            .Select(x => x.Path)
            .FirstOrDefault();
    }

    private static Version? TryParseVersion(string s) =>
        Version.TryParse(s, out Version? v) ? v : null;
}
