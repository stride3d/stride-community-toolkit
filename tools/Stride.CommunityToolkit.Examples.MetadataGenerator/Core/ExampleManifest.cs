namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

/// <summary>
/// The generated <c>examples-manifest.json</c> document.
/// </summary>
/// <remarks>
/// The examples are wrapped in an envelope rather than written as a bare array so that the shape can
/// grow - a consumer reading <see cref="SchemaVersion"/> can tell a v1 manifest from a later one
/// without guessing. This is a build artifact: <see cref="GeneratedAt"/> changes on every run, so the
/// file is not tracked in git.
/// </remarks>
public sealed class ExampleManifest
{
    /// <summary>The current schema version emitted by this tool.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Gets the schema version of this document.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    /// <summary>Gets the UTC timestamp of the run that produced the document, in ISO 8601.</summary>
    public required string GeneratedAt { get; init; }

    /// <summary>Gets the version of the generator that produced the document.</summary>
    public required string ToolVersion { get; init; }

    /// <summary>Gets the number of examples in <see cref="Examples"/>.</summary>
    public int Count => Examples.Count;

    /// <summary>Gets the examples, ordered by language, then level, then <c>order</c>.</summary>
    public required IReadOnlyList<ExampleMetadata> Examples { get; init; }
}