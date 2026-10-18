# Contribute Examples

All examples live in the [examples](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only) folder.

Before you start, look through the existing examples and the open issues: the idea may already be built, in progress, or declined for a reason worth knowing. If it is new, open an issue so it can be discussed before the work.

## Naming the project

```
E{NN}_[{Dimension}_]{Subject}[_{Qualifier}...]
```

`E05_3D_Constraints_Motors`, `E01_2D_BasicScene_Bullet`, `E12_Audio_Spatial`, `E04_ImGuiNet`.

**The number is a shelf, not a classification.** It groups related examples so the folder listing and
Visual Studio's Solution Explorer stay navigable. Nothing in the build derives ordering or meaning
from it - `level`, `category` and `tags` in the metadata block are what the launchers, the table of
contents and the landing pages actually read. Pick the shelf an example belongs on and take the next
free name on it; there is no "next available number" to claim.

| # | Shelf | | # | Shelf |
|---|---|---|---|---|
| 01 | Getting started - the base scene, every language and host | | 09 | Rendering and shaders |
| 02 | First concepts on top of the base scene | | 10 | Performance |
| 03 | Text and on-screen composition | | 11 | Toolkit rendering packages - ShapeBatch, charts |
| 04 | UI and input | | 12 | Audio |
| 05 | Physics (Bepu) | | 13 | Networking |
| 06 | Physics - other engines | | 14-15 | *reserved* - Input, Math |
| 07 | Geometry and procedural meshes | | 20-29 | Games and minigames |
| 08 | Debug and diagnostics | | | |

The rules, in the order they usually come up:

1. **Underscore separates fields; it never appears inside one.** Multi-word subjects and qualifiers
   are PascalCase - `E08_DpiAware`, not `E08_Dpi_Aware`.
2. **The dimension token marks the lesson, not the scenery.** A physics pile that only makes sense in
   2D gets `2D`; a UI window that happens to sit in front of a 3D scene gets nothing. This is the same
   test as the one categories use.
3. **Bepu is the default and is never named.** `E05_3D_FirstPersonCharacter`, not `..._BepuCharacter`.
4. **An engine name is the *subject* when the example exists to show that engine, and a trailing
   *variant* when it is a port of something that also exists on the default engine.** So
   `E06_Box2D_Junkyard`, but `E10_2D_StressPile_Box2D` and `E01_3D_BasicScene_Bullet`. A variant is
   the one qualifier that must come last, which is what keeps a port sorted next to its original.
5. **Drop the word "Physics".** `Bullet`, `Box2D` and `Jitter2` already say it.
6. **A category word belongs in the name only when it aids comprehension.** `Audio` stays, because
   "Spatial" and "WavFile" do not announce themselves as audio. `Physics`, `Rendering`, `UI` and
   `Performance` do not - the subject carries them.
7. **Keep the load-bearing word, drop the decorative one.** Shorter is the preference, but not at the
   cost of the lesson: `E04_StrideUI_ButtonHoverAnimation` keeps `Animation` because the animation
   *is* the example, while `E12_Audio_Procedural` needs no `Sound`.

Canonical spellings, so this is not re-litigated per example: `2D` / `3D`; `UI` and `VB` uppercase;
`HUD` uppercase as the one three-letter exception; `Dpi` and `Wav` in Pascal; products as their own
documentation writes them (`ImGui`, `ImGuiNet`, `SignalR`, `Blazor`, `Myra`, `StrideUI`, `Box2D`,
`Jitter2`, `Bepu`, `Bullet`); `FSharp` and `VisualBasic` spelled out; API types as declared
(`ShapeBatch`, `TextureCanvas`, `SyncScript`, `SimulationUpdate`).

The project name is a filesystem convenience and may be renamed. The `slug` is the permanent
identifier: it becomes a public URL, so it carries no level or category, which change as the toolkit
matures and would break external links when they did.

## How an example is registered

One metadata block, in the example's own entry file. There is nothing else to update: the console runner, the [Avalonia launcher](https://github.com/stride3d/stride-community-toolkit/tree/main/tools/Stride.CommunityToolkit.Examples.Launcher), the documentation page, the level landing pages and the table of contents are all generated from it.

1. Create a project under `examples/code-only/`, named to the convention above - pick the shelf it belongs on, then `E{NN}_[{Dimension}_]{Subject}`.

2. Add an `---example-metadata` block to the bottom of the entry file, inside a comment. Keep it
   last: the generated page includes the code above the block by line range, and anything after the
   block puts the whole file on the page, block included, with a validator warning.

    ```csharp
    /*
    ---example-metadata
    slug: my-example                 # REQUIRED. Short, kebab-case. Becomes the doc filename and URL.
    title:
      en: My Example
    level: Beginner                  # Getting Started | Beginner | Intermediate | Advanced | Other
    category: Physics                # See Core/MetadataVocabulary.cs for the full list
    complexity: 2                    # 1-5
    order: 40                        # Position within its (language, level) group
    description:
      en: |-
        What the example shows, and why it is worth reading. A few sentences.
    concepts:
      - One thing the reader will learn
      - "Quote any value containing a colon: like this one"
    tags:
      - 3D
      - Physics
    related:
      - E01_3D_BasicScene
    enabled: true
    created: 2026-08-23
    ---
    */
    ```

    F# uses `(* ... *)` and Visual Basic prefixes every line with `'`. Both are picked up automatically.

3. Check it:

    ```bash
    dotnet run --project tools/Stride.CommunityToolkit.Examples.MetadataGenerator -- scan examples/code-only
    ```

    Validation reports every problem in one pass. Two mistakes are worth knowing about in advance, because YAML makes both of them silently: an **unquoted `#`** truncates its value, and an **unquoted `: `** inside a list item turns the item into a mapping. Quote any value containing either.

4. Run the console app or the launcher. Your example appears in its level group, with no further registration.

   An example built on a toolkit package that is not on NuGet yet gets a note on its page, on its
   level page and on the index, saying it runs from a clone of the repository. The generator reads
   the packages from the project file; the list of unpublished packages is
   `DocPaths.PackagesNotOnNuGet` in the generator, kept in step with the publish list in
   `.github/workflows/dotnet-nuget.yml`.

5. Generate its documentation page:

    ```bash
    dotnet run --project tools/Stride.CommunityToolkit.Examples.MetadataGenerator -- docs examples/code-only --docs-path docs/manual/code-only/examples --media-path docs/manual/code-only/examples/media
    ```

    The `--docs-path` and `--media-path` defaults are relative to the current directory and assume the tool's own folder, so pass them explicitly from the repository root, as `docs/run.bat` does.

## Three flags worth knowing

| Field | Default | Effect when `false` |
|---|---|---|
| `enabled` | `true` | Excluded everywhere - use while an example does not build |
| `docs` | `true` | In the launchers, but no documentation page |
| `launcher` | `true` | Documented, but hidden from both launchers |

## Screenshots

Example screenshots are produced by running the examples, not by taking them by hand:

```bash
dotnet run --file build/capture-screenshots.cs -- --review
```

Each example runs once with capture enabled, saves its **GPU render target** at a fixed frame and exits. Nothing is scraped off the screen, so there is no window to keep in the foreground and the run can happen behind whatever you are doing.

Screen capture - `gdigrab`, `PrintWindow`, Windows.Graphics.Capture - was tried and rejected: a fixed delay photographs a different moment every run, the window has to stay unobstructed for the whole run, and GDI capture of a Direct3D swapchain famously returns a black rectangle.

`--review` writes every image to `screenshots-review/` at the repository root, together with an `index.html` contact sheet for looking at all of them in one pass. Run the command without `--review` to write them into the documentation media folder for real; an image already there is never replaced without `--force`.

Add `--only <slug>` to redo a single example, and `--frame <n>` to try a different moment without editing the metadata first.

### Edited the metadata? Rebuild the manifest

The capture script does not read the metadata blocks. It reads `tools/Stride.CommunityToolkit.Examples.Launcher/examples-manifest.json`, which is generated *from* them - so a `screenshot` or `screenshotFrame` you have just edited has no effect until the manifest is rebuilt:

```bash
dotnet build tools/Stride.CommunityToolkit.Examples.Launcher
```

Building either launcher regenerates it, through `tools/ExamplesManifest.targets`. The script reports a manifest that is missing, but it cannot tell that one is stale - it will capture at the old frame and call it a success. This is what `--frame <n>` is for while you are still deciding: it overrides the manifest, so you can find the right moment first and write it into the metadata once.

Two metadata fields control capture:

| Field | Default | Effect |
|---|---|---|
| `screenshot` | `true` | `false` excludes the example - for anything that cannot produce a meaningful frame on its own, such as the SignalR pair, which needs a running server |
| `screenshotFrame` | `240` | Which frame to keep. Raise it for a scene that needs longer to settle, lower it for one that has already scattered |

Every image is looked at by a person before it is committed. A capture that renders black, catches a scene mid-explosion or frames nothing but sky looks like a complete success to the script.

### Gold images: catching a rendering change by its pixels

The same capture path doubles as a regression test for the renderers, against **scenes built for
the purpose** rather than the examples: `tests/Stride.CommunityToolkit.GoldScenes` holds one small
scene per feature area - 2D shapes, 3D shapes, text, DebugShapes, ImGui - with a fixed layout and
nothing random, changed only to pin something new. The examples stay free to evolve; a golden that
photographed one would be re-captured with every change and prove nothing about the shader. The
scenes are listed in `tests/gold/scenes.jsonc` and their goldens live beside it:

```bash
dotnet run --file build/gold-images.cs                                  # compare every scene
dotnet run --file build/gold-images.cs -- --only shapes-2d              # compare one
dotnet run --file build/gold-images.cs -- --only shapes-2d --update     # capture and make it the golden
dotnet run --file build/gold-images.cs -- --only shapes-2d --noise      # capture twice, report run-to-run drift
dotnet run --file build/gold-images.cs -- --gpu                         # the real GPU: quick to look at, never a golden
```

The comparison is Stride's own: the maximum channel difference of every pixel goes into a histogram (`1-2`, `3-5`, `6-15`, `16+`), and the default rule fails the image if **any** pixel differs by 3 or more. `tests/gold/thresholds.jsonc` can relax that per image, with a comment saying why; the scenes are built not to need it. Every run writes the new capture, a diff mask (red for 3 and above, yellow for 1 and 2) and a side-by-side contact sheet to `screenshots-review/gold/`, and prints the bounding box of the failing pixels so a failure can be placed without opening anything.

The goldens are captured on **WARP**, Direct3D's software renderer, which is what the harness uses unless told `--gpu`: it is the one renderer every machine shares, bit-identical run to run, and a real GPU lands within a dozen levels of it - close, but over the rule, so a GPU capture must never become a golden. What else makes a capture reproducible: a fixed timestep with exactly one update per draw, so frame N is the same simulated instant every run; the profiler hidden and auto-exposure pinned, both of which run on real-time clocks; and the display scale pinned to 100% by the scenes, so pixel widths and font sizes do not follow the desktop. Run `--noise` on a new scene first: it shows what the harness would see with no change made.

The workflow `.github/workflows/gold-images.yml` runs the same comparison on every pull request that touches `src`, the scenes, the goldens or the harness, and uploads `screenshots-review/gold` as an artifact, so a failure is looked at there. A change that is meant to alter a golden updates it in the same pull request with `--update`, and the reviewer sees the new image next to the code.

To pin a new feature, add a scene class to the project, a line to `scenes.jsonc`, capture the golden with `--update`, and check `--noise`. Physics scenes are not in the suite yet: Bepu and Box2D step on the CPU with SIMD, and a pile settled over hundreds of frames has not been shown to land on the same pixels across machines.

## Editing a generated page

A documentation page carrying `generated: true` is overwritten on every run - change the metadata block, not the page.

To add hand-written prose to a generated page, change its frontmatter to `generated: partial` and wrap the tool-owned part in markers:

```markdown
---
generated: partial
---

<!-- #region generated -->
(everything here is regenerated)
<!-- #endregion generated -->

## My own section

Never touched by the generator.
```

A page with no `generated:` frontmatter at all is yours entirely, and the generator leaves it alone. That is how the older, hand-written example pages are treated.

## Going deeper

- [Example Metadata Schema](metadata-schema.md) - every field, the level rubric, the category vocabulary and the page ownership rules.
- [Metadata Generator README](https://github.com/stride3d/stride-community-toolkit/blob/main/tools/Stride.CommunityToolkit.Examples.MetadataGenerator/README.md) - the tool's own commands, validation and architecture.