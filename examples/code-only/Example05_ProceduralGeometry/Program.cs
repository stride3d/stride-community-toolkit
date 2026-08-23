using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

using var game = new Game();

Entity? circleEntity = null;
Material? sharedMaterial = null;
game.Run(start: Start, update: Update);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    CreateMeshEntity(game.GraphicsDevice, rootScene, Vector3.Zero, CreateTriangleMesh);
    CreateMeshEntity(game.GraphicsDevice, rootScene, Vector3.UnitX * 2, CreatePlaneMesh);
    CreateMeshEntity(game.GraphicsDevice, rootScene, Vector3.UnitX * 4, CreateNonIndexedTriangleMesh);

    CreateLetterEntity(rootScene);
}

void CreateLetterEntity(Scene rootScene)
{
    // Solid extruded lettering: glyph outlines authored in code, triangulated by ear clipping and
    // extruded through MeshBuilder. Real geometry, so it is lit and shadowed like any other mesh -
    // unlike EntityTextComponent and WorldTextComponent, which draw font glyphs.
    //
    // Standing on the ground behind the gallery row, on purpose: the glyph baseline is at Y = 0,
    // and letters floating in mid-air read strangely when the camera orbits - with nothing
    // anchoring them, the parallax between the front faces and the side walls looks like the
    // letters themselves are turning
    var entity = new Entity { Scene = rootScene, Transform = { Position = new Vector3(-1.5f, 0, -2.5f) } };

    // Not the gallery's shared material: that one reads colour from a vertex stream, and the letter
    // mesh has position and normal only, so it needs a material with a colour of its own
    var model = new Model
    {
        new MaterialInstance { Material = game.CreateMaterial(Color.Gold, specular: 0.1f, microSurface: 0.4f) },
        new Mesh
        {
            Draw = LetterMeshFactory.CreateTextMeshDraw(game.GraphicsDevice, "XYZ"),
            MaterialIndex = 0
        }
    };

    entity.Add(new ModelComponent { Model = model });
}

void CreateNonIndexedTriangleMesh(MeshBuilder meshBuilder)
{
    // No index buffer at all: with IndexingType.None the vertices are drawn in the order they were
    // added, three per triangle. Simplest possible mesh, at the cost of repeating shared vertices.
    meshBuilder.WithIndexType(IndexingType.None);
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    var position = meshBuilder.WithPosition<Vector3>();
    var color = meshBuilder.WithColor<Color>();

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(0, 0, 0));
    meshBuilder.SetElement(color, Color.Yellow);

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(0.5f, 1, 0));
    meshBuilder.SetElement(color, Color.Purple);

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(1, 0, 0));
    meshBuilder.SetElement(color, Color.Teal);
}

void Update(Scene rootScene, GameTime gameTime)
{
    var segments = (int)((Math.Cos(gameTime.Total.TotalMilliseconds / 500) + 1) / 2 * 47) + 3;

    // Rebuilding a mesh means releasing the old one first. ToMeshDraw hands the caller two GPU
    // buffers that no content manager tracks, so removing the entity alone leaks them - and this
    // runs every frame, which is exactly how such a leak becomes megabytes per second.
    if (circleEntity is not null)
    {
        ReleaseMeshBuffers(circleEntity);
        circleEntity.Remove();
    }

    circleEntity = CreateMeshEntity(game.GraphicsDevice, rootScene, Vector3.UnitX * -2, b => CreateCircleMesh(b, segments));
}

// Disposes the GPU buffers behind an entity's meshes. Only for meshes this example built itself -
// content-manager-loaded models manage their own buffers.
static void ReleaseMeshBuffers(Entity entity)
{
    var model = entity.Get<ModelComponent>()?.Model;

    if (model is null) return;

    foreach (var mesh in model.Meshes)
    {
        foreach (var vertexBuffer in mesh.Draw.VertexBuffers)
        {
            vertexBuffer.Buffer.Dispose();
        }

        mesh.Draw.IndexBuffer?.Buffer.Dispose();
    }
}

void CreateTriangleMesh(MeshBuilder meshBuilder)
{
    meshBuilder.WithIndexType(IndexingType.Int16);
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    var position = meshBuilder.WithPosition<Vector3>();
    var color = meshBuilder.WithColor<Color>();

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(0, 0, 0));
    meshBuilder.SetElement(color, Color.Red);

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(1, 0, 0));
    meshBuilder.SetElement(color, Color.Green);

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(.5f, 1, 0));
    meshBuilder.SetElement(color, Color.Blue);

    meshBuilder.AddIndex(0);
    meshBuilder.AddIndex(2);
    meshBuilder.AddIndex(1);
}

void CreatePlaneMesh(MeshBuilder meshBuilder)
{
    meshBuilder.WithIndexType(IndexingType.Int16);
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    var position = meshBuilder.WithPosition<Vector3>();
    var color = meshBuilder.WithColor<Color>();

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(0, 0, 0));
    meshBuilder.SetElement(color, Color.Red);

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(0, 1, 0));
    meshBuilder.SetElement(color, Color.Green);

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(1, 1, 0));
    meshBuilder.SetElement(color, Color.Blue);

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(1, 0, 0));
    meshBuilder.SetElement(color, Color.Yellow);

    meshBuilder.AddIndex(0);
    meshBuilder.AddIndex(1);
    meshBuilder.AddIndex(2);

    meshBuilder.AddIndex(0);
    meshBuilder.AddIndex(2);
    meshBuilder.AddIndex(3);
}

void CreateCircleMesh(MeshBuilder meshBuilder, int segments)
{
    meshBuilder.WithIndexType(IndexingType.Int16);
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    var position = meshBuilder.WithPosition<Vector3>();
    var color = meshBuilder.WithColor<Color4>();

    for (var i = 0; i < segments; i++)
    {
        var x = (float)Math.Sin(Math.Tau / segments * i) / 2;
        var y = (float)Math.Cos(Math.Tau / segments * i) / 2;
        var hsl = new ColorHSV(360f / segments * i, 1, 1, 1).ToColor();

        meshBuilder.AddVertex();
        meshBuilder.SetElement(position, new Vector3(x + .5f, y + .5f, 0));
        meshBuilder.SetElement(color, hsl);
    }

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(.5f, .5f, 0));
    meshBuilder.SetElement(color, Color.Black.ToColor4());

    for (var i = 0; i < segments; i++)
    {
        meshBuilder.AddIndex(segments);
        meshBuilder.AddIndex(i);
        meshBuilder.AddIndex((i + 1) % segments);
    }
}

Entity CreateMeshEntity(GraphicsDevice graphicsDevice, Scene rootScene, Vector3 position, Action<MeshBuilder> build)
{
    using var meshBuilder = new MeshBuilder();

    build(meshBuilder);

    var entity = new Entity { Scene = rootScene, Transform = { Position = position } };

    // One material shared by every mesh this example builds. A material is a GPU resource like the
    // buffers are, so building one per frame for the animated circle would be the same leak again.
    sharedMaterial ??= CreateMaterial(graphicsDevice);

    var model = new Model
    {
        new MaterialInstance { Material = sharedMaterial },
        new Mesh {
            Draw = meshBuilder.ToMeshDraw(graphicsDevice),
            MaterialIndex = 0
        }
    };

    entity.Add(new ModelComponent { Model = model });

    return entity;
}

static Material CreateMaterial(GraphicsDevice graphicsDevice) => Material.New(graphicsDevice, new MaterialDescriptor
{
    Attributes = new MaterialAttributes
    {
        DiffuseModel = new MaterialDiffuseLambertModelFeature(),
        Diffuse = new MaterialDiffuseMapFeature
        {
            DiffuseMap = new ComputeVertexStreamColor()
        },
    }
});
/*
---example-metadata
slug: procedural-geometry
title:
  en: Procedural Geometry
level: Intermediate
category: Geometry
complexity: 3
order: 10
description:
  en: |-
    A triangle, a plane and a circle built at runtime with MeshBuilder, which handles the vertex layout
    and buffer bookkeeping that raw buffers make you do by hand. The circle is rebuilt every frame with
    a changing segment count, which is where the one rule of dynamic geometry shows up: dispose the old
    buffers before swapping in the new mesh, or leak a buffer pair per frame.
concepts:
  - Declaring a vertex layout and filling it with MeshBuilder
  - Building a triangle, a plane and a circle from first principles
  - Rebuilding a mesh every frame as a parameter changes
  - "Disposing the previous mesh and material before replacing them"
  - Why clockwise winding matters for which face you see
  - Creating a non-indexed mesh when index reuse buys nothing
  - "Using helpers: SetupBase3DScene, AddSkybox, CreateFlatMaterial"
tags:
  - 3D
  - Geometry
  - Mesh
  - MeshBuilder
  - Procedural
  - Winding
  - Disposal
related:
  - Example05_SimpleGeometry
  - Example05_CylinderMesh
  - Example01_Letters3D
media: stride-game-engine-procedural-geometry.webp
enabled: true
created: 2023-10-15
---
*/