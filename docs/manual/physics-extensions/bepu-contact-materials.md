# Bepu: Bounce and Friction

Every Bepu collidable, a `BodyComponent` or a `StaticComponent`, has four properties that decide
what happens where it touches another one. Nothing bounces with the default values, and the way two
collidables combine their values decides which of them has any effect.

The [Physics Materials](../code-only/examples/physics-materials.md) example shows every case on
this page and prints what it measures.

## Properties

| Property | Default | Meaning |
|---|---|---|
| `SpringFrequency` | 30 | Stiffness of the contact, in hertz. Lower is softer. |
| `SpringDampingRatio` | 3 | Share of a hit the contact absorbs. 0 returns all of it, 1 or more returns none. |
| `FrictionCoefficient` | 1 | Resistance to sliding. 0 is frictionless. |
| `MaximumRecoveryVelocity` | 1000 | Highest speed at which two overlapping collidables are pushed apart. Also selects whose spring is used, see below. |

```csharp
var ball = new BodyComponent
{
    Collider = new CompoundCollider(),
    SpringFrequency = 10f,
    SpringDampingRatio = 0f,
};
```

## How two collidables combine

A contact has one set of values, made from the two collidables that touch.

| Value of the pair | Rule |
|---|---|
| Friction | The two coefficients multiplied. |
| Maximum recovery velocity | The larger of the two. |
| Spring frequency and damping ratio | Taken together from the collidable with the larger `MaximumRecoveryVelocity`. When the two are equal and one side is a static, the body's are used. |

The springs are not blended. One collidable's spring is used and the other's is ignored.

## Bounce

A contact bounces when its damping ratio is below 1. The default ratio of 3 absorbs the whole hit,
so the default spring does not bounce.

How much comes back then depends on the frequency. The simulation takes 60 steps a second, and a
stiff contact is over in a step or two, which loses most of the bounce. The lower the frequency, the
more of it survives.

Rebound of a ball dropped 3 m onto the default ground, as a share of the drop, at 60 steps a second:

| `SpringFrequency`, with a damping ratio of 0 | Rebound |
|---|---|
| 30 | 9% |
| 20 | 18% |
| 15 | 27% |
| 10 | 40% |
| 5 | 59% |

| `SpringDampingRatio`, at 10 Hz | Rebound |
|---|---|
| 0 | 40% |
| 0.1 | 22% |
| 0.2 | 13% |
| 0.5 | 3% |
| 1 | 0% |

A lower frequency is also softer: at 5 Hz the ball sinks about 0.2 m into the ground before it
comes back.

### A bouncy surface

A static with a bouncy spring does nothing under a body with default values, because the two
`MaximumRecoveryVelocity` values are equal and the body's spring is used. Raise the static's value
above the body's:

```csharp
var trampoline = new StaticComponent
{
    Collider = new CompoundCollider(),
    SpringFrequency = 10f,
    SpringDampingRatio = 0f,
    MaximumRecoveryVelocity = 2000f,
};
```

| Pad under a ball with default values | Rebound |
|---|---|
| 10 Hz, damping 0, `MaximumRecoveryVelocity` 1000 | 0% |
| 10 Hz, damping 0, `MaximumRecoveryVelocity` 2000 | 40% |

The reverse also holds: lowering `MaximumRecoveryVelocity` on a bouncy body below the ground's hands
the contact to the ground's spring, and the body stops bouncing.

## Friction

Friction is the product of the two coefficients, so a coefficient of 0 on either side removes it
whatever the other side has. A body of 4 on a surface of 0.5 slides like a body of 1 on a surface
of 2.

> [!NOTE]
> Stride 4.4 uses BepuPhysics 2.5.0-beta.28, which divides the friction of a contact between two
> convex shapes by its number of contact points. A box resting on a face has four, so it slides as
> if the friction were a quarter of the product: a box with the default coefficient of 1 slides down
> a 30 degree slope. Later Bepu versions apply the full product. Until Stride moves to one,
> multiply the coefficient to compensate. A sphere has one contact point and is not affected.

## See also

- [Bepu: Who Owns the Transform?](bepu-transform-ownership.md)
- [Physics Materials example](../code-only/examples/physics-materials.md)