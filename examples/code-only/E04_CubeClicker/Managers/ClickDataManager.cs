using E04_CubeClicker.Core;

namespace E04_CubeClicker.Managers;

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