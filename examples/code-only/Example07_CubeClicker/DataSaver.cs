using NexVYaml.Serialization;
using Stride.Core;
using Stride.Core.IO;

namespace Example06_SaveTheCube;
[DataContract]
public class DataSaver<T>
{
    /// <summary>
    /// The Data which should be covered for saving and deleting and loading
    /// Only Objects with direct [DataContract] Attribute work.
    /// </summary>
    public required T Data { get; set; }
    private const string SaveDataFileName = "StrideExampleCubeSaver.yaml";
    public void Save()
    {
        
        // Create a new file per default
        VirtualFileMode mode = VirtualFileMode.CreateNew;
        // if it exists allready, empty the file to write the new content to it
        if (VirtualFileSystem.ApplicationData.FileExists(SaveDataFileName))
            mode = VirtualFileMode.Truncate;
        // open a virtual filestream to the target path
        using var fileStream = VirtualFileSystem.ApplicationData.OpenStream(SaveDataFileName, mode,VirtualFileAccess.Write);
        
        // serialize the object to the stream
        YamlSerializer.Serialize(Data,fileStream);
    }
    public void Delete()
    {
        VirtualFileSystem.ApplicationData.FileDelete(SaveDataFileName);
    }
    public async Task<T> TryLoad()
    {
        // if the file exists, get it
        if(VirtualFileSystem.ApplicationData.FileExists(SaveDataFileName))
        {
            // open a stream to the save path
            using var fileStream = VirtualFileSystem.ApplicationData.OpenStream(SaveDataFileName, VirtualFileMode.Open, VirtualFileAccess.Read);
            // await the deserialization of the object in the yaml file
            return await YamlSerializer.DeserializeAsync<T>(fileStream);
        }
        // fallback option if the file doesn't exist, so we don't run into a null
        return Data;
    }
}
