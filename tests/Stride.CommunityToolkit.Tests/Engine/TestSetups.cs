using Stride.CommunityToolkit.Engine;

namespace Stride.CommunityToolkit.Tests.Engine;

/// <summary>
/// Scene setups for the headless games the tests run.
/// </summary>
internal static class TestSetups
{
    /// <summary>The 3D compositor and nothing that needs a shader: the test host has none.</summary>
    internal static SceneSetup3D Bare() => new() { Camera = null, Lighting = null, Skybox = null, Ground = null };
}