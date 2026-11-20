namespace QrCodeGenerator.E2ETests.Infrastructure;

internal static class TerminalDrivers
{
    public const string GuiVariable = "QRGEN_E2E_GUI";
    public const string Tmux = "tmux";
    public const string TerminalApp = "Terminal.app";
    public const string ITerm2 = "iTerm2";

    public static bool GuiEnabled => Environment.GetEnvironmentVariable(GuiVariable) == "1";

    public static IEnumerable<string> Names => [Tmux, TerminalApp, ITerm2];

    /// <summary>Theory data: one row per driver name.</summary>
    public static TheoryData<string> All
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (string name in Names) data.Add(name);
            return data;
        }
    }

    public static string? SkipReason(string name) => name switch
    {
        Tmux => TmuxSession.SkipReason,
        TerminalApp => TerminalAppSession.SkipReason,
        ITerm2 => ITerm2Session.SkipReason,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    /// <summary>
    /// SIGKILLs every job on a terminal device such as <c>/dev/ttys004</c>, sparing the window's login shell
    /// (<c>-zsh</c>): killing that makes iTerm2 show a blocking "Session ended very soon after starting" alert.
    /// </summary>
    /// <remarks><c>pkill -t</c> silently matches nothing when run from the test host, so the PIDs come from <c>lsof</c>.</remarks>
    public static void KillTty(string tty)
    {
        string device = tty.Trim();
        if (!device.StartsWith("/dev/", StringComparison.Ordinal)) return;

        string[] pids = ProcessRunner.Run("/usr/sbin/lsof", "-t", device).StdOut
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string pid in pids)
        {
            // argv[0] "-zsh" marks the login shell; iTerm2 also keeps a /usr/bin/login parent on the tty.
            string argv0 = ProcessRunner.Run("/bin/ps", "-o", "command=", "-p", pid).StdOut.Trim().Split(' ')[0];
            if (argv0.Length > 0 && !argv0.StartsWith('-') && !argv0.EndsWith("login", StringComparison.Ordinal))
            {
                ProcessRunner.Run("/bin/kill", "-KILL", pid);
            }
        }
    }

    /// <summary>Skips the calling test (with the reason) when the driver is unavailable, otherwise creates a session.</summary>
    public static ITerminalSession Open(string name)
    {
        string? reason = SkipReason(name);
        Skip.If(reason != null, reason);
        return name switch
        {
            Tmux => new TmuxSession(),
            TerminalApp => new TerminalAppSession(),
            ITerm2 => new ITerm2Session(),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
        };
    }
}
