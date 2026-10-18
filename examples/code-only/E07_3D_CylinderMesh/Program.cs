using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

/*
  A cylinder built vertex by vertex with MeshBuilder: a ring of vertices at each end, the side wall as
  two triangles per segment, and a cap on each end around a centre vertex. Shows vertex positions,
  normals, triangle winding and vertex reuse through indices.
*/

const int Segments = 1000;
const float Radius = 0.5f;
const float Length = 8;

using var game = new Game();

game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: (Scene rootScene) =>
{
    CreateMeshEntity(game.GraphicsDevice, rootScene, new(0, 1, -Length / 2), b => BuildCylinderMesh(b, Segments, Radius, Length));
});

Entity CreateMeshEntity(GraphicsDevice graphicsDevice, Scene scene, Vector3 position, Action<MeshBuilder> build)
{
    using var meshBuilder = new MeshBuilder();

    build(meshBuilder);

    var model = new Model
    {
        new MaterialInstance { Material = CreateMaterial(game) },
        new Mesh {
            Draw = meshBuilder.ToMeshDraw(graphicsDevice),
            MaterialIndex = 0
        }
    };

    var entity = new Entity { Scene = scene, Transform = { Position = position } };

    entity.Add(new ModelComponent { Model = model });

    return entity;
}

void BuildCylinderMesh(MeshBuilder meshBuilder, int segments, float radius, float length)
{
    meshBuilder.WithIndexType(IndexingType.Int16);
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    var position = meshBuilder.WithPosition<Vector3>();
    var normal = meshBuilder.WithNormal<Vector3>();

    // Create the vertices for both circular ends
    // First end (z = 0)
    CreateRingVertices(meshBuilder, segments, radius, position, normal, 0);
    // Second end (z = length)
    CreateRingVertices(meshBuilder, segments, radius, position, normal, length);

    CreateCylinderSideWalls(meshBuilder, segments);

    CreateCircularEndCap(meshBuilder, segments, position, normal, 0, false);

    CreateCircularEndCap(meshBuilder, segments, position, normal, length, true);
}

static Material CreateMaterial(Game game) => Material.New(game.GraphicsDevice, content: game.Content, descriptor: new()
{
    Attributes = new()
    {
        MicroSurface = new MaterialGlossinessMapFeature
        {
            GlossinessMap = new ComputeFloat(0.9f)
        },
        Diffuse = new MaterialDiffuseMapFeature
        {
            DiffuseMap = new ComputeColor(new Color4(1, 0.3f, 0.5f, 1))
        },
        DiffuseModel = new MaterialDiffuseLambertModelFeature(),
        Specular = new MaterialMetalnessMapFeature
        {
            MetalnessMap = new ComputeFloat(0.0f)
        },
        SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
    }
});

static void CreateRingVertices(MeshBuilder meshBuilder, int segments, float radius, int position, int normal, float zPosition)
{
    for (var i = 0; i < segments; i++)
    {
        var x = radius * (float)Math.Sin(Math.Tau / segments * i);
        var y = radius * (float)Math.Cos(Math.Tau / segments * i);

        meshBuilder.AddVertex();
        meshBuilder.SetElement(position, new Vector3(x, y, zPosition));
        // A normal has length 1: the direction out from the axis, without the radius
        meshBuilder.SetElement(normal, new Vector3(x, y, 0) / radius);
    }
}

// Creates the triangles that form the curved side wall of the cylinder
static void CreateCylinderSideWalls(MeshBuilder meshBuilder, int segments)
{
    for (var i = 0; i < segments; i++)
    {
        var i_next = (i + 1) % segments;

        // First triangle of the quad
        meshBuilder.AddIndex(i);
        meshBuilder.AddIndex(i_next + segments);
        meshBuilder.AddIndex(i + segments);

        // Second triangle of the quad
        meshBuilder.AddIndex(i);
        meshBuilder.AddIndex(i_next);
        meshBuilder.AddIndex(i_next + segments);
    }
}

// Creates a circular cap to close one end of the cylinder
static void CreateCircularEndCap(MeshBuilder meshBuilder, int segments, int position, int normal, float zPosition, bool isEndFacingPositiveZ)
{
    int centerVertexIndex = meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(0f, 0f, zPosition));

    // The normal points out of the end
    var normalDirection = isEndFacingPositiveZ ? new Vector3(0, 0, 1) : new Vector3(0, 0, -1);
    meshBuilder.SetElement(normal, normalDirection);

    // First end cap has vertices from 0 to segments-1
    // Second end cap has vertices from segments to 2*segments-1
    int vertexOffset = isEndFacingPositiveZ ? segments : 0;

    for (var i = 0; i < segments; i++)
    {
        if (isEndFacingPositiveZ)
        {
            // For end cap with Z+ normal
            meshBuilder.AddIndex(((i + 1) % segments) + vertexOffset);
            meshBuilder.AddIndex(centerVertexIndex);
            meshBuilder.AddIndex(i + vertexOffset);
        }
        else
        {
            // For end cap with Z- normal
            meshBuilder.AddIndex(((i + 1) % segments) + vertexOffset);
            meshBuilder.AddIndex(i + vertexOffset);
            meshBuilder.AddIndex(centerVertexIndex);
        }
    }
}
/*
---example-metadata
slug: cylinder-mesh
title:
  en: Cylinder Mesh
level: Intermediate
category: Geometry
complexity: 3
order: 30
description:
  en: |-
    A cylinder generated with MeshBuilder, split into the three jobs any surface of revolution needs:
    place a ring of vertices, join consecutive rings into side walls, then close both ends with caps.
    Separating them is what keeps the index arithmetic followable - the caps in particular wind the
    opposite way to each other, which is easy to get wrong and instantly visible when you do.
concepts:
  - Generating a ring of vertices from an angle sweep
  - Stitching consecutive rings into a quad strip of triangles
  - Closing the ends with fan-triangulated caps
  - Why the two caps must wind in opposite directions
  - Keeping index arithmetic readable by splitting the build into stages
  - "Using helpers: SceneSetup3D, UseSkybox"
tags:
  - 3D
  - Geometry
  - Mesh
  - MeshBuilder
  - Procedural
  - Cylinder
  - Winding
related:
  - E07_3D_PartialTorus
  - E07_3D_ProceduralGeometry
enabled: true
created: 2025-03-15
---
*/