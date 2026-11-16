using E04_CubeClicker.Core;
using Stride.Core.Mathematics;

namespace E04_CubeClicker.Managers;

/// <summary>
/// The cube positions as the game uses them, kept in a YAML file through <see cref="DataSaver{TData}"/>.
/// The file holds <see cref="SimpleVector"/> values; this class converts them to and from
/// <see cref="Vector3"/>, so the rest of the game never sees the saved form.
/// </summary>
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