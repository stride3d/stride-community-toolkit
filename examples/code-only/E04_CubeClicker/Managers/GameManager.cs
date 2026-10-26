using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.UI.Events;

namespace E04_CubeClicker.Managers;

public class GameManager
{
    private readonly CubeDataManager _cubeDataManager = new();
    private readonly ClickDataManager _clickDataManager = new();
    private readonly UIManager _uiManager;

    public bool ReloadCubes { get; set; }

    public GameManager(SpriteFont font)
    {
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
        _uiManager.UpdateMessage(await LoadClickDataAsync() ? "Data loaded. Start clicking." : "No click data found.");

        ReloadCubes = true;
    }

    public async Task<bool> LoadClickDataAsync()
    {
        try
        {
            var loaded = await _clickDataManager.TryLoadAsync();

            _uiManager.UpdateClickTextBlocks(_clickDataManager.GetClickables());

            return loaded;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during load operation: {ex.Message}");

            return false;
        }
    }

    public async Task<List<Vector3>> LoadCubeDataAsync()
    {
        try
        {
            return await _cubeDataManager.LoadDataAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during load operation: {ex.Message}");

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