using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;

namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// One piece of chart text - a tick label, a title, a legend name, the cursor readout - in whichever
/// mode <see cref="ChartLabelOptions.Mode"/> asks for: screen-sized <see cref="EntityTextComponent"/> or
/// billboarded <see cref="WorldTextComponent"/>. Everything that differs between the two lives here, so
/// the rest of the chart places text without knowing which it got.
/// </summary>
/// <remarks>
/// Offsets are given in pixels for both modes. World text has no pixels, so its offset is scaled as if
/// the text were sixteen pixels tall - the default screen font size - which keeps a label the same
/// distance from its tick, relative to its own height, in either mode.
/// </remarks>
internal sealed class ChartText : IDisposable
{
    private const float PixelsPerWorldHeight = 16f;

    private readonly EntityTextComponent? _screen;
    private readonly WorldTextComponent? _world;

    /// <summary>The entity carrying the text; parent it to the chart and move it to place the text.</summary>
    internal Entity Entity { get; }

    /// <summary>Creates a chart text entity in the requested mode.</summary>
    /// <param name="labels">The chart's label options: mode, colour and default size.</param>
    /// <param name="name">The entity's name.</param>
    /// <param name="fontSize">Screen font size in pixels; <see langword="null"/> for the labels' default.</param>
    /// <param name="worldHeight">World text height in chart units; <see langword="null"/> for the labels' default.</param>
    internal ChartText(ChartLabelOptions labels, string name, float? fontSize = null, float? worldHeight = null)
    {
        Entity = new Entity(name);

        if (labels.Mode == ChartLabelMode.Screen)
        {
            Entity.Add(_screen = new EntityTextComponent
            {
                Text = string.Empty,
                FontSize = fontSize ?? labels.FontSize,
                TextColor = labels.Color,
            });
        }
        else
        {
            Entity.Add(_world = new WorldTextComponent
            {
                Text = string.Empty,
                Height = worldHeight ?? labels.Height,
                TextColor = labels.Color,
                Billboard = true,
                KeepUpright = true,
            });
        }
    }

    /// <summary>
    /// Registers the renderer the chart's label mode needs; harmless when already registered, and needed
    /// beyond tick labels because the legend, titles and cursor draw text even when labels are off.
    /// </summary>
    internal static void EnsureRenderer(IGame game, ChartLabelMode mode)
    {
        if (mode == ChartLabelMode.Screen)
            game.AddEntityTextRenderer();
        else
            game.AddWorldTextRenderer();
    }

    /// <summary>Sets what the text says and how it hangs off its position.</summary>
    /// <param name="text">The text.</param>
    /// <param name="anchor">Which point of the text sits at the entity's position.</param>
    /// <param name="pixelOffset">A nudge from that position, in screen pixels, y down.</param>
    internal void Set(string text, Anchor anchor, Vector2 pixelOffset)
    {
        if (_screen is not null)
        {
            _screen.Text = text;
            _screen.Anchor = anchor;
            _screen.Offset = pixelOffset;
        }

        if (_world is not null)
        {
            _world.Text = text;
            _world.Anchor = anchor;
            _world.Offset = new Vector3(pixelOffset.X, -pixelOffset.Y, 0f) * (_world.Height / PixelsPerWorldHeight);
        }
    }

    /// <summary>Changes the text alone.</summary>
    internal string Text
    {
        set
        {
            if (_screen is not null)
                _screen.Text = value;

            if (_world is not null)
                _world.Text = value;
        }
    }

    /// <summary>Shows or hides the text.</summary>
    internal bool Visible
    {
        set
        {
            if (_screen is not null)
                _screen.IsVisible = value;

            if (_world is not null)
                _world.IsVisible = value;
        }
    }

    /// <summary>Moves the text, in chart units.</summary>
    internal Vector3 Position
    {
        set => Entity.Transform.Position = value;
    }

    /// <summary>
    /// Takes the text out of the chart. An entity is a scene object rather than a resource: leaving its
    /// parent is what removes it, and the engine's own reference counting does the rest.
    /// </summary>
    public void Dispose() => Entity.Transform.Parent = null;
}