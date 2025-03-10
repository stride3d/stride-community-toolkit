using Stride.BepuPhysics;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Gizmos;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

// Constants for the impulse strength and sphere properties
const float ImpulseStrength = 0.5f;
const float SphereRadius = 0.5f;

// Game entities and components
CameraComponent? mainCamera = null;
Entity? sphereEntity = null;
BodyComponent? sphereBody = null;
Buffer? vertexBuffer = null;

// Line vertices to visualize the impulse direction (start and end points)
Vector3[] lineVertices = new Vector3[2];

// Initialize the game instance
using var game = new Game();

// Run the game loop with the Start and Update methods
game.Run(start: Start, update: Update);

// Sets up the initial scene with a skybox, profiler, ground reference, and a sphere with physics
void Start(Scene scene)
{
    // Set up a basic 3D scene with skybox, profiler, and a ground gizmo
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();
    game.AddGroundGizmo(new(-5, 0, -5), showAxisName: true);

    // Create a sphere entity and position it above the ground
    sphereEntity = game.Create3DPrimitive(PrimitiveModelType.Sphere);
    sphereEntity.Transform.Position = new Vector3(0, 8, 0);

    // Retrieve the physics body component attached to the sphere
    sphereBody = sphereEntity.Get<BodyComponent>();

    // Add the sphere entity to the scene, this will make it visible in the game window
    sphereEntity.Scene = scene;

    // Retrieve the active camera from the scene
    mainCamera = scene.GetCamera();

    // Create a line entity used for visualizing the impulse direction
    var lineEntity = CreateLineEntity(game);

    // Attach the line as a child of the sphere so it moves along with it
    sphereEntity.AddChild(lineEntity);
}

// Called every frame to update game logic and process user input
void Update(Scene scene, GameTime time)
{
    if (mainCamera == null) return;

    // Display on-screen instructions for the user
    DisplayInstructions(game);

    // On left mouse button click, process the interaction
    if (game.Input.IsMouseButtonPressed(MouseButton.Left))
    {
        ProcessMouseClick();
    }
}

// Processes mouse click events by casting a ray and determining the appropriate action.
// If the sphere is clicked, its movement is halted; otherwise, an impulse is applied.
void ProcessMouseClick()
{
    // Cast a ray from the camera into the scene based on the mouse position
    var hit = mainCamera.Raycast(game.Input.MousePosition, 100, out var hitInfo);

    if (hit)
    {
        // Ensure the sphere's physics body and entity are valid
        if (sphereBody is null || sphereEntity is null) return;

        Console.WriteLine($"Hit entity: {hitInfo.Collidable.Entity.Name}");

        // If the sphere itself is clicked, stop its movement by zeroing its velocity
        if (hitInfo.Collidable.Entity == sphereEntity)
        {
            sphereBody.LinearVelocity = Vector3.Zero;
            sphereBody.AngularVelocity = Vector3.Zero;

            return;
        }

        // Update the line visualization to point from the sphere to the hit point
        UpdateLineVisualization(hitInfo.Point);

        // Calculate and apply an impulse to the sphere based on the hit point
        ApplyImpulseToSphere(hitInfo.Point);
    }
    else
    {
        Console.WriteLine("No hit");
    }
}

// Updates the endpoint of the line to visualize the hit position in the sphere's local space
void UpdateLineVisualization(Vector3 hitPointWorld)
{
    if (sphereEntity == null || vertexBuffer == null) return;

    // Convert the hit point from world space to the sphere's entity local coordinate space
    var localHitPoint = Vector3.Transform(hitPointWorld, Matrix.Invert(sphereEntity.Transform.WorldMatrix));

    // Update the end vertex of the line's endpoint
    lineVertices[1] = localHitPoint.XYZ();

    // Re-upload the updated vertex data to the GPU
    vertexBuffer.SetData(game.GraphicsContext.CommandList, lineVertices);
}

// Calculates and applies an impulse to the sphere to simulate a physics interaction
void ApplyImpulseToSphere(Vector3 hitPointWorld)
{
    if (sphereEntity == null || sphereBody == null) return;

    // Calculate the direction vector from the sphere's center to the hit point
    var sphereCenter = sphereEntity.Transform.WorldMatrix.TranslationVector;
    var direction = hitPointWorld - sphereCenter;

    // Normalize the direction to ensure a consistent impulse strength regardless of distance
    direction.Normalize();

    // Determine the impulse vector
    var impulse = direction * ImpulseStrength;

    // Calculate an offset from the center so the impulse is applied at the sphere's surface,
    // which helps in inducing a rotational effect
    var offset = direction * SphereRadius;

    // Apply the calculated impulse to the physics body
    sphereBody.ApplyImpulse(impulse, offset);

    // Mark the body as awake to ensure the physics engine processes the change
    sphereBody.Awake = true;
}

// Creates a line entity to visualize the direction of the applied impulse
Entity CreateLineEntity(Game game)
{
    // Initialize the line vertices.
    // The start point is at the origin; the endpoint is set arbitrarily
    lineVertices[0] = Vector3.Zero;
    lineVertices[1] = new(-1, 1, 1);

    // Create a vertex buffer for the line, with start and end points
    vertexBuffer = Buffer.New(game.GraphicsDevice, lineVertices, BufferFlags.VertexBuffer);

    // Create an index buffer defining the line's two endpoints
    var indices = new ushort[] { 0, 1 };
    var indexBuffer = Buffer.New(game.GraphicsDevice, indices, BufferFlags.IndexBuffer);

    // Set up the mesh draw parameters for a line list
    var meshDraw = new MeshDraw
    {
        PrimitiveType = PrimitiveType.LineList,
        VertexBuffers = [new VertexBufferBinding(vertexBuffer, new VertexDeclaration(VertexElement.Position<Vector3>()), lineVertices.Length)],
        IndexBuffer = new IndexBufferBinding(indexBuffer, is32Bit: false, indices.Length),
        DrawCount = indices.Length
    };

    // Create the mesh
    var mesh = new Mesh { Draw = meshDraw };

    // The model is built from the mesh and a gizmo material, an emissive material for clear visualization
    var lineModelComponent = new ModelComponent { Model = new Model { mesh, GizmoEmissiveColorMaterial.Create(game.GraphicsDevice, Color.DarkMagenta) } };

    // Return a new entity that contains the line model component
    return new Entity { lineModelComponent };
}

// Displays on-screen instructions to guide the user
static void DisplayInstructions(Game game)
{
    game.DebugTextSystem.Print("Click the ground to apply a direction impulse", new(5, 30));
    game.DebugTextSystem.Print("Click the sphere to stop moving", new(5, 50));
}