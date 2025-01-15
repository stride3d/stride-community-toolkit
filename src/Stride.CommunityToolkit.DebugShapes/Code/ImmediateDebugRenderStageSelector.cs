// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;
using System.ComponentModel;
using static Stride.CommunityToolkit.Rendering.DebugShapes.ImmediateDebugRenderFeature;

namespace Stride.CommunityToolkit.Rendering.DebugShapes;

public class ImmediateDebugRenderStageSelector : RenderStageSelector
{
    [DefaultValue(RenderGroupMask.All)]
    public RenderGroupMask RenderGroup { get; set; } = RenderGroupMask.All;

    [DefaultValue(null)]
    public RenderStage? OpaqueRenderStage { get; set; }

    [DefaultValue(null)]
    public RenderStage? TransparentRenderStage { get; set; }

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