using Stride.Core;
using Stride.Engine;
using Stride.Input;
using Stride.UI.Controls;

namespace Example06_SaveTheCube;
[DataContract]
public class UiData : StartupScript
{
    public string DataName { get; set; } = "UiData";
    public List<IClickable> Clickables { get; set; } = new();
}
public interface IClickable
{
    string Prefix { get; init; }
    int Count { get; set; }
    bool CanHandle(MouseButton button);
    void HandleClick(TextBlock block);
}
[DataContract]
public class Left : IClickable
{
    public string Prefix { get; init; } = "Left Mouse Button";
    public int Count { get; set; }

    public void HandleClick(TextBlock block) => block.Text = $"{Prefix} : { Count = Count +1 }";
    public bool CanHandle(MouseButton button) => button == MouseButton.Left;
    
}
[DataContract]
public class Right : IClickable
{
    public string Prefix { get; init; } = "Right Mouse Button";
    public int Count { get; set; }

    public bool CanHandle(MouseButton button) => button == MouseButton.Right;
    public void HandleClick(TextBlock block) => block.Text = $"{Prefix} : {Count = Count + 1 }";
}