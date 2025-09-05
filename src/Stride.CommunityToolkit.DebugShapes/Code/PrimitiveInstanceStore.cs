using Stride.CommunityToolkit.Collections;
using Stride.Core.Mathematics;
using System.Runtime.InteropServices;

namespace Stride.CommunityToolkit.DebugShapes.Code;

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
    }

    internal readonly List<InstanceData> _instances = new(1);
    internal readonly List<Matrix> _transforms = new(1);
    internal readonly List<Color> _colors = new(1);
    internal readonly List<LineVertex> _lineVertices = new(1);

    /// <summary>
    /// Ensures backing lists have capacity for the given number of instances and line vertices.
    /// </summary>
    public void EnsureCapacity(int additionalInstances, int additionalLineVertices)
    {
        _instances.EnsureSize(_instances.Count + additionalInstances);
        _lineVertices.EnsureSize(_lineVertices.Count + additionalLineVertices);
    }

    /// <summary>
    /// Processes renderables and writes instance/line data into backing lists.
    /// Offsets are incremented in-place to reflect consumed slots.
    /// </summary>
    public void ProcessRenderables(List<Renderable> renderables, ref Primitives offsets)
    {
        var span = CollectionsMarshal.AsSpan(renderables);
        for (int i = 0; i < span.Length; ++i)
        {
            ref readonly var cmd = ref span[i];
            switch (cmd.Type)
            {
                case DebugPrimitiveType.Quad:
                    _instances[offsets.Quads] = new InstanceData
                    {
                        Position = cmd.QuadData.Position,
                        Rotation = cmd.QuadData.Rotation,
                        Scale = new Vector3(cmd.QuadData.Size.X, 1.0f, cmd.QuadData.Size.Y),
                        Color = cmd.QuadData.Color
                    };
                    offsets.Quads++;
                    break;
                case DebugPrimitiveType.Circle:
                    _instances[offsets.Circles] = new InstanceData
                    {
                        Position = cmd.CircleData.Position,
                        Rotation = cmd.CircleData.Rotation,
                        Scale = new Vector3(cmd.CircleData.Radius * 2.0f, 0.0f, cmd.CircleData.Radius * 2.0f),
                        Color = cmd.CircleData.Color
                    };
                    offsets.Circles++;
                    break;
                case DebugPrimitiveType.Sphere:
                    _instances[offsets.Spheres] = new InstanceData
                    {
                        Position = cmd.SphereData.Position,
                        Rotation = Quaternion.Identity,
                        Scale = new Vector3(cmd.SphereData.Radius * 2),
                        Color = cmd.SphereData.Color
                    };
                    offsets.Spheres++;
                    break;
                case DebugPrimitiveType.HalfSphere:
                    _instances[offsets.HalfSpheres] = new InstanceData
                    {
                        Position = cmd.HalfSphereData.Position,
                        Rotation = cmd.HalfSphereData.Rotation,
                        Scale = new Vector3(cmd.HalfSphereData.Radius * 2),
                        Color = cmd.HalfSphereData.Color
                    };
                    offsets.HalfSpheres++;
                    break;
                case DebugPrimitiveType.Cube:
                    {
                        ref readonly var start = ref cmd.CubeData.Start;
                        ref readonly var end = ref cmd.CubeData.End;
                        _instances[offsets.Cubes] = new InstanceData
                        {
                            Position = start,
                            Rotation = cmd.CubeData.Rotation,
                            Scale = end - start,
                            Color = cmd.CubeData.Color
                        };
                        offsets.Cubes++;
                        break;
                    }
                case DebugPrimitiveType.Capsule:
                    _instances[offsets.Capsules] = new InstanceData
                    {
                        Position = cmd.CapsuleData.Position,
                        Rotation = cmd.CapsuleData.Rotation,
                        Scale = new Vector3(cmd.CapsuleData.Radius * 2.0f, cmd.CapsuleData.Height, cmd.CapsuleData.Radius * 2.0f),
                        Color = cmd.CapsuleData.Color
                    };
                    offsets.Capsules++;
                    break;
                case DebugPrimitiveType.Cylinder:
                    _instances[offsets.Cylinders] = new InstanceData
                    {
                        Position = cmd.CylinderData.Position,
                        Rotation = cmd.CylinderData.Rotation,
                        Scale = new Vector3(cmd.CylinderData.Radius * 2.0f, cmd.CylinderData.Height, cmd.CylinderData.Radius * 2.0f),
                        Color = cmd.CylinderData.Color
                    };
                    offsets.Cylinders++;
                    break;
                case DebugPrimitiveType.Cone:
                    _instances[offsets.Cones] = new InstanceData
                    {
                        Position = cmd.ConeData.Position,
                        Rotation = cmd.ConeData.Rotation,
                        Scale = new Vector3(cmd.ConeData.Radius * 2.0f, cmd.ConeData.Height, cmd.ConeData.Radius * 2.0f),
                        Color = cmd.ConeData.Color
                    };
                    offsets.Cones++;
                    break;
                case DebugPrimitiveType.Line:
                    _lineVertices[offsets.Lines++] = new LineVertex { Position = cmd.LineData.Start, Color = cmd.LineData.Color };
                    _lineVertices[offsets.Lines++] = new LineVertex { Position = cmd.LineData.End, Color = cmd.LineData.Color };
                    break;
            }
        }
    }
}