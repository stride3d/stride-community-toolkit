using Stride.CommunityToolkit.Rendering.Gizmos;
using Stride.CommunityToolkit.Scripts;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.CommunityToolkit.Engine;

/// <summary>
/// Provides extension methods for <see cref="Entity"/> to simplify common operations.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Adds an interactive camera script <see cref="Basic3DCameraController"/> to the specified entity, enabling camera movement and rotation through various input methods.
    /// </summary>
    /// <param name="entity">The entity to which the interactive camera script will be added.</param>
    /// <remarks>
    /// The camera entity can be moved using W, A, S, D, Q and E, arrow keys, a gamepad's left stick or dragging/scaling using multi-touch.
    /// Rotation is achieved using the Numpad, the mouse while holding the right mouse button, a gamepad's right stick, or dragging using single-touch.
    /// </remarks>
    public static void Add3DCameraController(this Entity entity)
        => entity.Add(new Basic3DCameraController());

    public static void Add2DCameraController(this Entity entity)
        => entity.Add(new Basic2DCameraController());

    /// <summary>
    /// Adds a TranslationGizmo to the specified entity with optional custom colors.
    /// </summary>
    /// <param name="entity">The entity to which the gizmo will be added.</param>
    /// <param name="graphicsDevice">The graphics device used for rendering the gizmo.</param>
    /// <param name="redColor">Optional custom color for the X-axis of the gizmo. If not specified, a default color is used.</param>
    /// <param name="greenColor">Optional custom color for the Y-axis of the gizmo. If not specified, a default color is used.</param>
    /// <param name="blueColor">Optional custom color for the Z-axis of the gizmo. If not specified, a default color is used.</param>
    /// <example>
    /// This example shows how to add a gizmo to an entity with the default colors:
    /// <code>
    /// var entity = new Entity();
    /// // Assume 'game' is an existing Game instance
    /// entity.AddGizmo(game.GraphicsDevice);
    /// </code>
    /// </example>
    public static void AddGizmo(this Entity entity, GraphicsDevice graphicsDevice, Color? redColor = null, Color? greenColor = null, Color? blueColor = null, bool showAxisName = false, bool rotateAxisNames = true)
    {
        var gizmo = new TranslationGizmo(graphicsDevice, redColor, greenColor, blueColor);

        gizmo.Create(entity, showAxisName, rotateAxisNames);
    }

    /// <summary>
    /// Adds a directional light gizmo to the specified entity for visual representation and manipulation in the editor or during runtime.
    /// </summary>
    /// <param name="entity">The entity to which the directional light gizmo will be added.</param>
    /// <param name="graphicsDevice">The graphics device used to render the gizmo.</param>
    /// <param name="color">Optional color for the gizmo. If not specified, a default color is used.</param>
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
    /// </example>///
    public static void AddLightDirectionalGizmo(this Entity entity, GraphicsDevice graphicsDevice, Color? color = null)
    {
        var gizmo = new LightDirectionalGizmo(graphicsDevice, color);

        gizmo.Create(entity);
    }

    /// <summary>
    /// Retrieves the first component of the specified type from the entity.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <returns>The first component of the specified type, or null if no such component exists.</returns>
    public static T? GetComponent<T>(this Entity entity)
    {
        var result = entity.OfType<T>().FirstOrDefault();

        return result;
    }

    /// <summary>
    /// Recursively searches for the first component of the specified type in the entity's children.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static T? GetComponentInChildren<T>(this Entity entity)
    {
        var result = entity.OfType<T>().FirstOrDefault();

        if (result is null)
        {
            var children = entity.GetChildren();
            foreach(var child in children)
            {
                result = child.GetComponentInChildren<T>();
                if (result != null)
                {
                    return result;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Retrieves all components of the specified type from the entity.
    /// </summary>
    /// <typeparam name="T">The type of components to retrieve.</typeparam>
    /// <returns>An IEnumerable of components of the specified type.</returns>
    public static IEnumerable<T> GetComponents<T>(this Entity entity)
    {
        var result = entity.OfType<T>();

        return result;
    }

    /// <summary>
    /// Removes the entity from its current scene by setting its <see cref="Scene"/> property to null.
    /// </summary>
    /// <param name="entity">The entity to be removed from its current scene.</param>
    public static void Remove(this Entity entity)
    {
        entity.Scene = null;
    }

    /// <summary>
    /// Searches for an entity by name within the top-level entities of the current scene.
    /// </summary>
    /// <param name="entity">The reference entity used to access the scene.</param>
    /// <param name="name">The name of the entity to find.</param>
    /// <returns>The first entity matching the specified name, or null if no match is found. This search does not include child entities.</returns>
    public static Entity? FindEntity(this Entity entity, string name)
    {
        return entity.Scene.Entities.FirstOrDefault(w => w.Name == name);
    }

    /// <summary>
    /// Searches for an entity by name within the top-level entities of the current scene.
    /// </summary>
    /// <param name="parent">The reference entity used to access the scene.</param>
    /// <param name="name">The name of the entity to find.</param>
    /// <returns>The first entity matching the specified name, or null if no match is found. This search does not include child entities.</returns>
    public static Entity? FindEntityRecursive(this Entity parent, string name)
    {
        var entities = parent.Scene.Entities;
        foreach(var entity in entities)
        {
            if (entity.Name == name)
            {
                return entity;
            }
            var child = entity.FindChild(name);
            if(child != null && child.Name == name)
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to retrieve a component of type <typeparamref name="T"/> from the given entity.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="entity">The entity from which to retrieve the component.</param>
    /// <param name="result">When this method returns, contains the first component of type</param>
    /// <returns>
    /// <c>true</c> if a component of type <typeparamref name="T"/> is found in the entity;
    /// otherwise, <c>false</c>.
    /// </returns>
    public static bool TryGetComponent<T>(this Entity entity, out T? result)
    {
        result = entity.GetComponent<T>();
        if (result is null)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// An easier way to get the previous frames world position rather than getting <see cref="Matrix.TranslationVector"/> from <see cref="TransformComponent.WorldMatrix"/>
    /// </summary>
    /// <param name="entity">The <see cref="Entity"/> to get the World Position</param>
    /// <param name="updateTransforms"> If true it will get the current frames world matrix</param>
    /// <returns>The <see cref="Vector3"/> as the World Position of the <see cref="Entity"/></returns>
    public static Vector3 WorldPosition(this Entity entity, bool updateTransforms = true)
    {
        if (updateTransforms)
        {
            entity.Transform.UpdateWorldMatrix();
        }

        return entity.Transform.WorldMatrix.TranslationVector;
    }
}