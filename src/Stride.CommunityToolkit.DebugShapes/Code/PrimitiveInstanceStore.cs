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
    internal struct InstanceData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public Color Color;

        internal InstanceData(Vector3 position, Quaternion rotation, Vector3 scale, Color color)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Color = color;
        }
    }

    internal readonly List<InstanceData> _instances = new(1);
    internal readonly List<Matrix> _transforms = new(1);
    internal readonly List<Color> _colors = new(1);
    internal readonly List<LineVertex> _lineVertices = new(1);

    /// <summary>
    /// Ensures backing lists have capacity for the given number of instances and line vertices.
    /// </summary>
    internal void EnsureCapacity(int additionalInstances, int additionalLineVertices)
    {
        _instances.EnsureSize(_instances.Count + additionalInstances);
        _lineVertices.EnsureSize(_lineVertices.Count + additionalLineVertices);
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
        var span = CollectionsMarshal.AsSpan(renderables);
        for (int i = 0; i < span.Length; ++i)
        {
            ref readonly var cmd = ref span[i];
            switch (cmd.Type)
            {
                case DebugPrimitiveType.Quad:
                    _instances[offsets.Quads] = new InstanceData(cmd.QuadData.Position, cmd.QuadData.Rotation, new Vector3(cmd.QuadData.Size.X, 1.0f, cmd.QuadData.Size.Y), cmd.QuadData.Color);
                    offsets.Quads++;
                    break;
                case DebugPrimitiveType.Circle:
                    _instances[offsets.Circles] = new InstanceData(cmd.CircleData.Position, cmd.CircleData.Rotation, new Vector3(cmd.CircleData.Radius * 2.0f, 0.0f, cmd.CircleData.Radius * 2.0f), cmd.CircleData.Color);
                    offsets.Circles++;
                    break;
                case DebugPrimitiveType.Sphere:
                    _instances[offsets.Spheres] = new InstanceData(cmd.SphereData.Position, Quaternion.Identity, new Vector3(cmd.SphereData.Radius * 2), cmd.SphereData.Color);
                    offsets.Spheres++;
                    break;
                case DebugPrimitiveType.HalfSphere:
                    _instances[offsets.HalfSpheres] = new InstanceData(cmd.HalfSphereData.Position, cmd.HalfSphereData.Rotation, new Vector3(cmd.HalfSphereData.Radius * 2), cmd.HalfSphereData.Color);
                    offsets.HalfSpheres++;
                    break;
                case DebugPrimitiveType.Cube:
                    {
                        ref readonly var start = ref cmd.CubeData.Start;
                        ref readonly var end = ref cmd.CubeData.End;
                        _instances[offsets.Cubes] = new InstanceData(start, cmd.CubeData.Rotation, end - start, cmd.CubeData.Color);
                        offsets.Cubes++;
                        break;
                    }
                case DebugPrimitiveType.Capsule:
                    _instances[offsets.Capsules] = new InstanceData(cmd.CapsuleData.Position, cmd.CapsuleData.Rotation, new Vector3(cmd.CapsuleData.Radius * 2.0f, cmd.CapsuleData.Height, cmd.CapsuleData.Radius * 2.0f), cmd.CapsuleData.Color);
                    offsets.Capsules++;
                    break;
                case DebugPrimitiveType.Cylinder:
                    _instances[offsets.Cylinders] = new InstanceData(cmd.CylinderData.Position, cmd.CylinderData.Rotation, new Vector3(cmd.CylinderData.Radius * 2.0f, cmd.CylinderData.Height, cmd.CylinderData.Radius * 2.0f), cmd.CylinderData.Color);
                    offsets.Cylinders++;
                    break;
                case DebugPrimitiveType.Cone:
                    _instances[offsets.Cones] = new InstanceData(cmd.ConeData.Position, cmd.ConeData.Rotation, new Vector3(cmd.ConeData.Radius * 2.0f, cmd.ConeData.Height, cmd.ConeData.Radius * 2.0f), cmd.ConeData.Color);
                    offsets.Cones++;
                    break;
                case DebugPrimitiveType.Line:
                    _lineVertices[offsets.Lines++] = new LineVertex(cmd.LineData.Start, cmd.LineData.Color);
                    _lineVertices[offsets.Lines++] = new LineVertex(cmd.LineData.End, cmd.LineData.Color);
                    break;
            }
        }
    }
}