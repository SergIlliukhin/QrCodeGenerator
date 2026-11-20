namespace QrCodeGenerator.E2ETests.Infrastructure;

/// <summary>Locates the app built for this test run, and builds a Release copy on demand.</summary>
internal static class AppBuild
{
#if DEBUG
    public const string Configuration = "Debug";
#else
    public const string Configuration = "Release";
#endif

    private static readonly Lazy<string> ReleaseDllLazy = new(BuildRelease);

    public static string RepoRoot { get; } = FindRepoRoot();

    public static string ProjectPath => Path.Combine(RepoRoot, "QrCodeGenerator.csproj");

    /// <summary>The app dll built alongside these tests (same configuration).</summary>
    public static string AppDll => Path.Combine(RepoRoot, "bin", Configuration, "net11.0", "QrCodeGenerator.dll");

    public static bool AppIsDebug => Configuration == "Debug";

    /// <summary>A Release build of the app in a temp dir, built once per test run.</summary>
    public static string ReleaseDll => ReleaseDllLazy.Value;

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "QrCodeGenerator.sln")))
            {
                return dir.FullName;
            }
        }

        throw new InvalidOperationException("QrCodeGenerator.sln not found above " + AppContext.BaseDirectory);
    }

    private static string BuildRelease()
    {
        string outDir = Path.Combine(Path.GetTempPath(), "qrgen-e2e-release");
        ProcessResult r = ProcessRunner.Run("dotnet", ["build", ProjectPath, "-c", "Release", "-o", outDir, "--nologo", "-v", "q"],
            timeout: TimeSpan.FromMinutes(3));
        if (r.ExitCode != 0)
        {
            throw new InvalidOperationException($"Release build failed:\n{r.StdOut}\n{r.StdErr}");
        }

        return Path.Combine(outDir, "QrCodeGenerator.dll");
    }
}
