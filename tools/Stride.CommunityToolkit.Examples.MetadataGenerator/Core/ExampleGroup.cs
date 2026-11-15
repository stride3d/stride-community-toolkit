namespace Stride.CommunityToolkit.Examples.MetadataGenerator.Core;

/// <summary>
/// The examples of one language and level, which is how the toc, the gallery and the landing pages group them.
/// </summary>
/// <param name="Language">The source language, one of <see cref="MetadataVocabulary.Languages"/>.</param>
/// <param name="Level">The teaching level, one of <see cref="MetadataVocabulary.Levels"/>.</param>
/// <param name="Examples">The examples of the group, in manifest order.</param>
public sealed record ExampleGroup(string Language, string Level, IReadOnlyList<ExampleMetadata> Examples);