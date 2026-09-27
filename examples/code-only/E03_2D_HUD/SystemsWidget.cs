using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// Hull, shield and fuel as three labelled bars of cells. A bar that runs low leaves the theme's
/// colour for amber or red.
/// </summary>
public sealed class SystemsWidget(ShipState ship) : HudWidget
{
    private const string Name = "systems";
    private const int Cells = 24;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var theme = canvas.Theme;
        var content = canvas.Panel(bounds, Name, "SYSTEMS");
        var systems = ship.Systems;
        var healthy = theme.For(HudRole.Engaged);

        ReadOnlySpan<(string Name, float Value, Color Colour)> rows =
        [
            ("HULL", systems.Hull, healthy),
            ("SHIELD", systems.Shield, systems.Shield < 0.35f ? HudTheme.Warning : healthy),
            ("FUEL", systems.Fuel, systems.Fuel < 0.2f ? HudTheme.Caution : healthy),
        ];

        for (var i = 0; i < rows.Length; i++)
        {
            var row = content.Row(i, rows.Length);
            var y = row.Center.Y;

            canvas.Text(new(Name, "name", i), rows[i].Name, new Vector2(row.Left, y), HudText.Caption.Left, theme.For(HudRole.Frame));
            canvas.SegmentedBar(HudRect.Centered(new Vector2(row.Center.X + 0.35f, y), new Vector2(row.Width - 3.4f, 0.3f)), Cells, rows[i].Value, rows[i].Colour, theme.Dim(HudRole.Engaged));
            canvas.Text(new(Name, "value", i), $"{rows[i].Value * 100f:0}%", new Vector2(row.Right, y), HudText.Small.Right, theme.Text);
        }
    }
}