using Stride.CommunityToolkit.Rendering;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Engine;
using Stride.Graphics;

namespace Example.Common.Galleries;

/// <summary>
/// The index board: a HUD panel listing every station, hung in the air where the home view has its
/// top-left corner. The list is world text placed once; the frame is drawn each frame like any
/// other shape. Steer the camera away and the board stays put; going home brings it back to the corner.
/// Every line of the list is a button: an invisible rectangle tagged with its station, so the same
/// pick that finds a pad under the mouse finds a line, and the gallery flies there on a click.
/// </summary>
internal sealed class GalleryBoard
{
    /// <summary>
    /// One line of the board, in world units; the frame is the lines plus a margin. The size on
    /// screen is set by <see cref="Share"/>: a taller line means a bigger board hung further away.
    /// </summary>
    private const float LineHeight = 0.45f;

    /// <summary>The board's width in world units; its height follows the registry.</summary>
    private const float Width = 10f;

    /// <summary>How much of the home view's height the board takes, and the margin it keeps from the view's top and left edges, in world units at its distance.</summary>
    private const float Share = 0.48f;
    private const float Margin = 0.6f;

    /// <summary>How far in from the frame the corner brackets sit and the list starts.</summary>
    private const float Inset = 0.3f;

    /// <summary>The field of view the board's placement assumes: the default camera's, at a 16:9 window.</summary>
    private const float ViewFovDegrees = 45f;
    private const float ViewAspect = 16f / 9f;

    private readonly Vector3 _centre;
    private readonly Vector3 _right;
    private readonly Vector3 _up;
    private readonly Vector2 _size;

    /// <summary>Where the list's first line starts, and how wide a line's button is: the frame less the insets.</summary>
    private readonly Vector3 _textTopLeft;
    private readonly float _lineWidth;

    /// <summary>Places the board in the home camera's frame and adds its list to the scene.</summary>
    /// <param name="scene">The scene the list goes into.</param>
    /// <param name="lines">One line per station. An empty registry keeps its board, with nothing listed.</param>
    /// <param name="homePosition">Where the home camera sits.</param>
    /// <param name="homeRotation">Where the home camera looks.</param>
    internal GalleryBoard(Scene scene, IReadOnlyList<string> lines, Vector3 homePosition, Quaternion homeRotation)
    {
        _size = new Vector2(Width, MathF.Max(4f, lines.Count * LineHeight + 1f));
        _right = Vector3.Transform(Vector3.UnitX, homeRotation);
        _up = Vector3.Transform(Vector3.UnitY, homeRotation);

        // Far enough in front of the home camera that the board takes its share of the view's
        // height, then across to the view's top-left corner
        var forward = Vector3.Transform(-Vector3.UnitZ, homeRotation);
        var halfTan = MathF.Tan(MathUtil.DegreesToRadians(ViewFovDegrees) * 0.5f);
        var distance = _size.Y / (2f * halfTan * Share);
        var viewHalf = new Vector2(distance * halfTan * ViewAspect, distance * halfTan);
        var half = _size * 0.5f;

        _centre = homePosition
            + forward * distance
            + _right * (-viewHalf.X + Margin + half.X)
            + _up * (viewHalf.Y - Margin - half.Y);

        var list = new WorldTextComponent
        {
            Text = lines.Count > 0 ? string.Join('\n', lines) : " ",
            FontSize = 40,
            // The height is the whole block's, every line of it; hung from the top of the frame
            Height = lines.Count * LineHeight,
            TextColor = new Color(130, 205, 255),
            GlowColor = new Color(0, 140, 255, 120),
            GlowSize = 3f,
            Anchor = Anchor.TopLeft,
            Alignment = TextAlignment.Left,
            Billboard = false,
        };

        // Hung from the top-left of the frame, inset like the corner brackets, a hair in front, in the board's own plane
        var towardsCamera = Vector3.Cross(_right, _up);

        _textTopLeft = _centre - _right * (half.X - Inset) + _up * (half.Y - 0.5f);
        _lineWidth = Width - 2f * Inset;

        var entity = new Entity("Index board")
        {
            Transform =
            {
                Position = _textTopLeft + towardsCamera * 0.01f,
                Rotation = homeRotation,
            },
        };

        entity.Add(list);
        entity.Scene = scene;
    }

    /// <summary>
    /// Draws the frame, its corner brackets and the line buttons, leaving the batch's fill and border
    /// width changed and its glow and tag cleared.
    /// </summary>
    /// <param name="shapes">The gallery's depth-tested batch, the one its pads are picked through.</param>
    /// <param name="stations">The stations, one per line, which become the lines' pick tags.</param>
    /// <param name="hovered">The index of the station under the mouse, or -1; its line is lit.</param>
    /// <param name="current">The index of the station the visitor is at, or -1 at home; its line is marked.</param>
    internal void DrawFrame(ShapeBatch shapes, IReadOnlyList<GalleryStation> stations, int hovered, int current)
    {
        var hudBlue = new Color(110, 200, 255);

        shapes.Fill.Set(new Color(4, 14, 30), 0.8f);
        shapes.BorderWidth = 1.5f;
        shapes.Glow.Set(7f, new Color(0, 150, 255, 160));
        shapes.DrawRectangle(_centre, _right, _up, _size, hudBlue, cornerRadius: 0.35f);
        shapes.Glow.Clear();

        // The corner brackets, the HUD cliche
        var half = _size * 0.5f;
        var topLeft = _centre - _right * (half.X - Inset) + _up * (half.Y - Inset);
        var bottomRight = _centre + _right * (half.X - Inset) - _up * (half.Y - Inset);

        shapes.DrawPixelLine(topLeft, topLeft + _right * 0.8f, 1.5f, hudBlue);
        shapes.DrawPixelLine(topLeft, topLeft - _up * 0.5f, 1.5f, hudBlue);
        shapes.DrawPixelLine(bottomRight, bottomRight - _right * 0.8f, 1.5f, hudBlue);
        shapes.DrawPixelLine(bottomRight, bottomRight + _up * 0.5f, 1.5f, hudBlue);

        // One rectangle per line, behind the text and in front of the frame, tagged with its station so
        // the pick reports it like a pad. Unlit lines draw nothing: no border and a fill at zero alpha,
        // which still picks as the whole rectangle. The lit ones are a wash behind the text
        var towardsCamera = Vector3.Cross(_right, _up);
        var lineSize = new Vector2(_lineWidth, LineHeight);

        shapes.BorderWidth = 0f;

        for (var i = 0; i < stations.Count; i++)
        {
            var centre = _textTopLeft + _right * (_lineWidth * 0.5f) - _up * ((i + 0.5f) * LineHeight) + towardsCamera * 0.005f;

            shapes.Tag = stations[i];
            shapes.Fill.Set(hudBlue, i == hovered ? 0.3f : i == current ? 0.12f : 0f);
            shapes.DrawRectangle(centre, _right, _up, lineSize, hudBlue, cornerRadius: 0.1f);
        }

        shapes.Tag = null;
    }
}