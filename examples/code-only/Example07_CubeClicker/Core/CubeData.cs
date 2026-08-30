using Stride.Core;
using Stride.Core.Mathematics;
using System.Diagnostics.CodeAnalysis;

namespace Example07_CubeClicker.Core;

[DataContract]
public sealed class CubeData
{
    // Accepts the null the YAML deserialiser hands over for a missing key and keeps a list - see ClickData
    [DataMember, AllowNull]
    public List<SimpleVector> CubePositions { get => field; set => field = value ?? []; } = [];

    public void AddPosition(Vector3 vector)
        => CubePositions.Add(new SimpleVector(vector.X, vector.Y, vector.Z));
}