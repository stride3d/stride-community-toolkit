using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Text.Meshes;
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

game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: Start, update: Update);

void Start(Scene rootScene)
{
    CreateMeshEntity(rootScene, Vector3.Zero, CreateTriangleMesh);
    CreateMeshEntity(rootScene, Vector3.UnitX * 2, CreatePlaneMesh);
    CreateMeshEntity(rootScene, Vector3.UnitX * 4, CreateNonIndexedTriangleMesh);

    CreateLetterEntity(rootScene);
}

void CreateLetterEntity(Scene rootScene)
{
    // Solid lettering: the toolkit's embedded font's glyph outlines, triangulated by ear clipping and
    // extruded. Real geometry, so it is lit and shadowed like any other mesh - unlike
    // EntityTextComponent and WorldTextComponent, which draw font glyphs.
    //
    // Standing on the ground (the baseline is at Y = 0) behind the gallery row, on purpose: letters
    // floating in mid-air seem to turn as the camera orbits, with nothing to anchor the parallax
    var entity = new Entity { Scene = rootScene, Transform = { Position = new Vector3(-1.5f, 0, -2.5f) } };

    // Not the shared material: that one reads colour from a vertex stream, which the text mesh lacks
    var model = TextMesh.CreateModel(game.GraphicsDevice, game.CreateMaterial(Color.Gold, metalness: 0.1f, glossiness: 0.4f), MeshFont.Default, "XYZ");

    entity.Add(new ModelComponent { Model = model });
}

void CreateNonIndexedTriangleMesh(MeshBuilder meshBuilder)
{
    // No index buffer: with IndexingType.None the vertices are drawn in the order added, three per
    // triangle, at the cost of repeating shared vertices.
    meshBuilder.WithIndexType(IndexingType.None);
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    var position = meshBuilder.WithPosition<Vector3>();
    var color = meshBuilder.WithColor<Color>();

    AddVertex(meshBuilder, position, color, new Vector3(0, 0, 0), Color.Yellow);
    AddVertex(meshBuilder, position, color, new Vector3(0.5f, 1, 0), Color.Purple);
    AddVertex(meshBuilder, position, color, new Vector3(1, 0, 0), Color.Teal);
}

void Update(Scene rootScene, GameTime gameTime)
{
    var segments = (int)((Math.Cos(gameTime.Total.TotalMilliseconds / 500) + 1) / 2 * 47) + 3;

    // Release the old mesh first: ToMeshDraw hands the caller two GPU buffers that no content manager
    // tracks, so removing the entity alone leaks them - every frame, here.
    if (circleEntity is not null)
    {
        ReleaseMeshBuffers(circleEntity);
        circleEntity.Remove();
    }

    circleEntity = CreateMeshEntity(rootScene, Vector3.UnitX * -2, b => CreateCircleMesh(b, segments));
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

    AddVertex(meshBuilder, position, color, new Vector3(0, 0, 0), Color.Red);
    AddVertex(meshBuilder, position, color, new Vector3(1, 0, 0), Color.Green);
    AddVertex(meshBuilder, position, color, new Vector3(.5f, 1, 0), Color.Blue);

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

    AddVertex(meshBuilder, position, color, new Vector3(0, 0, 0), Color.Red);
    AddVertex(meshBuilder, position, color, new Vector3(0, 1, 0), Color.Green);
    AddVertex(meshBuilder, position, color, new Vector3(1, 1, 0), Color.Blue);
    AddVertex(meshBuilder, position, color, new Vector3(1, 0, 0), Color.Yellow);

    meshBuilder.AddIndex(0);
    meshBuilder.AddIndex(1);
    meshBuilder.AddIndex(2);

    meshBuilder.AddIndex(0);
    meshBuilder.AddIndex(2);
    meshBuilder.AddIndex(3);
}

static void AddVertex(MeshBuilder meshBuilder, int position, int color, Vector3 at, Color colour)
{
    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, at);
    meshBuilder.SetElement(color, colour);
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
        var vertexColor = new ColorHSV(360f / segments * i, 1, 1, 1).ToColor();

        meshBuilder.AddVertex();
        meshBuilder.SetElement(position, new Vector3(x + .5f, y + .5f, 0));
        meshBuilder.SetElement(color, vertexColor);
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

Entity CreateMeshEntity(Scene scene, Vector3 position, Action<MeshBuilder> build)
{
    using var meshBuilder = new MeshBuilder();

    build(meshBuilder);

    var entity = new Entity { Scene = scene, Transform = { Position = position } };

    // One material for every mesh, so the animated circle does not generate a new one each frame.
    sharedMaterial ??= CreateMaterial(game.GraphicsDevice);

    var model = new Model { new Mesh { Draw = meshBuilder.ToMeshDraw(game.GraphicsDevice) }, sharedMaterial };

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
  - "Disposing the previous mesh's buffers before replacing it, and sharing one material"
  - Why clockwise winding matters for which face you see
  - Creating a non-indexed mesh when index reuse buys nothing
  - "Using helpers: SceneSetup3D, UseSkybox, CreateMaterial"
tags:
  - 3D
  - Geometry
  - Mesh
  - MeshBuilder
  - Procedural
  - Winding
  - Disposal
related:
  - E07_3D_SimpleGeometry
  - E07_3D_CylinderMesh
  - E03_3D_MeshText
enabled: true
created: 2023-10-15
---
*/