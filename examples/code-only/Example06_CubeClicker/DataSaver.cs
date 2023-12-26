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
        File.WriteAllText(_savePath,YamlSerializer.SerializeToString(Data));
    }
    public bool TryLoad(out T cubeSaver)
    {
        if(File.Exists(_savePath))
        {
            var yamlContent = File.ReadAllText(_savePath);
            cubeSaver = YamlSerializer.Deserialize<T>(yamlContent);
            return true;
        }
        cubeSaver = Data;
        return false;
    }
}
