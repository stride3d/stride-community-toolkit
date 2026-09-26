---
generated: true
slug: easing-3d-game
---

# Easing in a 3D Game

Easing doing real work in a 3D scene, four ways, each one a Tween: a kinematic platform lifts
a stack of physics bodies on a sine curve and comes back down, a door swings on a cubic curve,
crystals pop in with an overshoot and bob forever, and the camera flies between two viewpoints
on a smoother-step. The platform shows how an eased value drives a Bepu body: as a target the
body chases with a velocity, never as a transform write.

The `Program.cs` file shows how to:

- Tween - start, feed the frame time, read Lerp or Slerp
- Driving a kinematic Bepu body towards an eased target with feed-forward velocity plus a correction
- TweenLoop.PingPong for a lift and a bob, TweenLoop.None for a door and a camera flight
- A staggered pop-in by starting tweens with a negative head start
- A camera flight as one tween over position and rotation

View on [GitHub](https://github.com/stride3d/stride-community-toolkit/tree/main/examples/code-only/E02_3D_EasingInGame).

[!code-csharp[](../../../../examples/code-only/E02_3D_EasingInGame/Program.cs?start=1&end=292)]