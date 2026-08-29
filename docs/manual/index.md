# Stride Community Toolkit Manual

[!INCLUDE [global-note](../includes/global-note.md)]

The toolkit is a set of extensions and helpers for [Stride](https://www.stride3d.net/), an open-source C# game engine. Its largest piece is the **code-only** approach: building a Stride game from a plain C# project, with no editor and no asset pipeline.

Nothing here is required to use Stride. Everything is an extension method or a small component you can read, copy and change.

## Start here

- **New to the toolkit?** [Getting Started](getting-started.md) gets the package installed and a window on screen.
- **New to Stride itself?** [Components and Scripts](components-and-scripts.md) explains the entity-component model the rest of the manual assumes.
- **Want to see it running first?** The [examples gallery](code-only/examples/index.md) is every code-only example with a screenshot. Each one is a complete program you can copy and run.

## What is in here

**[Code-Only](code-only/index.md)** - why you might work without the editor, how to [create a project](code-only/create-project.md), and the [curated extension list](code-only/extensions.md) for doing it.

**Extensions** - the full reference, one page per type being extended: [Game](game-extensions/index.md), [Entity](entity-extensions/index.md), [Camera](camera-extensions/index.md), [Model](model-extensions/index.md), [Animation](animation-extensions/index.md), [Script](script-extensions/index.md) and [Script System](script-system-extensions/index.md).

**[Physics](physics-extensions/index.md)** - the Bepu and Bullet helpers, plus two long-form pages on the failures that produce no error message at all: [who owns the transform](physics-extensions/bepu-transform-ownership.md) and [why a constraint does nothing](physics-extensions/bepu-constraints.md). Both are worth reading before you spend an afternoon on either problem.

**Rendering** - drawing things the engine has no built-in component for: [Entity Text](rendering/entity-text.md) and [World Text](rendering/world-text.md) for labels, [Debug Shapes](rendering/debug-shapes.md) for immediate-mode wireframes, [MeshBuilder](rendering/mesh-builder.md) and [TextureCanvas](rendering/texture-canvas.md) for building meshes and textures at runtime.

**[Troubleshooting](troubleshooting.md)** - the errors people actually hit.

For the generated API reference, see the [API](../api/index.md) section.

## The toolkit is in preview

Names and signatures still change between releases. If something in this manual does not match the package you have installed, the [API reference](../api/index.md) is generated from the source and is the authority. Corrections and additions are welcome - see [Contributing](../contributing/index.md).
