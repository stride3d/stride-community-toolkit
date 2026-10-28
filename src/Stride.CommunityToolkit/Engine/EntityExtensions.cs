using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Gizmos;
using Stride.CommunityToolkit.Scripts;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using System.Diagnostics.CodeAnalysis;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for the <see cref="Entity"/> class to simplify common operations,
/// such as adding camera controllers, gizmos, and retrieving or manipulating components.
/// </summary>
public static partial class EntityExtensions
{
    /// <summary>
    /// Adds an interactive camera script <see cref="Basic3DCameraController"/> to the specified entity,
    /// enabling camera movement and rotation through various input methods.
    /// </summary>
    /// <param name="entity">The entity to which the interactive camera script will be added.</param>
    /// <param name="helpPosition">Where the shared <see cref="Rendering.Overlay.TextOverlay"/> is drawn. <see langword="null"/>, the default,
    /// leaves the overlay's own position alone.</param>
    /// <remarks>
    /// The camera entity can be moved using W, A, S, D, Q and E, arrow keys, a gamepad's left stick
    /// or by dragging/scaling using multitouch. Rotation is achieved using the Numpad, the mouse while holding
    /// the right mouse button, a gamepad's right stick, or by dragging using single-touch.
    /// </remarks>
    /// <param name="helpToggleKey">The key that collapses and expands the camera's help.</param>
    /// <param name="helpCollapsed">Whether the help starts collapsed to a single reminder line. Collapsed by default.</param>
    /// <param name="showHelp"><see langword="false"/> keeps the controller but puts no key help on the overlay.</param>
    /// <returns>The controller, already on the entity.</returns>
    public static Basic3DCameraController Add3DCameraController(this Entity entity,
        Anchor? helpPosition = null,
        Keys helpToggleKey = Keys.F2,
        bool helpCollapsed = true,
        bool showHelp = true)
    {
        var controller = new Basic3DCameraController(helpPosition)
        {
            HelpToggleKey = helpToggleKey,
            HelpCollapsed = helpCollapsed,
            ShowHelp = showHelp
        };

        entity.Add(controller);

        return controller;
    }

    /// <summary>
    /// Adds a <see cref="Basic2DCameraController"/> script to the entity enabling panning and zooming
    /// interactions suitable for orthographic 2D scenes.
    /// </summary>
    /// <param name="entity">The camera entity that will receive the controller script.</param>
    /// <param name="helpToggleKey">The key that collapses and expands the camera's help.</param>
    /// <param name="helpCollapsed">Whether the help starts collapsed to a single reminder line. Collapsed by default.</param>
    /// <returns>The controller, already on the entity.</returns>
    public static Basic2DCameraController Add2DCameraController(this Entity entity,
        Keys helpToggleKey = Keys.F2,
        bool helpCollapsed = true)
    {
        var controller = new Basic2DCameraController
        {
            HelpToggleKey = helpToggleKey,
            HelpCollapsed = helpCollapsed
        };

        entity.Add(controller);

        return controller;
    }

    /// <summary>
    /// Adds a TranslationGizmo to the specified entity with optional custom colors.
    /// </summary>
    /// <param name="entity">The entity to which the gizmo will be added.</param>
    /// <param name="graphicsDevice">The graphics device used for rendering the gizmo.</param>
    /// <param name="redColor">Optional custom color for the X-axis of the gizmo. If not specified, a default color is used.</param>
    /// <param name="greenColor">Optional custom color for the Y-axis of the gizmo. If not specified, a default color is used.</param>
    /// <param name="blueColor">Optional custom color for the Z-axis of the gizmo. If not specified, a default color is used.</param>
    /// <param name="showAxisName">Optional flag to show axis names. Default is false.</param>
    /// <param name="rotateAxisNames">Optional flag to rotate axis names. Default is true.</param>
    /// <returns>The gizmo, whose entity is already a child of <paramref name="entity"/>.</returns>
    /// <example>
    /// This example shows how to add a gizmo to an entity with the default colors:
    /// <code>
    /// var entity = new Entity();
    /// // Assume 'game' is an existing Game instance
    /// entity.AddGizmo(game.GraphicsDevice);
    /// </code>
    /// </example>
    public static TranslationGizmo AddGizmo(this Entity entity, GraphicsDevice graphicsDevice, Color? redColor = null, Color? greenColor = null, Color? blueColor = null, bool showAxisName = false, bool rotateAxisNames = true)
    {
        var gizmo = new TranslationGizmo(graphicsDevice, redColor, greenColor, blueColor);

        gizmo.Create(entity, showAxisName, rotateAxisNames);

        return gizmo;
    }

    /// <summary>
    /// Adds a gizmo that shows a directional light's direction at runtime.
    /// </summary>
    /// <param name="entity">The entity to which the directional light gizmo will be added.</param>
    /// <param name="graphicsDevice">The graphics device used to render the gizmo.</param>
    /// <param name="color">Optional color for the gizmo. If not specified, a default color is used.</param>
    /// <returns>The gizmo, whose entity is already a child of <paramref name="entity"/>.</returns>
    /// <remarks>
    /// This method is useful for visually representing the direction and orientation of a directional light in a scene.
    /// The gizmo can be used during runtime to provide a visual reference for the light's direction.
    /// </remarks>
    /// <example>
    /// This example shows how to add a light directional gizmo to an entity with the default colors:
    /// <code>
    /// var entity = new Entity();
    /// // Assume 'game' is an existing Game instance
    /// entity.AddLightDirectionalGizmo(game.GraphicsDevice);
    /// </code>
    /// </example>
    public static LightDirectionalGizmo AddLightDirectionalGizmo(this Entity entity, GraphicsDevice graphicsDevice, Color? color = null)
    {
        var gizmo = new LightDirectionalGizmo(graphicsDevice, color);

        gizmo.Create(entity);

        return gizmo;
    }

    /// <summary>
    /// The first component of type <typeparamref name="T"/> on the entity, or <see langword="null"/>: Stride's own
    /// <see cref="Entity.Get{T}()"/> under the name readers from other engines look for.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to look on.</param>
    /// <returns>The first component of that type, or <see langword="null"/>.</returns>
    /// <remarks>Prefer <c>entity.Get&lt;T&gt;()</c> in new code; this forwarder may be removed in a later release.</remarks>
    public static T? GetComponent<T>(this Entity entity) where T : EntityComponent
        => entity.Get<T>();

    /// <summary>
    /// Every component of type <typeparamref name="T"/> on the entity: Stride's own <see cref="Entity.GetAll{T}()"/>
    /// under the name readers from other engines look for.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to look on.</param>
    /// <returns>The components of that type, in the order they were added.</returns>
    /// <remarks>Prefer <c>entity.GetAll&lt;T&gt;()</c> in new code; this forwarder may be removed in a later release.</remarks>
    public static IEnumerable<T> GetComponents<T>(this Entity entity) where T : EntityComponent
        => entity.GetAll<T>();

    /// <summary>
    /// Removes the entity from its current scene by setting its <see cref="Scene"/> property to null.
    /// </summary>
    /// <param name="entity">The entity to be removed from its current scene.</param>
    public static void Remove(this Entity entity)
        => entity.Scene = null;

    /// <summary>
    /// Finds an entity by name among the top-level entities of the scene this entity is in.
    /// </summary>
    /// <param name="entity">An entity in the scene to search; it is only the way to reach that scene.</param>
    /// <param name="name">The name of the entity to find.</param>
    /// <returns>The first top-level entity with that name, or <see langword="null"/> if none has it. Children are not searched; see <see cref="FindEntityRecursive"/>.</returns>
    /// <exception cref="InvalidOperationException">The entity is not in a scene, so there is nothing to search.</exception>
    /// <remarks>The same as <see cref="SceneExtensions.FindEntity(Scene, string, bool)"/> on <c>entity.Scene</c>; call that directly where the scene is at hand.</remarks>
    public static Entity? FindEntity(this Entity entity, string name)
        => SceneOf(entity, name).FindEntity(name);

    /// <summary>
    /// Finds an entity by name among the top-level entities of the scene this entity is in and all their descendants.
    /// </summary>
    /// <param name="entity">An entity in the scene to search; it is only the way to reach that scene.</param>
    /// <param name="name">The name of the entity to find.</param>
    /// <returns>The first entity with that name, or <see langword="null"/> if none has it.</returns>
    /// <exception cref="InvalidOperationException">The entity is not in a scene, so there is nothing to search.</exception>
    /// <remarks>The same as <see cref="SceneExtensions.FindEntity(Scene, string, bool)"/> with <c>recursive: true</c> on <c>entity.Scene</c>; call that directly where the scene is at hand.</remarks>
    public static Entity? FindEntityRecursive(this Entity entity, string name)
        => SceneOf(entity, name).FindEntity(name, recursive: true);

    // Names both parties, says why, and gives the fix as code: what a beginner needs from an exception
    private static Scene SceneOf(Entity entity, string name)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return entity.Scene ?? throw new InvalidOperationException(
            $"Entity '{entity.Name}' is not in a scene, so it cannot look for '{name}'. " +
            "Add the entity to a scene first (entity.Scene = scene), or search the scene you have: " +
            $"scene.FindEntity(\"{name}\"). A script's Entity has a scene from Start onwards, not in its constructor.");
    }

    /// <summary>
    /// The first component of type <typeparamref name="T"/> on the entity, as a <c>Try</c> method: for when a missing
    /// component is expected rather than a bug.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to look on.</param>
    /// <param name="result">The first component of that type, or <see langword="null"/> when there is none.</param>
    /// <returns><see langword="true"/> when the entity has such a component.</returns>
    public static bool TryGetComponent<T>(this Entity entity, [MaybeNullWhen(false)] out T result) where T : EntityComponent
    {
        result = entity.Get<T>();

        return result is not null;
    }

    /// <summary>
    /// Retrieves the world position of the entity. This is a convenience method to get the <see cref="Matrix.TranslationVector"/>
    /// from the <see cref="TransformComponent.WorldMatrix"/>.
    /// </summary>
    /// <param name="entity">The <see cref="Entity"/> to get the world position from.</param>
    /// <param name="updateTransforms">If true, it will update the world matrix to the current frame's world matrix.</param>
    /// <returns>The <see cref="Vector3"/> representing the world position of the <see cref="Entity"/>.</returns>
    public static Vector3 WorldPosition(this Entity entity, bool updateTransforms = true)
    {
        if (updateTransforms) entity.Transform.UpdateWorldMatrix();

        return entity.Transform.WorldMatrix.TranslationVector;
    }
}