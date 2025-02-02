using Stride.Engine;
using Stride.Rendering;

namespace Example13_RootRendererShader.Renderers;

public class RibbonBackgroundRenderProcessor : EntityProcessor<RibbonBackgroundComponent, RibbonRenderBackground>, IEntityComponentRenderProcessor
{
    public VisibilityGroup VisibilityGroup { get; set; }

    /// <summary>
    /// Gets the active background.
    /// </summary>
    /// <value>The active background.</value>
    public RibbonRenderBackground ActiveBackground { get; private set; }

    /// <inheritdoc />
    protected override void OnSystemRemove()
    {
        if (ActiveBackground != null)
        {
            VisibilityGroup.RenderObjects.Remove(ActiveBackground);
            ActiveBackground = null;
        }

        base.OnSystemRemove();
    }

    /// <inheritdoc />
    protected override RibbonRenderBackground GenerateComponentData(Entity entity, RibbonBackgroundComponent component)
    {
        return new RibbonRenderBackground { Source = component, RenderGroup = component.RenderGroup };
    }

    /// <inheritdoc />
    protected override bool IsAssociatedDataValid(Entity entity, RibbonBackgroundComponent component, RibbonRenderBackground associatedData)
    {
        return component == associatedData.Source && component.RenderGroup == associatedData.RenderGroup;
    }

    /// <inheritdoc />
    public override void Draw(RenderContext context)
    {
        var previousBackground = ActiveBackground;
        ActiveBackground = null;

        // Start by making it not visible
        foreach (var entityKeyPair in ComponentDatas)
        {
            var backgroundComponent = entityKeyPair.Key;
            var renderBackground = entityKeyPair.Value;
            if (backgroundComponent.Enabled)
            {
                // Select the first active background
                renderBackground.Intensity = backgroundComponent.Intensity;
                renderBackground.RenderGroup = backgroundComponent.RenderGroup;
                renderBackground.Speed = backgroundComponent.Speed;
                renderBackground.Frequency = backgroundComponent.Frequency;
                renderBackground.Amplitude = backgroundComponent.Amplitude;
                renderBackground.Top = backgroundComponent.Top.ToVector3();
                renderBackground.Bottom = backgroundComponent.Bottom.ToVector3();
                renderBackground.WidthFactor = backgroundComponent.WidthFactor;

                ActiveBackground = renderBackground;
                break;
            }
        }

        if (ActiveBackground != previousBackground)
        {
            if (previousBackground != null)
                VisibilityGroup.RenderObjects.Remove(previousBackground);
            if (ActiveBackground != null)
                VisibilityGroup.RenderObjects.Add(ActiveBackground);
        }
    }
}