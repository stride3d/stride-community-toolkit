using Example07_CubeClicker.Core;
using Stride.Core.Mathematics;

namespace Example07_CubeClicker.Managers;

public class CubeDataManager
{
    private const string CubeDataFileName = "StrideExampleCubeData.yaml";
    private readonly DataSaver<CubeData> _dataSaver;

    public CubeDataManager()
    {
        _dataSaver = new DataSaver<CubeData>()
        {
            Data = new CubeData(),
            FileName = CubeDataFileName,
        };
    }

    public async Task<List<Vector3>> LoadDataAsync()
    {
        var isSuccesful = await _dataSaver.TryLoadAsync();

        if (!isSuccesful) return [];

        return _dataSaver.Data.CubePositions.ConvertAll(s => new Vector3(s.X, s.Y, s.Z));
    }

    public void DeleteData() => _dataSaver.Delete();

    public async Task SaveDataAsync() => await _dataSaver.SaveAsync();

    public void UpdatePositions(List<Vector3> positinos)
    {
        _dataSaver.Data.CubePositions.Clear();

        foreach (var position in positinos)
            _dataSaver.Data.AddPosition(position);
    }
}