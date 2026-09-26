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

Two shared files under `build/` keep the example, test and tool builds small. Without them, **each**
Stride project copies roughly 476 MB into `bin`, most of which is unreachable from a desktop build:
about 239 MB of Android native runtimes, a further ~38 MB of iOS/tvOS/macOS, and 54 MB of XML
documentation from referenced packages. The Avalonia launcher was worse still, at 561 MB per
configuration: Skia and HarfBuzz for 25 platforms, and 100 MB of native symbol files even for the
one platform it runs on.

| File | What it does |
|---|---|
| `build/HostRuntime.props` | Restricts the build to the host runtime identifier, so only the current platform's native runtimes are copied |
| `build/HostRuntime.targets` | Removes package XML documentation and native symbol files from the output |

The `Directory.Build.props` and `.targets` under `examples/`, `tests/` and `tools/` import them; the
library projects under `src/` do not, because a package has to stay runtime-neutral. Together these
take an example from ~476 MB to ~80 MB, the test project from 495 MB to 89 MB and the launcher from
561 MB to 28 MB, and a clean build of the whole solution to well under a minute.

Two details worth knowing before editing them:

1. **Every nested `Directory.Build.props` and `.targets` explicitly imports the repository-root
   equivalent.** MSBuild imports only the *nearest* one, so without that import the settings defined
   at the repository root, such as `TargetFramework` and `StrideVersion`, would be silently lost.
2. **The runtime identifier is derived from the host OS**, not hard-coded to Windows, so Linux and
   macOS builds keep working. `NETCoreSdkPortableRuntimeIdentifier` would be the obvious source for
   this but the SDK sets it *after* `Directory.Build.props` is evaluated, so explicit
   `IsOSPlatform` checks are used instead.

> [!NOTE]
> `AppendRuntimeIdentifierToOutputPath` is disabled deliberately, so output stays at
> `bin/<Configuration>/net10.0/` rather than gaining a `win-x64/` segment. This keeps documented
> paths and launcher commands valid. With a runtime identifier set, native libraries land flat
> beside the executable rather than under `runtimes/<rid>/`.

## Cleaning build output

Even at the smaller sizes above, a full tree of `bin` and `obj` folders runs to several gigabytes,
and a stale `obj/stride` can keep shaders compiled from a previous Stride package (see the
release notes for the asset-bundle trap). Two scripts at the repository root remove them:

| Script | Removes |
|---|---|
| `delete-bin.bat` | Every `bin` and `obj` folder in the repository, or only under the folders given as arguments: `delete-bin.bat examples tests` |
| `delete-bin-examples.bat` | The same under `examples/` only, snippets included; library, test and tool outputs stay |

Each folder removed is printed. One that a running process holds open, such as Visual Studio with
the project loaded, is reported as locked and skipped. Nothing git tracks is named `bin` or `obj`,
so the scripts never touch source. They are deliberately not `git clean -xdf`, which would also
delete every other ignored file, local settings included.

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

This packs every publishable library into `bin/packages` as version `99.0.0-dev`. The version is
fixed by design, so a reference such as `Stride.CommunityToolkit.Bepu@99.0.0-dev` keeps working
across rebuilds and never has to be edited.

The `99` is deliberate. A `PackageReference` means *at least* that version, and NuGet then picks the
**lowest** version satisfying it. Because prerelease labels compare alphabetically, a `1.0.0-dev`
constraint was satisfied by every published `1.0.0-preview.*` - so whenever the local feed was not
consulted, NuGet quietly chose the *oldest* of them and the failure surfaced much later as a missing
Stride 4.2 assembly. Nothing published can satisfy `99.0.0`, so that mistake now fails immediately
with `NU1101`/`NU1102` instead.

> [!IMPORTANT]
> NuGet keys its global cache by package id and version, and will not re-extract a rebuilt package
> that carries a version it has already seen. The script therefore deletes the matching folders under
> `~/.nuget/packages` before packing, so a freshly built package always wins. This mirrors what
> Stride does in `build/install-gamestudio.targets`.

### Consuming the local packages

The script also writes a ready-to-use `bin/packages/NuGet.config`. Copy it next to the project that
should consume the packages, then reference them normally:

```xml
<PackageReference Include="Stride.CommunityToolkit.Bepu" Version="99.0.0-dev" />
```

Nothing machine-wide needs changing. NuGet merges that configuration with any existing one, so
nuget.org and the Stride dev feed keep resolving exactly as before.

> [!WARNING]
> The `packageSourceMapping` entry in that file is required, not optional. Once **any** NuGet
> configuration on the machine defines `packageSourceMapping`, and the Stride development setup does,
> a source that is not mapped is silently never consulted - so the local feed is skipped entirely and
> the restore fails as if the packages did not exist.

The file maps two patterns, and both are needed:

```xml
<package pattern="Stride.CommunityToolkit" />
<package pattern="Stride.CommunityToolkit.*" />
```

`Stride.CommunityToolkit.*` requires a dot after the prefix, so it matches
`Stride.CommunityToolkit.Bepu` and friends but **not** the base `Stride.CommunityToolkit` package,
which needs the exact-name pattern of its own. Without it the base package falls through to the
nuget.org catch-all, where no `99.0.0` exists, and the restore fails.

Both are more specific than the `Stride.*` mapped to the Stride dev feed. NuGet resolves by longest
matching prefix, so the toolkit packages come from the local feed without disturbing how Stride
packages resolve.

### Testing the packages on Linux from WSL

The same feed serves a project built inside WSL, since WSL mounts the Windows drives under `/mnt`.
On Windows the pack script also writes `bin/packages/NuGet.wsl.config`: the feed by its `/mnt` path,
plus nuget.org and the Stride dev feed, because a Linux NuGet has no machine-wide configuration
mapping them and a source that is not mapped is never consulted. Copy it next to the project under
WSL as `NuGet.config`, and reference the exact versions the feeds hold: the toolkit at `99.0.0-dev`,
Stride at the version in the dev feed.

Four things that cost a day each the first time:

1. **A same-version repack is invisible to NuGet under WSL too.** After every pack, delete the
   extractions and the resolver's cache in the distribution, then restore again:
   `rm -rf ~/.nuget/packages/stride.communitytoolkit*/99.0.0-dev ~/.nuget/packages/stride.*/<version> /tmp/StrideNugetResolver-*`.
   To make the asset steps rerun as well, delete the game's `obj/stride`, the `*.Linux/obj/Debug/stride`
   folder and `Bin/Linux/Debug/data`.
2. **A self-built Stride dev feed must include Vulkan.** The engine's `build/Stride.Local.props`
   defaults `StrideGraphicsApis` to Direct3D11 only, and a Direct3D11-only `Stride.Graphics` on
   Linux fails inside the skybox compile with a null reference in `Silk.NET.DXGI.GetApi`. Pack the
   engine with `StrideGraphicsApiDependentBuildAll=true`, which is what its CI passes.
3. **Use Microsoft's .NET build, not the distribution's package.** Ubuntu's apt `dotnet` reports a
   distribution-specific runtime identifier and ships only the 1xx SDK band, whose Roslyn is older
   than the one Stride's analyzers target, so the engine's source generators are silently skipped
   (`CS9057`) and serializer registration never happens. Install the matching band with
   `dotnet-install.sh --channel 10.0.4xx`.
4. **The asset compiler under WSL needs `libgomp1`** (the texture compressor links it) and, on a
   build where the SDK has switched NuGet signature verification on for its child processes,
   `DOTNET_NUGET_SIGNATURE_VERIFICATION=false dotnet build` until the compiler learns the SDK's
   trust store. Rerun with `-tl:off` to see the errors the terminal logger hides.

A game that shows no window but burns several cores is WSLg's RDP client not running; `wsl --shutdown`
and reopen. Rendering goes through Mesa's software Vulkan, so it is CPU-bound, and SDL there is X11
only.

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
> If the local feed is not configured yet, the restore fails with `NU1101`/`NU1102`: nothing
> published satisfies `99.0.0`, which is the whole reason that version was chosen.
>
> Adding the `NuGet.config` afterwards does not fix it on its own, though. The failed resolution is
> already recorded in `obj/project.assets.json`, and a plain `dotnet build` keeps using it. Force a
> restore to recover:
>
> ```bash
> dotnet restore examples/code-only/<Example>/<Example>.csproj
> ```
>
> Deleting the example's `obj` folder has the same effect. This is also why the base package matters:
> the mapping in the generated config includes both `Stride.CommunityToolkit` and
> `Stride.CommunityToolkit.*`, because the `.*` pattern alone does not match the base package name and
> would leave it looking on nuget.org, where no `99.0.0` exists.

## Running the examples

Code-only examples are GUI applications. Run one directly:

```bash
dotnet run --project examples/code-only/E01_3D_BasicScene/E01_3D_BasicScene.csproj
```

`E01_3D_BasicScene_FileBasedApp` is a [file-based app](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps):
a single `.cs` file with no project file, which declares its dependencies inline with `#:package`
and `#:project` directives. It has no `.csproj`, so it is not part of the solution and Visual Studio
will not build it alongside the other projects. Run it from the command line instead:

```bash
dotnet run --file examples/code-only/E01_3D_BasicScene_FileBasedApp/Program.cs
```

## Debugging an example

Examples run until their window is closed, so a plain `dotnet run` cannot be waited on and read back.
Build first, then launch the executable with redirected output, wait, terminate, and read the log:

```powershell
$out = "$env:TEMP\example-run.txt"
dotnet build examples\code-only\E02_3D_GiveMeACube\E02_3D_GiveMeACube.csproj -v q --nologo
$exe = "examples\code-only\E02_3D_GiveMeACube\bin\Debug\net10.0\E02_3D_GiveMeACube.exe"
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