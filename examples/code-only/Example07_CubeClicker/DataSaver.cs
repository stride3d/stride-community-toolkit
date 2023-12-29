using NexVYaml.Serialization;
using Stride.Core.IO;

namespace Example07_CubeClicker;

public class DataSaver<T>
{
    /// <summary>
    /// The Data which should be covered for saving and deleting and loading
    /// Only Objects with direct [DataContract] Attribute work.
    /// </summary>
    public required T Data { get; set; }
    private const string SaveDataFileName = "StrideExampleCubeSaver.yaml";

    public async Task SaveAsync()
    {
        // Create a new file per default
        var fileMode = VirtualFileMode.CreateNew;

        // if it exists allready, empty the file to write the new content to it
        if (VirtualFileSystem.ApplicationData.FileExists(SaveDataFileName))
        {
            fileMode = VirtualFileMode.Truncate;
        }

        // open a virtual filestream to the target path
        // data are saved in this location: \bin\Debug\net8.0\data\
        await using var fileStream = VirtualFileSystem.ApplicationData.OpenStream(SaveDataFileName, fileMode, VirtualFileAccess.Write);

        // serialize the object to the stream
        YamlSerializer.Serialize(Data, fileStream);
    }

    public void Delete()
    {
        VirtualFileSystem.ApplicationData.FileDelete(SaveDataFileName);
    }

    public async Task<bool> TryLoadAsync()
    {
        if (VirtualFileSystem.ApplicationData.FileExists(SaveDataFileName))
        {
            // open a stream to the save path
            await using var fileStream = VirtualFileSystem.ApplicationData.OpenStream(SaveDataFileName, VirtualFileMode.Open, VirtualFileAccess.Read);

            // await the deserialization of the object in the yaml file
            Data = await YamlSerializer.DeserializeAsync<T>(fileStream);

            return true;
        }

        return false;
    }
}