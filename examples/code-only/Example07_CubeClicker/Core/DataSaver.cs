using NexVYaml;
using Stride.Core.IO;

namespace Example07_CubeClicker.Core;

public class DataSaver<TData>
{
    /// <summary>
    /// The Data which should be covered for saving and deleting and loading
    /// Only Objects with direct [DataContract] Attribute work.
    /// </summary>
    public required TData Data { get; set; }
    public required string FileName { get; set; }

    public async Task SaveAsync()
    {
        // Create a new file per default
        var fileMode = VirtualFileMode.CreateNew;

        // if it exists allready, empty the file to write the new content to it
        if (VirtualFileSystem.ApplicationData.FileExists(FileName))
            fileMode = VirtualFileMode.Truncate;

        // open a virtual filestream to the target path
        // data are saved in this location: \bin\Debug\net8.0\data\
        await using var fileStream = VirtualFileSystem.ApplicationData.OpenStream(FileName, fileMode, VirtualFileAccess.Write);

        // serialize the object to the stream
        await YamlSerializer.SerializeAsync(Data, fileStream);
    }

    public void Delete() => VirtualFileSystem.ApplicationData.FileDelete(FileName);

    public async Task<bool> TryLoadAsync()
    {
        if (!VirtualFileSystem.ApplicationData.FileExists(FileName)) return false;

        // open a stream to the save path
        await using var fileStream = VirtualFileSystem.ApplicationData.OpenStream(FileName, VirtualFileMode.Open, VirtualFileAccess.Read);

        // await the deserialization of the object in the yaml file
        Data = await YamlSerializer.DeserializeAsync<TData>(fileStream);

        return true;
    }
}