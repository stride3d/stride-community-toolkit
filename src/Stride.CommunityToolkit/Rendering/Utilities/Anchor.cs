namespace Stride.CommunityToolkit.Rendering.Utilities;

public partial class TextureCanvas
{
    /// <summary>
    ///     Anchor positions when drawing to a <see cref="TextureCanvas" />
    /// </summary>
    public enum Anchor
    {
        /// <summary>
        ///     Adjust the position so the top-left corner of the source and target rect are aligned.
        /// </summary>
        TopLeft,

        /// <summary>
        ///     Adjust the position so the top-edge center of the source and target rect are aligned.
        /// </summary>
        Top,

        /// <summary>
        ///     Adjust the position so the top-right corner of the source and target rect are aligned.
        /// </summary>
        TopRight,

        /// <summary>
        ///     Adjust the position so the left-edge center of the source and target rect are aligned.
        /// </summary>
        Left,

        /// <summary>
        ///     Adjust the position so the center of the source and target rect are aligned.
        /// </summary>
        Center,

        /// <summary>
        ///     Adjust the position so the right-edge center of the source and target rect are aligned.
        /// </summary>
        Right,

        /// <summary>
        ///     Adjust the position so the bottom-left corner of the source and target rect are aligned.
        /// </summary>
        BottomLeft,

        /// <summary>
        ///     Adjust the position so the bottom-edge center of the source and target rect are aligned.
        /// </summary>
        Bottom,

        /// <summary>
        ///     Adjust the position so the bottom-right corner of the source and target rect are aligned.
        /// </summary>
        BottomRight
    }
}
