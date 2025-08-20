using Stride.CommunityToolkit.Bullet;
using Stride.CommunityToolkit.DebugShapes.Code;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Physics;
using System.Runtime.InteropServices;

namespace Example08_DebugShapes.Scripts;

public class ShapeUpdater : SyncScript
{
    private const int ChangePerSecond = 8192 + 2048;
    private const int InitialNumPrimitives = 1024;
    private const int AreaSize = 64;
    private const int TextIncrement = 16;
    private const int StartTextPositionY = 32;
    private const int MinNumberOfPrimitives = 0;
    private const int MaxNumberOfPrimitives = 327680;

    [DataMemberIgnore]
    ImmediateDebugRenderSystem? _debugDraw; // Provided by services

    private int _currentNumPrimitives = InitialNumPrimitives;
    private CurRenderMode _mode = CurRenderMode.All;
    private bool _useDepthTesting = true;
    private bool _useWireframe = true;
    private bool _running = true;

    private readonly List<Vector3> _primitivePositions = new(InitialNumPrimitives);
    private readonly List<Quaternion> _primitiveRotations = new(InitialNumPrimitives);
    private readonly List<Vector3> _primitiveVelocities = new(InitialNumPrimitives);
    private readonly List<Vector3> _primitiveRotVelocities = new(InitialNumPrimitives);
    private readonly List<Color> _primitiveColors = new(InitialNumPrimitives);

    private CameraComponent? _currentCamera;
    private readonly Random _random = new();

    private void InitializePrimitives(int from, int to)
    {
        EnsureSize(_primitivePositions, to);
        EnsureSize(_primitiveRotations, to);
        EnsureSize(_primitiveVelocities, to);
        EnsureSize(_primitiveRotVelocities, to);
        EnsureSize(_primitiveColors, to);

        var posSpan = CollectionsMarshal.AsSpan(_primitivePositions);
        var rotSpan = CollectionsMarshal.AsSpan(_primitiveRotations);
        var velSpan = CollectionsMarshal.AsSpan(_primitiveVelocities);
        var rvelSpan = CollectionsMarshal.AsSpan(_primitiveRotVelocities);
        var colSpan = CollectionsMarshal.AsSpan(_primitiveColors);

        for (int i = from; i < to; ++i)
        {
            var randX = _random.Next(-AreaSize, AreaSize);
            var randY = _random.Next(-AreaSize, AreaSize);
            var randZ = _random.Next(-AreaSize, AreaSize);

            var ballVel = new Vector3(
                (float)(_random.NextDouble() * 4.0),
                (float)(_random.NextDouble() * 4.0),
                (float)(_random.NextDouble() * 4.0));

            var ballRotVel = new Vector3(
                (float)_random.NextDouble(),
                (float)_random.NextDouble(),
                (float)_random.NextDouble());

            posSpan[i] = new Vector3(randX, randY, randZ);
            rotSpan[i] = Quaternion.Identity;
            velSpan[i] = ballVel;
            rvelSpan[i] = ballRotVel;
            colSpan[i] = ComputeColor(posSpan[i]);
        }
    }

    private static void EnsureSize<T>(List<T> list, int size)
    {
        if (list.Count >= size) return;

        list.Capacity = Math.Max(list.Capacity, size);

        int toAdd = size - list.Count;

        for (int i = 0; i < toAdd; i++)
        {
            list.Add(default!);
        }
    }

    private static Color ComputeColor(in Vector3 p)
    {
        byte r = (byte)(((p.X / AreaSize) + 1f) * 0.5f * 255.0f);
        byte g = (byte)(((p.Y / AreaSize) + 1f) * 0.5f * 255.0f);
        byte b = (byte)(((p.Z / AreaSize) + 1f) * 0.5f * 255.0f);

        return new Color(r, g, b, 255);
    }

    public override void Start()
    {
        _currentCamera = SceneSystem.SceneInstance.RootScene.Entities.First(e => e.Get<CameraComponent>() != null).Get<CameraComponent>();
        _debugDraw = Services.GetService<ImmediateDebugRenderSystem>();

        if (_debugDraw is null)
        {
            throw new InvalidOperationException("ImmediateDebugRenderSystem service is not available. Ensure it is added to the services.");
        }

        _debugDraw.PrimitiveColor = Color.Green;
        ResizeDebugDrawCapacity(_currentNumPrimitives);
        _debugDraw.Visible = true;
        DebugText.Visible = true;

        InitializePrimitives(0, _currentNumPrimitives);
    }

    public override void Update()
    {
        var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;

        HandleInput(dt, out int newCount);
        AdjustPrimitiveCount(newCount);
        DrawUiText();
        if (_running && _currentNumPrimitives > 0) Simulate(dt);
        DrawPrimitives();
        HandleMousePicking();
        DrawAuxiliaryTestGeometry();
    }

    private void HandleInput(float dt, out int newCount)
    {
        var speedyDelta = Input.IsKeyDown(Stride.Input.Keys.LeftShift) ? 100.0f : 1.0f;

        newCount = Clamp(_currentNumPrimitives + (int)(Input.MouseWheelDelta * ChangePerSecond * speedyDelta * dt), MinNumberOfPrimitives, MaxNumberOfPrimitives);

        if (Input.IsKeyPressed(Stride.Input.Keys.LeftAlt)) _mode = (CurRenderMode)(((int)_mode + 1) % ((int)CurRenderMode.None + 1));
        if (Input.IsKeyPressed(Stride.Input.Keys.LeftCtrl)) _useDepthTesting = !_useDepthTesting;
        if (Input.IsKeyPressed(Stride.Input.Keys.Tab)) _useWireframe = !_useWireframe;
        if (Input.IsKeyPressed(Stride.Input.Keys.Space)) _running = !_running;
    }

    private void AdjustPrimitiveCount(int newAmount)
    {
        if (newAmount > _currentNumPrimitives)
        {
            InitializePrimitives(_currentNumPrimitives, newAmount);
            ResizeDebugDrawCapacity(newAmount);
        }

        _currentNumPrimitives = newAmount;
    }

    private void ResizeDebugDrawCapacity(int primitives)
    {
        if (_debugDraw is null) return;

        _debugDraw.MaxPrimitivesWithLifetime = (primitives * 2) + 8;
        _debugDraw.MaxPrimitives = (primitives * 2) + 8;
    }

    private void Simulate(float dt)
    {
        Dispatcher.For(0, _currentNumPrimitives, i =>
        {
            var posSpan = CollectionsMarshal.AsSpan(_primitivePositions);
            var velSpan = CollectionsMarshal.AsSpan(_primitiveVelocities);
            var rvelSpan = CollectionsMarshal.AsSpan(_primitiveRotVelocities);
            var rotSpan = CollectionsMarshal.AsSpan(_primitiveRotations);
            var colSpan = CollectionsMarshal.AsSpan(_primitiveColors);

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

    private void DrawPrimitives()
    {
        if (_debugDraw is null || _currentNumPrimitives <= 0) return;

        var posSpan = CollectionsMarshal.AsSpan(_primitivePositions);
        var rotSpan = CollectionsMarshal.AsSpan(_primitiveRotations);
        var velSpan = CollectionsMarshal.AsSpan(_primitiveVelocities);
        var rvelSpan = CollectionsMarshal.AsSpan(_primitiveRotVelocities);
        var colSpan = CollectionsMarshal.AsSpan(_primitiveColors);

        int currentShape = 0;
        for (int i = 0; i < _currentNumPrimitives; ++i)
        {
            ref readonly var position = ref posSpan[i];
            ref readonly var rotation = ref rotSpan[i];
            ref readonly var velocity = ref velSpan[i];
            ref readonly var rotVelocity = ref rvelSpan[i];
            ref readonly var color = ref colSpan[i];

            switch (_mode)
            {
                case CurRenderMode.All:
                    switch (currentShape++)
                    {
                        case 0: _debugDraw.DrawSphere(position, 0.5f, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                        case 1: _debugDraw.DrawCube(position, new Vector3(1, 1, 1), rotation, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                        case 2: _debugDraw.DrawCapsule(position, 2.0f, 0.5f, rotation, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                        case 3: _debugDraw.DrawCylinder(position, 2.0f, 0.5f, rotation, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                        case 4: _debugDraw.DrawCone(position, 1.0f, 0.5f, rotation, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                        case 5: _debugDraw.DrawRay(position, velocity, color, depthTest: _useDepthTesting); break;
                        case 6: _debugDraw.DrawQuad(position, new Vector2(1.0f), rotation, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                        case 7: _debugDraw.DrawCircle(position, 0.5f, rotation, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                        case 8: _debugDraw.DrawHalfSphere(position, 0.5f, color, rotation, depthTest: _useDepthTesting, solid: !_useWireframe); currentShape = 0; break;
                    }
                    break;
                case CurRenderMode.Quad: _debugDraw.DrawQuad(position, new Vector2(1.0f), rotation, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                case CurRenderMode.Circle: _debugDraw.DrawCircle(position, 0.5f, rotation, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                case CurRenderMode.Sphere: _debugDraw.DrawSphere(position, 0.5f, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                case CurRenderMode.HalfSphere: _debugDraw.DrawHalfSphere(position, 0.5f, color, rotation, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                case CurRenderMode.Cube: _debugDraw.DrawCube(position, new Vector3(1, 1, 1), rotation, color, depthTest: _useDepthTesting, solid: !_useWireframe && i % 2 == 0); break;
                case CurRenderMode.Capsule: _debugDraw.DrawCapsule(position, 2.0f, 0.5f, rotation, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                case CurRenderMode.Cylinder: _debugDraw.DrawCylinder(position, 2.0f, 0.5f, rotation, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                case CurRenderMode.Cone: _debugDraw.DrawCone(position, 1.0f, 0.5f, rotation, color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                case CurRenderMode.Ray: _debugDraw.DrawRay(position, velocity, color, depthTest: _useDepthTesting); break;
                case CurRenderMode.Arrow: _debugDraw.DrawArrow(position, position + velocity, color: color, depthTest: _useDepthTesting, solid: !_useWireframe); break;
                case CurRenderMode.None: break;
            }
        }
    }

    private void DrawUiText()
    {
        int textPositionX = (int)Input.Mouse.SurfaceSize.X - 384;
        DebugText.Print($"Primitive Count: {_currentNumPrimitives} (scroll wheel to adjust)", new Int2(textPositionX, StartTextPositionY));
        DebugText.Print(" - Hold shift: faster count adjustment", new Int2(textPositionX, StartTextPositionY + TextIncrement));
        DebugText.Print($" - Render Mode: {_mode} (left alt to switch)", new Int2(textPositionX, StartTextPositionY + (TextIncrement * 2)));
        DebugText.Print($" - Depth Testing: {(_useDepthTesting ? "On " : "Off")} (left ctrl to toggle)", new Int2(textPositionX, StartTextPositionY + (TextIncrement * 3)));
        DebugText.Print($" - Fill mode: {(_useWireframe ? "Wireframe" : "Solid")} (tab to toggle)", new Int2(textPositionX, StartTextPositionY + (TextIncrement * 4)));
        DebugText.Print($" - State: {(_running ? "Simulating" : "Paused")} (space to toggle)", new Int2(textPositionX, StartTextPositionY + (TextIncrement * 5)));
    }

    private void HandleMousePicking()
    {
        if (!Input.IsMouseButtonPressed(Stride.Input.MouseButton.Left) || _currentCamera is null || _debugDraw is null)
        {
            return;
        }

        var clickPos = Input.MousePosition;
        var result = _currentCamera.Raycast(this.GetSimulation(), clickPos);

        if (!result.Succeeded) return;

        var cameraWorldPos = _currentCamera.Entity.Transform.WorldMatrix.TranslationVector;
        var cameraWorldUp = _currentCamera.Entity.Transform.WorldMatrix.Up;
        var cameraWorldNormal = Vector3.Normalize(result.Point - cameraWorldPos);

        _debugDraw.DrawLine(cameraWorldPos + (cameraWorldNormal * -2.0f) + (cameraWorldUp * (-0.125f / 4.0f)), result.Point, color: Color.HotPink, duration: 5.0f);
        _debugDraw.DrawArrow(result.Point, result.Point + result.Normal, color: Color.HotPink, duration: 5.0f);
        _debugDraw.DrawArrow(result.Point, result.Point + Vector3.Reflect(result.Point - cameraWorldPos, result.Normal), color: Color.LimeGreen, duration: 5.0f);
    }

    private void DrawAuxiliaryTestGeometry()
    {
        if (_debugDraw is null) return;

        _debugDraw.DrawCube(new Vector3(0, 0, 0), new Vector3(1.0f, 1.0f, 1.0f), color: Color.White);
        _debugDraw.DrawBounds(new Vector3(-5, 0, -5), new Vector3(5, 5, 5), color: Color.White);
        _debugDraw.DrawBounds(new Vector3(-AreaSize), new Vector3(AreaSize), color: Color.HotPink);

        // Line of cubes for depth test visualization
        var startPos = new Vector3(0, 5, 0);
        for (int i = -8; i < 8; ++i)
        {
            var p = startPos + new Vector3(i, 0, 0);
            _debugDraw.DrawCube(p, Vector3.One, depthTest: _useDepthTesting, solid: true);
            _debugDraw.DrawCube(p, Vector3.One, depthTest: _useDepthTesting, solid: false, color: Color.White);
        }

        // Show every primitive type in a row
        var testPos = new Vector3(0.0f, 0.0f, -5.0f);
        _debugDraw.PrimitiveColor = Color.Red;
        _debugDraw.DrawQuad(testPos + new Vector3(1.0f, 0.0f, 0.0f), new Vector2(1.0f), solid: !_useWireframe);
        _debugDraw.DrawCircle(testPos + new Vector3(2.0f, 0.0f, 0.0f), 0.5f, solid: !_useWireframe);
        _debugDraw.DrawSphere(testPos + new Vector3(3.0f, 0.0f, 0.0f), 0.5f, solid: !_useWireframe);
        _debugDraw.DrawCube(testPos + new Vector3(4.0f, 0.0f, 0.0f), new Vector3(1.0f), solid: !_useWireframe);
        _debugDraw.DrawCapsule(testPos + new Vector3(5.0f, 0.0f, 0.0f), 1.0f, 0.5f, solid: !_useWireframe);
        _debugDraw.DrawCylinder(testPos + new Vector3(6.0f, 0.0f, 0.0f), 1.0f, 0.5f, solid: !_useWireframe);
        _debugDraw.DrawCone(testPos + new Vector3(7.0f, 0.0f, 0.0f), 1.0f, 0.5f, solid: !_useWireframe);
        _debugDraw.DrawHalfSphere(testPos + new Vector3(8.0f, 0.0f, 0.0f), 0.5f, solid: !_useWireframe);
        _debugDraw.DrawHalfSphere(testPos + new Vector3(9.0f, 0.0f, 0.0f), 0.5f, rotation: Quaternion.RotationX((float)Math.PI), solid: !_useWireframe);
        _debugDraw.DrawCone(new Vector3(0, 0.5f, 0), 2.0f, 0.5f, color: Color.HotPink);
    }

    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
}