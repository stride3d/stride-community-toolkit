using Stride.Core;
using Stride.Input;
using System.Diagnostics.CodeAnalysis;

namespace Example07_CubeClicker.Core;

[DataContract]
public sealed class RightMouseButtonCounter : IClickable
{
    /// <summary>
    /// get and init/set is required for the serializer
    /// this can be made internal, but it would require a [DataMember] Attribute then
    /// We can adjust that string in the yaml, so the application loads the new Prefix instead of this one.
    /// A missing key deserialises as null; [AllowNull] and the coalescing init keep the default instead.
    /// </summary>
    [AllowNull]
    public string Prefix { get => field; init => field = value ?? "Right Mouse Button"; } = "Right Mouse Button";

    /// <summary>
    /// The click count
    /// </summary>
    public int Count { get; set; }

    public MouseButton Type { get; } = MouseButton.Right;

    public void HandleClick() => Count++;

    public override string ToString() => $"{Prefix}: {Count}";
}