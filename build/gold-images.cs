#:package SixLabors.ImageSharp@3.1.12

// Gold-image regression for the toolkit's renderers.
//
//   dotnet run --file build/gold-images.cs                                 compare every scene in tests/gold/scenes.jsonc
//   dotnet run --file build/gold-images.cs -- --only shapes-2d             compare one
//   dotnet run --file build/gold-images.cs -- --only shapes-2d --update    capture and make it the golden
//   dotnet run --file build/gold-images.cs -- --only shapes-2d --noise     capture twice, report run-to-run drift
//   dotnet run --file build/gold-images.cs -- --gpu                        the real GPU: faster, for looking, never for a golden
//
// The --file switch is required: the repository root contains Stride.CommunityToolkit.ndproj, and
// without it the SDK runs that project and passes this script to it as an argument.
//
// WHAT IT IS FOR. A shader or renderer change that is meant to be invisible (a refactor) or meant
// to be visible in a known way (an anti-aliasing profile, a colour curve) needs more than a pair
// of screenshots and a squint. This runs the scenes in tests/Stride.CommunityToolkit.GoldScenes -
// one per feature area, with a fixed layout and nothing random, changed only to pin something new
// - captures each in-engine at a fixed frame on a fixed timestep (see ScreenshotCapture in
// src/Stride.CommunityToolkit/Engine), and compares the pixels against a golden PNG committed
// under tests/gold, the way Stride's own graphics tests do: the per-pixel maximum channel
// difference goes into a histogram and a rule says how many pixels may land in each bucket. The
// default rule is Stride's - any pixel differing by 3 or more fails - and tests/gold/thresholds.jsonc
// relaxes it per image, which the scenes are built not to need.
//
// WHAT IT WRITES. Every run leaves the new capture, a diff mask and a side-by-side contact sheet
// under screenshots-review/gold (gitignored). --update copies the capture over the golden;
// nothing is committed by this script. The workflow .github/workflows/gold-images.yml runs the
// same comparison on every pull request that touches a renderer and uploads that folder.
//
// DETERMINISM. Frame N on a fixed timestep with one update per draw is the same simulated instant
// every run, and the scenes pin the display scale to 100%, so what is left is the renderer. The
// goldens are captured on WARP, Direct3D's software adapter (STRIDE_GRAPHICS_SOFTWARE_RENDERING=1),
// the way Stride's own graphics tests run, because it is the one renderer every machine shares:
// a real GPU lands within a dozen levels of it, which is close, but over the rule. --gpu is for
// looking at a scene quickly, and its captures must not become goldens.

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var only = new List<string>();
var update = false;
var noise = false;
var warp = true;
int? frameOverride = null;
var timeout = TimeSpan.FromMinutes(5);

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--only" when i + 1 < args.Length:
            only.Add(args[++i]);
            break;
        case "--update":
            update = true;
            break;
        case "--noise":
            noise = true;
            break;
        case "--warp":
            warp = true;
            break;
        case "--gpu":
            warp = false;
            break;
        case "--frame" when i + 1 < args.Length && int.TryParse(args[i + 1], out var f):
            frameOverride = f;
            i++;
            break;
        case "--timeout" when i + 1 < args.Length && int.TryParse(args[i + 1], out var minutes):
            timeout = TimeSpan.FromMinutes(minutes);
            i++;
            break;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            return 1;
    }
}

if (update && only.Count == 0)
{
    Console.Error.WriteLine("--update needs --only <scene>: rewriting every golden in one unattended run is not something to do by accident.");
    return 1;
}

if (update && !warp)
{
    Console.Error.WriteLine("--update with --gpu would make a golden no other machine can match; capture goldens on WARP.");
    return 1;
}

var root = RepositoryRoot();
var goldDirectory = Path.Combine(root, "tests", "gold");
var reviewDirectory = Path.Combine(root, "screenshots-review", "gold");
var project = Path.Combine(root, "tests", "Stride.CommunityToolkit.GoldScenes", "Stride.CommunityToolkit.GoldScenes.csproj");
var scenesPath = Path.Combine(goldDirectory, "scenes.jsonc");

if (!File.Exists(scenesPath))
{
    Console.Error.WriteLine($"No scene list at {scenesPath}.");
    return 1;
}

Directory.CreateDirectory(reviewDirectory);

var scenes = LoadScenes(scenesPath);

// Without --only the suite is the whole list: the list defines what is under test.
var names = only.Count > 0 ? only : scenes.Select(s => s.Name).ToList();

foreach (var name in names.Where(n => scenes.All(s => !string.Equals(s.Name, n, StringComparison.OrdinalIgnoreCase))))
{
    Console.Error.WriteLine($"  ✖ {name}: not in tests/gold/scenes.jsonc");
}

// One build for every capture, rather than an msbuild evaluation per run.
Console.WriteLine($"Building the gold scenes on {(warp ? "WARP" : "the GPU")}...");

if (!Build(project, out var buildFailure))
{
    Console.Error.WriteLine(buildFailure);
    return 1;
}

var rules = LoadRules(Path.Combine(goldDirectory, "thresholds.jsonc"));
var results = new List<Result>();

foreach (var name in names)
{
    var scene = scenes.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    if (scene is null)
    {
        results.Add(new Result(name, Outcome.Error, "not in tests/gold/scenes.jsonc"));
        continue;
    }

    var frame = frameOverride ?? scene.Frame;
    var capturePath = Path.Combine(reviewDirectory, $"{scene.Name}.png");
    var goldPath = Path.Combine(goldDirectory, $"{scene.Name}.png");

    Console.WriteLine($"  · {scene.Name} (frame {frame?.ToString() ?? "default"})");

    if (!Capture(project, scene.Name, capturePath, frame, warp, timeout, out var failure))
    {
        Console.Error.WriteLine($"    ✖ {failure}");
        results.Add(new Result(scene.Name, Outcome.Error, failure));
        continue;
    }

    if (noise)
    {
        // A second capture of the same thing: what the harness would see with no change at all.
        var secondPath = Path.Combine(reviewDirectory, $"{scene.Name}-second.png");

        if (!Capture(project, scene.Name, secondPath, frame, warp, timeout, out failure))
        {
            Console.Error.WriteLine($"    ✖ {failure}");
            results.Add(new Result(scene.Name, Outcome.Error, failure));
            continue;
        }

        var drift = Compare(capturePath, secondPath, Resolve(rules, scene.Name), Path.Combine(reviewDirectory, $"{scene.Name}-noise.png"));

        Console.WriteLine($"    run-to-run: {drift.Describe()}");
        results.Add(new Result(scene.Name, drift.Passed ? Outcome.Pass : Outcome.Fail, drift.Describe(), drift));
        continue;
    }

    if (update)
    {
        File.Copy(capturePath, goldPath, overwrite: true);
        Console.WriteLine($"    ✅ golden written: tests/gold/{scene.Name}.png");
        results.Add(new Result(scene.Name, Outcome.Updated, "golden written"));
        continue;
    }

    if (!File.Exists(goldPath))
    {
        Console.Error.WriteLine($"    ✖ no golden at tests/gold/{scene.Name}.png - run with --update to make one");
        results.Add(new Result(scene.Name, Outcome.Error, "no golden"));
        continue;
    }

    var stats = Compare(capturePath, goldPath, Resolve(rules, scene.Name), Path.Combine(reviewDirectory, $"{scene.Name}-diff.png"));

    Console.WriteLine($"    {(stats.Passed ? "✅" : "✖")} {stats.Describe()}");
    results.Add(new Result(scene.Name, stats.Passed ? Outcome.Pass : Outcome.Fail, stats.Describe(), stats));
}

Console.WriteLine();
Console.WriteLine($"Passed {results.Count(r => r.Outcome == Outcome.Pass)}, failed {results.Count(r => r.Outcome == Outcome.Fail)}, " +
                  $"updated {results.Count(r => r.Outcome == Outcome.Updated)}, errors {results.Count(r => r.Outcome == Outcome.Error)}.");

if (!update)
{
    var indexPath = Path.Combine(reviewDirectory, "index.html");

    File.WriteAllText(indexPath, BuildIndex(results, goldDirectory, noise), new UTF8Encoding(false));
    Console.WriteLine($"Contact sheet: {indexPath}");
}

return results.Any(r => r.Outcome is Outcome.Fail or Outcome.Error) ? 1 : 0;

// Builds the scene project once; the captures then run it with --no-build.
static bool Build(string project, out string failure)
{
    failure = string.Empty;

    var startInfo = new ProcessStartInfo("dotnet", $"build \"{project}\" --nologo -v q")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    using var process = Process.Start(startInfo);

    if (process is null)
    {
        failure = "could not start dotnet";
        return false;
    }

    var output = process.StandardOutput.ReadToEndAsync();
    var error = process.StandardError.ReadToEndAsync();

    process.WaitForExit();

    if (process.ExitCode == 0) return true;

    failure = $"the gold scenes did not build:{Environment.NewLine}{output.Result}{error.Result}";
    return false;
}

// Runs one scene with capture enabled and waits for it to exit on its own.
static bool Capture(string project, string scene, string pngPath, int? frame, bool warp, TimeSpan timeout, out string failure)
{
    failure = string.Empty;

    File.Delete(pngPath);

    var startInfo = new ProcessStartInfo("dotnet", $"run --project \"{project}\" --no-build -- --scene {scene}")
    {
        WorkingDirectory = Path.GetDirectoryName(project),
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    startInfo.Environment["STRIDE_TOOLKIT_CAPTURE"] = pngPath;

    if (frame is { } value)
    {
        startInfo.Environment["STRIDE_TOOLKIT_CAPTURE_FRAME"] = value.ToString();
    }

    if (warp)
    {
        startInfo.Environment["STRIDE_GRAPHICS_SOFTWARE_RENDERING"] = "1";
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

    if (!File.Exists(pngPath))
    {
        failure = "the scene exited without writing a screenshot";
        return false;
    }

    return true;
}

// Stride's comparison, ported: the maximum channel difference of every pixel, counted into a
// histogram, and a rule saying how many pixels may fall into each bucket. Alpha is ignored: the
// saved render target carries whatever alpha the renderer left behind, which means nothing.
//
// The diff mask marks pixels that differ by 3 or more in red and by 1 or 2 in yellow, over a
// dimmed copy of the new capture, so a failure can be located without opening both images.
static Stats Compare(string actualPath, string expectedPath, AllowBucket[] buckets, string diffPath)
{
    using var actual = Image.Load<Rgba32>(actualPath);
    using var expected = Image.Load<Rgba32>(expectedPath);

    if (actual.Width != expected.Width || actual.Height != expected.Height)
    {
        return new Stats
        {
            Passed = false,
            SizeMismatch = $"{actual.Width}x{actual.Height} vs golden {expected.Width}x{expected.Height}"
        };
    }

    var histogram = new int[256];
    long squaredError = 0;
    var maxDiff = 0;

    // Where the pixels at 3 or more are, so a failure can be placed without opening the mask
    int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;

    using var mask = new Image<Rgba32>(actual.Width, actual.Height);

    for (var y = 0; y < actual.Height; y++)
    {
        for (var x = 0; x < actual.Width; x++)
        {
            var a = actual[x, y];
            var e = expected[x, y];
            var r = Math.Abs(a.R - e.R);
            var g = Math.Abs(a.G - e.G);
            var b = Math.Abs(a.B - e.B);
            var d = Math.Max(r, Math.Max(g, b));

            histogram[d]++;
            squaredError += (long)(r * r + g * g + b * b);
            maxDiff = Math.Max(maxDiff, d);

            if (d >= 3)
            {
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }

            mask[x, y] = d >= 3
                ? new Rgba32(255, 40, 40, 255)
                : d > 0
                    ? new Rgba32(255, 220, 0, 255)
                    : new Rgba32((byte)(a.R / 4), (byte)(a.G / 4), (byte)(a.B / 4), 255);
        }
    }

    mask.SaveAsPng(diffPath);

    var pixels = actual.Width * actual.Height;
    var mse = pixels > 0 ? (double)squaredError / (pixels * 3) : 0;

    return new Stats
    {
        Pixels = pixels,
        Exact = histogram[0],
        Bucket1To2 = Sum(histogram, 1, 2),
        Bucket3To5 = Sum(histogram, 3, 5),
        Bucket6To15 = Sum(histogram, 6, 15),
        Bucket16Plus = Sum(histogram, 16, 255),
        MaxDiff = maxDiff,
        Bounds = maxX >= 0 ? $"x {minX}-{maxX}, y {minY}-{maxY}" : null,
        Psnr = mse > 0 ? 10.0 * Math.Log10(255.0 * 255.0 / mse) : double.PositiveInfinity,
        Passed = buckets.All(bucket => Sum(histogram, bucket.Min, Math.Min(bucket.Max, 255)) <= bucket.Limit)
    };

    static int Sum(int[] histogram, int from, int to)
    {
        var total = 0;

        for (var d = from; d <= to; d++) total += histogram[d];

        return total;
    }
}

// scenes.jsonc: [{ "name": "shapes-2d", "frame": 60 }]. Comments with // are stripped before parsing.
static Scene[] LoadScenes(string path)
{
    var json = Regex.Replace(File.ReadAllText(path), @"//.*?$", "", RegexOptions.Multiline);

    // Read by hand: file-based apps run with reflection serialisation disabled.
    using var document = JsonDocument.Parse(json);

    return document.RootElement.EnumerateArray()
        .Select(element => new Scene(Text(element, "name") ?? throw new InvalidOperationException("A scene in scenes.jsonc has no name."), Int(element, "frame")))
        .ToArray();
}

// thresholds.jsonc: [{ "image": "shapes-3d", "allow": { "3-5": 2000, "6-15": 300, "16+": 50 } }]
// A bucket not listed for a matching rule is unlimited; an image with no rule gets the default,
// which allows nothing at 3 or above. Comments with // are stripped before parsing.
static Rule[] LoadRules(string path)
{
    if (!File.Exists(path)) return [];

    var json = Regex.Replace(File.ReadAllText(path), @"//.*?$", "", RegexOptions.Multiline);

    using var document = JsonDocument.Parse(json);
    var rules = new List<Rule>();

    foreach (var element in document.RootElement.EnumerateArray())
    {
        var allow = new Dictionary<string, int>();

        if (element.TryGetProperty("allow", out var allowElement) && allowElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var pair in allowElement.EnumerateObject())
            {
                allow[pair.Name] = pair.Value.GetInt32();
            }
        }

        rules.Add(new Rule(Text(element, "image"), allow));
    }

    return rules.ToArray();
}

static AllowBucket[] Resolve(Rule[] rules, string name)
{
    var rule = rules.FirstOrDefault(r => string.Equals(r.Image, name, StringComparison.OrdinalIgnoreCase));

    if (rule?.Allow is null || rule.Allow.Count == 0)
    {
        return [new AllowBucket(3, int.MaxValue, 0)];
    }

    return rule.Allow.Select(pair => AllowBucket.Parse(pair.Key, pair.Value)).ToArray();
}

// One row per scene: golden, new capture and the diff mask side by side, with the numbers.
static string BuildIndex(List<Result> results, string goldDirectory, bool noise)
{
    var html = new StringBuilder();

    html.Append("<!doctype html><meta charset=\"utf-8\"><title>Gold images</title>");
    html.Append("<style>body{font:14px system-ui;margin:16px;background:#111;color:#ddd}h2{margin:24px 0 4px}");
    html.Append(".row{display:grid;grid-template-columns:repeat(3,1fr);gap:8px}.row img{width:100%;background:#000}");
    html.Append(".pass{color:#7c6}.fail{color:#f66}.err{color:#fa4}.cap{color:#999;font-size:12px}</style>");
    html.Append($"<h1>Gold images - {DateTime.Now:yyyy-MM-dd HH:mm}</h1>");

    foreach (var result in results)
    {
        var cls = result.Outcome switch { Outcome.Pass => "pass", Outcome.Fail => "fail", _ => "err" };

        html.Append($"<h2>{result.Name} <span class=\"{cls}\">{result.Outcome}</span></h2><div class=\"cap\">{Escape(result.Message)}</div>");

        if (result.Outcome is Outcome.Pass or Outcome.Fail)
        {
            var left = noise ? $"{result.Name}.png" : Path.Combine(goldDirectory, $"{result.Name}.png");
            var middle = noise ? $"{result.Name}-second.png" : $"{result.Name}.png";
            var right = noise ? $"{result.Name}-noise.png" : $"{result.Name}-diff.png";

            html.Append("<div class=\"row\">");
            html.Append($"<div><div class=\"cap\">{(noise ? "first run" : "golden")}</div><img src=\"{Uri(left)}\"></div>");
            html.Append($"<div><div class=\"cap\">{(noise ? "second run" : "new")}</div><img src=\"{Uri(middle)}\"></div>");
            html.Append($"<div><div class=\"cap\">diff (red 3+, yellow 1-2)</div><img src=\"{Uri(right)}\"></div>");
            html.Append("</div>");
        }
    }

    return html.ToString();

    static string Escape(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;");
    static string Uri(string path) => Path.IsPathRooted(path) ? new Uri(path).AbsoluteUri : path;
}

static string? Text(JsonElement element, string name)
    => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

static int? Int(JsonElement element, string name)
    => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;

static string RepositoryRoot([CallerFilePath] string scriptPath = "")
    => Directory.GetParent(Path.GetDirectoryName(scriptPath)!)!.FullName;

enum Outcome { Pass, Fail, Updated, Error }

record Scene(string Name, int? Frame);

record Result(string Name, Outcome Outcome, string Message, Stats? Stats = null);

record Rule(string? Image, Dictionary<string, int>? Allow);

readonly record struct AllowBucket(int Min, int Max, int Limit)
{
    public static AllowBucket Parse(string key, int limit)
    {
        if (key.EndsWith('+')) return new AllowBucket(int.Parse(key[..^1], CultureInfo.InvariantCulture), int.MaxValue, limit);

        var parts = key.Split('-');

        return parts.Length == 2
            ? new AllowBucket(int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture), limit)
            : new AllowBucket(int.Parse(key, CultureInfo.InvariantCulture), int.Parse(key, CultureInfo.InvariantCulture), limit);
    }
}

sealed class Stats
{
    public int Pixels;
    public int Exact;
    public int Bucket1To2;
    public int Bucket3To5;
    public int Bucket6To15;
    public int Bucket16Plus;
    public int MaxDiff;
    public string? Bounds;
    public double Psnr;
    public bool Passed;
    public string? SizeMismatch;

    public string Describe()
        => SizeMismatch is not null
            ? $"size mismatch: {SizeMismatch}"
            : $"exact {Percent(Exact)}, 1-2: {Bucket1To2}, 3-5: {Bucket3To5}, 6-15: {Bucket6To15}, 16+: {Bucket16Plus}, max {MaxDiff}, " +
              $"PSNR {(double.IsPositiveInfinity(Psnr) ? "inf" : Psnr.ToString("0.0", CultureInfo.InvariantCulture))} dB" +
              (Bounds is not null ? $", 3+ within {Bounds}" : "");

    private string Percent(int count) => Pixels == 0 ? "-" : (100.0 * count / Pixels).ToString("0.00", CultureInfo.InvariantCulture) + "%";
}