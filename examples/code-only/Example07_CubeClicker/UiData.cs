using Stride.Core;
using Stride.Engine;
using Stride.Input;
using Stride.UI.Controls;

namespace Example07_CubeClicker;

[DataContract]
public class UiData : StartupScript
{
    /// <summary>
    /// "NullPattern", ensuring that there is never null and a valid fallback option
    /// </summary>
    public static UiData Default => new()
    {
        Clickables = [new LeftMouseButtonCounter(), new RightMouseButtonCounter()]
    };
    /// <summary>
    /// Just to have more data in the file
    /// </summary>
    public string DataName { get; set; } = "UiData";
    /// <summary>
    /// We can serialize Interfaces, Abstracts as long as the "real" object in it
    /// has it's class [DataContract] tagged
    /// </summary>
    public List<IClickable> Clickables { get; set; } = new();
}
/// <summary>
/// Interfaces aren't [DataContract] tagged.
/// An Implementing class will add it on registration to the YamlRegistry
/// </summary>
public interface IClickable
{
    string Prefix { get; init; }
    int Count { get; set; }
    bool CanHandle(MouseButton button);
    void HandleClick(TextBlock block);
}
/// <summary>
/// Has to be directly [DataContract] tagged, else it won't detect it.
/// </summary>
[DataContract]
public class LeftMouseButtonCounter : IClickable
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

    public void HandleClick(TextBlock block) => block.Text = $"{Prefix} : {Count = Count + 1}";
    public bool CanHandle(MouseButton button) => button == MouseButton.Left;
    public override string ToString() => $"{Prefix} : {Count}";
}
[DataContract]
public class RightMouseButtonCounter : IClickable
{
    public string Prefix { get; init; } = "Right Mouse Button";
    public int Count { get; set; }

    public bool CanHandle(MouseButton button) => button == MouseButton.Right;
    public void HandleClick(TextBlock block) => block.Text = $"{Prefix} : {Count = Count + 1}";
    public override string ToString() => $"{Prefix} : {Count}";
}