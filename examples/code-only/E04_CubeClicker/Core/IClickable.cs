using Stride.Input;

namespace E04_CubeClicker.Core;

/// <summary>
/// Not tagged [DataContract]; each tagged implementing class registers itself for it with the YAML registry.
/// </summary>
public interface IClickable
{
    string Prefix { get; init; }
    int Count { get; set; }
    MouseButton Type { get; }
    void HandleClick();
}