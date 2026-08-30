using Stride.Core;
using System.Diagnostics.CodeAnalysis;

namespace Example07_CubeClicker.Core;

// The YAML deserialiser assigns whatever it read to each property - including null for a key that is
// missing from the file. [AllowNull] lets the setter accept that without a nullable warning, and the
// coalescing setter turns it back into a usable value, so a hand-edited or older save cannot leave a
// property null.
[DataContract]
public sealed class ClickData
{
    /// <summary>
    /// "NullPattern", ensuring that there is never null and a valid fallback option
    /// </summary>
    public static ClickData Default => new()
    {
        Clickables = [new LeftMouseButtonCounter(), new RightMouseButtonCounter()]
    };

    [AllowNull]
    public string DataName { get => field; set => field = value ?? DefaultDataName; } = DefaultDataName;

    private const string DefaultDataName = "Just to have more example data in the saved file";

    /// <summary>
    /// We can serialize Interfaces, Abstracts as long as the "real" object in it
    /// has it's class [DataContract] tagged
    /// </summary>
    [AllowNull]
    public List<IClickable> Clickables { get => field; set => field = value ?? []; } = [];
}