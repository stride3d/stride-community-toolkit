using Stride.CommunityToolkit.Bullet; // Needed for Camera Raycast extension
using Stride.CommunityToolkit.DebugShapes.Code;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Physics;
using System.Runtime.InteropServices; // CollectionsMarshal for span access

namespace Example08_DebugShapes.Scripts;

public class ShapeUpdater : SyncScript
{
    enum CurRenderMode : byte
    {
        All = 0,
        Quad,
        Circle,
        Sphere,
        HalfSphere,
        Cube,
        Capsule,
        Cylinder,
        Cone,
        Ray,
        Arrow,
        None
    }

    const int ChangePerSecond = 8192 + 2048;
    const int InitialNumPrimitives = 1024;
    const int AreaSize = 64;
    const int TextIncrement = 16;
    const int StartTextPositionY = 32;
    const int ShapesInAllMode = 9; // sphere, cube, capsule, cylinder, cone, ray, quad, circle, half-sphere
    const int MinNumberOfPrimitives = 0;
    const int MaxNumberOfPrimitives = 327680;

    [DataMemberIgnore]
    ImmediateDebugRenderSystem DebugDraw; // Provided by services

    int currentNumPrimitives = InitialNumPrimitives;
    CurRenderMode mode = CurRenderMode.All;
    bool useDepthTesting = true;
    bool useWireframe = true;
    bool running = true;

    readonly List<Vector3> primitivePositions = new(InitialNumPrimitives);
    readonly List<Quaternion> primitiveRotations = new(InitialNumPrimitives);
    readonly List<Vector3> primitiveVelocities = new(InitialNumPrimitives);
    readonly List<Vector3> primitiveRotVelocities = new(InitialNumPrimitives);
    readonly List<Color> primitiveColors = new(InitialNumPrimitives);

    public CameraComponent? CurrentCamera;
    private readonly Random random = new();

    // --- Initialization helpers ------------------------------------------------
    static void EnsureSize<T>(List<T> list, int size)
    {
        if (list.Count < size)
        {
            list.Capacity = Math.Max(list.Capacity, size);
            int toAdd = size - list.Count;
            for (int i = 0; i < toAdd; i++) list.Add(default!);
        }
    }

    private static Color ComputeColor(in Vector3 p)
    {
        byte r = (byte)(((p.X / AreaSize) + 1f) * 0.5f * 255.0f);
        byte g = (byte)(((p.Y / AreaSize) + 1f) * 0.5f * 255.0f);
        byte b = (byte)(((p.Z / AreaSize) + 1f) * 0.5f * 255.0f);

        return new Color(r, g, b, 255);
    }

    private void InitializePrimitives(int from, int to)
    {
        EnsureSize(primitivePositions, to);
        EnsureSize(primitiveRotations, to);
        EnsureSize(primitiveVelocities, to);
        EnsureSize(primitiveRotVelocities, to);
        EnsureSize(primitiveColors, to);

        var posSpan = CollectionsMarshal.AsSpan(primitivePositions);
        var rotSpan = CollectionsMarshal.AsSpan(primitiveRotations);
        var velSpan = CollectionsMarshal.AsSpan(primitiveVelocities);
        var rvelSpan = CollectionsMarshal.AsSpan(primitiveRotVelocities);
        var colSpan = CollectionsMarshal.AsSpan(primitiveColors);

        for (int i = from; i < to; ++i)
        {
            var randX = random.Next(-AreaSize, AreaSize);
            var randY = random.Next(-AreaSize, AreaSize);
            var randZ = random.Next(-AreaSize, AreaSize);

            var ballVel = new Vector3(
                (float)(random.NextDouble() * 4.0),
                (float)(random.NextDouble() * 4.0),
                (float)(random.NextDouble() * 4.0));

            var ballRotVel = new Vector3(
                (float)random.NextDouble(),
                (float)random.NextDouble(),
                (float)random.NextDouble());

            posSpan[i] = new Vector3(randX, randY, randZ);
            rotSpan[i] = Quaternion.Identity;
            velSpan[i] = ballVel;
            rvelSpan[i] = ballRotVel;
            colSpan[i] = ComputeColor(posSpan[i]);
        }
    }

    public override void Start()
    {
        CurrentCamera = SceneSystem.SceneInstance.RootScene.Entities.First(e => e.Get<CameraComponent>() != null).Get<CameraComponent>();
        DebugDraw = Services.GetService<ImmediateDebugRenderSystem>();

        DebugDraw.PrimitiveColor = Color.Green;
        ResizeDebugDrawCapacity(currentNumPrimitives);
        DebugDraw.Visible = true;
        DebugText.Visible = true;

        InitializePrimitives(0, currentNumPrimitives);
    }

    // --- Update cycle ----------------------------------------------------------
    public override void Update()
    {
        var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        HandleInput(dt, out int newCount);
        AdjustPrimitiveCount(newCount);
        DrawUiText();
        if (running && currentNumPrimitives > 0) Simulate(dt);
        DrawPrimitives();
        HandleMousePicking();
        DrawAuxiliaryTestGeometry();
    }

    // --- Input & State ---------------------------------------------------------
    private void HandleInput(float dt, out int newCount)
    {
        var speedyDelta = Input.IsKeyDown(Stride.Input.Keys.LeftShift) ? 100.0f : 1.0f;

        newCount = Clamp(currentNumPrimitives + (int)(Input.MouseWheelDelta * ChangePerSecond * speedyDelta * dt), MinNumberOfPrimitives, MaxNumberOfPrimitives);

        if (Input.IsKeyPressed(Stride.Input.Keys.LeftAlt)) mode = (CurRenderMode)(((int)mode + 1) % ((int)CurRenderMode.None + 1));
        if (Input.IsKeyPressed(Stride.Input.Keys.LeftCtrl)) useDepthTesting = !useDepthTesting;
        if (Input.IsKeyPressed(Stride.Input.Keys.Tab)) useWireframe = !useWireframe;
        if (Input.IsKeyPressed(Stride.Input.Keys.Space)) running = !running;
    }

    private void AdjustPrimitiveCount(int newAmount)
    {
        if (newAmount > currentNumPrimitives)
        {
            InitializePrimitives(currentNumPrimitives, newAmount);
            ResizeDebugDrawCapacity(newAmount);
        }

        currentNumPrimitives = newAmount;
    }

    private void ResizeDebugDrawCapacity(int primitives)
    {
        DebugDraw.MaxPrimitivesWithLifetime = (primitives * 2) + 8;
        DebugDraw.MaxPrimitives = (primitives * 2) + 8;
    }

    private void Simulate(float dt)
    {
        // NOTE: Cannot cache spans outside lambda when using ref; reacquire inside each iteration.
        Dispatcher.For(0, currentNumPrimitives, i =>
        {
            var posSpan = CollectionsMarshal.AsSpan(primitivePositions);
            var velSpan = CollectionsMarshal.AsSpan(primitiveVelocities);
            var rvelSpan = CollectionsMarshal.AsSpan(primitiveRotVelocities);
            var rotSpan = CollectionsMarshal.AsSpan(primitiveRotations);
            var colSpan = CollectionsMarshal.AsSpan(primitiveColors);

            ref var position = ref posSpan[i];
            ref var velocity = ref velSpan[i];
            ref var rotVelocity = ref rvelSpan[i];
            ref var rotation = ref rotSpan[i];
            ref var color = ref colSpan[i];

            if (position.X > AreaSize || position.X < -AreaSize) velocity.X = -velocity.X;
            if (position.Y > AreaSize || position.Y < -AreaSize) velocity.Y = -velocity.Y;
            if (position.Z > AreaSize || position.Z < -AreaSize) velocity.Z = -velocity.Z;

            position += velocity * dt;

            rotation *=
                Quaternion.RotationX(rotVelocity.X * dt) *
                Quaternion.RotationY(rotVelocity.Y * dt) *
                Quaternion.RotationZ(rotVelocity.Z * dt);

            color = ComputeColor(position);
        });
    }

    // --- Drawing ---------------------------------------------------------------
    private void DrawPrimitives()
    {
        var posSpan = CollectionsMarshal.AsSpan(primitivePositions);
        var rotSpan = CollectionsMarshal.AsSpan(primitiveRotations);
        var velSpan = CollectionsMarshal.AsSpan(primitiveVelocities);
        var rvelSpan = CollectionsMarshal.AsSpan(primitiveRotVelocities);
        var colSpan = CollectionsMarshal.AsSpan(primitiveColors);

        int currentShape = 0;
        for (int i = 0; i < currentNumPrimitives; ++i)
        {
            ref readonly var position = ref posSpan[i];
            ref readonly var rotation = ref rotSpan[i];
            ref readonly var velocity = ref velSpan[i];
            ref readonly var rotVelocity = ref rvelSpan[i];
            ref readonly var color = ref colSpan[i];

            switch (mode)
            {
                case CurRenderMode.All:
                    switch (currentShape++)
                    {
                        case 0: DebugDraw.DrawSphere(position, 0.5f, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                        case 1: DebugDraw.DrawCube(position, new Vector3(1, 1, 1), rotation, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                        case 2: DebugDraw.DrawCapsule(position, 2.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                        case 3: DebugDraw.DrawCylinder(position, 2.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                        case 4: DebugDraw.DrawCone(position, 1.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                        case 5: DebugDraw.DrawRay(position, velocity, color, depthTest: useDepthTesting); break;
                        case 6: DebugDraw.DrawQuad(position, new Vector2(1.0f), rotation, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                        case 7: DebugDraw.DrawCircle(position, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                        case 8: DebugDraw.DrawHalfSphere(position, 0.5f, color, rotation, depthTest: useDepthTesting, solid: !useWireframe); currentShape = 0; break;
                    }
                    break;
                case CurRenderMode.Quad: DebugDraw.DrawQuad(position, new Vector2(1.0f), rotation, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                case CurRenderMode.Circle: DebugDraw.DrawCircle(position, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                case CurRenderMode.Sphere: DebugDraw.DrawSphere(position, 0.5f, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                case CurRenderMode.HalfSphere: DebugDraw.DrawHalfSphere(position, 0.5f, color, rotation, depthTest: useDepthTesting, solid: !useWireframe); break;
                case CurRenderMode.Cube: DebugDraw.DrawCube(position, new Vector3(1, 1, 1), rotation, color, depthTest: useDepthTesting, solid: !useWireframe && i % 2 == 0); break;
                case CurRenderMode.Capsule: DebugDraw.DrawCapsule(position, 2.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                case CurRenderMode.Cylinder: DebugDraw.DrawCylinder(position, 2.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                case CurRenderMode.Cone: DebugDraw.DrawCone(position, 1.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe); break;
                case CurRenderMode.Ray: DebugDraw.DrawRay(position, velocity, color, depthTest: useDepthTesting); break;
                case CurRenderMode.Arrow: DebugDraw.DrawArrow(position, position + velocity, color: color, depthTest: useDepthTesting, solid: !useWireframe); break;
                case CurRenderMode.None: break;
            }
        }
    }

    private void DrawUiText()
    {
        int textPositionX = (int)Input.Mouse.SurfaceSize.X - 384;
        DebugText.Print($"Primitive Count: {currentNumPrimitives} (scroll wheel to adjust)", new Int2(textPositionX, StartTextPositionY));
        DebugText.Print(" - Hold shift: faster count adjustment", new Int2(textPositionX, StartTextPositionY + TextIncrement));
        DebugText.Print($" - Render Mode: {mode} (left alt to switch)", new Int2(textPositionX, StartTextPositionY + (TextIncrement * 2)));
        DebugText.Print($" - Depth Testing: {(useDepthTesting ? "On " : "Off")} (left ctrl to toggle)", new Int2(textPositionX, StartTextPositionY + (TextIncrement * 3)));
        DebugText.Print($" - Fill mode: {(useWireframe ? "Wireframe" : "Solid")} (tab to toggle)", new Int2(textPositionX, StartTextPositionY + (TextIncrement * 4)));
        DebugText.Print($" - State: {(running ? "Simulating" : "Paused")} (space to toggle)", new Int2(textPositionX, StartTextPositionY + (TextIncrement * 5)));
    }

    // --- Interaction -----------------------------------------------------------
    private void HandleMousePicking()
    {
        if (!Input.IsMouseButtonPressed(Stride.Input.MouseButton.Left) || CurrentCamera is null)
            return;

        var clickPos = Input.MousePosition;
        var result = CurrentCamera.Raycast(this.GetSimulation(), clickPos);

        if (!result.Succeeded) return;

        var cameraWorldPos = CurrentCamera.Entity.Transform.WorldMatrix.TranslationVector;
        var cameraWorldUp = CurrentCamera.Entity.Transform.WorldMatrix.Up;
        var cameraWorldNormal = Vector3.Normalize(result.Point - cameraWorldPos);

        DebugDraw.DrawLine(cameraWorldPos + (cameraWorldNormal * -2.0f) + (cameraWorldUp * (-0.125f / 4.0f)), result.Point, color: Color.HotPink, duration: 5.0f);
        DebugDraw.DrawArrow(result.Point, result.Point + result.Normal, color: Color.HotPink, duration: 5.0f);
        DebugDraw.DrawArrow(result.Point, result.Point + Vector3.Reflect(result.Point - cameraWorldPos, result.Normal), color: Color.LimeGreen, duration: 5.0f);
    }

    // --- Extra debug geometry --------------------------------------------------
    private void DrawAuxiliaryTestGeometry()
    {
        DebugDraw.DrawCube(new Vector3(0, 0, 0), new Vector3(1.0f, 1.0f, 1.0f), color: Color.White);
        DebugDraw.DrawBounds(new Vector3(-5, 0, -5), new Vector3(5, 5, 5), color: Color.White);
        DebugDraw.DrawBounds(new Vector3(-AreaSize), new Vector3(AreaSize), color: Color.HotPink);

        // Line of cubes for depth test visualization
        var startPos = new Vector3(0, 5, 0);
        for (int i = -8; i < 8; ++i)
        {
            var p = startPos + new Vector3(i, 0, 0);
            DebugDraw.DrawCube(p, Vector3.One, depthTest: useDepthTesting, solid: true);
            DebugDraw.DrawCube(p, Vector3.One, depthTest: useDepthTesting, solid: false, color: Color.White);
        }

        // Show every primitive type in a row
        var testPos = new Vector3(0.0f, 0.0f, -5.0f);
        DebugDraw.PrimitiveColor = Color.Red;
        DebugDraw.DrawQuad(testPos + new Vector3(1.0f, 0.0f, 0.0f), new Vector2(1.0f), solid: !useWireframe);
        DebugDraw.DrawCircle(testPos + new Vector3(2.0f, 0.0f, 0.0f), 0.5f, solid: !useWireframe);
        DebugDraw.DrawSphere(testPos + new Vector3(3.0f, 0.0f, 0.0f), 0.5f, solid: !useWireframe);
        DebugDraw.DrawCube(testPos + new Vector3(4.0f, 0.0f, 0.0f), new Vector3(1.0f), solid: !useWireframe);
        DebugDraw.DrawCapsule(testPos + new Vector3(5.0f, 0.0f, 0.0f), 1.0f, 0.5f, solid: !useWireframe);
        DebugDraw.DrawCylinder(testPos + new Vector3(6.0f, 0.0f, 0.0f), 1.0f, 0.5f, solid: !useWireframe);
        DebugDraw.DrawCone(testPos + new Vector3(7.0f, 0.0f, 0.0f), 1.0f, 0.5f, solid: !useWireframe);
        DebugDraw.DrawHalfSphere(testPos + new Vector3(8.0f, 0.0f, 0.0f), 0.5f, solid: !useWireframe);
        DebugDraw.DrawHalfSphere(testPos + new Vector3(9.0f, 0.0f, 0.0f), 0.5f, rotation: Quaternion.RotationX((float)Math.PI), solid: !useWireframe);
        DebugDraw.DrawCone(new Vector3(0, 0.5f, 0), 2.0f, 0.5f, color: Color.HotPink);
    }

    // --- Utility ----------------------------------------------------------------
    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
}