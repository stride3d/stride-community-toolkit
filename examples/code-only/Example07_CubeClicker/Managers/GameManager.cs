using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.UI.Events;

namespace Example07_CubeClicker.Managers;

public class GameManager
{
    private const string ErrorDuringLoad = "Error during load operation: {0}";

    private readonly CubeDataManager _cubeDataManager;
    private readonly ClickDataManager _clickDataManager;
    private readonly UIManager _uiManager;

    public bool ReloadCubes { get; set; }

    public GameManager(SpriteFont font)
    {
        _cubeDataManager = new CubeDataManager();
        _clickDataManager = new ClickDataManager();
        _uiManager = new UIManager(font, _clickDataManager.GetClickables())
        {
            LoadDataHandler = LoadDataAsync,
            SaveDataHandler = SaveDataAsync,
            DeleteDataHandler = DeleteData
        };
    }

    public Entity CreateUI() => _uiManager.CreateUI();

    public void HandleClick(MouseButton type, List<Vector3> positions)
    {
        _cubeDataManager.UpdatePositions(positions);

        var clickable = _clickDataManager.GetClickables().FirstOrDefault(x => x.Type == type);

        if (clickable is null) return;

        clickable.HandleClick();

        _uiManager.UpdateClickTextBlocks(_clickDataManager.GetClickables());
    }

    private async void LoadDataAsync(object? sender, RoutedEventArgs e)
    {
        if (await LoadClickDataAsync())
        {
            _uiManager.UpdateMessage("Data loaded. Start clicking.");
        }
        else
        {
            _uiManager.UpdateMessage("No click data found.");
        }

        ReloadCubes = true;
    }

    public async Task<bool> LoadClickDataAsync()
    {
        bool result;

        try
        {
            result = await _clickDataManager.TryLoadAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format(ErrorDuringLoad, ex.Message));

            return false;
        }

        _uiManager.UpdateClickTextBlocks(_clickDataManager.GetClickables());

        return result;
    }

    public async Task<List<Vector3>> LoadCubeDataAsync()
    {
        try
        {
            return await _cubeDataManager.LoadDataAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format(ErrorDuringLoad, ex.Message));

            return [];
        }
    }

    private async void SaveDataAsync(object? sender, RoutedEventArgs e)
    {
        try
        {
            await _clickDataManager.SaveDataAsync();
            await _cubeDataManager.SaveDataAsync();

            _uiManager.UpdateMessage("Data saved. Keep clicking.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during save operation: {ex.Message}");
        }
    }

    private void DeleteData(object? sender, RoutedEventArgs e)
    {
        try
        {
            _clickDataManager.DeleteData();
            _cubeDataManager.DeleteData();

            _uiManager.UpdateMessage("Data deleted.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during delete operation: {ex.Message}");
        }

        _uiManager.UpdateClickTextBlocks(_clickDataManager.GetClickables());
    }
}