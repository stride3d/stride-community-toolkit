using Stride.Core;
using Stride.Input;

namespace Example07_CubeClicker.Core;

/// <summary>
/// Has to be directly [DataContract] tagged, else it won't detect it.
/// </summary>
[DataContract]
public sealed class LeftMouseButtonCounter : IClickable
{
    /// <summary>
    /// get and init/set is required for the serializer
    /// this can be made internal, but it would require a [DataMember] Attribute then
    /// We can adjust that string in the yaml, so the application loads the new Prefix instead of this one.
    /// </summary>
    public string Prefix { get; init; } = "Left Mouse Button";

    /// <summary>
    /// The click count
    /// </summary>
    public int Count { get; set; }

    public MouseButton Type { get; } = MouseButton.Left;

    public void HandleClick() => Count++;

    public override string ToString() => $"{Prefix}: {Count}";
}