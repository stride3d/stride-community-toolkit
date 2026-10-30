using NexVYaml;
using Stride.Core.IO;

namespace E04_CubeClicker.Core;

public class DataSaver<TData>
{
    /// <summary>
    /// The object saved and loaded; only types tagged [DataContract] directly work.
    /// </summary>
    public required TData Data { get; set; }
    public required string FileName { get; set; }

    public async Task SaveAsync()
    {
        // A new file, or the existing one emptied; the application data folder is bin\Debug\net10.0\data
        var fileMode = VirtualFileSystem.ApplicationData.FileExists(FileName) ? VirtualFileMode.Truncate : VirtualFileMode.CreateNew;

        await using var fileStream = VirtualFileSystem.ApplicationData.OpenStream(FileName, fileMode, VirtualFileAccess.Write);

        await YamlSerializer.SerializeAsync(Data, fileStream);
    }

    public void Delete() => VirtualFileSystem.ApplicationData.FileDelete(FileName);

    public async Task<bool> TryLoadAsync()
    {
        if (!VirtualFileSystem.ApplicationData.FileExists(FileName)) return false;

        await using var fileStream = VirtualFileSystem.ApplicationData.OpenStream(FileName, VirtualFileMode.Open, VirtualFileAccess.Read);

        Data = await YamlSerializer.DeserializeAsync<TData>(fileStream);

        return true;
    }
}