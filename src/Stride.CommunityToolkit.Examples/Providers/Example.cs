namespace Stride.CommunityToolkit.Examples.Providers;

public class Example
{
    public string Id { get; set; }
    public string Title { get; set; }
    public Action Action { get; set; }

    public Example(string id, string title, Action action)
    {
        Id = id;
        Title = title;
        Action = action;
    }
}