# Example Metadata Schema

Every example carries one `---example-metadata` block in its entry file, and everything downstream is generated from it: `examples-manifest.json`, both launchers, the documentation page, the level landing pages, the table of contents and the screenshot run.

This page is the field reference. For the shorter "how do I add an example" walkthrough, see [Contribute Examples](index.md); for how the generator itself works, see the [Metadata Generator README](https://github.com/stride3d/stride-community-toolkit/blob/main/tools/Stride.CommunityToolkit.Examples.MetadataGenerator/README.md); and for why the schema looks like this, see [Design Decisions](decisions.md).

> [!NOTE]
> This is **schema v1**. The manifest carries `schemaVersion`, and `ManifestLoader` refuses a manifest
> newer than it understands. Changing the meaning of a field means bumping that number and updating
> both launchers - it is a contract, not an internal detail.

## The block

```yaml
---example-metadata
# --- identity -------------------------------------------------------------
slug: mesh-outline                 # REQUIRED. Short, shareable, kebab-case. Doc filename + URL.
title:
  en: Mesh Outline Render Feature  # REQUIRED.
  cs: Obrysy modelů pomocí vlastní render feature

# --- classification -------------------------------------------------------
level: Advanced                    # REQUIRED. See "Levels" below.
category: Rendering                # REQUIRED. See "Categories" below.
complexity: 4                      # 1-5
order: 130                         # Position within the (language, level) group

# --- content --------------------------------------------------------------
description:
  en: |-                           # `|-`, not `|` - see "Block scalars" below
    What the example shows, and why it is worth reading.
concepts:                          # Becomes the "The Program.cs file shows how to:" bullets
  - Writing a custom RootRenderFeature
tags: [3D, Rendering, Shader]
related:                           # Project names; the generator resolves them to slugs
  - Example13_RootRendererShader

# --- docs generation ------------------------------------------------------
docs: true
media: stride-game-engine-example-13-mesh-outline.webp   # Optional. Defaults to <slug>.webp
tocName: Mesh Outline              # Optional. Falls back to title.en
screenshot: true
screenshotFrame: 240

# --- launchers ------------------------------------------------------------
launcher: true

# --- lifecycle ------------------------------------------------------------
enabled: true
created: 2025-08-07
---
```

F# uses `(* ... *)`, and Visual Basic prefixes every line with `'`. Both are picked up automatically.

Fields are written in that order - identity, classification, content, docs, launchers, lifecycle - so that any block can be read, or copied, without hunting.

## Fields

### Identity

| Field | Required | Notes |
|---|---|---|
| `slug` | ✔ | Kebab-case, globally unique, and **without a level prefix**. It becomes a public URL, so it must survive reclassification - see [D24](decisions.md#d24---why-slugs-stay-level-free). |
| `title.en` | ✔ | The page H1, the launcher entry and the toc fallback. |
| `title.cs` | | Optional. Launchers only. |

### Classification

| Field | Required | Notes |
|---|---|---|
| `level` | ✔ | Matched **case- and spelling-exactly**. `Beginners` is an error, not a synonym. |
| `category` | ✔ | Closed set, below. |
| `complexity` | | 1-5. Outside that range is an error. |
| `order` | | Sort position within the `(language, level)` group. Duplicates are a warning, not an error: the tie breaks on `slug`, so the sequence stays stable. |
| `language` | | **Omit it.** The parser fills it in from the file extension, and the validator errors on a declared value that disagrees. |

### Content

| Field | Required | Notes |
|---|---|---|
| `description.en` | | Rendered as the page intro and the gallery card. |
| `concepts` | | The "The `Program.cs` file shows how to:" bullet list. |
| `tags` | | Free-form topics. **Never repeat the `level` here** - it is a field, not a tag, and the validator rejects it. |
| `related` | | Project folder names, not slugs. A name matching no folder is an **error** - it is a typo. A folder that exists but carries no metadata block yet is a **warning**. A folder that is `enabled: false` is neither: the link is dropped deliberately and comes back when the example does. |

### Documentation

| Field | Default | Notes |
|---|---|---|
| `docs` | `true` | `false` keeps the example in the launchers with no page generated. |
| `media` | `<slug>.webp` | Filename only, resolved against `docs/manual/code-only/examples/media/`. An explicit duplicate is an **error** - two pages sharing one file means the second capture silently destroyed the first. |
| `tocName` | `title.en` | For a title too long to sit in the sidebar. |
| `screenshot` | `true` | `false` excludes the example from the capture run. |
| `screenshotFrame` | `240` | A frame index, not a delay. |

### Launchers and lifecycle

| Field | Default | Notes |
|---|---|---|
| `launcher` | `true` | `false` documents the example but hides it from both launchers. |
| `enabled` | `true` | `false` excludes it from the manifest **entirely** - no page, no launcher entry, invisible to everything. Use it while an example does not build. |
| `created` | | `yyyy-MM-dd`. Taken from the project's first commit, not invented. |

### Populated by the generator

`projectName`, `projectPath` and `relatedSlugs` appear in the manifest but are never written by hand. Writing one is an unknown-key error.

## Levels

`Getting Started` · `Beginner` · `Intermediate` · `Advanced` · `Other`

Level means **conceptual prerequisites, not line count**. A 589-line example that repeats one idea is easier than a 55-line one that requires understanding the render pipeline.

| Level | Admits |
|---|---|
| **Getting Started** | The minimum viable Stride app. The reader has never run code-only Stride. Boilerplate plus one helper call. |
| **Beginner** | One new concept on top of the base scene. Toolkit helpers only, no engine extension points. |
| **Intermediate** | A Stride subsystem used directly - UI, particles, constraints, collision filtering, custom scripts - or several concepts combined. |
| **Advanced** | Custom engine extension points (render features, shaders, custom renderers), third-party engine integration, or multi-project architecture. |
| **Other** | Playgrounds and demos that are not teaching a specific lesson. Sorts last. |

**Getting Started admits exactly the "your first code-only app" examples** - the 3D one, the 2D one, the file-based one, and the F#/VB ports. Anything that builds *on top of* the base scene is Beginner, however short it is. That rule decides most borderline cases on its own.

`Other` means *unclassified but published*. It is not a substitute for `enabled: false` (does not build) or `docs: false` (deliberately undocumented).

## Categories

`Shapes` · `Geometry` · `Physics` · `Rendering` · `Performance` · `Text` · `UI` · `Input` · `Scripts` · `Networking` · `Debug` · `Game`

**A category names the lesson, not the scenery.** A keyboard-menu example that happens to spawn shapes is `Input`. An instancing example is `Performance` whether it draws cubes or physics bodies.

Both vocabularies live in [`Core/MetadataVocabulary.cs`](https://github.com/stride3d/stride-community-toolkit/blob/main/tools/Stride.CommunityToolkit.Examples.MetadataGenerator/Core/MetadataVocabulary.cs) and nowhere else. Adding a value is a one-line change there; it is deliberately not a free-text field, because a silently accepted variant produces a second landing page nobody notices.

## Ordering

`order` is scoped **per `(language, level)` group**, so numbers only ever compete with the handful of examples beside them. Use a small ascending sequence with gaps of 10, leaving room to insert without renumbering.

Do not carry a global scheme forward. Three incompatible scales existed before the schema landed - `1`-`57`, `210`/`220`/`230`, and `32000` - and they were renumbered wholesale rather than reconciled.

## Block scalars

`description` uses `|-`, not `|`.

YAML's plain `|` keeps one newline at the end of a block scalar, which reaches the manifest as a literal `\n` at the tail of the JSON string and has to be trimmed by every consumer. `|-` strips it. Newlines *inside* the block are intentional and survive either way - this is only about the last one.

## Two YAML traps

Both are silent, and both cost an afternoon the first time:

- **An unquoted `#` starts a comment and truncates its value.** `- Declaring NuGet packages inline with #:package` becomes `- Declaring NuGet packages inline with`. No error, no warning.
- **An unquoted `: ` inside a list item turns the item into a mapping**, and parsing aborts deep inside YamlDotNet with a message that names neither the line nor the cause.

Quote any value containing either. The validator checks the raw source text for both, because neither is visible once the block has been parsed.

## Page ownership

Generated pages carry a `generated:` marker in their own frontmatter, and the generator honours it:

| `generated:` | Meaning |
|---|---|
| `true` | The whole file is tool-owned and overwritten on every run. Edit the metadata block, not the page. |
| `partial` | **Only the delimited region** is overwritten. Everything outside it is preserved verbatim. |
| `false` *or absent* | Hand-owned. Never touched; the generator only warns if the metadata has drifted. |

`partial` uses explicit markers rather than an inferred boundary:

```markdown
---
generated: partial
slug: myra-ui-draggable-window-and-services
---

<!-- #region generated -->
(intro, concepts, media, GitHub link, code include - all tool-owned)
<!-- #endregion generated -->

## MyraSceneRenderer.cs

Hand-written prose. Never touched by the generator.
```

Hand-written content may sit **before or after** the region - the author chooses, so the tool never has to guess where merged content belongs. A `partial` page with no markers is skipped with a warning rather than overwritten. The HTML comments are invisible in the rendered output.

## Checking your block

```bash
dotnet run --project tools/Stride.CommunityToolkit.Examples.MetadataGenerator -- scan examples/code-only
```

Findings are aggregated - one run reports every problem across every example rather than stopping at the first. Errors are fatal under `--strict`, which the launcher pre-build hook passes, so a malformed block fails the build rather than reaching the manifest.
