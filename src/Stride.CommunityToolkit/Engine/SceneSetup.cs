using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Images;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// What a code-only game needs before its own content, as data: the graphics compositor, the camera, the
/// ground, the physics engine and the engine settings. <see cref="GameExtensions.Run(Game, SceneSetup, Action{Scene}?, Action{Scene, Stride.Games.GameTime}?, Stride.Games.GameContext?)"/>
/// applies it before the <c>start</c> callback, in a fixed order, so the callback holds only the scene's own
/// content.
/// </summary>
/// <remarks>
/// <para>
/// The set is closed: <see cref="SceneSetup3D"/> adds the lighting and the sky, <see cref="SceneSetup2D"/> an
/// orthographic camera and a flat ground, and <see cref="None"/> applies nothing. Every property has a default,
/// so <c>new SceneSetup3D()</c> is the whole bundle and a property changes one thing. A null <see cref="Camera"/>
/// or <see cref="Ground"/> leaves that piece out.
/// </para>
/// <para>
/// A physics package and the skyboxes package add their pieces through extension methods on the setup
/// (<c>UseBepu()</c>, <c>UseSkybox()</c>), which set the typed properties <see cref="Physics"/> and
/// <see cref="SceneSetup3D.Skybox"/>. The setup is a convenience layered on the <c>Game</c> extension methods;
/// every helper also works without it, in a Game Studio project.
/// </para>
/// </remarks>
public abstract class SceneSetup
{
    private protected SceneSetup() { }

    /// <summary>
    /// A setup that adds nothing to the scene: for a game that builds its compositor and scene by hand, and for the
    /// headless tests. What is not scene still applies when set, the physics engine, the game settings, the overlay
    /// and the profiler, so <c>SceneSetup.None.UseBepu()</c> is a hand-built scene whose primitives get Bepu bodies.
    /// A new instance each time, so what one game sets on it is not seen by the next.
    /// </summary>
    public static SceneSetup None => new NoSceneSetup();

    /// <summary>The clear colour of the compositor, or <see langword="null"/> for the compositor's own default.</summary>
    public Color? ClearColor { get; set; }

    /// <summary>The multisampling level of the forward renderer.</summary>
    public MultisampleCount Msaa { get; set; } = MultisampleCount.None;

    /// <summary>The camera on the compositor's first slot, or <see langword="null"/> for no camera.</summary>
    public CameraSetup? Camera { get; set; }

    /// <summary>The ground, or <see langword="null"/> for none. The physics engine gives it a collider when it has one.</summary>
    public GroundSetup? Ground { get; set; }

    /// <summary>
    /// The physics engine that <c>CreatePrimitive</c> and the grounds attach bodies and colliders with. A physics
    /// package sets it (<c>UseBepu()</c>, <c>UseBullet()</c>); <see cref="PhysicsSetupBase.None"/> attaches nothing.
    /// </summary>
    public PhysicsSetupBase Physics { get; set; } = PhysicsSetupBase.None;

    /// <summary>Configures the compositor's post effects, which are otherwise all off but the tone map.</summary>
    public Action<PostProcessingEffects>? PostEffects { get; set; }

    /// <summary>
    /// Configures the engine's <see cref="Stride.Engine.Design.GameSettings"/> before the window is created, through
    /// <see cref="GameSettingsExtensions.UseGameSettings"/>: the graphics profile, the colour space, the window size.
    /// </summary>
    public Action<Stride.Engine.Design.GameSettings>? GameSettings { get; set; }

    /// <summary>Creates the <see cref="Rendering.Text.DebugOverlay"/>, so the first section added shows at once.</summary>
    public bool DebugOverlay { get; set; }

    /// <summary>Adds the frame-time profiler readout.</summary>
    public bool Profiler { get; set; }

    /// <summary>
    /// Puts the instancing render feature in the compositor, so instanced masters draw from the first
    /// frame; <c>UseInstancing()</c> sets it. Without it a master draws as one model and the run loop warns.
    /// </summary>
    public bool Instancing { get; set; }

    /// <summary>What runs before the engine starts: the settings and the physics service, which the start callback may need.</summary>
    internal virtual void Prepare(Game game)
    {
        if (GameSettings is { } configure)
            game.UseGameSettings(configure);

        if (Physics != PhysicsSetupBase.None)
            game.Services.AddService(Physics);
    }

    /// <summary>What runs in the root script, before the start callback, once the root scene exists.</summary>
    internal abstract void Apply(Game game);

    private protected void ApplyCommon(Game game)
    {
        if (Instancing)
            InstancingSupport.Ensure(game.SceneSystem.GraphicsCompositor);

        if (DebugOverlay)
            Rendering.Text.DebugOverlay.GetOrCreate(game);

        if (Profiler)
            game.AddProfiler();
    }

    private sealed class NoSceneSetup : SceneSetup
    {
        internal override void Apply(Game game) => ApplyCommon(game);
    }
}