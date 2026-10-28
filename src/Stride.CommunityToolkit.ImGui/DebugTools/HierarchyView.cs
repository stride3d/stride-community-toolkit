using Hexa.NET.ImGui;
using Stride.Core;
using Stride.Engine;
using System.Numerics;
using static Hexa.NET.ImGui.ImGui;
using static Stride.CommunityToolkit.ImGui.ImGuiExtension;
using Guid = System.Guid;

namespace Stride.CommunityToolkit.ImGui.DebugTools;

/// <summary>
/// A window that shows the hierarchy of scenes and entities in the current scene.
/// </summary>
public class HierarchyView : BaseWindow
{
    private const float DummyWidth = 19;
    private const float Indentation = DummyWidth + 8;

    // Ids rather than references, so the view never keeps an entity alive
    private readonly HashSet<Guid> _recursingThrough = [];
    private readonly List<IIdentifiable> _searchResult = [];
    private string _searchTerm = "";

    /// <summary>
    /// Creates the window and registers it with the game's systems; it draws from the next frame on.
    /// </summary>
    /// <param name="service">The game's service registry, which must already contain an <see cref="ImGuiSystem"/>.</param>
    public HierarchyView(IServiceRegistry service) : base(service) { }

    /// <inheritdoc />
    protected override void OnDraw(bool collapsed)
    {
        if (collapsed) return;

        if (InputText("Search", ref _searchTerm, 64))
        {
            _searchResult.Clear();

            if (!string.IsNullOrWhiteSpace(_searchTerm)) RecursiveSearch(_searchResult, _searchTerm, Game.SceneSystem.SceneInstance.RootScene);
        }

        using (Child())
        {
            foreach (var identifiable in _searchResult) RecursiveDrawing(identifiable);

            if (_searchResult.Count > 0)
            {
                Spacing();
                Spacing();
            }

            DrawChildren(Game.SceneSystem.SceneInstance.RootScene);
        }
    }

    /// <inheritdoc />
    protected override void OnDestroy() { }

    private static void RecursiveSearch(List<IIdentifiable> result, string term, IIdentifiable source)
    {
        foreach (var child in EnumerateChildren(source)) RecursiveSearch(result, term, child);

        var name = source switch { Entity entity => entity.Name, Scene scene => scene.Name, _ => null };

        if (name is null) return;

        if (name.Contains(term, StringComparison.OrdinalIgnoreCase) || term.Contains(name, StringComparison.OrdinalIgnoreCase)) result.Add(source);
    }

    private void RecursiveDrawing(IIdentifiable source)
    {
        var (label, canRecurse) = source switch
        {
            Entity entity => (entity.Name, entity.Transform.Children.Count > 0),
            Scene scene => (scene.Name, scene.Children.Count > 0 || scene.Entities.Count > 0),
            _ => (default(string), false),
        };

        if (label is null) return;

        using (ID(source.Id.GetHashCode()))
        {
            var recurse = canRecurse && _recursingThrough.Contains(source.Id);

            if (!canRecurse)
            {
                Dummy(new Vector2(DummyWidth, 1));
            }
            else if (ArrowButton("", recurse ? ImGuiDir.Down : ImGuiDir.Right) && !_recursingThrough.Remove(source.Id))
            {
                _recursingThrough.Add(source.Id);
            }

            SameLine();

            if (Button(label)) Inspector.FindFreeInspector(Services).Target = source;

            using (UIndent(Indentation))
            {
                if (recurse) DrawChildren(source);
            }
        }
    }

    // Spelled out per type rather than through the iterator, so drawing an open tree allocates nothing per node
    private void DrawChildren(IIdentifiable source)
    {
        switch (source)
        {
            case Entity entity:
                foreach (var child in entity.Transform.Children) RecursiveDrawing(child.Entity);
                break;
            case Scene scene:
                foreach (var entity in scene.Entities) RecursiveDrawing(entity);
                foreach (var child in scene.Children) RecursiveDrawing(child);
                break;
        }
    }

    private static IEnumerable<IIdentifiable> EnumerateChildren(IIdentifiable source)
    {
        if (source is Entity entity)
        {
            foreach (var child in entity.Transform.Children) yield return child.Entity;
        }
        else if (source is Scene scene)
        {
            foreach (var childEntity in scene.Entities) yield return childEntity;
            foreach (var childScene in scene.Children) yield return childScene;
        }
    }
}