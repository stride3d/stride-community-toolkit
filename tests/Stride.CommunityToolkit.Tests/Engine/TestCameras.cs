using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// Cameras for tests that have no game: on an entity at a position, looking down the Z axis, with the world,
/// view and projection matrices already updated for an aspect ratio.
/// </summary>
internal static class TestCameras
{
    /// <summary>A perspective camera at <paramref name="position"/>.</summary>
    internal static CameraComponent Perspective(Vector3 position, float aspect = 16f / 9f)
        => Updated(new CameraComponent(), position, aspect);

    /// <summary>An orthographic camera of the given view height at <paramref name="position"/>.</summary>
    internal static CameraComponent Orthographic(float size, Vector3 position, float aspect = 16f / 9f)
        => Updated(new CameraComponent { Projection = CameraProjectionMode.Orthographic, OrthographicSize = size }, position, aspect);

    private static CameraComponent Updated(CameraComponent camera, Vector3 position, float aspect)
    {
        var entity = new Entity("camera") { camera };

        entity.Transform.Position = position;
        entity.Transform.UpdateWorldMatrix();

        camera.Update(aspect);

        return camera;
    }
}