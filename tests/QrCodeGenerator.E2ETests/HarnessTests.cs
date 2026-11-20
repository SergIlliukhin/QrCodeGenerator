using QrCodeGenerator.E2ETests.Infrastructure;

namespace QrCodeGenerator.E2ETests;

/// <summary>Checks the harness itself: app builds, driver availability, and that each driver really reads scrollback.</summary>
public class HarnessTests
{
    [Fact]
    public void DebugAppDllExists() => Assert.True(File.Exists(AppBuild.AppDll), AppBuild.AppDll);

    [Fact]
    public void ReleaseBuildProducesDll() => Assert.True(File.Exists(AppBuild.ReleaseDll), AppBuild.ReleaseDll);

    [SkippableFact]
    public void GuiDriversSkippedWithReasonWhenNotEnabled()
    {
        Skip.If(TerminalDrivers.GuiEnabled, $"{TerminalDrivers.GuiVariable} is set");
        Assert.Contains(TerminalDrivers.GuiVariable, TerminalDrivers.SkipReason(TerminalDrivers.TerminalApp));
        Assert.Contains(TerminalDrivers.GuiVariable, TerminalDrivers.SkipReason(TerminalDrivers.ITerm2));
    }

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void ReadsEarliestScrollbackLine(string driver)
    {
        using ITerminalSession s = TerminalDrivers.Open(driver);
        s.StartShell("for i in $(seq 1 300); do echo SCROLL_$i; done");
        string text = s.WaitForText("SCROLL_300");
        Assert.Contains("SCROLL_1\n", text.Replace("\r", "", StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void InputReachesRunningProgram(string driver)
    {
        using ITerminalSession s = TerminalDrivers.Open(driver);
        // Markers are split in the command ("RE""ADY") so the echoed command line in GUI terminals can't match them.
        s.StartShell("sh -c 'echo RE\"\"ADY; read x; echo got:$x'");
        s.WaitForText("READY");
        s.SendLine("hello-e2e");
        s.WaitForText("got:hello-e2e");
    }

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void WaitForTimesOutWithLastScreen(string driver)
    {
        using ITerminalSession s = TerminalDrivers.Open(driver);
        s.StartShell("echo MARKER\"\"_VISIBLE");
        s.WaitForText("MARKER_VISIBLE");
        var ex = Assert.Throws<TimeoutException>(() => s.WaitForText("never-printed", TimeSpan.FromSeconds(2)));
        Assert.Contains("MARKER_VISIBLE", ex.Message, StringComparison.Ordinal);
        Assert.Contains(driver, ex.Message, StringComparison.Ordinal);
    }

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void DisposeKillsAppAndClosesSession(string driver)
    {
        int pid;
        ITerminalSession s = TerminalDrivers.Open(driver);
        using (s)
        {
            s.StartShell("sh -c 'echo $$ > app.pid; echo STAR\"\"TED; exec sleep 300'");
            s.WaitForText("STARTED");
            pid = int.Parse(File.ReadAllText(Path.Combine(s.WorkDir, "app.pid")).Trim(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(ProcessRunner.IsAlive(pid));
        }

        Thread.Sleep(500);
        Assert.False(ProcessRunner.IsAlive(pid), "app process survived Dispose");
        Assert.False(Directory.Exists(s.WorkDir), "work dir not deleted");
        if (s is TmuxSession tmux) Assert.False(tmux.SessionExists(), "tmux session not closed");
    }

    /// <summary>A scenario failing before the app writes its pid must still leave nothing running.</summary>
    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void DisposeWithoutPidFileLeavesNothingRunning(string driver)
    {
        string marker = "sleep " + Random.Shared.Next(1000, 9999).ToString(System.Globalization.CultureInfo.InvariantCulture);
        using (ITerminalSession s = TerminalDrivers.Open(driver))
        {
            s.StartShell($"echo STAR\"\"TED; {marker}");
            s.WaitForText("STARTED");
            Assert.Equal(0, ProcessRunner.Run("/usr/bin/pgrep", "-f", marker).ExitCode);
        }

        Thread.Sleep(500);
        Assert.NotEqual(0, ProcessRunner.Run("/usr/bin/pgrep", "-f", marker).ExitCode);
    }
}
