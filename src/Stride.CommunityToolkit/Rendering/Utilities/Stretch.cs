namespace Stride.CommunityToolkit.Rendering.Utilities;

public partial class TextureCanvas
{
    /// <summary>
    ///     Stretch modes when textures to a <see cref="TextureCanvas" />
    /// </summary>
    public enum Stretch
    {
        /// <summary>
        ///     The texture preserves its original size. Overflowing content is cropped.
        /// </summary>
        None,

        /// <summary>
        ///     The texture is resized to fill the destination dimensions. The aspect ratio is not preserved.
        /// </summary>
        Stretch,

        /// <summary>
        ///     The texture is resized to fit in the destination dimensions while it preserves its native aspect ratio.
        /// </summary>
        Contain,

        /// <summary>
        ///     The texture is resized to fill the destination dimensions while it preserves its native aspect ratio. If the aspect
        ///     ratio of the destination rectangle differs from the source, the source texture is clipped to fit in the destination
        ///     dimensions.
        /// </summary>
        Cover
    }
}
