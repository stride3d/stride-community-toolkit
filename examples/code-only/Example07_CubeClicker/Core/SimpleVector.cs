using Stride.Core;

namespace Example07_CubeClicker.Core;

/// <summary>
/// Stride.Core.Vector isn't generated as the generator can't reach the core without being in it
/// So a workaround has to be made with a local class
/// </summary>
[DataContract]
public readonly struct SimpleVector
{
    public float X { get; init; }

    public float Y { get; init; }

    public float Z { get; init; }

    public SimpleVector(float x, float y, float z) : this()
    {
        X = x;
        Y = y;
        Z = z;
    }
}