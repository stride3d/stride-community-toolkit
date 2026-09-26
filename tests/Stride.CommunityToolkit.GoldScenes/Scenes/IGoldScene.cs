using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.GoldScenes.Scenes;

/// <summary>
/// One scene the gold-image harness photographs: built once, drawn every frame, with nothing in it
/// that could differ between two runs at the same frame.
/// </summary>
internal interface IGoldScene
{
    /// <summary>Builds the scene: camera, geometry, renderers.</summary>
    void Start(Game game, Scene scene);

    /// <summary>Submits the frame's immediate-mode drawing; a function of the simulated time only.</summary>
    void Update(Game game, Scene scene, GameTime time);
}