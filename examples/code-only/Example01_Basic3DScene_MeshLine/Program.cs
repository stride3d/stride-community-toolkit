using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
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
};

static Entity CreateSphereEntity(Game game)
    => game.Create3DPrimitive(PrimitiveModelType.Sphere);

static Entity CreateLineEntity(Game game)
{
    // Create vertex buffer with start and end points
    var vertices = new Vector3[] { new(0, 0, 0), new(1, 1, -1) };
    var vertexBuffer = Buffer.New(game.GraphicsDevice, vertices, BufferFlags.VertexBuffer);

    // Create index buffer
    var indices = new short[] { 0, 1 };
    var indexBuffer = Buffer.New(game.GraphicsDevice, indices, BufferFlags.IndexBuffer);

    var meshDraw = new MeshDraw
    {
        PrimitiveType = PrimitiveType.LineList,
        VertexBuffers = [new VertexBufferBinding(vertexBuffer, new VertexDeclaration(VertexElement.Position<Vector3>()), vertices.Length)],
        IndexBuffer = new IndexBufferBinding(indexBuffer, is32Bit: false, indices.Length),
        DrawCount = indices.Length
    };

    var mesh = new Mesh { Draw = meshDraw };
    var model = new Model { mesh, game.CreateMaterial(Color.Blue) };

    return new Entity { new ModelComponent(model) };
}