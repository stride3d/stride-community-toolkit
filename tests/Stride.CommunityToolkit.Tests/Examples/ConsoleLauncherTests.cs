using System.Diagnostics;
using System.Reflection;
using Stride.CommunityToolkit.Examples.Core;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Examples;

/// <summary>
/// The console launcher's menu loop, run as the real program through <c>dotnet run --no-build</c> against a
/// manifest of two examples that are not on disk: what choosing an example, a command and a wrong id each
/// print. With nothing on disk the launch reports a missing project and no game process starts.
/// </summary>
public class ConsoleLauncherTests
{
    [Theory]
    [InlineData("1", "First fixture", "FirstFixture")]
    [InlineData("2", "Second fixture", "SecondFixture")]
    public async Task Selection_AnnouncesTitleBeforeAction(string choice, string title, string projectName)
    {
        var output = await RunLauncher($"{choice}\nq\n");
        var selection = output[output.IndexOf("Choice: ", StringComparison.Ordinal)..];
        var announcement = $"Starting example: {title}";
        var actionOutput = $"Cannot run {projectName}: no project or source file found on disk.";

        Assert.Contains(announcement, selection);
        Assert.Contains(actionOutput, selection);
        Assert.True(selection.IndexOf(announcement, StringComparison.Ordinal) < selection.IndexOf(actionOutput, StringComparison.Ordinal), selection);
        Assert.Contains("It might take a few moments to start the example...", selection);
    }

    [Theory]
    [InlineData("q")]
    [InlineData("Q")]
    public async Task Quit_LaunchesNothingAndComplainsOfNothing(string input)
    {
        var output = await RunLauncher($"{input}\n");

        AssertNothingLaunched(output);
        Assert.DoesNotContain("Invalid choice.", output);
        Assert.Equal(1, MenuCount(output));
    }

    [Theory]
    [InlineData("c\nq")]
    [InlineData("C\nQ")]
    public async Task Clear_RedrawsTheMenu(string input)
    {
        var output = await RunLauncher($"{input}\n");

        AssertNothingLaunched(output);
        Assert.Equal(2, MenuCount(output));
    }

    [Theory]
    [InlineData("invalid\nq")]
    [InlineData("\nq")]
    public async Task InvalidChoice_SaysSo(string input)
    {
        var output = await RunLauncher($"{input}\n");

        AssertNothingLaunched(output);
        Assert.Contains("Invalid choice. Try again.", output);
    }

    private static void AssertNothingLaunched(string output)
    {
        Assert.DoesNotContain("Starting example:", output);
        Assert.DoesNotContain("It might take a few moments to start the example...", output);
        Assert.DoesNotContain("Cannot run", output);
    }

    // How many times the menu was drawn: its heading is printed once per draw
    private static int MenuCount(string output) => output.Split("Stride Community Toolkit Examples").Length - 1;

    private static async Task<string> RunLauncher(string input)
    {
        var directory = Directory.CreateTempSubdirectory("toolkit-launcher-tests-").FullName;

        try
        {
            // The real entry point, run from its own build output through dotnet run, reading an isolated
            // manifest. No source paths means the real launch action reports a missing project synchronously,
            // never starting a game process.
            var manifestPath = Path.Combine(directory, ManifestLoader.ManifestFileName);

            await File.WriteAllTextAsync(manifestPath, """
                {
                  "schemaVersion": 1,
                  "examples": [
                    { "slug": "first-fixture", "projectName": "FirstFixture", "title": { "en": "First fixture" } },
                    { "slug": "second-fixture", "projectName": "SecondFixture", "title": { "en": "Second fixture" } }
                  ]
                }
                """);

            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = directory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--no-build");

            // The launcher's build in the configuration this test assembly was built in: dotnet run
            // otherwise looks for Debug, which a Release-only build (the CI workflow) does not have
            startInfo.ArgumentList.Add("--configuration");
            startInfo.ArgumentList.Add(BuildConfiguration);
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(LauncherProject());
            startInfo.Environment["NO_COLOR"] = "1";
            startInfo.Environment[ManifestLoader.ManifestPathVariable] = manifestPath;

            using var process = Process.Start(startInfo)!;

            try
            {
                var output = process.StandardOutput.ReadToEndAsync();
                var error = process.StandardError.ReadToEndAsync();
                await process.StandardInput.WriteAsync(input);
                await process.StandardInput.FlushAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                await process.WaitForExitAsync(timeout.Token);
                Assert.Equal(0, process.ExitCode);
                Assert.Equal(string.Empty, await error);

                return await output;
            }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>Debug or Release: what the test assembly, and so the launcher it references, was built in.</summary>
    private static readonly string BuildConfiguration =
        typeof(ConsoleLauncherTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Debug";

    /// <summary>The console launcher's project file, found by walking up from the test assembly to the repository root.</summary>
    private static string LauncherProject()
    {
        var directory = AppContext.BaseDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            var candidate = Path.Combine(directory, "tools", "Stride.CommunityToolkit.Examples", "Stride.CommunityToolkit.Examples.csproj");

            if (File.Exists(candidate)) return candidate;

            directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        throw new FileNotFoundException("The console launcher's project was not found above the test assembly.");
    }
}
