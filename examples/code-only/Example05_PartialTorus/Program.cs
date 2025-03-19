using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

/*
  Procedural Partial Torus Mesh Generation

  This example demonstrates creating a 3D partial torus mesh programmatically by breaking down
  the process into clear, distinct steps:

  1. Setting up a basic 3D scene with a skybox
  2. Creating a parametrically defined torus geometry using mathematical formulas
  3. Demonstrating how to generate a partial (incomplete) torus by constraining the bend angle
  4. Building structured 3D mesh generation with proper vertex positioning and normal definitions

  The example showcases important 3D graphics concepts including:
  - Parametric surface generation using two angle parameters (circumference and bend)
  - Correct normal calculation for accurate lighting and shading
  - Triangle winding order for proper face orientation
  - Vertex indexing to efficiently reuse vertices between adjacent triangles

  The partial nature of the torus also highlights the effect of back-face culling. When viewing
  the open ends of the torus from certain angles, you can see through to the inside or outside,
  as surfaces facing away from the camera are not rendered. This is normal behavior and improves
  performance by avoiding the drawing of surfaces that wouldn't be visible in a complete object.
*/

// Torus parameters
const float CylinderRadius = 0.3f;
const float TorusAngle = 270.0f;
const float BendRadius = 1.0f;
const int CircumferenceStepsCount = 20;
const int BendSegmentSteps = 40;

using var game = new Game();

game.Run(start: (Scene rootScene) =>
{
    game.SetupBase3DScene();
    game.AddSkybox();

    CreateMeshEntity(
        game.GraphicsDevice,
        rootScene,
        new Vector3(0.0f, 1.0f, 0.0f),
        b => BuildPartialTorusMesh(b, CylinderRadius, TorusAngle, BendRadius, CircumferenceStepsCount, BendSegmentSteps)
    );
});

Entity CreateMeshEntity(GraphicsDevice graphicsDevice, Scene scene, Vector3 position, Action<MeshBuilder> build)
{
    using var meshBuilder = new MeshBuilder();

    build(meshBuilder);

    var model = new Model
    {
        new MaterialInstance { Material = CreateMaterial(game) },
        new Mesh
        {
            Draw = meshBuilder.ToMeshDraw(graphicsDevice),
            MaterialIndex = 0
        }
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

    // vertices
    for (int j = 0; j <= bendSegmentSteps; j++)
    {
        // Torus angle position Phi starts at 0 in line with Z-axis and increases to Pi/2 at X-axis
        double tPhi = j * torusAngle / bendSegmentSteps * Math.PI / 180.0;
        double xc = bendRadius * Math.Sin(tPhi);
        double zc = bendRadius * Math.Cos(tPhi);

        for (int i = 0; i < circumferenceStepsCount; i++)
        {
            // Circumference angle tTheta
            double tTheta = i * Math.Tau / circumferenceStepsCount;
            double yr = cylinderRadius * Math.Sin(tTheta);
            double xr = cylinderRadius * Math.Cos(tTheta) * Math.Sin(tPhi);
            double zr = cylinderRadius * Math.Cos(tTheta) * Math.Cos(tPhi);

            var tNorm = new Vector3((float)xr, (float)yr, (float)zr);
            var tPos = new Vector3((float)(xc + xr), (float)yr, (float)(zc + zr));

            meshBuilder.AddVertex();
            meshBuilder.SetElement(position, tPos);
            meshBuilder.SetElement(normal, tNorm);
        }
    }

    // Triangle indices
    for (int j = 0; j < bendSegmentSteps; j++)
    {
        for (int i = 0; i < circumferenceStepsCount; i++)
        {
            int i_tot = i + j * circumferenceStepsCount;
            int i_next = (i + 1) % circumferenceStepsCount + j * circumferenceStepsCount;

            // Triangle 1
            meshBuilder.AddIndex(i_tot);
            meshBuilder.AddIndex(i_next + circumferenceStepsCount);
            meshBuilder.AddIndex(i_tot + circumferenceStepsCount);

            // Triangle 2
            meshBuilder.AddIndex(i_tot);
            meshBuilder.AddIndex(i_next);
            meshBuilder.AddIndex(i_next + circumferenceStepsCount);
        }
    }
}

static Material CreateMaterial(Game game)
{
    return Material.New(game.GraphicsDevice, new MaterialDescriptor
    {
        Attributes = new MaterialAttributes
        {
            MicroSurface = new MaterialGlossinessMapFeature
            {
                GlossinessMap = new ComputeFloat(0.9f)
            },
            Diffuse = new MaterialDiffuseMapFeature
            {
                DiffuseMap = new ComputeColor(new Color4(1.0f, 0.3f, 0.5f, 1.0f))
            },
            DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            Specular = new MaterialMetalnessMapFeature
            {
                MetalnessMap = new ComputeFloat(0.0f)
            },
            SpecularModel = new MaterialSpecularMicrofacetModelFeature
            {
                Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial()
            }
        }
    });
}