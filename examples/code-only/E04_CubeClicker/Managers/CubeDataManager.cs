using E04_CubeClicker.Core;
using Stride.Core.Mathematics;

namespace E04_CubeClicker.Managers;

public class CubeDataManager
{
    private readonly DataSaver<CubeData> _dataSaver = new() { Data = new CubeData(), FileName = "StrideExampleCubeData.yaml" };

    public async Task<List<Vector3>> LoadDataAsync()
        => await _dataSaver.TryLoadAsync() ? _dataSaver.Data.CubePositions.ConvertAll(s => new Vector3(s.X, s.Y, s.Z)) : [];

    public void DeleteData() => _dataSaver.Delete();

    public Task SaveDataAsync() => _dataSaver.SaveAsync();

    public void UpdatePositions(List<Vector3> positions)
    {
        _dataSaver.Data.CubePositions.Clear();

        foreach (var position in positions)
        {
            _dataSaver.Data.AddPosition(position);
        }
    }
}