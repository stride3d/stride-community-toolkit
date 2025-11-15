using System.Drawing;

namespace Stride.CommunityToolkit.Examples.Core;

public record Example(string Id, string Title, string? ProjectName, Action Action, string? Category)
{
    private static readonly Color _basicExampleColor = Color.CornflowerBlue;
    private static readonly Color _advancedExampleColor = Color.MediumSeaGreen;
    private static readonly Color _otherExampleColor = Color.Orange;

    public Color GetColor()
        => Category switch
        {
            Constants.BasicExample => _basicExampleColor,
            Constants.AdvanceExample => _advancedExampleColor,
            _ => _otherExampleColor,
        };
}