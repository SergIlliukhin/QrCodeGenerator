using System.Globalization;

namespace QrCodeGenerator.E2ETests.Infrastructure;

/// <summary>Headless driver: a detached tmux session on a dedicated server socket.</summary>
internal sealed class TmuxSession : TerminalSessionBase
{
    public const string Socket = "qrgen-e2e";

    private static readonly Lazy<string> ConfigFile = new(() =>
    {
        string path = Path.Combine(Path.GetTempPath(), "qrgen-e2e-tmux.conf");
        File.WriteAllText(path, "set -g history-limit 10000\nset -g remain-on-exit on\nset -g status off\n");
        return path;
    });

    private readonly string _session = "e2e-" + Guid.NewGuid().ToString("N")[..8];

    public TmuxSession() : base("tmux")
    {
    }

    public static string? SkipReason => ProcessRunner.IsOnPath("tmux") ? null : "tmux not installed";

    public string SessionName => _session;

    protected override void Start(string command)
    {
        Tmux("new-session", "-d", "-s", _session, "-x", "200", "-y", "50", command);
    }

    /// <summary>Types text without Enter (tmux only; GUI drivers always append Enter).</summary>
    public void SendKeys(string text) => Tmux("send-keys", "-t", _session, "-l", text);

    public override void SendLine(string text)
    {
        if (text.Length > 0)
        {
            Tmux("send-keys", "-t", _session, "-l", text);
        }

        Tmux("send-keys", "-t", _session, "Enter");
    }

    /// <summary>A real Ctrl+C keystroke (SIGINT to the foreground process group).</summary>
    public override void Interrupt() => Tmux("send-keys", "-t", _session, "C-c");

    public override string ReadAll() => Tmux("capture-pane", "-p", "-J", "-S", "-", "-t", _session).StdOut;

    public override int WaitForExit(TimeSpan? timeout = null)
    {
        // C-c also kills the wrapper shell, so the marker may never print; fall back to the dead pane's status.
        WaitFor(text => ExitMarker().IsMatch(text) || IsPaneDead(), "app exit", timeout);
        string text = ReadAll();
        var matches = ExitMarker().Matches(text);
        if (matches.Count > 0)
        {
            return int.Parse(matches[^1].Groups[1].Value, CultureInfo.InvariantCulture);
        }

        string status = PaneFormat("#{pane_dead_status}");
        if (status.Length > 0) return int.Parse(status, CultureInfo.InvariantCulture);
        // tmux reports the signal by name ("int") or number depending on version.
        string signal = PaneFormat("#{pane_dead_signal}").ToUpperInvariant();
        if (signal.Length == 0) return -1;
        return 128 + (int.TryParse(signal, CultureInfo.InvariantCulture, out int number) ? number : signal switch
        {
            "INT" or "SIGINT" => 2,
            "KILL" or "SIGKILL" => 9,
            "TERM" or "SIGTERM" => 15,
            _ => throw new InvalidOperationException($"Unknown tmux signal '{signal}'"),
        });
    }

    public bool SessionExists() => Tmux("has-session", "-t", _session).ExitCode == 0;

    private bool IsPaneDead() => PaneFormat("#{pane_dead}") == "1";

    private string PaneFormat(string format) => Tmux("display-message", "-p", "-t", _session, format).StdOut.Trim();

    protected override void Close() => Tmux("kill-session", "-t", _session);

    private static ProcessResult Tmux(params string[] args) =>
        ProcessRunner.Run("tmux", new[] { "-L", Socket, "-f", ConfigFile.Value }.Concat(args), timeout: TimeSpan.FromSeconds(10));
}
