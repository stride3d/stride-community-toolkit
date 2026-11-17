using Stride.CommunityToolkit.Rendering.Compositing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Font;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for the <see cref="Game"/> class to simplify common game setup tasks,
/// such as adding cameras, lights, and ground entities, as well as configuring scenes
/// and running the game with custom logic.
/// </summary>
public static partial class GameExtensions
{
    /// <summary>
    /// Adds a default graphics compositor with post-processing effects enabled to the specified game.
    /// </summary>
    /// <param name="game">The game to which the graphics compositor will be added. Cannot be null.</param>
    /// <param name="clearColor">The color used to clear the screen. Defaults to <see cref="Color.CornflowerBlue"/> if not specified.</param>
    /// <param name="msaa">Multisample anti-aliasing level. Defaults to <see cref="MultisampleCount.None"/>; use <see cref="MultisampleCount.X4"/>
    /// when the scene draws thin geometry such as line meshes, which otherwise flickers as it moves. Clamped to what the device supports.</param>
    /// <returns>The newly created <see cref="GraphicsCompositor"/> with post-processing effects enabled.</returns>
    public static GraphicsCompositor AddGraphicsCompositor(this IGame game, Color? clearColor = null, MultisampleCount msaa = MultisampleCount.None)
    {
        var graphicsCompositor = GraphicsCompositorHelper.CreateDefault(enablePostEffects: true, clearColor: clearColor);

        if (graphicsCompositor.SingleView is ForwardRenderer forwardRenderer)
        {
            forwardRenderer.MSAALevel = msaa;
        }

        game.GetSceneSystem().GraphicsCompositor = graphicsCompositor;

        return graphicsCompositor;
    }

    /// <summary>
    /// Adds a 2D graphics compositor to the specified game, optionally setting a clear color.
    /// </summary>
    /// <remarks>This method sets the graphics compositor of the game's scene system to a default 2D
    /// configuration without post-processing effects. The clear color can be specified to customize the background
    /// color of the rendered scene.</remarks>
    /// <param name="game">The game to which the 2D graphics compositor will be added. Cannot be null.</param>
    /// <param name="clearColor">The color used to clear the screen. Defaults to <see cref="Color.CornflowerBlue"/> if not specified.</param>
    /// <param name="msaa">Multisample anti-aliasing level. Defaults to <see cref="MultisampleCount.None"/>; use <see cref="MultisampleCount.X4"/>
    /// when the scene draws thin geometry such as line meshes, which otherwise flickers as it moves. Clamped to what the device supports.</param>
    /// <returns>The newly created 2D graphics compositor.</returns>
    public static GraphicsCompositor Add2DGraphicsCompositor(this IGame game, Color? clearColor = null, MultisampleCount msaa = MultisampleCount.None)
    {
        var graphicsCompositor = GraphicsCompositorHelper2D.CreateDefault(clearColor, msaa);

        game.GetSceneSystem().GraphicsCompositor = graphicsCompositor;

        return graphicsCompositor;
    }

    /// <summary>
    /// Sets the size the window opens at, in pixels. Call it before <c>Run</c>; once the device exists
    /// the window is the user's to resize.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The engine's default is 1280 by 720. An example whose scene is drawn in world units under a
    /// pixel-sized overlay - the easing sheet, with its twelve-line help block - gets more room from a
    /// taller window than from a smaller scene, so it opens a step larger. On a scaled display the
    /// window still opens at this many physical pixels: the size is the back buffer's, not the desktop's.
    /// </para>
    /// <para>
    /// The engine creates the window at the device manager's preferred back-buffer size, which its
    /// <c>PrepareContext</c> resets to the default just before - with <see cref="Game.AutoLoadDefaultSettings"/>
    /// on, which it is by default. So neither a <see cref="GameContext"/> with a requested size nor
    /// <c>PreferredBackBufferWidth</c> set before <c>Run</c> survives, and <c>RenderingSettings</c> put
    /// back afterwards are clamped to the window already made: that route can shrink a window but never
    /// grow it. Resizing the window later - at <see cref="GameBase.WindowCreated"/>, or from a startup
    /// script with <c>game.Window.SetSize</c> as the Stride manual shows - works, but the window has
    /// been resized after its handle was made and shows the old outline for its first few milliseconds.
    /// This turns <see cref="Game.AutoLoadDefaultSettings"/> off instead, so the size set through
    /// <see cref="GameSettingsExtensions.UseGameSettings"/> stands and the window is created at it.
    /// What the flag would otherwise have supplied from the same defaults - the graphics profile and
    /// the colour space - <c>UseGameSettings</c> applies too, so nothing else changes. An example that
    /// already mirrors a Game Studio project's settings can keep doing that for everything else; this
    /// is the one line for the window.
    /// </para>
    /// </remarks>
    /// <param name="game">The game whose window to size.</param>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <exception cref="InvalidOperationException">Thrown when the game is already running.</exception>
    public static void SetWindowSize(this Game game, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        // Off, PrepareContext leaves the device manager alone, the window is created at this size and
        // the engine's clamp to the window (which could only shrink) is skipped. UseGameSettings applies
        // the profile and colour space the flag would have set, from the same defaults.
        game.AutoLoadDefaultSettings = false;

        game.UseGameSettings(settings =>
        {
            var rendering = settings.GetOrCreateConfiguration<RenderingSettings>();

            rendering.DefaultBackBufferWidth = width;
            rendering.DefaultBackBufferHeight = height;
        });
    }

    /// <summary>
    /// Adds a ground gizmo to the game's root scene, attached to an existing ground entity.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> instance in which the ground gizmo will be added.</param>
    /// <param name="position">
    /// The position of the gizmo in 3D space. If null, the gizmo will be placed at the origin (0, 0, 0).
    /// </param>
    /// <param name="showAxisName">
    /// A boolean indicating whether the axis names (X, Y, Z) should be displayed on the gizmo. Default is false.
    /// </param>
    /// <param name="rotateAxisNames">
    /// A boolean indicating whether the axis names should rotate to always face the camera. Default is true.
    /// </param>
    /// <returns>The gizmo entity, a child of the ground, or <see langword="null"/> when there is no ground entity.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="showAxisName"/> is set and the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    /// <remarks>
    /// The gizmo is added as a child to an existing ground entity. If the ground entity is not found, the method will return without adding the gizmo.
    /// The gizmo helps visualize the ground with axis indicators in 3D space.
    /// </remarks>
    public static Entity? AddGroundGizmo(this IGame game, Vector3? position = null, bool showAxisName = false, bool rotateAxisNames = true)
    {
        var groundEntity = game.GetSceneSystem().SceneInstance.RootScene.FindEntity(GameDefaults.DefaultGroundName);

        if (groundEntity is null) return null;

        // The axis letters are world-space text, so the renderer that draws them has to be present.
        // Doing it here rather than leaving it to the caller keeps this a single call - and without it
        // the letters simply never appear, with no error to explain why.
        if (showAxisName)
            game.AddWorldTextRenderer();

        var gizmoEntity = new Entity("Gizmo");

        gizmoEntity.AddGizmo(game.GraphicsDevice, showAxisName: showAxisName, rotateAxisNames: rotateAxisNames);

        gizmoEntity.Transform.Position = position ?? Vector3.Zero;

        groundEntity.AddChild(gizmoEntity);

        return gizmoEntity;
    }

    /// <summary>
    /// Adds the <see cref="GameProfiler"/> script to the root scene, on its own entity, and turns it on. Ctrl+Shift+P
    /// toggles it; the other keys are Ctrl chords, listed on the <see cref="GameProfiler"/> class and behind its F1.
    /// </summary>
    /// <param name="game">The game to which the profiler will be added.</param>
    /// <param name="entityName">The name of the entity the script is attached to, when one has to be created.</param>
    /// <param name="enabled">Whether profiling starts on. <see langword="false"/> adds the script waiting for Ctrl+Shift+P.</param>
    /// <returns>The entity holding the <see cref="GameProfiler"/> script.</returns>
    /// <remarks>
    /// Every <see cref="SceneSetup"/> adds the profiler, off unless <see cref="SceneSetup.Profiler"/> is set, so Ctrl+Shift+P
    /// works in any scene. A root scene that already has a <see cref="GameProfiler"/> gets no second one: the call sets
    /// <paramref name="enabled"/> on the one there and returns its entity.
    /// </remarks>
    public static Entity AddProfiler(this IGame game, string? entityName = "Game Profiler", bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(game);

        var rootScene = game.GetSceneSystem().SceneInstance.RootScene;

        foreach (var existing in rootScene.Entities)
        {
            if (existing.Get<GameProfiler>() is { } profiler)
            {
                profiler.Enabled = enabled;

                return existing;
            }
        }

        var entity = new Entity(entityName) { new GameProfiler { Enabled = enabled } };

        entity.Scene = rootScene;

        return entity;
    }

    /// <summary>
    /// Saves a screenshot of the current frame to the specified file path.
    /// </summary>
    /// <param name="game">The game instance providing the current graphics context and render target.</param>
    /// <param name="fileName">The file path where the screenshot will be saved.</param>
    /// <param name="fileType">The image file format to use when saving the screenshot.</param>
    public static void TakeScreenshot(this IGame game, string fileName, ImageFileType fileType = ImageFileType.Png)
    {
        using var stream = File.Create(fileName);

        var commandList = game.GraphicsContext.CommandList;
        commandList.RenderTarget.Save(commandList, stream, fileType);
    }

    /// <summary>
    /// Adds a scene renderer to the game's <see cref="GraphicsCompositor"/>.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> instance to add the renderer to.</param>
    /// <param name="renderer">The scene renderer to be added, inheriting from <see cref="SceneRendererBase"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="renderer"/> is null.</exception>
    /// <returns>The game, so a chain of setup calls continues.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    public static IGame AddSceneRenderer(this IGame game, SceneRendererBase renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        game.RequireGraphicsCompositor().AddSceneRenderer(renderer);

        return game;
    }

    /// <summary>The game's compositor, which the scene setup or the caller must have put in place.</summary>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    private static GraphicsCompositor RequireGraphicsCompositor(this IGame game)
        => game.GetSceneSystem().GraphicsCompositor ?? throw new InvalidOperationException(GameDefaults.GraphicsCompositorNotSet);

    /// <summary>
    /// Registers the renderer that draws <see cref="WorldTextComponent"/>, once, however many times
    /// this is called.
    /// </summary>
    /// <param name="game">The game whose compositor the renderer is added to.</param>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    /// <remarks>
    /// The <see cref="WorldTextProcessor"/> adds the renderer itself when a component needs one, so
    /// this call is for keeping a reference to it or adding it ahead of the first component. Helpers
    /// that create world text themselves, such as <see cref="AddGroundGizmo"/> with axis names, call
    /// it; calling it again is harmless, because a duplicate renderer is not added.
    /// </remarks>
    /// <returns>The renderer: the one already in the compositor when there was one.</returns>
    public static WorldTextRenderer AddWorldTextRenderer(this IGame game)
        => game.RequireGraphicsCompositor().EnsureSceneRenderer(() => new WorldTextRenderer());

    /// <summary>
    /// Registers the renderer that draws <see cref="EntityTextComponent"/>, once, however many times
    /// this is called.
    /// </summary>
    /// <param name="game">The game whose compositor the renderer is added to.</param>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    /// <remarks>
    /// The screen-space counterpart of <see cref="AddWorldTextRenderer"/>; the <see cref="EntityTextProcessor"/>
    /// adds the renderer itself when a component needs one.
    /// </remarks>
    /// <returns>The renderer: the one already in the compositor when there was one.</returns>
    public static EntityTextRenderer AddEntityTextRenderer(this IGame game)
        => game.RequireGraphicsCompositor().EnsureSceneRenderer(() => new EntityTextRenderer());

    /// <summary>
    /// Loads a font installed on the machine, ready to hand to any component that takes a
    /// <see cref="SpriteFont"/>.
    /// </summary>
    /// <param name="game">The game whose services provide Stride's font system.</param>
    /// <param name="family">The family name as the system knows it, such as <c>"Segoe UI"</c>.</param>
    /// <param name="size">The default size in pixels, used when a draw does not pass one; glyphs are rasterised at the size they are drawn at.</param>
    /// <param name="style">The weight and slant wanted. Defaults to <see cref="FontStyle.Regular"/>.</param>
    /// <param name="fontFile">A TrueType file to load the family from, for fonts outside the system font folders.</param>
    /// <returns>The font, or <see langword="null"/> if the family is not installed.</returns>
    /// <remarks>
    /// A code-only game has no compiled font assets, so this is how it gets any font other than
    /// Stride's default one. Load each font once and keep it - see <see cref="SystemFonts"/>, which
    /// this forwards to and which can also try a list of families in order.
    /// </remarks>
    public static SpriteFont? LoadSystemFont(this IGame game, string family, float size, FontStyle style = FontStyle.Regular, string? fontFile = null)
    {
        ArgumentNullException.ThrowIfNull(game);

        return SystemFonts.Load(game.Services, family, size, style, fontFile);
    }

    /// <summary>
    /// Adds a root render feature to the game's graphics compositor.
    /// </summary>
    /// <param name="game">The game instance to which the render feature will be added. Cannot be null.</param>
    /// <param name="renderFeature">The root render feature to add. Cannot be null.</param>
    /// <returns>The game, so a chain of setup calls continues.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    public static IGame AddRootRenderFeature(this IGame game, RootRenderFeature renderFeature)
    {
        game.RequireGraphicsCompositor().AddRootRenderFeature(renderFeature);

        return game;
    }

    /// <summary>
    /// Adds particle rendering capabilities to the specified game.
    /// </summary>
    /// <remarks>This method extends the game's graphics compositor by incorporating stages and features
    /// necessary for rendering particles. Ensure that the game has a valid scene system before invoking this
    /// method.</remarks>
    /// <param name="game">The game to which particle rendering stages and features will be added. Cannot be null.</param>
    /// <returns>The game, so a chain of setup calls continues.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="GraphicsCompositor"/> is not set in the game's <see cref="SceneSystem"/>.</exception>
    public static IGame AddParticleRenderer(this IGame game)
    {
        game.RequireGraphicsCompositor().AddParticleStagesAndFeatures();

        return game;
    }

    /// <summary>
    /// Creates an entity with the procedural model of a 3D <paramref name="shape"/>.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance used to access game services.</param>
    /// <param name="shape">The shape and its dimensions, from the <see cref="Primitive3D"/> factories, such as <c>Primitive3D.Sphere(0.5f)</c>.</param>
    /// <param name="options">Optional creation parameters: material, render group, entity name and position. If <see langword="null"/>, default options are used.</param>
    /// <returns>A new <see cref="Entity"/> with a <see cref="ModelComponent"/> containing the generated primitive model.</returns>
    /// <remarks>
    /// <para>The returned entity is not added to a scene automatically. Assign it to a scene before rendering.</para>
    /// <para>If a material is specified in <paramref name="options"/>, it is added to the generated model's material collection.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> or <paramref name="shape"/> is <see langword="null"/>.</exception>
    public static Entity CreatePrimitive(this IGame game, Primitive3D shape, PrimitiveEntityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(shape);

        options ??= new();

        var entity = CreatePrimitiveEntity(GameModelExtensions.ModelFor(game, shape, options), options);

        // The physics engine the setup chose, if any; the core alone attaches nothing
        if (options.Body != BodyMode.None)
            PhysicsSetupBase.Of(game).AddBody(entity, shape, options);

        return entity;
    }

    /// <summary>
    /// Creates an entity with the procedural model of a flat 2D <paramref name="shape"/> in the XY plane.
    /// </summary>
    /// <param name="game">The <see cref="IGame"/> instance used to access game services.</param>
    /// <param name="shape">The shape and its dimensions, from the <see cref="Primitive2D"/> factories, such as <c>Primitive2D.Circle(0.5f)</c>.</param>
    /// <param name="options">Optional creation parameters: material, render group, entity name and position; the collider depth is the shape's <see cref="Primitive2D.Depth"/>. If <see langword="null"/>, default options are used.</param>
    /// <returns>A new <see cref="Entity"/> with a <see cref="ModelComponent"/> containing the generated primitive model.</returns>
    /// <remarks>
    /// <para>The returned entity is not added to a scene automatically. Assign it to a scene before rendering.</para>
    /// <para>If a material is specified in <paramref name="options"/>, it is added to the generated model's material collection.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="game"/> or <paramref name="shape"/> is <see langword="null"/>.</exception>
    public static Entity CreatePrimitive(this IGame game, Primitive2D shape, PrimitiveEntityOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(shape);

        options ??= new();

        var entity = CreatePrimitiveEntity(GameModelExtensions.ModelFor(game, shape, options), options);

        if (options.Body != BodyMode.None)
            PhysicsSetupBase.Of(game).AddBody(entity, shape, options);

        return entity;
    }

    /// <summary>The entity of a model with the options' name, render group and position; no physics yet.</summary>
    private static Entity CreatePrimitiveEntity(Model model, PrimitiveEntityOptions options)
    {
        var entity = new Entity(options.EntityName) { new ModelComponent(model) { RenderGroup = options.RenderGroup } };

        if (options.Position is { } position)
            entity.Transform.Position = position;

        return entity;
    }
}