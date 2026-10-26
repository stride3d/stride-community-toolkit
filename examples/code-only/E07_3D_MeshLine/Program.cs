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

game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: Start);

void Start(Scene rootScene)
{
    game.AddGrabber();

    // The line is a child of the first sphere, so it falls and rolls with it
    var sphere1 = game.CreatePrimitive(Primitive3D.Sphere());
    sphere1.Transform.Position = new Vector3(0, 8, 0);
    sphere1.AddChild(CreateLineEntity());
    sphere1.Scene = rootScene;

    var sphere2 = game.CreatePrimitive(Primitive3D.Sphere());
    sphere2.Transform.Position = new Vector3(-0.01f, 9, -0.01f);
    sphere2.Scene = rootScene;
}

Entity CreateLineEntity()
{
    Vector3[] vertices = [new(0, 0, 0), new(1, 1, -1)];
    var vertexBuffer = Buffer.New(game.GraphicsDevice, vertices, BufferFlags.VertexBuffer, GraphicsResourceUsage.Default);

    short[] indices = [0, 1];
    var indexBuffer = Buffer.New(game.GraphicsDevice, indices, BufferFlags.IndexBuffer, GraphicsResourceUsage.Default);

    // Emissive, so the line shows whatever the lighting; an intensity above 1 also carries it past the
    // bloom threshold and gives it a glow. A lit material such as game.CreateMaterial would draw this mesh
    // black: lighting needs normals, and the vertex layout below has positions only.
    var material = GizmoEmissiveColorMaterial.Create(game.GraphicsDevice, Color.OrangeRed, 4);

    var meshDraw = new MeshDraw
    {
        PrimitiveType = PrimitiveType.LineList,
        VertexBuffers = [new VertexBufferBinding(vertexBuffer, new VertexDeclaration(VertexElement.Position<Vector3>()), vertices.Length)],
        IndexBuffer = new IndexBufferBinding(indexBuffer, is32Bit: false, indices.Length),
        DrawCount = indices.Length
    };

    return new Entity { new ModelComponent(new Model { new Mesh { Draw = meshDraw }, material }) };
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
    A line attached to a sphere, built as a real mesh rather than a debug primitive. Two vertices,
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
  - "Using helpers: SceneSetup3D, UseSkybox, CreatePrimitive, GizmoEmissiveColorMaterial.Create"
tags:
  - 3D
  - Geometry
  - Mesh
  - Vertex Buffer
  - Index Buffer
  - Line
  - Emissive
related:
  - E07_3D_ProceduralGeometry
  - E07_3D_SimpleGeometry
  - E08_3D_DebugShapes
enabled: true
created: 2025-02-02
---
*/