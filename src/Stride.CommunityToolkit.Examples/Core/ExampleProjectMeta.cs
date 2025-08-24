namespace Stride.CommunityToolkit.Examples.Core;

public sealed record ExampleProjectMeta(
    string Id,
    string Title,
    string ProjectFile,
    int? Order,
    string? Category
);