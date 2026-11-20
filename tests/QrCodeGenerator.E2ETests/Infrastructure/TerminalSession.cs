using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace QrCodeGenerator.E2ETests.Infrastructure;

/// <summary>How to launch the app inside a terminal session.</summary>
internal sealed record AppLaunch(string Dll, IReadOnlyDictionary<string, string>? Env = null, bool RedirectStdout = false);

/// <summary>A real terminal running one command; shaped by the weakest driver (every send ends with Enter).</summary>
internal interface ITerminalSession : IDisposable
{
    string Name { get; }

    /// <summary>Per-session temp dir; the app runs with it as the working directory.</summary>
    string WorkDir { get; }

    void StartApp(AppLaunch launch);

    /// <summary>Starts an arbitrary shell command (driver self-tests).</summary>
    void StartShell(string command);

    void SendLine(string text);

    void PressEnter();

    /// <summary>Sends SIGINT to the app.</summary>
    void Interrupt();

    /// <summary>Screen plus scrollback, as the terminal holds it.</summary>
    string ReadAll();

    string WaitFor(Func<string, bool> predicate, string description, TimeSpan? timeout = null);

    string WaitForText(string text, TimeSpan? timeout = null);

    int WaitForExit(TimeSpan? timeout = null);

    string Tail(string text, int lines = 40);
}

internal abstract partial class TerminalSessionBase : ITerminalSession
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private static readonly Lazy<string> Dotnet = new(() =>
    {
        string path = ProcessRunner.Run("/usr/bin/which", "dotnet").StdOut.Trim();
        return path.Length > 0 ? path : "dotnet";
    });

    private bool _disposed;

    protected TerminalSessionBase(string name)
    {
        Name = name;
        WorkDir = Directory.CreateTempSubdirectory("qrgen-e2e-").FullName;
    }

    public string Name { get; }

    public string WorkDir { get; }

    protected string PidFile => Path.Combine(WorkDir, "app.pid");

    public void StartApp(AppLaunch launch)
    {
        var script = new StringBuilder();
        script.AppendLine("#!/bin/sh");
        script.AppendLine(CultureInfo.InvariantCulture, $"cd {ShellQuote(WorkDir)}");
        script.AppendLine("echo $$ > app.pid");
        script.Append("exec env");
        foreach (var (key, value) in launch.Env ?? new Dictionary<string, string>())
        {
            script.Append(CultureInfo.InvariantCulture, $" {key}={ShellQuote(value)}");
        }

        script.Append(CultureInfo.InvariantCulture, $" {ShellQuote(Dotnet.Value)} {ShellQuote(launch.Dll)}");
        if (launch.RedirectStdout)
        {
            script.Append(" > out.txt");
        }

        script.AppendLine();
        string scriptPath = Path.Combine(WorkDir, "run.sh");
        File.WriteAllText(scriptPath, script.ToString());
        // Non-interactive sh: an interactive shell (zsh in GUI terminals) drops the rest of the command list when
        // the app dies from SIGINT, so the exit marker would never print.
        Start("/bin/sh -c " + ShellQuote($"sh {ShellQuote(scriptPath)}; echo E2E_EXIT=$?"));
    }

    public void StartShell(string command) => Start($"cd {ShellQuote(WorkDir)} && {command}; echo E2E_EXIT=$?");

    protected abstract void Start(string command);

    public abstract void SendLine(string text);

    public void PressEnter() => SendLine(string.Empty);

    public virtual void Interrupt()
    {
        int pid = ReadPid() ?? throw new InvalidOperationException("App pid unknown; was StartApp used?");
        ProcessRunner.Signal(pid, "INT");
    }

    public abstract string ReadAll();

    public string WaitFor(Func<string, bool> predicate, string description, TimeSpan? timeout = null)
    {
        DateTime deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        string text = string.Empty;
        while (DateTime.UtcNow < deadline)
        {
            text = ReadAll();
            if (predicate(text))
            {
                return text;
            }

            Thread.Sleep(200);
        }

        throw new TimeoutException($"[{Name}] timed out waiting for {description}. Last screen:\n{Tail(text)}");
    }

    public string WaitForText(string text, TimeSpan? timeout = null) =>
        WaitFor(s => s.Contains(text, StringComparison.Ordinal), $"\"{text}\"", timeout);

    public virtual int WaitForExit(TimeSpan? timeout = null)
    {
        string text = WaitFor(s => ExitMarker().IsMatch(s), "app exit (E2E_EXIT marker)", timeout);
        MatchCollection matches = ExitMarker().Matches(text);
        return int.Parse(matches[^1].Groups[1].Value, CultureInfo.InvariantCulture);
    }

    public string Tail(string text, int lines = 40)
    {
        // Blank rows (e.g. between output and tmux's "Pane is dead" footer) carry no information.
        string[] all = text.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        return string.Join('\n', all.Skip(Math.Max(0, all.Length - lines)));
    }

    protected int? ReadPid()
    {
        if (!File.Exists(PidFile)) return null;
        return int.TryParse(File.ReadAllText(PidFile).Trim(), CultureInfo.InvariantCulture, out int pid) ? pid : null;
    }

    /// <summary>Closes the window/session. The app is already dead when this runs.</summary>
    protected abstract void Close();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (ReadPid() is int pid && ProcessRunner.IsAlive(pid))
        {
            ProcessRunner.Signal(pid, "KILL");
        }

        try
        {
            Close();
        }
        finally
        {
            try { Directory.Delete(WorkDir, recursive: true); } catch (IOException) { }
        }
    }

    protected static string ShellQuote(string s) => "'" + s.Replace("'", "'\\''", StringComparison.Ordinal) + "'";

    [GeneratedRegex(@"E2E_EXIT=(\d+)")]
    protected static partial Regex ExitMarker();
}
