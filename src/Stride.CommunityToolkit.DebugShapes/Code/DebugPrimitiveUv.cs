// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.CommunityToolkit.DebugShapes;

/// <summary>
/// The texture-coordinate values the debug-primitive generators encode wireframe information with:
/// a vertex carrying <see cref="Line"/> sits on a visible wireframe edge, one carrying <see cref="NoLine"/>
/// does not, and the primitive shader turns the interpolated value into the drawn line.
/// </summary>
internal static class DebugPrimitiveUv
{
    /// <summary>Texture coordinate for vertices that are not part of a wireframe line.</summary>
    internal static Vector2 NoLine { get; } = new(0.5f);

    /// <summary>Texture coordinate for vertices on a wireframe line.</summary>
    internal static Vector2 Line { get; } = new(1.0f);

    /// <summary>Error message thrown when a uv split count does not divide the tessellation count.</summary>
    internal const string SplitDivisorErrorMessage = "expected the desired number of uv splits to be a divisor of the number of tessellations";
}