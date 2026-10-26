using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

/*
  A partial torus built vertex by vertex: a parametric surface over two angles, around the tube and
  along the bend, cut short of a full ring by limiting the bend angle. Shows positions, normals,
  triangle winding and vertex reuse through indices. The open ends show back-face culling at work:
  from some angles the inside is visible and from others the far wall is see-through, since faces
  turned away from the camera are not drawn.
*/

// Torus parameters
const float CylinderRadius = 0.3f;
const float TorusAngle = 270.0f;
const float BendRadius = 1.0f;
const int CircumferenceStepsCount = 20;
const int BendSegmentSteps = 40;

using var game = new Game();

game.Run(new SceneSetup3D().UseBepu().UseSkybox(), start: scene =>
{
    CreateMeshEntity(scene, new Vector3(0, 1, 0), b => BuildPartialTorusMesh(b, CylinderRadius, TorusAngle, BendRadius, CircumferenceStepsCount, BendSegmentSteps));
});

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

void BuildPartialTorusMesh(MeshBuilder meshBuilder, float cylinderRadius, float torusAngle, float bendRadius, int circumferenceStepsCount, int bendSegmentSteps)
{
    // for partial torus up to 360 degrees (torusAngle in degrees)
    meshBuilder.WithIndexType(IndexingType.Int16);
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    var position = meshBuilder.WithPosition<Vector3>();
    var normal = meshBuilder.WithNormal<Vector3>();

    // Vertices: a ring around the tube at every step along the bend
    for (var j = 0; j <= bendSegmentSteps; j++)
    {
        // The bend angle phi starts at 0 on the Z axis and reaches the X axis at 90 degrees
        var phi = j * torusAngle / bendSegmentSteps * Math.PI / 180.0;
        var xc = bendRadius * Math.Sin(phi);
        var zc = bendRadius * Math.Cos(phi);

        for (var i = 0; i < circumferenceStepsCount; i++)
        {
            // The angle theta around the tube
            var theta = i * Math.Tau / circumferenceStepsCount;
            var yr = cylinderRadius * Math.Sin(theta);
            var xr = cylinderRadius * Math.Cos(theta) * Math.Sin(phi);
            var zr = cylinderRadius * Math.Cos(theta) * Math.Cos(phi);

            meshBuilder.AddVertex();
            meshBuilder.SetElement(position, new Vector3((float)(xc + xr), (float)yr, (float)(zc + zr)));
            // A normal has length 1: the direction out from the tube's centre line
            meshBuilder.SetElement(normal, Vector3.Normalize(new Vector3((float)xr, (float)yr, (float)zr)));
        }
    }

    // Triangle indices: two per quad between consecutive rings
    for (var j = 0; j < bendSegmentSteps; j++)
    {
        for (var i = 0; i < circumferenceStepsCount; i++)
        {
            var index = i + j * circumferenceStepsCount;
            var next = (i + 1) % circumferenceStepsCount + j * circumferenceStepsCount;

            meshBuilder.AddIndex(index);
            meshBuilder.AddIndex(next + circumferenceStepsCount);
            meshBuilder.AddIndex(index + circumferenceStepsCount);

            meshBuilder.AddIndex(index);
            meshBuilder.AddIndex(next);
            meshBuilder.AddIndex(next + circumferenceStepsCount);
        }
    }
}
/*
---example-metadata
slug: partial-torus-mesh
title:
  en: Partial Torus Mesh
level: Intermediate
category: Geometry
complexity: 3
order: 40
description:
  en: |-
    A torus defined parametrically from two angles - one around the tube, one around the ring - and cut
    short by limiting the second, which turns the same code into an arc, a horseshoe or a full ring
    without special cases. Normals are computed rather than guessed, because a parametric surface lit
    with wrong normals looks subtly faceted in a way that is hard to trace back.
concepts:
  - Defining a surface from two angle parameters
  - Making the shape partial by constraining the bend angle
  - Computing correct normals for smooth shading
  - Reusing vertices between adjacent triangles through indexing
  - Getting triangle winding right so faces are not culled
  - "Using helpers: SceneSetup3D, UseSkybox, CreateMaterial"
tags:
  - 3D
  - Geometry
  - Mesh
  - MeshBuilder
  - Procedural
  - Torus
  - Normals
related:
  - E07_3D_CylinderMesh
  - E07_3D_PartialTorus_FSharp
  - E07_3D_ProceduralGeometry
enabled: true
created: 2025-03-18
---
*/