using E04_CubeClicker.Core;

namespace E04_CubeClicker.Managers;

/// <summary>
/// The click counts as the game uses them, kept in a YAML file through <see cref="DataSaver{TData}"/>.
/// It owns the file name and the default counts, which come back when the save is deleted, so the
/// rest of the game asks for the counts and never deals with the file.
/// </summary>
public class ClickDataManager
{
    // The default data stands until a load replaces it, so there is never a null to deal with
    private readonly DataSaver<ClickData> _dataSaver = new() { Data = ClickData.Default, FileName = "StrideExampleClickData.yaml" };

    public Task<bool> TryLoadAsync() => _dataSaver.TryLoadAsync();

    public void DeleteData()
    {
        _dataSaver.Delete();

        _dataSaver.Data = ClickData.Default;
    }

    public Task SaveDataAsync() => _dataSaver.SaveAsync();

    public List<IClickable> GetClickables() => _dataSaver.Data.Clickables;
}