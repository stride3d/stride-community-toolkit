// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;
using System.ComponentModel;

namespace Stride.CommunityToolkit.DebugShapes.Code;

/// <summary>
/// Selects render stages for immediate debug rendering based on the render group and debug render stage.
/// </summary>
/// <remarks>This class allows configuration of render stages for opaque and transparent debug rendering. Render
/// objects are processed based on their render group and debug stage, and assigned to the appropriate active render
/// stage if applicable.</remarks>
public class ImmediateDebugRenderStageSelector : RenderStageSelector
{
    /// <summary>
    /// Gets or sets the render group mask that determines which render groups are included in rendering operations.
    /// </summary>
    [DefaultValue(RenderGroupMask.All)]
    public RenderGroupMask RenderGroup { get; set; } = RenderGroupMask.All;

    /// <summary>
    /// Gets or sets the render stage used for rendering opaque objects.
    /// </summary>
    /// <remarks>Assigning a value to this property allows customization of the rendering process for opaque
    /// objects.  If set to <see langword="null"/>, the default rendering behavior will be used.</remarks>
    [DefaultValue(null)]
    public RenderStage? OpaqueRenderStage { get; set; }

    /// <summary>
    /// Gets or sets the render stage used for rendering transparent objects.
    /// </summary>
    /// <remarks>Use this property to specify a custom render stage for handling transparent objects in the
    /// rendering pipeline.  If set to <see langword="null"/>, transparent objects will not be processed by a dedicated
    /// render stage.</remarks>
    [DefaultValue(null)]
    public RenderStage? TransparentRenderStage { get; set; }

    /// <inheritdoc/>
    public override void Process(RenderObject renderObject)
    {
        if (((RenderGroupMask)(1U << (int)renderObject.RenderGroup) & RenderGroup) != 0)
        {
            var debugObject = (ImmediateDebugRenderObject)renderObject;
            var renderStage = debugObject.Stage == DebugRenderStage.Opaque ? OpaqueRenderStage : TransparentRenderStage;

            if (renderStage != null)
                renderObject.ActiveRenderStages[renderStage.Index] = new ActiveRenderStage(null);
        }
    }
}