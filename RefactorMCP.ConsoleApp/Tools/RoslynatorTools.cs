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
        [Description("Absolute path to .sln or .csproj.")] string solution,
        [Description("Path to MSBuild directory or MSBuild.exe. Required for .NET Framework solutions when auto-detection fails.")] string? msbuildPath = null)
        => RunRoslynatorAsync(["analyze", solution], msbuildPath);

    [McpServerTool(Name = "roslynator-fix")]
    [Description("Apply Roslynator auto-fixes to a solution or project. Run roslynator-analyze first to see what will be fixed.")]
    public static Task<string> Fix(
        [Description("Absolute path to .sln or .csproj.")] string solution,
        [Description("Path to MSBuild directory or MSBuild.exe. Required for .NET Framework solutions when auto-detection fails.")] string? msbuildPath = null)
        => RunRoslynatorAsync(["fix", solution], msbuildPath);

    [McpServerTool(Name = "roslynator-format")]
    [Description("Format code in a solution or project using Roslynator.")]
    public static Task<string> Format(
        [Description("Absolute path to .sln or .csproj.")] string solution,
        [Description("Path to MSBuild directory or MSBuild.exe. Required for .NET Framework solutions when auto-detection fails.")] string? msbuildPath = null)
        => RunRoslynatorAsync(["format", solution], msbuildPath);

    [McpServerTool(Name = "roslynator-spellcheck")]
    [Description("Check spelling of identifiers and comments in a solution or project via Roslynator.")]
    public static Task<string> Spellcheck(
        [Description("Absolute path to .sln or .csproj.")] string solution,
        [Description("Path to MSBuild directory or MSBuild.exe. Required for .NET Framework solutions when auto-detection fails.")] string? msbuildPath = null)
        => RunRoslynatorAsync(["spellcheck", solution], msbuildPath);

    [McpServerTool(Name = "roslynator-lloc")]
    [Description("Count logical lines of code in a solution or project via Roslynator.")]
    public static Task<string> Lloc(
        [Description("Absolute path to .sln or .csproj.")] string solution,
        [Description("Path to MSBuild directory or MSBuild.exe. Required for .NET Framework solutions when auto-detection fails.")] string? msbuildPath = null)
        => RunRoslynatorAsync(["lloc", solution], msbuildPath);

    [McpServerTool(Name = "roslynator-loc")]
    [Description("Count physical lines of code in a solution or project via Roslynator.")]
    public static Task<string> Loc(
        [Description("Absolute path to .sln or .csproj.")] string solution,
        [Description("Path to MSBuild directory or MSBuild.exe. Required for .NET Framework solutions when auto-detection fails.")] string? msbuildPath = null)
        => RunRoslynatorAsync(["loc", solution], msbuildPath);

    [McpServerTool(Name = "roslynator-rename-symbol")]
    [Description("Rename symbols across a solution or project using Roslynator. " +
                 "match is a C# expression body for 'bool M(ISymbol symbol)' selecting which symbols to rename. " +
                 "newName is a C# expression body for 'string M(ISymbol symbol)' returning the new name. " +
                 "Runs as dry-run by default; set dryRun to false to apply changes.")]
    public static Task<string> RenameSymbol(
        [Description("Absolute path to .sln or .csproj.")] string solution,
        [Description("C# expression selecting symbols to rename, e.g. symbol.Name.StartsWith(\"_\")")] string match,
        [Description("C# expression returning the new name, e.g. symbol.Name.TrimStart('_')")] string newName,
        [Description("Preview only without writing changes. Defaults to true.")] bool dryRun = true,
        [Description("Path to MSBuild directory or MSBuild.exe. Required for .NET Framework solutions when auto-detection fails.")] string? msbuildPath = null)
    {
        List<string> args = ["rename-symbol", solution, "--match", match, "--new-name", newName];
        if (dryRun) args.Add("--dry-run");
        return RunRoslynatorAsync(args, msbuildPath);
    }

    [McpServerTool(Name = "roslynator-find-symbol")]
    [Description("Find symbols in a solution or project via Roslynator. " +
                 "Use unused to locate dead code. " +
                 "symbolKind filters by kind: class, delegate, enum, interface, struct, event, field, enum-field, const, method, property, indexer, member, type. " +
                 "visibility filters by accessibility: public, internal, private.")]
    public static Task<string> FindSymbol(
        [Description("Absolute path to .sln or .csproj.")] string solution,
        [Description("Find only symbols with zero references.")] bool unused = false,
        [Description("Space-separated symbol kinds to include, e.g. \"method field\".")] string? symbolKind = null,
        [Description("Space-separated visibility levels to include, e.g. \"public internal\".")] string? visibility = null,
        [Description("Path to MSBuild directory or MSBuild.exe. Required for .NET Framework solutions when auto-detection fails.")] string? msbuildPath = null)
    {
        List<string> args = ["find-symbol", solution];
        if (unused) args.Add("--unused");
        if (!string.IsNullOrEmpty(symbolKind)) { args.Add("--symbol-kind"); args.Add(symbolKind); }
        if (!string.IsNullOrEmpty(visibility)) { args.Add("--visibility"); args.Add(visibility); }
        return RunRoslynatorAsync(args, msbuildPath);
    }

    private static string GetRoslynatorExe()
    {
        string? exeDir = Path.GetDirectoryName(Environment.ProcessPath);
        if (exeDir != null)
        {
            string name = OperatingSystem.IsWindows() ? "roslynator.exe" : "roslynator";
            string local = Path.Combine(exeDir, "roslynator", name);
            if (File.Exists(local))
                return local;
        }
        return "roslynator";
    }

    private static async Task<string> RunRoslynatorAsync(IEnumerable<string> args, string? msbuildPath = null)
    {
        string exe = GetRoslynatorExe();
        List<string> allArgs = new List<string>(args);

        string? resolvedMsbuildPath = msbuildPath ?? FindMsBuildPath();
        if (resolvedMsbuildPath != null)
        {
            allArgs.Add("--msbuild-path");
            allArgs.Add(resolvedMsbuildPath);
        }

        var psi = new ProcessStartInfo(exe)
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
            return $"Failed to start roslynator: {ex.Message}\nEnsure the project has been built so the local roslynator binary is present, or install globally with: dotnet tool install -g Roslynator.DotNet.Cli";
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

    private static string? FindMsBuildPath()
    {
        // Prefer Visual Studio MSBuild (needed for .NET Framework solutions)
        string? vsPath = FindVsMsBuildPath();
        if (vsPath != null) return vsPath;

        // Fall back to highest .NET SDK < 10 (avoids XMakeElements init failure with .NET 10+)
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

    private static string? FindVsMsBuildPath()
    {
        if (!OperatingSystem.IsWindows()) return null;

        string vswhere = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Microsoft Visual Studio", "Installer", "vswhere.exe");

        if (!File.Exists(vswhere)) return null;

        try
        {
            var psi = new ProcessStartInfo(vswhere)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("-latest");
            psi.ArgumentList.Add("-requires");
            psi.ArgumentList.Add("Microsoft.Component.MSBuild");
            psi.ArgumentList.Add("-find");
            psi.ArgumentList.Add(@"MSBuild\**\Bin\MSBuild.exe");

            using Process? proc = Process.Start(psi);
            if (proc == null) return null;
            string output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            string? msbuildExe = output.Split('\n').Select(l => l.Trim()).FirstOrDefault(l => l.Length > 0);
            return msbuildExe != null ? Path.GetDirectoryName(msbuildExe) : null;
        }
        catch
        {
            return null;
        }
    }

    private static Version? TryParseVersion(string s) =>
        Version.TryParse(s, out Version? v) ? v : null;
}
