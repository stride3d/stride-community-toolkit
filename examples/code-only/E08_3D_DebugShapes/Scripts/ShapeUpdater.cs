using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Collections;
using Stride.CommunityToolkit.DebugShapes;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Mathematics;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Input;
using System.Runtime.InteropServices;

namespace E08_3D_DebugShapes.Scripts;

public class ShapeUpdater : SyncScript
{
    private const int ChangePerSecond = 8192 + 2048;
    private const int InitialPrimitives = 1024;
    private const int AreaSize = 64;
    private const int MinPrimitives = 0;
    private const int MaxPrimitives = 327680;

    private readonly List<Vector3> _positions = new(InitialPrimitives);
    private readonly List<Quaternion> _rotations = new(InitialPrimitives);
    private readonly List<Vector3> _velocities = new(InitialPrimitives);
    private readonly List<Vector3> _spins = new(InitialPrimitives);
    private readonly List<Color> _colors = new(InitialPrimitives);
    private readonly Random _random = new();

    private ImmediateDebugRenderSystem _debugDraw = null!;
    private CameraComponent _camera = null!;
    private int _count = InitialPrimitives;
    private CurrentRenderMode _mode = CurrentRenderMode.All;
    private bool _depthTest = true;
    private bool _wireframe = true;
    private bool _running = true;

    public override void Start()
    {
        _camera = SceneSystem.SceneInstance.RootScene.GetCamera() ?? throw new InvalidOperationException("The scene has no camera.");
        _debugDraw = Services.GetService<ImmediateDebugRenderSystem>()
            ?? throw new InvalidOperationException("ImmediateDebugRenderSystem service is not available. Ensure it is added to the services.");

        _debugDraw.PrimitiveColor = Color.Green;
        _debugDraw.Visible = true;
        ResizeDebugDrawCapacity(_count);

        // The callback runs every frame the overlay is drawn
        TextOverlay.GetOrCreate(Game).AddSection("Debug shapes", () =>
        [
            new("Mouse wheel", "Primitive count", Color.Gold),
            new("M", "Hold to change the count faster", Color.Gold),
            new("Left Alt", "Next render mode", Color.Gold),
            new("Left Ctrl", "Toggle depth testing", Color.Gold),
            new("Tab", "Toggle wireframe", Color.Gold),
            new("Space", "Pause and resume", Color.Gold),
            new(""),
            new($"Primitives {_count:N0}", Color.LightGreen),
            new($"Render mode {_mode}", Color.LightGreen),
            new($"Depth testing {(_depthTest ? "on" : "off")}", Color.LightGreen),
            new($"Fill {(_wireframe ? "wireframe" : "solid")}", Color.LightGreen),
            new(_running ? "Simulating" : "Paused", Color.LightGreen),
        ]);

        InitializePrimitives(0, _count);
    }

    public override void Update()
    {
        var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;

        HandleInput(dt);
        if (_running && _count > 0) Simulate(dt);
        DrawPrimitives();
        HandleMousePicking();
        DrawAuxiliaryTestGeometry();
    }

    private void HandleInput(float dt)
    {
        // The wheel changes the count, a hundred times faster with M held; primitives added by it are set up here
        var speed = Input.IsKeyDown(Keys.M) ? 100f : 1f;
        var count = Math.Clamp(_count + (int)(Input.MouseWheelDelta * ChangePerSecond * speed * dt), MinPrimitives, MaxPrimitives);

        if (count > _count)
        {
            InitializePrimitives(_count, count);
            ResizeDebugDrawCapacity(count);
        }

        _count = count;

        if (Input.IsKeyPressed(Keys.LeftAlt)) _mode = (CurrentRenderMode)(((int)_mode + 1) % ((int)CurrentRenderMode.None + 1));
        if (Input.IsKeyPressed(Keys.LeftCtrl)) _depthTest = !_depthTest;
        if (Input.IsKeyPressed(Keys.Tab)) _wireframe = !_wireframe;
        if (Input.IsKeyPressed(Keys.Space)) _running = !_running;
    }

    private void InitializePrimitives(int from, int to)
    {
        _positions.EnsureSize(to);
        _rotations.EnsureSize(to);
        _velocities.EnsureSize(to);
        _spins.EnsureSize(to);
        _colors.EnsureSize(to);

        var positions = CollectionsMarshal.AsSpan(_positions);
        var rotations = CollectionsMarshal.AsSpan(_rotations);
        var velocities = CollectionsMarshal.AsSpan(_velocities);
        var spins = CollectionsMarshal.AsSpan(_spins);
        var colors = CollectionsMarshal.AsSpan(_colors);

        for (var i = from; i < to; i++)
        {
            positions[i] = _random.NextVector3(-AreaSize, AreaSize);
            rotations[i] = Quaternion.Identity;
            velocities[i] = _random.NextVector3(0f, 4f);
            spins[i] = _random.NextVector3(0f, 1f);
            colors[i] = ColorAt(positions[i]);
        }
    }

    /// <summary>The colour of a primitive follows its position: each axis, from -AreaSize to AreaSize, is one channel.</summary>
    private static Color ColorAt(Vector3 position)
    {
        var unit = (position / AreaSize + Vector3.One) * 0.5f;

        return new Color((byte)(unit.X * 255f), (byte)(unit.Y * 255f), (byte)(unit.Z * 255f), 255);
    }

    private void ResizeDebugDrawCapacity(int primitives)
    {
        _debugDraw.MaxPrimitivesWithLifetime = primitives * 2 + 8;
        _debugDraw.MaxPrimitives = primitives * 2 + 8;
    }

    private void Simulate(float dt)
    {
        Dispatcher.For(0, _count, i =>
        {
            var positions = CollectionsMarshal.AsSpan(_positions);
            var velocities = CollectionsMarshal.AsSpan(_velocities);
            var spins = CollectionsMarshal.AsSpan(_spins);
            var rotations = CollectionsMarshal.AsSpan(_rotations);
            var colors = CollectionsMarshal.AsSpan(_colors);

            ref var position = ref positions[i];
            ref var velocity = ref velocities[i];
            ref var spin = ref spins[i];
            ref var rotation = ref rotations[i];
            ref var color = ref colors[i];

            // Bounce off the walls of the area
            if (MathF.Abs(position.X) > AreaSize) velocity.X = -velocity.X;
            if (MathF.Abs(position.Y) > AreaSize) velocity.Y = -velocity.Y;
            if (MathF.Abs(position.Z) > AreaSize) velocity.Z = -velocity.Z;

            position += velocity * dt;
            rotation *= Quaternion.RotationX(spin.X * dt) * Quaternion.RotationY(spin.Y * dt) * Quaternion.RotationZ(spin.Z * dt);
            color = ColorAt(position);
        });
    }

    private void DrawPrimitives()
    {
        var positions = CollectionsMarshal.AsSpan(_positions);
        var rotations = CollectionsMarshal.AsSpan(_rotations);
        var velocities = CollectionsMarshal.AsSpan(_velocities);
        var colors = CollectionsMarshal.AsSpan(_colors);
        var shape = 0;

        for (var i = 0; i < _count; i++)
        {
            ref readonly var position = ref positions[i];
            ref readonly var rotation = ref rotations[i];
            ref readonly var velocity = ref velocities[i];
            ref readonly var color = ref colors[i];

            if (_mode == CurrentRenderMode.All)
            {
                shape = DrawNextShapeInCycle(shape, in position, in rotation, in velocity, in color);
            }
            else
            {
                DrawShapeForMode(i, in position, in rotation, in velocity, in color);
            }
        }
    }

    /// <summary>Draws the next shape of the nine-shape cycle used by <see cref="CurrentRenderMode.All"/>.</summary>
    private int DrawNextShapeInCycle(int shape, in Vector3 position, in Quaternion rotation, in Vector3 velocity, in Color color)
    {
        switch (shape++)
        {
            case 0: _debugDraw.DrawSphere(position, 0.5f, color, depthTest: _depthTest, solid: !_wireframe); break;
            case 1: _debugDraw.DrawCube(position, Vector3.One, rotation, color, depthTest: _depthTest, solid: !_wireframe); break;
            case 2: _debugDraw.DrawCapsule(position, 2.0f, 0.5f, rotation, color, depthTest: _depthTest, solid: !_wireframe); break;
            case 3: _debugDraw.DrawCylinder(position, 2.0f, 0.5f, rotation, color, depthTest: _depthTest, solid: !_wireframe); break;
            case 4: _debugDraw.DrawCone(position, 1.0f, 0.5f, rotation, color, depthTest: _depthTest, solid: !_wireframe); break;
            case 5: _debugDraw.DrawRay(position, velocity, color, depthTest: _depthTest); break;
            case 6: _debugDraw.DrawQuad(position, new Vector2(1.0f), rotation, color, depthTest: _depthTest, solid: !_wireframe); break;
            case 7: _debugDraw.DrawCircle(position, 0.5f, rotation, color, depthTest: _depthTest, solid: !_wireframe); break;
            case 8: _debugDraw.DrawHalfSphere(position, 0.5f, color, rotation, depthTest: _depthTest, solid: !_wireframe); shape = 0; break;
        }

        return shape;
    }

    /// <summary>Draws the shape selected by <see cref="_mode"/> for one primitive.</summary>
    private void DrawShapeForMode(int index, in Vector3 position, in Quaternion rotation, in Vector3 velocity, in Color color)
    {
        switch (_mode)
        {
            case CurrentRenderMode.Quad: _debugDraw.DrawQuad(position, new Vector2(1.0f), rotation, color, depthTest: _depthTest, solid: !_wireframe); break;
            case CurrentRenderMode.Circle: _debugDraw.DrawCircle(position, 0.5f, rotation, color, depthTest: _depthTest, solid: !_wireframe); break;
            case CurrentRenderMode.Sphere: _debugDraw.DrawSphere(position, 0.5f, color, depthTest: _depthTest, solid: !_wireframe); break;
            case CurrentRenderMode.HalfSphere: _debugDraw.DrawHalfSphere(position, 0.5f, color, rotation, depthTest: _depthTest, solid: !_wireframe); break;
            case CurrentRenderMode.Cube: _debugDraw.DrawCube(position, Vector3.One, rotation, color, depthTest: _depthTest, solid: !_wireframe && index % 2 == 0); break;
            case CurrentRenderMode.Capsule: _debugDraw.DrawCapsule(position, 2.0f, 0.5f, rotation, color, depthTest: _depthTest, solid: !_wireframe); break;
            case CurrentRenderMode.Cylinder: _debugDraw.DrawCylinder(position, 2.0f, 0.5f, rotation, color, depthTest: _depthTest, solid: !_wireframe); break;
            case CurrentRenderMode.Cone: _debugDraw.DrawCone(position, 1.0f, 0.5f, rotation, color, depthTest: _depthTest, solid: !_wireframe); break;
            case CurrentRenderMode.Ray: _debugDraw.DrawRay(position, velocity, color, depthTest: _depthTest); break;
            case CurrentRenderMode.Arrow: _debugDraw.DrawArrow(position, position + velocity, color: color, depthTest: _depthTest, solid: !_wireframe); break;
            case CurrentRenderMode.None: break;
        }
    }

    /// <summary>A click draws the ray to what it hit, the normal there and the reflection, for five seconds.</summary>
    private void HandleMousePicking()
    {
        if (!Input.IsMouseButtonPressed(MouseButton.Left)) return;
        if (!_camera.Raycast(Input.MousePosition, 1000, out var hit)) return;

        var cameraPosition = _camera.Entity.Transform.WorldMatrix.TranslationVector;
        var cameraUp = _camera.Entity.Transform.WorldMatrix.Up;
        var towards = Vector3.Normalize(hit.Point - cameraPosition);

        _debugDraw.DrawLine(cameraPosition - towards * 2f - cameraUp * (0.125f / 4f), hit.Point, color: Color.HotPink, duration: 5f);
        _debugDraw.DrawArrow(hit.Point, hit.Point + hit.Normal, color: Color.HotPink, duration: 5f);
        _debugDraw.DrawArrow(hit.Point, hit.Point + Vector3.Reflect(hit.Point - cameraPosition, hit.Normal), color: Color.LimeGreen, duration: 5f);
    }

    private void DrawAuxiliaryTestGeometry()
    {
        _debugDraw.DrawCube(Vector3.Zero, Vector3.One, color: Color.White);
        _debugDraw.DrawBounds(new Vector3(-5, 0, -5), new Vector3(5, 5, 5), color: Color.White);
        _debugDraw.DrawBounds(new Vector3(-AreaSize), new Vector3(AreaSize), color: Color.HotPink);

        // A line of solid cubes outlined in wireframe, to see depth testing at work
        for (var i = -8; i < 8; i++)
        {
            var position = new Vector3(i, 5, 0);

            _debugDraw.DrawCube(position, Vector3.One, depthTest: _depthTest, solid: true);
            _debugDraw.DrawCube(position, Vector3.One, color: Color.White, depthTest: _depthTest, solid: false);
        }

        // One of each shape with a fill in a row
        var row = new Vector3(0, 0, -5);

        _debugDraw.PrimitiveColor = Color.Red;
        _debugDraw.DrawQuad(row + new Vector3(1, 0, 0), new Vector2(1), solid: !_wireframe);
        _debugDraw.DrawCircle(row + new Vector3(2, 0, 0), 0.5f, solid: !_wireframe);
        _debugDraw.DrawSphere(row + new Vector3(3, 0, 0), 0.5f, solid: !_wireframe);
        _debugDraw.DrawCube(row + new Vector3(4, 0, 0), Vector3.One, solid: !_wireframe);
        _debugDraw.DrawCapsule(row + new Vector3(5, 0, 0), 1f, 0.5f, solid: !_wireframe);
        _debugDraw.DrawCylinder(row + new Vector3(6, 0, 0), 1f, 0.5f, solid: !_wireframe);
        _debugDraw.DrawCone(row + new Vector3(7, 0, 0), 1f, 0.5f, solid: !_wireframe);
        _debugDraw.DrawHalfSphere(row + new Vector3(8, 0, 0), 0.5f, solid: !_wireframe);
        _debugDraw.DrawHalfSphere(row + new Vector3(9, 0, 0), 0.5f, rotation: Quaternion.RotationX(MathF.PI), solid: !_wireframe);
        _debugDraw.DrawCone(new Vector3(0, 0.5f, 0), 2f, 0.5f, color: Color.HotPink);
    }
}