using Stride.Core;

namespace Example07_CubeClicker.Scripts;

/// <summary>
/// Stride.Core.Vector isn't generated as the generator can't reach the core without being in it
/// So a workaround has to be made with a local class
/// </summary>
[DataContract]
internal struct SimpleVector
{
    public float X; public float Z;
}