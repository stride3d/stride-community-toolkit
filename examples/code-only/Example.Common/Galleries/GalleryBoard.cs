using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Shapes;
using Stride.Engine;
using Stride.Graphics;

namespace Example.Common.Galleries;

/// <summary>
/// The index board: a HUD panel listing every station, hung in the air where the home view has its
/// top-left corner. The list is world text placed once; the frame is drawn each frame like any
/// other shape. Steer the camera away and the board stays put; going home brings it back to the corner.
/// </summary>
internal sealed class GalleryBoard
{
    /// <summary>
    /// One line of the board, in world units; the frame is the lines plus a margin. The list's
    /// size on screen is set by <see cref="Share"/>, not by this or by the text's font size,
    /// which is only how finely it is rasterised: the board hangs at whatever distance makes it that
    /// share of the home view, so a taller line just means a bigger board at a greater distance.
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

    /// <summary>Places the board in the home camera's frame and adds its list to the scene.</summary>
    /// <param name="scene">The scene the list goes into.</param>
    /// <param name="lines">One line per station. An empty registry keeps its board, with nothing listed.</param>
    /// <param name="homePosition">Where the home camera sits.</param>
    /// <param name="homeRotation">Where the home camera looks.</param>
    public GalleryBoard(Scene scene, IReadOnlyList<string> lines, Vector3 homePosition, Quaternion homeRotation)
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
            Anchor = TextAnchor.TopLeft,
            Alignment = TextAlignment.Left,
            Billboard = false,
        };

        // Hung from the top-left of the frame, inset like the corner brackets, a hair in front, in the board's own plane
        var towardsCamera = Vector3.Cross(_right, _up);
        var entity = new Entity("Index board")
        {
            Transform =
            {
                Position = _centre - _right * (half.X - Inset) + _up * (half.Y - 0.5f) + towardsCamera * 0.01f,
                Rotation = homeRotation,
            },
        };

        entity.Add(list);
        entity.Scene = scene;
    }

    /// <summary>Draws the frame and its corner brackets. The batch's fill, border and glow are set and not restored.</summary>
    public void DrawFrame(ShapeBatch shapes)
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
    }
}