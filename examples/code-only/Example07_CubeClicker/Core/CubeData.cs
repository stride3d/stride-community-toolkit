using Stride.Core;
using Stride.Core.Mathematics;

namespace Example07_CubeClicker.Core;

[DataContract]
public sealed class CubeData
{
    [DataMember]
    public List<SimpleVector> CubePositions { get; set; } = new();

    public void AddPosition(Vector3 vector)
        => CubePositions.Add(new SimpleVector(vector.X, vector.Y, vector.Z));
}