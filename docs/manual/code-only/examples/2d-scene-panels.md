---
generated: true
slug: 2d-scene-panels
---

# 2D Panels and Text

Twenty-four HUD panel recipes side by side, each one property away from the last: fills, borders,
rounded corners, glows of three strengths, glass, a ship-console panel with corner ticks and a
gauge, dashed rings and lines that march, gradients, opacity, a translucent border, an additive
glow, a hairline, a twelve-sided badge, a dashed route, overlapping see-through panels and a
concave bracket. Every panel appears twice, alone and carrying world text that shows height,
font size, colour alpha, opacity, glow, alignment and system fonts. Five themes switch live.

The `Program.cs` file shows how to:

- Panels with ShapeBatch - border, fill, glow, dashes, gradient and opacity as captured state
- A fill colour of its own versus a fill derived from the outline colour
- Showing transparency honestly by drawing a stripe behind every panel
- Dashes on rings and lines, animated by advancing the phase
- A fill gradient to a colour, and to alpha 0 for a glass fade
- One opacity over a whole panel
- Pixel-width lines and arcs as HUD ornaments that survive zooming
- An additive glow against a covering one, and a glow on a see-through panel
- A translucent border, a hairline border, and a polygon of any vertex count
- Strokes with DrawPixelPolyline - a dashed route and a concave, closed frame
- World text styling - Height, FontSize, TextColor alpha, Opacity, GlowColor and GlowSize
- Installed system fonts with SystemFonts, in four styles
- A live theme switch through a DebugOverlay dropdown

![2D Panels and Text](media/2d-scene-panels.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E03_2D_Panels).

[!code-csharp[](../../../../examples/code-only/E03_2D_Panels/Program.cs?start=1&end=627)]