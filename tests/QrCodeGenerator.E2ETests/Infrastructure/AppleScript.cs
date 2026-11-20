using System.Text;

namespace QrCodeGenerator.E2ETests.Infrastructure;

internal static class AppleScript
{
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(20);

    /// <summary>Runs a script and returns its result, mapping permission failures to an actionable message.</summary>
    public static string Run(string app, string script)
    {
        ProcessResult r = ProcessRunner.Run("/usr/bin/osascript", ["-e", script], timeout: CallTimeout);
        if (r.TimedOut)
        {
            throw new InvalidOperationException(
                $"osascript timed out controlling {app}. If macOS showed a permission prompt, allow it, or grant it in " +
                $"System Settings → Privacy & Security → Automation → <your terminal/IDE> → {app}.");
        }

        if (r.ExitCode != 0)
        {
            if (r.StdErr.Contains("-1743", StringComparison.Ordinal) || r.StdErr.Contains("Not authorized", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Not allowed to control {app}. Grant Automation permission: System Settings → Privacy & Security → " +
                    $"Automation → <your terminal/IDE> → {app}. ({r.StdErr.Trim()})");
            }

            throw new InvalidOperationException($"osascript failed for {app}: {r.StdErr.Trim()}");
        }

        return r.StdOut.TrimEnd('\n');
    }

    /// <summary>AppleScript string expression for <paramref name="text"/>; DEL (Backspace) is spliced in as ASCII character 127.</summary>
    public static string Literal(string text)
    {
        var sb = new StringBuilder("\"");
        foreach (char c in text)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\u007f': sb.Append("\" & (ASCII character 127) & \""); break;
                default: sb.Append(c); break;
            }
        }

        return sb.Append('"').ToString();
    }
}
