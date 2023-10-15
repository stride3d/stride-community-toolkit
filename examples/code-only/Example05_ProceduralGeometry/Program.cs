using Stride.CommunityToolkit.Extensions;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Utilities;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;

using var game = new Game();

game.Run(start: Start);

void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    AddMesh(game.GraphicsDevice, rootScene, Vector3.Zero, GiveMeATriangle);
    AddMesh(game.GraphicsDevice, rootScene, Vector3.UnitX * 2, GiveMeAPlane);
    AddMesh(game.GraphicsDevice, rootScene, Vector3.UnitX * -2, GiveMeACircle);
}

void GiveMeATriangle(MeshBuilder meshBuilder)
{
    meshBuilder.WithIndexType(IndexingType.Int32);
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    var position = meshBuilder.WithPosition<Vector3>();

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(0, 0, 0));

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(1, 0, 0));

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(.5f, 1, 0));

    meshBuilder.AddIndex(0);
    meshBuilder.AddIndex(2);
    meshBuilder.AddIndex(1);
}

void GiveMeAPlane(MeshBuilder meshBuilder)
{
    meshBuilder.WithIndexType(IndexingType.Int32);
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    var position = meshBuilder.WithPosition<Vector3>();

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(0, 0, 0));

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(0, 1, 0));

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(1, 1, 0));

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(1, 0, 0));

    meshBuilder.AddIndex(0);
    meshBuilder.AddIndex(1);
    meshBuilder.AddIndex(2);

    meshBuilder.AddIndex(0);
    meshBuilder.AddIndex(2);
    meshBuilder.AddIndex(3);
}

void GiveMeACircle(MeshBuilder meshBuilder)
{
    meshBuilder.WithIndexType(IndexingType.Int32);
    meshBuilder.WithPrimitiveType(PrimitiveType.TriangleList);

    var position = meshBuilder.WithPosition<Vector3>();

    const int segments = 48;
    for (var i = 0; i < segments; i++)
    {
        var x = (float)Math.Sin(Math.Tau / segments * i) / 2;
        var y = (float)Math.Cos(Math.Tau / segments * i) / 2;

        meshBuilder.AddVertex();
        meshBuilder.SetElement(position, new Vector3(x + .5f, y + .5f, 0));
    }

    meshBuilder.AddVertex();
    meshBuilder.SetElement(position, new Vector3(.5f, .5f, 0));

    for (var i = 0; i < segments; i++)
    {
        meshBuilder.AddIndex(segments);
        meshBuilder.AddIndex(i);
        meshBuilder.AddIndex((i + 1) % segments);
    }
}

void AddMesh(GraphicsDevice graphicsDevice, Scene rootScene, Vector3 position, Action<MeshBuilder> build)
{
    using var meshBuilder = new MeshBuilder();
    build(meshBuilder);

    var entity = new Entity { Scene = rootScene, Transform = { Position = position }};
    var model = new Model
    {
        new Mesh {
            Draw = meshBuilder.ToMeshDraw(graphicsDevice),
            MaterialIndex = 0
        }
    };
    entity.Add(new ModelComponent { Model = model });
}