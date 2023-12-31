using Stride.Core;
using Stride.Core.Mathematics;

namespace Example07_CubeClicker.Core;

[DataContract]
internal class CubeData
{
    [DataMember]
    internal List<SimpleVector> CubePositions { get; set; } = new();

    public void AddPosition(Vector3 vector)
        => CubePositions.Add(new SimpleVector() { X = vector.X, Y = vector.Y, Z = vector.Z });
}