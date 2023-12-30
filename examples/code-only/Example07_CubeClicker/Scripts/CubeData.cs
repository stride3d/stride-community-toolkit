using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Example07_CubeClicker.Scripts;

[DataContract]
internal class CubeData
{
    [DataMember]
    internal List<SimpleVector> CubePositions { get; set;} = new ();
    public void AddCube(Entity entity)
    {
        Vector3 position = entity.Transform.Position;
        CubePositions.Add(new SimpleVector() { X = position.X, Z = position.Z });
    }
}
