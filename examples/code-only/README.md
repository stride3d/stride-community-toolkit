# Code-Only Examples

Examples for the Stride Community Toolkit, each a complete app built without the Game Studio editor.

Every example is described by one `---example-metadata` block at the bottom of its entry file, and
everything downstream - both launchers, the documentation page, the level landing pages, the table of
contents and the screenshot run - is generated from it. There is no registry to update.

## Finding your way around

Project names follow `E{NN}_[{Dimension}_]{Subject}[_{Qualifier}...]`, so the folder listing groups
itself:

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

`E05_3D_Constraints_Motors` reads as: physics shelf, 3D, constraints, the motors variation.

**The number is a shelf label, nothing more.** No tool reads it. What a reader actually filters by -
`level`, `category`, `tags` - lives in the metadata block, and the launchers and documentation are
built from that. So a shelf can be renamed or split without touching a single example.

Three folders sit outside the scheme on purpose:

- `Example_2D_Playground`, `Example_Bepu_Playground` - scratch space for experimenting. They carry no
  metadata block at all and are not examples; the naming says so at a glance.
- `Example.Common` - a shared library, not an example.

## Running an example

```bash
dotnet run --project examples/code-only/E01_3D_BasicScene
```

Or pick it from the Stride Community Toolkit Examples Launcher.

> [!TIP]
> After a fresh clone or a large rename, build once with `dotnet build -m:1`. Stride's asset compiler
> contends on its asset database when many example projects compile assets in parallel, and it fails
> differently on each run.

## Adding an example

The naming rules, the metadata block, the level rubric, the category vocabulary and the screenshot
workflow are all in **[Contribute Examples](../../docs/contributing/examples/index.md)**. They are
documented once, there, rather than restated here - the levels and categories are a closed set
validated in code, and a second copy of them only drifts.

Before starting, look through the existing examples and the open issues: the idea may already be
built, in progress, or declined for a reason worth knowing.

In short:

1. Pick the shelf, then name the project `E{NN}_[{Dimension}_]{Subject}`.
2. Add the metadata block to the bottom of the entry file.
3. Validate it:

   ```bash
   dotnet run --project tools/Stride.CommunityToolkit.Examples.MetadataGenerator -- scan examples/code-only
   ```

4. Generate its documentation page.

Keep each example focused on the concept it teaches, comment for a reader who has not seen Stride
before, and run it before submitting.

## Further reading

- [Contribute Examples](../../docs/contributing/examples/index.md) - the walkthrough.
- [Example Metadata Schema](../../docs/contributing/examples/metadata-schema.md) - every field, the
  level rubric and the category vocabulary.