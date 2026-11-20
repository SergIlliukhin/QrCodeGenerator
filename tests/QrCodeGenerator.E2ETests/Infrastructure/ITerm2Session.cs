namespace QrCodeGenerator.E2ETests.Infrastructure;

/// <summary>Drives an iTerm2 window through AppleScript.</summary>
internal sealed class ITerm2Session : TerminalSessionBase
{
    private const string App = "iTerm2";
    private string? _windowId;

    public ITerm2Session() : base("iTerm2")
    {
    }

    public static string? SkipReason =>
        !OperatingSystem.IsMacOS() ? "iTerm2 requires macOS"
        : !TerminalDrivers.GuiEnabled ? $"GUI terminals disabled; set {TerminalDrivers.GuiVariable}=1"
        : !Directory.Exists("/Applications/iTerm.app") ? "iTerm2 not installed"
        : null;

    private string Session =>
        $"current session of window id {_windowId ?? throw new InvalidOperationException("Session not started")}";

    protected override void Start(string command)
    {
        _windowId = AppleScript.Run(App, $$"""
            tell application "iTerm2"
                set w to (create window with default profile)
                tell current session of w
                    set columns to 200
                    set rows to 50
                    write text {{AppleScript.Literal(command)}}
                end tell
                return id of w
            end tell
            """);
    }

    public override void SendLine(string text) =>
        AppleScript.Run(App, $"tell application \"iTerm2\" to tell {Session} to write text {AppleScript.Literal(text)}");

    /// <summary>iTerm2 pads every row with a trailing space; strip it so text compares like other terminals.</summary>
    public override string ReadAll() =>
        string.Join('\n', AppleScript.Run(App, $"tell application \"iTerm2\" to return contents of {Session}")
            .Split('\n').Select(line => line.TrimEnd(' ')));

    protected override void Close()
    {
        if (_windowId == null) return;
        // Kill everything on the session's tty first so iTerm2 doesn't ask to confirm closing a running job.
        TerminalDrivers.KillTty(AppleScript.Run(App, $"tell application \"iTerm2\" to return tty of {Session}"));
        AppleScript.Run(App, $"tell application \"iTerm2\" to close window id {_windowId}");
    }
}
