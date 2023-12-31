using Example07_CubeClicker.Core;
using Stride.Core.Mathematics;

namespace Example07_CubeClicker;

public class CubeCollector
{
    private const string CubeDataFileName = "StrideExampleCubeData.yaml";
    private readonly DataSaver<CubeData> _dataSaver;

    public CubeCollector()
    {
        _dataSaver = new DataSaver<CubeData>()
        {
            Data = new CubeData(),
            FileName = CubeDataFileName,
        };
    }

    public async Task<List<Vector3>> LoadCubeDataAsync()
    {
        var isSuccesful = await _dataSaver.TryLoadAsync();

        if (!isSuccesful) return [];

        return _dataSaver.Data.CubePositions.Select(s => new Vector3(s.X, s.Y, s.Z)).ToList();
    }

    public void Delete() => _dataSaver.Delete();

    public async Task SaveCubeDataAsync() => await _dataSaver.SaveAsync();

    internal void UpdatePositions(List<Vector3> positinos)
    {
        _dataSaver.Data.CubePositions.Clear();

        foreach (var position in positinos)
            _dataSaver.Data.AddPosition(position);
    }
}