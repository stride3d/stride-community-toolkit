using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.CommunityToolkit.Effects.Picking;

/// <summary>
/// What a <see cref="GpuPicker"/> found under a screen point: nothing, or a mesh and where on it.
/// </summary>
/// <param name="Sequence">Counts the picker's answers, so a caller can tell a new answer from the one it already handled.</param>
/// <param name="ScreenPosition">The point that was asked about, normalised over the viewport with (0, 0) at the top left, as the request gave it.</param>
/// <param name="Entity">The entity whose mesh is under the point, or <see langword="null"/> for the background.</param>
/// <param name="ModelComponent">The model component that drew the pixel, or <see langword="null"/>.</param>
/// <param name="MeshIndex">Which of the model's meshes.</param>
/// <param name="MaterialIndex">Which of the model's materials that mesh uses.</param>
/// <param name="InstanceIndex">For an instanced model, which instance; 0 otherwise.</param>
/// <param name="Depth">The pixel's depth as the depth buffer holds it, 0 at the near plane and 1 at the far plane; 1 for the background.</param>
/// <param name="WorldPosition">The point on the mesh's surface under the screen point, rebuilt from the depth, or <see langword="null"/> for the background.</param>
public sealed record PickResult(
    int Sequence,
    Vector2 ScreenPosition,
    Entity? Entity,
    ModelComponent? ModelComponent,
    int MeshIndex,
    int MaterialIndex,
    int InstanceIndex,
    float Depth,
    Vector3? WorldPosition)
{
    /// <summary>Whether a mesh was under the point at all.</summary>
    public bool Hit => ModelComponent is not null;
}