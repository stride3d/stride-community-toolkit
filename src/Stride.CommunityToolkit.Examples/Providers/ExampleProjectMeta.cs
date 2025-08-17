namespace Stride.CommunityToolkit.Examples.Providers;

public sealed record ExampleProjectMeta(
    string Id,
    string Title,
    string ProjectFile,
    int? Order,
    string? Category
);