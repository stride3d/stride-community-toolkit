using Example07_CubeClicker.Core;

namespace Example07_CubeClicker.Managers;

public class ClickDataManager
{
    private const string ClickDataFileName = "StrideExampleClickData.yaml";
    private readonly DataSaver<ClickData> _dataSaver;

    public ClickDataManager()
    {
        _dataSaver = new DataSaver<ClickData>()
        {
            // The default if loading fails so we don't have to deal with null
            Data = ClickData.Default,
            FileName = ClickDataFileName
        };
    }

    public Task<bool> TryLoadAsync() => _dataSaver.TryLoadAsync();

    public void DeleteData()
    {
        _dataSaver.Delete();

        _dataSaver.Data = ClickData.Default;
    }

    public async Task SaveDataAsync() => await _dataSaver.SaveAsync();

    public List<IClickable> GetClickables() => _dataSaver.Data.Clickables;
}