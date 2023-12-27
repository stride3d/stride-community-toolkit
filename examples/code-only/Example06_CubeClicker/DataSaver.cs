using NexVYaml.Serialization;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Example06_SaveTheCube;
[DataContract]
public class DataSaver<T>
{
    /// <summary>
    /// The Data which should be covered for saving and deleting and loading
    /// Only Objects with direct [DataContract] Attribute work.
    /// </summary>
    public required T Data { get; set; }
    /// <summary>
    /// The path to store the data to, can be a parameter
    /// </summary>
    string _savePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "StrideExampleCubeSaver.yaml";
    public void Save()
    {
        // Create a new file per default
        FileMode mode = FileMode.CreateNew;
        // if it exists allready, empty the file to write the new content to it
        if (File.Exists(_savePath))
            mode = FileMode.Truncate;
        // open a filestream to the target path
        using FileStream fileStream = new FileStream(_savePath, mode, FileAccess.Write);
        // serialize the object to the stream
        YamlSerializer.Serialize(Data,fileStream);
    }
    public void Delete()
    {
        File.Delete(_savePath);
    }
    public async Task<T> TryLoad()
    {
        // if the file exists, get it
        if(File.Exists(_savePath))
        {
            // open a stream to the save path
            using FileStream fileStream = new FileStream(_savePath, FileMode.Open, FileAccess.Read);
            // read the content, this is simplified,
            // ReadonlyMemory<byte> etc. would also work with other streams for better performance
            var yamlContent = File.ReadAllText(_savePath);
            // await the deserialization of the object in the yaml file
            return await YamlSerializer.DeserializeAsync<T>(fileStream);
        }
        // fallback option if the file doesn't exist, so we don't run into a null
        return Data;
    }
}
