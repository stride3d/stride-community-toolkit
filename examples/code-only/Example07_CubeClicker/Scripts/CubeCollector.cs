using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.ProceduralModels;
using Stride.Input;
using Stride.Rendering;
using Stride.Core.Shaders.Ast;

namespace Example07_CubeClicker.Scripts;
[DataContract]
public class CubeCollector : StartupScript
{
    private const string HitEntityName = "Cube";
    private const string CubeDataFileName = "StrideExampleCubeData.yaml";
    [DataMember]
    internal CubeData cubeData { get; private set; } = new();
    private List<Entity> _entities = new();
    

    public void Add(Entity entity)
    {
        cubeData.AddCube(entity);
        _entities.Add(entity);
    }
    private void AddCube(Vector3 position)
    {
        var entity = Game.CreatePrimitive(PrimitiveModelType.Cube, HitEntityName);
        entity.Transform.Position = new Vector3(position.X, 8, position.Z);
        entity.Add(new CubeGrower());
        Add(entity);
        entity.Scene = Entity.Scene;
    }
    public async Task<bool> LoadCubeDataAsync()
    {
        DataSaver<CubeData> dataSaver = new()
        {
            Data = new CubeData()
        };

        var isSuccesful = await dataSaver.TryLoadAsync(CubeDataFileName);
        if (isSuccesful)
        {
            // delete the existing cubes
            _entities.ForEach(entity => entity.Remove());
            dataSaver.Data.CubePositions.ForEach(async position =>
            {
                // create cubes from the loaded data
                AddCube(new Vector3() { X = position.X, Y = 8, Z = position.Z });
            }
            );
            return true;
        }
        return false;
    }
    public void Delete()
    {
        DataSaver<CubeData> dataSaver = new()
        {
            Data = new CubeData()
        };
        dataSaver.Delete(CubeDataFileName);
    }
    public async Task SaveCubeDataAsync()
    {
        DataSaver<CubeData> dataSaver = new()
        {
            Data = new CubeData()
        };
        await dataSaver.SaveAsync(CubeDataFileName);
    }
}
