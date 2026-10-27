using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Gizmos;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.ProceduralModels;
using Buffer = Stride.Graphics.Buffer;

using var game = new Game();

game.Run(new SceneSetup3D { Profiler = true }.UseBepu().UseSkybox(), start: scene =>
{
    game.AddGroundGizmo(new(-4, 0, -4), showAxisName: true);

    // The world axes in the bottom left corner, turning with the camera
    game.AddOrientationGizmo();

    // Screen-space labels for the EntityTextComponents below
    game.AddSceneRenderer(new EntityTextRenderer());

    AddTriangleEntity(scene);
});

void AddTriangleEntity(Scene scene)
{
    const float startX = -4f;
    const float startZ = -1f;

    var vertices = new VertexPositionTexture[3];
    vertices[0].Position = new Vector3(startX, 0f, startZ);
    vertices[1].Position = new Vector3(startX + 0.5f, 1f, startZ);
    vertices[2].Position = new Vector3(startX + 1f, 0f, startZ);

    var vertexBuffer = Buffer.Vertex.New(game.GraphicsDevice, vertices, GraphicsResourceUsage.Dynamic);
    int[] indices = [0, 1, 2];
    var indexBuffer = Buffer.Index.New(game.GraphicsDevice, indices);

    var mesh = new Mesh
    {
        Draw = new MeshDraw
        {
            PrimitiveType = PrimitiveType.TriangleList,
            DrawCount = indices.Length,
            IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
            VertexBuffers = [new VertexBufferBinding(vertexBuffer, VertexPositionTexture.Layout, vertexBuffer.ElementCount)],
        }
    };

    var model = new Model { mesh, game.CreateFlatMaterial(Color.BlueViolet) };
    var entity = new Entity("Triangle", new Vector3(2f, 0, 2f)) { new ModelComponent(model) };

    AddVertexLabels(entity, [vertices[0].Position, vertices[1].Position, vertices[2].Position], "t");

    entity.Scene = scene;
}

// One label entity per local-space point, as children so they follow the parent
void AddVertexLabels(Entity parent, ReadOnlySpan<Vector3> localPositions, string labelPrefix = "v")
{
    for (var i = 0; i < localPositions.Length; i++)
    {
        var position = localPositions[i];
        var label = new Entity($"{labelPrefix}{i}")
        {
            new EntityTextComponent
            {
                Text = $"{labelPrefix}{i} ({position.X:0.##}, {position.Y:0.##}, {position.Z:0.##})",
                FontSize = 14,
                Offset = new Vector2(0, -18),
                TextColor = Color.White,
                // Anchor centres the label over its vertex; TextAlignment only arranges the lines of a block
                Anchor = Anchor.BottomCenter,
                EnableBackground = true,
                BackgroundColor = new Color4(0, 0, 0, 0.5f),
                Padding = new Vector2(2)
            }
        };

        // In the parent's local space, like the mesh vertices, so the label follows its transform
        label.Transform.Position = position;
        parent.AddChild(label);
    }
}

public class QuadPrimitiveModel : PrimitiveProceduralModelBase
{
    /// <summary>The size of the quad: a custom property, which shows up in Game Studio.</summary>
    public Vector3 Size { get; set; } = Vector3.One;

    /// <summary>Local-space vertex positions used for the generated quad. Useful for debug labels.</summary>
    public IReadOnlyList<Vector3> Vertices { get; private set; } = [];

    protected override GeometricMeshData<VertexPositionNormalTexture> CreatePrimitiveMeshData()
    {
        // A quad facing +Z
        var normal = Vector3.UnitZ;
        var vertices = new VertexPositionNormalTexture[4];
        vertices[0] = new VertexPositionNormalTexture(new Vector3(-0.5f, 0.5f, 0) * Size, normal, new Vector2(0, 0));
        vertices[1] = new VertexPositionNormalTexture(new Vector3(0.5f, 0.5f, 0) * Size, normal, new Vector2(1, 0));
        vertices[2] = new VertexPositionNormalTexture(new Vector3(-0.5f, -0.5f, 0) * Size, normal, new Vector2(0, 1));
        vertices[3] = new VertexPositionNormalTexture(new Vector3(0.5f, -0.5f, 0) * Size, normal, new Vector2(1, 1));

        // Kept for the labels
        Vertices = [vertices[0].Position, vertices[1].Position, vertices[2].Position, vertices[3].Position];

        int[] indices = [0, 1, 2, 1, 3, 2];

        return new GeometricMeshData<VertexPositionNormalTexture>(vertices, indices, isLeftHanded: false) { Name = "MyModel" };
    }
}
/*
---example-metadata
slug: simple-geometry
title:
  en: Simple Geometry (Labelled Triangle)
level: Intermediate
category: Geometry
complexity: 3
order: 20
description:
  en: |-
    The smallest possible custom mesh - one triangle from three vertices - with each vertex labelled on
    screen so the relationship between the numbers in the code and the shape on screen is visible. The
    labels come from EntityTextComponent, which needs its renderer registered once, and the ground gizmo
    shows which way the axes point. Useful as a scratchpad when a mesh comes out inside out or facing
    the wrong way.
concepts:
  - Building a single triangle from a vertex and index buffer
  - "Choosing a vertex format: VertexPositionTexture and its Layout"
  - Labelling vertices on screen to debug a mesh
  - "Registering the label renderer with AddSceneRenderer(new EntityTextRenderer())"
  - Orienting yourself with a named-axis ground gizmo
  - "Using helpers: SceneSetup3D, UseSkybox, AddGroundGizmo, AddOrientationGizmo, CreateFlatMaterial"
tags:
  - 3D
  - Geometry
  - Mesh
  - Vertex Buffer
  - Debug
  - Labels
  - Gizmo
related:
  - E07_3D_ProceduralGeometry
  - E07_3D_MeshLine
  - E03_3D_EntityText
enabled: true
created: 2025-10-06
---
*/