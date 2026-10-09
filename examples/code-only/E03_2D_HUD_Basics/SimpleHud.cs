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
    private readonly ShapeBatch _shapeBatch;
    private readonly HudStyle _style;
    private readonly EntityTextComponent _healthLabel;
    private readonly EntityTextComponent _energyLabel;
    private readonly EntityTextComponent _warningLabel;

    #region Step8Class
    public SimpleHud(Game game, Scene scene, ShapeBatch shapeBatch, HudStyle style)
    {
        _shapeBatch = shapeBatch;
        _style = style;

        game.AddEntityTextRenderer();

        _healthLabel = AddLabel(scene, TextAnchor.BottomLeft);
        _energyLabel = AddLabel(scene, TextAnchor.MiddleCenter);

        _warningLabel = AddLabel(scene, TextAnchor.MiddleCenter);
        _warningLabel.Text = "WARNING";
        _warningLabel.TextColor = style.Hurt;
        _warningLabel.FontSize = 20;
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
        set
        {
            _healthLabel.Opacity = _energyLabel.Opacity = value ? 1f : 0f;

            // The warning shows itself when the health is low, and only while the HUD is drawn
            if (!value) _warningLabel.IsVisible = false;
        }
    }

    /// <summary>Draws the whole HUD. Call it once a frame.</summary>
    /// <param name="seconds">The game's total time, which the warning panel pulses by.</param>
    public void Draw(float seconds)
    {
        _shapeBatch.Screen = true;

        DrawDamageFlash();
        DrawPanel();
        DrawHealthBar();
        DrawEnergyDial();
        DrawCrosshair();
        UpdateLabels();
        DrawWarning(seconds);

        _shapeBatch.Screen = false;
    }
    #endregion

    private Vector2 PanelCentre
        => _shapeBatch.Corner(ScreenCorner.BottomLeft) + new Vector2(_style.PanelSize.X * 0.5f, -_style.PanelSize.Y * 0.5f) + new Vector2(_style.Margin, -_style.Margin);

    private Vector2 DialCentre
        => _shapeBatch.Corner(ScreenCorner.BottomRight) + new Vector2(-_style.Margin - _style.DialRadius - _style.DialWidth, -_style.Margin - _style.DialRadius - _style.DialWidth);

    private void Outline(float width = 2f)
    {
        _shapeBatch.BorderWidth = width;
        _shapeBatch.Fill.Set(null, 0f);
    }

    private void Solid()
    {
        _shapeBatch.BorderWidth = 0f;
        _shapeBatch.Fill.Set(null, 1f);
    }

    private void DrawPanel()
    {
        _shapeBatch.BorderWidth = 2f;
        _shapeBatch.Fill.Set(_style.PanelFill, 0.8f);

        _shapeBatch.DrawRectangle(PanelCentre, _style.PanelSize, _style.PanelEdge, cornerRadius: 14f);
    }

    private void DrawHealthBar()
    {
        var left = PanelCentre + new Vector2(-_style.BarSize.X * 0.5f, 16f);

        Outline();
        _shapeBatch.DrawRectangle(left + new Vector2(_style.BarSize.X * 0.5f, 0f), _style.BarSize, _style.TrackEdge, cornerRadius: 5f);

        var colour = Health > _style.LowHealth ? _style.Healthy : _style.Hurt;
        var size = new Vector2((_style.BarSize.X - 8f) * Health, _style.BarSize.Y - 8f);

        Solid();
        _shapeBatch.DrawRectangle(left + new Vector2(4f + size.X * 0.5f, 0f), size, colour, cornerRadius: 2f);
    }

    private void DrawEnergyDial()
    {
        Solid();
        _shapeBatch.DrawArc(DialCentre, _style.DialRadius, 0f, MathF.Tau, _style.DialTrack, _style.DialWidth);
        _shapeBatch.DrawArc(DialCentre, _style.DialRadius, -MathF.PI * 0.5f, MathF.Tau * Energy, _style.Energy, _style.DialWidth);
    }

    private void DrawCrosshair()
    {
        var centre = _shapeBatch.ScreenSize * 0.5f;

        Outline();
        _shapeBatch.DrawRing(centre, 14f, Color.White);

        _shapeBatch.DrawPixelLine(centre + new Vector2(20f, 0f), centre + new Vector2(32f, 0f), 2f, Color.White);
        _shapeBatch.DrawPixelLine(centre + new Vector2(-20f, 0f), centre + new Vector2(-32f, 0f), 2f, Color.White);
        _shapeBatch.DrawPixelLine(centre + new Vector2(0f, 20f), centre + new Vector2(0f, 32f), 2f, Color.White);
        _shapeBatch.DrawPixelLine(centre + new Vector2(0f, -20f), centre + new Vector2(0f, -32f), 2f, Color.White);

        Solid();
        _shapeBatch.DrawPixelDisc(new Vector3(centre, 0f), 2f, Color.White);
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

    private void DrawWarning(float seconds)
    {
        _warningLabel.IsVisible = Health <= _style.LowHealth;

        if (Health > _style.LowHealth) return;

        var centre = new Vector2(_shapeBatch.ScreenSize.X * 0.5f, _style.Margin + _style.WarningSize.Y * 0.5f);
        var pulse = 0.4f + 0.6f * MathF.Abs(MathF.Sin(seconds * MathF.Tau));

        _shapeBatch.BorderWidth = 2f;
        _shapeBatch.Fill.Set(_style.PanelFill, 0.8f);
        _shapeBatch.Glow.Set(18f);
        _shapeBatch.Glow.Strength = 0.5f;
        _shapeBatch.Opacity = pulse;

        _shapeBatch.DrawRectangle(centre, _style.WarningSize, _style.Hurt, cornerRadius: 10f);

        _shapeBatch.Glow.Clear();
        _shapeBatch.Opacity = 1f;

        _warningLabel.ScreenPosition = centre;
        _warningLabel.Opacity = pulse;
    }

    private void DrawDamageFlash()
    {
        if (Flash <= 0f) return;

        Solid();
        _shapeBatch.Opacity = Flash * 0.35f;

        _shapeBatch.DrawRectangle(_shapeBatch.ScreenSize * 0.5f, _shapeBatch.ScreenSize, _style.Hurt);

        _shapeBatch.Opacity = 1f;
    }
}