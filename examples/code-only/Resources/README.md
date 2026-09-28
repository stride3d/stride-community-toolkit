# Shared example resources

Files here are linked into the examples that use them (see each `.csproj`), so one copy serves them all.

## Particle textures

`smoke.png`, `fire8x8.png`, `flame8x8.png`, `bonfire8x8.png`, `dot.png` and `radial-grad-gray.png` come from the
Stride engine's particle samples (`samples/Particles` in the [stride](https://github.com/stride3d/stride) repository),
MIT licensed. The `8x8` sheets and `smoke.png` are flipbooks of 64 frames; the rest are single frames, white on black
with no alpha channel, made for additive blending. `E09_3D_Particles_Gallery` derives alpha from brightness at load for the
ones that lack it, so the same textures work alpha-blended too.

`smoke-billow.png` is `Smoke01/SMO001.png` from Game Studio's **VFX Package**
(`samples/Templates/Packs/VFXPackage/Resources/Textures` in the same repository), MIT licensed, unchanged: a flipbook of
64 frames of a billowing puff. The gallery's thruster stations use it for exhaust and smoke trails.

## Material textures

`materials/` holds the textures of Game Studio's **Material Package** - the pack the new-game dialog offers - from
`samples/Templates/Packs/MaterialPackage/Resources/Textures` in the [stride](https://github.com/stride3d/stride)
repository, MIT licensed, unchanged: nine sets (brick, gold, iron_blend, marble, rock, rooftile, silver, wood_gloss,
wood_nongloss), 512x512, with diffuse (`_dif`), normal (`_nml`), glossiness (`_gls`), metalness (`_mtl`), specular
(`_spc`), ambient occlusion (`_AO`) and blend mask (`_msk`) maps where the pack has them. `E02_3D_Material_Gallery`
loads colour maps as sRGB and every other map as linear data, and transcribes the pack's own materials from their
`.sdmat` files.