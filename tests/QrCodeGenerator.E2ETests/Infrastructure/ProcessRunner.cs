using System.Diagnostics;

namespace QrCodeGenerator.E2ETests.Infrastructure;

internal sealed record ProcessResult(int ExitCode, string StdOut, string StdErr, bool TimedOut);

internal static class ProcessRunner
{
    public static ProcessResult Run(string fileName, IEnumerable<string> args, string? stdin = null,
        TimeSpan? timeout = null, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin != null,
            UseShellExecute = false,
        };
        if (workingDirectory != null) psi.WorkingDirectory = workingDirectory;
        foreach (string a in args) psi.ArgumentList.Add(a);

        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Could not start {fileName}");
        Task<string> outTask = p.StandardOutput.ReadToEndAsync();
        Task<string> errTask = p.StandardError.ReadToEndAsync();
        if (stdin != null)
        {
            p.StandardInput.Write(stdin);
            p.StandardInput.Close();
        }

        bool exited = p.WaitForExit(timeout ?? TimeSpan.FromSeconds(60));
        if (!exited)
        {
            try { p.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            p.WaitForExit();
        }

        return new ProcessResult(exited ? p.ExitCode : -1, outTask.Result, errTask.Result, !exited);
    }

    public static ProcessResult Run(string fileName, params string[] args) => Run(fileName, (IEnumerable<string>)args);

    public static bool IsOnPath(string tool) => Run("/usr/bin/which", tool).ExitCode == 0;

    public static bool IsAlive(int pid)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static void Signal(int pid, string signal) => Run("/bin/kill", $"-{signal}", pid.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
