using Stride.Core.Mathematics;

namespace Example07_CubeClicker.Scripts;

public class CubeCollector
{
    //private const string HitEntityName = "Cube";
    private const string CubeDataFileName = "StrideExampleCubeData.yaml";
    private readonly DataSaver2<CubeData> _dataSaver;

    //internal CubeData cubeData { get; private set; } = new();
    //private List<Entity> _entities = new();

    public CubeCollector()
    {
        _dataSaver = new DataSaver2<CubeData>()
        {
            Data = new CubeData(),
            FileName = CubeDataFileName,
        };
    }

    //public void Add(Entity entity)
    //{
    //    cubeData.AddCube(entity);
    //    _entities.Add(entity);
    //}

    //private void AddCube(Vector3 position)
    //{
    //    var entity = Game.CreatePrimitive(PrimitiveModelType.Cube, HitEntityName);
    //    entity.Transform.Position = new Vector3(position.X, 8, position.Z);
    //    entity.Add(new CubeGrower());
    //    Add(entity);
    //    entity.Scene = Entity.Scene;
    //}

    public async Task<List<Vector3>> LoadCubeDataAsync()
    {
        var isSuccesful = await _dataSaver.TryLoadAsync();

        if (!isSuccesful) return [];

        // delete the existing cubes
        //_entities.ForEach(entity => entity.Remove());

        return _dataSaver.Data.CubePositions.Select(s => new Vector3(s.X, 8, s.Z)).ToList();

        //_dataSaver.Data.CubePositions.ForEach(async position =>
        //{
        //    // create cubes from the loaded data
        //    AddCube(new Vector3() { X = position.X, Y = 8, Z = position.Z });
        //}
        //);

        //return true;
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
        //DataSaver<CubeData> dataSaver = new()
        //{
        //    Data = cubeData
        //};

        await _dataSaver.SaveAsync();
    }

    internal void UpdatePositions(List<Vector3> positinos)
    {
        _dataSaver.Data.CubePositions.Clear();

        foreach (var position in positinos)
        {
            _dataSaver.Data.AddPosition(position);
        }
    }
}