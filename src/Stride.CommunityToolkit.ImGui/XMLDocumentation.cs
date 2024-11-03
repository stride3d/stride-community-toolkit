using System.Collections.Concurrent;
using System.Reflection;
using System.Xml;
using WebUtility = System.Net.WebUtility;

namespace Stride.CommunityToolkit.ImGui;

/// <summary>
/// Utility class to provide documentation for various types where available with the assembly
/// </summary>
public static class XMLDocumentation
{
    static ConcurrentDictionary<Assembly, XmlDocument> _documents = new ConcurrentDictionary<Assembly, XmlDocument>();
    static ConcurrentDictionary<MemberInfo, CachedDocumentation> _documentation = new ConcurrentDictionary<MemberInfo, CachedDocumentation>();

    static bool TryGetDocumentation(MemberInfo member, out CachedDocumentation documentation)
    {
        if (_documentation.TryGetValue(member, out documentation) == false)
        {
            Assembly assembly;
            if (member is Type t)
                assembly = t.Assembly;
            else
                assembly = member.DeclaringType.Assembly;

            if (_documents.TryGetValue(assembly, out XmlDocument document) == false)
            {
                var filepath = assembly.Location;

                const string LOCAL_PREFIX = "file:///";
                if (filepath.StartsWith(LOCAL_PREFIX))
                {
                    filepath = filepath.Substring(LOCAL_PREFIX.Length);
                    filepath = Path.ChangeExtension(filepath, ".xml");
                    TextReader streamReader;
                    try
                    {
                        streamReader = new StreamReader(filepath);
                    }
                    catch (FileNotFoundException)
                    {
                        streamReader = null;
                    }

                    if (streamReader != null)
                    {
                        document = new XmlDocument();
                        document.Load(streamReader);
                    }
                    else
                        document = null;
                }
                else
                {
                    // not sure how to safely deal with other prefixes
                    document = null;
                }

                _documents.TryAdd(assembly, document);
            }

            if (document is null)
                documentation = null;
            else
            {
                string fullName;
                switch (member)
                {
                    case MethodInfo methodInfo:
                        var parameters = "";
                        foreach (var parameterInfo in methodInfo.GetParameters())
                        {
                            if (parameters.Length > 0)
                                parameters += ",";

                            parameters += parameterInfo.ParameterType.FullName;
                        }

                        if (parameters.Length > 0)
                            parameters = $"({parameters})";

                        fullName = $"M:{methodInfo.DeclaringType.FullName}.{methodInfo.Name}{parameters}";
                        break;
                    case Type type:
                        fullName = $"T:{type.FullName}";
                        break;
                    default:
                        fullName = $"{member.MemberType.ToString()[0]}:{member.DeclaringType}.{member.Name}";
                        break;
                }

                if (document["doc"]?["members"]?.SelectSingleNode($"member[@name='{fullName}']") is XmlElement element)
                    documentation = new CachedDocumentation(element);
                else
                    documentation = null;
            }

            _documentation.TryAdd(member, documentation);
        }

        return documentation != null;
    }

    /// <summary> Returns false if the documentation file wasn't found </summary>
    public static bool TryGetDocumentation(MemberInfo member, out XmlElement elementOut)
    {
        if (TryGetDocumentation(member, out CachedDocumentation docu))
        {
            elementOut = docu.Element;
            return true;
        }

        elementOut = null;
        return false;
    }

    /// <summary> Returns false if the documentation file wasn't found </summary>
    public static bool TryGetSummary(MemberInfo member, out string summary)
    {
        if (TryGetDocumentation(member, out CachedDocumentation doc))
        {
            summary = doc.CleanSummary;
            return true;
        }

        summary = null;
        return false;
    }

    public class CachedDocumentation
    {
        public XmlElement Element { get; }

        public string CleanSummary
        {
            get
            {
                lock (_lock)
                {
                    return _cleanSummary ?? (_cleanSummary = GetCleanSummary());
                }
            }
        }

        string _cleanSummary;

        object _lock = new object();

        public CachedDocumentation(XmlElement elem)
        {
            Element = elem;
        }

        string GetCleanSummary()
        {
            string rawString = Element?.SelectSingleNode("summary")?.InnerXml;
            if (rawString is null)
                return "";

            // Decodes xml entities like '&amp;'
            rawString = WebUtility.HtmlDecode(rawString);
            rawString = rawString.Replace("<see cref=", "").Replace("/>", "");

            // cleanup tabs and spaces on new line
            string final = "";
            foreach (string lines in rawString.Split(new[] { '\n' }, StringSplitOptions.None))
            {
                string cleanedLine = lines.Trim();
                if (final != "")
                    final += $"\n{cleanedLine}";
                else
                    final += cleanedLine;
            }

            return final;
        }
    }
}