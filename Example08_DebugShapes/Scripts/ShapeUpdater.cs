using Stride.Core.Collections;
using Stride.Core;
using Stride.Engine;
using Stride.Physics;
using Stride.Rendering;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using DebugShapes;

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

    [DataMemberIgnore]
    ImmediateDebugRenderSystem DebugDraw; // this is here to make it look like it should when properly integrated

    int minNumberofPrimitives = 0;
    int maxNumberOfPrimitives = 327680;
    int currentNumPrimitives = InitialNumPrimitives;
    CurRenderMode mode = CurRenderMode.All;
    bool useDepthTesting = true;
    bool useWireframe = true;
    bool running = true;

    FastList<Vector3> primitivePositions = new FastList<Vector3>(InitialNumPrimitives);
    FastList<Quaternion> primitiveRotations = new FastList<Quaternion>(InitialNumPrimitives);
    FastList<Vector3> primitiveVelocities = new FastList<Vector3>(InitialNumPrimitives);
    FastList<Vector3> primitiveRotVelocities = new FastList<Vector3>(InitialNumPrimitives);
    FastList<Color> primitiveColors = new FastList<Color>(InitialNumPrimitives);

    public CameraComponent CurrentCamera;

    private void InitializePrimitives(int from, int to)
    {

        var random = new Random();

        primitivePositions.Resize(to, true);
        primitiveRotations.Resize(to, true);
        primitiveVelocities.Resize(to, true);
        primitiveRotVelocities.Resize(to, true);
        primitiveColors.Resize(to, true);

        for (int i = from; i < to; ++i)
        {

            // initialize boxes

            var randX = random.Next(-AreaSize, AreaSize);
            var randY = random.Next(-AreaSize, AreaSize);
            var randZ = random.Next(-AreaSize, AreaSize);

            var velX = random.NextDouble() * 4.0;
            var velY = random.NextDouble() * 4.0;
            var velZ = random.NextDouble() * 4.0;
            var ballVel = new Vector3((float)velX, (float)velY, (float)velZ);

            var rotVelX = random.NextDouble();
            var rotVelY = random.NextDouble();
            var rotVelZ = random.NextDouble();
            var ballRotVel = new Vector3((float)rotVelX, (float)rotVelY, (float)rotVelZ);

            primitivePositions.Items[i] = new Vector3(randX, randY, randZ);
            primitiveRotations.Items[i] = Quaternion.Identity;
            primitiveVelocities.Items[i] = ballVel;
            primitiveRotVelocities.Items[i] = ballRotVel;

            ref var color = ref primitiveColors.Items[i];
            color.R = (byte)(((primitivePositions[i].X / AreaSize) + 1f) / 2.0f * 255.0f);
            color.G = (byte)(((primitivePositions[i].Y / AreaSize) + 1f) / 2.0f * 255.0f);
            color.B = (byte)(((primitivePositions[i].Z / AreaSize) + 1f) / 2.0f * 255.0f);
            color.A = 255;

        }
    }

    public override void Start()
    {
        CurrentCamera = SceneSystem.SceneInstance.RootScene.Entities.First(e => e.Get<CameraComponent>() != null).Get<CameraComponent>();

        RenderStage FindRenderStage(RenderSystem renderSystem, string name)
        {
            for (int i = 0; i < renderSystem.RenderStages.Count; ++i)
            {
                var stage = renderSystem.RenderStages[i];
                if (stage.Name == name)
                {
                    return stage;
                }
            }
            return null;
        }

        DebugDraw = new ImmediateDebugRenderSystem(Services);
        DebugDraw.PrimitiveColor = Color.Green;
        DebugDraw.MaxPrimitives = (currentNumPrimitives * 2) + 8;
        DebugDraw.MaxPrimitivesWithLifetime = (currentNumPrimitives * 2) + 8;
        DebugDraw.Visible = true;

        // FIXME
        var debugRenderFeatures = SceneSystem.GraphicsCompositor.RenderFeatures.OfType<ImmediateDebugRenderFeature>();
        var opaqueRenderStage = FindRenderStage(SceneSystem.GraphicsCompositor.RenderSystem, "Opaque");
        var transparentRenderStage = FindRenderStage(SceneSystem.GraphicsCompositor.RenderSystem, "Transparent");

        if (!debugRenderFeatures.Any())
        {
            var newDebugRenderFeature = new ImmediateDebugRenderFeature()
            {
                RenderStageSelectors = {
                        new ImmediateDebugRenderStageSelector
                        {
                            OpaqueRenderStage = opaqueRenderStage,
                            TransparentRenderStage = transparentRenderStage
                        }
                    }
            };
            SceneSystem.GraphicsCompositor.RenderFeatures.Add(newDebugRenderFeature);
        }
        // keep DebugText visible in release builds too
        DebugText.Visible = true;
        Services.AddService(DebugDraw);
        Game.GameSystems.Add(DebugDraw);

        InitializePrimitives(0, currentNumPrimitives);

    }

    private int Clamp(int v, int min, int max)
    {
        if (v < min)
        {
            return min;
        }
        else if (v > max)
        {
            return max;
        }
        else
        {
            return v;
        }
    }

    public override void Update()
    {

        var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;

        var speedyDelta = (Input.IsKeyDown(Stride.Input.Keys.LeftShift)) ? 100.0f : 1.0f;
        var newAmountOfBoxes = Clamp(currentNumPrimitives + (int)(Input.MouseWheelDelta * ChangePerSecond * speedyDelta * dt), minNumberofPrimitives, maxNumberOfPrimitives);

        if (Input.IsKeyPressed(Stride.Input.Keys.LeftAlt))
        {
            mode = (CurRenderMode)(((int)mode + 1) % ((int)CurRenderMode.None + 1));
        }

        if (Input.IsKeyPressed(Stride.Input.Keys.LeftCtrl))
        {
            useDepthTesting = !useDepthTesting;
        }

        if (Input.IsKeyPressed(Stride.Input.Keys.Tab))
        {
            useWireframe = !useWireframe;
        }

        if (Input.IsKeyPressed(Stride.Input.Keys.Space))
        {
            running = !running;
        }

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
        DebugText.Print($"Primitive Count: {currentNumPrimitives} (scrollwheel to adjust)",
            new Int2(textPositionX, 32));

        DebugText.Print($" - Render Mode: {mode} (left alt to switch)",
            new Int2(textPositionX, 48));

        DebugText.Print($" - Depth Testing: {(useDepthTesting ? "On " : "Off")} (left ctrl to toggle)",
            new Int2(textPositionX, 64));

        DebugText.Print($" - Fillmode: {(useWireframe ? "Wireframe" : "Solid")} (tab to toggle)",
            new Int2(textPositionX, 80));

        DebugText.Print($" - State: {(running ? "Simulating" : "Paused")} (space to toggle)",
            new Int2(textPositionX, 96));

        if (running)
        {
            Dispatcher.For(0, currentNumPrimitives, i =>
            {

                ref var position = ref primitivePositions.Items[i];
                ref var velocity = ref primitiveVelocities.Items[i];
                ref var rotVelocity = ref primitiveRotVelocities.Items[i];
                ref var rotation = ref primitiveRotations.Items[i];
                ref var color = ref primitiveColors.Items[i];

                if (position.X > AreaSize || position.X < -AreaSize)
                {
                    velocity.X = -velocity.X;
                }

                if (position.Y > AreaSize || position.Y < -AreaSize)
                {
                    velocity.Y = -velocity.Y;
                }

                if (position.Z > AreaSize || position.Z < -AreaSize)
                {
                    velocity.Z = -velocity.Z;
                }

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

        int currentShape = 0;

        for (int i = 0; i < currentNumPrimitives; ++i)
        {

            ref var position = ref primitivePositions.Items[i];
            ref var rotation = ref primitiveRotations.Items[i];
            ref var velocity = ref primitiveVelocities.Items[i];
            ref var rotVelocity = ref primitiveRotVelocities.Items[i];
            ref var color = ref primitiveColors.Items[i];

            switch (mode)
            {
                case CurRenderMode.All:
                    switch (currentShape++)
                    {
                        case 0: // sphere
                            DebugDraw.DrawSphere(position, 0.5f, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 1: // cube
                            DebugDraw.DrawCube(position, new Vector3(1, 1, 1), rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 2: // capsule
                            DebugDraw.DrawCapsule(position, 2.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 3: // cylinder
                            DebugDraw.DrawCylinder(position, 2.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 4: // cone
                            DebugDraw.DrawCone(position, 1.0f, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 5: // ray
                            DebugDraw.DrawRay(position, velocity, color, depthTest: useDepthTesting);
                            break;
                        case 6: // quad
                            DebugDraw.DrawQuad(position, new Vector2(1.0f), rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 7: // circle
                            DebugDraw.DrawCircle(position, 0.5f, rotation, color, depthTest: useDepthTesting, solid: !useWireframe);
                            break;
                        case 8: // half sphere
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

        // CUBE OF ORIGIN!!
        DebugDraw.DrawCube(new Vector3(0, 0, 0), new Vector3(1.0f, 1.0f, 1.0f), color: Color.White);
        DebugDraw.DrawBounds(new Vector3(-5, 0, -5), new Vector3(5, 5, 5), color: Color.White);
        DebugDraw.DrawBounds(new Vector3(-AreaSize), new Vector3(AreaSize), color: Color.HotPink);

        if (Input.IsMouseButtonPressed(Stride.Input.MouseButton.Left))
        {
            var clickPos = Input.MousePosition;
            var result = ScreenPositionToWorldPositionRaycast(clickPos, CurrentCamera, this.GetSimulation());
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
    public static HitResult ScreenPositionToWorldPositionRaycast(Vector2 screenPos, CameraComponent camera, Simulation simulation)
    {
        Matrix invViewProj = Matrix.Invert(camera.ViewProjectionMatrix);

        // Reconstruct the projection-space position in the (-1, +1) range.
        //    Don't forget that Y is down in screen coordinates, but up in projection space
        Vector3 sPos;
        sPos.X = screenPos.X * 2f - 1f;
        sPos.Y = 1f - screenPos.Y * 2f;

        // Compute the near (start) point for the raycast
        // It's assumed to have the same projection space (x,y) coordinates and z = 0 (lying on the near plane)
        // We need to unproject it to world space
        sPos.Z = 0f;
        var vectorNear = Vector3.Transform(sPos, invViewProj);
        vectorNear /= vectorNear.W;

        // Compute the far (end) point for the raycast
        // It's assumed to have the same projection space (x,y) coordinates and z = 1 (lying on the far plane)
        // We need to unproject it to world space
        sPos.Z = 1f;
        var vectorFar = Vector3.Transform(sPos, invViewProj);
        vectorFar /= vectorFar.W;

        // Raycast from the point on the near plane to the point on the far plane and get the collision result
        var result = simulation.Raycast(vectorNear.XYZ(), vectorFar.XYZ());
        return result;
    }
}
