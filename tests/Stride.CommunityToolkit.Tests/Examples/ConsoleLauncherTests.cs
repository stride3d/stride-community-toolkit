using System.Diagnostics;
using Stride.CommunityToolkit.Examples.Core;
using Xunit;

namespace Stride.CommunityToolkit.Tests.Examples;

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
    [InlineData("c\nq")]
    [InlineData("C\nQ")]
    [InlineData("invalid\nq")]
    [InlineData("\nq")]
    public async Task NonExampleInput_DoesNotAnnounceLaunch(string input)
    {
        var output = await RunLauncher($"{input}\n");

        Assert.DoesNotContain("Starting example:", output);
        Assert.DoesNotContain("It might take a few moments to start the example...", output);
        Assert.DoesNotContain("Cannot run", output);

        if (input.StartsWith('c') || input.StartsWith('C'))
        {
            Assert.Equal(2, output.Split("Stride Community Toolkit Examples").Length - 1);
        }
        else if (input is not "q" and not "Q")
        {
            Assert.Contains("Invalid choice. Try again.", output);
        }
    }

    private static async Task<string> RunLauncher(string input)
    {
        var directory = Directory.CreateTempSubdirectory("toolkit-launcher-tests-").FullName;

        try
        {
            // Run the real entry point with an isolated manifest. No source paths means the real
            // launch action reports a missing project synchronously, never starting a game process.
            var launcherAssembly = typeof(ExampleProvider).Assembly.Location;
            var launcherName = Path.GetFileNameWithoutExtension(launcherAssembly);
            File.Copy(launcherAssembly, Path.Combine(directory, Path.GetFileName(launcherAssembly)));
            File.Copy(Path.Combine(Path.GetDirectoryName(launcherAssembly)!, "Pastel.dll"), Path.Combine(directory, "Pastel.dll"));
            var testRuntimeConfig = Path.ChangeExtension(typeof(ConsoleLauncherTests).Assembly.Location, ".runtimeconfig.json");
            File.Copy(testRuntimeConfig, Path.Combine(directory, $"{launcherName}.runtimeconfig.json"));
            await File.WriteAllTextAsync(Path.Combine(directory, ManifestLoader.ManifestFileName), """
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
            startInfo.ArgumentList.Add(Path.Combine(directory, Path.GetFileName(launcherAssembly)));
            startInfo.Environment["NO_COLOR"] = "1";

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
}
