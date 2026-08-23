#:package SixLabors.ImageSharp@3.1.12

// Screenshot capture for the code-only examples.
//
//   dotnet run --file build/capture-screenshots.cs -- --review
//   dotnet run --file build/capture-screenshots.cs -- --review --only mesh-outline --only particles
//   dotnet run --file build/capture-screenshots.cs -- --review --frame 400 --keep-png
//   dotnet run --file build/capture-screenshots.cs
//   dotnet run --file build/capture-screenshots.cs -- --force
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
// TWO MODES. --review writes every image to a scratch folder named by slug, alongside an index.html
// contact sheet for looking at all of them in one pass. Without it each image lands on its real
// filename in the docs media folder. Review first, docs second: a screenshot that renders black,
// catches a scene mid-explosion or frames nothing but sky is only detectable by looking at it, and
// NOTHING IS COMMITTED AUTOMATICALLY either way.

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

var only = new List<string>();
var force = false;
var keepPng = false;
string? outputDirectory = null;
int? frameOverride = null;
var timeout = TimeSpan.FromMinutes(5);

// The folder --review writes to. Gitignored, and at the repository root so it is easy to find.
const string ReviewDirectory = "screenshots-review";

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
        case "--review":
            outputDirectory ??= ReviewDirectory;
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

// With --review everything is written to a scratch folder named by slug, for review in bulk before
// any of it goes near the docs. Without it, each image lands on its real media filename.
var reviewing = outputDirectory is not null;
var reviewDirectory = reviewing ? Path.GetFullPath(Path.Combine(root, outputDirectory!)) : null;

// Refuses to start if two examples would write to the same file.
//
// MetadataValidator rejects a duplicate media: name, but only fails the build under --strict, and a
// plain 'generate' still writes the manifest. Checking here too costs nothing and closes the gap,
// because the symptom is silent: the run reports every capture as a success and simply leaves one
// fewer file than it claims. That is how the PartialTorus C#/F# pair went unnoticed - 49 captures,
// 48 files.
if (!reviewing)
{
    var collisions = examples
        .Where(e => Text(e, "slug") is not null && Bool(e, "screenshot") != false)
        .Where(e => only.Count == 0 || only.Contains(Text(e, "slug")!, StringComparer.OrdinalIgnoreCase))
        .GroupBy(e => Text(e, "media") ?? $"{Text(e, "slug")}.webp", StringComparer.OrdinalIgnoreCase)
        .Where(group => group.Count() > 1)
        .ToList();

    foreach (var collision in collisions)
    {
        Console.Error.WriteLine(
            $"✖ {collision.Key} is claimed by {string.Join(", ", collision.Select(e => Text(e, "slug")))}. " +
            "One capture would overwrite the other.");
    }

    if (collisions.Count > 0)
    {
        return 1;
    }
}

var captured = 0;
var skipped = 0;
var failed = new List<string>();

// Why each example has no image, for the contact sheet to show in place of one.
var notes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

foreach (var example in examples)
{
    var slug = Text(example, "slug");
    var projectName = Text(example, "projectName");
    var projectPath = Text(example, "projectPath");

    if (slug is null || projectName is null || projectPath is null) continue;

    if (only.Count > 0 && !only.Contains(slug, StringComparer.OrdinalIgnoreCase))
    {
        notes[slug] = "not part of this run";
        continue;
    }

    if (Bool(example, "screenshot") == false)
    {
        Console.WriteLine($"  - {slug}: screenshot: false");
        notes[slug] = "screenshot: false - cannot produce a meaningful frame on its own";
        skipped++;
        continue;
    }

    var mediaName = reviewing ? $"{slug}.webp" : Text(example, "media") ?? $"{slug}.webp";
    var webpPath = Path.Combine(reviewing ? reviewDirectory! : mediaDirectory, mediaName);

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
        notes[slug] = failure;
        failed.Add(slug);
        continue;
    }

    if (!File.Exists(pngPath))
    {
        Console.Error.WriteLine("    ✖ the example exited without writing a screenshot");
        notes[slug] = "the example exited without writing a screenshot";
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

if (reviewing)
{
    var indexPath = Path.Combine(reviewDirectory!, "index.html");

    Directory.CreateDirectory(reviewDirectory!);
    File.WriteAllText(indexPath, BuildIndex(examples, reviewDirectory!, notes), new UTF8Encoding(false));

    Console.WriteLine();
    Console.WriteLine($"Contact sheet: {indexPath}");
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

    using var image = Image.Load<Rgba32>(pngPath);

    Opaque(image);

    // FileFormat must be set explicitly: the encoder defaults to lossless, where Quality is ignored and
    // a 1280x720 frame lands around 200 KB instead of 40 KB.
    image.Save(webpPath, new WebpEncoder { FileFormat = WebpFileFormatType.Lossy, Quality = 85 });
}

// Forces every pixel opaque.
//
// The saved render target carries the alpha the renderer happened to leave behind, and for the 3D
// examples that is nearly nothing: a sky pixel comes out rgba(80,86,93,24). Against a white page it
// washes out, against a dark one it turns murky, and the 2D examples - which end up fully opaque - hid
// the problem by looking fine.
//
// The alpha is straight, not premultiplied: 80 at alpha 24 would imply a true value near 850 if it
// were. So the colour underneath is already correct and only the channel is wrong, which is why this
// overwrites alpha rather than compositing onto a background colour. Compositing would multiply the
// colour by 24/255 and render the scene almost black.
static void Opaque(Image<Rgba32> image)
    => image.ProcessPixelRows(accessor =>
    {
        for (var y = 0; y < accessor.Height; y++)
        {
            var row = accessor.GetRowSpan(y);

            for (var x = 0; x < row.Length; x++)
            {
                row[x].A = byte.MaxValue;
            }
        }
    });

// Builds the contact sheet: every example in the manifest, its image if one is on disk, and enough
// metadata to judge whether the image suits the example without opening anything else.
//
// It lists all of them rather than only this run's, so that re-running a handful with --only leaves a
// complete page behind instead of a page about those few. An image is shown whenever the file exists,
// which is what makes that work.
static string BuildIndex(List<JsonElement> examples, string directory, Dictionary<string, string> notes)
{
    // Teaching order, not alphabetical: the sheet is read top to bottom the way the docs are.
    string[] levelOrder = ["Getting Started", "Beginner", "Intermediate", "Advanced"];

    var cards = new List<(string Level, int Order, string Slug, string Html, bool HasImage)>();

    foreach (var example in examples)
    {
        var slug = Text(example, "slug");

        if (slug is null) continue;

        var level = Text(example, "level") ?? "Other";
        var title = Localised(example, "title") ?? Text(example, "projectName") ?? slug;
        var hasImage = File.Exists(Path.Combine(directory, $"{slug}.webp"));

        cards.Add((level, Int(example, "order") ?? int.MaxValue, slug, Card(example, slug, title, level, hasImage, notes), hasImage));
    }

    var ordered = cards
        .OrderBy(c => Array.IndexOf(levelOrder, c.Level) is var rank && rank >= 0 ? rank : levelOrder.Length)
        .ThenBy(c => c.Level, StringComparer.Ordinal)
        .ThenBy(c => c.Order)
        .ThenBy(c => c.Slug, StringComparer.Ordinal)
        .ToList();

    var body = new StringBuilder();
    string? section = null;

    foreach (var card in ordered)
    {
        if (card.Level != section)
        {
            if (section is not null) body.AppendLine("</div>");

            section = card.Level;

            var count = ordered.Count(c => c.Level == section);
            body.AppendLine($"<h2 class=\"section\">{Esc(section)} <span class=\"count\">{count}</span></h2>");
            body.AppendLine("<div class=\"grid\">");
        }

        body.AppendLine(card.Html);
    }

    if (section is not null) body.AppendLine("</div>");

    var total = ordered.Count;
    var withImage = ordered.Count(c => c.HasImage);

    return $$"""
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Example screenshots - review</title>
<style>
:root {
  color-scheme: light dark;
  --bg: #fbfbfc; --panel: #fff; --ink: #16181d; --muted: #6a7280;
  --line: #e3e5ea; --accent: #2f6feb; --warn: #b45309; --warn-bg: #fef3c7;
}
@media (prefers-color-scheme: dark) {
  :root {
    --bg: #14161a; --panel: #1c1f25; --ink: #e8eaef; --muted: #9aa2ae;
    --line: #2c3038; --accent: #6ea8ff; --warn: #fbbf24; --warn-bg: #3a2f12;
  }
}
* { box-sizing: border-box; }
body {
  margin: 0; background: var(--bg); color: var(--ink);
  font: 15px/1.55 system-ui, -apple-system, "Segoe UI", sans-serif;
}
header { padding: 28px 32px 0; }
h1 { margin: 0 0 4px; font-size: 22px; letter-spacing: -0.01em; }
.lede { margin: 0 0 18px; color: var(--muted); font-size: 14px; }
.toolbar {
  position: sticky; top: 0; z-index: 5; display: flex; flex-wrap: wrap; gap: 10px;
  align-items: center; padding: 12px 0; margin-bottom: 8px;
  background: var(--bg); border-bottom: 1px solid var(--line);
}
input[type=search], select {
  padding: 7px 11px; border: 1px solid var(--line); border-radius: 7px;
  background: var(--panel); color: inherit; font: inherit;
}
input[type=search] { min-width: 260px; flex: 1 1 260px; }
button {
  padding: 7px 13px; border: 1px solid var(--line); border-radius: 7px;
  background: var(--panel); color: inherit; font: inherit; cursor: pointer;
}
button:hover { border-color: var(--accent); }
label.toggle { display: inline-flex; align-items: center; gap: 6px; color: var(--muted); font-size: 14px; }
main { padding: 0 32px 64px; }
.section {
  margin: 34px 0 14px; font-size: 15px; text-transform: uppercase;
  letter-spacing: 0.07em; color: var(--muted);
}
.section .count { color: var(--line); }
.grid {
  display: grid; gap: 18px;
  grid-template-columns: repeat(auto-fill, minmax(340px, 1fr));
}
.card {
  display: flex; flex-direction: column; overflow: hidden;
  background: var(--panel); border: 1px solid var(--line); border-radius: 11px;
}
.card.hidden { display: none; }
.shot { display: block; aspect-ratio: 16 / 9; background: #0d0f13; }
.shot img { width: 100%; height: 100%; object-fit: contain; display: block; }
.missing {
  display: flex; align-items: center; justify-content: center; padding: 16px;
  text-align: center; font-size: 13px; color: var(--warn); background: var(--warn-bg);
}
.body { padding: 13px 15px 15px; display: flex; flex-direction: column; gap: 9px; flex: 1; }
h3 { margin: 0; font-size: 15.5px; }
.chips { display: flex; flex-wrap: wrap; gap: 6px; }
.chip {
  padding: 2px 8px; border-radius: 20px; font-size: 11.5px;
  background: var(--bg); border: 1px solid var(--line); color: var(--muted);
}
.chip.cat { border-color: var(--accent); color: var(--accent); }
.desc { margin: 0; font-size: 13px; color: var(--muted); }
.tags { font-size: 11.5px; color: var(--muted); word-break: break-word; }
.slug { font: 12px/1 ui-monospace, Consolas, monospace; color: var(--muted); }
.verdict { display: flex; flex-wrap: wrap; gap: 6px; margin-top: auto; padding-top: 4px; }
.verdict button { padding: 4px 9px; font-size: 12px; border-radius: 6px; }
.verdict button[aria-pressed=true] { background: var(--accent); border-color: var(--accent); color: #fff; }
dialog { border: none; border-radius: 10px; padding: 16px; max-width: min(720px, 92vw); background: var(--panel); color: var(--ink); }
dialog textarea { width: 100%; height: 320px; font: 12px/1.5 ui-monospace, Consolas, monospace; }
</style>
</head>
<body>
<header>
  <h1>Example screenshots &mdash; review</h1>
  <p class="lede">{{withImage}} of {{total}} examples have an image. Click a shot to open it full size. Verdicts are saved in this browser; <em>Copy feedback</em> puts them on the clipboard.</p>
  <div class="toolbar">
    <input type="search" id="q" placeholder="Filter by title, slug, tag or category&hellip;" autocomplete="off">
    <select id="level"><option value="">All levels</option></select>
    <label class="toggle"><input type="checkbox" id="issues"> Missing only</label>
    <label class="toggle"><input type="checkbox" id="unjudged"> Unjudged only</label>
    <button id="copy">Copy feedback</button>
    <button id="reset">Clear verdicts</button>
  </div>
</header>
<main id="grid">
{{body}}
</main>
<dialog id="fallback">
  <p>Clipboard unavailable &mdash; copy this manually:</p>
  <textarea readonly></textarea>
  <p><button onclick="this.closest('dialog').close()">Close</button></p>
</dialog>
<script>
const store = (() => {
  // localStorage is unavailable in some file:// setups; degrade to a session-only object.
  try { localStorage.setItem('__t', '1'); localStorage.removeItem('__t'); return localStorage; }
  catch { const m = {}; return { getItem: k => m[k] ?? null, setItem: (k, v) => m[k] = v, removeItem: k => delete m[k] }; }
})();

const KEY = 'stride-screenshot-review';
let verdicts = {};
try { verdicts = JSON.parse(store.getItem(KEY) || '{}'); } catch { verdicts = {}; }

const cards = [...document.querySelectorAll('.card')];
const levels = [...new Set(cards.map(c => c.dataset.level))];
const levelSelect = document.getElementById('level');

for (const level of levels) {
  levelSelect.insertAdjacentHTML('beforeend', `<option>${level}</option>`);
}

function paint(card) {
  const verdict = verdicts[card.dataset.slug];
  for (const button of card.querySelectorAll('.verdict button')) {
    button.setAttribute('aria-pressed', String(button.dataset.v === verdict));
  }
}

function save() {
  store.setItem(KEY, JSON.stringify(verdicts));
}

function filter() {
  const needle = document.getElementById('q').value.trim().toLowerCase();
  const level = levelSelect.value;
  const missingOnly = document.getElementById('issues').checked;
  const unjudgedOnly = document.getElementById('unjudged').checked;

  for (const card of cards) {
    const hide =
      (needle && !card.dataset.text.includes(needle)) ||
      (level && card.dataset.level !== level) ||
      (missingOnly && card.dataset.image === 'yes') ||
      (unjudgedOnly && verdicts[card.dataset.slug]);

    card.classList.toggle('hidden', !!hide);
  }
}

document.getElementById('grid').addEventListener('click', event => {
  const button = event.target.closest('.verdict button');
  if (!button) return;

  const card = button.closest('.card');
  const slug = card.dataset.slug;

  if (verdicts[slug] === button.dataset.v) delete verdicts[slug];
  else verdicts[slug] = button.dataset.v;

  save();
  paint(card);
  filter();
});

document.getElementById('copy').addEventListener('click', async () => {
  const labels = { ok: 'ok', frame: 'wrong moment', framing: 'bad framing', drop: 'drop' };
  const lines = [];

  for (const key of ['ok', 'frame', 'framing', 'drop']) {
    const slugs = Object.keys(verdicts).filter(s => verdicts[s] === key).sort();
    if (slugs.length) lines.push(`## ${labels[key]} (${slugs.length})`, ...slugs.map(s => `- ${s}`), '');
  }

  const text = lines.length ? lines.join('\n') : 'No verdicts recorded.';

  try {
    await navigator.clipboard.writeText(text);
    const button = document.getElementById('copy');
    button.textContent = 'Copied';
    setTimeout(() => button.textContent = 'Copy feedback', 1400);
  } catch {
    const dialog = document.getElementById('fallback');
    dialog.querySelector('textarea').value = text;
    dialog.showModal();
  }
});

document.getElementById('reset').addEventListener('click', () => {
  verdicts = {};
  save();
  cards.forEach(paint);
  filter();
});

for (const id of ['q', 'level', 'issues', 'unjudged']) {
  document.getElementById(id).addEventListener('input', filter);
}

cards.forEach(paint);
filter();
</script>
</body>
</html>
""";
}

// One card. Everything the eye needs to judge the image is on it, so reviewing does not mean
// alt-tabbing to the manifest to remember what the example was supposed to show.
static string Card(JsonElement example, string slug, string title, string level, bool hasImage, Dictionary<string, string> notes)
{
    var category = Text(example, "category");
    var language = Text(example, "language");
    var frame = Int(example, "screenshotFrame");
    var complexity = Int(example, "complexity");
    var tags = Strings(example, "tags");
    var summary = FirstSentence(Localised(example, "description"));

    // Searched against as one lowercase blob - simpler than per-field matching, and a reviewer
    // typing "bepu" does not care which field it came from.
    var haystack = string.Join(' ', new[] { slug, title, category, level, language, summary }
        .Concat(tags)
        .Where(v => !string.IsNullOrEmpty(v))).ToLowerInvariant();

    var card = new StringBuilder();

    card.AppendLine($"<article class=\"card\" data-slug=\"{Esc(slug)}\" data-level=\"{Esc(level)}\" data-image=\"{(hasImage ? "yes" : "no")}\" data-text=\"{Esc(haystack)}\">");

    if (hasImage)
    {
        card.AppendLine($"  <a class=\"shot\" href=\"{Esc(slug)}.webp\" target=\"_blank\" rel=\"noopener\"><img loading=\"lazy\" src=\"{Esc(slug)}.webp\" alt=\"{Esc(title)}\"></a>");
    }
    else
    {
        card.AppendLine($"  <div class=\"shot missing\">no image &mdash; {Esc(notes.GetValueOrDefault(slug, "not captured"))}</div>");
    }

    card.AppendLine("  <div class=\"body\">");
    card.AppendLine($"    <h3>{Esc(title)}</h3>");
    card.AppendLine("    <div class=\"chips\">");

    if (category is not null) card.AppendLine($"      <span class=\"chip cat\">{Esc(category)}</span>");
    if (language is not null and not "csharp") card.AppendLine($"      <span class=\"chip\">{Esc(language)}</span>");
    if (complexity is { } value) card.AppendLine($"      <span class=\"chip\">complexity {value}/5</span>");

    card.AppendLine($"      <span class=\"chip\">frame {(frame?.ToString() ?? "default")}</span>");
    card.AppendLine("    </div>");

    if (summary is not null) card.AppendLine($"    <p class=\"desc\">{Esc(summary)}</p>");
    if (tags.Count > 0) card.AppendLine($"    <div class=\"tags\">{Esc(string.Join(" · ", tags))}</div>");

    card.AppendLine($"    <div class=\"slug\">{Esc(slug)}</div>");
    card.AppendLine("    <div class=\"verdict\">");
    card.AppendLine("      <button data-v=\"ok\">ok</button>");
    card.AppendLine("      <button data-v=\"frame\">wrong moment</button>");
    card.AppendLine("      <button data-v=\"framing\">bad framing</button>");
    card.AppendLine("      <button data-v=\"drop\">drop</button>");
    card.AppendLine("    </div>");
    card.AppendLine("  </div>");
    card.AppendLine("</article>");

    return card.ToString();
}

static string? FirstSentence(string? description)
{
    if (description is not { Length: > 0 }) return null;

    var text = string.Join(' ', description.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    var stop = text.IndexOf(". ", StringComparison.Ordinal);

    return stop > 0 ? text[..(stop + 1)] : text;
}

static string Esc(string? value)
    => (value ?? string.Empty)
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
        .Replace("\"", "&quot;").Replace("'", "&#39;");

static string? Text(JsonElement element, string name)
    => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

// Titles and descriptions are per-language maps; the sheet is read in English.
static string? Localised(JsonElement element, string name)
    => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Object ? Text(value, "en") : null;

static List<string> Strings(JsonElement element, string name)
    => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
        ? value.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString()!).ToList()
        : [];

static bool? Bool(JsonElement element, string name)
    => element.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

static int? Int(JsonElement element, string name)
    => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;

static string RepositoryRoot([CallerFilePath] string scriptPath = "")
    => Directory.GetParent(Path.GetDirectoryName(scriptPath)!)!.FullName;
