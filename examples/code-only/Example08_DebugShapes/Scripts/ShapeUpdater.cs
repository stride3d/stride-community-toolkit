using Stride.CommunityToolkit.Bullet;
using Stride.CommunityToolkit.DebugShapes.Code;
using Stride.Core;
using System.Runtime.InteropServices; // CollectionsMarshal for span access
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Physics;

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

    [DataMemberIgnore]
    ImmediateDebugRenderSystem DebugDraw; // this is here to make it look like it should when properly integrated

    int minNumberOfPrimitives = 0;
    int maxNumberOfPrimitives = 327680;
    int currentNumPrimitives = InitialNumPrimitives;
    CurRenderMode mode = CurRenderMode.All;
    bool useDepthTesting = true;
    bool useWireframe = true;
    bool running = true;

    // Replaced FastList<T> with List<T> + span access via CollectionsMarshal.AsSpan
    readonly List<Vector3> primitivePositions = new(InitialNumPrimitives);
    readonly List<Quaternion> primitiveRotations = new(InitialNumPrimitives);
    readonly List<Vector3> primitiveVelocities = new(InitialNumPrimitives);
    readonly List<Vector3> primitiveRotVelocities = new(InitialNumPrimitives);
    readonly List<Color> primitiveColors = new(InitialNumPrimitives);

    public CameraComponent? CurrentCamera;

    static void EnsureSize<T>(List<T> list, int size)
    {
        if (list.Count < size)
        {
            list.Capacity = Math.Max(list.Capacity, size);
            int toAdd = size - list.Count;
            for (int i = 0; i < toAdd; i++) list.Add(default!);
        }
        // When size shrinks we intentionally do NOT remove tail elements to avoid allocations; logic already tracks currentNumPrimitives.
    }

    private void InitializePrimitives(int from, int to)
    {
        var random = new Random();

        EnsureSize(primitivePositions, to);
        EnsureSize(primitiveRotations, to);
        EnsureSize(primitiveVelocities, to);
        EnsureSize(primitiveRotVelocities, to);
        EnsureSize(primitiveColors, to);

        // Use spans for fast ref access
        var posSpan = CollectionsMarshal.AsSpan(primitivePositions);
        var rotSpan = CollectionsMarshal.AsSpan(primitiveRotations);
        var velSpan = CollectionsMarshal.AsSpan(primitiveVelocities);
        var rvelSpan = CollectionsMarshal.AsSpan(primitiveRotVelocities);
        var colSpan = CollectionsMarshal.AsSpan(primitiveColors);

        for (int i = from; i < to; ++i)
        {
            // initialize boxes

            var randX = random.Next(-AreaSize, AreaSize);
            var randY = random.Next(-AreaSize, AreaSize);
            var randZ = random.Next(-AreaSize, AreaSize);

            var velX = (float)(random.NextDouble() * 4.0);
            var velY = (float)(random.NextDouble() * 4.0);
            var velZ = (float)(random.NextDouble() * 4.0);
            var ballVel = new Vector3(velX, velY, velZ);

            var rotVelX = (float)random.NextDouble();
            var rotVelY = (float)random.NextDouble();
            var rotVelZ = (float)random.NextDouble();
            var ballRotVel = new Vector3(rotVelX, rotVelY, rotVelZ);

            posSpan[i] = new Vector3(randX, randY, randZ);
            rotSpan[i] = Quaternion.Identity;
            velSpan[i] = ballVel;
            rvelSpan[i] = ballRotVel;

            ref var color = ref colSpan[i];
            ref readonly var p = ref posSpan[i];
            color.R = (byte)(((p.X / AreaSize) + 1f) / 2.0f * 255.0f);
            color.G = (byte)(((p.Y / AreaSize) + 1f) / 2.0f * 255.0f);
            color.B = (byte)(((p.Z / AreaSize) + 1f) / 2.0f * 255.0f);
            color.A = 255;
        }
    }

    public override void Start()
    {
        CurrentCamera = SceneSystem.SceneInstance.RootScene.Entities.First(e => e.Get<CameraComponent>() != null).Get<CameraComponent>();

        DebugDraw = Services.GetService<ImmediateDebugRenderSystem>();

        DebugDraw.PrimitiveColor = Color.Green;
        DebugDraw.MaxPrimitives = (currentNumPrimitives * 2) + 8;
        DebugDraw.MaxPrimitivesWithLifetime = (currentNumPrimitives * 2) + 8;
        DebugDraw.Visible = true;
        DebugText.Visible = true;

        InitializePrimitives(0, currentNumPrimitives);
    }

    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

    public override void Update()
    {
        var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;

        var speedyDelta = (Input.IsKeyDown(Stride.Input.Keys.LeftShift)) ? 100.0f : 1.0f;
        var newAmountOfBoxes = Clamp(currentNumPrimitives + (int)(Input.MouseWheelDelta * ChangePerSecond * speedyDelta * dt), minNumberOfPrimitives, maxNumberOfPrimitives);

        if (Input.IsKeyPressed(Stride.Input.Keys.LeftAlt)) mode = (CurRenderMode)(((int)mode + 1) % ((int)CurRenderMode.None + 1));
        if (Input.IsKeyPressed(Stride.Input.Keys.LeftCtrl)) useDepthTesting = !useDepthTesting;
        if (Input.IsKeyPressed(Stride.Input.Keys.Tab)) useWireframe = !useWireframe;
        if (Input.IsKeyPressed(Stride.Input.Keys.Space)) running = !running;

        if (newAmountOfBoxes > currentNumPrimitives)
        {
            InitializePrimitives(currentNumPrimitives, newAmountOfBoxes);
            DebugDraw.MaxPrimitivesWithLifetime = (newAmountOfBoxes * 2) + 8;
            DebugDraw.MaxPrimitives = (newAmountOfBoxes * 2) + 8;
            currentNumPrimitives = newAmountOfBoxes;
        }
        else
        {
            currentNumPrimitives = newAmountOfBoxes;
        }

        int textPositionX = (int)Input.Mouse.SurfaceSize.X - 384;
        DebugText.Print($"Primitive Count: {currentNumPrimitives} (scroll wheel to adjust)", new Int2(textPositionX, StartTextPositionY));
        DebugText.Print(" - Hold shift: faster count adjustment", new Int2(textPositionX, StartTextPositionY + TextIncrement));
        DebugText.Print($" - Render Mode: {mode} (left alt to switch)", new Int2(textPositionX, StartTextPositionY + (TextIncrement * 2)));
        DebugText.Print($" - Depth Testing: {(useDepthTesting ? "On " : "Off")} (left ctrl to toggle)", new Int2(textPositionX, StartTextPositionY + (TextIncrement * 3)));
        DebugText.Print($" - Fill mode: {(useWireframe ? "Wireframe" : "Solid")} (tab to toggle)", new Int2(textPositionX, StartTextPositionY + (TextIncrement * 4)));
        DebugText.Print($" - State: {(running ? "Simulating" : "Paused")} (space to toggle)", new Int2(textPositionX, StartTextPositionY + (TextIncrement * 5)));

        if (running)
        {
            // NOTE: Cannot cache Span outside lambda (ref struct). Reacquire spans inside iteration.
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

                color.R = (byte)(((position.X / AreaSize) + 1f) / 2.0f * 255.0f);
                color.G = (byte)(((position.Y / AreaSize) + 1f) / 2.0f * 255.0f);
                color.B = (byte)(((position.Z / AreaSize) + 1f) / 2.0f * 255.0f);
                color.A = 255;
            });
        }

        // Sequential draw loop: use spans for fewer bounds checks
        var drawPosSpan = CollectionsMarshal.AsSpan(primitivePositions);
        var drawRotSpan = CollectionsMarshal.AsSpan(primitiveRotations);
        var drawVelSpan = CollectionsMarshal.AsSpan(primitiveVelocities);
        var drawRVelSpan = CollectionsMarshal.AsSpan(primitiveRotVelocities);
        var drawColorSpan = CollectionsMarshal.AsSpan(primitiveColors);

        int currentShape = 0;
        for (int i = 0; i < currentNumPrimitives; ++i)
        {
            ref var position = ref drawPosSpan[i];
            ref var rotation = ref drawRotSpan[i];
            ref var velocity = ref drawVelSpan[i];
            ref var rotVelocity = ref drawRVelSpan[i];
            ref var color = ref drawColorSpan[i];

            switch (mode)
            {
                case CurRenderMode.All:
                    switch (currentShape++)
                    {
                        case 0:
                            DebugDraw.DrawSphere(position, 0.5f, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 1:
                            DebugDraw.DrawCube(position, new Vector3(1, 1, 1), rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 2:
                            DebugDraw.DrawCapsule(position, 2.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 3:
                            DebugDraw.DrawCylinder(position, 2.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 4:
                            DebugDraw.DrawCone(position, 1.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 5:
                            DebugDraw.DrawRay(position, velocity, color, depthTest: useDepthTesting);
                            break;
                        case 6:
                            DebugDraw.DrawQuad(position, new Vector2(1.0f), rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 7:
                            DebugDraw.DrawCircle(position, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 8:
                            DebugDraw.DrawHalfSphere(position, 0.5f, color, rotation, depthTest: useDepthTesting, solid: !useWireframe);
                            currentShape = 0;
                            break;
                        default:
                            break;
                    }
                    break;
                case CurRenderMode.Quad:
                    DebugDraw.DrawQuad(position, new Vector2(1.0f), rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                    break;
                case CurRenderMode.Circle:
                    DebugDraw.DrawCircle(position, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                    break;
                case CurRenderMode.Sphere:
                    DebugDraw.DrawSphere(position, 0.5f, color, depthTest: useDepthTesting, solid: !useWireframe);
                    break;
                case CurRenderMode.HalfSphere:
                    DebugDraw.DrawHalfSphere(position, 0.5f, color, rotation, depthTest: useDepthTesting, solid: !useWireframe);
                    break;
                case CurRenderMode.Cube:
                    DebugDraw.DrawCube(position, new Vector3(1, 1, 1), rotation, color, depthTest: useDepthTesting, solid: !useWireframe && i % 2 == 0);
                    break;
                case CurRenderMode.Capsule:
                    DebugDraw.DrawCapsule(position, 2.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                    break;
                case CurRenderMode.Cylinder:
                    DebugDraw.DrawCylinder(position, 2.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                    break;
                case CurRenderMode.Cone:
                    DebugDraw.DrawCone(position, 1.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                    break;
                case CurRenderMode.Ray:
                    DebugDraw.DrawRay(position, velocity, color, depthTest: useDepthTesting);
                    break;
                case CurRenderMode.Arrow:
                    DebugDraw.DrawArrow(position, position + velocity, color: color, depthTest: useDepthTesting, solid: !useWireframe);
                    break;
                case CurRenderMode.None:
                    break;
            }
        }

        DebugDraw.DrawCube(new Vector3(0, 0, 0), new Vector3(1.0f, 1.0f, 1.0f), color: Color.White);
        DebugDraw.DrawBounds(new Vector3(-5, 0, -5), new Vector3(5, 5, 5), color: Color.White);
        DebugDraw.DrawBounds(new Vector3(-AreaSize), new Vector3(AreaSize), color: Color.HotPink);

        if (Input.IsMouseButtonPressed(Stride.Input.MouseButton.Left) && CurrentCamera != null)
        {
            var clickPos = Input.MousePosition;
            var result = CurrentCamera.Raycast(this.GetSimulation(), clickPos);

            if (result.Succeeded)
            {
                var cameraWorldPos = CurrentCamera.Entity.Transform.WorldMatrix.TranslationVector;
                var cameraWorldUp = CurrentCamera.Entity.Transform.WorldMatrix.Up;
                var cameraWorldNormal = Vector3.Normalize(result.Point - cameraWorldPos);
                DebugDraw.DrawLine(cameraWorldPos + (cameraWorldNormal * -2.0f) + (cameraWorldUp * (-0.125f / 4.0f)), result.Point, color: Color.HotPink, duration: 5.0f);
                DebugDraw.DrawArrow(result.Point, result.Point + result.Normal, color: Color.HotPink, duration: 5.0f);
                DebugDraw.DrawArrow(result.Point, result.Point + Vector3.Reflect(result.Point - cameraWorldPos, result.Normal), color: Color.LimeGreen, duration: 5.0f);
            }
        }

        // draw 16 cubes in a line, for testing depth testing disabled stuff
        var startPos = new Vector3(0, 5, 0);
        for (int i = -(16 / 2); i < 16 / 2; ++i)
        {
            DebugDraw.DrawCube(startPos + new Vector3(i, 0, 0), Vector3.One, depthTest: useDepthTesting, solid: true);
            DebugDraw.DrawCube(startPos + new Vector3(i, 0, 0), Vector3.One, depthTest: useDepthTesting, solid: false, color: Color.White);
        }

        // draw every primitive to see where they're put
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

        // center cone thing yes
        DebugDraw.DrawCone(new Vector3(0, 0.5f, 0), 2.0f, 0.5f, color: Color.HotPink);
    }
}