---
generated: true
_disableAffix: true
---

# Code-Only Examples

Every code-only example, with a screenshot of what it actually renders. Each one is a complete, self-contained program you can copy and run.

Prefer a list? Each level has its own page, linked from the table of contents.

> [!NOTE]
> Most examples run from a copy of their project with the toolkit packages from NuGet. `Stride.CommunityToolkit.Box2D`, `Stride.CommunityToolkit.Charts` and `Stride.CommunityToolkit.ImGuiNet` are not on NuGet yet,
> so the examples built on them run from a clone of the repository: [Box2D Explosion](box2d-explosion.md), [Box2D Joints](box2d-joints.md), [Box2D Car](box2d-car.md), [Box2D Character Mover](box2d-character-mover.md), [Charts 2D](charts-2d.md), [Charts 3D](charts-3d.md), [Basic 2D Scene (Stress Pile, Box2D)](stress-pile-2d-box2d.md), [Junkyard (Box2D)](junkyard-box2d.md), [Junkyard Playground (Box2D)](junkyard-playground-box2d.md), [Box2D.NET Physics](box2d-physics.md), [ImGui.NET Text Rendering](imgui-net.md).

## C# Getting Started

<div class="row g-4 mb-4">
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/capsule-with-rigid-body.webp" class="card-img-top" alt="Screenshot of the Basic3D Scene (Capsule) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="capsule-with-rigid-body.md">Basic3D Scene (Capsule)</a></h3>
                <p><span class="badge text-bg-secondary">Shapes</span></p>
                <p class="card-text">Create a minimal 3D scene using toolkit helpers, add a skybox, and place a single capsule primitive.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/basic-scene-engine-only.webp" class="card-img-top" alt="Screenshot of the Basic3D Scene (Engine Only) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="basic-scene-engine-only.md">Basic3D Scene (Engine Only)</a></h3>
                <p><span class="badge text-bg-secondary">Shapes</span></p>
                <p class="card-text">A ground, a cube and a camera written against Stride alone, with no toolkit package: the graphics compositor, the camera, the lights, the procedural...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/file-based-app.webp" class="card-img-top" alt="Screenshot of the Basic3D Scene (Capsule) - File-Based App example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="file-based-app.md">Basic3D Scene (Capsule) - File-Based App</a></h3>
                <p><span class="badge text-bg-secondary">Shapes</span></p>
                <p class="card-text">The same minimal 3D scene as E01_3D_BasicScene, written as a .NET 10 file-based app: a single C# file with no .csproj.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/basic-2d-scene.webp" class="card-img-top" alt="Screenshot of the Basic2D Scene (Capsule) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="basic-2d-scene.md">Basic2D Scene (Capsule)</a></h3>
                <p><span class="badge text-bg-secondary">Shapes</span></p>
                <p class="card-text">Create a minimal 2D scene using toolkit helpers and place a single capsule primitive with a flat material.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/basic-2d-scene-bullet.webp" class="card-img-top" alt="Screenshot of the Basic2D Scene (Capsule) - Bullet Physics example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="basic-2d-scene-bullet.md">Basic2D Scene (Capsule) - Bullet Physics</a></h3>
                <p><span class="badge text-bg-secondary">Shapes</span></p>
                <p class="card-text">Create a minimal 2D scene using toolkit helpers and place a single capsule primitive.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/capsule-with-rigid-body-bullet.webp" class="card-img-top" alt="Screenshot of the Basic3D Scene (Capsule) - Bullet Physics example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="capsule-with-rigid-body-bullet.md">Basic3D Scene (Capsule) - Bullet Physics</a></h3>
                <p><span class="badge text-bg-secondary">Shapes</span></p>
                <p class="card-text">The same first scene as E01_3D_BasicScene, running on the legacy Bullet physics engine instead of Bepu.</p>
            </div>
        </div>
    </div>
</div>

## C# Beginner

<div class="row g-4 mb-4">
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/primitives-3d.webp" class="card-img-top" alt="Screenshot of the Basic3D Scene (Every Primitive) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="primitives-3d.md">Basic3D Scene (Every Primitive)</a></h3>
                <p><span class="badge text-bg-secondary">Shapes</span></p>
                <p class="card-text">Every 3D primitive the toolkit can build - cube, cone, capsule, sphere, cylinder, teapot, torus and triangular prism - dropped into one scene so the...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/gum-stride-ui-basic.webp" class="card-img-top" alt="Screenshot of the Basic Gum UI Setup example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="gum-stride-ui-basic.md">Basic Gum UI Setup</a></h3>
                <p><span class="badge text-bg-secondary">UI</span></p>
                <p class="card-text">Initialize Gum UI in Stride using the official Gum.Stride runtime and the Stride Community Toolkit.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/material.webp" class="card-img-top" alt="Screenshot of the Material example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="material.md">Material</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">The four numbers of a material, one per row of cubes: a glossiness sweep from rough to mirror, a metalness sweep from dielectric to metal, and a front...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/post-effects.webp" class="card-img-top" alt="Screenshot of the Post Effects example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="post-effects.md">Post Effects</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">Every post effect Stride ships, one key each: bloom, ambient occlusion, screen-space reflections, depth of field, light streaks, lens flare, fog,...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/mesh-line.webp" class="card-img-top" alt="Screenshot of the Mesh Line example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="mesh-line.md">Mesh Line</a></h3>
                <p><span class="badge text-bg-secondary">Geometry</span></p>
                <p class="card-text">A line drawn between two spheres, built as a real mesh rather than a debug primitive.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/wav-file.webp" class="card-img-top" alt="Screenshot of the Wav File example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="wav-file.md">Wav File</a></h3>
                <p><span class="badge text-bg-secondary">Audio</span></p>
                <p class="card-text">Play a .wav read from disk at runtime, with no compiled asset: LoadWav decodes the file into memory and each CreateInstance is an independent...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/procedural-sound.webp" class="card-img-top" alt="Screenshot of the Procedural Sound example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="procedural-sound.md">Procedural Sound</a></h3>
                <p><span class="badge text-bg-secondary">Audio</span></p>
                <p class="card-text">A tone with no sound file: a callback computes the samples as they play.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/give-me-cube-body.webp" class="card-img-top" alt="Screenshot of the Give Me a Cube example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="give-me-cube-body.md">Give Me a Cube</a></h3>
                <p><span class="badge text-bg-secondary">Scripts</span></p>
                <p class="card-text">Add behaviour to an entity with a SyncScript component instead of the update callback of game.Run.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/grabber.webp" class="card-img-top" alt="Screenshot of the Grabber example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="grabber.md">Grabber</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">A gravity gun: click any body to pick it up, carry it on the end of the camera ray, and let go - with its velocity, so a flick throws it.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/sync-script.webp" class="card-img-top" alt="Screenshot of the SyncScript - moving a body every frame example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="sync-script.md">SyncScript - moving a body every frame</a></h3>
                <p><span class="badge text-bg-secondary">Scripts</span></p>
                <p class="card-text">A cube driven in a circle by a SyncScript, which is the ordinary way to run code every frame.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/entity-text.webp" class="card-img-top" alt="Screenshot of the Entity Text (Screen-Space) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="entity-text.md">Entity Text (Screen-Space)</a></h3>
                <p><span class="badge text-bg-secondary">Text</span></p>
                <p class="card-text">A gallery of everything EntityTextComponent can do, one feature per pole: anchoring, shadows, backgrounds, scaling, rotation, opacity, distance...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/easing-basics.webp" class="card-img-top" alt="Screenshot of the Easing Basics example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="easing-basics.md">Easing Basics</a></h3>
                <p><span class="badge text-bg-secondary">Mathematics</span></p>
                <p class="card-text">Easing from the ground up, in four lanes that move a disc over the same two seconds: by hand with no easing, by hand with one formula, with the...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/easing.webp" class="card-img-top" alt="Screenshot of the Easing Cheat Sheet example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="easing.md">Easing Cheat Sheet</a></h3>
                <p><span class="badge text-bg-secondary">Mathematics</span></p>
                <p class="card-text">Every easing curve in the toolkit on one screen: a tile per curve with its graph, a dot riding the graph on a shared clock, and a slider that moves...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/easing-3d-game.webp" class="card-img-top" alt="Screenshot of the Easing in a 3D Game example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="easing-3d-game.md">Easing in a 3D Game</a></h3>
                <p><span class="badge text-bg-secondary">Mathematics</span></p>
                <p class="card-text">Easing doing real work in a 3D scene, four ways, each one a Tween: a kinematic platform lifts a stack of physics bodies on a sine curve and comes back...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/easing-2d-game.webp" class="card-img-top" alt="Screenshot of the Easing in a 2D Game example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="easing-2d-game.md">Easing in a 2D Game</a></h3>
                <p><span class="badge text-bg-secondary">Mathematics</span></p>
                <p class="card-text">Easing doing real work in a 2D physics scene, each piece a Tween: a kinematic lift carries a stack of boxes up and down on a sine curve, coins pop in...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/world-text.webp" class="card-img-top" alt="Screenshot of the World Text (In-Scene) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="world-text.md">World Text (In-Scene)</a></h3>
                <p><span class="badge text-bg-secondary">Text</span></p>
                <p class="card-text">A gallery of everything WorldTextComponent can do, one setting per station: billboarding that stays upright, free billboarding, text fixed in place,...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/2d-scene-panels.webp" class="card-img-top" alt="Screenshot of the 2D Panels and Text example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="2d-scene-panels.md">2D Panels and Text</a></h3>
                <p><span class="badge text-bg-secondary">Shapes</span></p>
                <p class="card-text">Twenty-four HUD panel recipes side by side, each one property away from the last: fill only, border only, transparent fill over a stripe that proves...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/primitives-2d.webp" class="card-img-top" alt="Screenshot of the Basic2D Scene (Multiple Primitives) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="primitives-2d.md">Basic2D Scene (Multiple Primitives)</a></h3>
                <p><span class="badge text-bg-secondary">Shapes</span></p>
                <p class="card-text">Create a minimal 2D scene using toolkit helpers and place multiple different primitive shapes.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/falling-shapes-2d.webp" class="card-img-top" alt="Screenshot of the Basic2D Scene (Falling Shapes) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="falling-shapes-2d.md">Basic2D Scene (Falling Shapes)</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">Create a minimal 2D scene using toolkit helpers and place multiple capsule primitives with flat materials.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/box2d-explosion.webp" class="card-img-top" alt="Screenshot of the Box2D Explosion example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="box2d-explosion.md">Box2D Explosion</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">A grenade in one call: Explode gives every shape within a radius an impulse away from the centre, per metre of perimeter facing the blast, so a wide...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/debug-render-2d.webp" class="card-img-top" alt="Screenshot of the Basic2D Scene (Debug Rendering) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="debug-render-2d.md">Basic2D Scene (Debug Rendering)</a></h3>
                <p><span class="badge text-bg-secondary">Debug</span></p>
                <p class="card-text">A pile of falling 2D shapes with the physics debug overlays turned on, so what the simulation is actually solving can be seen rather than inferred.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/dpi-aware.webp" class="card-img-top" alt="Screenshot of the DPI-Aware Window example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="dpi-aware.md">DPI-Aware Window</a></h3>
                <p><span class="badge text-bg-secondary">Debug</span></p>
                <p class="card-text">The capsule scene again, with two differences.</p>
            </div>
        </div>
    </div>
</div>

## C# Intermediate

<div class="row g-4 mb-4">
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/procedural-geometry.webp" class="card-img-top" alt="Screenshot of the Procedural Geometry example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="procedural-geometry.md">Procedural Geometry</a></h3>
                <p><span class="badge text-bg-secondary">Geometry</span></p>
                <p class="card-text">A triangle, a plane and a circle built at runtime with MeshBuilder, which handles the vertex layout and buffer bookkeeping that raw buffers make you...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/simple-geometry.webp" class="card-img-top" alt="Screenshot of the Simple Geometry (Labelled Triangle) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="simple-geometry.md">Simple Geometry (Labelled Triangle)</a></h3>
                <p><span class="badge text-bg-secondary">Geometry</span></p>
                <p class="card-text">The smallest possible custom mesh - one triangle from three vertices - with each vertex labelled on screen so the relationship between the numbers in...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/material-gallery.webp" class="card-img-top" alt="Screenshot of the Material Gallery example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="material-gallery.md">Material Gallery</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">The engine's material system on a ring of stations, all from code: the four numbers of a PBR material first, then the maps, the inputs a map can be...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/material-preview.webp" class="card-img-top" alt="Screenshot of the Material Preview example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="material-preview.md">Material Preview</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">Game Studio's material thumbnail rebuilt in code, with the editor's own numbers: the forward renderer with no post effects on a grey background, a...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/cylinder-mesh.webp" class="card-img-top" alt="Screenshot of the Cylinder Mesh example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="cylinder-mesh.md">Cylinder Mesh</a></h3>
                <p><span class="badge text-bg-secondary">Geometry</span></p>
                <p class="card-text">A cylinder generated with MeshBuilder, split into the three jobs any surface of revolution needs: place a ring of vertices, join consecutive rings...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/partial-torus-mesh.webp" class="card-img-top" alt="Screenshot of the Partial Torus Mesh example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="partial-torus-mesh.md">Partial Torus Mesh</a></h3>
                <p><span class="badge text-bg-secondary">Geometry</span></p>
                <p class="card-text">A torus defined parametrically from two angles - one around the tube, one around the ring - and cut short by limiting the second, which turns the same...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/letters-3d.webp" class="card-img-top" alt="Screenshot of the 3D Letters (Mesh Text) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="letters-3d.md">3D Letters (Mesh Text)</a></h3>
                <p><span class="badge text-bg-secondary">Text</span></p>
                <p class="card-text">A gallery of every glyph LetterMeshFactory can build - the digits, the full A-Z alphabet and the dash - as solid extruded meshes that catch the light...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/simulation-update.webp" class="card-img-top" alt="Screenshot of the Give Me a Cube (SimulationUpdate) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="simulation-update.md">Give Me a Cube (SimulationUpdate)</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">Drive an entity from the physics clock instead of the render loop.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/raycast.webp" class="card-img-top" alt="Screenshot of the Raycast example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="raycast.md">Raycast</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">Click the ground and a sphere is kicked towards where you clicked; click the sphere and it stops dead.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/hud.webp" class="card-img-top" alt="Screenshot of the Ship HUD example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="hud.md">Ship HUD</a></h3>
                <p><span class="badge text-bg-secondary">Shapes</span></p>
                <p class="card-text">A cockpit HUD composed from the toolkit's shapes and world text, with one widget per file.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/spatial-sound.webp" class="card-img-top" alt="Screenshot of the Spatial Sound example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="spatial-sound.md">Spatial Sound</a></h3>
                <p><span class="badge text-bg-secondary">Audio</span></p>
                <p class="card-text">3D positional audio for a runtime sound: a looping pad on an orb that circles a pillar, heard from the camera.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/collision-group.webp" class="card-img-top" alt="Screenshot of the Collision Group example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="collision-group.md">Collision Group</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">Two players and an enemy, where the players collide with each other but the enemy passes through both.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/collision-layer.webp" class="card-img-top" alt="Screenshot of the Collision Layer example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="collision-layer.md">Collision Layer</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">The same players-and-enemy scene as the collision group example, solved the other way.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/box2d-joints.webp" class="card-img-top" alt="Screenshot of the Box2D Joints example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="box2d-joints.md">Box2D Joints</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">Every Box2D joint, one rig each, in a row you can pull on: a hinge pendulum with a motor and a limit, a slider on a spring, a wheel on a suspension, a...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/box2d-car.webp" class="card-img-top" alt="Screenshot of the Box2D Car example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="box2d-car.md">Box2D Car</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">A car on two wheel joints over hilly terrain: the wheel joint pins the wheel, lets it turn, springs it along the suspension axis with a travel limit,...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/box2d-character-mover.webp" class="card-img-top" alt="Screenshot of the Box2D Character Mover example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="box2d-character-mover.md">Box2D Character Mover</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">A platformer character with no rigid body, on Box2D v3's mover API: a capsule the game moves itself, Quake style, asking the world only what it...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/multiple-simulations.webp" class="card-img-top" alt="Screenshot of the Multiple Physics Simulations example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="multiple-simulations.md">Multiple Physics Simulations</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">Two Bepu simulations in one game, side by side: the left lane falls under Earth gravity, the right under Moon gravity, and an amber ball that belongs...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/car.webp" class="card-img-top" alt="Screenshot of the Car example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="car.md">Car</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">A drivable car from four constraints per wheel, the recipe of bepuphysics2's own car demo in Stride's components: a linear axis servo as the...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/cloth.webp" class="card-img-top" alt="Screenshot of the Cloth example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="cloth.md">Cloth</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">Cloth from ordinary bodies and constraints, the way bepuphysics2's own demo does it: a lattice of sphere nodes tied by distance limits that may bunch...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/simple-constraint.webp" class="card-img-top" alt="Screenshot of the Simple Constraint example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="simple-constraint.md">Simple Constraint</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">One constraint, doing one thing: a distance servo holding two spheres three units apart, pulling them together or pushing them apart until they settle...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/constraint-motors.webp" class="card-img-top" alt="Screenshot of the Constraints - Servo vs Motor vs Limit example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="constraint-motors.md">Constraints - Servo vs Motor vs Limit</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">The three kinds of Bepu constraint, side by side.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/first-person-character.webp" class="card-img-top" alt="Screenshot of the First-Person Character (Bepu) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="first-person-character.md">First-Person Character (Bepu)</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">A first-person character built entirely from code - no Game Studio scene - on a Bepu CharacterComponent, with boxes to walk into and jump onto.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/instancing.webp" class="card-img-top" alt="Screenshot of the GPU Instancing example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="instancing.md">GPU Instancing</a></h3>
                <p><span class="badge text-bg-secondary">Performance</span></p>
                <p class="card-text">Render two identical walls of cubes built two different ways, side by side.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/particles.webp" class="card-img-top" alt="Screenshot of the Particle Gallery example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="particles.md">Particle Gallery</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">Thirty-eight particle systems on a ring of stations, all built from code: the building blocks one at a time - spawners, shapes, initializers,...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/debug-shapes.webp" class="card-img-top" alt="Screenshot of the Debug Shapes example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="debug-shapes.md">Debug Shapes</a></h3>
                <p><span class="badge text-bg-secondary">Debug</span></p>
                <p class="card-text">The full tour of the DebugShapes package: every immediate-mode primitive it can draw, exercised from a ShapeUpdater component so the shapes animate...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/render-to-texture.webp" class="card-img-top" alt="Screenshot of the Render to Texture example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="render-to-texture.md">Render to Texture</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">Five cameras watch one scene and each draws into a texture shown on a monitor: an overhead map, a chase camera following an orbiting ball, a fixed...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/gpu-picking.webp" class="card-img-top" alt="Screenshot of the GPU Picking example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="gpu-picking.md">GPU Picking</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">What is under the mouse, answered by the renderer instead of by physics.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/debug-shapes-usage.webp" class="card-img-top" alt="Screenshot of the Debug Shapes Usage example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="debug-shapes-usage.md">Debug Shapes Usage</a></h3>
                <p><span class="badge text-bg-secondary">Debug</span></p>
                <p class="card-text">The short version of the debug shapes example: turn the system on, draw a sphere and a circle, done.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/shape-batch.webp" class="card-img-top" alt="Screenshot of the ShapeBatch Gallery example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="shape-batch.md">ShapeBatch Gallery</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">A ring of numbered stations, one ShapeBatch idea each, from a single disc to a scrolling textured panel: discs, rings, polygons and rectangles on any...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/stride-ui-capsule-with-rigid-body.webp" class="card-img-top" alt="Screenshot of the Stride UI - Capsule and Window example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="stride-ui-capsule-with-rigid-body.md">Stride UI - Capsule and Window</a></h3>
                <p><span class="badge text-bg-secondary">UI</span></p>
                <p class="card-text">A capsule in a 3D scene with a "Hello, World" panel drawn over it using Stride's built-in UI.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/stride-ui-button-hover-animation.webp" class="card-img-top" alt="Screenshot of the Stride UI - Button Hover Animation example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="stride-ui-button-hover-animation.md">Stride UI - Button Hover Animation</a></h3>
                <p><span class="badge text-bg-secondary">UI</span></p>
                <p class="card-text">A main menu built from code whose buttons grow a blue underline while the pointer is over them.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/imgui-ui.webp" class="card-img-top" alt="Screenshot of the ImGui UI example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="imgui-ui.md">ImGui UI</a></h3>
                <p><span class="badge text-bg-secondary">UI</span></p>
                <p class="card-text">An ImGui overlay for in-game tools, debug panels and live tweaking.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/spawn-menu-2d.webp" class="card-img-top" alt="Screenshot of the 2D Spawn Menu example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="spawn-menu-2d.md">2D Spawn Menu</a></h3>
                <p><span class="badge text-bg-secondary">Input</span></p>
                <p class="card-text">Drive a scene from the keyboard without filling the screen with instructions.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/stride-ui-cube-clicker.webp" class="card-img-top" alt="Screenshot of the Cube Clicker example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="stride-ui-cube-clicker.md">Cube Clicker</a></h3>
                <p><span class="badge text-bg-secondary">UI</span></p>
                <p class="card-text">A small clicker game: cubes appear, left and right clicks are counted, and both the score and the cube positions are written to disk so the next run...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/charts-2d.webp" class="card-img-top" alt="Screenshot of the Charts 2D example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="charts-2d.md">Charts 2D</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">A flat, paper-like chart drawn entirely in code - no assets, no chart control, every line a pixel-width stroke in a shape batch.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/charts-3d.webp" class="card-img-top" alt="Screenshot of the Charts 3D example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="charts-3d.md">Charts 3D</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">The same code-only chart API as the 2D example, in a lit 3D scene.</p>
            </div>
        </div>
    </div>
</div>

## C# Advanced

<div class="row g-4 mb-4">
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/renderer.webp" class="card-img-top" alt="Screenshot of the Custom Scene Renderers example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="renderer.md">Custom Scene Renderers</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">Two ways to draw your own 2D content over a 3D scene, side by side.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/root-renderer-shader.webp" class="card-img-top" alt="Screenshot of the Root Renderer Shader example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="root-renderer-shader.md">Root Renderer Shader</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">An animated ribbon background drawn by a custom RootRenderFeature, which is the deepest extension point Stride offers short of writing your own...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/mesh-outline.webp" class="card-img-top" alt="Screenshot of the Mesh Outline Render Feature example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="mesh-outline.md">Mesh Outline Render Feature</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">Draw coloured outlines around 3D primitives with a custom RootRenderFeature.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/image-processing.webp" class="card-img-top" alt="Screenshot of the Image Processing (TextureCanvas) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="image-processing.md">Image Processing (TextureCanvas)</a></h3>
                <p><span class="badge text-bg-secondary">Rendering</span></p>
                <p class="card-text">Every combination of anchor and stretch that TextureCanvas can apply, drawn as a grid of thumbnails so the options can be compared rather than read...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/instancing-entity-transform.webp" class="card-img-top" alt="Screenshot of the Instancing with Entity Transforms example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="instancing-entity-transform.md">Instancing with Entity Transforms</a></h3>
                <p><span class="badge text-bg-secondary">Performance</span></p>
                <p class="card-text">Keep every object a real entity - with a transform, a physics body and anything else you need - while still drawing the whole crowd in a single draw...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/stress-pile-2d.webp" class="card-img-top" alt="Screenshot of the Basic2D Scene (Stress Pile) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="stress-pile-2d.md">Basic2D Scene (Stress Pile)</a></h3>
                <p><span class="badge text-bg-secondary">Performance</span></p>
                <p class="card-text">Thousands of 2D physics bodies piling up, drawn in two instanced draw calls - awake bodies through one master, sleeping bodies tinted green through...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/stress-pile-2d-box2d.webp" class="card-img-top" alt="Screenshot of the Basic 2D Scene (Stress Pile, Box2D) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="stress-pile-2d-box2d.md">Basic 2D Scene (Stress Pile, Box2D)</a></h3>
                <p><span class="badge text-bg-secondary">Performance</span></p>
                <p class="card-text">The Box2D twin of the stress pile: thousands of bodies piling up, drawn in two instanced draw calls - awake bodies through one master, sleeping bodies...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/junkyard-box2d.webp" class="card-img-top" alt="Screenshot of the Junkyard (Box2D) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="junkyard-box2d.md">Junkyard (Box2D)</a></h3>
                <p><span class="badge text-bg-secondary">Performance</span></p>
                <p class="card-text">A faithful replica of the Box2D.NET BenchmarkJunkyard sample: 8,000 small five-sided rocks rain into a walled yard and a kinematic plow sweeps back...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/junkyard-playground-box2d.webp" class="card-img-top" alt="Screenshot of the Junkyard Playground (Box2D) example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="junkyard-playground-box2d.md">Junkyard Playground (Box2D)</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">The playground sibling of the Junkyard replica: the same walled yard and sweeping plow, built the Stride way - every shape is an entity carrying...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/constraints.webp" class="card-img-top" alt="Screenshot of the Various Constraints example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="constraints.md">Various Constraints</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">The full tour of Bepu constraints in one interactive scene: a distance limit holding two spheres within a range, a distance servo actively driving a...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/constraint-rope.webp" class="card-img-top" alt="Screenshot of the Rope - building a stable chain of constraints example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="constraint-rope.md">Rope - building a stable chain of constraints</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">Bepu has no rope type, so a rope is a chain of small bodies tied together at runtime.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/box2d-physics.webp" class="card-img-top" alt="Screenshot of the Box2D.NET Physics example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="box2d-physics.md">Box2D.NET Physics</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">A 2D simulation run by Box2D.NET rather than by Stride's own physics, with Stride reduced to drawing the result.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/jitter2-physics.webp" class="card-img-top" alt="Screenshot of the Jitter2 Physics Integration example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="jitter2-physics.md">Jitter2 Physics Integration</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">Demonstrates integrating Jitter2 physics engine with Stride.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/jitter2-constraints.webp" class="card-img-top" alt="Screenshot of the Jitter2 Physics - Constraining to 2D example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="jitter2-constraints.md">Jitter2 Physics - Constraining to 2D</a></h3>
                <p><span class="badge text-bg-secondary">Physics</span></p>
                <p class="card-text">Demonstrates constraining a Jitter2 3D physics simulation to 2D-style behaviour.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/stride-ui-draggable-window.webp" class="card-img-top" alt="Screenshot of the Stride UI - Draggable Window example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="stride-ui-draggable-window.md">Stride UI - Draggable Window</a></h3>
                <p><span class="badge text-bg-secondary">UI</span></p>
                <p class="card-text">A windowing system built on Stride's UI: windows with title bars and close buttons that can be dragged around, and that come to the front when...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/stride-ui-draggable-window-bullet.webp" class="card-img-top" alt="Screenshot of the Stride UI - Draggable Window - Bullet Physics example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="stride-ui-draggable-window-bullet.md">Stride UI - Draggable Window - Bullet Physics</a></h3>
                <p><span class="badge text-bg-secondary">UI</span></p>
                <p class="card-text">The draggable window example running on the legacy Bullet physics engine.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/compute-boids.webp" class="card-img-top" alt="Screenshot of the Compute Shader Boids example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="compute-boids.md">Compute Shader Boids</a></h3>
                <p><span class="badge text-bg-secondary">Performance</span></p>
                <p class="card-text">A flock of thousands of boids that lives entirely on the GPU.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/imgui-net.webp" class="card-img-top" alt="Screenshot of the ImGui.NET Text Rendering example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="imgui-net.md">ImGui.NET Text Rendering</a></h3>
                <p><span class="badge text-bg-secondary">UI</span></p>
                <p class="card-text">Render debug text with ImGui.NET, both in screen space and anchored to positions in the 3D scene.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/stride-signalr.webp" class="card-img-top" alt="Screenshot of the Stride + SignalR - Orbital Cargo Deck example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="stride-signalr.md">Stride + SignalR - Orbital Cargo Deck</a></h3>
                <p><span class="badge text-bg-secondary">Networking</span></p>
                <p class="card-text">A Stride game and a Blazor web page as two consoles of the same orbital cargo deck, talking both ways over a SignalR hub.</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/cube-collapse.webp" class="card-img-top" alt="Screenshot of the Game - Cube Collapse example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="cube-collapse.md">Game - Cube Collapse</a></h3>
                <p><span class="badge text-bg-secondary">Game</span></p>
                <p class="card-text">A colour-match collapse puzzle built entirely from code.</p>
            </div>
        </div>
    </div>
</div>

## C# Other

<div class="row g-4 mb-4">
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/collidable-gizmo.webp" class="card-img-top" alt="Screenshot of the Collidable Gizmo example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="collidable-gizmo.md">Collidable Gizmo</a></h3>
                <p><span class="badge text-bg-secondary">Debug</span></p>
                <p class="card-text">A single-purpose demo of CollidableGizmoScript, which draws the collider Bepu is actually using so it can be compared against the model you think you...</p>
            </div>
        </div>
    </div>
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/debug-render-component.webp" class="card-img-top" alt="Screenshot of the Debug Render Component example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="debug-render-component.md">Debug Render Component</a></h3>
                <p><span class="badge text-bg-secondary">Debug</span></p>
                <p class="card-text">The companion to the collidable gizmo: DebugRenderComponentScript draws the wireframe of an entity's own mesh rather than its collider.</p>
            </div>
        </div>
    </div>
</div>

## F# Getting Started

<div class="row g-4 mb-4">
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/capsule-with-rigid-body-fs.webp" class="card-img-top" alt="Screenshot of the Capsule with rigid body in F# example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="capsule-with-rigid-body-fs.md">Capsule with rigid body in F#</a></h3>
                <p><span class="badge text-bg-secondary">Shapes</span></p>
                <p class="card-text">The first code-only scene written in F#.</p>
            </div>
        </div>
    </div>
</div>

## F# Intermediate

<div class="row g-4 mb-4">
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/partial-torus-mesh-fs.webp" class="card-img-top" alt="Screenshot of the Partial Torus Mesh in F# example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="partial-torus-mesh-fs.md">Partial Torus Mesh in F#</a></h3>
                <p><span class="badge text-bg-secondary">Geometry</span></p>
                <p class="card-text">The parametric partial torus, written in F#.</p>
            </div>
        </div>
    </div>
</div>

## Visual Basic Getting Started

<div class="row g-4 mb-4">
    <div class="col-xxl-4 col-md-6">
        <div class="card h-100">
            <img src="media/capsule-with-rigid-body-vb.webp" class="card-img-top" alt="Screenshot of the Capsule with rigid body in Visual Basic example" width="1280" height="720" loading="lazy">
            <div class="card-body">
                <h3 class="card-title h6"><a class="stretched-link text-decoration-none text-body" href="capsule-with-rigid-body-vb.md">Capsule with rigid body in Visual Basic</a></h3>
                <p><span class="badge text-bg-secondary">Shapes</span></p>
                <p class="card-text">The first code-only scene written in Visual Basic.</p>
            </div>
        </div>
    </div>
</div>