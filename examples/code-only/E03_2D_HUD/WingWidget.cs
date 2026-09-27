using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The wing: a tile for each wingman with a callsign, a count and a bar of cells. The selected
/// tile is bright and glows, the rest are dim. Click a tile to select it.
/// </summary>
public sealed class WingWidget(ShipState ship) : HudWidget
{
    private const string Name = "wing";
    private const float Gap = 0.14f;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var content = canvas.Panel(bounds, Name, "WING");

        for (var i = 0; i < ship.Wing.Length; i++)
        {
            DrawTile(canvas, i, content.Row(i, ship.Wing.Length, Gap));
        }
    }

    private void DrawTile(HudCanvas canvas, int index, HudRect tile)
    {
        var theme = canvas.Theme;
        var wingman = ship.Wing[index];
        var id = new HudId(Name, Index: index);
        var accent = theme.For(HudRole.Engaged);

        if (canvas.Clicks(id)) ship.SelectedWing = index;

        // Selected against idle is three numbers: border width, fill alpha and glow
        var selected = canvas.Animate(new(Name, "selected", index), index == ship.SelectedWing ? 1f : 0f);
        var hover = canvas.Animate(new(Name, "hover", index), canvas.Hovers(id) ? 1f : 0f);
        var lit = MathF.Max(selected, 0.5f * hover);
        var pulse = 0.5f + 0.5f * MathF.Sin(canvas.Time * 3f);

        canvas.Style(MathUtil.Lerp(HudCanvas.Thin, HudCanvas.Thick, selected), 0.45f + 0.3f * lit, theme.Ground, (4f + 4f * pulse) * selected, theme.GlowFor(HudRole.Engaged), additive: true);
        canvas.Shapes.Tag = id;
        canvas.ChamferedPanel(tile.Center, tile.Size, 0.16f, Color.Lerp(theme.Dim(HudRole.Engaged), accent, lit));
        canvas.Shapes.Tag = null;

        var colour = Color.Lerp(accent, theme.Text, selected);
        var left = tile.Left + 0.3f;

        canvas.Text(new(Name, "callsign", index), wingman.Callsign, new Vector2(left, tile.Center.Y + 0.2f), HudText.Caption.Left, colour);
        canvas.Text(new(Name, "count", index), $"{wingman.Value * Wingman.Capacity:0} / {Wingman.Capacity}", new Vector2(left, tile.Center.Y - 0.2f), HudText.Small.Left, colour);

        canvas.SegmentedBar(HudRect.Centered(new Vector2(tile.Right - 2.0f, tile.Center.Y), new Vector2(3.2f, 0.28f)), 14, wingman.Value, colour, theme.Dim(HudRole.Engaged));
    }
}