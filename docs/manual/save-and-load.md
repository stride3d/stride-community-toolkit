# Save and Load Game Data

A game saves progress, settings and scores as a file the player's machine keeps between runs. There is no toolkit helper for it, because the two pieces are the engine's and the runtime's: Stride's virtual file system says where the file goes, and a serializer of your choice says what is in it.

## Where the file goes

Stride mounts a few folders on its `VirtualFileSystem`, so a game never spells out a path:

| Folder | Use |
|---|---|
| `VirtualFileSystem.ApplicationRoaming` | The player's roaming application data: saves and settings that belong to the user |
| `VirtualFileSystem.ApplicationLocal` | The player's local application data: caches and anything that need not follow the user |
| `VirtualFileSystem.ApplicationData` | The `data` folder beside the executable, where the examples keep their files; fine while developing, not a place to write in an installed game |

Each is an `IVirtualFileProvider` with `FileExists`, `OpenStream`, `FileDelete` and the rest, so the same code works on every platform Stride runs on.

## A plain record and JSON

The shortest route: a record with the data, `System.Text.Json` to write it, and the roaming folder to keep it.

```csharp
using System.Text.Json;
using Stride.Core.IO;

public record SaveData(int Level, int Score, Vector3 PlayerPosition);

static class SaveFile
{
    private const string FileName = "save.json";

    public static void Save(SaveData data)
    {
        using var stream = VirtualFileSystem.ApplicationRoaming.OpenStream(FileName, VirtualFileMode.Create, VirtualFileAccess.Write);

        JsonSerializer.Serialize(stream, data);
    }

    public static SaveData? Load()
    {
        if (!VirtualFileSystem.ApplicationRoaming.FileExists(FileName)) return null;

        using var stream = VirtualFileSystem.ApplicationRoaming.OpenStream(FileName, VirtualFileMode.Open, VirtualFileAccess.Read);

        return JsonSerializer.Deserialize<SaveData>(stream);
    }
}
```

`VirtualFileMode.Create` makes the file or empties the one that is there. `Vector3` and the other Stride mathematics types serialize as their fields, so they need no converter.

## A data contract and YAML

A class marked `[DataContract]` is what Game Studio and the asset pipeline serialize, and a YAML file of one is readable and editable by hand, which a settings file wants. `E04_CubeClicker` saves this way through its `DataSaver<TData>` with the NexVYaml package, into the application data folder, and loads it back at start:

```csharp
[DataContract]
public class ClickData
{
    public int Clicks { get; set; }
    public int Best { get; set; }
}

var saver = new DataSaver<ClickData> { Data = new ClickData(), FileName = "clicks.yaml" };

await saver.TryLoadAsync();   // leaves Data as it was when there is no file yet
saver.Data.Clicks++;
await saver.SaveAsync();
```

The example's [DataSaver.cs](https://github.com/stride3d/stride-community-toolkit/blob/main/examples/code-only/E04_CubeClicker/Core/DataSaver.cs) is the whole of it: open the stream on the virtual file system, hand it to the serializer.

## When to save

Save when the player asks, at a checkpoint, and when the game exits: the `Game` raises `Exiting` before it shuts down, which is the last chance to write a file. Keep the data class small and flat, version it with a number field once a release is out, and treat a file that fails to load as "no save" rather than a crash: the player's file may come from an older build.

## See also

- [Engine Patterns](engine-patterns.md)
- [FAQ: How do I save the player's progress?](faq.md#how-do-i-save-the-players-progress)