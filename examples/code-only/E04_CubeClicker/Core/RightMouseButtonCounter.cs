using Stride.Core;
using Stride.Input;
using System.Diagnostics.CodeAnalysis;

namespace E04_CubeClicker.Core;

[DataContract]
public sealed class RightMouseButtonCounter : IClickable
{
    /// <summary>
    /// The serializer needs get and init/set; an internal property would also need [DataMember].
    /// Editing the value in the YAML file changes the loaded prefix.
    /// A missing key deserialises as null; [AllowNull] and the coalescing init keep the default instead.
    /// </summary>
    [AllowNull]
    public string Prefix { get => field; init => field = value ?? "Right Mouse Button"; } = "Right Mouse Button";

    public int Count { get; set; }

    public MouseButton Type { get; } = MouseButton.Right;

    public void HandleClick() => Count++;

    public override string ToString() => $"{Prefix}: {Count}";
}