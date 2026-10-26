namespace Stride.CommunityToolkit.Charts;

/// <summary>
/// The multiples of a step inside a range - where the ticks, labels and grid lines of an axis go - as a
/// struct that <c>foreach</c> walks without allocating, since the grid and the axes walk them every frame.
/// Computed from integer multiples so accumulated float error cannot drop the last one, and a value within
/// a ten-thousandth of a step of an edge still counts as inside it.
/// </summary>
/// <param name="step">The spacing.</param>
/// <param name="skip">A second spacing whose multiples are left out, or 0 to leave none out: the minor grid skips the lines the major one draws.</param>
/// <param name="first">The first multiple, as a count of steps.</param>
/// <param name="last">The last multiple, as a count of steps; below <paramref name="first"/> when there is none.</param>
internal readonly struct TickRange(float step, float skip, int first, int last)
{
    /// <summary>
    /// Every multiple of <paramref name="step"/> in [<paramref name="min"/>, <paramref name="max"/>], less
    /// the multiples of <paramref name="skip"/>; nothing for a step that is not positive.
    /// </summary>
    internal static TickRange Of(float min, float max, float step, float skip = 0f)
        => step > 0f
            ? new(step, skip, (int)MathF.Ceiling(min / step - 1e-4f), (int)MathF.Floor(max / step + 1e-4f))
            : new(step, skip, 0, -1);

    public Enumerator GetEnumerator() => new(step, skip, first, last);

    internal struct Enumerator(float step, float skip, int first, int last)
    {
        private int _index = first - 1;

        public float Current { get; private set; }

        public bool MoveNext()
        {
            while (++_index <= last)
            {
                Current = _index * step;

                // A multiple of the other spacing is that weight's line, left out so the two do not stack
                if (skip > 0f && MathF.Abs(Current / skip - MathF.Round(Current / skip)) < 1e-4f) continue;

                return true;
            }

            return false;
        }
    }
}