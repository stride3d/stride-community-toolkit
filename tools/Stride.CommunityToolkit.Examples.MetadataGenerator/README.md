# Stride Community Toolkit - Examples Metadata Generator

Scans the code-only example projects, validates the `---example-metadata` block each one carries, and
writes `examples-manifest.json`.

The manifest is the single source of truth for the docs generator and both example launchers. The
schema is documented in
[`docs/contributing/examples/metadata-schema.md`](../../docs/contributing/examples/metadata-schema.md),
and the reasoning behind it in
[`docs/contributing/examples/decisions.md`](../../docs/contributing/examples/decisions.md).

## Commands

All three take the examples root as an optional positional argument. It defaults to
`../../examples/code-only` **relative to the current directory**, so these commands are meant to be run
from this folder and the examples below leave it out.

### `scan [examples-root-path] [--media-path <dir>]`

Finds every metadata block, validates it, and prints the findings. Writes nothing.

```bash
dotnet run -- scan
```

### `generate [examples-root-path] [--output <file>] [--media-path <dir>] [--strict]`

The same scan, then writes the manifest.

```bash
dotnet run -- generate --output examples-manifest.json
```

| Option | Meaning |
|---|---|
| `--output`, `-o` | Manifest path. Defaults to `examples-manifest.json` in the current directory. |
| `--media-path` | Docs media folder. When given, every explicit `media:` filename is checked to exist. Skipped when omitted. |
| `--strict` | Treat validation errors as fatal: report them, write no manifest, exit non-zero. |

### `docs [examples-root-path] [--docs-path <dir>] [--media-path <dir>] [--dry-run]`

Generates the documentation: one page per example, a landing page per language and level group, the
examples folder's own `toc.yml`, and redirect stubs for the URLs that levels replaced.

```bash
dotnet run -- docs --dry-run
```

| Option | Meaning |
|---|---|
| `--docs-path` | The examples documentation folder. Defaults to `../../docs/manual/code-only/examples`. |
| `--media-path` | Screenshot folder. An image is linked only when the file actually exists. |
| `--dry-run` | List the files that would change and write nothing. |

The source listing on each page stops before the `---example-metadata` block, using a DocFX line range
computed while parsing. That keeps the fix at generation time - no DocFX plugin and no post-processing
pass over generated HTML. It only works when the block is the last thing in its file; otherwise the
whole file is included and validation warns.

Validation errors always stop this command, with or without `--strict`: a page built from a bad block
is wrong in ways that are tedious to spot by reading it. Files whose content would not change are left
untouched, so the git diff shows only real changes.

**Ownership is opt-in per file.** A page is rewritten only if its own frontmatter says so:

| `generated:` | Effect |
|---|---|
| `true` | The whole file is tool-owned and overwritten |
| `partial` | Only the text between `<!-- #region generated -->` and `<!-- #endregion generated -->` is replaced. A missing marker is a warning, never a guess |
| absent or `false` | Hand-owned. Never touched |

That is what made adoption safe: none of the documentation written by hand carried frontmatter, so the
first run could not have overwritten any of it.

### Exit codes

| Code | Meaning |
|---|---|
| `0` | Success. |
| `1` | Could not run - missing directory, failed write. |
| `2` | Scan found no metadata blocks at all, which almost always means the wrong path. |
| `3` | Validation errors, with `--strict` in force (`generate`) or always (`scan`). |

## Discovery

Every `.cs`, `.fs` and `.vb` file under the examples root is examined, excluding `bin` and `obj`. The
**metadata block itself** marks a file as an example - there is no `Program.cs` convention - which lets
file-based apps use a self-describing filename and lets one folder hold several examples. A unique
`slug` is what keeps that honest.

The block is a comment, so its delimiters follow the language:

```csharp
/* ---example-metadata
slug: mesh-outline
...
--- */
```

```fsharp
(* ---example-metadata
slug: mesh-outline-fs
...
--- *)
```

```vb
' ---example-metadata
' slug: mesh-outline-vb
' ...
' ---
```

## Validation

Findings are aggregated: one run reports every problem across every example, rather than stopping at
the first. Errors are fatal only under `--strict`, which the Launcher pre-build hook passes.

Findings come in three severities. **Errors and warnings go to stderr**, everything else to stdout, and
the pre-build hook raises stderr at high importance while lowering stdout - so a clean build prints one
line, a build with a warning prints the warning, and a failing one prints every finding. `Info` is the
third: it records something the generator *decided*, such as a `related:` link dropped because its
target is `enabled: false`, and is deliberately kept out of the warning count. A warning nobody can act
on appears on every build and stops being read.

When adding output here, never write a line where the word `error` or `warning` follows a colon.
Visual Studio runs its own error-format parser over task output and will turn it into a red row in the
Error List, on a build that MSBuild considers clean.

A duplicate `order` within a group is a **warning**, not an error: the tie is broken by `slug`, which is
required and unique, so the sequence stays stable - the author has just not said which of the two comes
first.

Checked: required fields (`slug`, `title.en`, `level`, `category`); kebab-case and globally unique
`slug`; `level` / `category` / `language` against the closed sets in `Core/MetadataVocabulary.cs`;
`complexity` within 1-5; `related:` names resolving to real project folders; explicit `media:` files
existing; `language:` agreeing with the file extension; and `level` names not duplicated into `tags`.

Two checks run against the **source text** rather than the parsed object, because they are invisible
afterwards - see `Core/YamlSourceInspector.cs`:

- **An unquoted `#`** starts a YAML comment and silently truncates its value. `- Declaring NuGet
  packages inline with #:package` becomes `- Declaring NuGet packages inline with`, with no error.
- **An unquoted `": "` inside a sequence item** turns the item into a mapping and aborts parsing deep
  inside YamlDotNet with "Uninitialized Strings cannot be created" - a message that names neither the
  line nor the cause. The inspector's diagnosis is attached to the failure, ahead of the deserializer's
  own message.

**Unknown keys are reported, with a suggestion.** `IgnoreUnmatchedProperties` is still enabled on the
deserializer, so a stray key does not abort the file; instead the literal key list is captured and
diffed against the schema. This is the check that catches `Order:` - which, under the camelCase naming
convention, was silently discarded from two examples.

## Output

A versioned envelope, not a bare array:

```json
{
  "schemaVersion": 1,
  "generatedAt": "2026-08-22T22:28:23.1257046Z",
  "toolVersion": "1.0.0.0",
  "count": 57,
  "examples": [ { "slug": "mesh-outline", "...": "..." } ]
}
```

Examples are sorted by language, then level, then `order`. `projectPath` uses forward slashes so the
file is identical whichever platform generated it, and non-ASCII characters are written literally so
the Czech titles stay readable in review.

`examples-manifest.json` is a **build artifact** - `generatedAt` changes on every run - so it is
gitignored and regenerated by `tools/ExamplesManifest.targets`, which both example launchers import.

## Consumers

Both launchers read the manifest and nothing else; the `<ExampleTitle>` csproj properties they used to
scan were deleted in Phase 2. The reader lives in
`Stride.CommunityToolkit.Examples/Core/` (`ExampleManifest.cs`, `ExampleEntry.cs`, `ManifestLoader.cs`)
and is linked into the Avalonia launcher rather than shared through a third project.

It is a deliberate second copy of the shape written here, not a reference to this project: a launcher
should not drag in a generic host, Serilog and YamlDotNet to read a JSON file. The two are coupled by
`schemaVersion`, which the loader refuses if it is newer than it understands.

### Screenshot capture

`build/capture-screenshots.cs` is the third consumer. It runs each example once with
`STRIDE_TOOLKIT_CAPTURE` set - which is what makes the toolkit's own `ScreenshotCapture` save a frame
and exit - then converts the PNG to WebP. From the manifest it reads `slug`, `projectPath`, `media`,
`screenshot` and `screenshotFrame`: what to run, whether to run it, which frame to keep and what to
call the result.

```bash
dotnet run --file build/capture-screenshots.cs -- --review
```

`--review` writes every image to `screenshots-review/` at the repository root, named by slug, with an
`index.html` contact sheet beside them - all of them on one page with their title, category, tags and
capture frame, plus per-image verdict buttons that copy out as markdown. Nothing reaches the docs media
folder until the command runs *without* `--review`, and an image already there is never replaced
without `--force`.

It reads the JSON directly rather than linking the model, for the same reason the launchers keep their
own copy: a file-based script should not have to build a project to look at a handful of fields.

The cost of that is a staleness trap, worth knowing before it bites: because the script reads the
manifest and never the metadata blocks, an edited `screenshotFrame` does nothing until the manifest is
regenerated - `dotnet build tools/Stride.CommunityToolkit.Examples.Launcher`. A missing manifest is
reported; a stale one is not, and captures at the old frame while reporting success. Use `--frame <n>`
to search for the right moment, then write it into the metadata and rebuild once.

## Architecture

```
Core/
  MetadataVocabulary.cs      # The closed sets: levels, categories, languages. Add new values here.
  MetadataBlockLocation.cs   # Where the block sits, so docs can include the code without it
  ParsedExample.cs           # A parsed block plus the raw text and literal keys it came from
  ValidationMessage.cs       # Severity + attribution for a single finding
  YamlMetadataExtractor.cs   # Pulls the block out of C# / F# / VB comment syntax
  YamlSourceInspector.cs     # Source-text checks that survive a failed parse
  DocOwnership.cs            # Reads the generated:/partial marker out of an existing page
  DocPaths.cs                # Doc-site paths, filenames and language labels
Services/
  ExampleScanner.cs          # Finds candidate files and project folders
  MetadataParser.cs          # Deserializes one block; normalises paths and trailing newlines
  MetadataValidator.cs       # Aggregated schema validation; resolves related: to slugs
  ManifestService.cs         # Orchestration, reporting, exit codes
  ManifestWriter.cs          # Serializes the envelope
  DocPageBuilder.cs          # Renders example pages, landing pages and redirect stubs
  DocsGenerator.cs           # Applies file ownership and writes the docs
CommandLineConfiguration.cs  # CLI structure (instance-based, takes IServiceProvider)
ExampleMetadata.cs           # Schema v1 model
ExampleManifest.cs           # The envelope
Program.cs                   # Host + DI setup
```

`System.CommandLine.Hosting` is deprecated, so DI is wired by hand: the host is built once, handlers
are registered as scoped services, `CommandLineConfiguration` takes the `IServiceProvider`, and each
command opens its own scope.

The content root is pinned to `AppContext.BaseDirectory` rather than the working directory. Without
that, running the tool from anywhere else - which the pre-build hook does - finds no `appsettings.json`,
configures no Serilog sinks, and produces no output at all.

The build output is `MetadataGenerator.exe` (`<AssemblyName>`), with the namespace unchanged.
