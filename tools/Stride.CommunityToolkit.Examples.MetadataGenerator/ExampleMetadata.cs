namespace Stride.CommunityToolkit.Examples.MetadataGenerator;

/// <summary>
/// Represents the metadata for a single example extracted from YAML frontmatter.
/// </summary>
public class ExampleMetadata
{
    public Dictionary<string, string>? Title { get; set; }
    public string? Level { get; set; }
    public string? Category { get; set; }
    public int? Complexity { get; set; }
    public Dictionary<string, string>? Description { get; set; }
    public List<string>? Concepts { get; set; }
    public List<string>? Related { get; set; }
    public List<string>? Tags { get; set; }
    public int? Order { get; set; }
    public bool? Enabled { get; set; }
    public string? Created { get; set; }

    // Populated during scanning
    public string? ProjectName { get; set; }
    public string? ProjectPath { get; set; }
}