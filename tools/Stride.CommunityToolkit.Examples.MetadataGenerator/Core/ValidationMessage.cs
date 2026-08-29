namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

/// <summary>
/// How seriously to take a validation finding.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Nothing is wrong. The finding records something the generator decided, so that a deliberate
    /// outcome is not indistinguishable from a silent one - a <c>related:</c> link dropped because its
    /// target is <c>enabled: false</c>, for instance.
    /// </summary>
    /// <remarks>
    /// Kept out of the warning count on purpose. A warning that nobody can act on is noise on every
    /// build, and noise that appears on every build stops being read.
    /// </remarks>
    Info,

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

    /// <summary>Creates an informational message, which is not counted as a warning.</summary>
    public static ValidationMessage Info(string projectName, string field, string message)
        => new(ValidationSeverity.Info, projectName, field, message);

    /// <inheritdoc />
    public override string ToString() => $"{ProjectName} [{Field}]: {Message}";
}
