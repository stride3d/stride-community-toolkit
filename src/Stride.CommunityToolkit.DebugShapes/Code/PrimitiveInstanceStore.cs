using Stride.CommunityToolkit.Collections;
using Stride.Core.Mathematics;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// Stores instance data for debug primitives and provides methods to process renderable commands.
/// </summary>
internal sealed class PrimitiveInstanceStore
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct InstanceData(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
    {
        public Vector3 Position = position;
        public Quaternion Rotation = rotation;
        public Vector3 Scale = scale;
        public Color Color = color;
    }

    internal readonly List<InstanceData> Instances = new(1);
    internal readonly List<Matrix> Transforms = new(1);
    internal readonly List<Color> Colors = new(1);
    internal readonly List<LineVertex> LineVertices = new(1);

    /// <summary>
    /// Ensures backing lists have capacity for the given number of instances and line vertices.
    /// </summary>
    internal void EnsureCapacity(int additionalInstances, int additionalLineVertices)
    {
        Instances.EnsureSize(Instances.Count + additionalInstances);
        LineVertices.EnsureSize(LineVertices.Count + additionalLineVertices);
    }

    /// <summary>
    /// Processes renderables and writes instance/line data into backing lists.
    /// Offsets are incremented in-place to reflect consumed slots.
    /// </summary>
    internal void ProcessRenderables(List<Renderable> renderables, ref Primitives offsets)
    {
        // One instance slot per shape, written at the offset its own kind has reached so far, so shapes
        // of a kind end up contiguous and can be drawn with a single instanced call.
        // Scale carries the shape's dimensions: a radius becomes a diameter, a cube's extents become
        // end - start, and a flat shape gets zero on the axis it has no thickness in.
        foreach (ref readonly var cmd in CollectionsMarshal.AsSpan(renderables))
        {
            switch (cmd.Type)
            {
                case DebugPrimitiveType.Quad:
                    Instances[offsets.Quads++] = new InstanceData(cmd.QuadData.Position, cmd.QuadData.Rotation, new Vector3(cmd.QuadData.Size.X, 1.0f, cmd.QuadData.Size.Y), cmd.QuadData.Color);
                    break;
                case DebugPrimitiveType.Circle:
                    Instances[offsets.Circles++] = new InstanceData(cmd.CircleData.Position, cmd.CircleData.Rotation, new Vector3(cmd.CircleData.Radius * 2.0f, 0.0f, cmd.CircleData.Radius * 2.0f), cmd.CircleData.Color);
                    break;
                case DebugPrimitiveType.Sphere:
                    Instances[offsets.Spheres++] = new InstanceData(cmd.SphereData.Position, Quaternion.Identity, new Vector3(cmd.SphereData.Radius * 2), cmd.SphereData.Color);
                    break;
                case DebugPrimitiveType.HalfSphere:
                    Instances[offsets.HalfSpheres++] = new InstanceData(cmd.HalfSphereData.Position, cmd.HalfSphereData.Rotation, new Vector3(cmd.HalfSphereData.Radius * 2), cmd.HalfSphereData.Color);
                    break;
                case DebugPrimitiveType.Cube:
                    Instances[offsets.Cubes++] = new InstanceData(cmd.CubeData.Start, cmd.CubeData.Rotation, cmd.CubeData.End - cmd.CubeData.Start, cmd.CubeData.Color);
                    break;
                case DebugPrimitiveType.Capsule:
                    Instances[offsets.Capsules++] = new InstanceData(cmd.CapsuleData.Position, cmd.CapsuleData.Rotation, new Vector3(cmd.CapsuleData.Radius * 2.0f, cmd.CapsuleData.Height, cmd.CapsuleData.Radius * 2.0f), cmd.CapsuleData.Color);
                    break;
                case DebugPrimitiveType.Cylinder:
                    Instances[offsets.Cylinders++] = new InstanceData(cmd.CylinderData.Position, cmd.CylinderData.Rotation, new Vector3(cmd.CylinderData.Radius * 2.0f, cmd.CylinderData.Height, cmd.CylinderData.Radius * 2.0f), cmd.CylinderData.Color);
                    break;
                case DebugPrimitiveType.Cone:
                    Instances[offsets.Cones++] = new InstanceData(cmd.ConeData.Position, cmd.ConeData.Rotation, new Vector3(cmd.ConeData.Radius * 2.0f, cmd.ConeData.Height, cmd.ConeData.Radius * 2.0f), cmd.ConeData.Color);
                    break;
                case DebugPrimitiveType.Line:
                    LineVertices[offsets.Lines++] = new LineVertex(cmd.LineData.Start, cmd.LineData.Color);
                    LineVertices[offsets.Lines++] = new LineVertex(cmd.LineData.End, cmd.LineData.Color);
                    break;
            }
        }
    }
}