# Contribute Examples

All examples live in the [examples](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only) folder.

Suggested and in-progress examples are tracked in the [example backlog](https://github.com/stride3d/stride-community-toolkit/blob/main/notes/example-backlog.md). Check it before you start, and add your idea there if it is not listed.

## How an example is registered

One metadata block, in the example's own entry file. There is nothing else to update: the console runner, the [Avalonia launcher](https://github.com/stride3d/stride-community-toolkit/tree/main/tools/Stride.CommunityToolkit.Examples.Launcher), the documentation page, the level landing pages and the table of contents are all generated from it.

1. Create a project under `examples/code-only/` named `ExampleXY_YourExampleName` (replace `XY` with the next available number).

2. Add an `---example-metadata` block to the bottom of the entry file, inside a comment:

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
      - Example01_Basic3DScene
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

5. Generate its documentation page:

    ```bash
    dotnet run --project tools/Stride.CommunityToolkit.Examples.MetadataGenerator -- docs examples/code-only
    ```

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

Screen capture - `gdigrab`, `PrintWindow`, Windows.Graphics.Capture - was tried and rejected: a fixed delay photographs a different moment every run, the window has to stay unobstructed for the whole run, and GDI capture of a Direct3D swapchain famously returns a black rectangle. See [D20-D23](decisions.md#d20-d23---why-capture-is-in-engine-not-off-the-screen).

`--review` writes every image to `screenshots-review/` at the repository root, together with an `index.html` contact sheet for looking at all of them in one pass. Run the command without `--review` to write them into the documentation media folder for real; an image already there is never replaced without `--force`.

Add `--only <slug>` to redo a single example, and `--frame <n>` to try a different moment without editing the metadata first.

### Edited the metadata? Rebuild the manifest

The capture script does not read the metadata blocks. It reads `examples-manifest.json`, which is generated *from* them - so a `screenshot` or `screenshotFrame` you have just edited has no effect until the manifest is rebuilt:

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
- [Design Decisions](decisions.md) - why the pipeline works the way it does, and which parts are deliberate rather than accidental.
- [Metadata Generator README](https://github.com/stride3d/stride-community-toolkit/blob/main/tools/Stride.CommunityToolkit.Examples.MetadataGenerator/README.md) - the tool's own commands, validation and architecture.
