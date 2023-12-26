using Stride.Core;
using Stride.Engine;
using Stride.Input;
using Stride.UI.Controls;

namespace Example06_SaveTheCube;
[DataContract]
public class UiData : StartupScript
{
    public List<IClickable> Clickables { get; set; } = new();
}
public interface IClickable
{
    string Prefix { get; }
    int Count { get; }
    void HandleClick(TextBlock textBlock);
    bool CanHandle(MouseButton button);
}
[DataContract]
public class Left : IClickable
{
    public string Prefix { get; init; } = "Left Mouse Button";
    public int Count { get; set; }

    public bool CanHandle(MouseButton button) => button == MouseButton.Left;
    public void HandleClick(TextBlock block) => block.Text = $"{Prefix} : {++Count }";
}
[DataContract]
public class Right : IClickable
{
    public string Prefix { get; init; } = "Right Mouse Button";
    public int Count { get; set; }

    public bool CanHandle(MouseButton button) => button == MouseButton.Right;
    public void HandleClick(TextBlock block) => block.Text = $"{Prefix} : {++Count }";
}