using Stride.Engine;
using Stride.Rendering;

namespace E09_3D_RootRendererShader.Renderers;

public class RibbonBackgroundRenderProcessor : EntityProcessor<RibbonBackgroundComponent, RibbonRenderBackground>, IEntityComponentRenderProcessor
{
    public VisibilityGroup? VisibilityGroup { get; set; }

    /// <summary>
    /// Gets the active background.
    /// </summary>
    public RibbonRenderBackground? ActiveBackground { get; private set; }

    /// <inheritdoc />
    protected override void OnSystemRemove()
    {
        if (ActiveBackground is not null)
        {
            VisibilityGroup?.RenderObjects.Remove(ActiveBackground);
            ActiveBackground = null;
        }

        base.OnSystemRemove();
    }

    /// <inheritdoc />
    protected override RibbonRenderBackground GenerateComponentData(Entity entity, RibbonBackgroundComponent component)
        => new() { Source = component, RenderGroup = component.RenderGroup };

    /// <inheritdoc />
    protected override bool IsAssociatedDataValid(Entity entity, RibbonBackgroundComponent component, RibbonRenderBackground associatedData)
        => component == associatedData.Source && component.RenderGroup == associatedData.RenderGroup;

    /// <inheritdoc />
    public override void Draw(RenderContext context)
    {
        var previousBackground = ActiveBackground;
        ActiveBackground = null;

        // Select the first enabled background
        foreach (var (backgroundComponent, renderBackground) in ComponentDatas)
        {
            if (backgroundComponent.Enabled)
            {
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

        if (ActiveBackground == previousBackground) return;

        if (previousBackground is not null)
        {
            VisibilityGroup?.RenderObjects.Remove(previousBackground);
        }

        if (ActiveBackground is not null)
        {
            VisibilityGroup?.RenderObjects.Add(ActiveBackground);
        }
    }
}