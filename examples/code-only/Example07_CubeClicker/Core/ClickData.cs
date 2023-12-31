using Stride.Core;

namespace Example07_CubeClicker.Core;

[DataContract]
public sealed class ClickData
{
    /// <summary>
    /// "NullPattern", ensuring that there is never null and a valid fallback option
    /// </summary>
    public static ClickData Default => new()
    {
        Clickables = [new LeftMouseButtonCounter(), new RightMouseButtonCounter()]
    };

    public string DataName { get; set; } = "Just to have more example data in the saved file";

    /// <summary>
    /// We can serialize Interfaces, Abstracts as long as the "real" object in it
    /// has it's class [DataContract] tagged
    /// </summary>
    public List<IClickable> Clickables { get; set; } = new();
}