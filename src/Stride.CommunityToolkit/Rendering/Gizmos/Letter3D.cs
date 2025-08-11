using Stride.CommunityToolkit.Scripts;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;

namespace Stride.CommunityToolkit.Rendering.Gizmos;

/// <summary>
/// Helper for constructing simple 3D axis letter gizmos (X, Y, Z) composed of cylinder primitives.
/// </summary>
public class Letter3D
{
    private const float CylinderRadius = 0.02f;
    private const float CylinderHeight = 0.3f;
    private const int CylinderTessellation = 8;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly bool _rotateAxisNames;
    private readonly Material _letterMaterial;

    /// <summary>
    /// Initializes a new instance of <see cref="Letter3D"/>.
    /// </summary>
    /// <param name="graphicsDevice">Graphics device used to create primitive meshes.</param>
    /// <param name="rotateAxisNames">If <c>true</c>, attaches a script that keeps letters facing the camera.</param>
    public Letter3D(GraphicsDevice graphicsDevice, bool rotateAxisNames)
    {
        _graphicsDevice = graphicsDevice;
        _rotateAxisNames = rotateAxisNames;
        _letterMaterial = GizmoUniformColorMaterial.Create(graphicsDevice, Color.White);
    }

    /// <summary>
    /// Creates an entity representing the letter 'X'.
    /// </summary>
    public Entity CreateLetterX()
    {
        var part1 = CreatePrimitive();
        part1.Transform.Rotation = Quaternion.RotationX(MathUtil.Pi / 5);

        var part2 = CreatePrimitive();
        part2.Transform.Rotation = Quaternion.RotationX(-MathUtil.Pi / 5);

        var letter = new Entity("LetterX");
        letter.AddChild(part1);
        letter.AddChild(part2);

        if (_rotateAxisNames)
        {
            letter.Add(new GizmoBillboardLetterScript());
        }

        return letter;
    }

    /// <summary>
    /// Creates an entity representing the letter 'Y'.
    /// </summary>
    public Entity CreateLetterY()
    {
        var stem = CreatePrimitive(height: CylinderHeight * 0.6f);

        var branch1 = CreatePrimitive(height: CylinderHeight / 2);
        branch1.Transform.Rotation = Quaternion.RotationX(MathUtil.Pi / 5);
        branch1.Transform.Position = new Vector3(0, CylinderHeight / 2, CylinderHeight / 6);

        var branch2 = CreatePrimitive(height: CylinderHeight / 2);
        branch2.Transform.Rotation = Quaternion.RotationX(-MathUtil.Pi / 5);
        branch2.Transform.Position = branch1.Transform.Position * new Vector3(1, 1, -1);

        var letter = new Entity("LetterY");
        letter.Transform.Children.Add(stem.Transform);
        letter.Transform.Children.Add(branch1.Transform);
        letter.Transform.Children.Add(branch2.Transform);

        if (_rotateAxisNames)
        {
            letter.Add(new GizmoBillboardLetterScript());
        }

        return letter;
    }

    /// <summary>
    /// Creates an entity representing the letter 'Z'.
    /// </summary>
    public Entity CreateLetterZ()
    {
        var top = CreatePrimitive(height: CylinderHeight / 2);
        top.Transform.Rotation = Quaternion.RotationX(MathUtil.PiOverTwo);
        top.Transform.Position = new Vector3(0, CylinderHeight / 2 - CylinderRadius * 1.9f, 0);

        var bottom = CreatePrimitive(height: CylinderHeight / 2); ;
        bottom.Transform.Rotation = Quaternion.RotationX(-MathUtil.PiOverTwo);
        bottom.Transform.Position = new Vector3(0, -CylinderHeight / 2 + CylinderRadius * 1.9f, 0);

        var diagonal = CreatePrimitive();
        diagonal.Transform.Rotation = Quaternion.RotationX(-MathUtil.Pi / 5);

        var letter = new Entity("LetterZ");
        letter.Transform.Children.Add(top.Transform);
        letter.Transform.Children.Add(bottom.Transform);
        letter.Transform.Children.Add(diagonal.Transform);

        if (_rotateAxisNames)
        {
            letter.Add(new GizmoBillboardLetterScript() { DefaultRotation = 270 });
        }

        return letter;
    }

    /// <summary>
    /// Creates a cylindrical primitive with the configured material.
    /// </summary>
    private Entity CreatePrimitive(float radius = CylinderRadius, float height = CylinderHeight, int tessellation = CylinderTessellation)
    {
        var mesh = GeometricPrimitive.Cylinder.New(_graphicsDevice, height, radius, tessellation).ToMeshDraw();

        return new() { new ModelComponent { Model = new Model { _letterMaterial, new Mesh { Draw = mesh } } } };
    }
}
