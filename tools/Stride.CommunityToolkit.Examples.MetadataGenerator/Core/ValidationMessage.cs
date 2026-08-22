namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

/// <summary>
/// How seriously to take a validation finding.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>Worth fixing, but the manifest is still usable.</summary>
    Warning,

    /// <summary>The metadata is wrong. In strict mode this fails the build.</summary>
    Error
}

/// <summary>
/// A single validation finding, attributed to the example that caused it.
/// </summary>
/// <param name="Severity">How seriously to take the finding.</param>
/// <param name="ProjectName">The example the finding belongs to, or <c>manifest</c> for cross-example checks.</param>
/// <param name="Field">The metadata key involved, for example <c>slug</c>.</param>
/// <param name="Message">What is wrong, and where practical, what to write instead.</param>
public sealed record ValidationMessage(
    ValidationSeverity Severity,
    string ProjectName,
    string Field,
    string Message)
{
    /// <summary>Creates an error-severity message.</summary>
    public static ValidationMessage Error(string projectName, string field, string message)
        => new(ValidationSeverity.Error, projectName, field, message);

    /// <summary>Creates a warning-severity message.</summary>
    public static ValidationMessage Warning(string projectName, string field, string message)
        => new(ValidationSeverity.Warning, projectName, field, message);

    /// <inheritdoc />
    public override string ToString() => $"{ProjectName} [{Field}]: {Message}";
}
