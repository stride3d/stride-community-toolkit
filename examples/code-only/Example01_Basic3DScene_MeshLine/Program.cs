using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Gizmos;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();
    game.AddSkybox();

    var lineEntity = CreateLineEntity(game);

    var entity1 = CreateSphereEntity(game);
    entity1.Transform.Position = new Vector3(0, 8, 0);
    entity1.AddChild(lineEntity);

    var entity2 = CreateSphereEntity(game);
    entity2.Transform.Position = new Vector3(-0.01f, 9, -0.01f);

    entity1.Scene = rootScene;
    entity2.Scene = rootScene;
}

static Entity CreateSphereEntity(Game game)
    => game.Create3DPrimitive(PrimitiveModelType.Sphere);

static Entity CreateLineEntity(Game game)
{
    // Create vertex buffer with start and end points
    var vertices = new Vector3[] { new(0, 0, 0), new(1, 1, -1) };
    var vertexBuffer = Buffer.New(game.GraphicsDevice, vertices, BufferFlags.VertexBuffer, GraphicsResourceUsage.Default);

    // Create index buffer
    var indices = new short[] { 0, 1 };
    var indexBuffer = Buffer.New(game.GraphicsDevice, indices, BufferFlags.IndexBuffer, GraphicsResourceUsage.Default);

    var material = GizmoEmissiveColorMaterial.Create(game.GraphicsDevice, Color.DarkMagenta);
    // Or use this for a specific color
    //var material = game.CreateMaterial(Color.DarkMagenta);

    var meshDraw = new MeshDraw
    {
        PrimitiveType = PrimitiveType.LineList,
        VertexBuffers = [new VertexBufferBinding(vertexBuffer, new VertexDeclaration(VertexElement.Position<Vector3>()), vertices.Length)],
        IndexBuffer = new IndexBufferBinding(indexBuffer, is32Bit: false, indices.Length),
        DrawCount = indices.Length
    };

    var mesh = new Mesh { Draw = meshDraw };
    var model = new Model { mesh, material };

    return new Entity { new ModelComponent(model) };
}
/*
---example-metadata
slug: mesh-line
title:
  en: Mesh Line
level: Beginner
category: Geometry
complexity: 2
order: 30
description:
  en: |-
    A line drawn between two spheres, built as a real mesh rather than a debug primitive. Two vertices,
    an index buffer, a MeshDraw set to LineList and an emissive material are all it takes, which makes
    this the smallest useful tour of Stride's low-level geometry API. The line is parented to one of the
    spheres, so moving that sphere moves the line with it.
concepts:
  - Building a mesh from raw vertex and index buffers
  - Declaring a vertex layout with VertexDeclaration
  - "Drawing with PrimitiveType.LineList instead of triangles"
  - Wrapping a MeshDraw in a Mesh, Model and ModelComponent
  - Making a line visible with an emissive material
  - Parenting an entity so it follows another
  - "Using helpers: SetupBase3DScene, AddSkybox, Create3DPrimitive, CreateMaterial"
tags:
  - 3D
  - Geometry
  - Mesh
  - Vertex Buffer
  - Index Buffer
  - Line
  - Emissive
related:
  - Example05_ProceduralGeometry
  - Example05_SimpleGeometry
  - Example08_DebugShapes
media: stride-game-engine-example-01-mesh-line.webp
enabled: true
created: 2025-02-02
---
*/