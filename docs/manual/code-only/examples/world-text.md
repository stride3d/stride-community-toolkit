---
generated: true
slug: world-text
---

# World Text (In-Scene)

A gallery of everything WorldTextComponent can do, one setting per station: billboarding that
stays upright, free billboarding, text fixed in place, text lying flat on the ground, world-unit
sizing, depth-tested text hidden behind a wall next to text drawn through it, distance fading,
and a glow behind the letters in a few colour combinations - a HUD blue, neon, a readability
halo and a soft bloom. World text lives inside the scene - it shrinks with distance and geometry
can hide it.

The `Program.cs` file shows how to:

- The text renderer registers itself with the first text
- Billboarding: KeepUpright versus facing the camera freely
- Text fixed to a surface or lying flat with Billboard = false
- Height in world units versus FontSize as sharpness
- Depth-tested text hidden by geometry, and DepthTest = false to draw through
- Distance fading with FadeStartDistance and MaxDistance
- A glow behind the letters with GlowColor and GlowSize, from HUD halo to neon bloom

![World Text (In-Scene)](media/world-text.webp)

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E03_3D_WorldText).

[!code-csharp[](../../../../examples/code-only/E03_3D_WorldText/Program.cs?start=1&end=179)]