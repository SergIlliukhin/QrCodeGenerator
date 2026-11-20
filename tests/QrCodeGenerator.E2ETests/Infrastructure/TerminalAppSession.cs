namespace QrCodeGenerator.E2ETests.Infrastructure;

/// <summary>Drives a macOS Terminal.app window through AppleScript.</summary>
internal sealed class TerminalAppSession : TerminalSessionBase
{
    private const string App = "Terminal";
    private string? _windowId;

    public TerminalAppSession() : base("Terminal.app")
    {
    }

    public static string? SkipReason =>
        !OperatingSystem.IsMacOS() ? "Terminal.app requires macOS"
        : !TerminalDrivers.GuiEnabled ? $"GUI terminals disabled; set {TerminalDrivers.GuiVariable}=1"
        : null;

    private string Tab => $"tab 1 of window id {_windowId ?? throw new InvalidOperationException("Session not started")}";

    protected override void Start(string command)
    {
        _windowId = AppleScript.Run(App, $$"""
            tell application "Terminal"
                set t to do script {{AppleScript.Literal(command)}}
                set number of columns of t to 200
                set number of rows of t to 50
                return id of (first window whose tabs contains t)
            end tell
            """);
    }

    public override void SendLine(string text) =>
        AppleScript.Run(App, $"tell application \"Terminal\" to do script {AppleScript.Literal(text)} in {Tab}");

    public override string ReadAll() =>
        AppleScript.Run(App, $"tell application \"Terminal\" to return (history of {Tab}) as text");

    protected override void Close()
    {
        if (_windowId == null) return;
        // Kill everything on the tab's tty first: Terminal won't close a window with a running job.
        TerminalDrivers.KillTty(AppleScript.Run(App, $"tell application \"Terminal\" to return tty of {Tab}"));
        AppleScript.Run(App, $"tell application \"Terminal\" to close window id {_windowId} saving no");
    }
}
