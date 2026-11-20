using System.Text.RegularExpressions;
using QrCodeGenerator.E2ETests.Infrastructure;

namespace QrCodeGenerator.E2ETests;

/// <summary>End-to-end secret-handling scenarios, each run in every available terminal.</summary>
public partial class ConsoleScenarios
{
    private const string QrFile = "t.png";
    private const string WrongPassword = "Wrong_Pass123!";
    private const string Del = "\u007f";

    // ---------- masked entry ----------

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void SeedEntryIsMaskedAndBackspaceRemovesOneMask(string driver)
    {
        using ITerminalSession s = TerminalDrivers.Open(driver);
        StartAtMenu(s);
        Step(s, "1", Ui.SeedPrompt);

        // Extra "x" then Backspace: the submitted seed must still be valid and show exactly one '*' per char.
        string text = Step(s, TestData.Seed + "x" + Del, Ui.PasswordPrompt);

        Assert.Equal(TestData.Seed.Length, SecretAssert.MaskCount(text, Ui.SeedPrompt, "Вимоги до пароля"));
        SecretAssert.NoSeedWords(s, text);
    }

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void InvalidSeedIsRejectedWithoutEcho(string driver)
    {
        const string invalid = "glory promote mansion submit captain finger virus verb utility general welcome zzzzqx";
        using ITerminalSession s = TerminalDrivers.Open(driver);
        StartAtMenu(s);
        Step(s, "1", Ui.SeedPrompt);
        string text = Step(s, invalid, Ui.InvalidWords);

        SecretAssert.NoSeedWords(s, text, invalid);
    }

    /// <summary>Keys arriving one at a time land between ReadKey calls; none may echo in clear.</summary>
    [SkippableFact]
    public void SeedTypedKeyByKeyNeverEchoes()
    {
        using var s = (TmuxSession)TerminalDrivers.Open(TerminalDrivers.Tmux);
        StartAtMenu(s);
        Step(s, "1", Ui.SeedPrompt);
        foreach (char c in TestData.Seed)
        {
            s.SendKeys(c.ToString());
            Thread.Sleep(15);
        }

        string typed = s.WaitFor(t => SecretAssert.MaskCount(t, Ui.SeedPrompt, "\n") == TestData.Seed.Length, "all mask chars");
        SecretAssert.NoSeedWords(s, typed);
        string text = Step(s, string.Empty, Ui.PasswordPrompt);
        SecretAssert.NoSeedWords(s, text);
    }

    /// <summary>A seed typed before its prompt appears (type-ahead while the menu choice is processed) must not echo.</summary>
    [SkippableFact]
    public void SeedTypedAheadOfPromptNeverEchoes()
    {
        using var s = (TmuxSession)TerminalDrivers.Open(TerminalDrivers.Tmux);
        StartAtMenu(s);
        s.SendKeys("1\r" + TestData.Seed);
        string typed = s.WaitFor(t => SecretAssert.MaskCount(t, Ui.SeedPrompt, "\n") == TestData.Seed.Length, "all mask chars");
        SecretAssert.NoSeedWords(s, typed);
        string text = Step(s, string.Empty, Ui.PasswordPrompt);
        SecretAssert.NoSeedWords(s, text);
    }

    // ---------- reveal and wipe ----------

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void CiphertextHiddenByDefault(string driver)
    {
        using ITerminalSession s = TerminalDrivers.Open(driver);
        StartAtMenu(s);
        string text = Encrypt(s, showCiphertext: false);

        Assert.True(File.Exists(Path.Combine(s.WorkDir, QrFile)), $"[{s.Name}] QR file not created");
        SecretAssert.Absent(s, text, Ui.CipherLabel, "ciphertext label");
        string appOutput = text[text.IndexOf(Ui.Menu, StringComparison.Ordinal)..];
        Assert.False(Base64Run().IsMatch(appOutput), $"[{s.Name}] base64 ciphertext printed:\n{s.Tail(text)}");
    }

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void CiphertextShownThenWiped(string driver)
    {
        using ITerminalSession s = TerminalDrivers.Open(driver);
        StartAtMenu(s);
        Encrypt(s, showCiphertext: false, answerShow: false);
        string shown = Step(s, "y", Ui.Acknowledge);

        string ciphertext = Base64Run().Match(shown[shown.LastIndexOf(Ui.CipherLabel, StringComparison.Ordinal)..]).Value;
        Assert.True(ciphertext.Length > 40, $"[{s.Name}] ciphertext not visible before key press:\n{s.Tail(shown)}");

        string after = Step(s, string.Empty, Ui.Another);
        SecretAssert.Absent(s, after, ciphertext, "ciphertext");
        SecretAssert.Absent(s, after, Ui.CipherLabel, "ciphertext label");
    }

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void DecryptedSeedShownThenWiped(string driver)
    {
        using ITerminalSession s = TerminalDrivers.Open(driver);
        RevealSeedFromGeneratedQr(s);
    }

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void DeclinedRevealNeverShowsSeed(string driver)
    {
        using ITerminalSession s = TerminalDrivers.Open(driver);
        StartAtMenu(s);
        Encrypt(s, showCiphertext: false);
        Step(s, "y", Ui.Menu);
        Step(s, "2", Ui.QrPathPrompt);
        Step(s, QrFile, Ui.PasswordPrompt);
        Step(s, TestData.Password, Ui.RevealConfirm);
        string text = Step(s, "n", Ui.Another);

        SecretAssert.Present(s, text, Ui.Hidden, "\"seed hidden\" message");
        SecretAssert.NoSeedWords(s, text);
    }

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void WrongPasswordThenRetrySucceeds(string driver)
    {
        using ITerminalSession s = TerminalDrivers.Open(driver);
        StartAtMenu(s);
        Step(s, "3", Ui.CipherTextPrompt);
        Step(s, TestData.Ciphertext, Ui.PasswordPrompt);
        int prompts = Count(s.ReadAll(), Ui.PasswordPrompt);
        s.SendLine(WrongPassword);
        string text = s.WaitFor(t => Count(t, Ui.PasswordPrompt) > prompts, "password prompt after wrong password");

        SecretAssert.Present(s, text, Ui.WrongPassword, "wrong-password message");
        Step(s, TestData.Password, Ui.RevealConfirm);
        Step(s, "n", Ui.Another);
    }

    // ---------- exit paths ----------

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void NormalExitLeavesNoSecrets(string driver)
    {
        using ITerminalSession s = TerminalDrivers.Open(driver);
        RevealSeedFromGeneratedQr(s);
        s.SendLine("n");

        Assert.Equal(0, s.WaitForExit());
        string text = s.ReadAll();
        SecretAssert.Present(s, text, Ui.Farewell, "farewell");
        SecretAssert.Absent(s, text, Ui.SeedRules, "seed prompt");
        SecretAssert.Absent(s, text, Ui.PasswordPrompt, "password prompt");
        SecretAssert.NoMaskRun(s, text);
        SecretAssert.NoSeedWords(s, text);
    }

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void InterruptAfterSeedWipes(string driver)
    {
        using ITerminalSession s = TerminalDrivers.Open(driver);
        StartAtMenu(s);
        Step(s, "1", Ui.SeedPrompt);
        Step(s, TestData.Seed, Ui.PasswordPrompt);
        s.Interrupt();

        Assert.Equal(130, s.WaitForExit());
        string text = s.ReadAll();
        SecretAssert.Absent(s, text, Ui.SeedPrompt, "seed prompt");
        SecretAssert.NoMaskRun(s, text);
    }

    [SkippableTheory]
    [MemberData(nameof(TerminalDrivers.All), MemberType = typeof(TerminalDrivers))]
    public void UnexpectedErrorWipesThenReports(string driver)
    {
        Skip.IfNot(AppBuild.AppIsDebug, "fault hook exists only in Debug builds");
        using ITerminalSession s = TerminalDrivers.Open(driver);
        StartAtMenu(s, new Dictionary<string, string> { ["QRGEN_E2E_FAULT"] = "after-seed" });
        Step(s, "1", Ui.SeedPrompt);
        s.SendLine(TestData.Seed);

        Assert.Equal(1, s.WaitForExit());
        string text = s.ReadAll();
        SecretAssert.Present(s, text, Ui.UnexpectedError, "error message");
        SecretAssert.Absent(s, text, Ui.SeedPrompt, "seed prompt");
        SecretAssert.NoMaskRun(s, text);
    }

    [SkippableFact]
    public void ReleaseBuildIgnoresFaultHook()
    {
        using ITerminalSession s = TerminalDrivers.Open(TerminalDrivers.Tmux);
        s.StartApp(new AppLaunch(AppBuild.ReleaseDll, new Dictionary<string, string> { ["QRGEN_E2E_FAULT"] = "after-seed" }));
        s.WaitForText(Ui.Menu);
        Step(s, "1", Ui.SeedPrompt);
        Step(s, TestData.Seed, Ui.PasswordPrompt);
    }

    // ---------- redirected streams ----------

    [Fact]
    public void PipedInputIsRefused()
    {
        ProcessResult r = ProcessRunner.Run("dotnet", [AppBuild.AppDll], stdin: "1\n", timeout: TimeSpan.FromSeconds(30));

        Assert.Equal(2, r.ExitCode);
        Assert.Contains(Ui.InputRedirected, r.StdErr, StringComparison.Ordinal);
        Assert.Equal(string.Empty, r.StdOut);
    }

    [SkippableFact]
    public void RevealWithRedirectedOutputWritesNoSecret()
    {
        using ITerminalSession s = TerminalDrivers.Open(TerminalDrivers.Tmux);
        s.StartApp(new AppLaunch(AppBuild.AppDll, RedirectStdout: true));
        string outFile = Path.Combine(s.WorkDir, "out.txt");

        WaitFile(outFile, Ui.Menu);
        s.SendLine("3");
        WaitFile(outFile, Ui.CipherTextPrompt);
        s.SendLine(TestData.Ciphertext);
        WaitFile(outFile, Ui.PasswordPrompt);
        s.SendLine(TestData.Password);
        WaitFile(outFile, Ui.RevealConfirm);
        s.SendLine("y");
        s.WaitForText(Ui.OutputRedirected);
        WaitFile(outFile, Ui.Another);
        s.SendLine("n");

        Assert.Equal(0, s.WaitForExit());
        string written = File.ReadAllText(outFile);
        Assert.Contains(Ui.RevealConfirm, written, StringComparison.Ordinal);
        foreach (string word in TestData.Seed.Split(' '))
        {
            Assert.DoesNotMatch($@"\b{word}\b", written);
        }
    }

    // ---------- helpers ----------

    private static void StartAtMenu(ITerminalSession s, IReadOnlyDictionary<string, string>? env = null)
    {
        s.StartApp(new AppLaunch(AppBuild.AppDll, env));
        s.WaitForText(Ui.Menu);
    }

    /// <summary>Sends a line, then waits until <paramref name="expect"/> shows up near the bottom of the output.</summary>
    private static string Step(ITerminalSession s, string line, string expect)
    {
        s.SendLine(line);
        return s.WaitFor(t => s.Tail(t, 8).Contains(expect, StringComparison.Ordinal), $"\"{expect}\" after sending \"{Redact(line)}\"");
    }

    /// <summary>Encrypts the example seed into <see cref="QrFile"/>; leaves the app at the "another operation?" prompt
    /// unless <paramref name="answerShow"/> is false (then it stops at the "show encrypted text?" question).</summary>
    private static string Encrypt(ITerminalSession s, bool showCiphertext, bool answerShow = true)
    {
        Step(s, "1", Ui.SeedPrompt);
        Step(s, TestData.Seed, Ui.PasswordPrompt);
        Step(s, TestData.Password, Ui.FilePrompt);
        string text = Step(s, QrFile, Ui.ShowCipher);
        if (!answerShow) return text;
        return showCiphertext ? Step(s, "y", Ui.Acknowledge) : Step(s, "n", Ui.Another);
    }

    /// <summary>Encrypts, decrypts the generated QR, reveals the seed, checks it's visible, then wiped after the key press.</summary>
    private static void RevealSeedFromGeneratedQr(ITerminalSession s)
    {
        StartAtMenu(s);
        Encrypt(s, showCiphertext: false);
        Step(s, "y", Ui.Menu);
        Step(s, "2", Ui.QrPathPrompt);
        Step(s, QrFile, Ui.PasswordPrompt);
        Step(s, TestData.Password, Ui.RevealConfirm);
        string shown = Step(s, "y", Ui.Acknowledge);
        SecretAssert.Present(s, shown, TestData.Seed, "decrypted seed before key press");

        string after = Step(s, string.Empty, Ui.Another);
        SecretAssert.NoSeedWords(s, after);
        SecretAssert.Absent(s, after, Ui.SeedLabel, "seed label");
    }

    private static void WaitFile(string path, string text)
    {
        DateTime deadline = DateTime.UtcNow + TerminalSessionBase.DefaultTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path) && File.ReadAllText(path).Contains(text, StringComparison.Ordinal)) return;
            Thread.Sleep(200);
        }

        throw new TimeoutException($"\"{text}\" never written to {path}:\n{(File.Exists(path) ? File.ReadAllText(path) : "<missing>")}");
    }

    private static int Count(string text, string needle)
    {
        int count = 0;
        for (int i = text.IndexOf(needle, StringComparison.Ordinal); i >= 0; i = text.IndexOf(needle, i + needle.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    private static string Redact(string line) => line.Length > 12 ? line[..4] + "…" : line;

    [GeneratedRegex(@"[A-Za-z0-9+/]{40,}={0,2}")]
    private static partial Regex Base64Run();
}
