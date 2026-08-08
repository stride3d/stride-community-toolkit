# Building the Toolkit

How the repository is built, why the examples build is configured the way it is, and how to produce
local NuGet packages for testing.

## Solutions and solution filters

| File | Contents | Use it when |
|---|---|---|
| `Stride.CommunityToolkit.slnx` | Everything: libraries, tests, tools, benchmarks and all example projects | Verifying a change across every example, e.g. after a Stride upgrade |
| `Stride.CommunityToolkit.Core.slnf` | Libraries, tests and tools only | Day-to-day library work |

The repository contains 56 example projects. Loading all of them slows the IDE noticeably, so the
solution filter exists to skip them. Open the `.slnf` exactly like a solution; excluded projects
still appear in Solution Explorer as unloaded nodes, and **Load Project** pulls in any single one
when you need to debug it. **Load All Projects** restores the full set without switching files.

Because a filter is only a view over `Stride.CommunityToolkit.slnx`, adding a project to the
solution automatically makes it available in the filter, and no filter update is needed.

`dotnet build` accepts a filter too:

```bash
dotnet build Stride.CommunityToolkit.Core.slnf
```

## Why the examples build is fast

Two files under `examples/` keep the example build small. Without them, **each** example project
copies roughly 476 MB into `bin`, most of which is unreachable from a desktop example: about 239 MB
of Android native runtimes, a further ~38 MB of iOS/tvOS/macOS, and 54 MB of XML documentation from
referenced packages.

| File | What it does |
|---|---|
| `examples/Directory.Build.props` | Restricts the build to the host runtime identifier, so only the current platform's native runtimes are copied |
| `examples/Directory.Build.targets` | Removes package XML documentation from the output |

Together these take an example from ~476 MB to ~90 MB, and a clean build of the whole solution to
well under a minute.

Two details worth knowing before editing them:

1. **Both files explicitly import the repository-root equivalent.** MSBuild imports only the
   *nearest* `Directory.Build.props`/`.targets`, so without that import the settings defined at the
   repository root, such as `TargetFramework` and `StrideVersion`, would be silently lost.
2. **The runtime identifier is derived from the host OS**, not hard-coded to Windows, so Linux and
   macOS builds keep working. `NETCoreSdkPortableRuntimeIdentifier` would be the obvious source for
   this but the SDK sets it *after* `Directory.Build.props` is evaluated, so explicit
   `IsOSPlatform` checks are used instead.

> [!NOTE]
> `AppendRuntimeIdentifierToOutputPath` is disabled deliberately, so output stays at
> `bin/<Configuration>/net10.0/` rather than gaining a `win-x64/` segment. This keeps documented
> paths and launcher commands valid.

## Building local NuGet packages

To test the toolkit the way a consumer uses it, through `PackageReference` rather than
`ProjectReference`, build the packages into a local feed:

```bash
build\pack-local.bat
```

Or, on any platform:

```bash
dotnet run --file build/pack-local.cs
```

Useful arguments: `--version <version>`, `--configuration <configuration>` and `--clean`.

This packs every publishable library into `bin/packages` as version `1.0.0-dev`. The version is
fixed by design, so a reference such as `Stride.CommunityToolkit.Bepu@1.0.0-dev` keeps working
across rebuilds and never has to be edited.

> [!IMPORTANT]
> NuGet keys its global cache by package id and version, and will not re-extract a rebuilt package
> that carries a version it has already seen. The script therefore deletes the matching folders under
> `~/.nuget/packages` before packing, so a freshly built package always wins. This mirrors what
> Stride does in `build/install-gamestudio.targets`.

### Consuming the local packages

The script also writes a ready-to-use `bin/packages/NuGet.config`. Copy it next to the project that
should consume the packages, then reference them normally:

```xml
<PackageReference Include="Stride.CommunityToolkit.Bepu" Version="1.0.0-dev" />
```

Nothing machine-wide needs changing. NuGet merges that configuration with any existing one, so
nuget.org and the Stride dev feed keep resolving exactly as before.

> [!WARNING]
> The `packageSourceMapping` entry in that file is required, not optional. Once **any** NuGet
> configuration on the machine defines `packageSourceMapping`, and the Stride development setup does,
> a source that is not mapped is silently never consulted. The symptom is not an error: the local
> packages quietly resolve to an older `-preview` version from nuget.org instead.

The file maps two patterns, and both are needed:

```xml
<package pattern="Stride.CommunityToolkit" />
<package pattern="Stride.CommunityToolkit.*" />
```

`Stride.CommunityToolkit.*` requires a dot after the prefix, so it matches
`Stride.CommunityToolkit.Bepu` and friends but **not** the base `Stride.CommunityToolkit` package.
Without the exact-name pattern the base package falls through to the nuget.org catch-all and quietly
resolves to an old published prerelease, because NuGet compares prerelease labels alphabetically and
`dev` sorts before `preview`.

Both are more specific than the `Stride.*` mapped to the Stride dev feed. NuGet resolves by longest
matching prefix, so the toolkit packages come from the local feed without disturbing how Stride
packages resolve.

## Testing local packages inside this repository

The examples reference the libraries by `ProjectReference`, which always wins over a package. To test
an example against the local packages instead:

1. **Copy `bin/packages/NuGet.config` into that example's folder first**, before touching the project
   file. NuGet discovers configuration by walking up from the project directory, so it applies to
   that example only. The order matters - see the warning below.
2. **Replace** the toolkit `ProjectReference` entries with `PackageReference` entries. Keeping both
   pulls the same assemblies in twice, and the `ProjectReference` wins, so the package is never
   actually exercised.
3. Revert both changes when finished. The copied file contains an absolute, machine-specific path and
   must not be committed.

> [!WARNING]
> **Add the `NuGet.config` before the package references, or restore explicitly afterwards.**
>
> A `PackageReference` to `1.0.0-dev` means *at least* `1.0.0-dev`. Prerelease labels are compared
> alphabetically, and `dev` sorts before `preview`, so `1.0.0-preview.1` satisfies the constraint. If
> the local feed is not configured yet, NuGet does not fail - it quietly resolves an **older
> published preview** from nuget.org instead.
>
> Worse, adding the `NuGet.config` afterwards does not fix it on its own. The wrong resolution is
> already recorded in `obj/project.assets.json`, and a plain `dotnet build` keeps using it, so the
> build continues to fail against the wrong assemblies. Force a restore to recover:
>
> ```bash
> dotnet restore examples/code-only/<Example>/<Example>.csproj
> ```
>
> Deleting the example's `obj` folder has the same effect. This is also why the base package matters:
> the mapping in the generated config includes both `Stride.CommunityToolkit` and
> `Stride.CommunityToolkit.*`, because the `.*` pattern alone does not match the base package name and
> would leave it resolving from nuget.org.

## Running the examples

Code-only examples are GUI applications. Run one directly:

```bash
dotnet run --project examples/code-only/Example01_Basic3DScene/Example01_Basic3DScene.csproj
```

`Example01_Basic3DScene_FileBasedApp` is a [file-based app](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps):
a single `.cs` file with no project file, which declares its dependencies inline with `#:package`
and `#:project` directives. It has no `.csproj`, so it is not part of the solution and Visual Studio
will not build it alongside the other projects. Run it from the command line instead:

```bash
dotnet run --file examples/code-only/Example01_Basic3DScene_FileBasedApp/Program.cs
```

## Debugging an example

Examples run until their window is closed, so a plain `dotnet run` cannot be waited on and read back.
Build first, then launch the executable with redirected output, wait, terminate, and read the log:

```powershell
$out = "$env:TEMP\example-run.txt"
dotnet build examples\code-only\Example02_GiveMeACube\Example02_GiveMeACube.csproj -v q --nologo
$exe = "examples\code-only\Example02_GiveMeACube\bin\Debug\net10.0\Example02_GiveMeACube.exe"
$process = Start-Process $exe -PassThru -RedirectStandardOutput $out -WorkingDirectory (Split-Path $exe)
Start-Sleep -Seconds 12
if (-not $process.HasExited) { Stop-Process -Id $process.Id -Force }
Get-Content $out | Select-String "DIAG"
```

### Where diagnostics actually appear

This catches people out, because the wrong choice produces no output at all rather than an error:

| Location | Use |
|---|---|
| Top-level statements, `game.Run(start:/update:)` callbacks | `Console.WriteLine` reaches the redirected stream |
| Inside a `SyncScript` / `AsyncScript` / `StartupScript` | `Console.WriteLine` does **not** reach it - use `Log.Info`, `Log.Warning` |
| Inside a render feature or game system | `GlobalLogger.GetLogger("Name")` |

Stride writes each line to both the console and the redirected stream, so captured output shows
everything twice. Expect the duplicates, or pipe through `Select-Object -Unique`.

### Keeping per-frame logging readable

Gate on a frame counter, but always include the first few frames:

```csharp
_frames++;
if (_frames > 3 && _frames % 120 != 0) return;

Log.Warning($"DIAG position={Entity.Transform.Position}");
```

Gating on `% N` alone can produce no output at all when the run is short or the frame rate is low,
which is easily misread as "the code never ran". Prefixing lines with a token such as `DIAG` makes
them easy to separate from Stride's own logging.

### Build warnings are a debugging tool

Real defects hide in the warning list. A Stride 4.4 regression that silently broke the ImGui.NET
integration was found only through a single `warning CS9193` among 66 warnings. Filter with
`Select-String ": error|warning CS"`; filtering by project path also matches unrelated `NU1903`
NuGet advisories.

> [!TIP]
> Reach out to maintainers anytime, process improvements, clarifications, or code reviews, we're
> happy to help!
