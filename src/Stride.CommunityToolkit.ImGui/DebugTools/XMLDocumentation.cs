using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml;
using WebUtility = System.Net.WebUtility;

namespace Stride.CommunityToolkit.ImGui.DebugTools;

/// <summary>
/// Utility class to provide documentation for various types where available with the assembly
/// </summary>
internal static class XMLDocumentation
{
    // Two-level cache: one xml document per assembly, one parsed entry per member. An assembly without a
    // documentation file, and a member without an entry, are cached as null so the search is not repeated
    private static readonly ConcurrentDictionary<Assembly, XmlDocument?> _documents = new();
    private static readonly ConcurrentDictionary<MemberInfo, CachedDocumentation?> _documentation = new();

    /// <summary> Returns false if the documentation file wasn't found </summary>
    internal static bool TryGetSummary(MemberInfo member, [NotNullWhen(true)] out string? summary)
    {
        summary = TryGetDocumentation(member, out var doc) ? doc.CleanSummary : null;

        return summary is not null;
    }

    private static bool TryGetDocumentation(MemberInfo member, [NotNullWhen(true)] out CachedDocumentation? documentation)
    {
        if (!_documentation.TryGetValue(member, out documentation))
        {
            var document = _documents.GetOrAdd(member.Module.Assembly, LoadDocument);

            documentation = document?["doc"]?["members"]?.SelectSingleNode($"member[@name='{DocumentationId(member)}']") is XmlElement element
                ? new CachedDocumentation(element)
                : null;

            _documentation.TryAdd(member, documentation);
        }

        return documentation is not null;
    }

    // The xml file the compiler writes next to the assembly, when the build produced one
    private static XmlDocument? LoadDocument(Assembly assembly)
    {
        if (string.IsNullOrEmpty(assembly.Location)) return null;

        var path = Path.ChangeExtension(assembly.Location, ".xml");

        if (!File.Exists(path)) return null;

        var document = new XmlDocument();

        document.Load(path);

        return document;
    }

    // The id the compiler writes: M:Type.Method(Params), T:Type, P: or F: for the rest
    private static string DocumentationId(MemberInfo member)
    {
        switch (member)
        {
            case MethodInfo method:
                var parameters = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));

                return $"M:{method.DeclaringType?.FullName}.{method.Name}{(parameters.Length > 0 ? $"({parameters})" : "")}";
            case Type type:
                return $"T:{type.FullName}";
            default:
                return $"{member.MemberType.ToString()[0]}:{member.DeclaringType}.{member.Name}";
        }
    }

    private sealed class CachedDocumentation(XmlElement element)
    {
        private readonly Lazy<string> _cleanSummary = new(() => CleanUp(element));

        internal string CleanSummary => _cleanSummary.Value;

        private static string CleanUp(XmlElement element)
        {
            if (element.SelectSingleNode("summary")?.InnerXml is not { } raw) return "";

            // Decode xml entities such as '&amp;' and strip the cref markup, then the indentation of each line
            raw = WebUtility.HtmlDecode(raw).Replace("<see cref=", "").Replace("/>", "");

            return string.Join('\n', raw.Split('\n').Select(line => line.Trim()).SkipWhile(line => line.Length == 0));
        }
    }
}