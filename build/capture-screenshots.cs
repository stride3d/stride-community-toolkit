#:package SixLabors.ImageSharp@3.1.12

// Screenshot capture for the code-only examples.
//
//   dotnet run --file build/capture-screenshots.cs
//   dotnet run --file build/capture-screenshots.cs -- --only mesh-outline --only particles
//   dotnet run --file build/capture-screenshots.cs -- --force
//   dotnet run --file build/capture-screenshots.cs -- --frame 400 --keep-png
//   dotnet run --file build/capture-screenshots.cs -- --output screenshots-review
//
// The --file switch is required rather than optional here: the repository root contains
// Stride.CommunityToolkit.ndproj, and without --file the SDK runs that project and passes this
// script to it as an argument.
//
// Runs each example once with STRIDE_TOOLKIT_CAPTURE set, which makes the toolkit's own capture
// system take a screenshot at a fixed frame and exit - see ScreenshotCapture in
// src/Stride.CommunityToolkit/Engine. Capture is in-engine on purpose: it saves the GPU render
// target rather than scraping the screen, so there is no window to foreground, no DPI scaling, no
// occlusion risk, and the run does not have to own the desktop for the twenty minutes it takes.
//
// The PNG is then converted to WebP, because Stride's ImageFileType has no WebP member and the docs
// use WebP throughout.
//
// NOTHING IS COMMITTED AUTOMATICALLY. Images land in the docs media folder as working-tree changes
// for a human to look at, because a screenshot that renders black, catches a scene mid-explosion or
// frames nothing but sky is only detectable by looking at it.

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

var only = new List<string>();
var force = false;
var keepPng = false;
string? outputDirectory = null;
int? frameOverride = null;
var timeout = TimeSpan.FromMinutes(5);

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--only" when i + 1 < args.Length:
            only.Add(args[++i]);
            break;
        case "--force":
            force = true;
            break;
        case "--output" when i + 1 < args.Length:
            outputDirectory = args[++i];
            break;
        case "--keep-png":
            keepPng = true;
            break;
        case "--frame" when i + 1 < args.Length && int.TryParse(args[i + 1], out var f):
            frameOverride = f;
            i++;
            break;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            return 1;
    }
}

var root = RepositoryRoot();
var manifestPath = Path.Combine(root, "tools", "Stride.CommunityToolkit.Examples.Launcher", "examples-manifest.json");
var mediaDirectory = Path.Combine(root, "docs", "manual", "code-only", "examples", "media");
var stagingDirectory = Path.Combine(root, "bin", "screenshots");

if (!File.Exists(manifestPath))
{
    Console.Error.WriteLine($"No manifest at {manifestPath}. Build the launcher first, or run the generator's 'generate' command.");
    return 1;
}

Directory.CreateDirectory(stagingDirectory);

using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
var examples = document.RootElement.GetProperty("examples").EnumerateArray().ToList();

var captured = 0;
var skipped = 0;
var failed = new List<string>();

foreach (var example in examples)
{
    var slug = Text(example, "slug");
    var projectName = Text(example, "projectName");
    var projectPath = Text(example, "projectPath");

    if (slug is null || projectName is null || projectPath is null) continue;

    if (only.Count > 0 && !only.Contains(slug, StringComparer.OrdinalIgnoreCase))
    {
        continue;
    }

    if (Bool(example, "screenshot") == false)
    {
        Console.WriteLine($"  - {slug}: screenshot: false");
        skipped++;
        continue;
    }

    // With --output everything is written to a scratch folder named by slug, for review in bulk before
    // any of it goes near the docs. Without it, each image lands on its real media filename.
    var reviewing = outputDirectory is not null;
    var mediaName = reviewing ? $"{slug}.webp" : Text(example, "media") ?? $"{slug}.webp";
    var webpPath = Path.Combine(reviewing ? Path.GetFullPath(outputDirectory!) : mediaDirectory, mediaName);

    // An existing screenshot was almost certainly taken and reviewed by a person. Replacing 25 of those
    // in one unattended run, silently, is not something a script should be able to do by accident.
    if (File.Exists(webpPath) && !force && !reviewing)
    {
        Console.WriteLine($"  - {slug}: already has {mediaName}, pass --force to replace it");
        skipped++;
        continue;
    }

    var frame = frameOverride ?? Int(example, "screenshotFrame");
    var pngPath = Path.Combine(stagingDirectory, $"{slug}.png");

    File.Delete(pngPath);

    Console.WriteLine($"  · {slug} ({projectName})");

    if (!RunExample(root, projectPath, pngPath, frame, timeout, out var failure))
    {
        Console.Error.WriteLine($"    ✖ {failure}");
        failed.Add(slug);
        continue;
    }

    if (!File.Exists(pngPath))
    {
        Console.Error.WriteLine("    ✖ the example exited without writing a screenshot");
        failed.Add(slug);
        continue;
    }

    ToWebp(pngPath, webpPath);

    if (!keepPng) File.Delete(pngPath);

    Console.WriteLine($"    ✅ {mediaName}");
    captured++;
}

Console.WriteLine();
Console.WriteLine($"Captured {captured}, skipped {skipped}, failed {failed.Count}.");

if (failed.Count > 0)
{
    Console.WriteLine($"Failed: {string.Join(", ", failed)}");
}

Console.WriteLine("Review every image before committing - a black frame or a mid-explosion pose looks fine to a script.");

return failed.Count > 0 ? 1 : 0;

// Runs one example with capture enabled and waits for it to exit on its own.
static bool RunExample(string root, string projectPath, string pngPath, int? frame, TimeSpan timeout, out string failure)
{
    failure = string.Empty;

    var exampleDirectory = Path.GetDirectoryName(Path.Combine(root, "examples", "code-only", projectPath.Replace('/', Path.DirectorySeparatorChar)))!;
    var project = Directory.EnumerateFiles(exampleDirectory, "*.*proj").FirstOrDefault();

    // A file-based app has no project file and is run by naming its source directly.
    var arguments = project is not null
        ? $"run --project \"{project}\""
        : $"run \"{Path.Combine(root, "examples", "code-only", projectPath.Replace('/', Path.DirectorySeparatorChar))}\"";

    var startInfo = new ProcessStartInfo("dotnet", arguments)
    {
        WorkingDirectory = exampleDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    startInfo.Environment["STRIDE_TOOLKIT_CAPTURE"] = pngPath;

    if (frame is { } value)
    {
        startInfo.Environment["STRIDE_TOOLKIT_CAPTURE_FRAME"] = value.ToString();
    }

    using var process = Process.Start(startInfo);

    if (process is null)
    {
        failure = "could not start dotnet";
        return false;
    }

    // Drained so the pipes cannot fill and deadlock the child.
    _ = process.StandardOutput.ReadToEndAsync();
    _ = process.StandardError.ReadToEndAsync();

    if (!process.WaitForExit((int)timeout.TotalMilliseconds))
    {
        try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }

        failure = $"did not exit within {timeout.TotalMinutes:0} minutes";
        return false;
    }

    return true;
}

static void ToWebp(string pngPath, string webpPath)
{
    Directory.CreateDirectory(Path.GetDirectoryName(webpPath)!);

    using var image = Image.Load(pngPath);

    // FileFormat must be set explicitly: the encoder defaults to lossless, where Quality is ignored and
    // a 1280x720 frame lands around 200 KB instead of 40 KB.
    image.Save(webpPath, new WebpEncoder { FileFormat = WebpFileFormatType.Lossy, Quality = 85 });
}

static string? Text(JsonElement element, string name)
    => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

static bool? Bool(JsonElement element, string name)
    => element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

static int? Int(JsonElement element, string name)
    => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;

static string RepositoryRoot([CallerFilePath] string scriptPath = "")
    => Directory.GetParent(Path.GetDirectoryName(scriptPath)!)!.FullName;
