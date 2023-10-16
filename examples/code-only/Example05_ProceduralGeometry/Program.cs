using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Utilities;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

using var game = new Game();

Entity? circleEntity = null;
game.Run(start: Start, update: Update);


void Start(Scene rootScene)
{
    game.SetupBase3DScene();

    AddMesh(game.GraphicsDevice, rootScene, Vector3.Zero, GiveMeATriangle);
    AddMesh(game.GraphicsDevice, rootScene, Vector3.UnitX * 2, GiveMeAPlane);
}
void Update(Scene rootScene, GameTime gameTime)
{
    var segments = (int)((Math.Cos(gameTime.Total.TotalMilliseconds / 500) + 1) / 2 * 47) + 3;
    circleEntity?.DestroyEntity();
    circleEntity = AddMesh(game.GraphicsDevice, rootScene, Vector3.UnitX * -2, b => GiveMeACircle(b, segments));
}

void GiveMeATriangle(MeshBuilder meshBuilder)
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

void GiveMeAPlane(MeshBuilder meshBuilder)
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

void GiveMeACircle(MeshBuilder meshBuilder, int segments)
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

Entity AddMesh(GraphicsDevice graphicsDevice, Scene rootScene, Vector3 position, Action<MeshBuilder> build)
{
    using var meshBuilder = new MeshBuilder();
    build(meshBuilder);

    var entity = new Entity { Scene = rootScene, Transform = { Position = position } };
    var model = new Model
    {
        new MaterialInstance {
            Material = Material.New(graphicsDevice, new MaterialDescriptor {
                Attributes = new MaterialAttributes {
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Diffuse = new MaterialDiffuseMapFeature {
                        DiffuseMap = new ComputeVertexStreamColor()
                    },
                }
            })
        },
        new Mesh {
            Draw = meshBuilder.ToMeshDraw(graphicsDevice),
            MaterialIndex = 0
        }
    };
    entity.Add(new ModelComponent { Model = model });
    return entity;
}