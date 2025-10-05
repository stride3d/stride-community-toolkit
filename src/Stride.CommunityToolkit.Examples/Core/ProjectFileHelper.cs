using System.Xml.Linq;

namespace Stride.CommunityToolkit.Examples.Core;

internal static class ProjectFileHelper
{
    public static (string? Title, string? AssemblyName, string? Category, int? Order, bool? Enabled) ReadExampleMetadata(string projectFile,
        string exampleTitleElement = "ExampleTitle",
        string exampleOrderElement = "ExampleOrder",
        string exampleCategoryElement = "ExampleCategory",
        string exampleEnabledElement = "ExampleEnabled")
    {
        var doc = XDocument.Load(projectFile, LoadOptions.None);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid project XML.");
        var ns = root.Name.Namespace;

        string? GetProp(string name) =>
            root.Elements(ns + "PropertyGroup")
                .Elements()
                .FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?.Value?.Trim();

        var explicitTitle = GetProp(exampleTitleElement) ?? GetProp("Title");
        var assemblyName = GetProp("AssemblyName");
        var category = GetProp(exampleCategoryElement);
        var orderRaw = GetProp(exampleOrderElement);
        var enabledRaw = GetProp(exampleEnabledElement);

        int? order = int.TryParse(orderRaw, out var o) ? o : null;
        bool? enabled = bool.TryParse(enabledRaw, out var e) && e;

        return (explicitTitle, assemblyName, category, order, enabled);
    }
}
