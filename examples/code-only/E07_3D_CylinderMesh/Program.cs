using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

/*
  A cylinder built vertex by vertex with MeshBuilder: a ring of vertices at each end, the side wall as
  two triangles per segment, and a cap on each end around a centre vertex. Shows vertex positions,
  normals, triangle winding and vertex reuse through indices.
*/

const int Segments = 1000;
const float Radius = 0.5f;
const float Length = 8;

using var game = new Game();

game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: scene => CreateMeshEntity(scene, new(0, 1, -Length / 2), b => BuildCylinderMesh(b, Segments, Radius, Length)));

Entity CreateMeshEntity(Scene scene, Vector3 position, Action<MeshBuilder> build)
{
    using var meshBuilder = new MeshBuilder();

    build(meshBuilder);

    var model = new Model
    {
        new Mesh { Draw = meshBuilder.ToMeshDraw(game.GraphicsDevice) },
        game.CreateMaterial(new Color(255, 77, 128), metalness: 0f, glossiness: 0.9f),
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

    CreateRingVertices(meshBuilder, segments, radius, position, normal, 0);
    CreateRingVertices(meshBuilder, segments, radius, position, normal, length);
    CreateSideWalls(meshBuilder, segments);
    CreateEndCap(meshBuilder, segments, radius, position, normal, 0, facesPositiveZ: false);
    CreateEndCap(meshBuilder, segments, radius, position, normal, length, facesPositiveZ: true);
}

static void CreateRingVertices(MeshBuilder meshBuilder, int segments, float radius, int position, int normal, float z)
{
    for (var i = 0; i < segments; i++)
    {
        var x = radius * (float)Math.Sin(Math.Tau / segments * i);
        var y = radius * (float)Math.Cos(Math.Tau / segments * i);

        meshBuilder.AddVertex();
        meshBuilder.SetElement(position, new Vector3(x, y, z));
        // Unit length: the direction out from the axis, without the radius
        meshBuilder.SetElement(normal, new Vector3(x, y, 0) / radius);
    }
}

// Two triangles per segment between the two rings
static void CreateSideWalls(MeshBuilder meshBuilder, int segments)
{
    for (var i = 0; i < segments; i++)
    {
        var next = (i + 1) % segments;

        meshBuilder.AddIndex(i);
        meshBuilder.AddIndex(next + segments);
        meshBuilder.AddIndex(i + segments);

        meshBuilder.AddIndex(i);
        meshBuilder.AddIndex(next);
        meshBuilder.AddIndex(next + segments);
    }
}

// A fan from a centre vertex to a ring of the cap's own. The side ring at the same height cannot be reused:
// its normals point out from the axis, which would shade the flat cap as a dome, so the cap gets a second
// ring at the same positions with normals along the axis. The two caps wind opposite ways so each faces out
static void CreateEndCap(MeshBuilder meshBuilder, int segments, float radius, int position, int normal, float z, bool facesPositiveZ)
{
    var axis = facesPositiveZ ? Vector3.UnitZ : -Vector3.UnitZ;

    var centre = meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(0, 0, z));
    meshBuilder.SetElement(normal, axis);

    var ring = meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(0, radius, z));
    meshBuilder.SetElement(normal, axis);

    for (var i = 1; i < segments; i++)
    {
        var x = radius * (float)Math.Sin(Math.Tau / segments * i);
        var y = radius * (float)Math.Cos(Math.Tau / segments * i);

        meshBuilder.AddVertex();
        meshBuilder.SetElement(position, new Vector3(x, y, z));
        meshBuilder.SetElement(normal, axis);
    }

    for (var i = 0; i < segments; i++)
    {
        meshBuilder.AddIndex(ring + (i + 1) % segments);
        meshBuilder.AddIndex(facesPositiveZ ? centre : ring + i);
        meshBuilder.AddIndex(facesPositiveZ ? ring + i : centre);
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
  - Closing the ends with fan-triangulated caps on rings of their own, so their normals are flat
  - Why the two caps must wind in opposite directions
  - Keeping index arithmetic readable by splitting the build into stages
  - "Using helpers: SceneSetup3D, UseSkybox, CreateMaterial"
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