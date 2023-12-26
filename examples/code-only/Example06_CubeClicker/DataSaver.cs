using NexVYaml.Serialization;
using Stride.Engine;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Example06_SaveTheCube;
[DataContract]
public class DataSaver<T>
{
    public required T Data { get; set; }
    string _savePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "StrideExampleCubeSaver.yaml";
    public void Save()
    {
        using FileStream fileStream = new FileStream(_savePath, FileMode.OpenOrCreate, FileAccess.Write);
        fileStream.Write(YamlSerializer.Serialize(this).Span);
    }
    public bool TryLoad(out DataSaver<T> cubeSaver)
    {
        if(File.Exists(_savePath))
        {
            var yamlContent = File.ReadAllText(_savePath);
            cubeSaver = YamlSerializer.Deserialize<DataSaver<T>>(yamlContent);
            return true;
        }
        cubeSaver = this;
        return false;
    }
}
