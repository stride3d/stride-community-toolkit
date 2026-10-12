using Stride.Audio;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Input;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// The engine systems a <see cref="Game"/> exposes as properties, reached through the service registry so that every
/// toolkit helper can take an <see cref="IGame"/>: the type a script's <c>Game</c> property has.
/// </summary>
/// <remarks>
/// <see cref="Game"/> registers each of these in <see cref="IGame.Services"/> when it is constructed, so on a game they
/// return the same objects as the properties. A host that registered none throws with the service's name rather than
/// failing later on a null.
/// </remarks>
public static class GameServiceExtensions
{
    /// <summary>The scene system: the root scene and the graphics compositor.</summary>
    public static SceneSystem GetSceneSystem(this IGame game) => Require<SceneSystem>(game);

    /// <summary>The script system, which schedules the scripts and the toolkit's root script.</summary>
    public static ScriptSystem GetScriptSystem(this IGame game) => Require<ScriptSystem>(game);

    /// <summary>The audio system.</summary>
    public static AudioSystem GetAudioSystem(this IGame game) => Require<AudioSystem>(game);

    /// <summary>The input manager: keyboard, mouse and the other devices.</summary>
    public static InputManager GetInputManager(this IGame game) => Require<InputManager>(game);

    /// <summary>The graphics device manager, which owns the device's settings and the back buffer.</summary>
    public static IGraphicsDeviceManager GetGraphicsDeviceManager(this IGame game) => Require<IGraphicsDeviceManager>(game);

    private static T Require<T>(IGame game) where T : class
    {
        ArgumentNullException.ThrowIfNull(game);

        return game.Services.GetService<T>()
            ?? throw new InvalidOperationException($"No {typeof(T).Name} is registered on this game's services; a Stride Game registers one when it is constructed.");
    }
}