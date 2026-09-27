using Stride.Core.Mathematics;

namespace E03_2D_HUD;

/// <summary>
/// The radar's contacts as a table: callsign, type, bearing and range. The target's row is lit.
/// Click a row to make that contact the target.
/// </summary>
public sealed class ContactsWidget(ShipState ship) : HudWidget
{
    private const string Name = "contacts";
    private const float Captions = 0.36f;

    // Where each column sits, as a fraction of the row's width
    private const float Callsign = 0.07f;
    private const float Type = 0.4f;
    private const float Bearing = 0.8f;

    public override void Draw(HudCanvas canvas, HudRect bounds)
    {
        var theme = canvas.Theme;
        var content = canvas.Panel(bounds, Name, "CONTACTS", $"{ship.Contacts.Length} TRACKED");
        var captions = content.TakeTop(Captions);
        var rows = content.DropTop(Captions);
        var caption = theme.For(HudRole.Frame).WithAlpha(0.7f);

        canvas.Text(new(Name, "caption", 0), "ID", new Vector2(captions.At(Callsign, 0f).X, captions.Center.Y), HudText.Caption.Left, caption);
        canvas.Text(new(Name, "caption", 1), "TYPE", new Vector2(captions.At(Type, 0f).X, captions.Center.Y), HudText.Caption.Left, caption);
        canvas.Text(new(Name, "caption", 2), "BRG", new Vector2(captions.At(Bearing, 0f).X, captions.Center.Y), HudText.Caption.Right, caption);
        canvas.Text(new(Name, "caption", 3), "KM", new Vector2(captions.Right - 0.1f, captions.Center.Y), HudText.Caption.Right, caption);

        for (var i = 0; i < ship.Contacts.Length; i++)
        {
            DrawRow(canvas, i, rows.Row(i, ship.Contacts.Length, 0.06f));
        }
    }

    private void DrawRow(HudCanvas canvas, int index, HudRect row)
    {
        var theme = canvas.Theme;
        var contact = ship.Contacts[index];
        var id = new HudId(Name, Index: index);
        var highlight = contact.Hostile ? HudTheme.Warning : theme.For(HudRole.Commanded);

        if (canvas.Clicks(id)) ship.TargetIndex = index;

        var selected = canvas.Animate(new(Name, "selected", index), index == ship.TargetIndex ? 1f : 0f);
        var hover = canvas.Animate(new(Name, "hover", index), canvas.Hovers(id) ? 1f : 0f);
        var bar = Color.Lerp(theme.For(HudRole.Frame), highlight, MathF.Max(selected, hover));

        // The row's bar: faint until the pointer or the selection lights it
        canvas.Style(HudCanvas.Thin * selected, 0.04f + 0.08f * hover + 0.14f * selected, bar);
        canvas.Shapes.Tag = id;
        canvas.ChamferedPanel(row.Center, row.Size, 0.1f, bar.WithAlpha(selected));
        canvas.Shapes.Tag = null;

        var colour = contact.Hostile ? HudTheme.Warning : Color.Lerp(theme.Text.WithAlpha(0.75f), theme.Text, MathF.Max(selected, hover));
        var y = row.Center.Y;

        // A diamond for a hostile, a disc for anything else
        var marker = new Vector2(row.Left + 0.22f, y);

        canvas.Style(0f, 1f, colour);

        if (contact.Hostile)
        {
            ReadOnlySpan<Vector2> diamond = [new(0f, -0.11f), new(0.11f, 0f), new(0f, 0.11f), new(-0.11f, 0f)];

            canvas.Shapes.DrawSolidPolygon(diamond, marker, 0f, colour);
        }
        else
        {
            canvas.Shapes.DrawSolidCircle(marker, 0.07f, colour);
        }

        canvas.Text(new(Name, "callsign", index), contact.Callsign, new Vector2(row.At(Callsign, 0f).X, y), HudText.Small.Left, colour);
        canvas.Text(new(Name, "type", index), contact.Type.ToUpperInvariant(), new Vector2(row.At(Type, 0f).X, y), HudText.Small.Left, colour.WithAlpha(0.7f));
        canvas.Text(new(Name, "bearing", index), $"{contact.Bearing:000}", new Vector2(row.At(Bearing, 0f).X, y), HudText.Small.Right, colour);
        canvas.Text(new(Name, "range", index), $"{contact.Range:0.0}", new Vector2(row.Right - 0.1f, y), HudText.Small.Right, colour);
    }
}