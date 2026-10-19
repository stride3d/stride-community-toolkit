using E03_3D_MeshText;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Rendering.Text.Meshes;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Font;
using Stride.Input;
using Stride.Rendering;

// Solid 3D text from a real font. TextMesh reads a font's glyph outlines through the FreeType the
// engine ships, triangulates them, holes and all, and extrudes them: letters that are MESHES, lit and
// shadowed like any other geometry, with a material of their own, and with a body when
// CreatePrimitive makes them. On show: the Open Sans embedded in the toolkit, so this works with no
// font file at hand, with a bevelled edge; an installed font found by family name; lines that align; a
// frame counter whose digits are rebuilt every frame; and words with physics, dropped on the ground.
//
// For ordinary text in a real font - labels, HUDs, signs - use E03_3D_EntityText (screen-space) or
// E03_3D_WorldText (in-scene) instead: they draw a font, which is far cheaper than geometry.

WindowsDpiManager.EnablePerMonitorV2();

// The default camera starts at (6, 6, 6) looking at the origin, so the gallery is turned 45 degrees
// to face it square-on. Fly around it: these are solids, not billboards
var faceCamera = Quaternion.RotationY(MathUtil.DegreesToRadians(45));
var dropped = 0;
var random = new Random(1);

MeshFont? systemFont = null;
Material? dropMaterial = null;

using var game = new Game();

game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: Start, update: Update);

void Start(Scene scene)
{
    game.Window.AllowUserResizing = true;
    game.Window.Title = "3D text from a font - Stride Community Toolkit";
    game.AddGrabber();

    // The base scene's single directional light shines from behind this gallery, leaving the front
    // caps in the dark. Six lights down the six world axes make solid lettering read from any side
    game.AddAllDirectionLighting(intensity: 20f, showLightGizmo: false);

    var gold = game.CreateMaterial(Color.Gold);
    var silver = game.CreateMaterial(new Color(210, 215, 225));
    var blue = game.CreateMaterial(new Color(120, 190, 255));

    // The embedded font: Open Sans Regular, so the lettering is the same on every machine. A bevel rounds
    // the edge between the caps and the walls, which is what catches the light on a title
    AddText(scene, MeshFont.Default, "Stride Community Toolkit", new Vector3(0f, 3.6f, 0f), gold,
        new TextMeshOptions { Size = 0.6f, Depth = 0.2f, Anchor = TextAnchor.MiddleCenter, Bevel = new TextBevel(size: 0.015f, thickness: 0.03f, segments: 4) });

    // An installed font by family name and style; null when this machine has no such family, so the
    // embedded one stands in and the line says so
    systemFont = MeshFont.FromFamily("Segoe UI", FontStyle.Bold)
        ?? MeshFont.FromFamily("DejaVu Sans", FontStyle.Bold)
        ?? MeshFont.FromFamily("Liberation Sans", FontStyle.Bold);

    AddText(scene, systemFont ?? MeshFont.Default, systemFont is null ? "No bold system font here" : $"{systemFont.Name}, installed here", new Vector3(0f, 2.7f, 0f), silver,
        new TextMeshOptions { Size = 0.45f, Depth = 0.15f, Anchor = TextAnchor.MiddleCenter });

    // Several lines in one mesh: centred on each other, a little closer than the font's own line
    // height, and a lower curve quality that is still smooth at this size
    AddText(scene, MeshFont.Default, "lines of text\nalign to each other\nand share one mesh", new Vector3(0f, 2.1f, 0f), blue,
        new TextMeshOptions { Size = 0.3f, Depth = 0.1f, Alignment = TextAlignment.Center, LineSpacing = 0.9f, Anchor = TextAnchor.TopCenter, CurveQuality = 16 });

    // The counter: a script that replaces its digit mesh whenever the number changes
    var counter = new Entity("FrameCounter")
    {
        new FrameCounterScript { Material = blue }
    };

    // A step toward the camera, so it sits in front of the gallery
    counter.Transform.Position = new Vector3(1.6f, 0.4f, 1.6f);
    counter.Transform.Rotation = faceCamera;
    counter.Transform.Scale = new Vector3(0.5f);
    counter.Scene = scene;

    // Three words to begin with, each higher than the last, so they land on each other
    for (var i = 0; i < 3; i++) DropWord(scene);

    TextOverlay.GetOrCreate(game).AddSection("3D text", () =>
    [
        new("R", "Drop another word", Color.Gold),
        new(""),
        new($"Embedded font: {MeshFont.Default.Name}", Color.LightGray),
        new($"System font: {systemFont?.Name ?? "none found"}", Color.LightGray),
        new($"Words dropped: {dropped}", Color.LightGray),
    ]);
}

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.R)) DropWord(scene);
}

/// <summary>
/// One string as one mesh, built once and never touched again. Static lettering is this cheap: the
/// cost lives only where text changes.
/// </summary>
void AddText(Scene scene, MeshFont font, string text, Vector3 position, Material material, TextMeshOptions options)
{
    // CreateModel uploads the mesh and sets its bounds, which the renderer needs to cull the lettering
    // when it is off screen; a bare MeshDraw has none
    var entity = new Entity($"Text {text}")
    {
        new ModelComponent { Model = TextMesh.CreateModel(game.GraphicsDevice, material, font, text, options) }
    };

    entity.Transform.Position = position;
    entity.Transform.Rotation = faceCamera;
    entity.Scene = scene;
}

/// <summary>
/// A word with a body. Primitive3D.Text is a shape like any other: CreatePrimitive gives it the
/// material and, with Bepu chosen, a collider of one convex hull per glyph, so it lands, tips over
/// and can be grabbed. Each word starts a little higher than the last.
/// </summary>
void DropWord(Scene scene)
{
    dropMaterial = game.CreateMaterial(random.NextColor(), metalness: 0.1f, glossiness: 0.5f);

    var word = game.CreatePrimitive(Primitive3D.Text("Drop", size: 0.7f, depth: 0.3f), new BepuPhysicsOptions { Material = dropMaterial });

    word.Transform.Position = new Vector3(-2.2f, 4f + dropped * 0.7f, 2.2f);
    word.Transform.Rotation = faceCamera;
    word.Scene = scene;

    dropped++;
}

/*
---example-metadata
slug: letters-3d
title:
  en: 3D Text from a Font
  cs: 3D text z fontu
level: Intermediate
category: Text
complexity: 3
order: 50
description:
  en: |-
    Solid 3D text from a real font: TextMesh reads a font's glyph outlines through the FreeType the
    engine ships, triangulates them, holes and all, and extrudes them into meshes that are lit and
    shadowed like any other geometry. On show: the Open Sans embedded in the toolkit with a bevelled edge, an installed
    font found by family name, several lines that align in one mesh, a frame counter whose digits
    are rebuilt as a new mesh every frame, and words with physics through Primitive3D.Text, dropped
    on the ground with one convex hull per glyph. The counter demonstrates the one rule of dynamic
    mesh text: dispose the old GPU buffers before swapping in the new mesh, or leak a buffer pair per
    rebuild.
  cs: |-
    Plný 3D text z opravdového fontu: TextMesh načte obrysy glyfů přes FreeType, které engine
    dodává, triangulizuje je i s dírami a vytlačí do meshů, které světlo a stíny zpracují jako
    jakoukoli jinou geometrii. K vidění: Open Sans vložený do toolkitu, nainstalovaný font nalezený
    podle rodiny, několik řádků zarovnaných v jednom meshi, počítadlo snímků, jehož číslice se každý
    snímek staví znovu, a slova s fyzikou přes Primitive3D.Text, upuštěná na zem s jedním konvexním
    obalem na glyf. Počítadlo ukazuje jediné pravidlo dynamického textu z meshů: před výměnou meshe
    uvolnit staré GPU buffery, jinak každá přestavba uteče o pár bufferů.
concepts:
  - "Solid 3D lettering from any font: TextMesh.CreateModel with MeshFont.Default, Load or FromFamily"
  - TextMeshOptions - size per EM, depth, bevel, anchor, alignment, line spacing, curve quality
  - Words as primitives with bodies - Primitive3D.Text through CreatePrimitive, one convex hull per glyph
  - Static lettering built once versus text that changes, and releasing the old buffers
  - "Using helpers: SceneSetup3D, AddAllDirectionLighting, CreatePrimitive, TextOverlay"
tags:
  - 3D
  - Text
  - Mesh
  - Font
  - Procedural Geometry
  - Physics
related:
  - E03_3D_EntityText
  - E03_3D_WorldText
  - E07_3D_ProceduralGeometry
  - E20_3D_CubeCollapse
screenshotFrame: 150
enabled: true
created: 2026-08-22
---
*/