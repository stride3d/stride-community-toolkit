using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using System.Globalization;

namespace E03_2D_HUD_Basics;

/// <summary>
/// The HUD of the tutorial as a class: the drawing that steps 1 to 7 wrote as local functions in
/// <c>Program.cs</c>, moved here unchanged apart from where the values come from.
/// </summary>
/// <remarks>
/// The game sets <see cref="Health"/>, <see cref="Energy"/> and <see cref="Flash"/> and calls
/// <see cref="Draw"/> once a frame. The HUD knows nothing about keys or about how the game arrives
/// at those numbers, which is what lets it be reused.
/// </remarks>
public sealed class SimpleHud
{
    private readonly ShapeBatch _shapes;
    private readonly HudStyle _style;
    private readonly EntityTextComponent _healthLabel;
    private readonly EntityTextComponent _energyLabel;

    #region Step8Class
    public SimpleHud(Game game, Scene scene, ShapeBatch shapes, HudStyle style)
    {
        _shapes = shapes;
        _style = style;

        game.AddEntityTextRenderer();

        _healthLabel = AddLabel(scene, TextAnchor.BottomLeft);
        _energyLabel = AddLabel(scene, TextAnchor.MiddleCenter);
    }

    /// <summary>From 0 to 1.</summary>
    public float Health { get; set; } = 1f;

    /// <summary>From 0 to 1.</summary>
    public float Energy { get; set; }

    /// <summary>How strong the damage flash is, from 1 when a hit lands down to 0.</summary>
    public float Flash { get; set; }

    /// <summary>Whether the labels are shown. The shapes need no such switch: not drawing them is enough.</summary>
    public bool Visible
    {
        set => _healthLabel.Opacity = _energyLabel.Opacity = value ? 1f : 0f;
    }

    /// <summary>Draws the whole HUD. Call it once a frame.</summary>
    /// <param name="seconds">The game's total time, which the warning light blinks by.</param>
    public void Draw(float seconds)
    {
        _shapes.Screen = true;

        DrawDamageFlash();
        DrawPanel();
        DrawHealthBar();
        DrawEnergyDial();
        DrawCrosshair();
        UpdateLabels();
        DrawWarningLight(seconds);

        _shapes.Screen = false;
    }
    #endregion

    private Vector2 PanelCentre
        => _shapes.Corner(ScreenCorner.BottomLeft) + new Vector2(_style.Margin + _style.PanelSize.X * 0.5f, -_style.Margin - _style.PanelSize.Y * 0.5f);

    private Vector2 DialCentre
        => _shapes.Corner(ScreenCorner.BottomRight) + new Vector2(-_style.Margin - _style.DialRadius - _style.DialWidth, -_style.Margin - _style.DialRadius - _style.DialWidth);

    private void Rectangle(Vector2 centre, Vector2 size, Color colour, float cornerRadius = 0f)
        => _shapes.DrawRectangle(new Vector3(centre, 0f), Vector3.UnitX, Vector3.UnitY, size, colour, cornerRadius);

    private void Outline(float width = 2f)
    {
        _shapes.BorderWidth = width;
        _shapes.Fill.Set(null, 0f);
    }

    private void Solid()
    {
        _shapes.BorderWidth = 0f;
        _shapes.Fill.Set(null, 1f);
    }

    private void DrawPanel()
    {
        _shapes.BorderWidth = 2f;
        _shapes.Fill.Set(_style.PanelFill, 0.8f);

        Rectangle(PanelCentre, _style.PanelSize, _style.PanelEdge, cornerRadius: 14f);
    }

    private void DrawHealthBar()
    {
        var left = PanelCentre + new Vector2(-_style.BarSize.X * 0.5f, 16f);

        Outline();
        Rectangle(left + new Vector2(_style.BarSize.X * 0.5f, 0f), _style.BarSize, _style.TrackEdge, cornerRadius: 5f);

        var colour = Health > _style.LowHealth ? _style.Healthy : _style.Hurt;
        var size = new Vector2((_style.BarSize.X - 8f) * Health, _style.BarSize.Y - 8f);

        Solid();
        Rectangle(left + new Vector2(4f + size.X * 0.5f, 0f), size, colour, cornerRadius: 2f);
    }

    private void DrawEnergyDial()
    {
        Solid();
        _shapes.DrawArc(DialCentre, _style.DialRadius, 0f, MathF.Tau, _style.DialTrack, _style.DialWidth);
        _shapes.DrawArc(DialCentre, _style.DialRadius, -MathF.PI * 0.5f, MathF.Tau * Energy, _style.Energy, _style.DialWidth);
    }

    private void DrawCrosshair()
    {
        var centre = _shapes.ScreenSize * 0.5f;

        Outline();
        _shapes.DrawRing(new Vector3(centre, 0f), Vector3.UnitZ, 14f, Color.White);

        foreach (var direction in (Vector2[])[new(1f, 0f), new(-1f, 0f), new(0f, 1f), new(0f, -1f)])
        {
            _shapes.DrawPixelLine(new Vector3(centre + direction * 20f, 0f), new Vector3(centre + direction * 32f, 0f), 2f, Color.White);
        }

        Solid();
        _shapes.DrawPixelDisc(new Vector3(centre, 0f), 2f, Color.White);
    }

    private static EntityTextComponent AddLabel(Scene scene, TextAnchor anchor)
    {
        var label = new EntityTextComponent
        {
            Text = "",
            FontSize = 16,
            Anchor = anchor,
            PositionMode = TextPositionMode.Screen,
            EnableShadow = true,
        };

        scene.Entities.Add(new Entity("HUD label") { label });

        return label;
    }

    private void UpdateLabels()
    {
        _healthLabel.Text = string.Create(CultureInfo.InvariantCulture, $"HEALTH {Health * 100f:0}%");
        _healthLabel.ScreenPosition = PanelCentre + new Vector2(-_style.BarSize.X * 0.5f, -6f);

        _energyLabel.Text = string.Create(CultureInfo.InvariantCulture, $"{Energy * 100f:0}%");
        _energyLabel.ScreenPosition = DialCentre;
    }

    private void DrawWarningLight(float seconds)
    {
        if (Health > _style.LowHealth) return;

        var centre = new Vector2(_shapes.ScreenSize.X * 0.5f, _style.Margin + 16f);

        Solid();
        _shapes.Glow.Set(18f);
        _shapes.Glow.Strength = 0.5f;
        _shapes.Opacity = 0.4f + 0.6f * MathF.Abs(MathF.Sin(seconds * MathF.Tau));

        _shapes.DrawDisc(new Vector3(centre, 0f), Vector3.UnitZ, 10f, _style.Hurt);

        _shapes.Glow.Clear();
        _shapes.Opacity = 1f;
    }

    private void DrawDamageFlash()
    {
        if (Flash <= 0f) return;

        Solid();
        _shapes.Opacity = Flash * 0.35f;

        Rectangle(_shapes.ScreenSize * 0.5f, _shapes.ScreenSize, _style.Hurt);

        _shapes.Opacity = 1f;
    }
}